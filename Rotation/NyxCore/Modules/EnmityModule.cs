using System;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.NyxCore.Modules;

/// <summary>
/// Handles the Dark Knight enmity management.
/// Manages Provoke and Shirk for threat control.
/// Coordinates with other Olympus tank instances via IPC.
/// </summary>
public sealed class EnmityModule : INyxModule
{
    public int Priority => 5; // Highest priority - enmity management is critical
    public string Name => "Enmity";

    private DateTime _lastProvokeTime = DateTime.MinValue;
    private DateTime _lastSwapRequestTime = DateTime.MinValue;

    public bool TryExecute(INyxContext context, bool isMoving)
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

    public void UpdateDebugState(INyxContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Provoke

    private bool TryProvoke(INyxContext context)
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
                    .Target(target.Name?.TextValue)
                    .Reason(
                        "Coordinated tank swap - co-tank requested aggro transfer. Provoke used to take boss aggro.",
                        "Provoke instantly puts you at top of enmity list. Use it for planned tank swaps (tankbusters, debuff drops).")
                    .Factors("Co-tank requested swap via IPC", "Provoke available", $"Target: {target.Name?.TextValue}")
                    .Alternatives("Ignore swap request (may cause wipe)", "Wait longer (mechanics may not allow)")
                    .Tip("Always respond to coordinated tank swap requests promptly. The co-tank likely needs to drop a debuff or handle a mechanic.")
                    .Concept(DrkConcepts.TankSwap)
                    .Record();

                context.TrainingService?.RecordConceptApplication(DrkConcepts.TankSwap, wasSuccessful: true);

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
                .Target(target.Name?.TextValue)
                .Reason(
                    "Emergency Provoke - you were losing aggro to another player. Boss must stay on a tank.",
                    "Provoke instantly puts you at top of enmity list. Use it when losing aggro to reclaim the boss before it attacks squishies.")
                .Factors("Lost aggro to non-tank", "Boss about to attack party", $"Target: {target.Name?.TextValue}")
                .Alternatives("Let co-tank take it (risky if unprepared)", "Use enmity combo (too slow in emergencies)")
                .Tip("If you're losing aggro as main tank, Provoke immediately. DPS dying to auto-attacks is always worse than using a cooldown.")
                .Concept(DrkConcepts.TankSwap)
                .Record();

            context.TrainingService?.RecordConceptApplication(DrkConcepts.TankSwap, wasSuccessful: true);

            return true;
        }

        return false;
    }

    #endregion

    #region Shirk

    private bool TryShirk(INyxContext context)
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
            DRKActions.HardSlash.ActionId,
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
                    .Target(coTankForSwap.Name?.TextValue)
                    .Reason(
                        "Coordinated tank swap - co-tank requested to take aggro. Shirk used to transfer enmity.",
                        "Shirk transfers 25% of your enmity to the target. Use it when your co-tank Provokes to ensure a clean swap.")
                    .Factors("Co-tank requested swap via IPC", "Shirk available", $"Co-tank: {coTankForSwap.Name?.TextValue}")
                    .Alternatives("Ignore swap (co-tank struggles to hold aggro)", "Keep aggro (may cause mechanic failures)")
                    .Tip("After co-tank Provokes, use Shirk immediately. The 25% enmity transfer ensures they maintain aggro without risk of you pulling back.")
                    .Concept(DrkConcepts.TankSwap)
                    .Record();

                context.TrainingService?.RecordConceptApplication(DrkConcepts.TankSwap, wasSuccessful: true);

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
                .Target(coTank.Name?.TextValue)
                .Reason(
                    "Proactive Shirk - you're in off-tank position but building enmity. Shirk helps main tank maintain aggro lead.",
                    "Shirk transfers 25% of your enmity to the target. As off-tank, use it to prevent accidentally pulling aggro.")
                .Factors("You're in off-tank position (#2)", "Building enmity from DPS rotation", $"Co-tank: {coTank.Name?.TextValue}")
                .Alternatives("Stop DPSing (massive damage loss)", "Let main tank use Provoke (wastes their cooldown)")
                .Tip("As off-tank, Shirk periodically to stay comfortable below the main tank. This lets you maintain full DPS without risk of pulling.")
                .Concept(DrkConcepts.TankSwap)
                .Record();

            context.TrainingService?.RecordConceptApplication(DrkConcepts.TankSwap, wasSuccessful: true);

            return true;
        }

        return false;
    }

    #endregion
}
