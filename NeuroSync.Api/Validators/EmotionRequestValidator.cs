using FluentValidation;
using NeuroSync.Core;

namespace NeuroSync.Api.Validators;

/// <summary>
/// Validator for emotion detection requests with input sanitization.
/// </summary>
public class EmotionRequestValidator : AbstractValidator<EmotionRequest>
{
    public EmotionRequestValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty()
            .WithMessage("Text is required")
            .MaximumLength(5000)
            .WithMessage("Text must not exceed 5000 characters")
            .Must(BeValidText)
            .WithMessage("Text contains invalid characters");
    }

    private bool BeValidText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // Basic sanitization: allow letters, numbers, spaces, punctuation, emojis
        // Reject potential script injections
        var invalidPatterns = new[]
        {
            "<script",
            "javascript:",
            "onerror=",
            "onload=",
            "eval(",
            "expression("
        };

        var textLower = text.ToLowerInvariant();
        return !invalidPatterns.Any(pattern => textLower.Contains(pattern));
    }
}
