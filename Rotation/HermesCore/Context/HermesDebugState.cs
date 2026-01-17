using Olympus.Data;

namespace Olympus.Rotation.HermesCore.Context;

/// <summary>
/// Debug state for Ninja (Hermes) rotation.
/// Tracks rotation decisions and state for debug display.
/// </summary>
public sealed class HermesDebugState
{
    // Module states
    public string DamageState { get; set; } = "";
    public string BuffState { get; set; } = "";
    public string NinjutsuState { get; set; } = "";

    // Current action planning
    public string PlannedAction { get; set; } = "";
    public string PlanningState { get; set; } = "";

    // Gauge tracking
    public int Ninki { get; set; }
    public int Kazematoi { get; set; }

    // Mudra state
    public bool IsMudraActive { get; set; }
    public int MudraCount { get; set; }
    public string MudraSequence { get; set; } = "";
    public NINActions.NinjutsuType PendingNinjutsu { get; set; }
    public bool HasKassatsu { get; set; }
    public bool HasTenChiJin { get; set; }
    public int TenChiJinStacks { get; set; }

    // Buff tracking
    public bool HasSuiton { get; set; }
    public float SuitonRemaining { get; set; }
    public bool HasBunshin { get; set; }
    public int BunshinStacks { get; set; }
    public bool HasPhantomKamaitachiReady { get; set; }
    public bool HasRaijuReady { get; set; }
    public int RaijuStacks { get; set; }
    public bool HasMeisui { get; set; }
    public bool HasTenriJindoReady { get; set; }

    // Debuff tracking
    public bool HasKunaisBaneOnTarget { get; set; }
    public float KunaisBaneRemaining { get; set; }
    public bool HasDokumoriOnTarget { get; set; }
    public float DokumoriRemaining { get; set; }

    // Combo tracking
    public int ComboStep { get; set; }
    public float ComboTimeRemaining { get; set; }

    // Positional tracking
    public bool IsAtRear { get; set; }
    public bool IsAtFlank { get; set; }
    public bool HasTrueNorth { get; set; }
    public bool TargetHasPositionalImmunity { get; set; }

    // Targeting
    public string CurrentTarget { get; set; } = "";
    public int NearbyEnemies { get; set; }

    /// <summary>
    /// Gets a formatted string of the current mudra sequence.
    /// </summary>
    public static string FormatMudraSequence(NINActions.MudraType m1, NINActions.MudraType m2, NINActions.MudraType m3)
    {
        static char MudraChar(NINActions.MudraType m) => m switch
        {
            NINActions.MudraType.Ten => 'T',
            NINActions.MudraType.Chi => 'C',
            NINActions.MudraType.Jin => 'J',
            _ => '-'
        };

        return $"[{MudraChar(m1)}{MudraChar(m2)}{MudraChar(m3)}]";
    }
}
