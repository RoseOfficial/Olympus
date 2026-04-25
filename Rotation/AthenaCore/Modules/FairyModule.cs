using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Scholar;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Handles fairy management for Scholar (scheduler-driven).
/// Push priorities are 0-7 so fairy management wins against Resurrection (1-2)
/// when Eos isn't summoned and against Healing handlers when fairy abilities fire.
/// </summary>
public sealed class FairyModule : IAthenaModule
{
    public int Priority => 3;
    public string Name => "Fairy";

    public bool TryExecute(IAthenaContext context, bool isMoving) => false;

    public void CollectCandidates(IAthenaContext context, RotationScheduler scheduler, bool isMoving)
    {
        TryPushSummonFairy(context, scheduler, isMoving);
        TryPushSeraphism(context, scheduler);
        TryPushSummonSeraph(context, scheduler);
        TryPushConsolation(context, scheduler);
        TryPushFeyUnion(context, scheduler);
        TryPushFeyBlessing(context, scheduler);
        TryPushWhisperingDawn(context, scheduler);
        TryPushFeyIllumination(context, scheduler);
    }

    public void UpdateDebugState(IAthenaContext context)
    {
        context.Debug.FairyState = context.FairyStateManager.CurrentState.ToString();
        context.Debug.FairyGauge = context.FairyGaugeService.CurrentGauge;
    }

    private void TryPushSummonFairy(IAthenaContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.AutoSummonFairy) return;
        if (!context.FairyStateManager.NeedsSummon) return;
        if (context.FairyStateManager.IsDissipationActive) return;
        if (player.Level < SCHActions.SummonEos.MinLevel) return;
        if (isMoving) return;

        scheduler.PushGcd(AthenaAbilities.SummonEos, player.GameObjectId, priority: 0,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = SCHActions.SummonEos.Name;
                context.Debug.PlanningState = "Summoning Fairy";
                context.TrainingService?.RecordConceptApplication(SchConcepts.FairyManagement, wasSuccessful: true);
            });
    }

    private void TryPushSeraphism(IAthenaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (config.SeraphismStrategy == SeraphismUsageStrategy.Manual) return;
        if (player.Level < SCHActions.Seraphism.MinLevel) return;
        if (!context.ActionService.IsActionReady(SCHActions.Seraphism.ActionId)) return;

        if (config.SeraphismStrategy == SeraphismUsageStrategy.SaveForDamage)
        {
            var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            if (avgHp > config.SeraphPartyHpThreshold) return;
        }

        scheduler.PushOgcd(AthenaAbilities.Seraphism, player.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = SCHActions.Seraphism.Name;
                context.Debug.PlanningState = "Seraphism";
                context.TrainingService?.RecordConceptApplication(SchConcepts.SeraphUsage, wasSuccessful: true);
            });
    }

    private void TryPushSummonSeraph(IAthenaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (config.SeraphStrategy == SeraphUsageStrategy.Manual) return;
        if (player.Level < SCHActions.SummonSeraph.MinLevel) return;
        if (!context.FairyStateManager.CanUseEosAbilities) return;
        if (!context.ActionService.IsActionReady(SCHActions.SummonSeraph.ActionId)) return;

        if (config.SeraphStrategy == SeraphUsageStrategy.SaveForDamage)
        {
            var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
            if (avgHp > config.SeraphPartyHpThreshold) return;
        }

        scheduler.PushOgcd(AthenaAbilities.SummonSeraph, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = SCHActions.SummonSeraph.Name;
                context.Debug.PlanningState = "Seraph";
                context.TrainingService?.RecordConceptApplication(SchConcepts.SeraphUsage, wasSuccessful: true);
            });
    }

    private void TryPushConsolation(IAthenaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableConsolation) return;
        if (!context.FairyStateManager.CanUseSeraphAbilities) return;
        if (player.Level < SCHActions.Consolation.MinLevel) return;
        if (!context.ActionService.IsActionReady(SCHActions.Consolation.ActionId)) return;

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.AoEHealThreshold && injuredCount < config.AoEHealMinTargets) return;

        scheduler.PushOgcd(AthenaAbilities.Consolation, player.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = SCHActions.Consolation.Name;
                context.Debug.PlanningState = "Consolation";
                context.TrainingService?.RecordConceptApplication(SchConcepts.SeraphUsage, wasSuccessful: true);
            });
    }

    private void TryPushFeyUnion(IAthenaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableFairyAbilities) return;
        if (!context.FairyStateManager.CanUseEosAbilities) return;
        if (player.Level < SCHActions.FeyUnion.MinLevel) return;
        if (context.FairyGaugeService.CurrentGauge < config.FeyUnionMinGauge) return;
        if (context.StatusHelper.HasFeyUnionActive(player)) return;

        var target = context.PartyHelper.FindFeyUnionTarget(player, config.FeyUnionThreshold);
        if (target == null) return;

        scheduler.PushOgcd(AthenaAbilities.FeyUnion, target.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = SCHActions.FeyUnion.Name;
                context.Debug.PlanningState = "Fey Union";
                context.TrainingService?.RecordConceptApplication(SchConcepts.FeyUnionUsage, wasSuccessful: true);
            });
    }

    private void TryPushFeyBlessing(IAthenaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableFairyAbilities) return;
        if (!context.FairyStateManager.CanUseEosAbilities) return;
        if (player.Level < SCHActions.FeyBlessing.MinLevel) return;
        if (!context.ActionService.IsActionReady(SCHActions.FeyBlessing.ActionId)) return;

        var (avgHp, _, _) = context.PartyHealthMetrics;
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService, context.BossMechanicDetector, context.Configuration, out _);

        var burstImminent = false;
        var coordConfig = context.Configuration.PartyCoordination;
        var partyCoord = context.PartyCoordinationService;
        if (coordConfig.EnableHealerBurstAwareness && coordConfig.PreferShieldsBeforeBurst && partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsImminent && burstState.SecondsUntilBurst >= 3f && burstState.SecondsUntilBurst <= 8f)
                burstImminent = true;
        }

        if (!raidwideImminent && !burstImminent && avgHp > config.FeyBlessingThreshold) return;

        scheduler.PushOgcd(AthenaAbilities.FeyBlessing, player.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = SCHActions.FeyBlessing.Name;
                context.Debug.PlanningState = "Fey Blessing";
                context.TrainingService?.RecordConceptApplication(SchConcepts.FeyBlessingUsage, wasSuccessful: true);
            });
    }

    private void TryPushWhisperingDawn(IAthenaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableFairyAbilities) return;
        if (!context.FairyStateManager.IsFairyAvailable) return;
        if (player.Level < SCHActions.WhisperingDawn.MinLevel) return;
        if (!context.ActionService.IsActionReady(SCHActions.WhisperingDawn.ActionId)) return;

        var (avgHp, _, injuredCount) = context.PartyHealthMetrics;
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService, context.BossMechanicDetector, context.Configuration, out _);

        var burstImminent = false;
        var coordConfig = context.Configuration.PartyCoordination;
        var partyCoord = context.PartyCoordinationService;
        if (coordConfig.EnableHealerBurstAwareness && coordConfig.PreferShieldsBeforeBurst && partyCoord != null)
        {
            var burstState = partyCoord.GetBurstWindowState();
            if (burstState.IsImminent && burstState.SecondsUntilBurst >= 3f && burstState.SecondsUntilBurst <= 8f)
                burstImminent = true;
        }

        if (!raidwideImminent && !burstImminent)
        {
            if (avgHp > config.WhisperingDawnThreshold) return;
            if (injuredCount < config.WhisperingDawnMinTargets) return;
        }

        scheduler.PushOgcd(AthenaAbilities.WhisperingDawn, player.GameObjectId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = SCHActions.WhisperingDawn.Name;
                context.Debug.PlanningState = "Whispering Dawn";
                context.TrainingService?.RecordConceptApplication(SchConcepts.WhisperingDawnUsage, wasSuccessful: true);
            });
    }

    private void TryPushFeyIllumination(IAthenaContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableFairyAbilities) return;
        if (!context.FairyStateManager.IsFairyAvailable) return;
        if (player.Level < SCHActions.FeyIllumination.MinLevel) return;
        if (!context.ActionService.IsActionReady(SCHActions.FeyIllumination.ActionId)) return;

        var (avgHp, lowestHp, _) = context.PartyHealthMetrics;
        if (lowestHp > 0.5f && avgHp > 0.8f) return;

        scheduler.PushOgcd(AthenaAbilities.FeyIllumination, player.GameObjectId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = SCHActions.FeyIllumination.Name;
                context.Debug.PlanningState = "Fey Illumination";
                context.TrainingService?.RecordConceptApplication(SchConcepts.FeyIlluminationUsage, wasSuccessful: true);
            });
    }
}
