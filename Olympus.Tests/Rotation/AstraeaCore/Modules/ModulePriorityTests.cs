using System.Collections.Generic;
using System.Linq;
using Olympus.Rotation.AstraeaCore.Modules;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules;

/// <summary>
/// Tests for Astrologian (Astraea) module priority ordering.
/// Ensures modules execute in the correct priority order:
///   3 - Card, 5 - Resurrection, 10 - Healing, 20 - Defensive, 30 - Buff, 50 - Damage
/// </summary>
public class ModulePriorityTests
{
    [Fact]
    public void CardModule_HasHighestPriority()
    {
        var card = new CardModule();
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(card.Priority < resurrection.Priority);
        Assert.True(card.Priority < healing.Priority);
        Assert.True(card.Priority < defensive.Priority);
        Assert.True(card.Priority < buff.Priority);
        Assert.True(card.Priority < damage.Priority);
    }

    [Fact]
    public void ResurrectionModule_HasSecondHighestPriority()
    {
        var card = new CardModule();
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var damage = new DamageModule();

        Assert.True(resurrection.Priority > card.Priority);
        Assert.True(resurrection.Priority < healing.Priority);
        Assert.True(resurrection.Priority < damage.Priority);
    }

    [Fact]
    public void HealingModule_HasThirdHighestPriority()
    {
        var card = new CardModule();
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var damage = new DamageModule();

        Assert.True(healing.Priority > card.Priority);
        Assert.True(healing.Priority > resurrection.Priority);
        Assert.True(healing.Priority < defensive.Priority);
        Assert.True(healing.Priority < damage.Priority);
    }

    [Fact]
    public void DefensiveModule_HasFourthHighestPriority()
    {
        var card = new CardModule();
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(defensive.Priority > healing.Priority);
        Assert.True(defensive.Priority < buff.Priority);
        Assert.True(defensive.Priority < damage.Priority);
    }

    [Fact]
    public void BuffModule_HasFifthHighestPriority()
    {
        var buff = new BuffModule();
        var defensive = new DefensiveModule();
        var damage = new DamageModule();

        Assert.True(buff.Priority > defensive.Priority);
        Assert.True(buff.Priority < damage.Priority);
    }

    [Fact]
    public void DamageModule_HasLowestPriority()
    {
        var card = new CardModule();
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(damage.Priority > card.Priority);
        Assert.True(damage.Priority > resurrection.Priority);
        Assert.True(damage.Priority > healing.Priority);
        Assert.True(damage.Priority > defensive.Priority);
        Assert.True(damage.Priority > buff.Priority);
    }

    [Fact]
    public void AllModules_HaveUniquePriorities()
    {
        var modules = new IAstraeaModule[]
        {
            new CardModule(),
            new ResurrectionModule(),
            new HealingModule(),
            new DefensiveModule(),
            new BuffModule(),
            new DamageModule(),
        };

        var priorities = modules.Select(m => m.Priority).ToArray();

        Assert.Equal(priorities.Length, priorities.Distinct().Count());
    }

    [Fact]
    public void AllModules_HaveNonEmptyNames()
    {
        var modules = new IAstraeaModule[]
        {
            new CardModule(),
            new ResurrectionModule(),
            new HealingModule(),
            new DefensiveModule(),
            new BuffModule(),
            new DamageModule(),
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
        var modules = new IAstraeaModule[]
        {
            new CardModule(),
            new ResurrectionModule(),
            new HealingModule(),
            new DefensiveModule(),
            new BuffModule(),
            new DamageModule(),
        };

        var names = modules.Select(m => m.Name).ToArray();

        Assert.Equal(names.Length, names.Distinct().Count());
    }

    [Fact]
    public void Modules_SortedByPriority_ExecuteInCorrectOrder()
    {
        var modules = new List<IAstraeaModule>
        {
            new DamageModule(),      // Added out of order
            new BuffModule(),
            new DefensiveModule(),
            new HealingModule(),
            new ResurrectionModule(),
            new CardModule(),
        };

        // Sort by priority (as Astraea.cs does)
        modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        var expectedOrder = new[]
        {
            "Card",
            "Resurrection",
            "Healing",
            "Defensive",
            "Buff",
            "Damage",
        };

        var actualOrder = modules.Select(m => m.Name).ToArray();

        Assert.Equal(expectedOrder, actualOrder);
    }

    [Theory]
    [InlineData(typeof(CardModule), 3)]
    [InlineData(typeof(ResurrectionModule), 5)]
    [InlineData(typeof(HealingModule), 10)]
    [InlineData(typeof(DefensiveModule), 20)]
    [InlineData(typeof(BuffModule), 30)]
    [InlineData(typeof(DamageModule), 50)]
    public void Module_HasExpectedPriority(Type moduleType, int expectedPriority)
    {
        var module = (IAstraeaModule)Activator.CreateInstance(moduleType)!;
        Assert.Equal(expectedPriority, module.Priority);
    }
}
