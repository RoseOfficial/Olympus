using Olympus.Rotation.AsclepiusCore.Modules;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Tests for Asclepius (Sage) module priority ordering.
/// Ensures modules execute in the correct priority order:
///   3 - Kardia, 5 - Resurrection, 10 - Healing, 20 - Defensive, 50 - Damage
/// </summary>
public class ModulePriorityTests
{
    [Fact]
    public void KardiaModule_HasHighestPriority()
    {
        var kardia = new KardiaModule();
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var damage = new DamageModule();

        // Kardia should have the lowest priority number (runs first)
        Assert.True(kardia.Priority < resurrection.Priority);
        Assert.True(kardia.Priority < healing.Priority);
        Assert.True(kardia.Priority < defensive.Priority);
        Assert.True(kardia.Priority < damage.Priority);
    }

    [Fact]
    public void ResurrectionModule_HasSecondHighestPriority()
    {
        var kardia = new KardiaModule();
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var damage = new DamageModule();

        Assert.True(resurrection.Priority > kardia.Priority);
        Assert.True(resurrection.Priority < healing.Priority);
        Assert.True(resurrection.Priority < defensive.Priority);
        Assert.True(resurrection.Priority < damage.Priority);
    }

    [Fact]
    public void HealingModule_HasThirdHighestPriority()
    {
        var kardia = new KardiaModule();
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var damage = new DamageModule();

        Assert.True(healing.Priority > kardia.Priority);
        Assert.True(healing.Priority > resurrection.Priority);
        Assert.True(healing.Priority < defensive.Priority);
        Assert.True(healing.Priority < damage.Priority);
    }

    [Fact]
    public void DamageModule_HasLowestPriority()
    {
        var kardia = new KardiaModule();
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var damage = new DamageModule();

        // Damage should have the highest priority number (runs last)
        Assert.True(damage.Priority > kardia.Priority);
        Assert.True(damage.Priority > resurrection.Priority);
        Assert.True(damage.Priority > healing.Priority);
        Assert.True(damage.Priority > defensive.Priority);
    }

    [Fact]
    public void AllModules_HaveUniquePriorities()
    {
        var modules = new IAsclepiusModule[]
        {
            new KardiaModule(),
            new ResurrectionModule(),
            new HealingModule(),
            new DefensiveModule(),
            new DamageModule()
        };

        var priorities = modules.Select(m => m.Priority).ToArray();

        Assert.Equal(priorities.Length, priorities.Distinct().Count());
    }

    [Fact]
    public void AllModules_HaveNonEmptyNames()
    {
        var modules = new IAsclepiusModule[]
        {
            new KardiaModule(),
            new ResurrectionModule(),
            new HealingModule(),
            new DefensiveModule(),
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
        var modules = new IAsclepiusModule[]
        {
            new KardiaModule(),
            new ResurrectionModule(),
            new HealingModule(),
            new DefensiveModule(),
            new DamageModule()
        };

        var names = modules.Select(m => m.Name).ToArray();

        Assert.Equal(names.Length, names.Distinct().Count());
    }

    [Fact]
    public void Modules_SortedByPriority_ExecuteInCorrectOrder()
    {
        var modules = new List<IAsclepiusModule>
        {
            new DamageModule(),      // Added out of order
            new HealingModule(),
            new DefensiveModule(),
            new ResurrectionModule(),
            new KardiaModule()
        };

        // Sort by priority (as Asclepius.cs does)
        modules.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        var expectedOrder = new[]
        {
            "Kardia",
            "Resurrection",
            "Healing",
            "Defensive",
            "Damage"
        };

        var actualOrder = modules.Select(m => m.Name).ToArray();

        Assert.Equal(expectedOrder, actualOrder);
    }

    [Theory]
    [InlineData(typeof(KardiaModule), 3)]
    [InlineData(typeof(ResurrectionModule), 5)]
    [InlineData(typeof(HealingModule), 10)]
    [InlineData(typeof(DefensiveModule), 20)]
    [InlineData(typeof(DamageModule), 50)]
    public void Module_HasExpectedPriority(Type moduleType, int expectedPriority)
    {
        var module = (IAsclepiusModule)Activator.CreateInstance(moduleType)!;
        Assert.Equal(expectedPriority, module.Priority);
    }
}
