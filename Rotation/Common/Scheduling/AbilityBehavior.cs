using System;
using Dalamud.Plugin.Services;
using Olympus.Models.Action;

namespace Olympus.Rotation.Common.Scheduling;

/// <summary>
/// Declarative metadata for a rotation ability. The scheduler reads these
/// fields to decide whether an ability is currently firable. Defined as
/// a <c>record</c> so tests can use <c>with</c> expressions to vary fields.
/// </summary>
public sealed record AbilityBehavior
{
    /// <summary>The underlying game-data action definition (source of level, cast time, range, dispatch ID).</summary>
    public required ActionDefinition Action { get; init; }

    /// <summary>
    /// Per-ability configuration gate. Returning <c>false</c> prevents the
    /// ability from firing. Null means "no toggle — always allowed."
    /// </summary>
    public Func<Configuration, bool>? Toggle { get; init; }

    /// <summary>
    /// Status ID the player must have for this ability to fire (ReadyToRip etc.).
    /// Null means "no proc requirement."
    /// </summary>
    public uint? ProcBuff { get; init; }

    /// <summary>
    /// Typed job-gauge predicate for combo-step / gauge-state gating.
    /// Prefer this over <see cref="AdjustedActionProbe"/> when a typed gauge field exists.
    /// </summary>
    public Func<IJobGauges, bool>? ComboStep { get; init; }

    /// <summary>
    /// Base action ID to probe via <c>GetAdjustedActionId</c> — if the game
    /// resolves the base ID to <see cref="Action"/>'s ID, the combo step is
    /// valid. Used for chains without a typed gauge field (RDM Enchanted).
    /// </summary>
    public uint? AdjustedActionProbe { get; init; }

    /// <summary>
    /// Base action ID to dispatch via <c>ExecuteXxxRaw</c> when the game's
    /// <c>UseAction</c> rejects the replacement ID (NIN ninjutsu pattern).
    /// When set, scheduler uses <c>ExecuteGcdRaw</c>/<c>ExecuteOgcdRaw</c>
    /// instead of <c>ExecuteGcd</c>/<c>ExecuteOgcd</c>.
    /// </summary>
    public uint? ReplacementBaseId { get; init; }

    /// <summary>
    /// Level-aware upgrades. Scheduler picks the highest entry whose
    /// <c>Level</c> is less than or equal to player level. Null means "no upgrades."
    /// </summary>
    public (byte Level, ActionDefinition Replacement)[]? LevelReplacements { get; init; }

    /// <summary>
    /// Charge-source action ID when it differs from <see cref="Action"/>.
    /// MCH GaussRound to DoubleCheck at level 92: charges are queried on the
    /// level-appropriate ID, not the base.
    /// </summary>
    public uint? ChargeSource { get; init; }

    /// <summary>
    /// When true, cast-time GCD damage is blocked if <c>MechanicCastGate.ShouldBlock</c>
    /// predicts a raidwide or tank buster before the cast completes.
    /// Instants (CastTime = 0) are never blocked regardless.
    /// </summary>
    public bool MechanicGate { get; init; }
}
