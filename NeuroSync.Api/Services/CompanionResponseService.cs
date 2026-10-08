using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Natural companion replies via <see cref="ICompanionProvider"/>. Emotion signals stay internal.
/// </summary>
public interface ICompanionResponseService
{
    Task<CompanionReply> GenerateAsync(
        CompanionContext context,
        ResponsePolicyService.PolicyResult policy,
        CancellationToken cancellationToken = default);
}

public class CompanionResponseService : ICompanionResponseService
{
    private readonly ResponsePolicyService _policy;
    private readonly ICompanionProvider _provider;

    public CompanionResponseService(ResponsePolicyService policy, ICompanionProvider provider)
    {
        _policy = policy;
        _provider = provider;
    }

    public async Task<CompanionReply> GenerateAsync(
        CompanionContext context,
        ResponsePolicyService.PolicyResult policy,
        CancellationToken cancellationToken = default)
    {
        if (context.Safety.BlockNormalCompanionFlow)
        {
            return new CompanionReply
            {
                Message = context.Safety.Guidance,
                ProviderId = "safety-protocol",
                ExposedEmotionToUser = false
            };
        }

        context.PolicyGuidance ??= policy.SystemGuidance;

        var reply = await _provider.GenerateAsync(context, cancellationToken).ConfigureAwait(false);
        reply.Message = _policy.Sanitize(reply.Message, policy);
        reply.ExposedEmotionToUser = false;
        return reply;
    }
}
