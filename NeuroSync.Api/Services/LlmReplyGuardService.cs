using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Post-generation guard for optional LLM replies.
/// Safety remains upstream; this guard rejects low-quality or policy-risky completions
/// and lets the provider fall back to the deterministic companion.
/// </summary>
public sealed class LlmReplyGuardService
{
    private readonly ResponseQualityService _quality;
    private readonly IConfiguration _configuration;

    public LlmReplyGuardService(ResponseQualityService quality, IConfiguration configuration)
    {
        _quality = quality;
        _configuration = configuration;
    }

    public bool Accept(CompanionContext context, string content, out ResponseQualityResult quality)
    {
        quality = _quality.Evaluate(context, new CompanionReply
        {
            Message = content,
            ProviderId = "llm-v1",
            ExposedEmotionToUser = false
        });

        var threshold = Math.Clamp(
            _configuration.GetValue("Companion:Llm:MinQualityScore", 0.65),
            0.4,
            0.95);

        var hardBlock = quality.Flags.Any(x =>
            x is "prompt_leak" or "diagnostic_claim" or "confidence_leak"
                or "safety_provider_mismatch" or "robotic_uncertainty");

        return !hardBlock && quality.IsAcceptable(threshold);
    }
}
