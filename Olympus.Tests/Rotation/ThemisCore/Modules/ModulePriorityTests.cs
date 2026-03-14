using Olympus.Rotation.ThemisCore.Modules;

namespace Olympus.Tests.Rotation.ThemisCore.Modules;

/// <summary>
/// Verifies that Themis modules have the correct priority ordering.
/// Priority 5 (EnmityModule) fires before mitigation (10), buffs (20), and damage (30).
/// </summary>
public class ModulePriorityTests
{
    [Fact]
    public void EnmityModule_HasPriority5()
    {
        var module = new EnmityModule();
        Assert.Equal(5, module.Priority);
    }

    [Fact]
    public void MitigationModule_HasPriority10()
    {
        var module = new MitigationModule();
        Assert.Equal(10, module.Priority);
    }

    [Fact]
    public void BuffModule_HasHigherPriorityThanDamage()
    {
        var buff = new BuffModule();
        var damage = new DamageModule();
        Assert.True(buff.Priority < damage.Priority);
    }

    [Fact]
    public void DamageModule_HasLowestPriority()
    {
        var enmity = new EnmityModule();
        var mitigation = new MitigationModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(enmity.Priority < mitigation.Priority);
        Assert.True(mitigation.Priority < buff.Priority);
        Assert.True(buff.Priority < damage.Priority);
    }

    [Fact]
    public void AllModules_HaveNames()
    {
        Assert.NotEmpty(new EnmityModule().Name);
        Assert.NotEmpty(new MitigationModule().Name);
        Assert.NotEmpty(new BuffModule().Name);
        Assert.NotEmpty(new DamageModule().Name);
    }
}
