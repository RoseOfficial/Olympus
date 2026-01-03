using Olympus.Rotation.ApolloCore.Modules;

namespace Olympus.Tests.Rotation.ApolloCore.Modules;

/// <summary>
/// Tests for Apollo module priority ordering.
/// Ensures modules execute in the correct priority order.
/// </summary>
public class ModulePriorityTests
{
    [Fact]
    public void ResurrectionModule_HasHighestPriority()
    {
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        // Resurrection should have lowest priority number (highest priority)
        Assert.True(resurrection.Priority < healing.Priority);
        Assert.True(resurrection.Priority < defensive.Priority);
        Assert.True(resurrection.Priority < buff.Priority);
        Assert.True(resurrection.Priority < damage.Priority);
    }

    [Fact]
    public void HealingModule_HasSecondHighestPriority()
    {
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        // Healing should be after resurrection but before others
        Assert.True(healing.Priority > resurrection.Priority);
        Assert.True(healing.Priority < defensive.Priority);
        Assert.True(healing.Priority < buff.Priority);
        Assert.True(healing.Priority < damage.Priority);
    }

    [Fact]
    public void DefensiveModule_HasThirdHighestPriority()
    {
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(defensive.Priority > healing.Priority);
        Assert.True(defensive.Priority < buff.Priority);
        Assert.True(defensive.Priority < damage.Priority);
    }

    [Fact]
    public void BuffModule_HasFourthHighestPriority()
    {
        var defensive = new DefensiveModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(buff.Priority > defensive.Priority);
        Assert.True(buff.Priority < damage.Priority);
    }

    [Fact]
    public void DamageModule_HasLowestPriority()
    {
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        // Damage should have highest priority number (lowest priority)
        Assert.True(damage.Priority > resurrection.Priority);
        Assert.True(damage.Priority > healing.Priority);
        Assert.True(damage.Priority > defensive.Priority);
        Assert.True(damage.Priority > buff.Priority);
    }

    [Fact]
    public void AllModules_HaveUniquePriorities()
    {
        var modules = new IApolloModule[]
        {
            new ResurrectionModule(),
            new HealingModule(),
            new DefensiveModule(),
            new BuffModule(),
            new DamageModule()
        };

        var priorities = modules.Select(m => m.Priority).ToArray();

        Assert.Equal(priorities.Length, priorities.Distinct().Count());
    }

    [Fact]
    public void AllModules_HaveNonEmptyNames()
    {
        var modules = new IApolloModule[]
        {
            new ResurrectionModule(),
            new HealingModule(),
            new DefensiveModule(),
            new BuffModule(),
            new DamageModule()
        };

        foreach (var module in modules)
        {
            Assert.False(string.IsNullOrWhiteSpace(module.Name),
                $"Module with priority {module.Priority} has empty name");
        }
    }

    [Fact]
    public void AllModules_HaveUniqueNames()
    {
        var modules = new IApolloModule[]
        {
            new ResurrectionModule(),
            new HealingModule(),
            new DefensiveModule(),
            new BuffModule(),
            new DamageModule()
        };

        var names = modules.Select(m => m.Name).ToArray();

        Assert.Equal(names.Length, names.Distinct().Count());
    }

    [Fact]
    public void Modules_SortedByPriority_ExecuteInCorrectOrder()
    {
        var modules = new List<IApolloModule>
        {
            new DamageModule(),      // Added out of order
            new HealingModule(),
            new BuffModule(),
            new ResurrectionModule(),
            new DefensiveModule()
        };

        // Sort by priority (as Apollo.cs does)
        modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        var expectedOrder = new[]
        {
            "Resurrection",
            "Healing",
            "Defensive",
            "Buffs",
            "Damage"
        };

        var actualOrder = modules.Select(m => m.Name).ToArray();

        Assert.Equal(expectedOrder, actualOrder);
    }

    [Theory]
    [InlineData(typeof(ResurrectionModule), 5)]
    [InlineData(typeof(HealingModule), 10)]
    [InlineData(typeof(DefensiveModule), 20)]
    [InlineData(typeof(BuffModule), 30)]
    [InlineData(typeof(DamageModule), 50)]
    public void Module_HasExpectedPriority(Type moduleType, int expectedPriority)
    {
        var module = (IApolloModule)Activator.CreateInstance(moduleType)!;
        Assert.Equal(expectedPriority, module.Priority);
    }
}
