using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Scholar-specific damage module (scheduler-driven).
/// </summary>
public sealed class DamageModule : BaseDamageModule<IAthenaContext>, IAthenaModule
{
    protected override bool IsDamageEnabled(IAthenaContext context) => context.Configuration.Scholar.EnableSingleTargetDamage;
    protected override bool IsDoTEnabled(IAthenaContext context) => context.Configuration.Scholar.EnableDot;
    protected override bool IsAoEDamageEnabled(IAthenaContext context) => context.Configuration.Scholar.EnableAoEDamage;
    protected override int AoEMinTargets(IAthenaContext context) => context.Configuration.Scholar.AoEDamageMinTargets;
    protected override float DoTRefreshThreshold(IAthenaContext context) => context.Configuration.Scholar.DotRefreshThreshold;
    protected override uint GetDoTStatusId(IAthenaContext context) => SCHActions.GetDotStatusId(context.Player.Level);
    protected override ActionDefinition? GetDoTAction(IAthenaContext context) => SCHActions.GetDotForLevel(context.Player.Level);
    protected override ActionDefinition? GetAoEDamageAction(IAthenaContext context) => SCHActions.GetAoEDamageForLevel(context.Player.Level);
    protected override ActionDefinition GetSingleTargetAction(IAthenaContext context, bool isMoving) => SCHActions.GetDamageGcdForLevel(context.Player.Level, isMoving);
    protected override void SetDpsState(IAthenaContext context, string state) => context.Debug.DpsState = state;
    protected override void SetAoEDpsState(IAthenaContext context, string state) => context.Debug.AoEDpsState = state;
    protected override void SetAoEDpsEnemyCount(IAthenaContext context, int count) => context.Debug.AoEDpsEnemyCount = count;
    protected override void SetPlannedAction(IAthenaContext context, string action) => context.Debug.PlannedAction = action;
    protected override bool BlocksOnExecution => false;

    public override bool TryExecute(IAthenaContext context, bool isMoving) => false;

    public new void CollectCandidates(IAthenaContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat) return;
        if (context.TargetingService.IsDamageTargetingPaused()) { SetDpsState(context, "Paused (no target)"); return; }
        if (context.Configuration.Targeting.SuppressDamageOnForcedMovement
            && PlayerSafetyHelper.IsForcedMovementActive(context.Player))
        {
            SetDpsState(context, "Paused (forced movement)");
            return;
        }

        TryPushChainStratagem(context, scheduler);
        TryPushBanefulImpaction(context, scheduler);
        TryPushEnergyDrain(context, scheduler);
        TryPushAetherflow(context, scheduler);
        TryPushDoT(context, scheduler);
        TryPushAoEDamage(context, scheduler);
        if (!isMoving) TryPushSingleTargetDamage(context, scheduler, isMoving);
        if (isMoving) TryPushRuinII(context, scheduler);
    }

    public override void UpdateDebugState(IAthenaContext context)
    {
        context.Debug.AetherflowState = $"{context.AetherflowService.CurrentStacks}/3";
    }

    private void TryPushChainStratagem(IAthenaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableChainStratagem) return;
        if (player.Level < SCHActions.ChainStratagem.MinLevel) return;
        if (!context.ActionService.IsActionReady(SCHActions.ChainStratagem.ActionId)) return;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy, SCHActions.ChainStratagem.Range, player);
        if (target == null) return;

        var capturedTarget = target;

        scheduler.PushOgcd(AthenaAbilities.ChainStratagem, target.GameObjectId, priority: 285,
            onDispatched: _ =>
            {
                SetPlannedAction(context, SCHActions.ChainStratagem.Name);
                SetDpsState(context, "Chain Stratagem");

                TrainingHelper.RecordBuffDecision(
                    context.TrainingService,
                    SCHActions.ChainStratagem.ActionId, "Chain Stratagem",
                    capturedTarget.Name?.TextValue,
                    "Chain Stratagem - party damage buff",
                    "Chain Stratagem increases the critical hit rate of all party members against the target by 10% for 15 seconds.",
                    new[] { "Action ready", "Enemy in range", "Party burst alignment" },
                    new[] { "Delay for better alignment", "Use during 2-min burst" },
                    "Chain Stratagem on cooldown aligned with 2-minute bursts.",
                    SchConcepts.ChainStratagemTiming,
                    ExplanationPriority.High);

                context.TrainingService?.RecordConceptApplication(SchConcepts.ChainStratagemTiming, wasSuccessful: true);
            });
    }

    private void TryPushBanefulImpaction(IAthenaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableBanefulImpaction) return;
        if (player.Level < SCHActions.BanefulImpaction.MinLevel) return;
        if (!context.StatusHelper.HasImpactImminent(player)) return;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy, SCHActions.BanefulImpaction.Range, player);
        if (target == null) return;

        scheduler.PushOgcd(AthenaAbilities.BanefulImpaction, target.GameObjectId, priority: 287,
            onDispatched: _ =>
            {
                SetPlannedAction(context, SCHActions.BanefulImpaction.Name);
                SetDpsState(context, "Baneful Impaction");
                context.TrainingService?.RecordConceptApplication(SchConcepts.ChainStratagemTiming, wasSuccessful: true);
            });
    }

    private void TryPushEnergyDrain(IAthenaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableEnergyDrain) return;
        if (player.Level < SCHActions.EnergyDrain.MinLevel) return;
        if (!context.ActionService.IsActionReady(SCHActions.EnergyDrain.ActionId)) return;

        var stacks = context.AetherflowService.CurrentStacks;
        if (stacks == 0) return;

        bool shouldDrain = config.AetherflowStrategy switch
        {
            AetherflowUsageStrategy.AggressiveDps => stacks > 0,
            AetherflowUsageStrategy.Balanced => ShouldDrainBalanced(context),
            AetherflowUsageStrategy.HealingPriority => ShouldDrainConservative(context),
            _ => false
        };
        if (!shouldDrain) return;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy, SCHActions.EnergyDrain.Range, player);
        if (target == null) return;

        scheduler.PushOgcd(AthenaAbilities.EnergyDrain, target.GameObjectId, priority: 290,
            onDispatched: _ =>
            {
                context.AetherflowService.ConsumeStack();
                SetPlannedAction(context, SCHActions.EnergyDrain.Name);
                SetDpsState(context, "Energy Drain");
                context.TrainingService?.RecordConceptApplication(SchConcepts.EnergyDrainUsage, wasSuccessful: true);
            });
    }

    private void TryPushAetherflow(IAthenaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableAetherflow) return;
        if (player.Level < SCHActions.Aetherflow.MinLevel) return;
        if (!context.ActionService.IsActionReady(SCHActions.Aetherflow.ActionId)) return;
        if (context.AetherflowService.CurrentStacks > 0) return;

        scheduler.PushOgcd(AthenaAbilities.Aetherflow, player.GameObjectId, priority: 295,
            onDispatched: _ =>
            {
                SetPlannedAction(context, SCHActions.Aetherflow.Name);
                SetDpsState(context, "Aetherflow");
                context.TrainingService?.RecordConceptApplication(SchConcepts.AetherflowRefresh, wasSuccessful: true);
            });
    }

    private void TryPushRuinII(IAthenaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableRuinII) return;
        if (player.Level < SCHActions.RuinII.MinLevel) return;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy, SCHActions.RuinII.Range, player);
        if (target == null) return;

        scheduler.PushGcd(AthenaAbilities.RuinII, target.GameObjectId, priority: 305,
            onDispatched: _ =>
            {
                SetPlannedAction(context, SCHActions.RuinII.Name);
                SetDpsState(context, "Ruin II (moving)");
                context.TrainingService?.RecordConceptApplication(SchConcepts.DpsOptimization, wasSuccessful: true);
            });
    }

    private void TryPushDoT(IAthenaContext context, RotationScheduler scheduler)
    {
        if (!IsDoTEnabled(context)) return;

        var dotAction = GetDoTAction(context);
        if (dotAction == null) return;

        if (IsAoEDamageEnabled(context))
        {
            var aoeAction = GetAoEDamageAction(context);
            if (aoeAction != null)
            {
                var enemyCount = context.TargetingService.CountEnemiesInRange(aoeAction.Radius, context.Player);
                if (enemyCount >= AoEMinTargets(context)) { SetDpsState(context, $"DoT: skipped ({enemyCount} enemies)"); return; }
            }
        }

        var dotCastTime = context.HasSwiftcast ? 0f : dotAction.CastTime;
        if (MechanicCastGate.ShouldBlock(context, dotCastTime)) { SetDpsState(context, "DoT: mechanic imminent"); return; }

        var dotStatusId = GetDoTStatusId(context);
        if (dotStatusId == 0) return;

        var target = context.TargetingService.FindEnemyNeedingDot(dotStatusId, DoTRefreshThreshold(context), dotAction.Range, context.Player);
        if (target == null) { SetDpsState(context, "DoT: no target"); return; }

        var capturedAction = dotAction;
        var behavior = new AbilityBehavior { Action = dotAction };

        scheduler.PushGcd(behavior, target.GameObjectId, priority: 310,
            onDispatched: _ =>
            {
                SetPlannedAction(context, capturedAction.Name);
                SetDpsState(context, "DoT");
            });
    }

    private void TryPushAoEDamage(IAthenaContext context, RotationScheduler scheduler)
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

    private void TryPushSingleTargetDamage(IAthenaContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!IsDamageEnabled(context)) { SetDpsState(context, "Damage disabled"); return; }

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

    private bool ShouldDrainBalanced(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var stacks = context.AetherflowService.CurrentStacks;
        var aetherflowCd = context.AetherflowService.GetCooldownRemaining();
        if (aetherflowCd <= config.AetherflowDumpWindow && stacks > 0) return true;
        if (stacks <= config.AetherflowReserve) return false;
        var (avgHp, lowestHp, _) = context.PartyHelper.CalculatePartyHealthMetrics(context.Player);
        return avgHp > 0.8f && lowestHp > 0.5f;
    }

    private bool ShouldDrainConservative(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var stacks = context.AetherflowService.CurrentStacks;
        var aetherflowCd = context.AetherflowService.GetCooldownRemaining();
        if (aetherflowCd <= config.AetherflowDumpWindow && stacks == 3) return true;
        return false;
    }
}
