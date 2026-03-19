using System;
using System.Collections.Generic;

namespace Olympus.Services.Healing;

/// <summary>
/// Consolidates spell enablement checking from configuration.
/// Uses a dictionary-based registry pattern for maintainability and multi-job scalability.
/// </summary>
public class SpellEnablementService : ISpellEnablementService
{
    private readonly Dictionary<uint, Func<bool>> _spellEnablementRegistry;

    public SpellEnablementService(Configuration configuration)
    {
        // Initialize registry with all known WHM spells and their config bindings
        _spellEnablementRegistry = new Dictionary<uint, Func<bool>>
        {
            // Healing - Single Target
            { 120, () => configuration.Healing.EnableCure },              // Cure
            { 135, () => configuration.Healing.EnableCureII },            // Cure II
            { 137, () => configuration.Healing.EnableRegen },             // Regen
            { 16531, () => configuration.Healing.EnableAfflatusSolace },  // Afflatus Solace
            { 3570, () => configuration.Healing.EnableTetragrammaton },   // Tetragrammaton
            { 140, () => configuration.Healing.EnableBenediction },       // Benediction

            // Healing - AoE
            { 131, () => configuration.Healing.EnableCureIII },           // Cure III
            { 124, () => configuration.Healing.EnableMedica },            // Medica
            { 133, () => configuration.Healing.EnableMedicaII },          // Medica II
            { 37010, () => configuration.Healing.EnableMedicaIII },       // Medica III
            { 16534, () => configuration.Healing.EnableAfflatusRapture }, // Afflatus Rapture
            { 3571, () => configuration.Healing.EnableAssize },           // Assize
            { 3569, () => configuration.Healing.EnableAsylum },           // Asylum

            // Damage
            { 119, () => configuration.Damage.EnableStone },              // Stone
            { 127, () => configuration.Damage.EnableStoneII },            // Stone II
            { 3568, () => configuration.Damage.EnableStoneIII },          // Stone III
            { 7431, () => configuration.Damage.EnableStoneIV },           // Stone IV
            { 16533, () => configuration.Damage.EnableGlare },            // Glare
            { 25859, () => configuration.Damage.EnableGlareIII },         // Glare III
            { 37009, () => configuration.Damage.EnableGlareIV },          // Glare IV
            { 139, () => configuration.Damage.EnableHoly },               // Holy
            { 25860, () => configuration.Damage.EnableHolyIII },          // Holy III
            { 16535, () => configuration.Damage.EnableAfflatusMisery },   // Afflatus Misery

            // DoT
            { 121, () => configuration.Dot.EnableAero },                  // Aero
            { 132, () => configuration.Dot.EnableAeroII },                // Aero II
            { 16532, () => configuration.Dot.EnableDia },                 // Dia

            // Defensive
            { 7432, () => configuration.Defensive.EnableDivineBenison },  // Divine Benison
            { 7433, () => configuration.Defensive.EnablePlenaryIndulgence }, // Plenary Indulgence
            { 16536, () => configuration.Defensive.EnableTemperance },    // Temperance
            { 25861, () => configuration.Defensive.EnableAquaveil },      // Aquaveil
            { 25862, () => configuration.Defensive.EnableLiturgyOfTheBell }, // Liturgy of the Bell
            { 37011, () => configuration.Defensive.EnableDivineCaress },  // Divine Caress

            // Buffs
            { 136, () => configuration.Buffs.EnablePresenceOfMind },      // Presence of Mind
            { 7430, () => configuration.Buffs.EnableThinAir },            // Thin Air
            { 37008, () => configuration.Buffs.EnableAetherialShift },    // Aetherial Shift

            // Role Actions
            { 7568, () => configuration.RoleActions.EnableEsuna },        // Esuna

            // Resurrection
            { 125, () => configuration.Resurrection.EnableRaise },        // Raise

            // ---------------------------------------------------------------
            // Scholar (SCH) — configuration.Scholar.*
            // ---------------------------------------------------------------

            // Healing - Single Target
            { 190, () => configuration.Scholar.EnablePhysick },            // Physick
            { 185, () => configuration.Scholar.EnableAdloquium },          // Adloquium
            { 37015, () => configuration.Scholar.EnableAdloquium },        // Manifestation (Seraphism upgrade)

            // Healing - AoE
            { 186, () => configuration.Scholar.EnableSuccor },             // Succor
            { 37013, () => configuration.Scholar.EnableSuccor },           // Concitation (Lv96 upgrade)
            { 37016, () => configuration.Scholar.EnableSuccor },           // Accession (Seraphism upgrade)

            // oGCD Heals - Aetherflow
            { 189, () => configuration.Scholar.EnableLustrate },           // Lustrate
            { 3583, () => configuration.Scholar.EnableIndomitability },    // Indomitability
            { 7434, () => configuration.Scholar.EnableExcogitation },      // Excogitation
            { 188, () => configuration.Scholar.EnableSacredSoil },         // Sacred Soil

            // oGCD Heals - Free
            { 25867, () => configuration.Scholar.EnableProtraction },      // Protraction

            // Shield
            { 3586, () => configuration.Scholar.EnableEmergencyTactics },  // Emergency Tactics
            { 3585, () => configuration.Scholar.EnableDeploymentTactics }, // Deployment Tactics

            // Damage - Single Target
            { 17869, () => configuration.Scholar.EnableSingleTargetDamage }, // Ruin
            { 17870, () => configuration.Scholar.EnableRuinII },             // Ruin II
            { 3584, () => configuration.Scholar.EnableSingleTargetDamage },  // Broil
            { 7435, () => configuration.Scholar.EnableSingleTargetDamage },  // Broil II
            { 16541, () => configuration.Scholar.EnableSingleTargetDamage }, // Broil III
            { 25865, () => configuration.Scholar.EnableSingleTargetDamage }, // Broil IV

            // Damage - AoE
            { 16539, () => configuration.Scholar.EnableAoEDamage },          // Art of War
            { 25866, () => configuration.Scholar.EnableAoEDamage },          // Art of War II

            // DoT
            { 17864, () => configuration.Scholar.EnableDot },               // Bio
            { 17865, () => configuration.Scholar.EnableDot },               // Bio II
            { 16540, () => configuration.Scholar.EnableDot },               // Biolysis

            // Aetherflow
            { 166, () => configuration.Scholar.EnableAetherflow },          // Aetherflow
            { 167, () => configuration.Scholar.EnableEnergyDrain },         // Energy Drain

            // Buffs
            { 16542, () => configuration.Scholar.EnableRecitation },        // Recitation
            { 3587, () => configuration.Scholar.EnableDissipation },        // Dissipation
            { 7436, () => configuration.Scholar.EnableChainStratagem },     // Chain Stratagem
            { 25868, () => configuration.Scholar.EnableExpedient },         // Expedient
            { 37012, () => configuration.Scholar.EnableBanefulImpaction },  // Baneful Impaction

            // Fairy Abilities
            { 16537, () => configuration.Scholar.EnableFairyAbilities },    // Whispering Dawn
            { 16543, () => configuration.Scholar.EnableFairyAbilities },    // Fey Blessing
            { 16546, () => configuration.Scholar.EnableConsolation },       // Consolation (Seraph)

            // ---------------------------------------------------------------
            // Astrologian (AST) — configuration.Astrologian.*
            // ---------------------------------------------------------------

            // Healing - Single Target
            { 3594, () => configuration.Astrologian.EnableBenefic },        // Benefic
            { 3610, () => configuration.Astrologian.EnableBeneficII },      // Benefic II
            { 3595, () => configuration.Astrologian.EnableAspectedBenefic }, // Aspected Benefic

            // Healing - AoE
            { 3600, () => configuration.Astrologian.EnableHelios },         // Helios
            { 3601, () => configuration.Astrologian.EnableAspectedHelios }, // Aspected Helios
            { 37030, () => configuration.Astrologian.EnableAspectedHelios }, // Helios Conjunction (Lv96)

            // oGCD Heals
            { 3614, () => configuration.Astrologian.EnableEssentialDignity },     // Essential Dignity
            { 16556, () => configuration.Astrologian.EnableCelestialIntersection }, // Celestial Intersection
            { 16553, () => configuration.Astrologian.EnableCelestialOpposition },  // Celestial Opposition
            { 25873, () => configuration.Astrologian.EnableExaltation },           // Exaltation
            { 16557, () => configuration.Astrologian.EnableHoroscope },            // Horoscope
            { 25874, () => configuration.Astrologian.EnableMacrocosmos },          // Macrocosmos

            // Earthly Star
            { 7439, () => configuration.Astrologian.EnableEarthlyStar },           // Earthly Star

            // Cards
            { 37023, () => configuration.Astrologian.EnableCards },                // The Balance
            { 37026, () => configuration.Astrologian.EnableCards },                // The Spear
            { 7445, () => configuration.Astrologian.EnableMinorArcana },           // Lady of Crowns
            { 16552, () => configuration.Astrologian.EnableDivination },           // Divination
            { 25870, () => configuration.Astrologian.EnableAstrodyne },            // Astrodyne
            { 37029, () => configuration.Astrologian.EnableOracle },               // Oracle

            // Buffs
            { 3606, () => configuration.Astrologian.EnableLightspeed },            // Lightspeed
            { 3612, () => configuration.Astrologian.EnableSynastry },              // Synastry
            { 16559, () => configuration.Astrologian.EnableNeutralSect },          // Neutral Sect
            { 37031, () => configuration.Astrologian.EnableSunSign },              // Sun Sign
            { 3613, () => configuration.Astrologian.EnableCollectiveUnconscious }, // Collective Unconscious

            // Damage - Single Target
            { 3596, () => configuration.Astrologian.EnableSingleTargetDamage },    // Malefic
            { 3598, () => configuration.Astrologian.EnableSingleTargetDamage },    // Malefic II
            { 7442, () => configuration.Astrologian.EnableSingleTargetDamage },    // Malefic III
            { 16555, () => configuration.Astrologian.EnableSingleTargetDamage },   // Malefic IV
            { 25871, () => configuration.Astrologian.EnableSingleTargetDamage },   // Fall Malefic

            // Damage - AoE
            { 3615, () => configuration.Astrologian.EnableAoEDamage },             // Gravity
            { 25872, () => configuration.Astrologian.EnableAoEDamage },            // Gravity II

            // DoT
            { 3599, () => configuration.Astrologian.EnableDot },                   // Combust
            { 3608, () => configuration.Astrologian.EnableDot },                   // Combust II
            { 16554, () => configuration.Astrologian.EnableDot },                  // Combust III

            // ---------------------------------------------------------------
            // Sage (SGE) — configuration.Sage.*
            // ---------------------------------------------------------------

            // Healing - Single Target
            { 24284, () => configuration.Sage.EnableDiagnosis },              // Diagnosis
            { 24291, () => configuration.Sage.EnableEukrasianDiagnosis },     // Eukrasian Diagnosis

            // Healing - AoE
            { 24286, () => configuration.Sage.EnablePrognosis },              // Prognosis
            { 24292, () => configuration.Sage.EnableEukrasianPrognosis },     // Eukrasian Prognosis
            { 37033, () => configuration.Sage.EnableEukrasianPrognosis },     // Eukrasian Prognosis II (Lv96)

            // oGCD Heals - Addersgall
            { 24296, () => configuration.Sage.EnableDruochole },              // Druochole
            { 24303, () => configuration.Sage.EnableTaurochole },             // Taurochole
            { 24299, () => configuration.Sage.EnableIxochole },               // Ixochole
            { 24298, () => configuration.Sage.EnableKerachole },              // Kerachole

            // oGCD Heals - Free
            { 24302, () => configuration.Sage.EnablePhysisII },               // Physis II
            { 24310, () => configuration.Sage.EnableHolos },                  // Holos
            { 24301, () => configuration.Sage.EnablePepsis },                 // Pepsis
            { 24318, () => configuration.Sage.EnablePneuma },                 // Pneuma

            // Addersgall
            { 24309, () => configuration.Sage.EnableRhizomata },              // Rhizomata

            // Shields
            { 24305, () => configuration.Sage.EnableHaima },                  // Haima
            { 24311, () => configuration.Sage.EnablePanhaima },               // Panhaima

            // Buffs
            { 24294, () => configuration.Sage.EnableSoteria },                // Soteria
            { 24300, () => configuration.Sage.EnableZoe },                    // Zoe
            { 24317, () => configuration.Sage.EnableKrasis },                 // Krasis
            { 37035, () => configuration.Sage.EnablePhilosophia },            // Philosophia

            // Damage - Single Target
            { 24283, () => configuration.Sage.EnableSingleTargetDamage },     // Dosis
            { 24306, () => configuration.Sage.EnableSingleTargetDamage },     // Dosis II
            { 24312, () => configuration.Sage.EnableSingleTargetDamage },     // Dosis III

            // Damage - AoE
            { 24297, () => configuration.Sage.EnableAoEDamage },              // Dyskrasia
            { 24315, () => configuration.Sage.EnableAoEDamage },              // Dyskrasia II

            // DoT
            { 24293, () => configuration.Sage.EnableDot },                    // Eukrasian Dosis
            { 24308, () => configuration.Sage.EnableDot },                    // Eukrasian Dosis II
            { 24314, () => configuration.Sage.EnableDot },                    // Eukrasian Dosis III

            // Damage - oGCD
            { 24289, () => configuration.Sage.EnablePhlegma },                // Phlegma
            { 24307, () => configuration.Sage.EnablePhlegma },                // Phlegma II
            { 24313, () => configuration.Sage.EnablePhlegma },                // Phlegma III
            { 24304, () => configuration.Sage.EnableToxikon },                // Toxikon
            { 24316, () => configuration.Sage.EnableToxikon },                // Toxikon II
            { 37034, () => configuration.Sage.EnablePsyche },                 // Psyche
        };
    }

    /// <summary>
    /// Checks if a spell is enabled in the configuration.
    /// Covers all WHM healing, damage, defensive, and role actions.
    /// </summary>
    public bool IsSpellEnabled(uint actionId)
    {
        return _spellEnablementRegistry.TryGetValue(actionId, out var enabledCheck)
            ? enabledCheck()
            : true; // Default to enabled for unknown spells
    }
}
