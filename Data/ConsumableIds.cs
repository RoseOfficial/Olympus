using System.Collections.Generic;

namespace Olympus.Data;

/// <summary>
/// Item IDs and per-job stat mappings for current-expansion (Dawntrail) combat tinctures.
///
/// Tinctures share recast group 68 (270s shared cooldown across all stats).
/// HQ item ID is always NQ ID + 1_000_000.
///
/// To verify the IDs: in-game, use any tincture in inventory, hover the tooltip,
/// and check ItemHelpers / Allagan Tools for the item ID. As of Dawntrail 7.x, all
/// six stat tinctures are listed below — we only consume 4 (STR/DEX/INT/MND) per
/// the v1 spec scope (VIT/PIE survival pots out of scope).
/// </summary>
public static class ConsumableIds
{
    // Dawntrail Grade 1 (NQ item IDs). HQ = NQ + 1_000_000.
    public const uint TinctureOfStrength_NQ      = 44164;
    public const uint TinctureOfDexterity_NQ     = 44165;
    public const uint TinctureOfVitality_NQ      = 44166;  // unused in v1
    public const uint TinctureOfIntelligence_NQ  = 44167;
    public const uint TinctureOfMind_NQ          = 44168;
    public const uint TinctureOfPiety_NQ         = 44169;  // unused in v1

    public const uint HqOffset = 1_000_000;

    /// <summary>Per-job main DPS stat → tincture NQ ID. 21 jobs covered.</summary>
    public static readonly IReadOnlyDictionary<uint, uint> TinctureByJob = new Dictionary<uint, uint>
    {
        // STR (8): PLD, WAR, DRK, GNB, MNK, DRG, SAM, RPR
        { JobRegistry.Paladin,        TinctureOfStrength_NQ },
        { JobRegistry.Warrior,        TinctureOfStrength_NQ },
        { JobRegistry.DarkKnight,     TinctureOfStrength_NQ },
        { JobRegistry.Gunbreaker,     TinctureOfStrength_NQ },
        { JobRegistry.Monk,           TinctureOfStrength_NQ },
        { JobRegistry.Dragoon,        TinctureOfStrength_NQ },
        { JobRegistry.Samurai,        TinctureOfStrength_NQ },
        { JobRegistry.Reaper,         TinctureOfStrength_NQ },

        // DEX (5): NIN, VPR, BRD, MCH, DNC
        { JobRegistry.Ninja,          TinctureOfDexterity_NQ },
        { JobRegistry.Viper,          TinctureOfDexterity_NQ },
        { JobRegistry.Bard,           TinctureOfDexterity_NQ },
        { JobRegistry.Machinist,      TinctureOfDexterity_NQ },
        { JobRegistry.Dancer,         TinctureOfDexterity_NQ },

        // INT (4): BLM, SMN, RDM, PCT
        { JobRegistry.BlackMage,      TinctureOfIntelligence_NQ },
        { JobRegistry.Summoner,       TinctureOfIntelligence_NQ },
        { JobRegistry.RedMage,        TinctureOfIntelligence_NQ },
        { JobRegistry.Pictomancer,    TinctureOfIntelligence_NQ },

        // MND (4): WHM, SCH, AST, SGE
        { JobRegistry.WhiteMage,      TinctureOfMind_NQ },
        { JobRegistry.Scholar,        TinctureOfMind_NQ },
        { JobRegistry.Astrologian,    TinctureOfMind_NQ },
        { JobRegistry.Sage,           TinctureOfMind_NQ },
    };

    /// <summary>Display name for warning messages, keyed by tincture NQ ID.</summary>
    public static readonly IReadOnlyDictionary<uint, string> StatLabel = new Dictionary<uint, string>
    {
        { TinctureOfStrength_NQ,     "Strength" },
        { TinctureOfDexterity_NQ,    "Dexterity" },
        { TinctureOfIntelligence_NQ, "Intelligence" },
        { TinctureOfMind_NQ,         "Mind" },
    };
}
