using StackExchange.Redis;

namespace NeuroSync.Api.Services;

/// <summary>
/// Daily LLM spend and per-user request caps. Counters only — never message text.
/// Uses Redis when it is configured so the cap holds across API instances.
/// </summary>
public sealed class LlmUsageGovernor
{
    private readonly IConfiguration _config;
    private readonly ILogger<LlmUsageGovernor> _logger;
    private readonly LiveBetaMetrics _metrics;
    private readonly IConnectionMultiplexer? _redis;
    private readonly object _gate = new();
    private string _day = "";
    private decimal _spentUsd;
    private readonly Dictionary<string, int> _requestsByUser = new();

    public LlmUsageGovernor(
        IConfiguration config,
        ILogger<LlmUsageGovernor> logger,
        LiveBetaMetrics metrics,
        IConnectionMultiplexer? redis = null)
    {
        _config = config;
        _logger = logger;
        _metrics = metrics;
        _redis = redis;
    }

    public int MaxOutputTokens => Math.Clamp(_config.GetValue("Companion:Llm:MaxTokens", 280), 64, 800);
    public decimal DailyCostLimitUsd => _config.GetValue("Companion:Llm:DailyCostLimitUsd", 5m);
    public int PerUserDailyRequests => Math.Clamp(_config.GetValue("Companion:Llm:PerUserDailyRequests", 200), 1, 5000);

    public bool TryReserve(string? userId, int estimatedPromptTokens, out string reason)
    {
        var estimate = EstimateUsd(estimatedPromptTokens, MaxOutputTokens);
        var day = DateTime.UtcNow.ToString("yyyyMMdd");
        var user = string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId.Trim();

        if (_redis != null)
            return TryReserveRedis(day, user, estimate, out reason);

        lock (_gate)
        {
            ResetIfNewDay(day);
            var count = _requestsByUser.GetValueOrDefault(user);
            if (count >= PerUserDailyRequests)
            {
                reason = "per-user daily request limit";
                _metrics.LlmBudgetRejected();
                _logger.LogWarning("LLM reserved skipped: {Reason}", reason);
                return false;
            }
            if (_spentUsd + estimate > DailyCostLimitUsd)
            {
                reason = "daily cost limit";
                _metrics.LlmBudgetRejected();
                _logger.LogWarning("LLM reserved skipped: {Reason}", reason);
                return false;
            }
            _requestsByUser[user] = count + 1;
            // Reserve the conservative estimated cost before the provider call starts.
            // This closes the concurrency gap where many requests could all pass the
            // budget check before any completion had been recorded.
            _spentUsd += estimate;
            reason = "";
            return true;
        }
    }

    public void Record(string? userId, int promptTokens, int completionTokens, long latencyMs)
    {
        var cost = EstimateUsd(promptTokens, completionTokens);

        // Budget is reserved in TryReserve using the maximum configured output size.
        // Do not charge the budget a second time here; Record tracks actual usage only
        // for metrics/observability.
        _metrics.LlmSuccess(promptTokens, completionTokens, cost, latencyMs);
        _logger.LogInformation(
            "LLM completion model tokens prompt={PromptTokens} completion={CompletionTokens} estimatedUsd={EstimatedUsd} latencyMs={LatencyMs}",
            promptTokens, completionTokens, cost, latencyMs);
    }

    public void RecordTimeout() 
    {
        _metrics.LlmTimeout();
        _logger.LogWarning("LLM completion timed out");
    }

    public void RecordFallback(string reason)
    {
        _metrics.LlmFallback();
        _logger.LogWarning("LLM fallback: {Reason}", reason);
    }

    public decimal EstimateUsd(int promptTokens, int completionTokens)
    {
        var input = _config.GetValue("Companion:Llm:InputPricePerMillionUsd", 0.15m);
        var output = _config.GetValue("Companion:Llm:OutputPricePerMillionUsd", 0.60m);
        var prompt = Math.Max(0, promptTokens);
        var completion = Math.Max(0, completionTokens);
        return (prompt / 1_000_000m) * input + (completion / 1_000_000m) * output;
    }

    public static int EstimateTokens(string? text) =>
        Math.Max(1, ((text?.Length ?? 0) + 3) / 4);

    private bool TryReserveRedis(string day, string user, decimal estimate, out string reason)
    {
        var lockKey = "ns:llm:budget-lock:" + day;
        var lockToken = Guid.NewGuid().ToString("N");
        try
        {
            var db = _redis!.GetDatabase();
            if (!db.LockTake(lockKey, lockToken, TimeSpan.FromSeconds(2)))
            {
                reason = "budget check busy";
                _metrics.LlmBudgetRejected();
                _logger.LogWarning("LLM reservation failed closed: {Reason}", reason);
                return false;
            }

            try
            {
                var costKey = "ns:llm:cost:" + day;
                var spentValue = db.StringGet(costKey);
                var spent = spentValue.IsNullOrEmpty ? 0m : (decimal)(double)spentValue;
                var countKey = $"ns:llm:user:{user}:{day}";
                var countValue = db.StringGet(countKey);
                var count = countValue.IsNullOrEmpty ? 0 : (int)countValue;

                if (count >= PerUserDailyRequests)
                {
                    reason = "per-user daily request limit";
                    _metrics.LlmBudgetRejected();
                    _logger.LogWarning("LLM reserved skipped: {Reason}", reason);
                    return false;
                }

                if (spent + estimate > DailyCostLimitUsd)
                {
                    reason = "daily cost limit";
                    _metrics.LlmBudgetRejected();
                    _logger.LogWarning("LLM reserved skipped: {Reason}", reason);
                    return false;
                }

                db.StringIncrement(countKey);
                db.KeyExpire(countKey, TimeSpan.FromHours(48));
                db.StringIncrement(costKey, (double)estimate);
                db.KeyExpire(costKey, TimeSpan.FromHours(48));
                reason = "";
                return true;
            }
            finally
            {
                db.LockRelease(lockKey, lockToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM budget check failed closed");
            reason = "budget check unavailable";
            _metrics.LlmBudgetRejected();
            return false;
        }
    }

    private void ResetIfNewDay(string day)
    {
        if (_day == day)
            return;
        _day = day;
        _spentUsd = 0;
        _requestsByUser.Clear();
    }
}
