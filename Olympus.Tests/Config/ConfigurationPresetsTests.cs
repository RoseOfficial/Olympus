using Olympus.Config;

namespace Olympus.Tests.Config;

public class ConfigurationPresetsTests
{
    // ──────────────────────────────────────────────────────────────
    // Custom preset guard: ApplyPreset returns early, nothing changes
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Custom_DoesNotModifyConfiguration()
    {
        var config = new Configuration();
        var originalTolerance = config.MovementTolerance;
        var originalPreset = config.ActivePreset; // ConfigurationPreset.Custom by default

        ConfigurationPresets.ApplyPreset(config, ConfigurationPreset.Custom);

        Assert.Equal(originalTolerance, config.MovementTolerance);
        Assert.Equal(originalPreset, config.ActivePreset);
    }

    // ──────────────────────────────────────────────────────────────
    // All non-Custom presets apply without throwing on a fresh Configuration
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ConfigurationPreset.Raid)]
    [InlineData(ConfigurationPreset.Dungeon)]
    [InlineData(ConfigurationPreset.Casual)]
    [InlineData(ConfigurationPreset.Conservative)]
    [InlineData(ConfigurationPreset.Balanced)]
    [InlineData(ConfigurationPreset.Aggressive)]
    [InlineData(ConfigurationPreset.Proactive)]
    public void ApplyPreset_FreshConfiguration_DoesNotThrow(ConfigurationPreset preset)
    {
        var config = new Configuration();
        var ex = Record.Exception(() => ConfigurationPresets.ApplyPreset(config, preset));
        Assert.Null(ex);
    }

    // ──────────────────────────────────────────────────────────────
    // ActivePreset is stamped by every non-Custom preset
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ConfigurationPreset.Raid)]
    [InlineData(ConfigurationPreset.Dungeon)]
    [InlineData(ConfigurationPreset.Casual)]
    [InlineData(ConfigurationPreset.Conservative)]
    [InlineData(ConfigurationPreset.Balanced)]
    [InlineData(ConfigurationPreset.Aggressive)]
    [InlineData(ConfigurationPreset.Proactive)]
    public void ApplyPreset_SetsActivePreset(ConfigurationPreset preset)
    {
        var config = new Configuration();
        ConfigurationPresets.ApplyPreset(config, preset);
        Assert.Equal(preset, config.ActivePreset);
    }

    // ──────────────────────────────────────────────────────────────
    // Idempotency: applying the same preset twice yields identical representative field values
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ConfigurationPreset.Raid)]
    [InlineData(ConfigurationPreset.Dungeon)]
    [InlineData(ConfigurationPreset.Casual)]
    [InlineData(ConfigurationPreset.Conservative)]
    [InlineData(ConfigurationPreset.Balanced)]
    [InlineData(ConfigurationPreset.Aggressive)]
    [InlineData(ConfigurationPreset.Proactive)]
    public void ApplyPreset_AppliedTwice_YieldsIdenticalRepresentativeFields(ConfigurationPreset preset)
    {
        var config = new Configuration();
        ConfigurationPresets.ApplyPreset(config, preset);

        var tolerance1   = config.MovementTolerance;
        var healerLucid1 = config.HealerShared.LucidDreamingThreshold;
        var casterLucid1 = config.CasterShared.LucidDreamingThreshold;
        var secondWind1  = config.MeleeShared.SecondWindHpThreshold;
        var bloodbath1   = config.MeleeShared.BloodbathHpThreshold;
        var burstPool1   = config.Dragoon.EnableBurstPooling;

        ConfigurationPresets.ApplyPreset(config, preset);

        Assert.Equal(tolerance1,   config.MovementTolerance);
        Assert.Equal(healerLucid1, config.HealerShared.LucidDreamingThreshold);
        Assert.Equal(casterLucid1, config.CasterShared.LucidDreamingThreshold);
        Assert.Equal(secondWind1,  config.MeleeShared.SecondWindHpThreshold);
        Assert.Equal(bloodbath1,   config.MeleeShared.BloodbathHpThreshold);
        Assert.Equal(burstPool1,   config.Dragoon.EnableBurstPooling);
    }

    // ──────────────────────────────────────────────────────────────
    // Shared role thresholds: content-type presets (Raid / Dungeon / Casual)
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ConfigurationPreset.Raid,    0.50f, 0.85f, 0.70f, 0.70f)]
    [InlineData(ConfigurationPreset.Dungeon, 0.40f, 0.75f, 0.60f, 0.60f)]
    [InlineData(ConfigurationPreset.Casual,  0.60f, 0.90f, 0.80f, 0.80f)]
    public void ContentPreset_SetsExpectedSharedRoleThresholds(
        ConfigurationPreset preset,
        float expectedSecondWind,
        float expectedBloodbath,
        float expectedCasterLucid,
        float expectedHealerLucid)
    {
        var config = new Configuration();
        ConfigurationPresets.ApplyPreset(config, preset);

        Assert.Equal(expectedSecondWind,  config.MeleeShared.SecondWindHpThreshold);
        Assert.Equal(expectedBloodbath,   config.MeleeShared.BloodbathHpThreshold);
        Assert.Equal(expectedCasterLucid, config.CasterShared.LucidDreamingThreshold);
        Assert.Equal(expectedHealerLucid, config.HealerShared.LucidDreamingThreshold);
    }

    // ──────────────────────────────────────────────────────────────
    // Shared role thresholds: playstyle presets with no role filter
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ConfigurationPreset.Conservative, 0.60f, 0.90f, 0.80f, 0.80f)]
    [InlineData(ConfigurationPreset.Balanced,     0.50f, 0.85f, 0.70f, 0.70f)]
    [InlineData(ConfigurationPreset.Aggressive,   0.40f, 0.75f, 0.60f, 0.60f)]
    [InlineData(ConfigurationPreset.Proactive,    0.50f, 0.85f, 0.70f, 0.70f)]
    public void PlaystylePreset_NoRole_SetsExpectedSharedRoleThresholds(
        ConfigurationPreset preset,
        float expectedSecondWind,
        float expectedBloodbath,
        float expectedCasterLucid,
        float expectedHealerLucid)
    {
        var config = new Configuration();
        ConfigurationPresets.ApplyPreset(config, preset, currentRole: null);

        Assert.Equal(expectedSecondWind,  config.MeleeShared.SecondWindHpThreshold);
        Assert.Equal(expectedBloodbath,   config.MeleeShared.BloodbathHpThreshold);
        Assert.Equal(expectedCasterLucid, config.CasterShared.LucidDreamingThreshold);
        Assert.Equal(expectedHealerLucid, config.HealerShared.LucidDreamingThreshold);
    }

    // ──────────────────────────────────────────────────────────────
    // Movement tolerance per preset
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ConfigurationPreset.Raid,         0.10f)]
    [InlineData(ConfigurationPreset.Dungeon,       0.05f)]
    [InlineData(ConfigurationPreset.Casual,        0.15f)]
    [InlineData(ConfigurationPreset.Conservative,  0.15f)]
    [InlineData(ConfigurationPreset.Balanced,      0.10f)]
    [InlineData(ConfigurationPreset.Aggressive,    0.05f)]
    [InlineData(ConfigurationPreset.Proactive,     0.10f)]
    public void ApplyPreset_SetsExpectedMovementTolerance(ConfigurationPreset preset, float expected)
    {
        var config = new Configuration();
        ConfigurationPresets.ApplyPreset(config, preset);
        Assert.Equal(expected, config.MovementTolerance);
    }

    // ──────────────────────────────────────────────────────────────
    // DPS burst pooling: Conservative disables all 13 DPS jobs
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Conservative_DisablesBurstPoolingForAllDpsJobs()
    {
        var config = new Configuration();
        ConfigurationPresets.ApplyPreset(config, ConfigurationPreset.Conservative);

        Assert.False(config.Dragoon.EnableBurstPooling);
        Assert.False(config.Monk.EnableBurstPooling);
        Assert.False(config.Ninja.EnableBurstPooling);
        Assert.False(config.Samurai.EnableBurstPooling);
        Assert.False(config.Reaper.EnableBurstPooling);
        Assert.False(config.Viper.EnableBurstPooling);
        Assert.False(config.Bard.EnableBurstPooling);
        Assert.False(config.Machinist.EnableBurstPooling);
        Assert.False(config.Dancer.EnableBurstPooling);
        Assert.False(config.BlackMage.EnableBurstPooling);
        Assert.False(config.Summoner.EnableBurstPooling);
        Assert.False(config.RedMage.EnableBurstPooling);
        Assert.False(config.Pictomancer.EnableBurstPooling);
    }

    // ──────────────────────────────────────────────────────────────
    // DPS burst pooling: Balanced / Aggressive / Proactive re-enable all 13 jobs
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ConfigurationPreset.Balanced)]
    [InlineData(ConfigurationPreset.Aggressive)]
    [InlineData(ConfigurationPreset.Proactive)]
    public void Preset_AfterConservative_ReenablesBurstPoolingForAllDpsJobs(ConfigurationPreset preset)
    {
        var config = new Configuration();
        ConfigurationPresets.ApplyPreset(config, ConfigurationPreset.Conservative);
        ConfigurationPresets.ApplyPreset(config, preset);

        Assert.True(config.Dragoon.EnableBurstPooling);
        Assert.True(config.Monk.EnableBurstPooling);
        Assert.True(config.Ninja.EnableBurstPooling);
        Assert.True(config.Samurai.EnableBurstPooling);
        Assert.True(config.Reaper.EnableBurstPooling);
        Assert.True(config.Viper.EnableBurstPooling);
        Assert.True(config.Bard.EnableBurstPooling);
        Assert.True(config.Machinist.EnableBurstPooling);
        Assert.True(config.Dancer.EnableBurstPooling);
        Assert.True(config.BlackMage.EnableBurstPooling);
        Assert.True(config.Summoner.EnableBurstPooling);
        Assert.True(config.RedMage.EnableBurstPooling);
        Assert.True(config.Pictomancer.EnableBurstPooling);
    }

    // ──────────────────────────────────────────────────────────────
    // Healer burst pooling: Conservative disables, Balanced/Aggressive/Proactive enable
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Conservative_DisablesBurstPoolingForHealerShared()
    {
        var config = new Configuration();
        ConfigurationPresets.ApplyPreset(config, ConfigurationPreset.Conservative);

        Assert.False(config.HealerShared.EnableBurstPooling);
    }

    [Theory]
    [InlineData(ConfigurationPreset.Balanced)]
    [InlineData(ConfigurationPreset.Aggressive)]
    [InlineData(ConfigurationPreset.Proactive)]
    public void Preset_AfterConservative_ReenablesHealerBurstPooling(ConfigurationPreset preset)
    {
        var config = new Configuration();
        ConfigurationPresets.ApplyPreset(config, ConfigurationPreset.Conservative);
        ConfigurationPresets.ApplyPreset(config, preset);

        Assert.True(config.HealerShared.EnableBurstPooling);
    }

    // ──────────────────────────────────────────────────────────────
    // GetJobRole: all 21 combat job IDs return the correct role
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(24u, JobRole.Healer)]    // WHM
    [InlineData(28u, JobRole.Healer)]    // SCH
    [InlineData(33u, JobRole.Healer)]    // AST
    [InlineData(40u, JobRole.Healer)]    // SGE
    [InlineData(19u, JobRole.Tank)]      // PLD
    [InlineData(21u, JobRole.Tank)]      // WAR
    [InlineData(32u, JobRole.Tank)]      // DRK
    [InlineData(37u, JobRole.Tank)]      // GNB
    [InlineData(20u, JobRole.MeleeDps)]  // MNK
    [InlineData(22u, JobRole.MeleeDps)]  // DRG
    [InlineData(30u, JobRole.MeleeDps)]  // NIN
    [InlineData(34u, JobRole.MeleeDps)]  // SAM
    [InlineData(39u, JobRole.MeleeDps)]  // RPR
    [InlineData(41u, JobRole.MeleeDps)]  // VPR
    [InlineData(23u, JobRole.RangedDps)] // BRD
    [InlineData(31u, JobRole.RangedDps)] // MCH
    [InlineData(38u, JobRole.RangedDps)] // DNC
    [InlineData(25u, JobRole.CasterDps)] // BLM
    [InlineData(27u, JobRole.CasterDps)] // SMN
    [InlineData(35u, JobRole.CasterDps)] // RDM
    [InlineData(42u, JobRole.CasterDps)] // PCT
    public void GetJobRole_KnownJobId_ReturnsExpectedRole(uint jobId, JobRole expected)
    {
        var role = ConfigurationPresets.GetJobRole(jobId);
        Assert.Equal(expected, role);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(99u)]
    public void GetJobRole_UnknownJobId_ReturnsNull(uint jobId)
    {
        Assert.Null(ConfigurationPresets.GetJobRole(jobId));
    }

    // ──────────────────────────────────────────────────────────────
    // Role-aware playstyle: Conservative with a specific role only touches that role's fields
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Conservative_HealerRole_SetsHealerLucidButDoesNotOverwriteMeleeThresholds()
    {
        var config = new Configuration();
        config.MeleeShared.SecondWindHpThreshold = 0.99f;
        config.MeleeShared.BloodbathHpThreshold  = 0.99f;

        ConfigurationPresets.ApplyPreset(config, ConfigurationPreset.Conservative, currentRole: JobRole.Healer);

        Assert.Equal(0.80f, config.HealerShared.LucidDreamingThreshold);
        // Melee thresholds must not have been touched
        Assert.Equal(0.99f, config.MeleeShared.SecondWindHpThreshold);
        Assert.Equal(0.99f, config.MeleeShared.BloodbathHpThreshold);
    }

    [Fact]
    public void Conservative_MeleeRole_SetsMeleeThresholdsButDoesNotOverwriteHealerLucid()
    {
        var config = new Configuration();
        config.HealerShared.LucidDreamingThreshold = 0.99f;

        ConfigurationPresets.ApplyPreset(config, ConfigurationPreset.Conservative, currentRole: JobRole.MeleeDps);

        Assert.Equal(0.60f, config.MeleeShared.SecondWindHpThreshold);
        Assert.Equal(0.90f, config.MeleeShared.BloodbathHpThreshold);
        // HealerShared was not in scope for this role
        Assert.Equal(0.99f, config.HealerShared.LucidDreamingThreshold);
    }

    [Fact]
    public void Conservative_CasterRole_SetsCasterLucidButDoesNotOverwriteMeleeThresholds()
    {
        var config = new Configuration();
        config.MeleeShared.SecondWindHpThreshold = 0.99f;

        ConfigurationPresets.ApplyPreset(config, ConfigurationPreset.Conservative, currentRole: JobRole.CasterDps);

        Assert.Equal(0.80f, config.CasterShared.LucidDreamingThreshold);
        // Melee thresholds must not have been touched
        Assert.Equal(0.99f, config.MeleeShared.SecondWindHpThreshold);
    }

    // ──────────────────────────────────────────────────────────────
    // Proactive: sets the master EnablePartyCoordination switch AND sub-feature flags
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Proactive_EnablesPartyCoordinationMasterSwitch()
    {
        var config = new Configuration();
        config.PartyCoordination.EnablePartyCoordination = false;

        ConfigurationPresets.ApplyPreset(config, ConfigurationPreset.Proactive);

        // The master switch must be on for sub-features to have any runtime effect.
        // partyCoordinationService is created conditionally at startup based on this flag.
        Assert.True(config.PartyCoordination.EnablePartyCoordination);
    }

    [Fact]
    public void Proactive_ExplicitlyEnablesPartyCoordinationBurstFlags()
    {
        var config = new Configuration();
        config.PartyCoordination.EnableHealerBurstAwareness = false;
        config.PartyCoordination.EnableRaidBuffCoordination = false;

        ConfigurationPresets.ApplyPreset(config, ConfigurationPreset.Proactive);

        Assert.True(config.PartyCoordination.EnableHealerBurstAwareness);
        Assert.True(config.PartyCoordination.EnableRaidBuffCoordination);
    }

    // ──────────────────────────────────────────────────────────────
    // Content presets: Dungeon is reactive; Raid is proactive
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Dungeon_DisablesCoHealerAwarenessAndPreemptiveHealing()
    {
        var config = new Configuration();
        ConfigurationPresets.ApplyPreset(config, ConfigurationPreset.Dungeon);

        Assert.False(config.Healing.EnableCoHealerAwareness);
        Assert.False(config.Healing.EnablePreemptiveHealing);
        Assert.False(config.Healing.EnableMechanicAwareness);
    }

    [Fact]
    public void Raid_EnablesCoHealerAwarenessAndPreemptiveHealing()
    {
        var config = new Configuration();
        ConfigurationPresets.ApplyPreset(config, ConfigurationPreset.Raid);

        Assert.True(config.Healing.EnableCoHealerAwareness);
        Assert.True(config.Healing.EnablePreemptiveHealing);
        Assert.True(config.Healing.EnableMechanicAwareness);
    }

    // ──────────────────────────────────────────────────────────────
    // Healer burst pooling: Conservative/Dungeon/Casual disable it,
    // Balanced/Aggressive/Proactive/Raid enable it
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ConfigurationPreset.Conservative)]
    [InlineData(ConfigurationPreset.Dungeon)]
    [InlineData(ConfigurationPreset.Casual)]
    public void Preset_DisablesBurstPoolingForHealers(ConfigurationPreset preset)
    {
        var config = new Configuration();
        ConfigurationPresets.ApplyPreset(config, preset);

        Assert.False(config.HealerShared.EnableBurstPooling);
    }

    [Theory]
    [InlineData(ConfigurationPreset.Balanced)]
    [InlineData(ConfigurationPreset.Aggressive)]
    [InlineData(ConfigurationPreset.Proactive)]
    [InlineData(ConfigurationPreset.Raid)]
    public void Preset_EnablesBurstPoolingForHealers(ConfigurationPreset preset)
    {
        var config = new Configuration();
        ConfigurationPresets.ApplyPreset(config, ConfigurationPreset.Conservative); // disable first
        ConfigurationPresets.ApplyPreset(config, preset);

        Assert.True(config.HealerShared.EnableBurstPooling);
    }

    // ──────────────────────────────────────────────────────────────
    // GetDescription: returns a non-empty string for every preset value
    // ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ConfigurationPreset.Custom)]
    [InlineData(ConfigurationPreset.Raid)]
    [InlineData(ConfigurationPreset.Dungeon)]
    [InlineData(ConfigurationPreset.Casual)]
    [InlineData(ConfigurationPreset.Conservative)]
    [InlineData(ConfigurationPreset.Balanced)]
    [InlineData(ConfigurationPreset.Aggressive)]
    [InlineData(ConfigurationPreset.Proactive)]
    public void GetDescription_ReturnsNonEmptyString(ConfigurationPreset preset)
    {
        var desc = ConfigurationPresets.GetDescription(preset);
        Assert.False(string.IsNullOrWhiteSpace(desc));
    }
}
