using System.IO;
using Olympus.Timeline;
using Olympus.Timeline.Models;
using Olympus.Timeline.Parser;
using Xunit;

namespace Olympus.Tests.Timeline;

/// <summary>
/// Tests for TimelineService.FindSecondsUntilNextUntargetablePhase -- the internal static
/// that backs ITimelineService.SecondsUntilNextUntargetablePhase().
/// Both the real-bundled-file path (mirrors CactbotTimelineParserSyncTests) and synthetic
/// unit paths (both marker strings, Phase gate, hidden gate, empty case) are covered.
/// </summary>
public class TimelineSecondsUntilUntargetableTests
{
    // ----- helpers -----

    private static string LoadEmbedded(string name)
    {
        var asm = typeof(CactbotTimelineParser).Assembly;
        using var stream = asm.GetManifestResourceStream($"Olympus.Timeline.Data.{name}");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    private static FightTimeline ParseTimeline(string name)
    {
        var parser = new CactbotTimelineParser();
        var timeline = parser.Parse(LoadEmbedded(name), 1, "test", name);
        Assert.NotNull(timeline);
        return timeline!;
    }

    private static FightTimeline Make(params TimelineEntry[] entries)
        => new FightTimeline(0, "test", "test", entries);

    private static TimelineEntry PhaseEntry(float t, string name, bool hidden = false)
        => new TimelineEntry(t, name, TimelineEntryType.Phase, isHidden: hidden);

    private static TimelineEntry AbilityEntry(float t, string name)
        => new TimelineEntry(t, name, TimelineEntryType.Ability);

    // ----- real bundled-file test -----

    [Fact]
    public void FindSecondsUntilNextUntargetablePhase_DsuAtT0_ReturnsSanePositiveValue()
    {
        // dsu.txt: first "--untargetable--" is at 38.4s.
        // At currentTime=0, result should be ~38.4s (a sane positive value > 0 and < 50).
        var timeline = ParseTimeline("dsu.txt");
        var result = TimelineService.FindSecondsUntilNextUntargetablePhase(timeline, 0f);
        Assert.NotNull(result);
        Assert.True(result!.Value > 0f,
            $"Expected a positive seconds-until value from t=0, got {result.Value}");
        Assert.True(result.Value < 50f,
            $"Expected value < 50s from t=0, got {result.Value} (first untargetable is at 38.4s)");
    }

    // ----- synthetic: correct marker matches -----

    [Fact]
    public void FindSecondsUntilNextUntargetablePhase_UntargetablePhaseEntry_ReturnsSecondsUntil()
    {
        // The main happy path: a Phase entry named "--untargetable--" in the future.
        var timeline = Make(PhaseEntry(50f, "--untargetable--"));
        var result = TimelineService.FindSecondsUntilNextUntargetablePhase(timeline, 30f);
        Assert.NotNull(result);
        Assert.Equal(20f, result!.Value, 3); // 50 - 30 = 20
    }

    [Fact]
    public void FindSecondsUntilNextUntargetablePhase_TargetablePhaseEntry_ReturnsNull()
    {
        // Discrimination: "--targetable--" is also a Phase, but must NOT match.
        // "targetable" appears as a substring of "untargetable", so the predicate must use
        // Contains("untargetable") -- not Contains("targetable") -- to distinguish them.
        var timeline = Make(PhaseEntry(50f, "--targetable--"));
        var result = TimelineService.FindSecondsUntilNextUntargetablePhase(timeline, 30f);
        Assert.Null(result);
    }

    [Fact]
    public void FindSecondsUntilNextUntargetablePhase_BothMarkers_ReturnsOnlyUntargetable()
    {
        // Both markers present: targetable at 40s, untargetable at 60s.
        // Should return 60 - 30 = 30, skipping the targetable entry at 40s.
        var timeline = Make(
            PhaseEntry(40f, "--targetable--"),
            PhaseEntry(60f, "--untargetable--"));
        var result = TimelineService.FindSecondsUntilNextUntargetablePhase(timeline, 30f);
        Assert.NotNull(result);
        Assert.Equal(30f, result!.Value, 3); // 60 - 30 = 30
    }

    // ----- synthetic: case-insensitive -----

    [Fact]
    public void FindSecondsUntilNextUntargetablePhase_UppercaseName_Matches()
    {
        // FRU has "--Usurper untargetable--" and "--Oracle untargetable--"; capitalisation must work.
        var timeline = Make(PhaseEntry(50f, "--UNTARGETABLE--"));
        var result = TimelineService.FindSecondsUntilNextUntargetablePhase(timeline, 30f);
        Assert.NotNull(result);
    }

    [Fact]
    public void FindSecondsUntilNextUntargetablePhase_MixedCaseVariant_Matches()
    {
        // Matches FRU's "--Usurper untargetable--" shape.
        var timeline = Make(PhaseEntry(100f, "--Boss untargetable--"));
        var result = TimelineService.FindSecondsUntilNextUntargetablePhase(timeline, 80f);
        Assert.NotNull(result);
        Assert.Equal(20f, result!.Value, 3);
    }

    // ----- synthetic: past / none -----

    [Fact]
    public void FindSecondsUntilNextUntargetablePhase_PastEntry_ReturnsNull()
    {
        // currentTime is past the only untargetable entry -- should not look backwards.
        var timeline = Make(PhaseEntry(20f, "--untargetable--"));
        var result = TimelineService.FindSecondsUntilNextUntargetablePhase(timeline, 25f);
        Assert.Null(result);
    }

    [Fact]
    public void FindSecondsUntilNextUntargetablePhase_EmptyTimeline_ReturnsNull()
    {
        var result = TimelineService.FindSecondsUntilNextUntargetablePhase(Make(), 0f);
        Assert.Null(result);
    }

    // ----- synthetic: non-Phase entry with untargetable name -----

    [Fact]
    public void FindSecondsUntilNextUntargetablePhase_NonPhaseEntryNamedUntargetable_ReturnsNull()
    {
        // An Ability entry named "--untargetable--" must NOT match; only Phase entries count.
        var timeline = Make(AbilityEntry(50f, "--untargetable--"));
        var result = TimelineService.FindSecondsUntilNextUntargetablePhase(timeline, 30f);
        Assert.Null(result);
    }

    // ----- synthetic: hidden entry is skipped -----

    [Fact]
    public void FindSecondsUntilNextUntargetablePhase_HiddenEntry_IsSkipped()
    {
        // Mirrors GetNextMechanic's !entry.IsHidden guard.
        var timeline = Make(PhaseEntry(50f, "--untargetable--", hidden: true));
        var result = TimelineService.FindSecondsUntilNextUntargetablePhase(timeline, 30f);
        Assert.Null(result);
    }

    [Fact]
    public void FindSecondsUntilNextUntargetablePhase_HiddenThenVisible_ReturnsVisible()
    {
        // Hidden untargetable at 50s, visible untargetable at 80s.
        // Should skip the hidden one and return 80-30=50.
        var timeline = Make(
            PhaseEntry(50f, "--untargetable--", hidden: true),
            PhaseEntry(80f, "--untargetable--", hidden: false));
        var result = TimelineService.FindSecondsUntilNextUntargetablePhase(timeline, 30f);
        Assert.NotNull(result);
        Assert.Equal(50f, result!.Value, 3); // 80 - 30 = 50
    }
}
