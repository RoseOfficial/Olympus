using System;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.ThemisCore.Abilities;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.ThemisCore.Modules;

/// <summary>
/// Handles the Paladin enmity management.
/// Manages Provoke and Shirk for threat control.
/// Coordinates with other Olympus tank instances via IPC.
/// </summary>
public sealed class EnmityModule : IThemisModule
{
    public int Priority => 5; // Highest priority - enmity management is critical
    public string Name => "Enmity";

    private DateTime _lastProvokeTime = DateTime.MinValue;
    private DateTime _lastSwapRequestTime = DateTime.MinValue;

    public bool TryExecute(IThemisContext context, bool isMoving) => false;

    public void UpdateDebugState(IThemisContext context)
    {
        // Debug state updated during CollectCandidates
    }

    #region CollectCandidates (scheduler path)

    public void CollectCandidates(IThemisContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.EnmityState = "Not in combat";
            return;
        }

        TryPushProvoke(context, scheduler);
        TryPushShirk(context, scheduler);
    }

    private void TryPushProvoke(IThemisContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RoleActions.Provoke.MinLevel) return;

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

        // Branch A: co-tank requested a swap (they want to give aggro)
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

            scheduler.PushOgcd(ThemisAbilities.Provoke, targetId, priority: 1,
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
                        .Reason(
                            "Coordinated tank swap - co-tank requested aggro transfer. Provoke used to take boss aggro.",
                            "Provoke instantly puts you at top of enmity list. Use it for planned tank swaps coordinated with your co-tank.")
                        .Factors("Co-tank requested swap via IPC", "Provoke available", $"Target: {targetName}")
                        .Alternatives("Ignore swap request (may cause wipe)", "Wait longer (mechanics may not allow delay)")
                        .Tip("Always respond to coordinated tank swap requests promptly.")
                        .Concept("pld_provoke")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("pld_provoke", true, "Coordinated tank swap");
                });
            return;
        }

        // Branch B: losing aggro on the target
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
        scheduler.PushOgcd(ThemisAbilities.Provoke, targetIdB, priority: 1,
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
                    .Reason(
                        "Emergency Provoke - losing aggro to another player.",
                        "Provoke instantly puts you at top of enmity list. Use it when losing aggro unexpectedly to reclaim the boss.")
                    .Factors("Lost aggro to non-tank", "Boss about to attack party", $"Target: {targetNameB}")
                    .Alternatives("Let co-tank take it (risky if they're not ready)", "Use more enmity combos (too slow in emergencies)")
                    .Tip("If losing aggro as main tank, Provoke immediately.")
                    .Concept("pld_provoke")
                    .Record();
                context.TrainingService?.RecordConceptApplication("pld_provoke", true, "Emergency aggro recovery");
            });
    }

    private void TryPushShirk(IThemisContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RoleActions.Shirk.MinLevel) return;

        if (!context.Configuration.Tank.AutoShirk) return;

        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            PLDActions.FastBlade.ActionId,
            player);

        if (target == null) return;

        var partyCoord = context.PartyCoordinationService;
        var targetEntityId = (uint)target.GameObjectId;

        // Branch A: co-tank requested a swap (they want to take aggro)
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

            scheduler.PushOgcd(ThemisAbilities.Shirk, coTankForSwap.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    partyCoord?.ClearTankSwapReservation(targetEntityId);
                    context.Debug.PlannedAction = RoleActions.Shirk.Name;
                    context.Debug.EnmityState = "Shirking (coordinated swap)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(RoleActions.Shirk.ActionId, RoleActions.Shirk.Name)
                        .AsEnmity()
                        .Target(coTankName)
                        .Reason(
                            "Coordinated tank swap - co-tank requested to take aggro. Shirk transfers enmity.",
                            "Shirk transfers 25% of your enmity to the target.")
                        .Factors("Co-tank requested swap via IPC", "Shirk available", $"Co-tank: {coTankName}")
                        .Alternatives("Ignore swap request", "Keep aggro (may cause mechanic failures)")
                        .Tip("After co-tank Provokes, use Shirk immediately.")
                        .Concept("pld_shirk")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("pld_shirk", true, "Coordinated tank swap");
                });
            return;
        }

        // Branch B: proactive off-tank enmity drop
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
        scheduler.PushOgcd(ThemisAbilities.Shirk, coTank.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.Shirk.Name;
                context.Debug.EnmityState = "Shirking to co-tank";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.Shirk.ActionId, RoleActions.Shirk.Name)
                    .AsEnmity()
                    .Target(coTankNameB)
                    .Reason(
                        "Proactive Shirk - off-tank position, building enmity. Shirk helps main tank.",
                        "Shirk transfers 25% of your enmity. As off-tank, use it to prevent accidentally pulling aggro.")
                    .Factors("Off-tank position (#2)", "Building enmity from DPS rotation", $"Co-tank: {coTankNameB}")
                    .Alternatives("Stop DPSing (massive damage loss)", "Let main tank use Provoke (wastes their cooldown)")
                    .Tip("As off-tank, Shirk periodically to stay comfortable below the main tank.")
                    .Concept("pld_shirk")
                    .Record();
                context.TrainingService?.RecordConceptApplication("pld_shirk", true, "Off-tank enmity management");
            });
    }

    #endregion
}
