using System.Reflection;
using Olympus.Config;
using Xunit;

namespace Olympus.Tests.Config;

/// <summary>
/// Tests for Configuration.ResetToDefaults() coverage and schema migration.
/// </summary>
public class ConfigurationResetTests
{
    // ──────────────────────────────────────────────────────────────
    // Finding #25: Consumables, Movement, and Input must be reset
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ResetToDefaults_ResetsConsumablesConfig()
    {
        var config = new Configuration();
        config.Consumables.EnableAutoTincture = true;

        config.ResetToDefaults();

        Assert.False(config.Consumables.EnableAutoTincture,
            "EnableAutoTincture must be false after reset -- tinctures cost real gil");
    }

    [Fact]
    public void ResetToDefaults_ResetsMovementConfig()
    {
        var config = new Configuration();
        config.Movement.EnableTrashAoEAvoidance = true;
        config.Movement.EnableAutoInteract = true;

        config.ResetToDefaults();

        Assert.False(config.Movement.EnableTrashAoEAvoidance,
            "EnableTrashAoEAvoidance must be false after reset -- deliberate opt-in only");
        Assert.False(config.Movement.EnableAutoInteract,
            "EnableAutoInteract must be false after reset -- deliberate opt-in only");
    }

    [Fact]
    public void ResetToDefaults_ResetsInputConfig()
    {
        var config = new Configuration();
        config.Input.EnableModifierOverrides = true;

        config.ResetToDefaults();

        Assert.False(config.Input.EnableModifierOverrides,
            "EnableModifierOverrides must be false after reset -- conflicts with chat typing when stuck on");
    }

    // ──────────────────────────────────────────────────────────────
    // Reflection guard: every nested config property must be a new
    // instance after ResetToDefaults(). If this test fails, a newly
    // added config property is missing a reset line in ResetToDefaults().
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ResetToDefaults_AllNestedConfigPropertiesAreNewInstances()
    {
        var config = new Configuration();

        // Collect references to all nested config objects before reset.
        // We only look at public properties whose type is in an Olympus.Config namespace
        // (covers both Olympus.Config and Olympus.Config.DPS).
        var before = CollectNestedConfigRefs(config);

        config.ResetToDefaults();

        var after = CollectNestedConfigRefs(config);

        foreach (var (name, beforeRef) in before)
        {
            Assert.True(after.ContainsKey(name),
                $"Property {name} disappeared from Configuration after ResetToDefaults()");

            Assert.False(ReferenceEquals(beforeRef, after[name]),
                $"Configuration.{name} was NOT replaced by ResetToDefaults(). " +
                $"Add '{name} = new {beforeRef!.GetType().Name}();' to ResetToDefaults().");
        }
    }

    /// <summary>
    /// Collects name->reference pairs for all Configuration properties whose
    /// declared type is a class in an Olympus.Config namespace (covers both
    /// Olympus.Config.* and Olympus.Config.DPS.*).
    /// </summary>
    private static Dictionary<string, object?> CollectNestedConfigRefs(Configuration config)
    {
        var result = new Dictionary<string, object?>();
        foreach (var prop in typeof(Configuration).GetProperties(
                     BindingFlags.Public | BindingFlags.Instance))
        {
            var t = prop.PropertyType;
            if (!t.IsClass || t == typeof(string))
                continue;
            var ns = t.Namespace ?? string.Empty;
            if (!ns.StartsWith("Olympus.Config", StringComparison.Ordinal))
                continue;
            result[prop.Name] = prop.GetValue(config);
        }
        return result;
    }

    // ──────────────────────────────────────────────────────────────
    // C3: Version migration -- OnDeserialized / MigrateIfNeeded
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void OnDeserialized_V1Config_BumpsVersionTo2()
    {
        var config = new Configuration { Version = 1 };

        config.OnDeserialized();

        Assert.Equal(2, config.Version);
    }

    [Fact]
    public void OnDeserialized_V2Config_VersionUnchanged()
    {
        var config = new Configuration { Version = 2 };

        config.OnDeserialized();

        Assert.Equal(2, config.Version);
    }

    [Fact]
    public void OnDeserialized_FreshConfig_VersionUnchanged()
    {
        // A brand-new Configuration already has Version = 2, so migration is a no-op.
        var config = new Configuration();
        var initialVersion = config.Version;

        config.OnDeserialized();

        Assert.Equal(initialVersion, config.Version);
    }
}
