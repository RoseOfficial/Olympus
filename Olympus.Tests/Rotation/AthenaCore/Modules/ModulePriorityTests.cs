using System.Collections.Generic;
using Olympus.Rotation.AthenaCore.Modules;

namespace Olympus.Tests.Rotation.AthenaCore.Modules;

/// <summary>
/// Tests that Scholar modules have the correct priority ordering
/// and that all priorities are unique.
/// </summary>
public class ModulePriorityTests
{
    private static IReadOnlyList<IAthenaModule> CreateAllModules()
    {
        return new IAthenaModule[]
        {
            new FairyModule(),
            new ResurrectionModule(),
            new HealingModule(),
            new DefensiveModule(),
            new BuffModule(),
            new DamageModule(),
        };
    }

    #region Priority Ordering Tests

    [Fact]
    public void FairyModule_HasPriority3()
    {
        var module = new FairyModule();
        Assert.Equal(3, module.Priority);
    }

    [Fact]
    public void ResurrectionModule_HasPriority5()
    {
        var module = new ResurrectionModule();
        Assert.Equal(5, module.Priority);
    }

    [Fact]
    public void HealingModule_HasPriority10()
    {
        var module = new HealingModule();
        Assert.Equal(10, module.Priority);
    }

    [Fact]
    public void DefensiveModule_HasPriority20()
    {
        var module = new DefensiveModule();
        Assert.Equal(20, module.Priority);
    }

    [Fact]
    public void BuffModule_HasPriority30()
    {
        var module = new BuffModule();
        Assert.Equal(30, module.Priority);
    }

    [Fact]
    public void DamageModule_HasPriority50()
    {
        var module = new DamageModule();
        Assert.Equal(50, module.Priority);
    }

    #endregion

    #region Priority Uniqueness Tests

    [Fact]
    public void AllModules_HaveUniquePriorities()
    {
        var modules = CreateAllModules();
        var priorities = modules.Select(m => m.Priority).ToList();

        // All priorities must be distinct
        Assert.Equal(priorities.Count, priorities.Distinct().Count());
    }

    [Fact]
    public void AllModules_HaveNonZeroPriority()
    {
        var modules = CreateAllModules();

        foreach (var module in modules)
        {
            Assert.True(module.Priority > 0,
                $"Module {module.Name} has priority {module.Priority} which is not positive");
        }
    }

    #endregion

    #region Priority Ordering — Lower Number = Higher Priority

    [Fact]
    public void FairyModule_HasHigherPriorityThan_ResurrectionModule()
    {
        var fairy = new FairyModule();
        var resurrection = new ResurrectionModule();

        Assert.True(fairy.Priority < resurrection.Priority,
            $"FairyModule priority ({fairy.Priority}) should be less than ResurrectionModule priority ({resurrection.Priority})");
    }

    [Fact]
    public void ResurrectionModule_HasHigherPriorityThan_HealingModule()
    {
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();

        Assert.True(resurrection.Priority < healing.Priority,
            $"ResurrectionModule priority ({resurrection.Priority}) should be less than HealingModule priority ({healing.Priority})");
    }

    [Fact]
    public void HealingModule_HasHigherPriorityThan_DefensiveModule()
    {
        var healing = new HealingModule();
        var defensive = new DefensiveModule();

        Assert.True(healing.Priority < defensive.Priority,
            $"HealingModule priority ({healing.Priority}) should be less than DefensiveModule priority ({defensive.Priority})");
    }

    [Fact]
    public void DefensiveModule_HasHigherPriorityThan_BuffModule()
    {
        var defensive = new DefensiveModule();
        var buff = new BuffModule();

        Assert.True(defensive.Priority < buff.Priority,
            $"DefensiveModule priority ({defensive.Priority}) should be less than BuffModule priority ({buff.Priority})");
    }

    [Fact]
    public void BuffModule_HasHigherPriorityThan_DamageModule()
    {
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(buff.Priority < damage.Priority,
            $"BuffModule priority ({buff.Priority}) should be less than DamageModule priority ({damage.Priority})");
    }

    #endregion

    #region Module Count Test

    [Fact]
    public void AllModules_CountIs6()
    {
        var modules = CreateAllModules();
        Assert.Equal(6, modules.Count);
    }

    #endregion
}
