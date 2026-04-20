using System;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HephaestusCore.Abilities;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.HephaestusCore.Modules;

/// <summary>
/// Handles the Gunbreaker enmity management.
/// Manages Provoke and Shirk for threat control.
/// Coordinates with other Olympus tank instances via IPC.
/// </summary>
public sealed class EnmityModule : IHephaestusModule
{
    public int Priority => 5; // Highest priority - enmity management is critical
    public string Name => "Enmity";

    private DateTime _lastProvokeTime = DateTime.MinValue;
    private DateTime _lastSwapRequestTime = DateTime.MinValue;

    public bool TryExecute(IHephaestusContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.EnmityState = "Not in combat";
            return false;
        }

        // Only use enmity actions during oGCD windows
        if (!context.CanExecuteOgcd)
            return false;

        // Priority 1: Provoke if losing aggro
        if (TryProvoke(context))
            return true;

        // Priority 2: Shirk to co-tank if needed
        if (TryShirk(context))
            return true;

        return false;
    }

    public void UpdateDebugState(IHephaestusContext context)
    {
        // Debug state updated during TryExecute
    }

    public void CollectCandidates(IHephaestusContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.EnmityState = "Not in combat";
            return;
        }

        // Priority 1: Provoke if losing aggro
        TryPushProvoke(context, scheduler);

        // Priority 2: Shirk to co-tank if needed
        TryPushShirk(context, scheduler);
    }

    #region CollectCandidates helpers

    private void TryPushProvoke(IHephaestusContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RoleActions.Provoke.MinLevel)
            return;

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

        // Check if co-tank has requested a swap (they want to give aggro)
        var pendingSwap = partyCoord?.GetPendingTankSwapRequest(targetEntityId);
        if (pendingSwap != null && !pendingSwap.IntendToTakeAggro)
        {
            // Co-tank wants to give aggro - confirm and push Provoke
            context.Debug.EnmityState = "Provoke pending (swap pending)";

            partyCoord?.ConfirmTankSwap(targetEntityId);

            scheduler.PushOgcd(
                GnbAbilities.Provoke,
                target.GameObjectId,
                priority: 1,
                onDispatched: _ =>
                {
                    _lastProvokeTime = DateTime.UtcNow;
                    partyCoord?.ClearTankSwapReservation(targetEntityId);
                    context.Debug.PlannedAction = RoleActions.Provoke.Name;
                    context.Debug.EnmityState = "Provoking (coordinated swap)";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(RoleActions.Provoke.ActionId, RoleActions.Provoke.Name)
                        .AsEnmity()
                        .Target(target.Name?.TextValue ?? "Enemy")
                        .Reason(
                            "Coordinated tank swap - taking aggro from co-tank",
                            "Provoke instantly gives you top enmity on the target. " +
                            "This coordinated swap was initiated by your co-tank signaling they want to give up aggro. " +
                            "Tank swaps are essential for mechanics like tank buster debuffs.")
                        .Factors("Co-tank requested swap", "Provoke ready", "Confirming coordinated action")
                        .Alternatives("Ignore request (may cause wipe)", "Wait longer (might be too late)")
                        .Tip("After Provoking, maintain aggro with damage and Royal Guard. Your co-tank should Shirk to you for clean swap.")
                        .Concept("gnb_provoke")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("gnb_provoke", true, "Coordinated tank swap");
                });
            return;
        }

        // Check if we're losing aggro on the target
        if (!context.EnmityService.IsLosingAggro(target, player.EntityId))
        {
            var position = context.EnmityService.GetEnmityPosition(target, player.EntityId);
            context.Debug.EnmityState = position == 1 ? "Main tank" : $"Position {position}";
            return;
        }

        // Apply provoke delay to prevent spam
        var timeSinceLastProvoke = (DateTime.UtcNow - _lastProvokeTime).TotalSeconds;
        if (timeSinceLastProvoke < context.Configuration.Tank.ProvokeDelay)
        {
            context.Debug.EnmityState = $"Provoke cooldown ({context.Configuration.Tank.ProvokeDelay - timeSinceLastProvoke:F1}s)";
            return;
        }

        // If we have a remote tank, try coordinated swap first
        if (partyCoord?.HasRemoteTank == true && !partyCoord.IsTankSwapInProgress(targetEntityId))
        {
            var timeSinceLastRequest = (DateTime.UtcNow - _lastSwapRequestTime).TotalSeconds;
            var timeoutSeconds = context.Configuration.PartyCoordination.TankSwapConfirmationTimeoutSeconds;

            if (timeSinceLastRequest > timeoutSeconds)
            {
                // Request coordinated swap
                partyCoord.RequestTankSwap(targetEntityId, true, 1);
                _lastSwapRequestTime = DateTime.UtcNow;
                context.Debug.EnmityState = "Requesting tank swap";
                return;
            }
        }

        // Push Provoke (solo or after timeout)
        scheduler.PushOgcd(
            GnbAbilities.Provoke,
            target.GameObjectId,
            priority: 1,
            onDispatched: _ =>
            {
                _lastProvokeTime = DateTime.UtcNow;
                partyCoord?.ClearTankSwapReservation(targetEntityId);
                context.Debug.PlannedAction = RoleActions.Provoke.Name;
                context.Debug.EnmityState = "Provoking (losing aggro)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.Provoke.ActionId, RoleActions.Provoke.Name)
                    .AsEnmity()
                    .Target(target.Name?.TextValue ?? "Enemy")
                    .Reason(
                        "Emergency Provoke - losing aggro",
                        "Provoke was used because you were losing aggro on the target. " +
                        "This can happen if DPS are bursting hard or if the co-tank is generating more enmity. " +
                        "Keep Royal Guard active and maintain your combo to hold aggro.")
                    .Factors("Losing aggro on target", "Provoke ready", "Need to maintain tank position")
                    .Alternatives("Let someone else tank (risky for party)", "Use more damage abilities (might be too slow)")
                    .Tip("If you keep losing aggro, check that Royal Guard is active. Use Provoke sparingly - it has a 30s cooldown.")
                    .Concept("gnb_provoke")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_provoke", true, "Aggro recovery");
            });
    }

    private void TryPushShirk(IHephaestusContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RoleActions.Shirk.MinLevel)
            return;

        if (!context.Configuration.Tank.AutoShirk)
            return;

        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            GNBActions.KeenEdge.ActionId,
            player);

        if (target == null)
            return;

        var partyCoord = context.PartyCoordinationService;
        var targetEntityId = (uint)target.GameObjectId;

        // Check if co-tank has requested a swap (they want to take aggro)
        var pendingSwap = partyCoord?.GetPendingTankSwapRequest(targetEntityId);
        if (pendingSwap != null && pendingSwap.IntendToTakeAggro)
        {
            // Find co-tank to shirk to
            var coTankForSwap = context.PartyHelper.FindCoTank(player);
            if (coTankForSwap == null)
            {
                context.Debug.EnmityState = "No co-tank found for swap";
                return;
            }

            context.Debug.EnmityState = "Shirk pending (swap pending)";

            partyCoord?.ConfirmTankSwap(targetEntityId);

            scheduler.PushOgcd(
                GnbAbilities.Shirk,
                coTankForSwap.GameObjectId,
                priority: 2,
                onDispatched: _ =>
                {
                    partyCoord?.ClearTankSwapReservation(targetEntityId);
                    context.Debug.PlannedAction = RoleActions.Shirk.Name;
                    context.Debug.EnmityState = "Shirking (coordinated swap)";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(RoleActions.Shirk.ActionId, RoleActions.Shirk.Name)
                        .AsEnmity()
                        .Target(coTankForSwap.Name?.TextValue ?? "Co-tank")
                        .Reason(
                            "Coordinated tank swap - giving aggro to co-tank",
                            "Shirk transfers 25% of your enmity to the target party member. " +
                            "This coordinated swap was initiated by your co-tank signaling they want to take aggro. " +
                            "Use Shirk after they Provoke for a clean swap.")
                        .Factors("Co-tank requested swap", "Shirk ready", "Confirming coordinated action")
                        .Alternatives("Ignore request (may cause failed mechanic)", "Wait longer (might mess up timing)")
                        .Tip("For clean swaps, the new MT Provokes first, then the old MT Shirks. This gives the new MT a large enmity lead.")
                        .Concept("gnb_shirk")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("gnb_shirk", true, "Coordinated tank swap");
                });
            return;
        }

        // Only shirk when co-tank has aggro and we're position #2
        if (!context.EnmityService.HasCoTankAggro(target, player.EntityId))
            return;

        var coTank = context.PartyHelper.FindCoTank(player);
        if (coTank == null)
        {
            context.Debug.EnmityState = "No co-tank found";
            return;
        }

        // Check distance to co-tank (Shirk range is 25y)
        var dx = player.Position.X - coTank.Position.X;
        var dy = player.Position.Y - coTank.Position.Y;
        var dz = player.Position.Z - coTank.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);

        if (distance > 25f)
        {
            context.Debug.EnmityState = "Co-tank too far for Shirk";
            return;
        }

        // Only auto-shirk if our enmity position is #2 (off-tank position)
        var myPosition = context.EnmityService.GetEnmityPosition(target, player.EntityId);
        if (myPosition != 2)
        {
            context.Debug.EnmityState = $"Position {myPosition}, not off-tanking";
            return;
        }

        scheduler.PushOgcd(
            GnbAbilities.Shirk,
            coTank.GameObjectId,
            priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.Shirk.Name;
                context.Debug.EnmityState = "Shirking to co-tank";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.Shirk.ActionId, RoleActions.Shirk.Name)
                    .AsEnmity()
                    .Target(coTank.Name?.TextValue ?? "Co-tank")
                    .Reason(
                        "Proactive Shirk - supporting main tank",
                        "Shirk is being used proactively to transfer enmity to the main tank. " +
                        "As off-tank, using Shirk helps the MT maintain a comfortable aggro lead " +
                        "and reduces the chance of accidentally pulling aggro during burst phases.")
                    .Factors("Currently off-tank (position #2)", "Co-tank has aggro", "Shirk helps maintain enmity gap")
                    .Alternatives("Don't Shirk (might pull aggro during burst)", "Wait for swap mechanic")
                    .Tip("Shirk on cooldown as off-tank to keep the MT's aggro lead comfortable, especially after your burst windows.")
                    .Concept("gnb_shirk")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_shirk", true, "Off-tank enmity support");
            });
    }

    #endregion

    #region Provoke

    private bool TryProvoke(IHephaestusContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RoleActions.Provoke.MinLevel)
            return false;

        // Check configuration
        if (!context.Configuration.Tank.AutoProvoke)
        {
            context.Debug.EnmityState = "AutoProvoke disabled";
            return false;
        }

        // Find current target
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            25f, // Provoke range
            player);

        if (target == null)
        {
            context.Debug.EnmityState = "No target";
            return false;
        }

        var partyCoord = context.PartyCoordinationService;
        var targetEntityId = (uint)target.GameObjectId;

        // Check if co-tank has requested a swap (they want to give aggro)
        var pendingSwap = partyCoord?.GetPendingTankSwapRequest(targetEntityId);
        if (pendingSwap != null && !pendingSwap.IntendToTakeAggro)
        {
            // Co-tank wants to give aggro - confirm and execute Provoke
            if (!context.ActionService.IsActionReady(RoleActions.Provoke.ActionId))
            {
                context.Debug.EnmityState = "Provoke on CD (swap pending)";
                return false;
            }

            partyCoord?.ConfirmTankSwap(targetEntityId);

            if (context.ActionService.ExecuteOgcd(RoleActions.Provoke, target.GameObjectId))
            {
                _lastProvokeTime = DateTime.UtcNow;
                partyCoord?.ClearTankSwapReservation(targetEntityId);
                context.Debug.PlannedAction = RoleActions.Provoke.Name;
                context.Debug.EnmityState = "Provoking (coordinated swap)";

                // Training: Record coordinated Provoke
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.Provoke.ActionId, RoleActions.Provoke.Name)
                    .AsEnmity()
                    .Target(target.Name?.TextValue ?? "Enemy")
                    .Reason(
                        "Coordinated tank swap - taking aggro from co-tank",
                        "Provoke instantly gives you top enmity on the target. " +
                        "This coordinated swap was initiated by your co-tank signaling they want to give up aggro. " +
                        "Tank swaps are essential for mechanics like tank buster debuffs.")
                    .Factors("Co-tank requested swap", "Provoke ready", "Confirming coordinated action")
                    .Alternatives("Ignore request (may cause wipe)", "Wait longer (might be too late)")
                    .Tip("After Provoking, maintain aggro with damage and Royal Guard. Your co-tank should Shirk to you for clean swap.")
                    .Concept("gnb_provoke")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_provoke", true, "Coordinated tank swap");

                return true;
            }
        }

        // Check if we're losing aggro on the target
        if (!context.EnmityService.IsLosingAggro(target, player.EntityId))
        {
            // We have aggro, all good
            var position = context.EnmityService.GetEnmityPosition(target, player.EntityId);
            context.Debug.EnmityState = position == 1 ? "Main tank" : $"Position {position}";
            return false;
        }

        // Apply provoke delay to prevent spam
        var timeSinceLastProvoke = (DateTime.UtcNow - _lastProvokeTime).TotalSeconds;
        if (timeSinceLastProvoke < context.Configuration.Tank.ProvokeDelay)
        {
            context.Debug.EnmityState = $"Provoke cooldown ({context.Configuration.Tank.ProvokeDelay - timeSinceLastProvoke:F1}s)";
            return false;
        }

        // Check if Provoke is ready
        if (!context.ActionService.IsActionReady(RoleActions.Provoke.ActionId))
        {
            context.Debug.EnmityState = "Provoke on CD";
            return false;
        }

        // If we have a remote tank, try coordinated swap first
        if (partyCoord?.HasRemoteTank == true && !partyCoord.IsTankSwapInProgress(targetEntityId))
        {
            var timeSinceLastRequest = (DateTime.UtcNow - _lastSwapRequestTime).TotalSeconds;
            var timeoutSeconds = context.Configuration.PartyCoordination.TankSwapConfirmationTimeoutSeconds;

            if (timeSinceLastRequest > timeoutSeconds)
            {
                // Request coordinated swap
                partyCoord.RequestTankSwap(targetEntityId, true, 1); // Priority 1 = losing aggro
                _lastSwapRequestTime = DateTime.UtcNow;
                context.Debug.EnmityState = "Requesting tank swap";
                return false; // Wait for confirmation
            }
        }

        // Execute Provoke (solo or after timeout)
        if (context.ActionService.ExecuteOgcd(RoleActions.Provoke, target.GameObjectId))
        {
            _lastProvokeTime = DateTime.UtcNow;
            partyCoord?.ClearTankSwapReservation(targetEntityId);
            context.Debug.PlannedAction = RoleActions.Provoke.Name;
            context.Debug.EnmityState = "Provoking (losing aggro)";

            // Training: Record emergency Provoke
            TrainingHelper.Decision(context.TrainingService)
                .Action(RoleActions.Provoke.ActionId, RoleActions.Provoke.Name)
                .AsEnmity()
                .Target(target.Name?.TextValue ?? "Enemy")
                .Reason(
                    "Emergency Provoke - losing aggro",
                    "Provoke was used because you were losing aggro on the target. " +
                    "This can happen if DPS are bursting hard or if the co-tank is generating more enmity. " +
                    "Keep Royal Guard active and maintain your combo to hold aggro.")
                .Factors("Losing aggro on target", "Provoke ready", "Need to maintain tank position")
                .Alternatives("Let someone else tank (risky for party)", "Use more damage abilities (might be too slow)")
                .Tip("If you keep losing aggro, check that Royal Guard is active. Use Provoke sparingly - it has a 30s cooldown.")
                .Concept("gnb_provoke")
                .Record();
            context.TrainingService?.RecordConceptApplication("gnb_provoke", true, "Aggro recovery");

            return true;
        }

        return false;
    }

    #endregion

    #region Shirk

    private bool TryShirk(IHephaestusContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RoleActions.Shirk.MinLevel)
            return false;

        // Check configuration
        if (!context.Configuration.Tank.AutoShirk)
        {
            return false;
        }

        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            GNBActions.KeenEdge.ActionId,
            player);

        if (target == null)
            return false;

        var partyCoord = context.PartyCoordinationService;
        var targetEntityId = (uint)target.GameObjectId;

        // Check if co-tank has requested a swap (they want to take aggro)
        var pendingSwap = partyCoord?.GetPendingTankSwapRequest(targetEntityId);
        if (pendingSwap != null && pendingSwap.IntendToTakeAggro)
        {
            // Co-tank wants to take aggro - confirm and execute Shirk
            if (!context.ActionService.IsActionReady(RoleActions.Shirk.ActionId))
            {
                context.Debug.EnmityState = "Shirk on CD (swap pending)";
                return false;
            }

            // Find co-tank to shirk to
            var coTankForSwap = context.PartyHelper.FindCoTank(player);
            if (coTankForSwap == null)
            {
                context.Debug.EnmityState = "No co-tank found for swap";
                return false;
            }

            partyCoord?.ConfirmTankSwap(targetEntityId);

            if (context.ActionService.ExecuteOgcd(RoleActions.Shirk, coTankForSwap.GameObjectId))
            {
                partyCoord?.ClearTankSwapReservation(targetEntityId);
                context.Debug.PlannedAction = RoleActions.Shirk.Name;
                context.Debug.EnmityState = "Shirking (coordinated swap)";

                // Training: Record coordinated Shirk
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.Shirk.ActionId, RoleActions.Shirk.Name)
                    .AsEnmity()
                    .Target(coTankForSwap.Name?.TextValue ?? "Co-tank")
                    .Reason(
                        "Coordinated tank swap - giving aggro to co-tank",
                        "Shirk transfers 25% of your enmity to the target party member. " +
                        "This coordinated swap was initiated by your co-tank signaling they want to take aggro. " +
                        "Use Shirk after they Provoke for a clean swap.")
                    .Factors("Co-tank requested swap", "Shirk ready", "Confirming coordinated action")
                    .Alternatives("Ignore request (may cause failed mechanic)", "Wait longer (might mess up timing)")
                    .Tip("For clean swaps, the new MT Provokes first, then the old MT Shirks. This gives the new MT a large enmity lead.")
                    .Concept("gnb_shirk")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_shirk", true, "Coordinated tank swap");

                return true;
            }
        }

        // Only shirk if we're main tank and should be off-tank
        // For now, only shirk when co-tank has aggro and we're #2

        // Check if co-tank has aggro
        if (!context.EnmityService.HasCoTankAggro(target, player.EntityId))
            return false;

        // Find co-tank to shirk to
        var coTank = context.PartyHelper.FindCoTank(player);
        if (coTank == null)
        {
            context.Debug.EnmityState = "No co-tank found";
            return false;
        }

        // Check distance to co-tank (Shirk range is 25y)
        var dx = player.Position.X - coTank.Position.X;
        var dy = player.Position.Y - coTank.Position.Y;
        var dz = player.Position.Z - coTank.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);

        if (distance > 25f)
        {
            context.Debug.EnmityState = "Co-tank too far for Shirk";
            return false;
        }

        // Check if Shirk is ready
        if (!context.ActionService.IsActionReady(RoleActions.Shirk.ActionId))
        {
            context.Debug.EnmityState = "Shirk on CD";
            return false;
        }

        // Only auto-shirk if our enmity position is #2 (off-tank position)
        var myPosition = context.EnmityService.GetEnmityPosition(target, player.EntityId);
        if (myPosition != 2)
        {
            context.Debug.EnmityState = $"Position {myPosition}, not off-tanking";
            return false;
        }

        // Execute Shirk
        if (context.ActionService.ExecuteOgcd(RoleActions.Shirk, coTank.GameObjectId))
        {
            context.Debug.PlannedAction = RoleActions.Shirk.Name;
            context.Debug.EnmityState = "Shirking to co-tank";

            // Training: Record proactive Shirk
            TrainingHelper.Decision(context.TrainingService)
                .Action(RoleActions.Shirk.ActionId, RoleActions.Shirk.Name)
                .AsEnmity()
                .Target(coTank.Name?.TextValue ?? "Co-tank")
                .Reason(
                    "Proactive Shirk - supporting main tank",
                    "Shirk is being used proactively to transfer enmity to the main tank. " +
                    "As off-tank, using Shirk helps the MT maintain a comfortable aggro lead " +
                    "and reduces the chance of accidentally pulling aggro during burst phases.")
                .Factors("Currently off-tank (position #2)", "Co-tank has aggro", "Shirk helps maintain enmity gap")
                .Alternatives("Don't Shirk (might pull aggro during burst)", "Wait for swap mechanic")
                .Tip("Shirk on cooldown as off-tank to keep the MT's aggro lead comfortable, especially after your burst windows.")
                .Concept("gnb_shirk")
                .Record();
            context.TrainingService?.RecordConceptApplication("gnb_shirk", true, "Off-tank enmity support");

            return true;
        }

        return false;
    }

    #endregion
}
