using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NeuroSync.Api.Auth;
using NeuroSync.Api.Data;
using NeuroSync.Core;

namespace NeuroSync.Api.Services;

public interface IAccountDataService
{
    Task EnsureDefaultsAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task SaveConsentAsync(string userId, EthicalAIConsent consent, CancellationToken cancellationToken = default);
    Task<EthicalAIConsent?> GetConsentAsync(string userId, CancellationToken cancellationToken = default);
    Task RememberTurnAsync(
        string userId,
        string userMessage,
        string assistantMessage,
        EmotionalBaselineSnapshot? baseline,
        bool persistConversation,
        bool persistBaseline,
        CancellationToken cancellationToken = default);
    Task<UserPreferenceRecord?> GetPreferencesAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdatePreferencesAsync(string userId, string? displayName, string? locale, CancellationToken cancellationToken = default);
    Task DeleteOwnedDataAsync(string userId, CancellationToken cancellationToken = default);
    Task<AccountExport> ExportAsync(string userId, CancellationToken cancellationToken = default);
    Task SweepExpiredAsync(CancellationToken cancellationToken = default);
}

public sealed record AccountExport(
    DateTime ExportedAtUtc,
    string UserId,
    string? Email,
    string? DisplayName,
    EthicalAIConsent? Consent,
    UserPreferenceRecord? Preferences,
    IReadOnlyList<ConversationHistoryEntry> Conversations,
    IReadOnlyList<PersistentMemoryItem> Memories,
    UserBaselineRecord? Baseline,
    string RetentionNote);

public sealed class AccountDataService : IAccountDataService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly NeuroSyncDbContext _db;
    private readonly IConfiguration _config;

    public AccountDataService(NeuroSyncDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public int RetentionDays => Math.Clamp(_config.GetValue("Database:RetentionDays", 365), 7, 3650);

    public async Task EnsureDefaultsAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        if (!await _db.UserPrivacyProfiles.AnyAsync(p => p.UserId == user.Id, cancellationToken).ConfigureAwait(false))
        {
            _db.UserPrivacyProfiles.Add(new UserPrivacyProfile
            {
                UserId = user.Id,
                MemoryEnabled = false,
                EmotionHistoryEnabled = false,
                FaceAnalysisEnabled = false,
                IoTEnabled = false,
                ShareLearningData = false,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        if (!await _db.UserPreferences.AnyAsync(p => p.UserId == user.Id, cancellationToken).ConfigureAwait(false))
        {
            _db.UserPreferences.Add(new UserPreferenceRecord
            {
                UserId = user.Id,
                DisplayName = user.DisplayName,
                Locale = "en",
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveConsentAsync(string userId, EthicalAIConsent consent, CancellationToken cancellationToken = default)
    {
        var profile = await _db.UserPrivacyProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        if (profile == null)
            return;

        profile.MemoryEnabled = consent.MemoryConsent;
        profile.EmotionHistoryEnabled = consent.EmotionHistoryConsent;
        profile.FaceAnalysisEnabled = consent.FaceAnalysisConsent;
        profile.IoTEnabled = consent.IoTConsent;
        profile.ShareLearningData = consent.DataSharingConsent;
        profile.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<EthicalAIConsent?> GetConsentAsync(string userId, CancellationToken cancellationToken = default)
    {
        var profile = await _db.UserPrivacyProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        if (profile == null)
            return null;

        return new EthicalAIConsent
        {
            UserId = userId,
            EmotionSensingConsent = true,
            MemoryConsent = profile.MemoryEnabled,
            EmotionHistoryConsent = profile.EmotionHistoryEnabled,
            FaceAnalysisConsent = profile.FaceAnalysisEnabled,
            IoTConsent = profile.IoTEnabled,
            DataSharingConsent = profile.ShareLearningData,
            DataStorageConsent = profile.MemoryEnabled || profile.EmotionHistoryEnabled,
            AnonymizationEnabled = true,
            PrivacyLevel = 10,
            ConsentTimestamp = profile.UpdatedAtUtc,
            LastUpdated = profile.UpdatedAtUtc
        };
    }

    public async Task RememberTurnAsync(
        string userId,
        string userMessage,
        string assistantMessage,
        EmotionalBaselineSnapshot? baseline,
        bool persistConversation,
        bool persistBaseline,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false))
            return;

        if (persistConversation)
        {
            var userText = Trim(userMessage, 2000);
            var assistantText = Trim(assistantMessage, 4000);
            if (userText.Length > 0 || assistantText.Length > 0)
            {
                _db.ConversationHistories.Add(new ConversationHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    UserMessage = userText,
                    AssistantMessage = assistantText,
                    CreatedAtUtc = DateTime.UtcNow
                });
                if (userText.Length > 0)
                {
                    _db.PersistentMemories.Add(new PersistentMemoryItem
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Content = Trim(userText, 500),
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }
        }

        if (persistBaseline && baseline != null)
        {
            var json = JsonSerializer.Serialize(baseline, JsonOptions);
            var row = await _db.UserBaselines.FirstOrDefaultAsync(b => b.UserId == userId, cancellationToken)
                .ConfigureAwait(false);
            if (row == null)
            {
                _db.UserBaselines.Add(new UserBaselineRecord
                {
                    UserId = userId,
                    PayloadJson = json,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                row.PayloadJson = json;
                row.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await TrimCapsAsync(userId, cancellationToken).ConfigureAwait(false);
    }

    public Task<UserPreferenceRecord?> GetPreferencesAsync(string userId, CancellationToken cancellationToken = default) =>
        _db.UserPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public async Task UpdatePreferencesAsync(
        string userId,
        string? displayName,
        string? locale,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
        if (row == null)
            return;
        if (displayName != null)
            row.DisplayName = displayName;
        if (!string.IsNullOrWhiteSpace(locale))
            row.Locale = locale;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteOwnedDataAsync(string userId, CancellationToken cancellationToken = default)
    {
        _db.PersistentMemories.RemoveRange(_db.PersistentMemories.Where(m => m.UserId == userId));
        _db.ConversationHistories.RemoveRange(_db.ConversationHistories.Where(m => m.UserId == userId));
        _db.UserBaselines.RemoveRange(_db.UserBaselines.Where(m => m.UserId == userId));
        _db.UserPreferences.RemoveRange(_db.UserPreferences.Where(m => m.UserId == userId));
        _db.UserPrivacyProfiles.RemoveRange(_db.UserPrivacyProfiles.Where(m => m.UserId == userId));
        _db.RefreshTokens.RemoveRange(_db.RefreshTokens.Where(m => m.UserId == userId));
        _db.ConversationEntries.RemoveRange(_db.ConversationEntries.Where(m => m.UserId == userId));
        _db.ConversationSessions.RemoveRange(_db.ConversationSessions.Where(m => m.UserId == userId));
        _db.EmotionPatterns.RemoveRange(_db.EmotionPatterns.Where(m => m.UserId == userId));
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AccountExport> ExportAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            .ConfigureAwait(false);
        var conversations = await _db.ConversationHistories.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var memories = await _db.PersistentMemories.AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return new AccountExport(
            DateTime.UtcNow,
            userId,
            user?.Email,
            user?.DisplayName,
            await GetConsentAsync(userId, cancellationToken).ConfigureAwait(false),
            await GetPreferencesAsync(userId, cancellationToken).ConfigureAwait(false),
            conversations,
            memories,
            await _db.UserBaselines.AsNoTracking().FirstOrDefaultAsync(b => b.UserId == userId, cancellationToken).ConfigureAwait(false),
            $"Session context expires after 45 minutes and is not this export. Stored account data is kept for {RetentionDays} days or until you delete the account. NeuroSync is not a medical record.");
    }

    public async Task SweepExpiredAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);
        var oldConversations = await _db.ConversationHistories.Where(c => c.CreatedAtUtc < cutoff)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var oldMemories = await _db.PersistentMemories.Where(c => c.CreatedAtUtc < cutoff)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var oldLegacy = await _db.ConversationEntries.Where(c => c.Timestamp < cutoff)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (oldConversations.Count == 0 && oldMemories.Count == 0 && oldLegacy.Count == 0)
            return;
        _db.ConversationHistories.RemoveRange(oldConversations);
        _db.PersistentMemories.RemoveRange(oldMemories);
        _db.ConversationEntries.RemoveRange(oldLegacy);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task TrimCapsAsync(string userId, CancellationToken cancellationToken)
    {
        const int conversationCap = 200;
        const int memoryCap = 50;
        var conversationIds = await _db.ConversationHistories
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip(conversationCap)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (conversationIds.Count > 0)
        {
            var stale = await _db.ConversationHistories.Where(c => conversationIds.Contains(c.Id))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            _db.ConversationHistories.RemoveRange(stale);
        }

        var memoryIds = await _db.PersistentMemories
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip(memoryCap)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (memoryIds.Count > 0)
        {
            var stale = await _db.PersistentMemories.Where(c => memoryIds.Contains(c.Id))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            _db.PersistentMemories.RemoveRange(stale);
        }

        if (conversationIds.Count > 0 || memoryIds.Count > 0)
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string Trim(string? value, int max)
    {
        var text = (value ?? "").Trim();
        return text.Length <= max ? text : text[..max];
    }
}
