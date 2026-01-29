using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
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

            var enableJumps = config.Dragoon.EnableJumps;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dragoon.EnableJumps, "Enable Jumps"), ref enableJumps,
                Loc.T(LocalizedStrings.Dragoon.EnableJumpsDesc, "Use Jump/High Jump on cooldown"), save))
            {
                config.Dragoon.EnableJumps = enableJumps;
            }

            var enableStardiver = config.Dragoon.EnableStardiver;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dragoon.EnableStardiver, "Enable Stardiver"), ref enableStardiver,
                Loc.T(LocalizedStrings.Dragoon.EnableStardiverDesc, "Use Stardiver during Life of the Dragon"), save))
            {
                config.Dragoon.EnableStardiver = enableStardiver;
            }

            var enableGeirskogul = config.Dragoon.EnableGeirskogul;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dragoon.EnableGeirskogul, "Enable Geirskogul"), ref enableGeirskogul,
                Loc.T(LocalizedStrings.Dragoon.EnableGeirskogulDesc, "Use Geirskogul to enter Life of the Dragon"), save))
            {
                config.Dragoon.EnableGeirskogul = enableGeirskogul;
            }

            ConfigUIHelpers.Spacing();

            config.Dragoon.GeirskogulMinEyes = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Dragoon.GeirskogulMinEyes, "Geirskogul Min Eyes"),
                config.Dragoon.GeirskogulMinEyes, 0, 2,
                Loc.T(LocalizedStrings.Dragoon.GeirskogulMinEyesDesc, "0 = use immediately, 2 = wait for full gauge"), save);

            ConfigUIHelpers.Spacing();

            config.Dragoon.AoEMinTargets = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Dragoon.AoEMinTargets, "AoE Min Targets"),
                config.Dragoon.AoEMinTargets, 2, 8,
                Loc.T(LocalizedStrings.Dragoon.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBuffSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dragoon.BuffSection, "Buffs"), "DRG"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableLanceCharge = config.Dragoon.EnableLanceCharge;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dragoon.EnableLanceCharge, "Enable Lance Charge"), ref enableLanceCharge,
                Loc.T(LocalizedStrings.Dragoon.EnableLanceChargeDesc, "Use Lance Charge for damage buff"), save))
            {
                config.Dragoon.EnableLanceCharge = enableLanceCharge;
            }

            var enableBattleLitany = config.Dragoon.EnableBattleLitany;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dragoon.EnableBattleLitany, "Enable Battle Litany"), ref enableBattleLitany,
                Loc.T(LocalizedStrings.Dragoon.EnableBattleLitanyDesc, "Use Battle Litany (party crit buff)"), save))
            {
                config.Dragoon.EnableBattleLitany = enableBattleLitany;
            }

            var enableLifeSurge = config.Dragoon.EnableLifeSurge;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dragoon.EnableLifeSurge, "Enable Life Surge"), ref enableLifeSurge,
                Loc.T(LocalizedStrings.Dragoon.EnableLifeSurgeDesc, "Use Life Surge for guaranteed crits"), save))
            {
                config.Dragoon.EnableLifeSurge = enableLifeSurge;
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dragoon.BurstSection, "Burst Windows"), "DRG"))
        {
            ConfigUIHelpers.BeginIndent();

            var holdJumpsForBurst = config.Dragoon.HoldJumpsForBurst;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dragoon.HoldJumpsForBurst, "Hold Jumps for Burst"), ref holdJumpsForBurst,
                Loc.T(LocalizedStrings.Dragoon.HoldJumpsForBurstDesc, "Save jumps for Lance Charge windows"), save))
            {
                config.Dragoon.HoldJumpsForBurst = holdJumpsForBurst;
            }

            var alignWithParty = config.Dragoon.AlignBattleLitanyWithParty;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dragoon.AlignWithParty, "Align Battle Litany with Party"), ref alignWithParty,
                Loc.T(LocalizedStrings.Dragoon.AlignWithPartyDesc, "Coordinate with party burst windows"), save))
            {
                config.Dragoon.AlignBattleLitanyWithParty = alignWithParty;
            }

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

            var enforcePositionals = config.Dragoon.EnforcePositionals;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dragoon.EnforcePositionals, "Enforce Positionals"), ref enforcePositionals,
                Loc.T(LocalizedStrings.Dragoon.EnforcePositionalsDesc, "Only use positional actions when in correct position"), save))
            {
                config.Dragoon.EnforcePositionals = enforcePositionals;
            }

            var allowPositionalLoss = config.Dragoon.AllowPositionalLoss;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dragoon.AllowPositionalLoss, "Allow Positional Loss"), ref allowPositionalLoss,
                Loc.T(LocalizedStrings.Dragoon.AllowPositionalLossDesc, "Continue rotation even if positionals will miss"), save))
            {
                config.Dragoon.AllowPositionalLoss = allowPositionalLoss;
            }

            ConfigUIHelpers.EndIndent();
        }
    }
}
