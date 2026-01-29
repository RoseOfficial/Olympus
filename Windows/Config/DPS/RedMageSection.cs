using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders the Red Mage (Circe) settings section.
/// </summary>
public sealed class RedMageSection
{
    private readonly Configuration config;
    private readonly Action save;

    public RedMageSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Red Mage", "Circe", ConfigUIHelpers.RedMageColor);

        DrawDamageSection();
        DrawManaSection();
        DrawMeleeSection();
        DrawBurstSection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.RedMage.DamageSection, "Damage"), "RDM"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableProcs = config.RedMage.EnableProcs;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.RedMage.EnableProcs, "Enable Procs"), ref enableProcs,
                Loc.T(LocalizedStrings.RedMage.EnableProcsDesc, "Use Verstone/Verfire procs"), save))
            {
                config.RedMage.EnableProcs = enableProcs;
            }

            var enableGrandImpact = config.RedMage.EnableGrandImpact;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.RedMage.EnableGrandImpact, "Enable Grand Impact"), ref enableGrandImpact,
                Loc.T(LocalizedStrings.RedMage.EnableGrandImpactDesc, "Use Grand Impact procs"), save))
            {
                config.RedMage.EnableGrandImpact = enableGrandImpact;
            }

            ConfigUIHelpers.Spacing();

            config.RedMage.AoEMinTargets = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.RedMage.AoEMinTargets, "AoE Min Targets"),
                config.RedMage.AoEMinTargets, 2, 8,
                Loc.T(LocalizedStrings.RedMage.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawManaSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.RedMage.ManaSection, "Mana Balance"), "RDM"))
        {
            ConfigUIHelpers.BeginIndent();

            var strictManaBalance = config.RedMage.StrictManaBalance;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.RedMage.StrictManaBalance, "Strict Mana Balance"), ref strictManaBalance,
                Loc.T(LocalizedStrings.RedMage.StrictManaBalanceDesc, "Strictly balance mana (prioritize lower)"), save))
            {
                config.RedMage.StrictManaBalance = strictManaBalance;
            }

            config.RedMage.ManaImbalanceThreshold = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.RedMage.ManaImbalanceThreshold, "Mana Imbalance Threshold"),
                config.RedMage.ManaImbalanceThreshold, 10, 50,
                Loc.T(LocalizedStrings.RedMage.ManaImbalanceThresholdDesc, "Max imbalance before prioritizing lower color"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawMeleeSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.RedMage.MeleeSection, "Melee Combo"), "RDM"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableMeleeCombo = config.RedMage.EnableMeleeCombo;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.RedMage.EnableMeleeCombo, "Enable Melee Combo"), ref enableMeleeCombo,
                Loc.T(LocalizedStrings.RedMage.EnableMeleeComboDesc, "Use melee combo (Riposte chain)"), save))
            {
                config.RedMage.EnableMeleeCombo = enableMeleeCombo;
            }

            var enableFinisherCombo = config.RedMage.EnableFinisherCombo;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.RedMage.EnableFinisherCombo, "Enable Finisher Combo"), ref enableFinisherCombo,
                Loc.T(LocalizedStrings.RedMage.EnableFinisherComboDesc, "Use finisher combo (Verholy/Verflare chain)"), save))
            {
                config.RedMage.EnableFinisherCombo = enableFinisherCombo;
            }

            config.RedMage.MeleeComboMinMana = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.RedMage.MeleeComboMinMana, "Melee Min Mana"),
                config.RedMage.MeleeComboMinMana, 50, 100,
                Loc.T(LocalizedStrings.RedMage.MeleeComboMinManaDesc, "Minimum mana to enter melee combo"), save);

            var finisherPreference = config.RedMage.FinisherPreference;
            if (ConfigUIHelpers.EnumCombo(Loc.T(LocalizedStrings.RedMage.FinisherPreference, "Finisher Preference"), ref finisherPreference,
                Loc.T(LocalizedStrings.RedMage.FinisherPreferenceDesc, "Verholy vs Verflare preference"), save))
            {
                config.RedMage.FinisherPreference = finisherPreference;
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.RedMage.BurstSection, "Burst Windows"), "RDM", false))
        {
            ConfigUIHelpers.BeginIndent();

            var enableEmbolden = config.RedMage.EnableEmbolden;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.RedMage.EnableEmbolden, "Enable Embolden"), ref enableEmbolden,
                Loc.T(LocalizedStrings.RedMage.EnableEmboldenDesc, "Use Embolden (party buff)"), save))
            {
                config.RedMage.EnableEmbolden = enableEmbolden;
            }

            var enableManafication = config.RedMage.EnableManafication;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.RedMage.EnableManafication, "Enable Manafication"), ref enableManafication,
                Loc.T(LocalizedStrings.RedMage.EnableManaficationDesc, "Use Manafication"), save))
            {
                config.RedMage.EnableManafication = enableManafication;
            }

            var alignWithParty = config.RedMage.AlignEmboldenWithParty;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.RedMage.AlignWithParty, "Align with Party"), ref alignWithParty,
                Loc.T(LocalizedStrings.RedMage.AlignWithPartyDesc, "Coordinate Embolden with party burst"), save))
            {
                config.RedMage.AlignEmboldenWithParty = alignWithParty;
            }

            config.RedMage.EmboldenHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.RedMage.EmboldenHoldTime, "Embolden Hold Time"),
                config.RedMage.EmboldenHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.RedMage.EmboldenHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
