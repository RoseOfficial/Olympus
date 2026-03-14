using Olympus.Rotation.HermesCore.Modules;

namespace Olympus.Tests.Rotation.HermesCore.Modules;

/// <summary>
/// Verifies that Hermes modules have correct priority ordering.
/// NinjutsuModule (10) fires before BuffModule (20) which fires before DamageModule (50).
/// Lower number = higher priority (fires first).
/// </summary>
public class ModulePriorityTests
{
    [Fact]
    public void NinjutsuModule_HasPriority10()
    {
        var module = new NinjutsuModule();
        Assert.Equal(10, module.Priority);
    }

    [Fact]
    public void BuffModule_HasPriority20()
    {
        var module = new BuffModule();
        Assert.Equal(20, module.Priority);
    }

    [Fact]
    public void DamageModule_HasPriority30()
    {
        var module = new DamageModule();
        Assert.Equal(30, module.Priority);
    }

    [Fact]
    public void NinjutsuModule_HasHighestPriority_FiresFirst()
    {
        var ninjutsu = new NinjutsuModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(ninjutsu.Priority < buff.Priority);
        Assert.True(ninjutsu.Priority < damage.Priority);
    }

    [Fact]
    public void BuffModule_HasHigherPriorityThanDamage()
    {
        var buff = new BuffModule();
        var damage = new DamageModule();
        Assert.True(buff.Priority < damage.Priority);
    }

    [Fact]
    public void AllModules_HaveDistinctPriorities()
    {
        var priorities = new[]
        {
            new NinjutsuModule().Priority,
            new BuffModule().Priority,
            new DamageModule().Priority
        };

        Assert.Equal(priorities.Length, priorities.Distinct().Count());
    }

    [Fact]
    public void AllModules_HaveNames()
    {
        Assert.NotEmpty(new NinjutsuModule().Name);
        Assert.NotEmpty(new BuffModule().Name);
        Assert.NotEmpty(new DamageModule().Name);
    }
}
