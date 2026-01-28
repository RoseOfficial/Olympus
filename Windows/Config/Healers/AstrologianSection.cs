using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
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

            var enableBenefic = config.Astrologian.EnableBenefic;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableBenefic, "Enable Benefic"), ref enableBenefic))
            {
                config.Astrologian.EnableBenefic = enableBenefic;
                save();
            }

            ImGui.SameLine();
            var enableBeneficII = config.Astrologian.EnableBeneficII;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableBeneficII, "Enable Benefic II"), ref enableBeneficII))
            {
                config.Astrologian.EnableBeneficII = enableBeneficII;
                save();
            }

            var enableAspectedBenefic = config.Astrologian.EnableAspectedBenefic;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableAspectedBenefic, "Enable Aspected Benefic"), ref enableAspectedBenefic))
            {
                config.Astrologian.EnableAspectedBenefic = enableAspectedBenefic;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.AspectedBeneficDesc, "Single-target regen."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.AoEHeals, "AoE Heals:"));

            var enableHelios = config.Astrologian.EnableHelios;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableHelios, "Enable Helios"), ref enableHelios))
            {
                config.Astrologian.EnableHelios = enableHelios;
                save();
            }

            ImGui.SameLine();
            var enableAspectedHelios = config.Astrologian.EnableAspectedHelios;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableAspectedHelios, "Enable Aspected Helios"), ref enableAspectedHelios))
            {
                config.Astrologian.EnableAspectedHelios = enableAspectedHelios;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.AspectedHeliosDesc, "AoE heal + regen. Becomes Helios Conjunction at higher levels."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.OgcdHeals, "oGCD Heals:"));

            var enableED = config.Astrologian.EnableEssentialDignity;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableEssentialDignity, "Enable Essential Dignity"), ref enableED))
            {
                config.Astrologian.EnableEssentialDignity = enableED;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.EssentialDignityDesc, "Potency scales with target's missing HP (400-1100)."));

            var enableCI = config.Astrologian.EnableCelestialIntersection;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableCelestialIntersection, "Enable Celestial Intersection"), ref enableCI))
            {
                config.Astrologian.EnableCelestialIntersection = enableCI;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.CelestialIntersectionDesc, "oGCD heal + shield."));

            var enableCO = config.Astrologian.EnableCelestialOpposition;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableCelestialOpposition, "Enable Celestial Opposition"), ref enableCO))
            {
                config.Astrologian.EnableCelestialOpposition = enableCO;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.CelestialOppositionDesc, "oGCD AoE heal + regen."));

            var enableExaltation = config.Astrologian.EnableExaltation;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableExaltation, "Enable Exaltation"), ref enableExaltation))
            {
                config.Astrologian.EnableExaltation = enableExaltation;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.ExaltationDesc, "Damage reduction + delayed heal."));

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

            var enableStar = config.Astrologian.EnableEarthlyStar;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableEarthlyStar, "Enable Earthly Star"), ref enableStar))
            {
                config.Astrologian.EnableEarthlyStar = enableStar;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.EarthlyStarDesc, "Ground-targeted AoE that matures over 10s."));

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

            var waitForGiant = config.Astrologian.WaitForGiantDominance;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.WaitForGiantDominance, "Wait for Giant Dominance"), ref waitForGiant))
            {
                config.Astrologian.WaitForGiantDominance = waitForGiant;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.WaitForGiantDominanceDesc, "Wait 10s for star to mature before detonating."));

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

            var enableHoroscope = config.Astrologian.EnableHoroscope;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableHoroscope, "Enable Horoscope"), ref enableHoroscope))
            {
                config.Astrologian.EnableHoroscope = enableHoroscope;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.HoroscopeDesc, "Delayed AoE heal that can be detonated manually."));

            ConfigUIHelpers.BeginDisabledGroup(!config.Astrologian.EnableHoroscope);

            var autoCast = config.Astrologian.AutoCastHoroscope;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.AutoCastHoroscope, "Auto-Cast Horoscope"), ref autoCast))
            {
                config.Astrologian.AutoCastHoroscope = autoCast;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.AutoCastHoroscopeDesc, "Automatically prepare Horoscope."));

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

            var enableMacro = config.Astrologian.EnableMacrocosmos;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableMacrocosmos, "Enable Macrocosmos"), ref enableMacro))
            {
                config.Astrologian.EnableMacrocosmos = enableMacro;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.MacrocosmosDesc, "Absorbs damage taken and heals based on it."));

            ConfigUIHelpers.BeginDisabledGroup(!config.Astrologian.EnableMacrocosmos);

            var autoUse = config.Astrologian.AutoUseMacrocosmos;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.AutoUseMacrocosmos, "Auto-Use Macrocosmos"), ref autoUse))
            {
                config.Astrologian.AutoUseMacrocosmos = autoUse;
                save();
            }

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

            var enableNS = config.Astrologian.EnableNeutralSect;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableNeutralSect, "Enable Neutral Sect"), ref enableNS))
            {
                config.Astrologian.EnableNeutralSect = enableNS;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.NeutralSectDesc, "Boosts healing and adds shield to Aspected spells."));

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

            var enableSunSign = config.Astrologian.EnableSunSign;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableSunSign, "Enable Sun Sign"), ref enableSunSign))
            {
                config.Astrologian.EnableSunSign = enableSunSign;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.SunSignDesc, "Level 100 follow-up ability."));

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawCardSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Astrologian.CardsSection, "Cards"), "AST"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableCards = config.Astrologian.EnableCards;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableCards, "Enable Cards"), ref enableCards))
            {
                config.Astrologian.EnableCards = enableCards;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.EnableCardsDesc, "Automatically draw and play cards."));

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

            var enableMinor = config.Astrologian.EnableMinorArcana;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableMinorArcana, "Enable Minor Arcana"), ref enableMinor))
            {
                config.Astrologian.EnableMinorArcana = enableMinor;
                save();
            }

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

            var enableDivination = config.Astrologian.EnableDivination;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableDivination, "Enable Divination"), ref enableDivination))
            {
                config.Astrologian.EnableDivination = enableDivination;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.DivinationDesc, "Party-wide damage buff."));

            var enableAstrodyne = config.Astrologian.EnableAstrodyne;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableAstrodyne, "Enable Astrodyne"), ref enableAstrodyne))
            {
                config.Astrologian.EnableAstrodyne = enableAstrodyne;
                save();
            }

            if (config.Astrologian.EnableAstrodyne)
            {
                config.Astrologian.AstrodyneMinSeals = ConfigUIHelpers.IntSliderSmall(Loc.T(LocalizedStrings.Astrologian.AstrodyneMinSeals, "Min Unique Seals"),
                    config.Astrologian.AstrodyneMinSeals, 1, 3,
                    Loc.T(LocalizedStrings.Astrologian.AstrodyneMinSealsDesc, "Wait for this many unique seals (more seals = better buffs)."), save);
            }

            var enableOracle = config.Astrologian.EnableOracle;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableOracle, "Enable Oracle"), ref enableOracle))
            {
                config.Astrologian.EnableOracle = enableOracle;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.OracleDesc, "Divination follow-up ability."));

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawSynastrySection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Astrologian.SynastrySection, "Synastry"), "AST", false))
        {
            ConfigUIHelpers.BeginIndent();

            var enableSynastry = config.Astrologian.EnableSynastry;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableSynastry, "Enable Synastry"), ref enableSynastry))
            {
                config.Astrologian.EnableSynastry = enableSynastry;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.SynastryDesc, "Mirrors 40% of single-target heals to Synastry target."));

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

            var enableLS = config.Astrologian.EnableLightspeed;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableLightspeed, "Enable Lightspeed"), ref enableLS))
            {
                config.Astrologian.EnableLightspeed = enableLS;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.LightspeedDesc, "Reduces cast times and MP costs."));

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

            var enableST = config.Astrologian.EnableSingleTargetDamage;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableMalefic, "Enable Malefic"), ref enableST))
            {
                config.Astrologian.EnableSingleTargetDamage = enableST;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Astrologian.MaleficDesc, "Casted single-target damage spell."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.DotLabel, "DoT:"));

            var enableDot = config.Astrologian.EnableDot;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableCombust, "Enable Combust"), ref enableDot))
            {
                config.Astrologian.EnableDot = enableDot;
                save();
            }

            if (config.Astrologian.EnableDot)
            {
                config.Astrologian.DotRefreshThreshold = ConfigUIHelpers.FloatSlider(Loc.T(LocalizedStrings.Astrologian.DotRefreshThreshold, "DoT Refresh (sec)"),
                    config.Astrologian.DotRefreshThreshold, 0f, 10f, "%.1f",
                    Loc.T(LocalizedStrings.Astrologian.DotRefreshThresholdDesc, "Refresh DoT when this many seconds remain."), save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.AoEDamage, "AoE:"));

            var enableAoE = config.Astrologian.EnableAoEDamage;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableGravity, "Enable Gravity"), ref enableAoE))
            {
                config.Astrologian.EnableAoEDamage = enableAoE;
                save();
            }

            if (config.Astrologian.EnableAoEDamage)
            {
                config.Astrologian.AoEDamageMinTargets = ConfigUIHelpers.IntSlider(Loc.T(LocalizedStrings.Astrologian.AoEMinEnemies, "Min Enemies"),
                    config.Astrologian.AoEDamageMinTargets, 1, 10, null, save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.MpManagement, "MP Management:"));

            var enableLucid = config.Astrologian.EnableLucidDreaming;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableLucidDreaming, "Enable Lucid Dreaming"), ref enableLucid))
            {
                config.Astrologian.EnableLucidDreaming = enableLucid;
                save();
            }

            if (config.Astrologian.EnableLucidDreaming)
            {
                config.Astrologian.LucidDreamingThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Astrologian.LucidMpThreshold, "Lucid MP Threshold"),
                    config.Astrologian.LucidDreamingThreshold, 40f, 90f, null, save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Astrologian.CollectiveUnconsciousLabel, "Collective Unconscious:"));

            var enableCU = config.Astrologian.EnableCollectiveUnconscious;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Astrologian.EnableCollectiveUnconscious, "Enable Collective Unconscious"), ref enableCU))
            {
                config.Astrologian.EnableCollectiveUnconscious = enableCU;
                save();
            }
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
