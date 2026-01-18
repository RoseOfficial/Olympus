using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.HecateCore.Context;

namespace Olympus.Rotation.HecateCore.Modules;

/// <summary>
/// Handles the Black Mage damage rotation.
/// Manages Fire/Ice phase transitions, Polyglot spending, and proc usage.
/// </summary>
public sealed class DamageModule : IHecateModule
{
    public int Priority => 30; // Lower priority than buffs (higher number = lower priority)
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    // MP thresholds
    private const int Fire4MpCost = 800;
    private const int DespairMpCost = 800;

    // Timer thresholds
    private const float ElementRefreshThreshold = 6f;
    private const float ThunderRefreshThreshold = 3f;

    public bool TryExecute(IHecateContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.DamageState = "Not in combat";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Find target
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.CasterTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return false;
        }

        // Count nearby enemies for AoE decisions
        var enemyCount = context.TargetingService.CountEnemiesInRange(8f, player);
        context.Debug.NearbyEnemies = enemyCount;

        if (!context.CanExecuteGcd)
        {
            context.Debug.DamageState = "GCD not ready";
            return false;
        }

        var useAoe = enemyCount >= AoeThreshold;

        // === MOVEMENT HANDLING ===
        if (isMoving && !context.HasInstantCast && !context.CanSlidecast)
        {
            if (TryMovementAction(context, target, useAoe))
                return true;
        }

        // === FLARE STAR (Lv.100 finisher) ===
        if (TryFlareStar(context, target))
            return true;

        // === PROC HANDLING (Firestarter, Thunderhead) ===
        if (TryProcs(context, target, useAoe))
            return true;

        // === POLYGLOT SPENDING ===
        if (TryPolyglot(context, target, useAoe, isMoving))
            return true;

        // === MAIN ROTATION ===
        if (context.InAstralFire)
        {
            // Fire Phase
            if (TryFirePhase(context, target, useAoe))
                return true;
        }
        else if (context.InUmbralIce)
        {
            // Ice Phase
            if (TryIcePhase(context, target, useAoe))
                return true;
        }
        else
        {
            // No element - start Fire phase
            if (TryStartRotation(context, target, useAoe))
                return true;
        }

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(IHecateContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Movement Handling

    private bool TryMovementAction(IHecateContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        // Priority 1: Xenoglossy (instant, high damage)
        if (context.PolyglotStacks > 0 && level >= BLMActions.Xenoglossy.MinLevel && !useAoe)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Xenoglossy, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Xenoglossy.Name;
                context.Debug.DamageState = "Xenoglossy (movement)";
                return true;
            }
        }

        // Priority 2: Foul for AoE
        if (context.PolyglotStacks > 0 && level >= BLMActions.Foul.MinLevel && useAoe)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Foul, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Foul.Name;
                context.Debug.DamageState = "Foul (movement AoE)";
                return true;
            }
        }

        // Priority 3: Firestarter proc (instant Fire III)
        if (context.HasFirestarter)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Fire3, target.GameObjectId))
            {
                context.Debug.PlannedAction = "Fire III (Firestarter)";
                context.Debug.DamageState = "Firestarter proc (movement)";
                return true;
            }
        }

        // Priority 4: Thunderhead proc (instant Thunder)
        if (context.HasThunderhead)
        {
            var thunderAction = useAoe ? BLMActions.GetThunderAoe(level) : BLMActions.GetThunderST(level);
            if (context.ActionService.ExecuteGcd(thunderAction, target.GameObjectId))
            {
                context.Debug.PlannedAction = thunderAction.Name;
                context.Debug.DamageState = "Thunderhead proc (movement)";
                return true;
            }
        }

        // Priority 5: Paradox in Umbral Ice III (instant)
        if (context.HasParadox && context.InUmbralIce && context.UmbralIceStacks == 3 && level >= BLMActions.Paradox.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Paradox, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Paradox.Name;
                context.Debug.DamageState = "Paradox (movement)";
                return true;
            }
        }

        // Priority 6: Scathe (last resort)
        if (level >= BLMActions.Scathe.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Scathe, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Scathe.Name;
                context.Debug.DamageState = "Scathe (emergency movement)";
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Flare Star

    private bool TryFlareStar(IHecateContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BLMActions.FlareStar.MinLevel)
            return false;

        // Requires 6 Astral Soul stacks
        if (context.AstralSoulStacks < 6)
            return false;

        if (!context.ActionService.IsActionReady(BLMActions.FlareStar.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(BLMActions.FlareStar, target.GameObjectId))
        {
            context.Debug.PlannedAction = BLMActions.FlareStar.Name;
            context.Debug.DamageState = "Flare Star (6 stacks)";
            return true;
        }

        return false;
    }

    #endregion

    #region Procs

    private bool TryProcs(IHecateContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        // Use Firestarter if about to expire
        if (context.HasFirestarter && context.FirestarterRemaining < 5f)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Fire3, target.GameObjectId))
            {
                context.Debug.PlannedAction = "Fire III (Firestarter)";
                context.Debug.DamageState = $"Firestarter expiring ({context.FirestarterRemaining:F1}s)";
                return true;
            }
        }

        // Use Thunderhead if about to expire or DoT needs refresh
        if (context.HasThunderhead && (context.ThunderheadRemaining < 5f || context.ThunderDoTRemaining < ThunderRefreshThreshold))
        {
            var thunderAction = useAoe ? BLMActions.GetThunderAoe(level) : BLMActions.GetThunderST(level);
            if (context.ActionService.ExecuteGcd(thunderAction, target.GameObjectId))
            {
                context.Debug.PlannedAction = thunderAction.Name;
                context.Debug.DamageState = $"Thunderhead expiring ({context.ThunderheadRemaining:F1}s)";
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Polyglot

    private bool TryPolyglot(IHecateContext context, IBattleChara target, bool useAoe, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        if (context.PolyglotStacks == 0)
            return false;

        var maxPolyglot = level >= 98 ? 3 : 2;

        // Use Polyglot if at max stacks (avoid overcapping)
        if (context.PolyglotStacks >= maxPolyglot)
        {
            var action = (useAoe && level >= BLMActions.Foul.MinLevel) ? BLMActions.Foul :
                         (level >= BLMActions.Xenoglossy.MinLevel) ? BLMActions.Xenoglossy : BLMActions.Foul;

            if (level >= action.MinLevel && context.ActionService.ExecuteGcd(action, target.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (cap avoidance)";
                return true;
            }
        }

        // Use for movement if needed
        if (isMoving && !context.HasInstantCast)
        {
            var action = (useAoe && level >= BLMActions.Foul.MinLevel) ? BLMActions.Foul :
                         (level >= BLMActions.Xenoglossy.MinLevel) ? BLMActions.Xenoglossy : BLMActions.Foul;

            if (level >= action.MinLevel && context.ActionService.ExecuteGcd(action, target.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (movement)";
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Fire Phase

    private bool TryFirePhase(IHecateContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        context.Debug.Phase = "Fire";

        // Check element timer - transition to Ice if about to drop
        if (context.ElementTimer < 3f && context.ElementTimer > 0)
        {
            return TryTransitionToIce(context, target, useAoe);
        }

        // Use Paradox to refresh timer if available and timer is low
        if (context.HasParadox && context.ElementTimer < ElementRefreshThreshold && level >= BLMActions.Paradox.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Paradox, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Paradox.Name;
                context.Debug.DamageState = "Paradox (timer refresh)";
                return true;
            }
        }

        // AoE rotation
        if (useAoe)
        {
            return TryFireAoe(context, target);
        }

        // Single target Fire rotation
        return TryFireSingleTarget(context, target);
    }

    private bool TryFireSingleTarget(IHecateContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Cast Despair when low MP (finisher)
        if (level >= BLMActions.Despair.MinLevel && context.CurrentMp >= DespairMpCost && context.CurrentMp < Fire4MpCost * 2)
        {
            // Use Firestarter first if we have it
            if (context.HasFirestarter)
            {
                if (context.ActionService.ExecuteGcd(BLMActions.Fire3, target.GameObjectId))
                {
                    context.Debug.PlannedAction = "Fire III (Firestarter)";
                    context.Debug.DamageState = "Firestarter before Despair";
                    return true;
                }
            }

            if (context.ActionService.ExecuteGcd(BLMActions.Despair, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Despair.Name;
                context.Debug.DamageState = "Despair (finisher)";
                return true;
            }
        }

        // Spam Fire IV while we have MP
        if (level >= BLMActions.Fire4.MinLevel && context.CurrentMp >= Fire4MpCost)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Fire4, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Fire4.Name;
                context.Debug.DamageState = $"Fire IV (MP: {context.CurrentMp})";
                return true;
            }
        }

        // Fallback: transition to Ice if out of MP
        if (context.CurrentMp < DespairMpCost)
        {
            return TryTransitionToIce(context, target, false);
        }

        // Low level: use Fire I
        if (level < BLMActions.Fire4.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Fire, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Fire.Name;
                context.Debug.DamageState = "Fire (low level)";
                return true;
            }
        }

        return false;
    }

    private bool TryFireAoe(IHecateContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Use Flare when possible
        if (level >= BLMActions.Flare.MinLevel && context.CurrentMp >= 800)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Flare, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Flare.Name;
                context.Debug.DamageState = "Flare (AoE)";
                return true;
            }
        }

        // High Fire II / Fire II
        var fireAoe = BLMActions.GetFireAoe(level);
        if (level >= fireAoe.MinLevel && context.CurrentMp >= fireAoe.MpCost)
        {
            if (context.ActionService.ExecuteGcd(fireAoe, target.GameObjectId))
            {
                context.Debug.PlannedAction = fireAoe.Name;
                context.Debug.DamageState = $"{fireAoe.Name} (AoE)";
                return true;
            }
        }

        // Out of MP, transition to Ice
        return TryTransitionToIce(context, target, true);
    }

    private bool TryTransitionToIce(IHecateContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        var iceTransition = BLMActions.GetIceTransition(level);
        if (context.ActionService.ExecuteGcd(iceTransition, target.GameObjectId))
        {
            context.Debug.PlannedAction = iceTransition.Name;
            context.Debug.DamageState = "Transition to Ice";
            return true;
        }

        return false;
    }

    #endregion

    #region Ice Phase

    private bool TryIcePhase(IHecateContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        context.Debug.Phase = "Ice";

        // Priority 1: Get Umbral Hearts with Blizzard IV (requires UI3)
        if (context.UmbralHearts < 3 && context.UmbralIceStacks == 3 && level >= BLMActions.Blizzard4.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Blizzard4, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Blizzard4.Name;
                context.Debug.DamageState = "Blizzard IV (hearts)";
                return true;
            }
        }

        // Priority 2: Apply/refresh Thunder if needed
        if (!context.HasThunderDoT || context.ThunderDoTRemaining < ThunderRefreshThreshold)
        {
            if (context.HasThunderhead)
            {
                var thunderAction = useAoe ? BLMActions.GetThunderAoe(level) : BLMActions.GetThunderST(level);
                if (context.ActionService.ExecuteGcd(thunderAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = thunderAction.Name;
                    context.Debug.DamageState = "Thunder (DoT refresh)";
                    return true;
                }
            }
            else if (level >= BLMActions.Thunder.MinLevel)
            {
                // Hard cast Thunder if no Thunderhead proc
                var thunderAction = useAoe ? BLMActions.GetThunderAoe(level) : BLMActions.GetThunderST(level);
                if (context.ActionService.ExecuteGcd(thunderAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = thunderAction.Name;
                    context.Debug.DamageState = "Thunder (hard cast)";
                    return true;
                }
            }
        }

        // Priority 3: Use Paradox if available
        if (context.HasParadox && level >= BLMActions.Paradox.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Paradox, target.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Paradox.Name;
                context.Debug.DamageState = "Paradox (Ice phase)";
                return true;
            }
        }

        // Priority 4: Transition to Fire when MP is full and hearts are ready
        if (context.MpPercent >= 0.99f && context.UmbralHearts >= 3)
        {
            return TryTransitionToFire(context, target, useAoe);
        }

        // Transition to Fire when MP is full (lower level, no hearts mechanic)
        if (context.MpPercent >= 0.99f && level < BLMActions.Blizzard4.MinLevel)
        {
            return TryTransitionToFire(context, target, useAoe);
        }

        // AoE in Ice: Freeze or High Blizzard II for hearts
        if (useAoe && context.UmbralHearts < 3)
        {
            var iceAoe = BLMActions.GetIceAoe(level);
            if (level >= iceAoe.MinLevel && context.ActionService.ExecuteGcd(iceAoe, target.GameObjectId))
            {
                context.Debug.PlannedAction = iceAoe.Name;
                context.Debug.DamageState = $"{iceAoe.Name} (hearts)";
                return true;
            }
        }

        // Wait for MP regen - use filler if needed
        context.Debug.DamageState = $"Waiting for MP ({context.MpPercent * 100:F0}%)";
        return false;
    }

    private bool TryTransitionToFire(IHecateContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        // Use Firestarter if available for instant transition
        if (context.HasFirestarter)
        {
            if (context.ActionService.ExecuteGcd(BLMActions.Fire3, target.GameObjectId))
            {
                context.Debug.PlannedAction = "Fire III (Firestarter)";
                context.Debug.DamageState = "Transition to Fire (Firestarter)";
                return true;
            }
        }

        var fireTransition = BLMActions.GetFireTransition(level);
        if (context.ActionService.ExecuteGcd(fireTransition, target.GameObjectId))
        {
            context.Debug.PlannedAction = fireTransition.Name;
            context.Debug.DamageState = "Transition to Fire";
            return true;
        }

        return false;
    }

    #endregion

    #region Start Rotation

    private bool TryStartRotation(IHecateContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        context.Debug.Phase = "Starting";

        // Start with Fire III for full AF3
        var fireStarter = BLMActions.GetFireTransition(level);
        if (context.ActionService.ExecuteGcd(fireStarter, target.GameObjectId))
        {
            context.Debug.PlannedAction = fireStarter.Name;
            context.Debug.DamageState = "Start rotation (Fire)";
            return true;
        }

        return false;
    }

    #endregion
}
