using Olympus.Rotation.ZeusCore.Modules;

namespace Olympus.Tests.Rotation.ZeusCore.Modules;

/// <summary>
/// Verifies that Zeus modules have correct priority ordering.
/// BuffModule (20) fires before DamageModule (30).
/// </summary>
public class ModulePriorityTests
{
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
    public void BuffModule_HasHigherPriorityThanDamage()
    {
        var buff = new BuffModule();
        var damage = new DamageModule();
        Assert.True(buff.Priority < damage.Priority);
    }

    [Fact]
    public void AllModules_HaveNames()
    {
        Assert.NotEmpty(new BuffModule().Name);
        Assert.NotEmpty(new DamageModule().Name);
    }
}
