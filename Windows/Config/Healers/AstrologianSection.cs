using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Data;
using Olympus.Localization;

namespace Olympus.Windows.Config.Healers;

/// <summary>
/// Renders the Astrologian (Astraea) settings section.
/// </summary>
public sealed class AstrologianSection
{
    private readonly Configuration config;
    private readonly Action save;

    public AstrologianSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Astrologian", "Astraea", ConfigUIHelpers.AstrologianColor);

        DrawHealingSection();
        DrawEarthlyStarSection();
        DrawHoroscopeSection();
        DrawMacrocosmosSection();
        DrawNeutralSectSection();
        DrawCardSection();
        DrawSynastrySection();
        DrawLightspeedSection();
        DrawDamageSection();
    }

    private void DrawHealingSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Astrologian.HealingSection, "Healing"), "AST"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.GcdHeals, "GCD Heals:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableBenefic, "Enable Benefic"), () => config.Astrologian.EnableBenefic, v => config.Astrologian.EnableBenefic = v, null, save,
                actionId: ASTActions.Benefic.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableBeneficII, "Enable Benefic II"), () => config.Astrologian.EnableBeneficII, v => config.Astrologian.EnableBeneficII = v, null, save,
                actionId: ASTActions.BeneficII.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableAspectedBenefic, "Enable Aspected Benefic"), () => config.Astrologian.EnableAspectedBenefic, v => config.Astrologian.EnableAspectedBenefic = v,
                null, save, actionId: ASTActions.AspectedBenefic.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.AoEHeals, "AoE Heals:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableHelios, "Enable Helios"), () => config.Astrologian.EnableHelios, v => config.Astrologian.EnableHelios = v, null, save,
                actionId: ASTActions.Helios.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableAspectedHelios, "Enable Aspected Helios"), () => config.Astrologian.EnableAspectedHelios, v => config.Astrologian.EnableAspectedHelios = v,
                null, save, actionId: ASTActions.AspectedHelios.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.OgcdHeals, "oGCD Heals:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableEssentialDignity, "Enable Essential Dignity"), () => config.Astrologian.EnableEssentialDignity, v => config.Astrologian.EnableEssentialDignity = v,
                null, save, actionId: ASTActions.EssentialDignity.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableCelestialIntersection, "Enable Celestial Intersection"), () => config.Astrologian.EnableCelestialIntersection, v => config.Astrologian.EnableCelestialIntersection = v,
                null, save, actionId: ASTActions.CelestialIntersection.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableCelestialOpposition, "Enable Celestial Opposition"), () => config.Astrologian.EnableCelestialOpposition, v => config.Astrologian.EnableCelestialOpposition = v,
                null, save, actionId: ASTActions.CelestialOpposition.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableExaltation, "Enable Exaltation"), () => config.Astrologian.EnableExaltation, v => config.Astrologian.EnableExaltation = v,
                null, save, actionId: ASTActions.Exaltation.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.SingleTargetThresholds, "Single-Target Thresholds:"));

            config.Astrologian.BeneficThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Astrologian.BeneficThreshold, "Benefic Threshold"),
                config.Astrologian.BeneficThreshold, 20f, 80f, null, save);
            config.Astrologian.BeneficIIThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Astrologian.BeneficIIThreshold, "Benefic II Threshold"),
                config.Astrologian.BeneficIIThreshold, 30f, 85f, null, save);
            config.Astrologian.AspectedBeneficThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Astrologian.AspectedBeneficThreshold, "Aspected Benefic Threshold"),
                config.Astrologian.AspectedBeneficThreshold, 50f, 95f, null, save);
            config.Astrologian.EssentialDignityThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Astrologian.EssentialDignityThreshold, "Essential Dignity Threshold"),
                config.Astrologian.EssentialDignityThreshold, 20f, 60f, Loc.T(LocalizedStrings.Astrologian.EssentialDignityThresholdDesc, "Lower = more healing potency (scales with missing HP)."), save);
            config.Astrologian.CelestialIntersectionThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Astrologian.CelestialIntersectionThreshold, "Celestial Intersection Threshold"),
                config.Astrologian.CelestialIntersectionThreshold, 40f, 90f, null, save);
            config.Astrologian.ExaltationThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Astrologian.ExaltationThreshold, "Exaltation Threshold"),
                config.Astrologian.ExaltationThreshold, 50f, 95f, null, save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.AoESettings, "AoE Settings:"));

            config.Astrologian.AoEHealThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Astrologian.AoEHpThreshold, "AoE HP Threshold"),
                config.Astrologian.AoEHealThreshold, 50f, 90f, null, save);

            config.Astrologian.AoEHealMinTargets = ConfigUIHelpers.IntSlider(Loc.T(LocalizedStrings.Astrologian.AoEMinTargets, "AoE Min Targets"),
                config.Astrologian.AoEHealMinTargets, 1, 8,
                Loc.T(LocalizedStrings.Astrologian.AoEMinTargetsDesc, "Minimum party members below threshold for AoE heals."), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawEarthlyStarSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Astrologian.EarthlyStarSection, "Earthly Star"), "AST"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableEarthlyStar, "Enable Earthly Star"), () => config.Astrologian.EnableEarthlyStar, v => config.Astrologian.EnableEarthlyStar = v,
                null, save, actionId: ASTActions.EarthlyStar.ActionId);

            ConfigUIHelpers.BeginDisabledGroup(!config.Astrologian.EnableEarthlyStar);

            var placementNames = Enum.GetNames<EarthlyStarPlacementStrategy>();
            var currentPlacement = (int)config.Astrologian.StarPlacement;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo(Loc.T(LocalizedStrings.Astrologian.Placement, "Placement"), ref currentPlacement, placementNames, placementNames.Length))
            {
                config.Astrologian.StarPlacement = (EarthlyStarPlacementStrategy)currentPlacement;
                save();
            }

            var placementDesc = config.Astrologian.StarPlacement switch
            {
                EarthlyStarPlacementStrategy.OnMainTank => Loc.T(LocalizedStrings.Astrologian.PlacementOnMainTank, "Place on main tank"),
                EarthlyStarPlacementStrategy.OnSelf => Loc.T(LocalizedStrings.Astrologian.PlacementOnSelf, "Place on self"),
                EarthlyStarPlacementStrategy.Manual => Loc.T(LocalizedStrings.Astrologian.PlacementManual, "Manual control only"),
                _ => ""
            };
            ImGui.TextDisabled(placementDesc);

            ConfigUIHelpers.Spacing();

            config.Astrologian.EarthlyStarDetonateThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Astrologian.DetonateThreshold, "Detonate Threshold"),
                config.Astrologian.EarthlyStarDetonateThreshold, 40f, 85f, Loc.T(LocalizedStrings.Astrologian.DetonateThresholdDesc, "Party average HP to trigger detonation."), save);

            config.Astrologian.EarthlyStarMinTargets = ConfigUIHelpers.IntSlider(Loc.T(LocalizedStrings.Astrologian.MinTargetsInRange, "Min Targets in Range"),
                config.Astrologian.EarthlyStarMinTargets, 1, 8, null, save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.WaitForGiantDominance, "Wait for Giant Dominance"), () => config.Astrologian.WaitForGiantDominance, v => config.Astrologian.WaitForGiantDominance = v,
                Loc.T(LocalizedStrings.Astrologian.WaitForGiantDominanceDesc, "Wait 10s for star to mature before detonating."), save);

            if (config.Astrologian.WaitForGiantDominance)
            {
                config.Astrologian.EarthlyStarEmergencyThreshold = ConfigUIHelpers.ThresholdSliderSmall(Loc.T(LocalizedStrings.Astrologian.EmergencyThreshold, "Emergency Threshold"),
                    config.Astrologian.EarthlyStarEmergencyThreshold, 20f, 60f,
                    Loc.T(LocalizedStrings.Astrologian.EmergencyThresholdDesc, "Detonate immature star if HP drops below this."), save);
            }

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawHoroscopeSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Astrologian.HoroscopeSection, "Horoscope"), "AST"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableHoroscope, "Enable Horoscope"), () => config.Astrologian.EnableHoroscope, v => config.Astrologian.EnableHoroscope = v,
                null, save, actionId: ASTActions.Horoscope.ActionId);

            ConfigUIHelpers.BeginDisabledGroup(!config.Astrologian.EnableHoroscope);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.AutoCastHoroscope, "Auto-Cast Horoscope"), () => config.Astrologian.AutoCastHoroscope, v => config.Astrologian.AutoCastHoroscope = v,
                Loc.T(LocalizedStrings.Astrologian.AutoCastHoroscopeDesc, "Automatically prepare Horoscope."), save);

            config.Astrologian.HoroscopeThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Astrologian.HoroscopeThreshold, "Detonate Threshold"),
                config.Astrologian.HoroscopeThreshold, 50f, 90f, Loc.T(LocalizedStrings.Astrologian.HoroscopeThresholdDesc, "Party HP threshold to detonate."), save);

            config.Astrologian.HoroscopeMinTargets = ConfigUIHelpers.IntSlider(Loc.T(LocalizedStrings.Astrologian.HoroscopeMinTargets, "Min Injured Targets"),
                config.Astrologian.HoroscopeMinTargets, 1, 8, null, save);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawMacrocosmosSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Astrologian.MacrocosmosSection, "Macrocosmos"), "AST"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableMacrocosmos, "Enable Macrocosmos"), () => config.Astrologian.EnableMacrocosmos, v => config.Astrologian.EnableMacrocosmos = v,
                null, save, actionId: ASTActions.Macrocosmos.ActionId);

            ConfigUIHelpers.BeginDisabledGroup(!config.Astrologian.EnableMacrocosmos);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.AutoUseMacrocosmos, "Auto-Use Macrocosmos"), () => config.Astrologian.AutoUseMacrocosmos, v => config.Astrologian.AutoUseMacrocosmos = v, null, save);

            config.Astrologian.MacrocosmosThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Astrologian.MacrocosmosThreshold, "Party HP Threshold"),
                config.Astrologian.MacrocosmosThreshold, 60f, 95f, Loc.T(LocalizedStrings.Astrologian.MacrocosmosThresholdDesc, "Use when party average HP is below this."), save);

            config.Astrologian.MacrocosmosMinTargets = ConfigUIHelpers.IntSlider(Loc.T(LocalizedStrings.Astrologian.MacrocosmosMinTargets, "Min Targets"),
                config.Astrologian.MacrocosmosMinTargets, 1, 8, null, save);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawNeutralSectSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Astrologian.NeutralSectSection, "Neutral Sect"), "AST"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableNeutralSect, "Enable Neutral Sect"), () => config.Astrologian.EnableNeutralSect, v => config.Astrologian.EnableNeutralSect = v,
                null, save, actionId: ASTActions.NeutralSect.ActionId);

            ConfigUIHelpers.BeginDisabledGroup(!config.Astrologian.EnableNeutralSect);

            var strategyNames = Enum.GetNames<NeutralSectUsageStrategy>();
            var currentStrategy = (int)config.Astrologian.NeutralSectStrategy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo(Loc.T(LocalizedStrings.Astrologian.Strategy, "Strategy"), ref currentStrategy, strategyNames, strategyNames.Length))
            {
                config.Astrologian.NeutralSectStrategy = (NeutralSectUsageStrategy)currentStrategy;
                save();
            }

            var strategyDesc = config.Astrologian.NeutralSectStrategy switch
            {
                NeutralSectUsageStrategy.OnCooldown => Loc.T(LocalizedStrings.Astrologian.StrategyOnCooldown, "Use on cooldown"),
                NeutralSectUsageStrategy.SaveForDamage => Loc.T(LocalizedStrings.Astrologian.StrategySaveForDamage, "Save for high damage phases"),
                NeutralSectUsageStrategy.Manual => Loc.T(LocalizedStrings.Astrologian.StrategyManual, "Manual control only"),
                _ => ""
            };
            ImGui.TextDisabled(strategyDesc);

            config.Astrologian.NeutralSectThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Astrologian.NeutralSectThreshold, "HP Threshold"),
                config.Astrologian.NeutralSectThreshold, 40f, 85f, Loc.T(LocalizedStrings.Astrologian.NeutralSectThresholdDesc, "Party average HP to trigger."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableSunSign, "Enable Sun Sign"), () => config.Astrologian.EnableSunSign, v => config.Astrologian.EnableSunSign = v,
                null, save, actionId: ASTActions.SunSign.ActionId);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawCardSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Astrologian.CardsSection, "Cards"), "AST"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableCards, "Enable Cards"), () => config.Astrologian.EnableCards, v => config.Astrologian.EnableCards = v,
                Loc.T(LocalizedStrings.Astrologian.EnableCardsDesc, "Automatically draw and play cards."), save);

            ConfigUIHelpers.BeginDisabledGroup(!config.Astrologian.EnableCards);

            var strategyNames = Enum.GetNames<CardPlayStrategy>();
            var currentStrategy = (int)config.Astrologian.CardStrategy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo(Loc.T(LocalizedStrings.Astrologian.CardStrategy, "Card Strategy"), ref currentStrategy, strategyNames, strategyNames.Length))
            {
                config.Astrologian.CardStrategy = (CardPlayStrategy)currentStrategy;
                save();
            }

            var strategyDesc = config.Astrologian.CardStrategy switch
            {
                CardPlayStrategy.DpsFocused => Loc.T(LocalizedStrings.Astrologian.CardStrategyDpsFocused, "Target highest-contributing DPS"),
                CardPlayStrategy.Balanced => Loc.T(LocalizedStrings.Astrologian.CardStrategyBalanced, "Balance between DPS and support"),
                CardPlayStrategy.SafetyFocused => Loc.T(LocalizedStrings.Astrologian.CardStrategySafetyFocused, "Prioritize safety over damage"),
                _ => ""
            };
            ImGui.TextDisabled(strategyDesc);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.MinorArcanaLabel, "Minor Arcana:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableMinorArcana, "Enable Minor Arcana"), () => config.Astrologian.EnableMinorArcana, v => config.Astrologian.EnableMinorArcana = v, null, save);

            if (config.Astrologian.EnableMinorArcana)
            {
                var minorNames = Enum.GetNames<MinorArcanaUsageStrategy>();
                var currentMinor = (int)config.Astrologian.MinorArcanaStrategy;
                ImGui.SetNextItemWidth(150);
                if (ImGui.Combo(Loc.T(LocalizedStrings.Astrologian.MinorArcanaStrategy, "Minor Arcana Strategy"), ref currentMinor, minorNames, minorNames.Length))
                {
                    config.Astrologian.MinorArcanaStrategy = (MinorArcanaUsageStrategy)currentMinor;
                    save();
                }

                config.Astrologian.LadyOfCrownsThreshold = ConfigUIHelpers.ThresholdSliderSmall(Loc.T(LocalizedStrings.Astrologian.LadyOfCrownsThreshold, "Lady of Crowns HP"),
                    config.Astrologian.LadyOfCrownsThreshold, 30f, 80f,
                    Loc.T(LocalizedStrings.Astrologian.LadyOfCrownsThresholdDesc, "HP threshold to use Lady of Crowns heal."), save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.BurstAbilities, "Burst Abilities:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableDivination, "Enable Divination"), () => config.Astrologian.EnableDivination, v => config.Astrologian.EnableDivination = v,
                null, save, actionId: ASTActions.Divination.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableAstrodyne, "Enable Astrodyne"), () => config.Astrologian.EnableAstrodyne, v => config.Astrologian.EnableAstrodyne = v, null, save,
                actionId: ASTActions.Astrodyne.ActionId);

            if (config.Astrologian.EnableAstrodyne)
            {
                config.Astrologian.AstrodyneMinSeals = ConfigUIHelpers.IntSliderSmall(Loc.T(LocalizedStrings.Astrologian.AstrodyneMinSeals, "Min Unique Seals"),
                    config.Astrologian.AstrodyneMinSeals, 1, 3,
                    Loc.T(LocalizedStrings.Astrologian.AstrodyneMinSealsDesc, "Wait for this many unique seals (more seals = better buffs)."), save);
            }

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableOracle, "Enable Oracle"), () => config.Astrologian.EnableOracle, v => config.Astrologian.EnableOracle = v,
                null, save, actionId: ASTActions.Oracle.ActionId);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawSynastrySection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Astrologian.SynastrySection, "Synastry"), "AST", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableSynastry, "Enable Synastry"), () => config.Astrologian.EnableSynastry, v => config.Astrologian.EnableSynastry = v,
                null, save, actionId: ASTActions.Synastry.ActionId);

            ConfigUIHelpers.BeginDisabledGroup(!config.Astrologian.EnableSynastry);

            config.Astrologian.SynastryThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Astrologian.SynastryThreshold, "Synastry Threshold"),
                config.Astrologian.SynastryThreshold, 30f, 70f, Loc.T(LocalizedStrings.Astrologian.SynastryThresholdDesc, "HP threshold to apply Synastry."), save);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawLightspeedSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Astrologian.LightspeedSection, "Lightspeed"), "AST", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableLightspeed, "Enable Lightspeed"), () => config.Astrologian.EnableLightspeed, v => config.Astrologian.EnableLightspeed = v,
                null, save, actionId: ASTActions.Lightspeed.ActionId);

            ConfigUIHelpers.BeginDisabledGroup(!config.Astrologian.EnableLightspeed);

            var strategyNames = Enum.GetNames<LightspeedUsageStrategy>();
            var currentStrategy = (int)config.Astrologian.LightspeedStrategy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo(Loc.T(LocalizedStrings.Astrologian.Strategy, "Strategy"), ref currentStrategy, strategyNames, strategyNames.Length))
            {
                config.Astrologian.LightspeedStrategy = (LightspeedUsageStrategy)currentStrategy;
                save();
            }

            var strategyDesc = config.Astrologian.LightspeedStrategy switch
            {
                LightspeedUsageStrategy.OnCooldown => Loc.T(LocalizedStrings.Astrologian.LightspeedStrategyOnCooldown, "Use on cooldown"),
                LightspeedUsageStrategy.SaveForMovement => Loc.T(LocalizedStrings.Astrologian.LightspeedStrategySaveForMovement, "Save for movement-heavy phases"),
                LightspeedUsageStrategy.SaveForRaise => Loc.T(LocalizedStrings.Astrologian.LightspeedStrategySaveForRaise, "Save for raising dead party members"),
                _ => ""
            };
            ImGui.TextDisabled(strategyDesc);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Astrologian.DamageSection, "Damage"), "AST"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.SingleTargetDamage, "Single-Target:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableMalefic, "Enable Malefic"), () => config.Astrologian.EnableSingleTargetDamage, v => config.Astrologian.EnableSingleTargetDamage = v,
                Loc.T(LocalizedStrings.Astrologian.MaleficDesc, "Casted single-target damage spell."), save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.DotLabel, "DoT:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableCombust, "Enable Combust"), () => config.Astrologian.EnableDot, v => config.Astrologian.EnableDot = v, null, save);

            if (config.Astrologian.EnableDot)
            {
                config.Astrologian.DotRefreshThreshold = ConfigUIHelpers.FloatSlider(Loc.T(LocalizedStrings.Astrologian.DotRefreshThreshold, "DoT Refresh (sec)"),
                    config.Astrologian.DotRefreshThreshold, 0f, 10f, "%.1f",
                    Loc.T(LocalizedStrings.Astrologian.DotRefreshThresholdDesc, "Refresh DoT when this many seconds remain."), save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.AoEDamage, "AoE:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableGravity, "Enable Gravity"), () => config.Astrologian.EnableAoEDamage, v => config.Astrologian.EnableAoEDamage = v, null, save);

            if (config.Astrologian.EnableAoEDamage)
            {
                config.Astrologian.AoEDamageMinTargets = ConfigUIHelpers.IntSlider(Loc.T(LocalizedStrings.Astrologian.AoEMinEnemies, "Min Enemies"),
                    config.Astrologian.AoEDamageMinTargets, 1, 10, null, save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.MpManagement, "MP Management:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableLucidDreaming, "Enable Lucid Dreaming"), () => config.Astrologian.EnableLucidDreaming, v => config.Astrologian.EnableLucidDreaming = v, null, save);

            if (config.Astrologian.EnableLucidDreaming)
            {
                config.Astrologian.LucidDreamingThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Astrologian.LucidMpThreshold, "Lucid MP Threshold"),
                    config.Astrologian.LucidDreamingThreshold, 40f, 90f, null, save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.CollectiveUnconsciousLabel, "Collective Unconscious:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Astrologian.EnableCollectiveUnconscious, "Enable Collective Unconscious"), () => config.Astrologian.EnableCollectiveUnconscious, v => config.Astrologian.EnableCollectiveUnconscious = v, null, save,
                actionId: ASTActions.CollectiveUnconscious.ActionId);
            ConfigUIHelpers.WarningText(Loc.T(LocalizedStrings.Astrologian.CollectiveUnconsciousWarning, "Channeled ability - may interrupt other actions."));

            if (config.Astrologian.EnableCollectiveUnconscious)
            {
                config.Astrologian.CollectiveUnconsciousThreshold = ConfigUIHelpers.ThresholdSliderSmall(Loc.T(LocalizedStrings.Astrologian.CollectiveUnconsciousThreshold, "CU HP Threshold"),
                    config.Astrologian.CollectiveUnconsciousThreshold, 30f, 70f, null, save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }
}
