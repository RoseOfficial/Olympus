using Olympus.Data;
using Olympus.Rotation.CirceCore.Context;

namespace Olympus.Rotation.CirceCore.Modules;

/// <summary>
/// Handles Red Mage oGCD buffs and abilities.
/// Manages Fleche, Contre Sixte, Embolden, Manafication, Corps-a-corps, Engagement, etc.
/// </summary>
public sealed class BuffModule : ICirceModule
{
    public int Priority => 20; // Higher priority than damage (lower number = higher priority)
    public string Name => "Buff";

    public bool TryExecute(ICirceContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return false;
        }

        if (!context.CanExecuteOgcd)
        {
            context.Debug.BuffState = "oGCD not ready";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Find target for damage oGCDs
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.CasterTargetingRange,
            player);

        // Priority 1: Fleche (use on cooldown - high damage single target)
        if (TryFleche(context, target))
            return true;

        // Priority 2: Contre Sixte (use on cooldown - AoE damage)
        if (TryContreSixte(context, target))
            return true;

        // Priority 3: Vice of Thorns (when Thorned Flourish is active)
        if (TryViceOfThorns(context, target))
            return true;

        // Priority 4: Prefulgence (when Prefulgence Ready is active)
        if (TryPrefulgence(context, target))
            return true;

        // Priority 5: Embolden (align with melee combo or burst windows)
        if (TryEmbolden(context))
            return true;

        // Priority 6: Manafication (at ~50|50 mana, before melee combo)
        if (TryManafication(context))
            return true;

        // Priority 7: Corps-a-corps (during melee combo or when capped on charges)
        if (TryCorpsACorps(context, target))
            return true;

        // Priority 8: Engagement (during melee combo or when capped on charges)
        if (TryEngagement(context, target))
            return true;

        // Priority 9: Acceleration (when no procs and not in melee combo)
        if (TryAcceleration(context))
            return true;

        // Priority 10: Lucid Dreaming (when MP < 70%)
        if (TryLucidDreaming(context))
            return true;

        context.Debug.BuffState = "No oGCD needed";
        return false;
    }

    public void UpdateDebugState(ICirceContext context)
    {
        // Debug state updated during TryExecute
    }

    #region oGCD Actions

    private bool TryFleche(ICirceContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.Fleche.MinLevel)
            return false;

        if (!context.FlecheReady)
            return false;

        // Use on cooldown - it's a high damage oGCD
        if (context.ActionService.ExecuteOgcd(RDMActions.Fleche, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Fleche.Name;
            context.Debug.BuffState = "Fleche";
            return true;
        }

        return false;
    }

    private bool TryContreSixte(ICirceContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.ContreSixte.MinLevel)
            return false;

        if (!context.ContreSixteReady)
            return false;

        // Use on cooldown - good single target and AoE damage
        if (context.ActionService.ExecuteOgcd(RDMActions.ContreSixte, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.ContreSixte.Name;
            context.Debug.BuffState = "Contre Sixte";
            return true;
        }

        return false;
    }

    private bool TryViceOfThorns(ICirceContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.ViceOfThorns.MinLevel)
            return false;

        // Requires Thorned Flourish buff
        if (!context.HasThornedFlourish)
            return false;

        if (context.ActionService.ExecuteOgcd(RDMActions.ViceOfThorns, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.ViceOfThorns.Name;
            context.Debug.BuffState = "Vice of Thorns";
            return true;
        }

        return false;
    }

    private bool TryPrefulgence(ICirceContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.Prefulgence.MinLevel)
            return false;

        // Requires Prefulgence Ready buff
        if (!context.HasPrefulgenceReady)
            return false;

        if (context.ActionService.ExecuteOgcd(RDMActions.Prefulgence, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Prefulgence.Name;
            context.Debug.BuffState = "Prefulgence";
            return true;
        }

        return false;
    }

    private bool TryEmbolden(ICirceContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.Embolden.MinLevel)
            return false;

        if (!context.EmboldenReady)
            return false;

        // Don't use if already active
        if (context.HasEmbolden)
            return false;

        // Best used just before melee combo for burst alignment
        // Use when about to enter melee combo (50|50 mana ready)
        if (!context.CanStartMeleeCombo)
        {
            // Check if close to melee combo entry
            var lowerMana = context.LowerMana;
            if (lowerMana < 40)
            {
                context.Debug.BuffState = "Hold Embolden for melee combo";
                return false;
            }
        }

        if (context.ActionService.ExecuteOgcd(RDMActions.Embolden, player.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Embolden.Name;
            context.Debug.BuffState = "Embolden (burst)";
            return true;
        }

        return false;
    }

    private bool TryManafication(ICirceContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.Manafication.MinLevel)
            return false;

        if (!context.ManaficationReady)
            return false;

        // Don't use if already active
        if (context.HasManafication)
            return false;

        // Don't use during melee combo
        if (context.IsInMeleeCombo)
            return false;

        // Don't use if already at 50|50 or higher (waste of mana)
        if (context.CanStartMeleeCombo)
            return false;

        // Best used when at 40-50 mana to double it for melee combo entry
        // At level 60-89, it adds 50|50
        // At level 90+, it adds 50|50 and grants 6 Manafication stacks
        var lowerMana = context.LowerMana;

        // Ideal: Use at 40-50 to get 80-100 mana
        // At minimum: Use at 25 to get 50 mana (melee combo threshold)
        if (lowerMana < 25)
        {
            context.Debug.BuffState = "Hold Manafication (low mana)";
            return false;
        }

        // Also prefer to use with Embolden for burst alignment
        if (context.EmboldenReady && lowerMana < 40)
        {
            context.Debug.BuffState = "Hold Manafication for Embolden";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(RDMActions.Manafication, player.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Manafication.Name;
            context.Debug.BuffState = $"Manafication (mana: {context.BlackMana}|{context.WhiteMana})";
            return true;
        }

        return false;
    }

    private bool TryCorpsACorps(ICirceContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.CorpsACorps.MinLevel)
            return false;

        if (context.CorpsACorpsCharges == 0)
            return false;

        // Use during melee combo for burst damage
        // Or use when capped on charges to avoid waste
        var inBurst = context.IsInMeleeCombo || context.HasEmbolden || context.HasManafication;
        var capped = context.CorpsACorpsCharges >= 2;

        if (!inBurst && !capped)
        {
            context.Debug.BuffState = "Hold Corps-a-corps for burst";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(RDMActions.CorpsACorps, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.CorpsACorps.Name;
            context.Debug.BuffState = $"Corps-a-corps ({context.CorpsACorpsCharges - 1} charges)";
            return true;
        }

        return false;
    }

    private bool TryEngagement(ICirceContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.Engagement.MinLevel)
            return false;

        if (context.EngagementCharges == 0)
            return false;

        // Use during melee combo for burst damage
        // Or use when capped on charges to avoid waste
        var inBurst = context.IsInMeleeCombo || context.HasEmbolden || context.HasManafication;
        var capped = context.EngagementCharges >= 2;

        if (!inBurst && !capped)
        {
            context.Debug.BuffState = "Hold Engagement for burst";
            return false;
        }

        // Note: Using Engagement instead of Displacement for safety
        // Displacement backsteps which can be dangerous in fights
        if (context.ActionService.ExecuteOgcd(RDMActions.Engagement, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Engagement.Name;
            context.Debug.BuffState = $"Engagement ({context.EngagementCharges - 1} charges)";
            return true;
        }

        return false;
    }

    private bool TryAcceleration(ICirceContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.Acceleration.MinLevel)
            return false;

        if (context.AccelerationCharges == 0)
            return false;

        // Don't use if already active
        if (context.HasAcceleration)
            return false;

        // Don't use during melee combo (procs aren't useful then)
        if (context.IsInMeleeCombo)
            return false;

        // Don't use if both procs are already active
        if (context.HasBothProcs)
        {
            context.Debug.BuffState = "Hold Acceleration (have procs)";
            return false;
        }

        // Use when no procs to guarantee one
        // Or use when capped on charges
        var noProcs = !context.HasVerfire && !context.HasVerstone;
        var capped = context.AccelerationCharges >= 2;

        if (!noProcs && !capped)
        {
            context.Debug.BuffState = "Hold Acceleration";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(RDMActions.Acceleration, player.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Acceleration.Name;
            context.Debug.BuffState = $"Acceleration ({context.AccelerationCharges - 1} charges)";
            return true;
        }

        return false;
    }

    private bool TryLucidDreaming(ICirceContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.LucidDreaming.MinLevel)
            return false;

        if (!context.LucidDreamingReady)
            return false;

        // Use when MP is below 70%
        if (context.MpPercent > 0.7f)
            return false;

        if (context.ActionService.ExecuteOgcd(RDMActions.LucidDreaming, player.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.LucidDreaming.Name;
            context.Debug.BuffState = "Lucid Dreaming (MP)";
            return true;
        }

        return false;
    }

    #endregion
}
