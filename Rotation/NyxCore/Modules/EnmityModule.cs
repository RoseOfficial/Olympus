using System;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.NyxCore.Abilities;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.NyxCore.Modules;

/// <summary>
/// Handles the Dark Knight enmity management (scheduler-driven).
/// </summary>
public sealed class EnmityModule : INyxModule
{
    public int Priority => 5;
    public string Name => "Enmity";

    private DateTime _lastProvokeTime = DateTime.MinValue;
    private DateTime _lastSwapRequestTime = DateTime.MinValue;

    public bool TryExecute(INyxContext context, bool isMoving) => false;

    public void UpdateDebugState(INyxContext context) { }

    public void CollectCandidates(INyxContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.EnmityState = "Not in combat";
            return;
        }

        TryPushProvoke(context, scheduler);
        TryPushShirk(context, scheduler);
    }

    private void TryPushProvoke(INyxContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        if (player.Level < RoleActions.Provoke.MinLevel) return;
        if (!context.Configuration.Tank.AutoProvoke)
        {
            context.Debug.EnmityState = "AutoProvoke disabled";
            return;
        }

        var target = context.TargetingService.FindEnemy(context.Configuration.Targeting.EnemyStrategy, 25f, player);
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
            var tName = target.Name?.TextValue;
            scheduler.PushOgcd(NyxAbilities.Provoke, target.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    _lastProvokeTime = DateTime.UtcNow;
                    partyCoord?.ClearTankSwapReservation(targetEntityId);
                    context.Debug.PlannedAction = RoleActions.Provoke.Name;
                    context.Debug.EnmityState = "Provoking (coordinated swap)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(RoleActions.Provoke.ActionId, RoleActions.Provoke.Name).AsEnmity().Target(tName)
                        .Reason("Coordinated tank swap.", "Provoke transfers aggro.")
                        .Factors("Co-tank requested swap", $"Target: {tName}")
                        .Alternatives("Ignore swap request")
                        .Tip("Respond to tank swap requests promptly.")
                        .Concept("drk_provoke").Record();
                    context.TrainingService?.RecordConceptApplication("drk_provoke", true, "Coordinated tank swap");
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

        var tNameB = target.Name?.TextValue;
        scheduler.PushOgcd(NyxAbilities.Provoke, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                _lastProvokeTime = DateTime.UtcNow;
                partyCoord?.ClearTankSwapReservation(targetEntityId);
                context.Debug.PlannedAction = RoleActions.Provoke.Name;
                context.Debug.EnmityState = "Provoking (losing aggro)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.Provoke.ActionId, RoleActions.Provoke.Name).AsEnmity().Target(tNameB)
                    .Reason("Emergency Provoke - losing aggro.", "Provoke instantly regains aggro.")
                    .Factors("Lost aggro", $"Target: {tNameB}")
                    .Alternatives("Let co-tank handle it")
                    .Tip("Provoke immediately when losing aggro.")
                    .Concept("drk_provoke").Record();
                context.TrainingService?.RecordConceptApplication("drk_provoke", true, "Emergency aggro recovery");
            });
    }

    private void TryPushShirk(INyxContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        if (player.Level < RoleActions.Shirk.MinLevel) return;
        if (!context.Configuration.Tank.AutoShirk) return;

        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy, DRKActions.HardSlash.ActionId, player);
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
            scheduler.PushOgcd(NyxAbilities.Shirk, coTankForSwap.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    partyCoord?.ClearTankSwapReservation(targetEntityId);
                    context.Debug.PlannedAction = RoleActions.Shirk.Name;
                    context.Debug.EnmityState = "Shirking (coordinated swap)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(RoleActions.Shirk.ActionId, RoleActions.Shirk.Name).AsEnmity().Target(coTankName)
                        .Reason("Coordinated tank swap.", "Shirk transfers 25% enmity.")
                        .Factors("Co-tank requested swap", $"Co-tank: {coTankName}")
                        .Alternatives("Ignore swap")
                        .Tip("Shirk after co-tank Provokes.")
                        .Concept("drk_shirk").Record();
                    context.TrainingService?.RecordConceptApplication("drk_shirk", true, "Coordinated tank swap");
                });
            return;
        }

        if (!context.EnmityService.HasCoTankAggro(target, player.EntityId)) return;

        var coTank = context.PartyHelper.FindCoTank(player);
        if (coTank == null) { context.Debug.EnmityState = "No co-tank found"; return; }

        var dx = player.Position.X - coTank.Position.X;
        var dy = player.Position.Y - coTank.Position.Y;
        var dz = player.Position.Z - coTank.Position.Z;
        var distance = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        if (distance > 25f) { context.Debug.EnmityState = "Co-tank too far for Shirk"; return; }

        if (!context.ActionService.IsActionReady(RoleActions.Shirk.ActionId)) { context.Debug.EnmityState = "Shirk on CD"; return; }

        var myPosition = context.EnmityService.GetEnmityPosition(target, player.EntityId);
        if (myPosition != 2) { context.Debug.EnmityState = $"Position {myPosition}, not off-tanking"; return; }

        var coTankNameB = coTank.Name?.TextValue;
        scheduler.PushOgcd(NyxAbilities.Shirk, coTank.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.Shirk.Name;
                context.Debug.EnmityState = "Shirking to co-tank";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.Shirk.ActionId, RoleActions.Shirk.Name).AsEnmity().Target(coTankNameB)
                    .Reason("Proactive Shirk.", "Shirk transfers 25% enmity.")
                    .Factors("Off-tank position", $"Co-tank: {coTankNameB}")
                    .Alternatives("Stop DPSing")
                    .Tip("As off-tank, Shirk periodically.")
                    .Concept("drk_shirk").Record();
                context.TrainingService?.RecordConceptApplication("drk_shirk", true, "Off-tank enmity management");
            });
    }
}
