using System.IO;
using Olympus.Timeline.Parser;

namespace Olympus.Tests.Timeline;

public class CactbotTimelineParserSyncTests
{
    private static string LoadEmbedded(string name)
    {
        var asm = typeof(CactbotTimelineParser).Assembly;
        using var stream = asm.GetManifestResourceStream($"Olympus.Timeline.Data.{name}");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    [Theory]
    [InlineData("p1s.txt")]   // old ACT format: sync /^.{14} Name 6DA1/
    [InlineData("r9s.txt")]   // new format: StartsUsing { id: "..." } + array IDs
    [InlineData("r12s.txt")]  // forcejump + array IDs
    [InlineData("fru.txt")]   // ultimate
    [InlineData("dsu.txt")]   // ultimate
    public void Parse_BundledTimeline_ProducesSyncIndex(string file)
    {
        var parser = new CactbotTimelineParser();
        var timeline = parser.Parse(LoadEmbedded(file), 1, "test", file);
        Assert.NotNull(timeline);
        Assert.True(timeline!.SyncIndex.Count >= 10,
            $"{file}: SyncIndex has only {timeline.SyncIndex.Count} entries");
    }

    [Fact]
    public void Parse_OldActFormat_ExtractsTrailingHexId()
    {
        var parser = new CactbotTimelineParser();
        var tl = parser.Parse(
            "10.0 \"Gaoler's Flail\" sync /^.{14} Erichthonios 6DA1/ window 5,5\n", 1, "t", "t");
        Assert.NotNull(tl);
        Assert.True(tl!.SyncIndex.ContainsKey(0x6DA1));
    }

    [Fact]
    public void Parse_OldActFormat_ZoneSealLine_DoesNotFakeSync()
    {
        var parser = new CactbotTimelineParser();
        var tl = parser.Parse(
            "0.0 \"--sync--\" sync /00:0839::Erichthonios will be sealed off/ window 1,0\n"
            + "10.0 \"Real\" sync /^.{14} Erichthonios 6DA1/\n", 1, "t", "t");
        Assert.NotNull(tl);
        Assert.Single(tl!.SyncIndex); // only 6DA1; the seal line has no trailing hex token
    }

    [Fact]
    public void Parse_NewFormat_StartsUsingWithoutSyncWrapper_Extracts()
    {
        var parser = new CactbotTimelineParser();
        var tl = parser.Parse(
            "5.4 \"--sync--\" StartsUsing { id: \"B384\", source: \"Vamp Fatale\" } window 10,10\n", 1, "t", "t");
        Assert.NotNull(tl);
        Assert.True(tl!.SyncIndex.ContainsKey(0xB384));
    }

    [Fact]
    public void Parse_NewFormat_ArrayIds_AllIndexed()
    {
        var parser = new CactbotTimelineParser();
        var tl = parser.Parse(
            "74.3 \"--sync--\" Ability { id: [\"B34E\", \"B34F\", \"B350\"], source: \"Vamp Fatale\" }\n", 1, "t", "t");
        Assert.NotNull(tl);
        Assert.True(tl!.SyncIndex.ContainsKey(0xB34E));
        Assert.True(tl.SyncIndex.ContainsKey(0xB34F));
        Assert.True(tl.SyncIndex.ContainsKey(0xB350));
    }

    [Fact]
    public void Parse_CommentedOutNetworkSync_IsNotIndexed()
    {
        var parser = new CactbotTimelineParser();
        var tl = parser.Parse(
            "556.2 \"Ascalon's Might 1\" #Ability { id: \"63C5\", source: \"King Thordan\" }\n"
            + "560.0 \"Real\" Ability { id: \"63C6\" }\n", 1, "t", "t");
        Assert.NotNull(tl);
        Assert.Single(tl!.SyncIndex);
        Assert.True(tl.SyncIndex.ContainsKey(0x63C6));
    }

    [Theory]
    [InlineData("67.5 \"--untargetable--\"\n", "--untargetable--")]
    [InlineData("120.1 \"--targetable--\"\n", "--targetable--")]
    public void Parse_TargetabilityMarkers_ClassifiedAsPhase(string line, string name)
    {
        var parser = new CactbotTimelineParser();
        var tl = parser.Parse(line, 1, "t", "t");
        Assert.NotNull(tl);
        var entry = Assert.Single(tl!.Entries);
        Assert.Equal(name, entry.Name);
        Assert.Equal(Olympus.Timeline.Models.TimelineEntryType.Phase, entry.EntryType);
    }

    [Fact]
    public void Parse_LabelBearingEntry_ClassifiedAsPhase()
    {
        var parser = new CactbotTimelineParser();
        var tl = parser.Parse(
            "200.0 \"Adds Appear\" Ability { id: \"B400\" } label \"p2-start\"\n", 1, "t", "t");
        Assert.NotNull(tl);
        var entry = Assert.Single(tl!.Entries);
        Assert.Equal(Olympus.Timeline.Models.TimelineEntryType.Phase, entry.EntryType);
        Assert.Equal("p2-start", entry.Label);
    }
}
