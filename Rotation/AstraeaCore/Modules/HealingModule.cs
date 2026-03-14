using Olympus.Rotation.AstraeaCore.Context;

namespace Olympus.Rotation.AstraeaCore.Modules;

/// <summary>
/// Coordinates all healing logic for the Astrologian rotation.
/// Delegates to sub-modules: SingleTarget, AoE, Shield, and Emergency.
/// </summary>
public sealed class HealingModule : IAstraeaModule
{
    public int Priority => 10; // High priority for healing
    public string Name => "Healing";

    private readonly SingleTargetHealingModule _singleTarget = new();
    private readonly AoEHealingModule _aoe = new();
    private readonly ShieldHealingModule _shield = new();
    private readonly EmergencyHealingModule _emergency = new();

    public bool TryExecute(IAstraeaContext context, bool isMoving)
    {
        // Clear frame-scoped coordination state to allow new reservations
        context.HealingCoordination.Clear();

        var config = context.Configuration;

        if (!config.EnableHealing)
            return false;

        // oGCD heals first (free healing, no GCD cost)
        if (context.CanExecuteOgcd)
        {
            // Priority 1-2: Essential Dignity, Celestial Intersection
            if (_singleTarget.TryOgcd(context)) return true;

            // Priority 3: Celestial Opposition
            if (_aoe.TryOgcd(context)) return true;

            // Priority 4: Exaltation
            if (_shield.TryOgcd(context)) return true;

            // Priority 5-10: Horoscope detonation, Microcosmos, Earthly Star detonation,
            //                 Synastry, Earthly Star placement, Lady of Crowns
            if (_emergency.TryOgcd(context)) return true;
        }

        // GCD heals
        if (context.CanExecuteGcd)
        {
            // Priority 11-12: Horoscope preparation, Macrocosmos preparation
            if (_emergency.TryGcd(context, isMoving)) return true;

            // Priority 13: AoE healing (Helios/Aspected Helios/Helios Conjunction)
            if (!isMoving && _aoe.TryGcd(context)) return true;

            // Priority 14-15: Aspected Benefic, Single-target healing
            if (_singleTarget.TryGcd(context, isMoving)) return true;
        }

        return false;
    }

    public void UpdateDebugState(IAstraeaContext context)
    {
        var (avgHp, lowestHp, injured) = context.PartyHealthMetrics;
        context.Debug.AoEInjuredCount = injured;
        context.Debug.PlayerHpPercent = context.Player.MaxHp > 0
            ? (float)context.Player.CurrentHp / context.Player.MaxHp
            : 1f;
    }
}
