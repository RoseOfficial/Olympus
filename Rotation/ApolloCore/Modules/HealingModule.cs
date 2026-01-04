using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Services.Debuff;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// Handles all healing logic for the WHM rotation.
/// Includes single-target heals, AoE heals, Regen, Esuna, and emergency oGCD heals.
/// </summary>
public sealed class HealingModule : IApolloModule
{
    // Constants
    private const byte CureMinLevel = 2;
    private const byte MedicaMinLevel = 10;

    public int Priority => 10; // High priority for healing
    public string Name => "Healing";

    public bool TryExecute(ApolloContext context, bool isMoving)
    {
        // Priority 1: Emergency oGCD heal (Benediction)
        if (context.CanExecuteOgcd && TryExecuteBenediction(context))
            return true;

        // Priority 2: Esuna for lethal debuffs
        if (context.InCombat && TryExecuteEsuna(context, isMoving))
            return true;

        // Priority 3: AoE healing
        if (TryExecuteAoEHeal(context, isMoving))
            return true;

        // Priority 4: Single-target healing
        if (TryExecuteSingleHeal(context, isMoving))
            return true;

        // Priority 5: Regen maintenance
        if (context.InCombat && TryExecuteRegen(context, isMoving))
            return true;

        // Priority 6: oGCD single-target heal (Tetragrammaton)
        if (context.CanExecuteOgcd && TryExecuteTetragrammaton(context))
            return true;

        return false;
    }

    public void UpdateDebugState(ApolloContext context)
    {
        // Debug state is updated during execution
    }

    private bool TryExecuteAoEHeal(ApolloContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing)
        {
            context.Debug.AoEStatus = "Healing disabled";
            return false;
        }

        if (player.Level < MedicaMinLevel)
        {
            context.Debug.AoEStatus = $"Level {player.Level} < {MedicaMinLevel}";
            return false;
        }

        // Calculate heal amounts for overheal prevention
        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
        var medicaHealAmount = WHMActions.Medica.EstimateHealAmount(mind, det, wd, player.Level);
        var cureIIIHealAmount = WHMActions.CureIII.EstimateHealAmount(mind, det, wd, player.Level);

        // Count injured for self-centered AoE heals
        var (injuredCount, anyHaveRegen, allTargets, averageMissingHp) =
            context.PartyHelper.CountPartyMembersNeedingAoEHeal(player, medicaHealAmount);
        context.Debug.AoEInjuredCount = injuredCount;

        // Find best Cure III target
        var (cureIIITarget, cureIIITargetCount, cureIIITargetIds) =
            context.PartyHelper.FindBestCureIIITarget(player, cureIIIHealAmount);

        // Check if we have enough targets
        var hasEnoughSelfCenteredTargets = injuredCount >= config.AoEHealMinTargets;
        var hasEnoughCureIIITargets = cureIIITargetCount >= config.AoEHealMinTargets;

        if (!hasEnoughSelfCenteredTargets && !hasEnoughCureIIITargets)
        {
            context.Debug.AoEStatus = $"Injured {injuredCount} (self) / {cureIIITargetCount} (CureIII) < min {config.AoEHealMinTargets}";
            return false;
        }

        // Get best AoE heal from selector
        var (action, healAmount, selectedCureIIITarget) = context.HealingSpellSelector.SelectBestAoEHeal(
            player, averageMissingHp, injuredCount, anyHaveRegen, context.CanExecuteOgcd,
            cureIIITargetCount, cureIIITarget);

        if (action == null)
        {
            context.Debug.AoEStatus = "No AoE heal available";
            return false;
        }

        if (isMoving && action.CastTime > 0)
        {
            context.Debug.AoEStatus = "Moving";
            return false;
        }

        context.Debug.AoESelectedSpell = action.ActionId;
        context.Debug.AoEStatus = $"Executing {action.Name}" +
            (selectedCureIIITarget != null ? $" on {selectedCureIIITarget.Name}" : "");

        // Register pending heals
        List<uint> targetIds;
        if (selectedCureIIITarget != null)
        {
            targetIds = cureIIITargetIds;
        }
        else
        {
            targetIds = new List<uint>();
            foreach (var (entityId, _) in allTargets)
            {
                targetIds.Add(entityId);
            }
        }
        context.HpPredictionService.RegisterPendingAoEHeal(targetIds, healAmount);

        // Execute
        bool success;
        var executionTarget = selectedCureIIITarget?.GameObjectId ?? player.GameObjectId;

        if (action.Category == ActionCategory.oGCD)
        {
            success = context.ActionService.ExecuteOgcd(action, executionTarget);
        }
        else
        {
            if (action.MpCost >= 1000 && ShouldWaitForThinAir(context))
            {
                context.Debug.AoEStatus = "Waiting for Thin Air";
                context.HpPredictionService.ClearPendingHeals();
                return false;
            }

            success = context.ActionService.ExecuteGcd(action, executionTarget);
        }

        if (success)
        {
            var thinAirNote = StatusHelper.HasThinAir(player) ? " + Thin Air" : "";
            context.Debug.PlannedAction = action.Name + thinAirNote;
            context.Debug.PlanningState = "AoE Heal";
            var targetName = selectedCureIIITarget?.Name?.TextValue ?? player.Name?.TextValue ?? "Unknown";
            context.ActionTracker.LogAttempt(action.ActionId, targetName, player.CurrentHp, ActionResult.Success, player.Level);
        }
        else
        {
            context.HpPredictionService.ClearPendingHeals();
        }

        return success;
    }

    private bool TryExecuteSingleHeal(ApolloContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing)
            return false;

        if (player.Level < CureMinLevel)
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        var hasFreecure = StatusHelper.HasFreecure(player);
        var hasRegen = StatusHelper.HasRegenActive(target, out var regenRemaining);

        var (action, healAmount) = context.HealingSpellSelector.SelectBestSingleHeal(
            player, target, context.CanExecuteOgcd, hasFreecure, hasRegen, regenRemaining);
        if (action == null)
            return false;

        if (isMoving && action.CastTime > 0)
            return false;

        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
        var healAmountRaw = action.EstimateHealAmountRaw(mind, det, wd, player.Level);
        context.Debug.LastHealAmount = healAmount;
        context.Debug.LastHealStats = $"MND:{mind} DET:{det} WD:{wd} Lv:{player.Level} Pot:{action.HealPotency}";
        context.HpPredictionService.RegisterPendingHeal(target.EntityId, healAmount);

        if (action.HealPotency > 0)
            context.CombatEventService.RegisterPredictionForCalibration(healAmountRaw);

        bool success;
        if (action.Category == ActionCategory.oGCD)
        {
            success = context.ActionService.ExecuteOgcd(action, target.GameObjectId);
        }
        else
        {
            if (action.MpCost >= 1000 && ShouldWaitForThinAir(context))
            {
                context.HpPredictionService.ClearPendingHeals();
                return false;
            }

            success = context.ActionService.ExecuteGcd(action, target.GameObjectId);
        }

        if (success)
        {
            var thinAirNote = StatusHelper.HasThinAir(player) ? " + Thin Air" : "";
            context.Debug.PlannedAction = action.Name + thinAirNote;
            context.Debug.PlanningState = "Single Heal";
            context.ActionTracker.LogAttempt(action.ActionId, target.Name?.TextValue ?? "Unknown", target.CurrentHp, ActionResult.Success, player.Level);
        }
        else
        {
            context.HpPredictionService.ClearPendingHeals();
        }

        return success;
    }

    private bool TryExecuteRegen(ApolloContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.EnableRegen)
            return false;

        if (player.Level < WHMActions.Regen.MinLevel)
            return false;

        var target = context.PartyHelper.FindRegenTarget(player, GameConstants.RegenHpThreshold, GameConstants.RegenRefreshThreshold);
        if (target == null)
            return false;

        if (isMoving && WHMActions.Regen.CastTime > 0)
            return false;

        var success = context.ActionService.ExecuteGcd(WHMActions.Regen, target.GameObjectId);
        if (success)
        {
            context.Debug.PlannedAction = "Regen (tank priority)";
            context.Debug.PlanningState = "Regen";
            context.ActionTracker.LogAttempt(WHMActions.Regen.ActionId, target.Name?.TextValue ?? "Unknown", target.CurrentHp, ActionResult.Success, player.Level);
        }

        return success;
    }

    private bool TryExecuteEsuna(ApolloContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableEsuna)
        {
            context.Debug.EsunaState = "Disabled";
            return false;
        }

        if (player.Level < WHMActions.Esuna.MinLevel)
        {
            context.Debug.EsunaState = $"Level {player.Level} < {WHMActions.Esuna.MinLevel}";
            return false;
        }

        if (player.CurrentMp < WHMActions.Esuna.MpCost)
        {
            context.Debug.EsunaState = $"MP {player.CurrentMp} < {WHMActions.Esuna.MpCost}";
            return false;
        }

        var (target, statusId, priority) = FindBestEsunaTarget(context);
        if (target == null)
        {
            context.Debug.EsunaState = "No target";
            context.Debug.EsunaTarget = "None";
            return false;
        }

        if (priority != DebuffPriority.Lethal && (int)priority > config.EsunaPriorityThreshold)
        {
            context.Debug.EsunaState = $"Priority {priority} > threshold {config.EsunaPriorityThreshold}";
            return false;
        }

        if (isMoving)
        {
            context.Debug.EsunaState = "Moving";
            return false;
        }

        context.Debug.EsunaTarget = target.Name?.TextValue ?? "Unknown";
        context.Debug.EsunaState = $"Cleansing {priority} debuff";

        var success = context.ActionService.ExecuteGcd(WHMActions.Esuna, target.GameObjectId);
        if (success)
        {
            context.Debug.PlannedAction = "Esuna";
            context.Debug.PlanningState = "Esuna";
            context.ActionTracker.LogAttempt(WHMActions.Esuna.ActionId, target.Name?.TextValue ?? "Unknown", target.CurrentHp, ActionResult.Success, player.Level);
        }

        return success;
    }

    private (IBattleChara? target, uint statusId, DebuffPriority priority) FindBestEsunaTarget(ApolloContext context)
    {
        var player = context.Player;
        IBattleChara? bestTarget = null;
        uint bestStatusId = 0;
        var bestPriority = DebuffPriority.None;
        float bestRemainingTime = float.MaxValue;

        foreach (var member in context.PartyHelper.GetAllPartyMembers(player))
        {
            if (member.IsDead)
                continue;

            if (!DistanceHelper.IsInRange(player, member, WHMActions.Esuna.Range))
                continue;

            var (statusId, priority, remainingTime) = context.DebuffDetectionService.FindHighestPriorityDebuff(member);

            if (priority == DebuffPriority.None)
                continue;

            if (priority < bestPriority ||
                (priority == bestPriority && remainingTime < bestRemainingTime))
            {
                bestTarget = member;
                bestStatusId = statusId;
                bestPriority = priority;
                bestRemainingTime = remainingTime;
            }
        }

        return (bestTarget, bestStatusId, bestPriority);
    }

    private bool TryExecuteBenediction(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.EnableBenediction)
            return false;

        if (player.Level < WHMActions.Benediction.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(WHMActions.Benediction.ActionId))
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        if (hpPercent >= config.BenedictionEmergencyThreshold)
            return false;

        if (!DistanceHelper.IsInRange(player, target, WHMActions.Benediction.Range))
            return false;

        if (context.ActionService.ExecuteOgcd(WHMActions.Benediction, target.GameObjectId))
        {
            context.Debug.PlannedAction = "Benediction";
            context.ActionTracker.LogAttempt(WHMActions.Benediction.ActionId, target.Name?.TextValue ?? "Unknown",
                target.CurrentHp, ActionResult.Success, player.Level);

            var missingHp = (int)(target.MaxHp - target.CurrentHp);
            context.HpPredictionService.RegisterPendingHeal(target.EntityId, missingHp);
            return true;
        }

        return false;
    }

    private bool TryExecuteTetragrammaton(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.EnableTetragrammaton)
            return false;

        if (player.Level < WHMActions.Tetragrammaton.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(WHMActions.Tetragrammaton.ActionId))
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target == null)
            return false;

        if (!DistanceHelper.IsInRange(player, target, WHMActions.Tetragrammaton.Range))
            return false;

        var predictedHp = context.HpPredictionService.GetPredictedHp(target.EntityId, target.CurrentHp, target.MaxHp);
        var missingHp = (int)(target.MaxHp - predictedHp);

        if (missingHp <= 0)
            return false;

        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
        var healAmount = WHMActions.Tetragrammaton.EstimateHealAmount(mind, det, wd, player.Level);

        if (healAmount > missingHp * 1.5f)
            return false;

        if (context.ActionService.ExecuteOgcd(WHMActions.Tetragrammaton, target.GameObjectId))
        {
            context.Debug.PlannedAction = "Tetragrammaton";
            context.ActionTracker.LogAttempt(WHMActions.Tetragrammaton.ActionId, target.Name?.TextValue ?? "Unknown",
                target.CurrentHp, ActionResult.Success, player.Level);

            context.HpPredictionService.RegisterPendingHeal(target.EntityId, healAmount);
            return true;
        }

        return false;
    }

    private static bool ShouldWaitForThinAir(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableThinAir || player.Level < WHMActions.ThinAir.MinLevel)
            return false;

        if (StatusHelper.HasThinAir(player))
            return false;

        if (!context.ActionService.IsActionReady(WHMActions.ThinAir.ActionId))
            return false;

        return true;
    }
}
