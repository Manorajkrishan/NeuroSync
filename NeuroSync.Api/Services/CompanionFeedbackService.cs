using Microsoft.EntityFrameworkCore;
using NeuroSync.Api.Data;
using NeuroSync.Core;

namespace NeuroSync.Api.Services;

public sealed record CompanionFeedbackSaveResult(
    string ResponseId,
    int Rating,
    bool ShareForLearning,
    DateTime UpdatedAtUtc);

/// <summary>
/// Stores explicit response feedback without raw chat text.
/// Only rows marked ShareForLearning may be used by future training/evaluation jobs.
/// </summary>
public sealed class CompanionFeedbackService
{
    private static readonly HashSet<string> AllowedReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        "helpful",
        "not_relevant",
        "robotic",
        "wrong_context",
        "too_much",
        "unsafe",
        "other"
    };

    private readonly NeuroSyncDbContext _db;

    public CompanionFeedbackService(NeuroSyncDbContext db)
    {
        _db = db;
    }

    public async Task<CompanionFeedbackSaveResult> SaveAsync(
        string userId,
        CompanionResponseMetadata metadata,
        int rating,
        string? reasonCode,
        bool shareForLearning,
        CancellationToken cancellationToken = default)
    {
        if (rating is not -1 and not 1)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be -1 or 1.");

        var reason = NormalizeReason(reasonCode);
        var now = DateTime.UtcNow;

        var row = await _db.CompanionFeedback
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.ResponseId == metadata.ResponseId,
                cancellationToken)
            .ConfigureAwait(false);

        if (row == null)
        {
            row = new CompanionFeedbackRecord
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ResponseId = metadata.ResponseId,
                CreatedAtUtc = now
            };
            _db.CompanionFeedback.Add(row);
        }

        row.Rating = rating;
        row.ReasonCode = reason;
        row.ProviderId = Trim(metadata.ProviderId, 32);
        row.Intent = Trim(metadata.Intent.ToString(), 32);
        row.Mode = Trim(metadata.Mode.ToString(), 32);
        row.Locale = Trim(metadata.Locale, 16);
        row.QualityScore = Math.Round(Math.Clamp(metadata.QualityScore, 0, 1), 3);
        row.ShareForLearning = shareForLearning;
        row.UpdatedAtUtc = now;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CompanionFeedbackSaveResult(
            row.ResponseId,
            row.Rating,
            row.ShareForLearning,
            row.UpdatedAtUtc);
    }

    public async Task<object> GetLearningSummaryAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.CompanionFeedback.AsNoTracking()
            .Where(x => x.ShareForLearning)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var total = rows.Count;
        var positive = rows.Count(x => x.Rating > 0);
        var negative = rows.Count(x => x.Rating < 0);

        return new
        {
            total,
            positive,
            negative,
            positiveRate = total == 0 ? 0d : Math.Round((double)positive / total, 3),
            byProvider = rows
                .GroupBy(x => x.ProviderId)
                .OrderBy(x => x.Key)
                .ToDictionary(
                    x => x.Key,
                    x => new
                    {
                        total = x.Count(),
                        positive = x.Count(r => r.Rating > 0),
                        negative = x.Count(r => r.Rating < 0),
                        meanQualityScore = x.Count() == 0 ? 0d : Math.Round(x.Average(r => r.QualityScore), 3)
                    })
        };
    }

    private static string? NormalizeReason(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().ToLowerInvariant().Replace(' ', '_');
        return AllowedReasons.Contains(normalized) ? normalized : "other";
    }

    private static string Trim(string? value, int max)
    {
        var text = (value ?? "").Trim();
        return text.Length <= max ? text : text[..max];
    }
}
