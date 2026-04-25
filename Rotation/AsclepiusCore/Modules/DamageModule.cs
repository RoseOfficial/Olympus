using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// SGE-specific damage module (scheduler-driven).
/// Eukrasia activation for DoT bypasses the scheduler (direct dispatch) for the same
/// reason as ShieldHealingHandler — Eukrasia must fire during the GCD pass and the
/// scheduler's CanExecuteOgcd queue gate would block it.
/// </summary>
public sealed class DamageModule : BaseDamageModule<IAsclepiusContext>, IAsclepiusModule
{
    protected override bool IsDamageEnabled(IAsclepiusContext context) => context.Configuration.EnableDamage;
    protected override bool IsDoTEnabled(IAsclepiusContext context) => context.Configuration.EnableDoT;
    protected override bool IsAoEDamageEnabled(IAsclepiusContext context) => context.Configuration.Sage.EnableAoEDamage;
    protected override int AoEMinTargets(IAsclepiusContext context) => context.Configuration.Sage.AoEDamageMinTargets;
    protected override float DoTRefreshThreshold(IAsclepiusContext context) => FFXIVConstants.DotRefreshThreshold;
    protected override uint GetDoTStatusId(IAsclepiusContext context) => SGEActions.GetDotStatusId(context.Player.Level);
    protected override ActionDefinition? GetDoTAction(IAsclepiusContext context) => SGEActions.GetDotForLevel(context.Player.Level);
    protected override ActionDefinition? GetAoEDamageAction(IAsclepiusContext context) => SGEActions.GetAoEDamageGcdForLevel(context.Player.Level);
    protected override ActionDefinition GetSingleTargetAction(IAsclepiusContext context, bool isMoving) => SGEActions.GetDamageGcdForLevel(context.Player.Level);
    protected override void SetDpsState(IAsclepiusContext context, string state) => context.Debug.DpsState = state;
    protected override void SetAoEDpsState(IAsclepiusContext context, string state) => context.Debug.AoEDpsState = state;
    protected override void SetAoEDpsEnemyCount(IAsclepiusContext context, int count) => context.Debug.AoEDpsEnemyCount = count;
    protected override void SetPlannedAction(IAsclepiusContext context, string action) => context.Debug.PlannedAction = action;
    protected override bool BlocksOnExecution => false;
    protected override bool CanSingleTarget(IAsclepiusContext context, bool isMoving) => !isMoving;

    public override bool TryExecute(IAsclepiusContext context, bool isMoving) => false;

    public new void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat) return;
        if (context.TargetingService.IsDamageTargetingPaused()) { SetDpsState(context, "Paused (no target)"); return; }
        if (context.Configuration.Targeting.SuppressDamageOnForcedMovement
            && PlayerSafetyHelper.IsForcedMovementActive(context.Player))
        {
            SetDpsState(context, "Paused (forced movement)");
            return;
        }

        TryPushPsyche(context, scheduler);
        TryPushPhlegma(context, scheduler);
        TryDirectDispatchEukrasiaForDoT(context);
        TryPushDoT(context, scheduler);
        TryPushAoEDamage(context, scheduler);
        TryPushSingleTargetDamage(context, scheduler, isMoving);
        if (isMoving) TryPushToxikon(context, scheduler);
    }

    public override void UpdateDebugState(IAsclepiusContext context)
    {
        var player = context.Player;
        var dotAction = SGEActions.GetDotForLevel(player.Level);
        var dotStatusId = SGEActions.GetDotStatusId(player.Level);

        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy, dotAction.Range, player);
        if (enemy != null)
        {
            var dotRemaining = GetStatusRemainingTime(enemy, dotStatusId, player.GameObjectId);
            context.Debug.DoTRemaining = dotRemaining;
            context.Debug.DoTState = dotRemaining > 0 ? $"{dotRemaining:F1}s" : "Not applied";
        }
        else
        {
            context.Debug.DoTState = "No target";
        }

        var phlegmaAction = SGEActions.GetPhlegmaForLevel(player.Level);
        if (phlegmaAction != null)
        {
            var charges = (int)context.ActionService.GetCurrentCharges(phlegmaAction.ActionId);
            context.Debug.PhlegmaCharges = charges;
            context.Debug.PhlegmaState = charges > 0 ? $"{charges} charges" : "No charges";
        }

        context.Debug.AdderstingStacks = context.AdderstingStacks;
        context.Debug.ToxikonState = context.AdderstingStacks > 0 ? $"{context.AdderstingStacks} stacks" : "No Addersting";
    }

    private void TryPushPsyche(IAsclepiusContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePsyche) return;
        if (player.Level < SGEActions.Psyche.MinLevel) return;
        if (!context.ActionService.IsActionReady(SGEActions.Psyche.ActionId)) { context.Debug.PsycheState = "On CD"; return; }

        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy, SGEActions.Psyche.Range, player);
        if (enemy == null) { context.Debug.PsycheState = "No target"; return; }

        var capturedEnemy = enemy;

        scheduler.PushOgcd(AsclepiusAbilities.Psyche, enemy.GameObjectId, priority: 290,
            onDispatched: _ =>
            {
                SetPlannedAction(context, SGEActions.Psyche.Name);
                SetDpsState(context, "Psyche");
                context.Debug.PsycheState = "Executing";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = capturedEnemy.Name?.TextValue ?? "Unknown";
                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = SGEActions.Psyche.ActionId,
                        ActionName = "Psyche",
                        Category = "Damage",
                        TargetName = targetName,
                        ShortReason = $"Psyche oGCD damage on {targetName}",
                        DetailedReason = $"Psyche used on {targetName} - SGE's oGCD damage cooldown. Always use on cooldown for optimal DPS.",
                        Factors = new[] { "Psyche off cooldown", "Enemy in range", "oGCD slot available", "High potency damage without using GCD" },
                        Alternatives = new[] { "Nothing - use Psyche on cooldown", "Delay only if you need the oGCD for Eukrasia/healing" },
                        Tip = "Psyche is one of SGE's best oGCD damage tools! Weave it between GCDs on cooldown.",
                        ConceptId = SgeConcepts.PsycheUsage,
                        Priority = ExplanationPriority.Normal,
                    });
                }
            });
    }

    /// <summary>
    /// Direct-dispatch Eukrasia for DoT setup. Uses ExecuteOgcd directly because the
    /// scheduler's CanExecuteOgcd gate would block it during the GCD pass.
    /// </summary>
    private void TryDirectDispatchEukrasiaForDoT(IAsclepiusContext context)
    {
        if (context.HasEukrasia) return;
        if (!IsDoTEnabled(context)) return;

        var player = context.Player;
        if (player.Level < SGEActions.EukrasianDosis.MinLevel) return;

        var dotAction = GetDoTAction(context);
        if (dotAction == null) return;

        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy, dotAction.Range, player);
        if (enemy == null) return;

        var dotStatusId = GetDoTStatusId(context);
        bool needsDot = true;
        if (enemy.StatusList != null)
        {
            foreach (var status in enemy.StatusList)
            {
                if (status.StatusId == dotStatusId && status.RemainingTime > DoTRefreshThreshold(context))
                {
                    needsDot = false;
                    break;
                }
            }
        }

        if (!needsDot) return;

        if (context.ActionService.ExecuteOgcd(SGEActions.Eukrasia, player.GameObjectId))
        {
            SetPlannedAction(context, "Eukrasia");
            SetDpsState(context, "Eukrasia for DoT");
            context.Debug.EukrasiaState = "Activating";
        }
    }

    private void TryPushPhlegma(IAsclepiusContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePhlegma) return;
        var phlegmaAction = SGEActions.GetPhlegmaForLevel(player.Level);
        if (phlegmaAction == null) { context.Debug.PhlegmaState = "Level too low"; return; }

        var charges = context.ActionService.GetCurrentCharges(phlegmaAction.ActionId);
        if (charges < 1) { context.Debug.PhlegmaState = "No charges"; return; }

        var maxCharges = 2u;
        var rechargingTime = context.ActionService.GetCooldownRemaining(phlegmaAction.ActionId);
        var shouldUse = charges >= maxCharges || (charges == maxCharges - 1 && rechargingTime < 5f);
        if (!shouldUse && charges < maxCharges) { context.Debug.PhlegmaState = $"Saving ({charges}/{maxCharges})"; return; }

        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy, phlegmaAction.Range, player);
        if (enemy == null) { context.Debug.PhlegmaState = "Out of range"; return; }

        var capturedAction = phlegmaAction;
        var capturedEnemy = enemy;
        var capturedCharges = charges;
        var behavior = new AbilityBehavior { Action = phlegmaAction };

        scheduler.PushGcd(behavior, enemy.GameObjectId, priority: 295,
            onDispatched: _ =>
            {
                SetPlannedAction(context, capturedAction.Name);
                SetDpsState(context, capturedAction.Name);
                context.Debug.PhlegmaState = "Executing";
            });
    }

    private void TryPushToxikon(IAsclepiusContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableToxikon) return;
        var toxikonAction = SGEActions.GetToxikonForLevel(player.Level);
        if (toxikonAction == null) { context.Debug.ToxikonState = "Level too low"; return; }
        if (context.AdderstingStacks < 1) { context.Debug.ToxikonState = "No Addersting"; return; }

        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy, toxikonAction.Range, player);
        if (enemy == null) { context.Debug.ToxikonState = "No target"; return; }

        var capturedAction = toxikonAction;
        var behavior = new AbilityBehavior { Action = toxikonAction };

        scheduler.PushGcd(behavior, enemy.GameObjectId, priority: 305,
            onDispatched: _ =>
            {
                SetPlannedAction(context, capturedAction.Name);
                SetDpsState(context, capturedAction.Name);
                context.Debug.ToxikonState = "Executing";
            });
    }

    private void TryPushDoT(IAsclepiusContext context, RotationScheduler scheduler)
    {
        if (!IsDoTEnabled(context)) return;

        var player = context.Player;
        if (player.Level < SGEActions.EukrasianDosis.MinLevel) return;

        var dotAction = GetDoTAction(context);
        if (dotAction == null) return;
        if (!context.HasEukrasia) return; // Eukrasia activates separately via direct dispatch

        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy, dotAction.Range, player);
        if (enemy == null) return;

        var capturedAction = dotAction;
        var behavior = new AbilityBehavior { Action = dotAction };

        scheduler.PushGcd(behavior, enemy.GameObjectId, priority: 310,
            onDispatched: _ =>
            {
                SetPlannedAction(context, capturedAction.Name);
                SetDpsState(context, "DoT Applied");
            });
    }

    private void TryPushAoEDamage(IAsclepiusContext context, RotationScheduler scheduler)
    {
        if (!IsAoEDamageEnabled(context)) return;

        var aoeAction = GetAoEDamageAction(context);
        if (aoeAction == null) return;

        var aoeCastTime = context.HasSwiftcast ? 0f : aoeAction.CastTime;
        if (MechanicCastGate.ShouldBlock(context, aoeCastTime)) { SetAoEDpsState(context, "Holding: mechanic imminent"); return; }

        var enemyCount = context.TargetingService.CountEnemiesInRange(aoeAction.Radius, context.Player);
        SetAoEDpsEnemyCount(context, enemyCount);
        if (enemyCount < AoEMinTargets(context)) { SetAoEDpsState(context, $"{enemyCount} < {AoEMinTargets(context)} min"); return; }

        var targetId = aoeAction.TargetType == ActionTargetType.Self
            ? context.Player.GameObjectId
            : FindBestAoETarget(context, aoeAction);
        if (targetId == 0) return;

        var capturedAction = aoeAction;
        var capturedEnemyCount = enemyCount;
        var behavior = new AbilityBehavior { Action = aoeAction };

        scheduler.PushGcd(behavior, targetId, priority: 320,
            onDispatched: _ =>
            {
                SetPlannedAction(context, capturedAction.Name);
                SetDpsState(context, $"AoE ({capturedEnemyCount} targets)");
                SetAoEDpsState(context, $"{capturedEnemyCount} enemies");
            });
    }

    private void TryPushSingleTargetDamage(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!IsDamageEnabled(context)) { SetDpsState(context, "Damage disabled"); return; }
        if (isMoving) return;

        var action = GetSingleTargetAction(context, isMoving);

        var stCastTime = context.HasSwiftcast ? 0f : action.CastTime;
        if (MechanicCastGate.ShouldBlock(context, stCastTime)) { SetDpsState(context, "Holding: mechanic imminent"); return; }

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy, action.Range, context.Player);
        if (target == null) { SetDpsState(context, "No enemy found"); return; }

        var capturedAction = action;
        var behavior = new AbilityBehavior { Action = action };

        scheduler.PushGcd(behavior, target.GameObjectId, priority: 330,
            onDispatched: _ =>
            {
                SetPlannedAction(context, capturedAction.Name);
                SetDpsState(context, capturedAction.Name);
            });
    }

    private float GetStatusRemainingTime(IBattleChara target, uint statusId, ulong sourceId)
    {
        if (target.StatusList == null) return 0f;
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == statusId && status.SourceId == (uint)sourceId)
                return status.RemainingTime;
        }
        return 0f;
    }
}
