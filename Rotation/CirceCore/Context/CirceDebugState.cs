namespace Olympus.Rotation.CirceCore.Context;

/// <summary>
/// Debug state for Circe (Red Mage) rotation.
/// </summary>
public sealed class CirceDebugState
{
    // Planning state
    public string PlanningState { get; set; } = "";
    public string PlannedAction { get; set; } = "";

    // Module states
    public string DamageState { get; set; } = "";
    public string BuffState { get; set; } = "";

    // Mana state
    public int BlackMana { get; set; }
    public int WhiteMana { get; set; }
    public int ManaImbalance { get; set; }
    public int ManaStacks { get; set; }
    public bool CanStartMeleeCombo { get; set; }

    // Dualcast state
    public bool HasDualcast { get; set; }
    public float DualcastRemaining { get; set; }

    // Proc state
    public bool HasVerfire { get; set; }
    public float VerfireRemaining { get; set; }
    public bool HasVerstone { get; set; }
    public float VerstoneRemaining { get; set; }

    // Melee combo state
    public bool IsInMeleeCombo { get; set; }
    public string MeleeComboStep { get; set; } = "None";
    public bool IsFinisherReady { get; set; }
    public bool IsScorchReady { get; set; }
    public bool IsResolutionReady { get; set; }

    // Buff state
    public bool HasEmbolden { get; set; }
    public float EmboldenRemaining { get; set; }
    public bool HasManafication { get; set; }
    public float ManaficationRemaining { get; set; }
    public bool HasAcceleration { get; set; }
    public float AccelerationRemaining { get; set; }
    public bool HasSwiftcast { get; set; }

    // Special ability state
    public bool HasThornedFlourish { get; set; }
    public bool HasGrandImpactReady { get; set; }
    public bool HasPrefulgenceReady { get; set; }

    // Cooldown state
    public bool FlecheReady { get; set; }
    public bool ContreSixteReady { get; set; }
    public bool EmboldenReady { get; set; }
    public bool ManaficationReady { get; set; }
    public int CorpsACorpsCharges { get; set; }
    public int EngagementCharges { get; set; }
    public int AccelerationCharges { get; set; }

    // Resource state
    public int CurrentMp { get; set; }
    public int MaxMp { get; set; }

    // Combat info
    public string CurrentTarget { get; set; } = "None";
    public int NearbyEnemies { get; set; }

    // Phase
    public string Phase { get; set; } = "Waiting";
}
