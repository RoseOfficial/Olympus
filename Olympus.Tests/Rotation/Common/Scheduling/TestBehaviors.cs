using Olympus.Models.Action;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Tests.Rotation.Common.Scheduling;

internal static class TestBehaviors
{
    public static AbilityBehavior InstantGcd(uint actionId = 1001, byte minLevel = 1) => new()
    {
        Action = new ActionDefinition
        {
            ActionId = actionId,
            Name = "TestGCD",
            MinLevel = minLevel,
            Category = ActionCategory.GCD,
            TargetType = ActionTargetType.SingleEnemy,
            CastTime = 0f,
            RecastTime = 2.5f,
            Range = 3f,
        },
    };

    public static AbilityBehavior InstantOgcd(uint actionId = 2001, byte minLevel = 1) => new()
    {
        Action = new ActionDefinition
        {
            ActionId = actionId,
            Name = "TestoGCD",
            MinLevel = minLevel,
            Category = ActionCategory.oGCD,
            TargetType = ActionTargetType.SingleEnemy,
            CastTime = 0f,
            RecastTime = 30f,
            Range = 3f,
        },
    };
}
