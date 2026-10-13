namespace NeuroSync.Api.Services;

/// <summary>In-process counters for latency, tokens, fallback, and estimated LLM spend. No message text.</summary>
public sealed class LiveBetaMetrics
{
    private long _companionTurns;
    private long _llmSuccess;
    private long _llmFallback;
    private long _llmTimeout;
    private long _llmRejectedByBudget;
    private long _authFailures;
    private long _promptTokens;
    private long _completionTokens;
    private long _lastLatencyMs;
    private long _costMicroUsd;

    public void CompanionTurn(long latencyMs) 
    {
        Interlocked.Increment(ref _companionTurns);
        Interlocked.Exchange(ref _lastLatencyMs, latencyMs);
    }

    public void LlmSuccess(int promptTokens, int completionTokens, decimal costUsd, long latencyMs)
    {
        Interlocked.Increment(ref _llmSuccess);
        Interlocked.Add(ref _promptTokens, promptTokens);
        Interlocked.Add(ref _completionTokens, completionTokens);
        Interlocked.Add(ref _costMicroUsd, (long)(costUsd * 1_000_000m));
        Interlocked.Exchange(ref _lastLatencyMs, latencyMs);
    }

    public void LlmFallback() => Interlocked.Increment(ref _llmFallback);
    public void LlmTimeout() => Interlocked.Increment(ref _llmTimeout);
    public void LlmBudgetRejected() => Interlocked.Increment(ref _llmRejectedByBudget);
    public void AuthFailure() => Interlocked.Increment(ref _authFailures);

    public object Snapshot() => new
    {
        companionTurns = Interlocked.Read(ref _companionTurns),
        llmSuccess = Interlocked.Read(ref _llmSuccess),
        llmFallback = Interlocked.Read(ref _llmFallback),
        llmTimeout = Interlocked.Read(ref _llmTimeout),
        llmRejectedByBudget = Interlocked.Read(ref _llmRejectedByBudget),
        authFailures = Interlocked.Read(ref _authFailures),
        promptTokens = Interlocked.Read(ref _promptTokens),
        completionTokens = Interlocked.Read(ref _completionTokens),
        estimatedCostUsd = Interlocked.Read(ref _costMicroUsd) / 1_000_000d,
        lastLatencyMs = Interlocked.Read(ref _lastLatencyMs)
    };
}
