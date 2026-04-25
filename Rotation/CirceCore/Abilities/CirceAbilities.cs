using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;

namespace Olympus.Rotation.CirceCore.Abilities;

/// <summary>
/// Declarative <see cref="AbilityBehavior"/> for every ability the Red Mage rotation fires.
/// Notes: melee combo step IDs (Riposte/Zwerchhau/Redoublement) pass the BASE id to UseAction;
/// the game upgrades to the Enchanted variant server-side. Same for Moulinet → Moulinet Deux/Trois.
/// </summary>
public static class CirceAbilities
{
    // --- Hardcast filler ---
    public static readonly AbilityBehavior Jolt = new() { Action = RDMActions.Jolt };
    public static readonly AbilityBehavior Jolt2 = new() { Action = RDMActions.Jolt2 };
    public static readonly AbilityBehavior Jolt3 = new() { Action = RDMActions.Jolt3 };

    // --- Dualcast consumers ---
    public static readonly AbilityBehavior Verthunder = new() { Action = RDMActions.Verthunder };
    public static readonly AbilityBehavior Veraero = new() { Action = RDMActions.Veraero };
    public static readonly AbilityBehavior Verthunder3 = new() { Action = RDMActions.Verthunder3 };
    public static readonly AbilityBehavior Veraero3 = new() { Action = RDMActions.Veraero3 };

    // --- Procs ---
    public static readonly AbilityBehavior Verfire = new() { Action = RDMActions.Verfire, Toggle = cfg => cfg.RedMage.EnableProcs };
    public static readonly AbilityBehavior Verstone = new() { Action = RDMActions.Verstone, Toggle = cfg => cfg.RedMage.EnableProcs };

    // --- Melee combo (pass BASE ids — UseAction rejects Enchanted replacement ids) ---
    public static readonly AbilityBehavior Riposte = new() { Action = RDMActions.Riposte };
    public static readonly AbilityBehavior Zwerchhau = new() { Action = RDMActions.Zwerchhau };
    public static readonly AbilityBehavior Redoublement = new() { Action = RDMActions.Redoublement };

    // --- Finishers ---
    public static readonly AbilityBehavior Verflare = new() { Action = RDMActions.Verflare, Toggle = cfg => cfg.RedMage.EnableFinisherCombo };
    public static readonly AbilityBehavior Verholy = new() { Action = RDMActions.Verholy, Toggle = cfg => cfg.RedMage.EnableFinisherCombo };
    public static readonly AbilityBehavior Scorch = new() { Action = RDMActions.Scorch };
    public static readonly AbilityBehavior Resolution = new() { Action = RDMActions.Resolution };
    public static readonly AbilityBehavior GrandImpact = new() { Action = RDMActions.GrandImpact, Toggle = cfg => cfg.RedMage.EnableGrandImpact };

    // --- AoE ---
    public static readonly AbilityBehavior Verthunder2 = new() { Action = RDMActions.Verthunder2, Toggle = cfg => cfg.RedMage.EnableAoERotation };
    public static readonly AbilityBehavior Veraero2 = new() { Action = RDMActions.Veraero2, Toggle = cfg => cfg.RedMage.EnableAoERotation };
    public static readonly AbilityBehavior Impact = new() { Action = RDMActions.Impact, Toggle = cfg => cfg.RedMage.EnableAoERotation };
    public static readonly AbilityBehavior EnchantedMoulinet = new() { Action = RDMActions.EnchantedMoulinet, Toggle = cfg => cfg.RedMage.EnableMeleeCombo };
    public static readonly AbilityBehavior EnchantedMoulinetDeux = new() { Action = RDMActions.EnchantedMoulinetDeux };
    public static readonly AbilityBehavior EnchantedMoulinetTrois = new() { Action = RDMActions.EnchantedMoulinetTrois };

    // --- oGCD damage ---
    public static readonly AbilityBehavior Fleche = new() { Action = RDMActions.Fleche, Toggle = cfg => cfg.RedMage.EnableFleche };
    public static readonly AbilityBehavior ContreSixte = new() { Action = RDMActions.ContreSixte, Toggle = cfg => cfg.RedMage.EnableContreSixte };
    public static readonly AbilityBehavior CorpsACorps = new() { Action = RDMActions.CorpsACorps, Toggle = cfg => cfg.RedMage.EnableCorpsACorps };
    public static readonly AbilityBehavior Engagement = new() { Action = RDMActions.Engagement, Toggle = cfg => cfg.RedMage.EnableEngagement };
    public static readonly AbilityBehavior ViceOfThorns = new() { Action = RDMActions.ViceOfThorns, Toggle = cfg => cfg.RedMage.EnableViceOfThorns };
    public static readonly AbilityBehavior Prefulgence = new() { Action = RDMActions.Prefulgence, Toggle = cfg => cfg.RedMage.EnablePrefulgence };

    // --- Buffs ---
    public static readonly AbilityBehavior Embolden = new() { Action = RDMActions.Embolden, Toggle = cfg => cfg.RedMage.EnableEmbolden };
    public static readonly AbilityBehavior Manafication = new() { Action = RDMActions.Manafication, Toggle = cfg => cfg.RedMage.EnableManafication };
    public static readonly AbilityBehavior Acceleration = new() { Action = RDMActions.Acceleration, Toggle = cfg => cfg.RedMage.EnableAcceleration };

    // --- Resurrection ---
    public static readonly AbilityBehavior Verraise = new() { Action = RDMActions.Verraise, Toggle = cfg => cfg.RedMage.EnableVerraise };

    // --- Role ---
    public static readonly AbilityBehavior Swiftcast = new() { Action = RoleActions.Swiftcast };
    public static readonly AbilityBehavior LucidDreaming = new() { Action = RoleActions.LucidDreaming, Toggle = cfg => cfg.RedMage.EnableLucidDreaming };
}
