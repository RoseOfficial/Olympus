using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Debuff;
using Olympus.Tests.Mocks;
using Xunit;

namespace Olympus.Tests.Rotation.Common.Helpers;

/// <summary>
/// Tests for <see cref="EsunaHelper.FindBestTarget"/>.
/// DistanceHelper.IsInRange uses IGameObject.Position, which MockBuilders sets to Vector3.Zero
/// by default -- placing both player and member at the origin puts them well within Esuna's
/// 30y range. Out-of-range tests override position to exceed that threshold.
/// </summary>
public class EsunaHelperTests
{
    private static readonly float EsunaRange = RoleActions.Esuna.Range; // 30f

    // --- Empty / trivial cases ---

    [Fact]
    public void Empty_Party_Returns_Null()
    {
        var player = MockBuilders.CreateMockPlayerCharacter().Object;
        var debuffService = MockBuilders.CreateMockDebuffDetectionService().Object;

        var (target, statusId, priority) = EsunaHelper.FindBestTarget(
            player, new List<IBattleChara>(), debuffService);

        Assert.Null(target);
        Assert.Equal(0u, statusId);
        Assert.Equal(DebuffPriority.None, priority);
    }

    [Fact]
    public void Dead_Member_Skipped()
    {
        var player = MockBuilders.CreateMockPlayerCharacter().Object;
        var member = MockBuilders.CreateMockBattleChara(isDead: true).Object;
        var debuffService = MockBuilders.CreateMockDebuffDetectionService().Object;

        var (target, statusId, priority) = EsunaHelper.FindBestTarget(
            player, new[] { member }, debuffService);

        Assert.Null(target);
        Assert.Equal(0u, statusId);
        Assert.Equal(DebuffPriority.None, priority);
    }

    // --- In-range member with a cleansable debuff ---

    [Fact]
    public void Returns_Member_With_Cleansable_Debuff()
    {
        // Player and member both at Vector3.Zero -- distance 0, within 30y.
        var player = MockBuilders.CreateMockPlayerCharacter().Object;
        var member = MockBuilders.CreateMockBattleChara(isDead: false).Object;

        const uint debuffStatusId = 910u; // Doom (Lethal tier)
        var debuffService = MockBuilders.CreateMockDebuffDetectionService(
            target => (debuffStatusId, DebuffPriority.Lethal, 5f)).Object;

        var (target, statusId, priority) = EsunaHelper.FindBestTarget(
            player, new[] { member }, debuffService);

        Assert.Same(member, target);
        Assert.Equal(debuffStatusId, statusId);
        Assert.Equal(DebuffPriority.Lethal, priority);
    }

    // --- Priority selection ---

    [Fact]
    public void Highest_Priority_Wins_Over_Lower()
    {
        var player = MockBuilders.CreateMockPlayerCharacter().Object;

        // Member A has Lethal debuff; member B has Low debuff.
        var memberA = MockBuilders.CreateMockBattleChara(entityId: 2u, isDead: false).Object;
        var memberB = MockBuilders.CreateMockBattleChara(entityId: 3u, isDead: false).Object;

        const uint lethalId = 910u;
        const uint lowId = 13u; // Bind (Low tier)

        var debuffServiceMock = MockBuilders.CreateMockDebuffDetectionService();
        debuffServiceMock
            .Setup(d => d.FindHighestPriorityDebuff(memberA))
            .Returns((lethalId, DebuffPriority.Lethal, 5f));
        debuffServiceMock
            .Setup(d => d.FindHighestPriorityDebuff(memberB))
            .Returns((lowId, DebuffPriority.Low, 8f));

        var (target, statusId, priority) = EsunaHelper.FindBestTarget(
            player, new[] { memberA, memberB }, debuffServiceMock.Object);

        Assert.Same(memberA, target);
        Assert.Equal(lethalId, statusId);
        Assert.Equal(DebuffPriority.Lethal, priority);
    }

    // --- Out-of-range member skipped ---

    [Fact]
    public void Out_Of_Range_Member_Skipped()
    {
        var player = MockBuilders.CreateMockPlayerCharacter(position: Vector3.Zero).Object;
        // Place member beyond Esuna's 30y range.
        var member = MockBuilders.CreateMockBattleChara(
            isDead: false,
            position: new Vector3(EsunaRange + 1f, 0f, 0f)).Object;

        const uint debuffStatusId = 17u; // Paralysis (Medium tier)
        var debuffService = MockBuilders.CreateMockDebuffDetectionService(
            _ => (debuffStatusId, DebuffPriority.Medium, 4f)).Object;

        var (target, statusId, priority) = EsunaHelper.FindBestTarget(
            player, new[] { member }, debuffService);

        Assert.Null(target);
        Assert.Equal(0u, statusId);
        Assert.Equal(DebuffPriority.None, priority);
    }
}
