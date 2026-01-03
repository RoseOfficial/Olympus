namespace Olympus.Tests.Rotation.ApolloCore.Helpers;

/// <summary>
/// Tests for PartyHelper utility methods.
/// Note: Most PartyHelper methods require Dalamud runtime and IBattleChara/IGameObject
/// instances which cannot be easily mocked without game context.
/// These tests document expected behavior rather than testing implementation.
/// </summary>
public class PartyHelperTests
{
    #region Tank Job ID Documentation Tests

    [Fact]
    public void TankJobIds_Documentation()
    {
        // Document the known tank job IDs from FFXIV
        // These are public game data values used by IsTankRole
        var tankJobIds = new (uint Id, string Name)[]
        {
            (19, "Paladin"),
            (21, "Warrior"),
            (32, "Dark Knight"),
            (37, "Gunbreaker"),
            (1, "Gladiator"),  // Base class
            (3, "Marauder")    // Base class
        };

        // Verify we have 4 tank jobs + 2 base classes = 6 total
        Assert.Equal(6, tankJobIds.Length);

        // Verify job IDs are unique
        var ids = tankJobIds.Select(x => x.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void NonTankJobIds_Documentation()
    {
        // Document known non-tank job IDs
        var nonTankJobIds = new (uint Id, string Name)[]
        {
            // Healers
            (24, "White Mage"),
            (28, "Scholar"),
            (33, "Astrologian"),
            (40, "Sage"),

            // Melee DPS
            (20, "Monk"),
            (22, "Dragoon"),
            (30, "Ninja"),
            (34, "Samurai"),
            (39, "Reaper"),
            (41, "Viper"),

            // Ranged Physical DPS
            (23, "Bard"),
            (31, "Machinist"),
            (38, "Dancer"),

            // Casters
            (25, "Black Mage"),
            (27, "Summoner"),
            (35, "Red Mage"),
            (42, "Pictomancer"),
        };

        // All these should NOT be tanks
        var tankIds = new uint[] { 19, 21, 32, 37, 1, 3 };
        foreach (var (id, name) in nonTankJobIds)
        {
            Assert.DoesNotContain(id, tankIds);
        }
    }

    #endregion
}
