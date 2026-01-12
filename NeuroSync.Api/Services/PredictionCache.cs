using System.Collections.Concurrent;
using NeuroSync.Core;

namespace NeuroSync.Api.Services;

/// <summary>
/// Caches emotion predictions for faster responses.
/// Speeds up decision-making by avoiding redundant model predictions.
/// </summary>
public class PredictionCache
{
    private readonly ConcurrentDictionary<string, CachedPrediction> _cache = new();
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromHours(24);
    private const int MaxCacheSize = 1000;

    public class CachedPrediction
    {
        public EmotionResult Result { get; set; } = null!;
        public DateTime CachedAt { get; set; }
    }

    /// <summary>
    /// Gets a cached prediction if available and not expired.
    /// </summary>
    public EmotionResult? GetCached(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var normalizedText = NormalizeText(text);
        
        if (_cache.TryGetValue(normalizedText, out var cached) && !IsExpired(cached))
        {
            return cached.Result;
        }

        // Remove expired entry
        if (cached != null)
        {
            _cache.TryRemove(normalizedText, out _);
        }

        return null;
    }

    /// <summary>
    /// Caches a prediction result.
    /// </summary>
    public void Cache(string text, EmotionResult result)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var normalizedText = NormalizeText(text);

        // Limit cache size
        if (_cache.Count >= MaxCacheSize)
        {
            // Remove oldest entries
            var oldest = _cache.OrderBy(kvp => kvp.Value.CachedAt).Take(100);
            foreach (var item in oldest)
            {
                _cache.TryRemove(item.Key, out _);
            }
        }

        _cache[normalizedText] = new CachedPrediction
        {
            Result = result,
            CachedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    public (int Count, int Expired) GetStats()
    {
        var expired = _cache.Values.Count(c => IsExpired(c));
        return (_cache.Count, expired);
    }

    private bool IsExpired(CachedPrediction cached)
    {
        return (DateTime.UtcNow - cached.CachedAt) > _cacheExpiry;
    }

    private string NormalizeText(string text)
    {
        return text.Trim().ToLowerInvariant();
    }
}

