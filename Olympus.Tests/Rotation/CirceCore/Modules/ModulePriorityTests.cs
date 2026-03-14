using Olympus.Rotation.CirceCore.Modules;

namespace Olympus.Tests.Rotation.CirceCore.Modules;

public class ModulePriorityTests
{
    [Fact]
    public void BuffModule_HasHigherPriority_ThanDamageModule()
    {
        var buff = new BuffModule();
        var damage = new DamageModule();
        Assert.True(buff.Priority < damage.Priority);
    }

    [Fact]
    public void BuffModule_Priority_IsCorrectValue()
    {
        Assert.Equal(20, new BuffModule().Priority);
    }

    [Fact]
    public void DamageModule_Priority_IsCorrectValue()
    {
        Assert.Equal(30, new DamageModule().Priority);
    }
}
