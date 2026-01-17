using Olympus.Data;

namespace Olympus.Rotation.HermesCore.Helpers;

/// <summary>
/// Helper class for Ninja mudra sequence tracking and execution.
/// Handles the state machine for mudra inputs and Ninjutsu execution.
/// </summary>
public sealed class MudraHelper
{
    /// <summary>
    /// Current state of the mudra sequence.
    /// </summary>
    public MudraState State { get; private set; } = MudraState.Idle;

    /// <summary>
    /// The target Ninjutsu we're trying to execute.
    /// </summary>
    public NINActions.NinjutsuType TargetNinjutsu { get; private set; } = NINActions.NinjutsuType.None;

    /// <summary>
    /// First mudra in the current sequence.
    /// </summary>
    public NINActions.MudraType Mudra1 { get; private set; } = NINActions.MudraType.None;

    /// <summary>
    /// Second mudra in the current sequence.
    /// </summary>
    public NINActions.MudraType Mudra2 { get; private set; } = NINActions.MudraType.None;

    /// <summary>
    /// Third mudra in the current sequence.
    /// </summary>
    public NINActions.MudraType Mudra3 { get; private set; } = NINActions.MudraType.None;

    /// <summary>
    /// How many mudras have been input so far.
    /// </summary>
    public int MudraCount { get; private set; }

    /// <summary>
    /// Whether we're currently in the middle of a mudra sequence.
    /// </summary>
    public bool IsSequenceActive => State != MudraState.Idle;

    /// <summary>
    /// Whether we're ready to execute the Ninjutsu (all mudras input).
    /// </summary>
    public bool IsReadyToExecute => State == MudraState.ReadyToExecute;

    /// <summary>
    /// Starts a new mudra sequence for the specified Ninjutsu.
    /// </summary>
    /// <param name="ninjutsu">The Ninjutsu to execute.</param>
    public void StartSequence(NINActions.NinjutsuType ninjutsu)
    {
        Reset();
        TargetNinjutsu = ninjutsu;
        State = MudraState.FirstMudra;

        // Pre-calculate the mudra sequence
        var sequence = NINActions.GetMudraSequence(ninjutsu);
        Mudra1 = sequence.Item1;
        Mudra2 = sequence.Item2;
        Mudra3 = sequence.Item3;
    }

    /// <summary>
    /// Gets the next mudra to input in the sequence.
    /// </summary>
    /// <returns>The next mudra action, or null if sequence is complete or invalid.</returns>
    public NINActions.MudraType GetNextMudra()
    {
        return State switch
        {
            MudraState.FirstMudra => Mudra1,
            MudraState.SecondMudra => Mudra2,
            MudraState.ThirdMudra => Mudra3,
            _ => NINActions.MudraType.None
        };
    }

    /// <summary>
    /// Advances the mudra sequence after successfully inputting a mudra.
    /// </summary>
    public void AdvanceSequence()
    {
        MudraCount++;

        State = State switch
        {
            MudraState.FirstMudra => Mudra2 != NINActions.MudraType.None
                ? MudraState.SecondMudra
                : MudraState.ReadyToExecute,
            MudraState.SecondMudra => Mudra3 != NINActions.MudraType.None
                ? MudraState.ThirdMudra
                : MudraState.ReadyToExecute,
            MudraState.ThirdMudra => MudraState.ReadyToExecute,
            _ => State
        };
    }

    /// <summary>
    /// Marks the Ninjutsu as executed and resets the sequence.
    /// </summary>
    public void CompleteSequence()
    {
        Reset();
    }

    /// <summary>
    /// Resets the mudra helper to idle state.
    /// Call this if the sequence is interrupted or needs to be cancelled.
    /// </summary>
    public void Reset()
    {
        State = MudraState.Idle;
        TargetNinjutsu = NINActions.NinjutsuType.None;
        Mudra1 = NINActions.MudraType.None;
        Mudra2 = NINActions.MudraType.None;
        Mudra3 = NINActions.MudraType.None;
        MudraCount = 0;
    }

    /// <summary>
    /// Gets the number of mudras required for the target Ninjutsu.
    /// </summary>
    public int GetRequiredMudraCount()
    {
        if (Mudra3 != NINActions.MudraType.None) return 3;
        if (Mudra2 != NINActions.MudraType.None) return 2;
        if (Mudra1 != NINActions.MudraType.None) return 1;
        return 0;
    }

    /// <summary>
    /// Determines the best Ninjutsu to use based on current situation.
    /// </summary>
    /// <param name="level">Player level.</param>
    /// <param name="hasKassatsu">Whether Kassatsu is active.</param>
    /// <param name="needsSuiton">Whether we need Suiton for Kunai's Bane.</param>
    /// <param name="enemyCount">Number of nearby enemies.</param>
    /// <returns>The recommended Ninjutsu to use.</returns>
    public static NINActions.NinjutsuType GetRecommendedNinjutsu(
        byte level,
        bool hasKassatsu,
        bool needsSuiton,
        int enemyCount)
    {
        // Kassatsu-enhanced Ninjutsu
        if (hasKassatsu)
        {
            // AoE situation
            if (enemyCount >= 3 && level >= NINActions.GokaMekkyaku.MinLevel)
                return NINActions.NinjutsuType.GokaMekkyaku;

            // Single target - Hyosho Ranryu is huge damage
            if (level >= NINActions.HyoshoRanryu.MinLevel)
                return NINActions.NinjutsuType.HyoshoRanryu;

            // Fallback to Raiton for lower levels
            if (level >= NINActions.Raiton.MinLevel)
                return NINActions.NinjutsuType.Raiton;

            return NINActions.NinjutsuType.FumaShuriken;
        }

        // Need Suiton for Kunai's Bane window
        if (needsSuiton && level >= NINActions.Suiton.MinLevel)
            return NINActions.NinjutsuType.Suiton;

        // AoE situations
        if (enemyCount >= 3)
        {
            // Doton for stationary AoE
            if (level >= NINActions.Doton.MinLevel)
                return NINActions.NinjutsuType.Doton;

            // Katon for burst AoE
            if (level >= NINActions.Katon.MinLevel)
                return NINActions.NinjutsuType.Katon;
        }

        // Single target - Raiton is the go-to
        if (level >= NINActions.Raiton.MinLevel)
            return NINActions.NinjutsuType.Raiton;

        // Low level fallback
        return NINActions.NinjutsuType.FumaShuriken;
    }
}

/// <summary>
/// States for the mudra input state machine.
/// </summary>
public enum MudraState
{
    /// <summary>Not currently in a mudra sequence.</summary>
    Idle,

    /// <summary>Waiting to input the first mudra.</summary>
    FirstMudra,

    /// <summary>First mudra input, waiting for second.</summary>
    SecondMudra,

    /// <summary>Second mudra input, waiting for third.</summary>
    ThirdMudra,

    /// <summary>All mudras input, ready to execute Ninjutsu.</summary>
    ReadyToExecute
}
