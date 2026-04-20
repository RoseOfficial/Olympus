using System.Linq;
using System.Reflection;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HephaestusCore.Abilities;
using Xunit;

namespace Olympus.Tests.Rotation.Common.Scheduling;

public class AbilityBehaviorMetaTests
{
    [Fact]
    public void EveryGnbAbility_HasNonNullAction()
    {
        var behaviors = typeof(GnbAbilities)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(AbilityBehavior))
            .Select(f => (Name: f.Name, Value: (AbilityBehavior)f.GetValue(null)!))
            .ToArray();

        Assert.NotEmpty(behaviors);
        foreach (var (name, value) in behaviors)
        {
            Assert.NotNull(value.Action);
            Assert.NotEqual(0u, value.Action.ActionId);
            Assert.False(string.IsNullOrEmpty(value.Action.Name), $"{name} has empty Action.Name");
        }
    }

    [Fact]
    public void EveryGnbAbility_ComboStepAndAdjustedProbe_AreMutuallyExclusive()
    {
        var behaviors = typeof(GnbAbilities)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(AbilityBehavior))
            .Select(f => (Name: f.Name, Value: (AbilityBehavior)f.GetValue(null)!))
            .ToArray();

        foreach (var (name, value) in behaviors)
        {
            var usesBoth = value.ComboStep is not null && value.AdjustedActionProbe is not null;
            Assert.False(usesBoth, $"{name} uses both ComboStep and AdjustedActionProbe - pick one");
        }
    }
}
