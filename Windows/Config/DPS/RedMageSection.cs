using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Data;
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
        DrawMovementSection();
        DrawRoleActionsSection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.RedMage.DamageSection, "Damage"), "RDM"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.RedMage.EnableProcs, "Enable Procs"),
                () => config.RedMage.EnableProcs,
                v => config.RedMage.EnableProcs = v,
                Loc.T(LocalizedStrings.RedMage.EnableProcsDesc, "Use Verstone/Verfire procs"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.RedMage.EnableGrandImpact, "Enable Grand Impact"),
                () => config.RedMage.EnableGrandImpact,
                v => config.RedMage.EnableGrandImpact = v,
                null, save, actionId: RDMActions.GrandImpact.ActionId);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.RedMage.EnableAoERotation, "Enable AoE Rotation"),
                () => config.RedMage.EnableAoERotation,
                v => config.RedMage.EnableAoERotation = v,
                Loc.T(LocalizedStrings.RedMage.EnableAoERotationDesc, "Switch to AoE combo at 3+ enemies."), save);

            if (config.RedMage.EnableAoERotation)
            {
                config.RedMage.AoEMinTargets = ConfigUIHelpers.IntSlider(
                    Loc.T(LocalizedStrings.RedMage.AoEMinTargets, "AoE Min Targets"),
                    config.RedMage.AoEMinTargets, 2, 8,
                    Loc.T(LocalizedStrings.RedMage.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save, v => config.RedMage.AoEMinTargets = v);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawManaSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.RedMage.ManaSection, "Mana Balance"), "RDM"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.RedMage.StrictManaBalance, "Strict Mana Balance"),
                () => config.RedMage.StrictManaBalance,
                v => config.RedMage.StrictManaBalance = v,
                Loc.T(LocalizedStrings.RedMage.StrictManaBalanceDesc, "Strictly balance mana (prioritize lower)"), save);

            config.RedMage.ManaImbalanceThreshold = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.RedMage.ManaImbalanceThreshold, "Mana Imbalance Threshold"),
                config.RedMage.ManaImbalanceThreshold, 10, 50,
                Loc.T(LocalizedStrings.RedMage.ManaImbalanceThresholdDesc, "Max imbalance before prioritizing lower color"), save, v => config.RedMage.ManaImbalanceThreshold = v);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawMeleeSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.RedMage.MeleeSection, "Melee Combo"), "RDM"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.RedMage.EnableMeleeCombo, "Enable Melee Combo"),
                () => config.RedMage.EnableMeleeCombo,
                v => config.RedMage.EnableMeleeCombo = v,
                Loc.T(LocalizedStrings.RedMage.EnableMeleeComboDesc, "Use melee combo (Riposte chain)"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.RedMage.EnableFinisherCombo, "Enable Finisher Combo"),
                () => config.RedMage.EnableFinisherCombo,
                v => config.RedMage.EnableFinisherCombo = v,
                Loc.T(LocalizedStrings.RedMage.EnableFinisherComboDesc, "Use finisher combo (Verholy/Verflare chain)"), save);

            config.RedMage.MeleeComboMinMana = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.RedMage.MeleeComboMinMana, "Melee Min Mana"),
                config.RedMage.MeleeComboMinMana, 50, 100,
                Loc.T(LocalizedStrings.RedMage.MeleeComboMinManaDesc, "Minimum mana to enter melee combo"), save, v => config.RedMage.MeleeComboMinMana = v);

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

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.RedMage.EnableEmbolden, "Enable Embolden"),
                () => config.RedMage.EnableEmbolden,
                v => config.RedMage.EnableEmbolden = v,
                null, save, actionId: RDMActions.Embolden.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.RedMage.EnableManafication, "Enable Manafication"),
                () => config.RedMage.EnableManafication,
                v => config.RedMage.EnableManafication = v,
                null, save, actionId: RDMActions.Manafication.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.RedMage.EnableViceOfThorns, "Enable Vice of Thorns"),
                () => config.RedMage.EnableViceOfThorns,
                v => config.RedMage.EnableViceOfThorns = v,
                null, save, actionId: RDMActions.ViceOfThorns.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.RedMage.EnablePrefulgence, "Enable Prefulgence"),
                () => config.RedMage.EnablePrefulgence,
                v => config.RedMage.EnablePrefulgence = v,
                null, save, actionId: RDMActions.Prefulgence.ActionId);

            config.RedMage.EmboldenHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.RedMage.EmboldenHoldTime, "Embolden Hold Time"),
                config.RedMage.EmboldenHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.RedMage.EmboldenHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save, v => config.RedMage.EmboldenHoldTime = v);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawMovementSection()
    {
        if (ConfigUIHelpers.SectionHeader("Movement / Gap Closers", "RDM", false))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled("Controls Corps-a-corps and Engagement/Displacement usage.");
            ImGui.TextDisabled("Disable these if you don't want the rotation to dash into enemies.");

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                "Use Corps-a-corps",
                () => config.RedMage.EnableCorpsACorps,
                v => config.RedMage.EnableCorpsACorps = v,
                null, save, actionId: RDMActions.CorpsACorps.ActionId);

            ConfigUIHelpers.Toggle(
                "Use Engagement / Displacement",
                () => config.RedMage.EnableEngagement,
                v => config.RedMage.EnableEngagement = v,
                null, save, actionId: RDMActions.Engagement.ActionId);

            ConfigUIHelpers.Spacing();

            var hpPercent = config.RedMage.MeleeDashMinHpPercent * 100f;
            if (ImGui.SliderFloat("Dash Min HP %", ref hpPercent, 0f, 100f, "%.0f%%"))
            {
                config.RedMage.MeleeDashMinHpPercent = hpPercent / 100f;
                save();
            }
            ImGui.TextDisabled("Corps-a-corps and Engagement won't fire below this HP threshold.");
            ImGui.TextDisabled("Prevents dashing into boss mechanics when you're already hurt.");

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawRoleActionsSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.RedMage.RoleActionsSection, "Role Actions"), "RDM", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.RedMage.EnableAddle, "Enable Addle"),
                () => config.RedMage.EnableAddle,
                v => config.RedMage.EnableAddle = v,
                null, save,
                actionId: RoleActions.Addle.ActionId);

            ConfigUIHelpers.EndIndent();
        }
    }
}
