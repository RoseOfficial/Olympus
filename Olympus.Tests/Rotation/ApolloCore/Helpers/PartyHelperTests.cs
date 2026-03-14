using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using Moq;
using Olympus.Tests.Mocks;

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

    #region CountPartyMembersNeedingAoEHeal Tests

    // Helper: create a mock IBattleChara positioned at origin (within Medica 20y radius)
    private static Mock<IBattleChara> CreateMemberAtFullHp(uint entityId)
    {
        var mock = MockBuilders.CreateMockBattleChara(
            entityId: entityId,
            currentHp: 100000,
            maxHp: 100000,
            isDead: false,
            position: Vector3.Zero);
        mock.Setup(x => x.Name).Returns(new SeString());
        return mock;
    }

    private static Mock<IBattleChara> CreateMemberAtHpPercent(uint entityId, float hpPercent, bool isDead = false)
    {
        uint maxHp = 100000;
        uint currentHp = (uint)(maxHp * hpPercent);
        var mock = MockBuilders.CreateMockBattleChara(
            entityId: entityId,
            currentHp: currentHp,
            maxHp: maxHp,
            isDead: isDead,
            position: Vector3.Zero);
        mock.Setup(x => x.Name).Returns(new SeString());
        return mock;
    }

    private static Mock<IBattleChara> CreateMemberFarAway(uint entityId, float hpPercent)
    {
        uint maxHp = 100000;
        uint currentHp = (uint)(maxHp * hpPercent);
        // 100y away — outside Medica's 20y radius
        var mock = MockBuilders.CreateMockBattleChara(
            entityId: entityId,
            currentHp: currentHp,
            maxHp: maxHp,
            isDead: false,
            position: new Vector3(0f, 0f, 100f));
        mock.Setup(x => x.Name).Returns(new SeString());
        return mock;
    }

    private static Mock<IPlayerCharacter> CreatePlayer()
    {
        return MockBuilders.CreateMockPlayerCharacter(
            level: 100,
            currentHp: 50000,
            maxHp: 50000,
            position: Vector3.Zero);
    }

    [Fact]
    public void CountPartyMembersNeedingAoEHeal_AllAtFullHp_ReturnsZero()
    {
        // Arrange — all members at 100% HP (above 85% threshold)
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.AoEHealHpThreshold = 0.85f;

        var members = new List<IBattleChara>
        {
            CreateMemberAtFullHp(1u).Object,
            CreateMemberAtFullHp(2u).Object,
            CreateMemberAtFullHp(3u).Object,
            CreateMemberAtFullHp(4u).Object,
        };

        var helper = new TestableApolloPartyHelper(members, config);
        var player = CreatePlayer().Object;

        // Act
        var (count, anyHaveRegen, allTargets, averageMissingHp) =
            helper.CountPartyMembersNeedingAoEHeal(player, healAmount: 0);

        // Assert
        Assert.Equal(0, count);
        Assert.False(anyHaveRegen);
        Assert.Equal(0, averageMissingHp);
    }

    [Fact]
    public void CountPartyMembersNeedingAoEHeal_SomeInjuredInFourMan_ReturnsCorrectCount()
    {
        // Arrange — 4-man party, 2 members below 85% threshold
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.AoEHealHpThreshold = 0.85f;

        var members = new List<IBattleChara>
        {
            CreateMemberAtHpPercent(1u, 1.00f).Object,  // full HP — not counted
            CreateMemberAtHpPercent(2u, 0.90f).Object,  // 90% — not counted
            CreateMemberAtHpPercent(3u, 0.80f).Object,  // 80% — counted
            CreateMemberAtHpPercent(4u, 0.70f).Object,  // 70% — counted
        };

        var helper = new TestableApolloPartyHelper(members, config);
        var player = CreatePlayer().Object;

        // Act
        var (count, _, _, _) = helper.CountPartyMembersNeedingAoEHeal(player, healAmount: 0);

        // Assert
        Assert.Equal(2, count);
    }

    /// <summary>
    /// Regression test for the v4.10.7 AoE heal bug.
    /// An 8-man raid at level 100 with 3 members below 85% HP must return count = 3.
    /// The original bug caused the count to always return 0 in 8-man parties.
    /// </summary>
    [Fact]
    public void CountPartyMembersNeedingAoEHeal_EightManRaid_ThreeMembersBelow85Pct_ReturnsThree()
    {
        // Arrange — 8-man raid, 3 members below the 85% AoE threshold
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.AoEHealHpThreshold = 0.85f;

        var members = new List<IBattleChara>
        {
            CreateMemberAtHpPercent(1u, 1.00f).Object,  // full HP
            CreateMemberAtHpPercent(2u, 0.95f).Object,  // 95%
            CreateMemberAtHpPercent(3u, 0.90f).Object,  // 90%
            CreateMemberAtHpPercent(4u, 0.88f).Object,  // 88%
            CreateMemberAtHpPercent(5u, 0.80f).Object,  // 80% — counted (below 85%)
            CreateMemberAtHpPercent(6u, 0.75f).Object,  // 75% — counted
            CreateMemberAtHpPercent(7u, 0.72f).Object,  // 72% — counted
            CreateMemberAtHpPercent(8u, 1.00f).Object,  // full HP
        };

        var helper = new TestableApolloPartyHelper(members, config);
        var player = CreatePlayer().Object;

        // Act
        var (count, _, _, _) = helper.CountPartyMembersNeedingAoEHeal(player, healAmount: 0);

        // Assert — must be exactly 3, not 0
        Assert.Equal(3, count);
    }

    [Fact]
    public void CountPartyMembersNeedingAoEHeal_DeadMembersExcluded()
    {
        // Arrange — 4 members, 1 dead and injured, 1 alive and injured
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.AoEHealHpThreshold = 0.85f;

        var members = new List<IBattleChara>
        {
            CreateMemberAtHpPercent(1u, 1.00f).Object,           // full HP, alive
            CreateMemberAtHpPercent(2u, 0.70f).Object,           // 70%, alive — counted
            CreateMemberAtHpPercent(3u, 0.70f, isDead: true).Object, // 70%, dead — NOT counted
            CreateMemberAtHpPercent(4u, 0.90f).Object,           // 90%, alive — not counted
        };

        var helper = new TestableApolloPartyHelper(members, config);
        var player = CreatePlayer().Object;

        // Act
        var (count, _, _, _) = helper.CountPartyMembersNeedingAoEHeal(player, healAmount: 0);

        // Assert — dead member is excluded
        Assert.Equal(1, count);
    }

    [Fact]
    public void CountPartyMembersNeedingAoEHeal_MembersOutOfRange_NotCounted()
    {
        // Arrange — 1 injured member in range, 1 injured member 100y away (outside Medica 20y radius)
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.AoEHealHpThreshold = 0.85f;

        var members = new List<IBattleChara>
        {
            CreateMemberAtHpPercent(1u, 0.70f).Object,    // 70%, at origin — counted
            CreateMemberFarAway(2u, 0.70f).Object,         // 70%, 100y away — NOT counted
        };

        var helper = new TestableApolloPartyHelper(members, config);
        var player = CreatePlayer().Object;

        // Act
        var (count, _, _, _) = helper.CountPartyMembersNeedingAoEHeal(player, healAmount: 0);

        // Assert — only the in-range member counts
        Assert.Equal(1, count);
    }

    [Fact]
    public void CountPartyMembersNeedingAoEHeal_ExactlyAtThreshold_NotCounted()
    {
        // Arrange — member at exactly 85% HP. The check is strict less-than (<),
        // so 85% exactly should NOT be counted.
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.AoEHealHpThreshold = 0.85f;

        var members = new List<IBattleChara>
        {
            CreateMemberAtHpPercent(1u, 0.85f).Object,  // exactly at threshold — not counted
        };

        var helper = new TestableApolloPartyHelper(members, config);
        var player = CreatePlayer().Object;

        // Act
        var (count, _, _, _) = helper.CountPartyMembersNeedingAoEHeal(player, healAmount: 0);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void CountPartyMembersNeedingAoEHeal_JustBelowThreshold_IsCounted()
    {
        // Arrange — member just below 85% threshold
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.AoEHealHpThreshold = 0.85f;

        // Use integer HP to avoid floating-point precision issues: 84999/100000 = 0.84999
        var mock = MockBuilders.CreateMockBattleChara(
            entityId: 1u,
            currentHp: 84999,
            maxHp: 100000,
            isDead: false,
            position: Vector3.Zero);
        mock.Setup(x => x.Name).Returns(new SeString());

        var members = new List<IBattleChara> { mock.Object };
        var helper = new TestableApolloPartyHelper(members, config);
        var player = CreatePlayer().Object;

        // Act
        var (count, _, _, _) = helper.CountPartyMembersNeedingAoEHeal(player, healAmount: 0);

        // Assert
        Assert.Equal(1, count);
    }

    #endregion
}
