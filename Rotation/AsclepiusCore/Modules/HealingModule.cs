using Olympus.Rotation.AsclepiusCore.Context;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Handles healing for Sage.
/// Coordinates four focused sub-modules in the following priority order:
///
/// oGCD window:
///   1. SingleTargetHealingModule  — Druochole, Taurochole
///   2. AoEHealingModule           — Ixochole, Kerachole, PhysisII
///   3. EmergencyHealingModule     — Holos, Haima, Panhaima, Pepsis, Rhizomata, Krasis, Zoe, LucidDreaming
///
/// GCD window:
///   1. AoEHealingModule.TryGcdPneuma    — Pneuma
///   2. ShieldHealingModule              — EukrasianHealing (E.Diagnosis / E.Prognosis)
///   3. AoEHealingModule.TryGcdPrognosis — Prognosis
///   4. SingleTargetHealingModule        — Diagnosis
/// </summary>
public sealed class HealingModule : IAsclepiusModule
{
    private readonly SingleTargetHealingModule _singleTarget = new();
    private readonly AoEHealingModule _aoe = new();
    private readonly ShieldHealingModule _shield = new();
    private readonly EmergencyHealingModule _emergency = new();

    public int Priority => 10; // High priority - healing is essential
    public string Name => "Healing";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
    {
        // Clear frame-scoped coordination state to allow new reservations
        context.HealingCoordination.Clear();

        // oGCD: Emergency Addersgall heals and cooldowns
        if (context.CanExecuteOgcd)
        {
            if (_singleTarget.TryOgcd(context)) return true;
            if (_aoe.TryOgcd(context)) return true;
            if (_emergency.TryOgcd(context)) return true;
        }

        // GCD: Healing spells
        if (context.CanExecuteGcd)
        {
            if (!isMoving && _aoe.TryGcdPneuma(context)) return true;
            if (_shield.TryGcd(context, isMoving)) return true;
            if (!isMoving && _aoe.TryGcdPrognosis(context)) return true;
            if (!isMoving && _singleTarget.TryGcd(context)) return true;
        }

        return false;
    }

    public void UpdateDebugState(IAsclepiusContext context)
    {
        context.Debug.AddersgallStacks = context.AddersgallStacks;
        context.Debug.AddersgallTimer = context.AddersgallTimer;
        context.Debug.AdderstingStacks = context.AdderstingStacks;

        // Update healing state based on party health
        var (avgHp, lowestHp, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(context.Player);
        context.Debug.AoEInjuredCount = injuredCount;
        context.Debug.PlayerHpPercent = context.Player.MaxHp > 0
            ? (float)context.Player.CurrentHp / context.Player.MaxHp
            : 1f;
    }
}
