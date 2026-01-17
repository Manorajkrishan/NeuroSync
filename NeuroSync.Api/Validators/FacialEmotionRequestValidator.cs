using FluentValidation;
using NeuroSync.Core;

namespace NeuroSync.Api.Validators;

/// <summary>
/// Validator for facial emotion detection requests.
/// </summary>
public class FacialEmotionRequestValidator : AbstractValidator<FacialEmotionRequest>
{
    public FacialEmotionRequestValidator()
    {
        RuleFor(x => x.Emotion)
            .NotEmpty()
            .WithMessage("Emotion is required")
            .Must(BeValidEmotion)
            .WithMessage("Invalid emotion type");

        RuleFor(x => x.Confidence)
            .InclusiveBetween(0.0f, 1.0f)
            .WithMessage("Confidence must be between 0 and 1");
    }

    private bool BeValidEmotion(string? emotion)
    {
        if (string.IsNullOrWhiteSpace(emotion))
            return false;

        return Enum.TryParse<EmotionType>(emotion, true, out _);
    }
}
