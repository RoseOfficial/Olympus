using Olympus.Data;
using Olympus.Rotation.HermesCore.Context;

namespace Olympus.Rotation.HermesCore.Modules;

/// <summary>
/// Handles Ninja buff management.
/// Manages Mug/Dokumori, Kassatsu, Ten Chi Jin, Bunshin, Meisui.
/// </summary>
public sealed class BuffModule : IHermesModule
{
    public int Priority => 20; // After Ninjutsu, before damage
    public string Name => "Buff";

    // Threshold for Ninki spending
    private const int NinkiThreshold = 50;

    public bool TryExecute(IHermesContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return false;
        }

        // Only use buff actions during oGCD windows
        if (!context.CanExecuteOgcd)
            return false;

        // Don't use buffs during mudra sequences
        if (context.IsMudraActive)
        {
            context.Debug.BuffState = "Mudra active";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Priority 1: Tenri Jindo (after Kunai's Bane)
        if (TryTenriJindo(context))
            return true;

        // Priority 2: Kunai's Bane / Trick Attack (main burst window)
        if (TryKunaisBane(context))
            return true;

        // Priority 3: Mug / Dokumori (damage + Ninki)
        if (TryMug(context))
            return true;

        // Priority 4: Kassatsu (enhanced Ninjutsu)
        if (TryKassatsu(context))
            return true;

        // Priority 5: Ten Chi Jin (triple Ninjutsu)
        if (TryTenChiJin(context))
            return true;

        // Priority 6: Bunshin (shadow clone)
        if (TryBunshin(context))
            return true;

        // Priority 7: Meisui (Suiton to Ninki conversion)
        if (TryMeisui(context))
            return true;

        return false;
    }

    public void UpdateDebugState(IHermesContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Kunai's Bane / Trick Attack

    private bool TryKunaisBane(IHermesContext context)
    {
        var player = context.Player;
        var level = player.Level;

        // Need Suiton buff for Kunai's Bane/Trick Attack
        if (!context.HasSuiton)
        {
            context.Debug.BuffState = "Need Suiton for burst";
            return false;
        }

        // Get the appropriate action
        var action = level >= NINActions.KunaisBane.MinLevel
            ? NINActions.KunaisBane
            : (level >= NINActions.TrickAttack.MinLevel ? NINActions.TrickAttack : null);

        if (action == null)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
        {
            context.Debug.BuffState = $"{action.Name} on cooldown";
            return false;
        }

        // Find target
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            action.Range,
            player);

        if (target == null)
            return false;

        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.BuffState = $"Activating {action.Name}";
            return true;
        }

        return false;
    }

    #endregion

    #region Tenri Jindo

    private bool TryTenriJindo(IHermesContext context)
    {
        var level = context.Player.Level;

        if (level < NINActions.TenriJindo.MinLevel)
            return false;

        if (!context.HasTenriJindoReady)
            return false;

        if (!context.ActionService.IsActionReady(NINActions.TenriJindo.ActionId))
        {
            context.Debug.BuffState = "Tenri Jindo not ready";
            return false;
        }

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            NINActions.TenriJindo.Range,
            context.Player);

        if (target == null)
            return false;

        if (context.ActionService.ExecuteOgcd(NINActions.TenriJindo, target.GameObjectId))
        {
            context.Debug.PlannedAction = NINActions.TenriJindo.Name;
            context.Debug.BuffState = "Tenri Jindo";
            return true;
        }

        return false;
    }

    #endregion

    #region Mug / Dokumori

    private bool TryMug(IHermesContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < NINActions.Mug.MinLevel)
            return false;

        var action = NINActions.GetMugAction(level);

        // Already have Dokumori debuff
        if (level >= NINActions.Dokumori.MinLevel && context.HasDokumoriOnTarget)
        {
            context.Debug.BuffState = $"Dokumori active ({context.DokumoriRemaining:F1}s)";
            return false;
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
        {
            context.Debug.BuffState = $"{action.Name} on cooldown";
            return false;
        }

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            action.Range,
            player);

        if (target == null)
            return false;

        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.BuffState = $"Activating {action.Name}";
            return true;
        }

        return false;
    }

    #endregion

    #region Kassatsu

    private bool TryKassatsu(IHermesContext context)
    {
        var level = context.Player.Level;

        if (level < NINActions.Kassatsu.MinLevel)
            return false;

        // Already have Kassatsu
        if (context.HasKassatsu)
        {
            context.Debug.BuffState = "Kassatsu active";
            return false;
        }

        if (!context.ActionService.IsActionReady(NINActions.Kassatsu.ActionId))
        {
            context.Debug.BuffState = "Kassatsu on cooldown";
            return false;
        }

        // Optimal: Use Kassatsu during burst window
        // But don't hold it too long
        if (context.ActionService.ExecuteOgcd(NINActions.Kassatsu, context.Player.GameObjectId))
        {
            context.Debug.PlannedAction = NINActions.Kassatsu.Name;
            context.Debug.BuffState = "Activating Kassatsu";
            return true;
        }

        return false;
    }

    #endregion

    #region Ten Chi Jin

    private bool TryTenChiJin(IHermesContext context)
    {
        var level = context.Player.Level;

        if (level < NINActions.TenChiJin.MinLevel)
            return false;

        // Already have TCJ active
        if (context.HasTenChiJin)
        {
            context.Debug.BuffState = $"TCJ active ({context.TenChiJinStacks} stacks)";
            return false;
        }

        // Don't use TCJ while moving
        if (context.IsMoving)
        {
            context.Debug.BuffState = "TCJ: Don't use while moving";
            return false;
        }

        if (!context.ActionService.IsActionReady(NINActions.TenChiJin.ActionId))
        {
            context.Debug.BuffState = "TCJ on cooldown";
            return false;
        }

        // Best used during burst window when Kunai's Bane is active
        // But don't hold too long
        if (context.ActionService.ExecuteOgcd(NINActions.TenChiJin, context.Player.GameObjectId))
        {
            context.Debug.PlannedAction = NINActions.TenChiJin.Name;
            context.Debug.BuffState = "Activating TCJ";
            return true;
        }

        return false;
    }

    #endregion

    #region Bunshin

    private bool TryBunshin(IHermesContext context)
    {
        var level = context.Player.Level;

        if (level < NINActions.Bunshin.MinLevel)
            return false;

        // Already have Bunshin
        if (context.HasBunshin)
        {
            context.Debug.BuffState = $"Bunshin active ({context.BunshinStacks} stacks)";
            return false;
        }

        // Need 50 Ninki for Bunshin
        if (context.Ninki < NinkiThreshold)
        {
            context.Debug.BuffState = $"Need {NinkiThreshold - context.Ninki} more Ninki for Bunshin";
            return false;
        }

        if (!context.ActionService.IsActionReady(NINActions.Bunshin.ActionId))
        {
            context.Debug.BuffState = "Bunshin on cooldown";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(NINActions.Bunshin, context.Player.GameObjectId))
        {
            context.Debug.PlannedAction = NINActions.Bunshin.Name;
            context.Debug.BuffState = "Activating Bunshin";
            return true;
        }

        return false;
    }

    #endregion

    #region Meisui

    private bool TryMeisui(IHermesContext context)
    {
        var level = context.Player.Level;

        if (level < NINActions.Meisui.MinLevel)
            return false;

        // Need Suiton for Meisui
        if (!context.HasSuiton)
        {
            return false;
        }

        // Don't use Meisui if Kunai's Bane is ready (save Suiton for it)
        var kunaiAction = level >= NINActions.KunaisBane.MinLevel
            ? NINActions.KunaisBane
            : NINActions.TrickAttack;
        if (context.ActionService.IsActionReady(kunaiAction.ActionId))
        {
            context.Debug.BuffState = "Save Suiton for burst";
            return false;
        }

        if (!context.ActionService.IsActionReady(NINActions.Meisui.ActionId))
        {
            context.Debug.BuffState = "Meisui on cooldown";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(NINActions.Meisui, context.Player.GameObjectId))
        {
            context.Debug.PlannedAction = NINActions.Meisui.Name;
            context.Debug.BuffState = "Activating Meisui";
            return true;
        }

        return false;
    }

    #endregion
}
