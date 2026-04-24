using System;
using Olympus.Data;
using Olympus.Rotation.AresCore.Abilities;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.AresCore.Modules;

/// <summary>
/// Handles the Warrior enmity management (scheduler-driven).
/// Manages Provoke and Shirk for threat control.
/// </summary>
public sealed class EnmityModule : IAresModule
{
    public int Priority => 5;
    public string Name => "Enmity";

    private DateTime _lastProvokeTime = DateTime.MinValue;
    private DateTime _lastSwapRequestTime = DateTime.MinValue;

    public bool TryExecute(IAresContext context, bool isMoving) => false;

    public void UpdateDebugState(IAresContext context) { }

    public void CollectCandidates(IAresContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.EnmityState = "Not in combat";
            return;
        }

        TryPushProvoke(context, scheduler);
        TryPushShirk(context, scheduler);
    }

    private void TryPushProvoke(IAresContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        if (player.Level < RoleActions.Provoke.MinLevel) return;

        if (!context.Configuration.Tank.AutoProvoke)
        {
            context.Debug.EnmityState = "AutoProvoke disabled";
            return;
        }

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            25f,
            player);

        if (target == null)
        {
            context.Debug.EnmityState = "No target";
            return;
        }

        var partyCoord = context.PartyCoordinationService;
        var targetEntityId = (uint)target.GameObjectId;

        var pendingSwap = partyCoord?.GetPendingTankSwapRequest(targetEntityId);
        if (pendingSwap != null && !pendingSwap.IntendToTakeAggro)
        {
            if (!context.ActionService.IsActionReady(RoleActions.Provoke.ActionId))
            {
                context.Debug.EnmityState = "Provoke on CD (swap pending)";
                return;
            }

            partyCoord?.ConfirmTankSwap(targetEntityId);
            var targetName = target.Name?.TextValue;
            var targetId = target.GameObjectId;

            scheduler.PushOgcd(AresAbilities.Provoke, targetId, priority: 1,
                onDispatched: _ =>
                {
                    _lastProvokeTime = DateTime.UtcNow;
                    partyCoord?.ClearTankSwapReservation(targetEntityId);
                    context.Debug.PlannedAction = RoleActions.Provoke.Name;
                    context.Debug.EnmityState = "Provoking (coordinated swap)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(RoleActions.Provoke.ActionId, RoleActions.Provoke.Name)
                        .AsEnmity()
                        .Target(targetName)
                        .Reason("Coordinated tank swap - co-tank requested aggro transfer.", "Provoke instantly puts you at top of enmity list.")
                        .Factors("Co-tank requested swap via IPC", "Provoke available", $"Target: {targetName}")
                        .Alternatives("Ignore swap request")
                        .Tip("Always respond to coordinated tank swap requests promptly.")
                        .Concept("war_provoke")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("war_provoke", true, "Coordinated tank swap");
                });
            return;
        }

        if (!context.EnmityService.IsLosingAggro(target, player.EntityId))
        {
            var position = context.EnmityService.GetEnmityPosition(target, player.EntityId);
            context.Debug.EnmityState = position == 1 ? "Main tank" : $"Position {position}";
            return;
        }

        var timeSinceLastProvoke = (DateTime.UtcNow - _lastProvokeTime).TotalSeconds;
        if (timeSinceLastProvoke < context.Configuration.Tank.ProvokeDelay)
        {
            context.Debug.EnmityState = $"Provoke cooldown ({context.Configuration.Tank.ProvokeDelay - timeSinceLastProvoke:F1}s)";
            return;
        }

        if (!context.ActionService.IsActionReady(RoleActions.Provoke.ActionId))
        {
            context.Debug.EnmityState = "Provoke on CD";
            return;
        }

        if (partyCoord?.HasRemoteTank == true && !partyCoord.IsTankSwapInProgress(targetEntityId))
        {
            var timeSinceLastRequest = (DateTime.UtcNow - _lastSwapRequestTime).TotalSeconds;
            var timeoutSeconds = context.Configuration.PartyCoordination.TankSwapConfirmationTimeoutSeconds;

            if (timeSinceLastRequest > timeoutSeconds)
            {
                partyCoord.RequestTankSwap(targetEntityId, true, 1);
                _lastSwapRequestTime = DateTime.UtcNow;
                context.Debug.EnmityState = "Requesting tank swap";
                return;
            }
        }

        var targetNameB = target.Name?.TextValue;
        var targetIdB = target.GameObjectId;
        scheduler.PushOgcd(AresAbilities.Provoke, targetIdB, priority: 1,
            onDispatched: _ =>
            {
                _lastProvokeTime = DateTime.UtcNow;
                partyCoord?.ClearTankSwapReservation(targetEntityId);
                context.Debug.PlannedAction = RoleActions.Provoke.Name;
                context.Debug.EnmityState = "Provoking (losing aggro)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.Provoke.ActionId, RoleActions.Provoke.Name)
                    .AsEnmity()
                    .Target(targetNameB)
                    .Reason("Emergency Provoke - losing aggro.", "Provoke instantly puts you at top of enmity list.")
                    .Factors("Lost aggro to non-tank", $"Target: {targetNameB}")
                    .Alternatives("Let co-tank take it", "Use enmity combo")
                    .Tip("If losing aggro, Provoke immediately.")
                    .Concept("war_provoke")
                    .Record();
                context.TrainingService?.RecordConceptApplication("war_provoke", true, "Emergency aggro recovery");
            });
    }

    private void TryPushShirk(IAresContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        if (player.Level < RoleActions.Shirk.MinLevel) return;
        if (!context.Configuration.Tank.AutoShirk) return;

        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            WARActions.HeavySwing.ActionId,
            player);

        if (target == null) return;

        var partyCoord = context.PartyCoordinationService;
        var targetEntityId = (uint)target.GameObjectId;

        var pendingSwap = partyCoord?.GetPendingTankSwapRequest(targetEntityId);
        if (pendingSwap != null && pendingSwap.IntendToTakeAggro)
        {
            if (!context.ActionService.IsActionReady(RoleActions.Shirk.ActionId))
            {
                context.Debug.EnmityState = "Shirk on CD (swap pending)";
                return;
            }

            var coTankForSwap = context.PartyHelper.FindCoTank(player);
            if (coTankForSwap == null)
            {
                context.Debug.EnmityState = "No co-tank found for swap";
                return;
            }

            partyCoord?.ConfirmTankSwap(targetEntityId);
            var coTankName = coTankForSwap.Name?.TextValue;

            scheduler.PushOgcd(AresAbilities.Shirk, coTankForSwap.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    partyCoord?.ClearTankSwapReservation(targetEntityId);
                    context.Debug.PlannedAction = RoleActions.Shirk.Name;
                    context.Debug.EnmityState = "Shirking (coordinated swap)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(RoleActions.Shirk.ActionId, RoleActions.Shirk.Name)
                        .AsEnmity()
                        .Target(coTankName)
                        .Reason("Coordinated tank swap.", "Shirk transfers 25% of enmity.")
                        .Factors("Co-tank requested swap via IPC", $"Co-tank: {coTankName}")
                        .Alternatives("Ignore swap request", "Keep aggro")
                        .Tip("After co-tank Provokes, Shirk immediately.")
                        .Concept("war_shirk")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("war_shirk", true, "Coordinated tank swap");
                });
            return;
        }

        if (!context.EnmityService.HasCoTankAggro(target, player.EntityId)) return;

        var coTank = context.PartyHelper.FindCoTank(player);
        if (coTank == null)
        {
            context.Debug.EnmityState = "No co-tank found";
            return;
        }

        var dx = player.Position.X - coTank.Position.X;
        var dy = player.Position.Y - coTank.Position.Y;
        var dz = player.Position.Z - coTank.Position.Z;
        var distance = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (distance > 25f)
        {
            context.Debug.EnmityState = "Co-tank too far for Shirk";
            return;
        }

        if (!context.ActionService.IsActionReady(RoleActions.Shirk.ActionId))
        {
            context.Debug.EnmityState = "Shirk on CD";
            return;
        }

        var myPosition = context.EnmityService.GetEnmityPosition(target, player.EntityId);
        if (myPosition != 2)
        {
            context.Debug.EnmityState = $"Position {myPosition}, not off-tanking";
            return;
        }

        var coTankNameB = coTank.Name?.TextValue;
        scheduler.PushOgcd(AresAbilities.Shirk, coTank.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.Shirk.Name;
                context.Debug.EnmityState = "Shirking to co-tank";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.Shirk.ActionId, RoleActions.Shirk.Name)
                    .AsEnmity()
                    .Target(coTankNameB)
                    .Reason("Proactive Shirk - off-tank position.", "Shirk transfers 25% of enmity.")
                    .Factors("Off-tank position (#2)", $"Co-tank: {coTankNameB}")
                    .Alternatives("Stop DPSing", "Let main tank Provoke")
                    .Tip("As off-tank, Shirk periodically.")
                    .Concept("war_shirk")
                    .Record();
                context.TrainingService?.RecordConceptApplication("war_shirk", true, "Off-tank enmity management");
            });
    }
}
