using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Data;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders the Dragoon (Zeus) settings section.
/// </summary>
public sealed class DragoonSection
{
    private readonly Configuration config;
    private readonly Action save;

    public DragoonSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Dragoon", "Zeus", ConfigUIHelpers.DragoonColor);

        DrawDamageSection();
        DrawBuffSection();
        DrawBurstSection();
        DrawPositionalSection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dragoon.DamageSection, "Damage"), "DRG"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dragoon.EnableJumps, "Enable Jumps"),
                () => config.Dragoon.EnableJumps,
                v => config.Dragoon.EnableJumps = v,
                Loc.T(LocalizedStrings.Dragoon.EnableJumpsDesc, "Use Jump/High Jump on cooldown"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dragoon.EnableSpineshatterDive, "Enable Spineshatter Dive"),
                () => config.Dragoon.EnableSpineshatterDive,
                v => config.Dragoon.EnableSpineshatterDive = v,
                Loc.T(LocalizedStrings.Dragoon.EnableSpineshatterDiveDesc, "Use Spineshatter Dive on cooldown"), save,
                actionId: DRGActions.SpineshatterDive.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dragoon.EnableStardiver, "Enable Stardiver"),
                () => config.Dragoon.EnableStardiver,
                v => config.Dragoon.EnableStardiver = v,
                null, save,
                actionId: DRGActions.Stardiver.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dragoon.EnableGeirskogul, "Enable Geirskogul"),
                () => config.Dragoon.EnableGeirskogul,
                v => config.Dragoon.EnableGeirskogul = v,
                null, save,
                actionId: DRGActions.Geirskogul.ActionId);

            ConfigUIHelpers.Spacing();

            config.Dragoon.GeirskogulMinEyes = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Dragoon.GeirskogulMinEyes, "Geirskogul Min Eyes"),
                config.Dragoon.GeirskogulMinEyes, 0, 2,
                Loc.T(LocalizedStrings.Dragoon.GeirskogulMinEyesDesc, "0 = use immediately, 2 = wait for full gauge"), save);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dragoon.EnableAoERotation, "Enable AoE Rotation"),
                () => config.Dragoon.EnableAoERotation,
                v => config.Dragoon.EnableAoERotation = v,
                Loc.T(LocalizedStrings.Dragoon.EnableAoERotationDesc, "Switch to AoE combo at 3+ enemies."), save);

            if (config.Dragoon.EnableAoERotation)
            {
                config.Dragoon.AoEMinTargets = ConfigUIHelpers.IntSlider(
                    Loc.T(LocalizedStrings.Dragoon.AoEMinTargets, "AoE Min Targets"),
                    config.Dragoon.AoEMinTargets, 2, 8,
                    Loc.T(LocalizedStrings.Dragoon.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBuffSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dragoon.BuffSection, "Buffs"), "DRG"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dragoon.EnableLanceCharge, "Enable Lance Charge"),
                () => config.Dragoon.EnableLanceCharge,
                v => config.Dragoon.EnableLanceCharge = v,
                null, save,
                actionId: DRGActions.LanceCharge.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dragoon.EnableBattleLitany, "Enable Battle Litany"),
                () => config.Dragoon.EnableBattleLitany,
                v => config.Dragoon.EnableBattleLitany = v,
                null, save,
                actionId: DRGActions.BattleLitany.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dragoon.EnableLifeSurge, "Enable Life Surge"),
                () => config.Dragoon.EnableLifeSurge,
                v => config.Dragoon.EnableLifeSurge = v,
                null, save,
                actionId: DRGActions.LifeSurge.ActionId);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dragoon.BurstSection, "Burst Windows"), "DRG"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dragoon.HoldJumpsForBurst, "Hold Jumps for Burst"),
                () => config.Dragoon.HoldJumpsForBurst,
                v => config.Dragoon.HoldJumpsForBurst = v,
                Loc.T(LocalizedStrings.Dragoon.HoldJumpsForBurstDesc, "Save jumps for Lance Charge windows"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dragoon.AlignWithParty, "Align Battle Litany with Party"),
                () => config.Dragoon.AlignBattleLitanyWithParty,
                v => config.Dragoon.AlignBattleLitanyWithParty = v,
                Loc.T(LocalizedStrings.Dragoon.AlignWithPartyDesc, "Coordinate with party burst windows"), save);

            config.Dragoon.BattleLitanyHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Dragoon.BattleLitanyHoldTime, "Battle Litany Hold Time"),
                config.Dragoon.BattleLitanyHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Dragoon.BattleLitanyHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawPositionalSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dragoon.PositionalSection, "Positionals"), "DRG", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dragoon.EnforcePositionals, "Enforce Positionals"),
                () => config.Dragoon.EnforcePositionals,
                v => config.Dragoon.EnforcePositionals = v,
                Loc.T(LocalizedStrings.Dragoon.EnforcePositionalsDesc, "Only use positional actions when in correct position"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dragoon.AllowPositionalLoss, "Allow Positional Loss"),
                () => config.Dragoon.AllowPositionalLoss,
                v => config.Dragoon.AllowPositionalLoss = v,
                Loc.T(LocalizedStrings.Dragoon.AllowPositionalLossDesc, "Continue rotation even if positionals will miss"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
