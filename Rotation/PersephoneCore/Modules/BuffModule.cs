using Olympus.Data;
using Olympus.Rotation.PersephoneCore.Context;

namespace Olympus.Rotation.PersephoneCore.Modules;

/// <summary>
/// Handles Summoner oGCD buffs and abilities.
/// Manages Enkindle, Energy Drain, Searing Light, Mountain Buster, and Astral Flow abilities.
/// </summary>
public sealed class BuffModule : IPersephoneModule
{
    public int Priority => 20; // Higher priority than damage (lower number = higher priority)
    public string Name => "Buff";

    public bool TryExecute(IPersephoneContext context, bool isMoving)
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

        // Priority 1: Enkindle during demi-summon phases (high damage)
        if (TryEnkindle(context, target))
            return true;

        // Priority 2: Astral Flow abilities (Deathflare/Sunflare for damage, Rekindle for heals)
        if (TryAstralFlow(context, target))
            return true;

        // Priority 3: Mountain Buster during Titan phase (use after each Topaz Rite)
        if (TryMountainBuster(context, target))
            return true;

        // Priority 4: Searing Light (align with demi-summon or raid buffs)
        if (TrySearingLight(context))
            return true;

        // Priority 5: Searing Flash (during Searing Light window)
        if (TrySearingFlash(context, target))
            return true;

        // Priority 6: Energy Drain (generate Aetherflow when empty)
        if (TryEnergyDrain(context, target))
            return true;

        // Priority 7: Necrotize/Fester (spend Aetherflow, prefer burst windows)
        if (TryAetherflowSpender(context, target))
            return true;

        // Priority 8: Lucid Dreaming (MP management)
        if (TryLucidDreaming(context))
            return true;

        context.Debug.BuffState = "No oGCD needed";
        return false;
    }

    public void UpdateDebugState(IPersephoneContext context)
    {
        // Debug state updated during TryExecute
    }

    #region oGCD Actions

    private bool TryEnkindle(IPersephoneContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        // Only use during demi-summon phases
        if (!context.IsDemiSummonActive)
            return false;

        // Check if we have a valid Enkindle action
        var enkindleAction = SMNActions.GetEnkindleAction(
            context.IsBahamutActive,
            context.IsPhoenixActive,
            context.IsSolarBahamutActive);

        if (enkindleAction == null || level < enkindleAction.MinLevel)
            return false;

        if (!context.EnkindleReady)
            return false;

        // Use Enkindle during demi-summon phase
        if (context.ActionService.ExecuteOgcd(enkindleAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = enkindleAction.Name;
            context.Debug.BuffState = $"{enkindleAction.Name} (Enkindle)";
            return true;
        }

        return false;
    }

    private bool TryAstralFlow(IPersephoneContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        var player = context.Player;
        var level = player.Level;

        // Only use during demi-summon phases
        if (!context.IsDemiSummonActive)
            return false;

        // Get the appropriate Astral Flow action
        var astralFlowAction = SMNActions.GetAstralFlowAction(
            context.IsBahamutActive,
            context.IsPhoenixActive,
            context.IsSolarBahamutActive);

        if (astralFlowAction == null || level < astralFlowAction.MinLevel)
            return false;

        if (!context.AstralFlowReady)
            return false;

        // For Rekindle (Phoenix), find a party member to heal
        if (context.IsPhoenixActive)
        {
            var rekindleTarget = context.PartyHelper.FindRekindleTarget(player, 0.9f);
            if (rekindleTarget != null)
            {
                if (context.ActionService.ExecuteOgcd(SMNActions.Rekindle, rekindleTarget.GameObjectId))
                {
                    context.Debug.PlannedAction = SMNActions.Rekindle.Name;
                    context.Debug.BuffState = "Rekindle (healing)";
                    return true;
                }
            }
            // If no one needs healing, still use it on self or lowest HP
            var lowestMember = context.PartyHelper.GetLowestHpMember(player) ?? player;
            if (context.ActionService.ExecuteOgcd(SMNActions.Rekindle, lowestMember.GameObjectId))
            {
                context.Debug.PlannedAction = SMNActions.Rekindle.Name;
                context.Debug.BuffState = "Rekindle (preventive)";
                return true;
            }
            return false;
        }

        // For Deathflare/Sunflare, use on enemy target
        if (target == null)
            return false;

        if (context.ActionService.ExecuteOgcd(astralFlowAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = astralFlowAction.Name;
            context.Debug.BuffState = $"{astralFlowAction.Name} (Astral Flow)";
            return true;
        }

        return false;
    }

    private bool TryMountainBuster(IPersephoneContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < SMNActions.MountainBuster.MinLevel)
            return false;

        // Mountain Buster requires Titan's Favor buff
        if (!context.HasTitansFavor)
            return false;

        // Use Mountain Buster immediately when available
        if (context.ActionService.ExecuteOgcd(SMNActions.MountainBuster, target.GameObjectId))
        {
            context.Debug.PlannedAction = SMNActions.MountainBuster.Name;
            context.Debug.BuffState = "Mountain Buster";
            return true;
        }

        return false;
    }

    private bool TrySearingLight(IPersephoneContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < SMNActions.SearingLight.MinLevel)
            return false;

        if (!context.SearingLightReady)
            return false;

        // Don't use if already active
        if (context.HasSearingLight)
            return false;

        // Best used during demi-summon phases for burst alignment
        // Also good to align with party buffs (2-minute windows)
        if (!context.IsDemiSummonActive)
        {
            context.Debug.BuffState = "Hold Searing Light for demi";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(SMNActions.SearingLight, player.GameObjectId))
        {
            context.Debug.PlannedAction = SMNActions.SearingLight.Name;
            context.Debug.BuffState = "Searing Light (burst)";
            return true;
        }

        return false;
    }

    private bool TrySearingFlash(IPersephoneContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < SMNActions.SearingFlash.MinLevel)
            return false;

        // Requires Searing Light to be active
        if (!context.HasSearingLight)
            return false;

        // Check if action is ready (should be once per Searing Light)
        if (!context.ActionService.IsActionReady(SMNActions.SearingFlash.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(SMNActions.SearingFlash, target.GameObjectId))
        {
            context.Debug.PlannedAction = SMNActions.SearingFlash.Name;
            context.Debug.BuffState = "Searing Flash";
            return true;
        }

        return false;
    }

    private bool TryEnergyDrain(IPersephoneContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < SMNActions.EnergyDrain.MinLevel)
            return false;

        if (!context.EnergyDrainReady)
            return false;

        // Only use when Aetherflow is empty
        if (context.HasAetherflow)
        {
            context.Debug.BuffState = "Have Aetherflow, hold Energy Drain";
            return false;
        }

        // Count enemies for AoE version
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        var useAoe = enemyCount >= 3 && level >= SMNActions.EnergySiphon.MinLevel;
        var action = useAoe ? SMNActions.EnergySiphon : SMNActions.EnergyDrain;

        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.BuffState = $"{action.Name} (+2 Aetherflow)";
            return true;
        }

        return false;
    }

    private bool TryAetherflowSpender(IPersephoneContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        // Need Aetherflow stacks to spend
        if (!context.HasAetherflow)
            return false;

        // Prefer to spend during burst windows (demi-summon + Searing Light)
        // But always spend before Energy Drain comes off cooldown
        var energyDrainSoon = !context.EnergyDrainReady &&
                              context.ActionService.GetCooldownRemaining(SMNActions.EnergyDrain.ActionId) < 5f;

        // During burst, spend freely
        var inBurst = context.IsDemiSummonActive || context.HasSearingLight;

        // Must spend if Energy Drain is coming off cooldown soon
        if (!inBurst && !energyDrainSoon)
        {
            context.Debug.BuffState = "Hold Aetherflow for burst";
            return false;
        }

        // Count enemies for AoE decision
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        var useAoe = enemyCount >= 3;
        var action = SMNActions.GetAetherflowSpenderAoe(level);
        if (!useAoe)
            action = SMNActions.GetAetherflowSpenderST(level);

        if (level < action.MinLevel)
            return false;

        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.BuffState = $"{action.Name} (Aetherflow: {context.AetherflowStacks - 1})";
            return true;
        }

        return false;
    }

    private bool TryLucidDreaming(IPersephoneContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < SMNActions.LucidDreaming.MinLevel)
            return false;

        if (!context.LucidDreamingReady)
            return false;

        // Use when MP is below 70%
        if (context.MpPercent > 0.7f)
            return false;

        if (context.ActionService.ExecuteOgcd(SMNActions.LucidDreaming, player.GameObjectId))
        {
            context.Debug.PlannedAction = SMNActions.LucidDreaming.Name;
            context.Debug.BuffState = "Lucid Dreaming (MP)";
            return true;
        }

        return false;
    }

    #endregion
}
