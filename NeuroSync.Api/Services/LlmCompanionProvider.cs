using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// OpenAI-compatible LLM companion. Disabled by default; falls back to <see cref="TemplateCompanionProvider"/>.
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

    public string Generate(
        EmotionResult emotion,
        CompanionInteractionMode mode,
        SafetyAssessment safety,
        UncertaintyLevel uncertainty,
        string? userMessage,
        string? displayName)
    {
        if (safety.BlockNormalCompanionFlow)
            return safety.Guidance;

        if (!IsEnabled)
            return _fallback.Generate(emotion, mode, safety, uncertainty, userMessage, displayName);

        var apiKey = _configuration["Companion:Llm:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Companion:Llm:Enabled is true but ApiKey is missing — using template fallback.");
            return _fallback.Generate(emotion, mode, safety, uncertainty, userMessage, displayName);
        }

        try
        {
            var reply = CallChatCompletions(apiKey, emotion, mode, safety, uncertainty, userMessage, displayName);
            if (string.IsNullOrWhiteSpace(reply))
            {
                _logger.LogWarning("LLM returned empty content — using template fallback.");
                return _fallback.Generate(emotion, mode, safety, uncertainty, userMessage, displayName);
            }

            return reply.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM companion call failed — using template fallback.");
            return _fallback.Generate(emotion, mode, safety, uncertainty, userMessage, displayName);
        }
    }

    private string? CallChatCompletions(
        string apiKey,
        EmotionResult emotion,
        CompanionInteractionMode mode,
        SafetyAssessment safety,
        UncertaintyLevel uncertainty,
        string? userMessage,
        string? displayName)
    {
        var baseUrl = (_configuration["Companion:Llm:BaseUrl"] ?? "https://api.openai.com/v1").TrimEnd('/');
        var model = _configuration["Companion:Llm:Model"] ?? "gpt-4o-mini";
        var timeoutSeconds = _configuration.GetValue("Companion:Llm:TimeoutSeconds", 12);

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 3, 60));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var system = BuildSystemPrompt();
        var user = BuildUserPayload(emotion, mode, safety, uncertainty, userMessage, displayName);

        var body = new
        {
            model,
            temperature = 0.7,
            max_tokens = 280,
            messages = new object[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

        using var response = client.Send(request);
        response.EnsureSuccessStatusCode();

        using var stream = response.Content.ReadAsStream();
        using var doc = JsonDocument.Parse(stream);
        if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            return null;

        var message = choices[0].GetProperty("message");
        return message.TryGetProperty("content", out var content) ? content.GetString() : null;
    }

    private static string BuildSystemPrompt() =>
        """
        You are NeuroSync, an emotion-aware wellbeing companion.
        Be conversational, warm, curious, and occasionally playful.
        Do not pretend to be human. Do not constantly talk about emotions.
        Respond to the user's actual request first.
        Do not state inferred emotions as facts. Do not expose model scores.
        Never diagnose mental-health conditions.
        Safety rules override personality. Keep replies concise (2–5 sentences).
        """;

    private static string BuildUserPayload(
        EmotionResult emotion,
        CompanionInteractionMode mode,
        SafetyAssessment safety,
        UncertaintyLevel uncertainty,
        string? userMessage,
        string? displayName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Structured context (do not quote scores to the user):");
        sb.AppendLine($"displayName: {(string.IsNullOrWhiteSpace(displayName) ? "(none)" : displayName.Trim())}");
        sb.AppendLine($"mode: {mode}");
        sb.AppendLine($"safety: {safety.Level}");
        sb.AppendLine($"uncertainty: {uncertainty}");
        sb.AppendLine($"understoodAs: {emotion.UnderstoodAs ?? "(none)"}");
        sb.AppendLine($"primaryEmotion: {emotion.Emotion}");
        sb.AppendLine($"confidence: {emotion.Confidence:F2}");
        sb.AppendLine();
        sb.AppendLine("User message:");
        sb.AppendLine(string.IsNullOrWhiteSpace(userMessage) ? "(no text)" : userMessage.Trim());
        return sb.ToString();
    }
}
