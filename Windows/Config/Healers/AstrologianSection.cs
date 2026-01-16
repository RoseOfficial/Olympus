using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;

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
        if (ConfigUIHelpers.SectionHeader("Healing", "AST"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel("GCD Heals:");

            var enableBenefic = config.Astrologian.EnableBenefic;
            if (ImGui.Checkbox("Enable Benefic", ref enableBenefic))
            {
                config.Astrologian.EnableBenefic = enableBenefic;
                save();
            }

            ImGui.SameLine();
            var enableBeneficII = config.Astrologian.EnableBeneficII;
            if (ImGui.Checkbox("Enable Benefic II", ref enableBeneficII))
            {
                config.Astrologian.EnableBeneficII = enableBeneficII;
                save();
            }

            var enableAspectedBenefic = config.Astrologian.EnableAspectedBenefic;
            if (ImGui.Checkbox("Enable Aspected Benefic", ref enableAspectedBenefic))
            {
                config.Astrologian.EnableAspectedBenefic = enableAspectedBenefic;
                save();
            }
            ImGui.TextDisabled("Single-target regen.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("AoE Heals:");

            var enableHelios = config.Astrologian.EnableHelios;
            if (ImGui.Checkbox("Enable Helios", ref enableHelios))
            {
                config.Astrologian.EnableHelios = enableHelios;
                save();
            }

            ImGui.SameLine();
            var enableAspectedHelios = config.Astrologian.EnableAspectedHelios;
            if (ImGui.Checkbox("Enable Aspected Helios", ref enableAspectedHelios))
            {
                config.Astrologian.EnableAspectedHelios = enableAspectedHelios;
                save();
            }
            ImGui.TextDisabled("AoE heal + regen. Becomes Helios Conjunction at higher levels.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("oGCD Heals:");

            var enableED = config.Astrologian.EnableEssentialDignity;
            if (ImGui.Checkbox("Enable Essential Dignity", ref enableED))
            {
                config.Astrologian.EnableEssentialDignity = enableED;
                save();
            }
            ImGui.TextDisabled("Potency scales with target's missing HP (400-1100).");

            var enableCI = config.Astrologian.EnableCelestialIntersection;
            if (ImGui.Checkbox("Enable Celestial Intersection", ref enableCI))
            {
                config.Astrologian.EnableCelestialIntersection = enableCI;
                save();
            }
            ImGui.TextDisabled("oGCD heal + shield.");

            var enableCO = config.Astrologian.EnableCelestialOpposition;
            if (ImGui.Checkbox("Enable Celestial Opposition", ref enableCO))
            {
                config.Astrologian.EnableCelestialOpposition = enableCO;
                save();
            }
            ImGui.TextDisabled("oGCD AoE heal + regen.");

            var enableExaltation = config.Astrologian.EnableExaltation;
            if (ImGui.Checkbox("Enable Exaltation", ref enableExaltation))
            {
                config.Astrologian.EnableExaltation = enableExaltation;
                save();
            }
            ImGui.TextDisabled("Damage reduction + delayed heal.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Single-Target Thresholds:");

            config.Astrologian.BeneficThreshold = ConfigUIHelpers.ThresholdSlider("Benefic Threshold",
                config.Astrologian.BeneficThreshold, 20f, 80f, null, save);
            config.Astrologian.BeneficIIThreshold = ConfigUIHelpers.ThresholdSlider("Benefic II Threshold",
                config.Astrologian.BeneficIIThreshold, 30f, 85f, null, save);
            config.Astrologian.AspectedBeneficThreshold = ConfigUIHelpers.ThresholdSlider("Aspected Benefic Threshold",
                config.Astrologian.AspectedBeneficThreshold, 50f, 95f, null, save);
            config.Astrologian.EssentialDignityThreshold = ConfigUIHelpers.ThresholdSlider("Essential Dignity Threshold",
                config.Astrologian.EssentialDignityThreshold, 20f, 60f, "Lower = more healing potency (scales with missing HP).", save);
            config.Astrologian.CelestialIntersectionThreshold = ConfigUIHelpers.ThresholdSlider("Celestial Intersection Threshold",
                config.Astrologian.CelestialIntersectionThreshold, 40f, 90f, null, save);
            config.Astrologian.ExaltationThreshold = ConfigUIHelpers.ThresholdSlider("Exaltation Threshold",
                config.Astrologian.ExaltationThreshold, 50f, 95f, null, save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("AoE Settings:");

            config.Astrologian.AoEHealThreshold = ConfigUIHelpers.ThresholdSlider("AoE HP Threshold",
                config.Astrologian.AoEHealThreshold, 50f, 90f, null, save);

            config.Astrologian.AoEHealMinTargets = ConfigUIHelpers.IntSlider("AoE Min Targets",
                config.Astrologian.AoEHealMinTargets, 1, 8,
                "Minimum party members below threshold for AoE heals.", save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawEarthlyStarSection()
    {
        if (ConfigUIHelpers.SectionHeader("Earthly Star", "AST"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableStar = config.Astrologian.EnableEarthlyStar;
            if (ImGui.Checkbox("Enable Earthly Star", ref enableStar))
            {
                config.Astrologian.EnableEarthlyStar = enableStar;
                save();
            }
            ImGui.TextDisabled("Ground-targeted AoE that matures over 10s.");

            ConfigUIHelpers.BeginDisabledGroup(!config.Astrologian.EnableEarthlyStar);

            var placementNames = Enum.GetNames<EarthlyStarPlacementStrategy>();
            var currentPlacement = (int)config.Astrologian.StarPlacement;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Placement", ref currentPlacement, placementNames, placementNames.Length))
            {
                config.Astrologian.StarPlacement = (EarthlyStarPlacementStrategy)currentPlacement;
                save();
            }

            var placementDesc = config.Astrologian.StarPlacement switch
            {
                EarthlyStarPlacementStrategy.OnMainTank => "Place on main tank",
                EarthlyStarPlacementStrategy.OnSelf => "Place on self",
                EarthlyStarPlacementStrategy.Manual => "Manual control only",
                _ => ""
            };
            ImGui.TextDisabled(placementDesc);

            ConfigUIHelpers.Spacing();

            config.Astrologian.EarthlyStarDetonateThreshold = ConfigUIHelpers.ThresholdSlider("Detonate Threshold",
                config.Astrologian.EarthlyStarDetonateThreshold, 40f, 85f, "Party average HP to trigger detonation.", save);

            config.Astrologian.EarthlyStarMinTargets = ConfigUIHelpers.IntSlider("Min Targets in Range",
                config.Astrologian.EarthlyStarMinTargets, 1, 8, null, save);

            var waitForGiant = config.Astrologian.WaitForGiantDominance;
            if (ImGui.Checkbox("Wait for Giant Dominance", ref waitForGiant))
            {
                config.Astrologian.WaitForGiantDominance = waitForGiant;
                save();
            }
            ImGui.TextDisabled("Wait 10s for star to mature before detonating.");

            if (config.Astrologian.WaitForGiantDominance)
            {
                config.Astrologian.EarthlyStarEmergencyThreshold = ConfigUIHelpers.ThresholdSliderSmall("Emergency Threshold",
                    config.Astrologian.EarthlyStarEmergencyThreshold, 20f, 60f,
                    "Detonate immature star if HP drops below this.", save);
            }

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawHoroscopeSection()
    {
        if (ConfigUIHelpers.SectionHeader("Horoscope", "AST"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableHoroscope = config.Astrologian.EnableHoroscope;
            if (ImGui.Checkbox("Enable Horoscope", ref enableHoroscope))
            {
                config.Astrologian.EnableHoroscope = enableHoroscope;
                save();
            }
            ImGui.TextDisabled("Delayed AoE heal that can be detonated manually.");

            ConfigUIHelpers.BeginDisabledGroup(!config.Astrologian.EnableHoroscope);

            var autoCast = config.Astrologian.AutoCastHoroscope;
            if (ImGui.Checkbox("Auto-Cast Horoscope", ref autoCast))
            {
                config.Astrologian.AutoCastHoroscope = autoCast;
                save();
            }
            ImGui.TextDisabled("Automatically prepare Horoscope.");

            config.Astrologian.HoroscopeThreshold = ConfigUIHelpers.ThresholdSlider("Detonate Threshold",
                config.Astrologian.HoroscopeThreshold, 50f, 90f, "Party HP threshold to detonate.", save);

            config.Astrologian.HoroscopeMinTargets = ConfigUIHelpers.IntSlider("Min Injured Targets",
                config.Astrologian.HoroscopeMinTargets, 1, 8, null, save);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawMacrocosmosSection()
    {
        if (ConfigUIHelpers.SectionHeader("Macrocosmos", "AST"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableMacro = config.Astrologian.EnableMacrocosmos;
            if (ImGui.Checkbox("Enable Macrocosmos", ref enableMacro))
            {
                config.Astrologian.EnableMacrocosmos = enableMacro;
                save();
            }
            ImGui.TextDisabled("Absorbs damage taken and heals based on it.");

            ConfigUIHelpers.BeginDisabledGroup(!config.Astrologian.EnableMacrocosmos);

            var autoUse = config.Astrologian.AutoUseMacrocosmos;
            if (ImGui.Checkbox("Auto-Use Macrocosmos", ref autoUse))
            {
                config.Astrologian.AutoUseMacrocosmos = autoUse;
                save();
            }

            config.Astrologian.MacrocosmosThreshold = ConfigUIHelpers.ThresholdSlider("Party HP Threshold",
                config.Astrologian.MacrocosmosThreshold, 60f, 95f, "Use when party average HP is below this.", save);

            config.Astrologian.MacrocosmosMinTargets = ConfigUIHelpers.IntSlider("Min Targets",
                config.Astrologian.MacrocosmosMinTargets, 1, 8, null, save);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawNeutralSectSection()
    {
        if (ConfigUIHelpers.SectionHeader("Neutral Sect", "AST"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableNS = config.Astrologian.EnableNeutralSect;
            if (ImGui.Checkbox("Enable Neutral Sect", ref enableNS))
            {
                config.Astrologian.EnableNeutralSect = enableNS;
                save();
            }
            ImGui.TextDisabled("Boosts healing and adds shield to Aspected spells.");

            ConfigUIHelpers.BeginDisabledGroup(!config.Astrologian.EnableNeutralSect);

            var strategyNames = Enum.GetNames<NeutralSectUsageStrategy>();
            var currentStrategy = (int)config.Astrologian.NeutralSectStrategy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Strategy", ref currentStrategy, strategyNames, strategyNames.Length))
            {
                config.Astrologian.NeutralSectStrategy = (NeutralSectUsageStrategy)currentStrategy;
                save();
            }

            var strategyDesc = config.Astrologian.NeutralSectStrategy switch
            {
                NeutralSectUsageStrategy.OnCooldown => "Use on cooldown",
                NeutralSectUsageStrategy.SaveForDamage => "Save for high damage phases",
                NeutralSectUsageStrategy.Manual => "Manual control only",
                _ => ""
            };
            ImGui.TextDisabled(strategyDesc);

            config.Astrologian.NeutralSectThreshold = ConfigUIHelpers.ThresholdSlider("HP Threshold",
                config.Astrologian.NeutralSectThreshold, 40f, 85f, "Party average HP to trigger.", save);

            var enableSunSign = config.Astrologian.EnableSunSign;
            if (ImGui.Checkbox("Enable Sun Sign", ref enableSunSign))
            {
                config.Astrologian.EnableSunSign = enableSunSign;
                save();
            }
            ImGui.TextDisabled("Level 100 follow-up ability.");

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawCardSection()
    {
        if (ConfigUIHelpers.SectionHeader("Cards", "AST"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableCards = config.Astrologian.EnableCards;
            if (ImGui.Checkbox("Enable Cards", ref enableCards))
            {
                config.Astrologian.EnableCards = enableCards;
                save();
            }
            ImGui.TextDisabled("Automatically draw and play cards.");

            ConfigUIHelpers.BeginDisabledGroup(!config.Astrologian.EnableCards);

            var strategyNames = Enum.GetNames<CardPlayStrategy>();
            var currentStrategy = (int)config.Astrologian.CardStrategy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Card Strategy", ref currentStrategy, strategyNames, strategyNames.Length))
            {
                config.Astrologian.CardStrategy = (CardPlayStrategy)currentStrategy;
                save();
            }

            var strategyDesc = config.Astrologian.CardStrategy switch
            {
                CardPlayStrategy.DpsFocused => "Target highest-contributing DPS",
                CardPlayStrategy.Balanced => "Balance between DPS and support",
                CardPlayStrategy.SafetyFocused => "Prioritize safety over damage",
                _ => ""
            };
            ImGui.TextDisabled(strategyDesc);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Minor Arcana:");

            var enableMinor = config.Astrologian.EnableMinorArcana;
            if (ImGui.Checkbox("Enable Minor Arcana", ref enableMinor))
            {
                config.Astrologian.EnableMinorArcana = enableMinor;
                save();
            }

            if (config.Astrologian.EnableMinorArcana)
            {
                var minorNames = Enum.GetNames<MinorArcanaUsageStrategy>();
                var currentMinor = (int)config.Astrologian.MinorArcanaStrategy;
                ImGui.SetNextItemWidth(150);
                if (ImGui.Combo("Minor Arcana Strategy", ref currentMinor, minorNames, minorNames.Length))
                {
                    config.Astrologian.MinorArcanaStrategy = (MinorArcanaUsageStrategy)currentMinor;
                    save();
                }

                config.Astrologian.LadyOfCrownsThreshold = ConfigUIHelpers.ThresholdSliderSmall("Lady of Crowns HP",
                    config.Astrologian.LadyOfCrownsThreshold, 30f, 80f,
                    "HP threshold to use Lady of Crowns heal.", save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Burst Abilities:");

            var enableDivination = config.Astrologian.EnableDivination;
            if (ImGui.Checkbox("Enable Divination", ref enableDivination))
            {
                config.Astrologian.EnableDivination = enableDivination;
                save();
            }
            ImGui.TextDisabled("Party-wide damage buff.");

            var enableAstrodyne = config.Astrologian.EnableAstrodyne;
            if (ImGui.Checkbox("Enable Astrodyne", ref enableAstrodyne))
            {
                config.Astrologian.EnableAstrodyne = enableAstrodyne;
                save();
            }

            if (config.Astrologian.EnableAstrodyne)
            {
                config.Astrologian.AstrodyneMinSeals = ConfigUIHelpers.IntSliderSmall("Min Unique Seals",
                    config.Astrologian.AstrodyneMinSeals, 1, 3,
                    "Wait for this many unique seals (more seals = better buffs).", save);
            }

            var enableOracle = config.Astrologian.EnableOracle;
            if (ImGui.Checkbox("Enable Oracle", ref enableOracle))
            {
                config.Astrologian.EnableOracle = enableOracle;
                save();
            }
            ImGui.TextDisabled("Divination follow-up ability.");

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawSynastrySection()
    {
        if (ConfigUIHelpers.SectionHeader("Synastry", "AST", false))
        {
            ConfigUIHelpers.BeginIndent();

            var enableSynastry = config.Astrologian.EnableSynastry;
            if (ImGui.Checkbox("Enable Synastry", ref enableSynastry))
            {
                config.Astrologian.EnableSynastry = enableSynastry;
                save();
            }
            ImGui.TextDisabled("Mirrors 40% of single-target heals to Synastry target.");

            ConfigUIHelpers.BeginDisabledGroup(!config.Astrologian.EnableSynastry);

            config.Astrologian.SynastryThreshold = ConfigUIHelpers.ThresholdSlider("Synastry Threshold",
                config.Astrologian.SynastryThreshold, 30f, 70f, "HP threshold to apply Synastry.", save);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawLightspeedSection()
    {
        if (ConfigUIHelpers.SectionHeader("Lightspeed", "AST", false))
        {
            ConfigUIHelpers.BeginIndent();

            var enableLS = config.Astrologian.EnableLightspeed;
            if (ImGui.Checkbox("Enable Lightspeed", ref enableLS))
            {
                config.Astrologian.EnableLightspeed = enableLS;
                save();
            }
            ImGui.TextDisabled("Reduces cast times and MP costs.");

            ConfigUIHelpers.BeginDisabledGroup(!config.Astrologian.EnableLightspeed);

            var strategyNames = Enum.GetNames<LightspeedUsageStrategy>();
            var currentStrategy = (int)config.Astrologian.LightspeedStrategy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Strategy", ref currentStrategy, strategyNames, strategyNames.Length))
            {
                config.Astrologian.LightspeedStrategy = (LightspeedUsageStrategy)currentStrategy;
                save();
            }

            var strategyDesc = config.Astrologian.LightspeedStrategy switch
            {
                LightspeedUsageStrategy.OnCooldown => "Use on cooldown",
                LightspeedUsageStrategy.SaveForMovement => "Save for movement-heavy phases",
                LightspeedUsageStrategy.SaveForRaise => "Save for raising dead party members",
                _ => ""
            };
            ImGui.TextDisabled(strategyDesc);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader("Damage", "AST"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel("Single-Target:");

            var enableST = config.Astrologian.EnableSingleTargetDamage;
            if (ImGui.Checkbox("Enable Malefic", ref enableST))
            {
                config.Astrologian.EnableSingleTargetDamage = enableST;
                save();
            }
            ImGui.TextDisabled("Casted single-target damage spell.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("DoT:");

            var enableDot = config.Astrologian.EnableDot;
            if (ImGui.Checkbox("Enable Combust", ref enableDot))
            {
                config.Astrologian.EnableDot = enableDot;
                save();
            }

            if (config.Astrologian.EnableDot)
            {
                config.Astrologian.DotRefreshThreshold = ConfigUIHelpers.FloatSlider("DoT Refresh (sec)",
                    config.Astrologian.DotRefreshThreshold, 0f, 10f, "%.1f",
                    "Refresh DoT when this many seconds remain.", save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("AoE:");

            var enableAoE = config.Astrologian.EnableAoEDamage;
            if (ImGui.Checkbox("Enable Gravity", ref enableAoE))
            {
                config.Astrologian.EnableAoEDamage = enableAoE;
                save();
            }

            if (config.Astrologian.EnableAoEDamage)
            {
                config.Astrologian.AoEDamageMinTargets = ConfigUIHelpers.IntSlider("Min Enemies",
                    config.Astrologian.AoEDamageMinTargets, 1, 10, null, save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("MP Management:");

            var enableLucid = config.Astrologian.EnableLucidDreaming;
            if (ImGui.Checkbox("Enable Lucid Dreaming", ref enableLucid))
            {
                config.Astrologian.EnableLucidDreaming = enableLucid;
                save();
            }

            if (config.Astrologian.EnableLucidDreaming)
            {
                config.Astrologian.LucidDreamingThreshold = ConfigUIHelpers.ThresholdSlider("Lucid MP Threshold",
                    config.Astrologian.LucidDreamingThreshold, 40f, 90f, null, save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Collective Unconscious:");

            var enableCU = config.Astrologian.EnableCollectiveUnconscious;
            if (ImGui.Checkbox("Enable Collective Unconscious", ref enableCU))
            {
                config.Astrologian.EnableCollectiveUnconscious = enableCU;
                save();
            }
            ConfigUIHelpers.WarningText("Channeled ability - may interrupt other actions.");

            if (config.Astrologian.EnableCollectiveUnconscious)
            {
                config.Astrologian.CollectiveUnconsciousThreshold = ConfigUIHelpers.ThresholdSliderSmall("CU HP Threshold",
                    config.Astrologian.CollectiveUnconsciousThreshold, 30f, 70f, null, save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }
}
