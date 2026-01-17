namespace Olympus.Rotation.KratosCore.Context;

/// <summary>
/// Debug state for Monk (Kratos) rotation.
/// Tracks rotation decisions and state for debug display.
/// </summary>
public sealed class KratosDebugState
{
    // Module states
    public string DamageState { get; set; } = "";
    public string BuffState { get; set; } = "";

    // Current action planning
    public string PlannedAction { get; set; } = "";
    public string PlanningState { get; set; } = "";

    // Form tracking
    public MonkForm CurrentForm { get; set; }
    public bool HasPerfectBalance { get; set; }
    public int PerfectBalanceStacks { get; set; }
    public bool HasFormlessFist { get; set; }

    // Chakra tracking
    public int Chakra { get; set; }
    public string BeastChakraState { get; set; } = "";
    public int BeastChakraCount { get; set; }
    public bool HasLunarNadi { get; set; }
    public bool HasSolarNadi { get; set; }

    // Buff tracking
    public bool HasDisciplinedFist { get; set; }
    public float DisciplinedFistRemaining { get; set; }
    public bool HasLeadenFist { get; set; }
    public bool HasRiddleOfFire { get; set; }
    public float RiddleOfFireRemaining { get; set; }
    public bool HasBrotherhood { get; set; }
    public bool HasRiddleOfWind { get; set; }

    // Proc tracking
    public bool HasRaptorsFury { get; set; }
    public bool HasCoeurlsFury { get; set; }
    public bool HasOpooposFury { get; set; }
    public bool HasFiresRumination { get; set; }
    public bool HasWindsRumination { get; set; }

    // DoT tracking
    public bool HasDemolishOnTarget { get; set; }
    public float DemolishRemaining { get; set; }

    // Positional tracking
    public bool IsAtRear { get; set; }
    public bool IsAtFlank { get; set; }
    public bool HasTrueNorth { get; set; }
    public bool TargetHasPositionalImmunity { get; set; }

    // Targeting
    public string CurrentTarget { get; set; } = "";
    public int NearbyEnemies { get; set; }

    /// <summary>
    /// Gets a formatted string of the Beast Chakra state.
    /// </summary>
    public static string FormatBeastChakra(byte c1, byte c2, byte c3)
    {
        static char ChakraChar(byte c) => c switch
        {
            1 => 'O', // Opo-opo
            2 => 'R', // Raptor
            3 => 'C', // Coeurl
            _ => '-'  // None
        };

        return $"[{ChakraChar(c1)}{ChakraChar(c2)}{ChakraChar(c3)}]";
    }
}
