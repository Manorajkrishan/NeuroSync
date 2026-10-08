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

    public LlmCompanionProvider(
        TemplateCompanionProvider fallback,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<LlmCompanionProvider> logger)
    {
        _fallback = fallback;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
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

        var apiKey = _configuration["Companion:Llm:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Companion:Llm:Enabled is true but ApiKey is missing — using template fallback.");
            return await _fallback.GenerateAsync(context, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var content = await CallChatCompletionsAsync(apiKey, context, cancellationToken)
                .ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("LLM returned empty content — using template fallback.");
                return await _fallback.GenerateAsync(context, cancellationToken).ConfigureAwait(false);
            }

            return new CompanionReply
            {
                Message = content.Trim(),
                ProviderId = ProviderId,
                ExposedEmotionToUser = false
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Timeouts / HTTP failures → template fallback.
            _logger.LogWarning(ex, "LLM companion call failed — using template fallback.");
            return await _fallback.GenerateAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<string?> CallChatCompletionsAsync(
        string apiKey,
        CompanionContext context,
        CancellationToken cancellationToken)
    {
        var baseUrl = (_configuration["Companion:Llm:BaseUrl"] ?? "https://api.openai.com/v1").TrimEnd('/');
        var model = _configuration["Companion:Llm:Model"] ?? "gpt-4o-mini";
        var timeoutSeconds = _configuration.GetValue("Companion:Llm:TimeoutSeconds", 12);

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 3, 60));

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var body = new
        {
            model,
            temperature = 0.7,
            max_tokens = 280,
            messages = new object[]
            {
                new { role = "system", content = BuildSystemPrompt(context) },
                new { role = "user", content = BuildUserPayload(context) }
            }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            return null;

        var message = choices[0].GetProperty("message");
        return message.TryGetProperty("content", out var content) ? content.GetString() : null;
    }

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
