using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// OpenAI-compatible LLM companion. Disabled by default; falls back to <see cref="TemplateCompanionProvider"/>.
/// Does not make safety decisions or execute IoT — SafetyGate / DecisionEngine remain authoritative.
/// </summary>
public sealed class LlmCompanionProvider : ICompanionProvider
{
    private readonly TemplateCompanionProvider _fallback;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LlmCompanionProvider> _logger;
    private readonly LlmUsageGovernor? _governor;

    public LlmCompanionProvider(
        TemplateCompanionProvider fallback,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<LlmCompanionProvider> logger,
        LlmUsageGovernor? governor = null)
    {
        _fallback = fallback;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _governor = governor;
    }

    public string ProviderId => IsEnabled ? "llm-v1" : _fallback.ProviderId;

    private bool IsEnabled =>
        _configuration.GetValue("Companion:Llm:Enabled", false);

    public async Task<CompanionReply> GenerateAsync(
        CompanionContext context,
        CancellationToken cancellationToken = default)
    {
        // Safety is decided upstream; never let the LLM override crisis guidance.
        if (context.Safety.BlockNormalCompanionFlow)
        {
            return new CompanionReply
            {
                Message = context.Safety.Guidance,
                ProviderId = "safety-protocol",
                ExposedEmotionToUser = false
            };
        }

        if (!IsEnabled)
            return await _fallback.GenerateAsync(context, cancellationToken).ConfigureAwait(false);

        // Server configuration only. Never accept a key from the browser or localStorage.
        var apiKey = _configuration["Companion:Llm:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _governor?.RecordFallback("missing api key");
            _logger.LogWarning("Companion:Llm:Enabled is true but ApiKey is missing — using template fallback.");
            return await _fallback.GenerateAsync(context, cancellationToken).ConfigureAwait(false);
        }

        var estimatedPrompt = LlmUsageGovernor.EstimateTokens(BuildSystemPrompt(context) + BuildUserPayload(context));
        if (_governor != null && !_governor.TryReserve(context.UserId, estimatedPrompt, out var budgetReason))
        {
            _governor.RecordFallback(budgetReason);
            return await _fallback.GenerateAsync(context, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var completion = await CallChatCompletionsAsync(apiKey, context, cancellationToken)
                .ConfigureAwait(false);
            if (completion == null || string.IsNullOrWhiteSpace(completion.Content))
            {
                _governor?.RecordFallback("empty completion");
                _logger.LogWarning("LLM returned empty content — using template fallback.");
                return await _fallback.GenerateAsync(context, cancellationToken).ConfigureAwait(false);
            }

            _governor?.Record(context.UserId, completion.PromptTokens, completion.CompletionTokens, completion.LatencyMs);
            return new CompanionReply
            {
                Message = completion.Content.Trim(),
                ProviderId = ProviderId,
                ExposedEmotionToUser = false
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _governor?.RecordTimeout();
            _governor?.RecordFallback("timeout");
            return await _fallback.GenerateAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _governor?.RecordFallback("provider error");
            _logger.LogWarning(ex, "LLM companion call failed — using template fallback.");
            return await _fallback.GenerateAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<LlmCompletion?> CallChatCompletionsAsync(
        string apiKey,
        CompanionContext context,
        CancellationToken cancellationToken)
    {
        var baseUrl = (_configuration["Companion:Llm:BaseUrl"] ?? "https://api.openai.com/v1").TrimEnd('/');
        var model = _configuration["Companion:Llm:Model"] ?? "gpt-4o-mini";
        var timeoutSeconds = Math.Clamp(_configuration.GetValue("Companion:Llm:TimeoutSeconds", 12), 3, 60);
        var maxTokens = Math.Clamp(_configuration.GetValue("Companion:Llm:MaxTokens", 280), 64, 800);
        var retries = Math.Clamp(_configuration.GetValue("Companion:Llm:RetryCount", 2), 0, 4);
        var attempts = retries + 1;

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var body = new
            {
                model,
                temperature = 0.7,
                max_tokens = maxTokens,
                messages = new object[]
                {
                    new { role = "system", content = BuildSystemPrompt(context) },
                    new { role = "user", content = BuildUserPayload(context) }
                }
            };
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var started = DateTime.UtcNow;
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var status = (int)response.StatusCode;
            if (status == 429 || status >= 500)
            {
                if (attempt == attempts)
                    response.EnsureSuccessStatusCode();
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken).ConfigureAwait(false);
                continue;
            }

            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                return null;
            var message = choices[0].GetProperty("message");
            var content = message.TryGetProperty("content", out var contentNode) ? contentNode.GetString() : null;
            var promptTokens = 0;
            var completionTokens = 0;
            if (doc.RootElement.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var prompt))
                    promptTokens = prompt.GetInt32();
                if (usage.TryGetProperty("completion_tokens", out var completion))
                    completionTokens = completion.GetInt32();
            }
            if (promptTokens == 0)
                promptTokens = LlmUsageGovernor.EstimateTokens(BuildUserPayload(context));
            if (completionTokens == 0)
                completionTokens = LlmUsageGovernor.EstimateTokens(content);
            var latency = (long)(DateTime.UtcNow - started).TotalMilliseconds;
            return new LlmCompletion(content, promptTokens, completionTokens, latency);
        }

        return null;
    }

    private sealed record LlmCompletion(string? Content, int PromptTokens, int CompletionTokens, long LatencyMs);

    private static string BuildSystemPrompt(CompanionContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            """
            You are NeuroSync, an emotion-aware wellbeing companion.
            Be conversational, warm, curious, and occasionally playful.
            Do not pretend to be human. Do not constantly talk about emotions.
            Respond to the user's actual request first.
            Do not state inferred emotions as facts. Do not expose model scores.
            Never diagnose mental-health conditions.
            You do NOT decide safety policy and you do NOT execute IoT or device actions.
            Safety rules override personality. Keep replies concise (2–5 sentences).
            """);
        if (!string.IsNullOrWhiteSpace(context.PolicyGuidance))
            sb.AppendLine(context.PolicyGuidance.Trim());
        return sb.ToString();
    }

    private static string BuildUserPayload(CompanionContext context)
    {
        var emotion = context.Emotion;
        var sb = new StringBuilder();
        sb.AppendLine("Structured context (do not quote scores to the user):");
        sb.AppendLine($"displayName: {(string.IsNullOrWhiteSpace(context.DisplayName) ? "(none)" : context.DisplayName.Trim())}");
        sb.AppendLine($"intent: {context.Intent}");
        sb.AppendLine($"mode: {context.Mode}");
        sb.AppendLine($"safety: {context.Safety.Level}");
        sb.AppendLine($"uncertainty: {context.Uncertainty}");
        sb.AppendLine($"understoodAs: {emotion?.UnderstoodAs ?? "(none)"}");
        sb.AppendLine($"primaryEmotion: {emotion?.Emotion.ToString() ?? "(none)"}");
        if (emotion != null)
            sb.AppendLine($"confidence: {emotion.Confidence:F2}");

        if (context.EmotionSignals.Count > 0)
        {
            sb.AppendLine("emotionSignals:");
            foreach (var kv in context.EmotionSignals.Take(8))
                sb.AppendLine($"  {kv.Key}: {kv.Value:F2}");
        }

        if (context.RecentTurns.Count > 0)
        {
            sb.AppendLine("recentTurns:");
            foreach (var turn in context.RecentTurns.Take(6))
                sb.AppendLine($"  [{turn.Role}] {turn.Text}");
        }

        if (!string.IsNullOrWhiteSpace(context.RelevantMemory))
        {
            sb.AppendLine("relevantMemory (consent-approved):");
            sb.AppendLine(context.RelevantMemory.Trim());
        }

        sb.AppendLine();
        sb.AppendLine("User message:");
        sb.AppendLine(string.IsNullOrWhiteSpace(context.CurrentMessage) ? "(no text)" : context.CurrentMessage.Trim());
        return sb.ToString();
    }
}
