using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.HermesCore.Context;
using Olympus.Rotation.HermesCore.Helpers;

namespace Olympus.Rotation.HermesCore.Modules;

/// <summary>
/// Handles Ninja mudra sequences and Ninjutsu execution.
/// Highest priority module - must complete mudra sequences once started.
/// </summary>
public sealed class NinjutsuModule : IHermesModule
{
    public int Priority => 10; // Highest priority - mudras must complete
    public string Name => "Ninjutsu";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    public bool TryExecute(IHermesContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.NinjutsuState = "Not in combat";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Find target
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            20f, // Ninjutsu has 20-25y range
            player);

        if (target == null && !context.MudraHelper.IsSequenceActive)
        {
            context.Debug.NinjutsuState = "No target";
            return false;
        }

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);

        // If we're in the middle of a mudra sequence, continue it
        if (context.MudraHelper.IsSequenceActive)
        {
            return ContinueMudraSequence(context, target);
        }

        // If mudra is on cooldown or we can't start a sequence, let other modules handle it
        if (!context.CanExecuteOgcd && !context.CanExecuteGcd)
        {
            context.Debug.NinjutsuState = "Waiting for action window";
            return false;
        }

        // Handle Ten Chi Jin mode
        if (context.HasTenChiJin)
        {
            return HandleTenChiJin(context, target, enemyCount);
        }

        // Decide whether to start a new Ninjutsu
        if (ShouldStartNinjutsu(context, level, enemyCount))
        {
            return StartNinjutsuSequence(context, target, enemyCount);
        }

        context.Debug.NinjutsuState = "No Ninjutsu needed";
        return false;
    }

    public void UpdateDebugState(IHermesContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Mudra Sequence

    private bool ContinueMudraSequence(IHermesContext context, IBattleChara? target)
    {
        var mudraHelper = context.MudraHelper;

        // Ready to execute the Ninjutsu
        if (mudraHelper.IsReadyToExecute)
        {
            if (context.CanExecuteGcd)
            {
                return ExecuteNinjutsu(context, target);
            }
            context.Debug.NinjutsuState = "Waiting for GCD to execute Ninjutsu";
            return true; // Block other modules
        }

        // Need to input more mudras
        if (context.CanExecuteOgcd)
        {
            return InputNextMudra(context);
        }

        context.Debug.NinjutsuState = $"Waiting to input mudra ({mudraHelper.MudraCount}/3)";
        return true; // Block other modules - we're mid-sequence
    }

    private bool InputNextMudra(IHermesContext context)
    {
        var mudraHelper = context.MudraHelper;
        var nextMudra = mudraHelper.GetNextMudra();

        if (nextMudra == NINActions.MudraType.None)
        {
            context.Debug.NinjutsuState = "Invalid mudra sequence";
            mudraHelper.Reset();
            return false;
        }

        var mudraAction = NINActions.GetMudraAction(nextMudra);

        if (!context.ActionService.IsActionReady(mudraAction.ActionId))
        {
            context.Debug.NinjutsuState = $"Waiting for {mudraAction.Name}";
            return true;
        }

        if (context.ActionService.ExecuteOgcd(mudraAction, context.Player.GameObjectId))
        {
            mudraHelper.AdvanceSequence();
            context.Debug.PlannedAction = mudraAction.Name;
            context.Debug.NinjutsuState = $"Input {mudraAction.Name} ({mudraHelper.MudraCount}/{mudraHelper.GetRequiredMudraCount()})";
            return true;
        }

        return false;
    }

    private bool ExecuteNinjutsu(IHermesContext context, IBattleChara? target)
    {
        var mudraHelper = context.MudraHelper;
        var targetNinjutsu = mudraHelper.TargetNinjutsu;

        // Get the Ninjutsu action ID based on what we're executing
        var ninjutsuAction = GetNinjutsuAction(targetNinjutsu, context.HasKassatsu, context.Player.Level);

        if (ninjutsuAction == null)
        {
            context.Debug.NinjutsuState = "Invalid Ninjutsu";
            mudraHelper.Reset();
            return false;
        }

        var targetId = target?.GameObjectId ?? context.Player.GameObjectId;

        // Self-targeted Ninjutsu (Huton, buffs)
        if (targetNinjutsu == NINActions.NinjutsuType.Huton)
        {
            targetId = context.Player.GameObjectId;
        }

        if (!context.ActionService.IsActionReady(ninjutsuAction.ActionId))
        {
            context.Debug.NinjutsuState = $"Waiting for {ninjutsuAction.Name}";
            return true;
        }

        if (context.ActionService.ExecuteGcd(ninjutsuAction, targetId))
        {
            context.Debug.PlannedAction = ninjutsuAction.Name;
            context.Debug.NinjutsuState = $"Executed {ninjutsuAction.Name}";
            mudraHelper.CompleteSequence();
            return true;
        }

        return false;
    }

    private static Models.Action.ActionDefinition? GetNinjutsuAction(
        NINActions.NinjutsuType ninjutsu,
        bool hasKassatsu,
        byte level)
    {
        // Kassatsu upgrades
        if (hasKassatsu)
        {
            return ninjutsu switch
            {
                NINActions.NinjutsuType.Katon or NINActions.NinjutsuType.GokaMekkyaku
                    when level >= NINActions.GokaMekkyaku.MinLevel => NINActions.GokaMekkyaku,
                NINActions.NinjutsuType.Hyoton or NINActions.NinjutsuType.HyoshoRanryu
                    when level >= NINActions.HyoshoRanryu.MinLevel => NINActions.HyoshoRanryu,
                NINActions.NinjutsuType.Raiton => NINActions.Raiton, // Kassatsu Raiton is just better damage
                _ => GetBaseNinjutsuAction(ninjutsu)
            };
        }

        return GetBaseNinjutsuAction(ninjutsu);
    }

    private static Models.Action.ActionDefinition? GetBaseNinjutsuAction(NINActions.NinjutsuType ninjutsu)
    {
        return ninjutsu switch
        {
            NINActions.NinjutsuType.FumaShuriken => NINActions.FumaShuriken,
            NINActions.NinjutsuType.Raiton => NINActions.Raiton,
            NINActions.NinjutsuType.Katon => NINActions.Katon,
            NINActions.NinjutsuType.Hyoton => NINActions.Hyoton,
            NINActions.NinjutsuType.Huton => NINActions.Huton,
            NINActions.NinjutsuType.Doton => NINActions.Doton,
            NINActions.NinjutsuType.Suiton => NINActions.Suiton,
            NINActions.NinjutsuType.GokaMekkyaku => NINActions.GokaMekkyaku,
            NINActions.NinjutsuType.HyoshoRanryu => NINActions.HyoshoRanryu,
            _ => null
        };
    }

    #endregion

    #region Start Ninjutsu

    private bool ShouldStartNinjutsu(IHermesContext context, byte level, int enemyCount)
    {
        // Can't use Ninjutsu below level 30
        if (level < NINActions.Ten.MinLevel)
            return false;

        // Check if mudra is on cooldown
        if (!context.ActionService.IsActionReady(NINActions.Ten.ActionId))
            return false;

        // Don't start during Kassatsu if we're not ready to burst
        if (context.HasKassatsu)
            return true; // Always use Kassatsu when available

        // Need Suiton for Kunai's Bane window
        if (NeedsSuiton(context, level))
            return true;

        // Standard Ninjutsu usage
        return context.CanExecuteOgcd;
    }

    private bool NeedsSuiton(IHermesContext context, byte level)
    {
        // Can't use Suiton below level 45
        if (level < NINActions.Suiton.MinLevel)
            return false;

        // Already have Suiton
        if (context.HasSuiton)
            return false;

        // Check if Kunai's Bane (or Trick Attack) is coming off cooldown soon
        var kunaiAction = level >= NINActions.KunaisBane.MinLevel
            ? NINActions.KunaisBane
            : NINActions.TrickAttack;

        // If Kunai's Bane/Trick Attack is ready or nearly ready, we need Suiton
        return context.ActionService.IsActionReady(kunaiAction.ActionId);
    }

    private bool StartNinjutsuSequence(IHermesContext context, IBattleChara? target, int enemyCount)
    {
        var level = context.Player.Level;
        var needsSuiton = NeedsSuiton(context, level);

        // Determine which Ninjutsu to use
        var ninjutsu = MudraHelper.GetRecommendedNinjutsu(
            level,
            context.HasKassatsu,
            needsSuiton,
            enemyCount);

        if (ninjutsu == NINActions.NinjutsuType.None)
        {
            context.Debug.NinjutsuState = "No recommended Ninjutsu";
            return false;
        }

        // Start the sequence
        context.MudraHelper.StartSequence(ninjutsu);
        context.Debug.NinjutsuState = $"Starting {ninjutsu}";

        // Input the first mudra
        return InputNextMudra(context);
    }

    #endregion

    #region Ten Chi Jin

    private bool HandleTenChiJin(IHermesContext context, IBattleChara? target, int enemyCount)
    {
        // Ten Chi Jin allows instant sequential Ninjutsu without mudra cooldowns
        // Standard sequence: Fuma Shuriken -> Raiton -> Suiton (or Katon for AoE)
        if (!context.CanExecuteGcd)
        {
            context.Debug.NinjutsuState = "TCJ: Waiting for GCD";
            return true;
        }

        var level = context.Player.Level;
        var stacks = context.TenChiJinStacks;
        var targetId = target?.GameObjectId ?? context.Player.GameObjectId;

        Models.Action.ActionDefinition? action = null;
        string actionName;

        // TCJ sequence based on remaining stacks
        if (stacks >= 3)
        {
            // First TCJ action - Ten (Fuma Shuriken)
            action = NINActions.FumaShuriken;
            actionName = "TCJ: Fuma Shuriken";
        }
        else if (stacks == 2)
        {
            // Second TCJ action - Chi (Raiton or Katon)
            if (enemyCount >= AoeThreshold)
            {
                action = NINActions.Katon;
                actionName = "TCJ: Katon";
            }
            else
            {
                action = NINActions.Raiton;
                actionName = "TCJ: Raiton";
            }
        }
        else if (stacks == 1)
        {
            // Third TCJ action - Jin (Suiton or Doton)
            if (enemyCount >= AoeThreshold)
            {
                action = NINActions.Doton;
                actionName = "TCJ: Doton";
            }
            else
            {
                action = NINActions.Suiton;
                actionName = "TCJ: Suiton";
            }
        }
        else
        {
            context.Debug.NinjutsuState = "TCJ complete";
            return false;
        }

        if (action != null && context.ActionService.IsActionReady(action.ActionId))
        {
            if (context.ActionService.ExecuteGcd(action, targetId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.NinjutsuState = actionName;
                return true;
            }
        }

        context.Debug.NinjutsuState = $"TCJ: Waiting for {action?.Name ?? "action"}";
        return true;
    }

    #endregion
}
