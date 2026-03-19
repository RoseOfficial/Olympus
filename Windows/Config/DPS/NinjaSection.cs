using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Data;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders the Ninja (Hermes) settings section.
/// </summary>
public sealed class NinjaSection
{
    private readonly Configuration config;
    private readonly Action save;

    public NinjaSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Ninja", "Hermes", ConfigUIHelpers.NinjaColor);

        DrawDamageSection();
        DrawNinkiSection();
        DrawMudraSection();
        DrawBurstSection();
        DrawPositionalSection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Ninja.DamageSection, "Damage"), "NIN"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableNinjutsu, "Enable Ninjutsu"),
                () => config.Ninja.EnableNinjutsu,
                v => config.Ninja.EnableNinjutsu = v,
                Loc.T(LocalizedStrings.Ninja.EnableNinjutsuDesc, "Use mudra combinations for Ninjutsu"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableRaiju, "Enable Raiju"),
                () => config.Ninja.EnableRaiju,
                v => config.Ninja.EnableRaiju = v,
                Loc.T(LocalizedStrings.Ninja.EnableRaijuDesc, "Use Forked/Fleeting Raiju procs"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnablePhantomKamaitachi, "Enable Phantom Kamaitachi"),
                () => config.Ninja.EnablePhantomKamaitachi,
                v => config.Ninja.EnablePhantomKamaitachi = v,
                null, save,
                actionId: NINActions.PhantomKamaitachi.ActionId);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableAoERotation, "Enable AoE Rotation"),
                () => config.Ninja.EnableAoERotation,
                v => config.Ninja.EnableAoERotation = v,
                Loc.T(LocalizedStrings.Ninja.EnableAoERotationDesc, "Switch to AoE combo at 3+ enemies."), save);

            if (config.Ninja.EnableAoERotation)
            {
                config.Ninja.AoEMinTargets = ConfigUIHelpers.IntSlider(
                    Loc.T(LocalizedStrings.Ninja.AoEMinTargets, "AoE Min Targets"),
                    config.Ninja.AoEMinTargets, 2, 8,
                    Loc.T(LocalizedStrings.Ninja.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawNinkiSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Ninja.NinkiSection, "Ninki Gauge"), "NIN"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableBhavacakra, "Enable Bhavacakra"),
                () => config.Ninja.EnableBhavacakra,
                v => config.Ninja.EnableBhavacakra = v,
                null, save,
                actionId: NINActions.Bhavacakra.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableHellfrogMedium, "Enable Hellfrog Medium"),
                () => config.Ninja.EnableHellfrogMedium,
                v => config.Ninja.EnableHellfrogMedium = v,
                null, save,
                actionId: NINActions.HellfrogMedium.ActionId);

            config.Ninja.NinkiMinGauge = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Ninja.NinkiMinGauge, "Ninki Min Gauge"),
                config.Ninja.NinkiMinGauge, 50, 100,
                Loc.T(LocalizedStrings.Ninja.NinkiMinGaugeDesc, "Minimum Ninki to use spenders"), save);

            config.Ninja.NinkiOvercapThreshold = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Ninja.NinkiOvercapThreshold, "Ninki Overcap Threshold"),
                config.Ninja.NinkiOvercapThreshold, 50, 100,
                Loc.T(LocalizedStrings.Ninja.NinkiOvercapThresholdDesc, "Dump Ninki above this to avoid overcap"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawMudraSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Ninja.MudraSection, "Mudra Settings"), "NIN"))
        {
            ConfigUIHelpers.BeginIndent();

            var priority = config.Ninja.SingleTargetNinjutsuPriority;
            if (ConfigUIHelpers.EnumCombo(Loc.T(LocalizedStrings.Ninja.NinjutsuPriority, "Ninjutsu Priority"), ref priority,
                Loc.T(LocalizedStrings.Ninja.NinjutsuPriorityDesc, "Preferred Ninjutsu for single-target"), save))
            {
                config.Ninja.SingleTargetNinjutsuPriority = priority;
            }

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.UseDotonForAoE, "Use Doton for AoE"),
                () => config.Ninja.UseDotonForAoE,
                v => config.Ninja.UseDotonForAoE = v,
                null, save,
                actionId: NINActions.Doton.ActionId);

            config.Ninja.DotonMinTargets = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Ninja.DotonMinTargets, "Doton Min Targets"),
                config.Ninja.DotonMinTargets, 2, 8,
                Loc.T(LocalizedStrings.Ninja.DotonMinTargetsDesc, "Minimum enemies for Doton placement"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Ninja.BurstSection, "Burst Windows"), "NIN", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableKunaisBane, "Enable Kunai's Bane"),
                () => config.Ninja.EnableKunaisBane,
                v => config.Ninja.EnableKunaisBane = v,
                null, save,
                actionId: NINActions.KunaisBane.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableMug, "Enable Mug / Dokumori"),
                () => config.Ninja.EnableMug,
                v => config.Ninja.EnableMug = v,
                null, save,
                actionId: NINActions.Mug.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableTenriJindo, "Enable Tenri Jindo"),
                () => config.Ninja.EnableTenriJindo,
                v => config.Ninja.EnableTenriJindo = v,
                null, save,
                actionId: NINActions.TenriJindo.ActionId);

            config.Ninja.KunaisBaneHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Ninja.KunaisBaneHoldTime, "Kunai's Bane Hold Time"),
                config.Ninja.KunaisBaneHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Ninja.KunaisBaneHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableKassatsu, "Enable Kassatsu"),
                () => config.Ninja.EnableKassatsu,
                v => config.Ninja.EnableKassatsu = v,
                null, save,
                actionId: NINActions.Kassatsu.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableTenChiJin, "Enable Ten Chi Jin"),
                () => config.Ninja.EnableTenChiJin,
                v => config.Ninja.EnableTenChiJin = v,
                null, save,
                actionId: NINActions.TenChiJin.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableBunshin, "Enable Bunshin"),
                () => config.Ninja.EnableBunshin,
                v => config.Ninja.EnableBunshin = v,
                null, save,
                actionId: NINActions.Bunshin.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableMeisui, "Enable Meisui"),
                () => config.Ninja.EnableMeisui,
                v => config.Ninja.EnableMeisui = v,
                null, save,
                actionId: NINActions.Meisui.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.SaveNinkiForBurst, "Save Ninki for Burst"),
                () => config.Ninja.SaveNinkiForBurst,
                v => config.Ninja.SaveNinkiForBurst = v,
                Loc.T(LocalizedStrings.Ninja.SaveNinkiForBurstDesc, "Hold Ninki spenders for burst windows"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableBurstPooling, "Enable Burst Pooling"),
                () => config.Ninja.EnableBurstPooling,
                v => config.Ninja.EnableBurstPooling = v,
                Loc.T(LocalizedStrings.Ninja.EnableBurstPoolingDesc, "Hold Ninki spenders for party burst windows"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawPositionalSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Ninja.PositionalSection, "Positionals"), "NIN", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnforcePositionals, "Enforce Positionals"),
                () => config.Ninja.EnforcePositionals,
                v => config.Ninja.EnforcePositionals = v,
                Loc.T(LocalizedStrings.Ninja.EnforcePositionalsDesc, "Only use positional actions when in correct position"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.AllowPositionalLoss, "Allow Positional Loss"),
                () => config.Ninja.AllowPositionalLoss,
                v => config.Ninja.AllowPositionalLoss = v,
                Loc.T(LocalizedStrings.Ninja.AllowPositionalLossDesc, "Continue rotation even if positionals will miss"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
