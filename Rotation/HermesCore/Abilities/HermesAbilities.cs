using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.HermesCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for Ninja abilities pushed through the scheduler.
/// Mudras, ninjutsu execution, and Ten Chi Jin sequence are NOT pushed here — they bypass
/// the scheduler and use raw ActionManager directly because UseAction rejects the replacement
/// IDs that the ninjutsu chain produces (see CLAUDE.md NIN ninjutsu/mudra notes). Those live
/// in NinjutsuModule's CollectCandidates as direct dispatch.
/// </summary>
public static class HermesAbilities
{
    // --- Single-target combo ---
    public static readonly AbilityBehavior SpinningEdge = new() { Action = NINActions.SpinningEdge };
    public static readonly AbilityBehavior GustSlash = new() { Action = NINActions.GustSlash };
    public static readonly AbilityBehavior AeolianEdge = new() { Action = NINActions.AeolianEdge };
    public static readonly AbilityBehavior ArmorCrush = new() { Action = NINActions.ArmorCrush };

    // --- AoE combo ---
    public static readonly AbilityBehavior DeathBlossom = new() { Action = NINActions.DeathBlossom, Toggle = cfg => cfg.Ninja.EnableAoERotation };
    public static readonly AbilityBehavior HakkeMujinsatsu = new() { Action = NINActions.HakkeMujinsatsu, Toggle = cfg => cfg.Ninja.EnableAoERotation };

    // --- Procs ---
    public static readonly AbilityBehavior ForkedRaiju = new() { Action = NINActions.ForkedRaiju, Toggle = cfg => cfg.Ninja.EnableRaiju };
    public static readonly AbilityBehavior FleetingRaiju = new() { Action = NINActions.FleetingRaiju, Toggle = cfg => cfg.Ninja.EnableRaiju };
    public static readonly AbilityBehavior PhantomKamaitachi = new() { Action = NINActions.PhantomKamaitachi, Toggle = cfg => cfg.Ninja.EnablePhantomKamaitachi };

    // --- Ninki spenders ---
    public static readonly AbilityBehavior Bhavacakra = new() { Action = NINActions.Bhavacakra, Toggle = cfg => cfg.Ninja.EnableBhavacakra };
    public static readonly AbilityBehavior HellfrogMedium = new() { Action = NINActions.HellfrogMedium, Toggle = cfg => cfg.Ninja.EnableHellfrogMedium };
    public static readonly AbilityBehavior ZeshoMeppo = new() { Action = NINActions.ZeshoMeppo, Toggle = cfg => cfg.Ninja.EnableBhavacakra };
    public static readonly AbilityBehavior DeathfrogMedium = new() { Action = NINActions.DeathfrogMedium, Toggle = cfg => cfg.Ninja.EnableHellfrogMedium };

    // --- Burst window oGCDs ---
    public static readonly AbilityBehavior KunaisBane = new() { Action = NINActions.KunaisBane, Toggle = cfg => cfg.Ninja.EnableKunaisBane };
    public static readonly AbilityBehavior TrickAttack = new() { Action = NINActions.TrickAttack, Toggle = cfg => cfg.Ninja.EnableKunaisBane };
    public static readonly AbilityBehavior TenriJindo = new() { Action = NINActions.TenriJindo, Toggle = cfg => cfg.Ninja.EnableTenriJindo };
    public static readonly AbilityBehavior Mug = new() { Action = NINActions.Mug, Toggle = cfg => cfg.Ninja.EnableMug };
    public static readonly AbilityBehavior Dokumori = new() { Action = NINActions.Dokumori, Toggle = cfg => cfg.Ninja.EnableMug };
    public static readonly AbilityBehavior Kassatsu = new() { Action = NINActions.Kassatsu, Toggle = cfg => cfg.Ninja.EnableKassatsu };
    public static readonly AbilityBehavior TenChiJin = new() { Action = NINActions.TenChiJin, Toggle = cfg => cfg.Ninja.EnableTenChiJin };
    public static readonly AbilityBehavior Bunshin = new() { Action = NINActions.Bunshin, Toggle = cfg => cfg.Ninja.EnableBunshin };
    public static readonly AbilityBehavior Meisui = new() { Action = NINActions.Meisui, Toggle = cfg => cfg.Ninja.EnableMeisui };

    // --- Role ---
    public static readonly AbilityBehavior Feint = new() { Action = RoleActions.Feint, Toggle = cfg => cfg.Ninja.EnableFeint };
}
