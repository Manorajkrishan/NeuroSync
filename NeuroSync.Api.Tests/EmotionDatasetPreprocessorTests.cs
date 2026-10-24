using FluentAssertions;
using NeuroSync.ML;
using Xunit;

namespace NeuroSync.Api.Tests;

public class EmotionDatasetPreprocessorTests
{
    [Fact]
    public void Prepare_RemovesDuplicates_AndDropsConflictingLabels()
    {
        var rows = new List<EmotionData>
        {
            new() { Text = "I'm ecstatic!", Label = "excited" },
            new() { Text = " im ecstatic ", Label = "excited" },
            new() { Text = "I'm ecstatic.", Label = "happy" },
            new() { Text = "I feel sad", Label = "sad" },
            new() { Text = "I feel sad", Label = "sad" }
        };

        var result = EmotionDatasetPreprocessor.Prepare(rows);

        result.Examples.Should().ContainSingle(x => x.Label == "sad");
        result.Examples.Should().NotContain(x => EmotionDatasetPreprocessor.NormalizeText(x.Text) == "i m ecstatic");
        result.ConflictingTextsRemoved.Should().Be(1);
        result.DuplicateRowsRemoved.Should().Be(4);
    }

    [Fact]
    public void DeterministicSplit_HasNoNormalizedTextLeakage()
    {
        var rows = Enumerable.Range(0, 80)
            .Select(i => new EmotionData
            {
                Text = $"example {i}",
                Label = i % 2 == 0 ? "happy" : "sad"
            })
            .ToList();

        var prepared = EmotionDatasetPreprocessor.Prepare(rows);
        var first = EmotionDatasetPreprocessor.DeterministicStratifiedSplit(prepared.Examples);
        var second = EmotionDatasetPreprocessor.DeterministicStratifiedSplit(prepared.Examples);

        first.Train.Select(x => x.Text).Should().Equal(second.Train.Select(x => x.Text));
        first.Test.Select(x => x.Text).Should().Equal(second.Test.Select(x => x.Text));

        var trainKeys = first.Train.Select(x => EmotionDatasetPreprocessor.NormalizeText(x.Text)).ToHashSet();
        var testKeys = first.Test.Select(x => EmotionDatasetPreprocessor.NormalizeText(x.Text)).ToHashSet();

        trainKeys.Intersect(testKeys).Should().BeEmpty();
        first.Train.Select(x => x.Label).Distinct().Should().BeEquivalentTo(new[] { "happy", "sad" });
        first.Test.Select(x => x.Label).Distinct().Should().BeEquivalentTo(new[] { "happy", "sad" });
    }
}
