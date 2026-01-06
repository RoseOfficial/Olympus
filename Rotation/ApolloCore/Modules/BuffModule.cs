using System;
using System.Numerics;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// Handles buff and utility oGCDs for the WHM rotation.
/// Includes Thin Air, Presence of Mind, Assize, Asylum, Lucid Dreaming, Surecast, Aetherial Shift.
/// </summary>
public sealed class BuffModule : IApolloModule
{
    private const int RaiseMpCost = 2400;

    public int Priority => 30; // After healing/defensive, before damage
    public string Name => "Buffs";

    public bool TryExecute(ApolloContext context, bool isMoving)
    {
        if (!context.CanExecuteOgcd)
            return false;

        // Priority 1: Thin Air before expensive casts
        if (TryExecuteThinAir(context))
            return true;

        // Priority 2: Presence of Mind on cooldown (DPS buff)
        if (TryExecutePresenceOfMind(context))
            return true;

        // Priority 3: Asylum on cooldown (ground-targeted HoT)
        if (TryExecuteAsylum(context))
            return true;

        // Priority 4: Assize on cooldown (DPS oGCD that also heals)
        if (TryExecuteAssize(context))
            return true;

        // Priority 5: Lucid Dreaming for MP
        if (TryExecuteLucidDreaming(context))
            return true;

        // Priority 6: Surecast for knockback immunity
        if (TryExecuteSurecast(context))
            return true;

        // Priority 7: Aetherial Shift for gap closing
        if (!isMoving)
            TryExecuteAetherialShift(context);

        return false;
    }

    public void UpdateDebugState(ApolloContext context)
    {
        // Update Thin Air state
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Buffs.EnableThinAir)
        {
            context.Debug.ThinAirState = "Disabled";
        }
        else if (player.Level < WHMActions.ThinAir.MinLevel)
        {
            context.Debug.ThinAirState = $"Level {player.Level} < 58";
        }
        else if (context.HasThinAir)
        {
            context.Debug.ThinAirState = "Already active";
        }
        else if (!context.ActionService.IsActionReady(WHMActions.ThinAir.ActionId))
        {
            context.Debug.ThinAirState = "On cooldown";
        }
        else
        {
            context.Debug.ThinAirState = "Ready";
        }
    }

    private bool TryExecuteThinAir(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Buffs.EnableThinAir)
        {
            context.Debug.ThinAirState = "Disabled";
            return false;
        }

        if (player.Level < WHMActions.ThinAir.MinLevel)
        {
            context.Debug.ThinAirState = $"Level {player.Level} < 58";
            return false;
        }

        if (!context.ActionService.IsActionReady(WHMActions.ThinAir.ActionId))
        {
            context.Debug.ThinAirState = "On cooldown";
            return false;
        }

        if (context.HasThinAir)
        {
            context.Debug.ThinAirState = "Already active";
            return false;
        }

        var shouldUseThinAir = false;

        // Priority 1: Raise incoming (highest MP cost at 2400)
        if (config.Resurrection.EnableRaise && player.CurrentMp >= RaiseMpCost)
        {
            var deadMember = context.PartyHelper.FindDeadPartyMemberNeedingRaise(player);
            if (deadMember is not null)
            {
                var swiftcastReady = context.ActionService.IsActionReady(WHMActions.Swiftcast.ActionId);

                if (context.HasSwiftcast || swiftcastReady || config.Resurrection.AllowHardcastRaise)
                {
                    shouldUseThinAir = true;
                    context.Debug.ThinAirState = "For Raise";
                }
            }
        }

        // Priority 2: High-cost AoE heal incoming
        if (!shouldUseThinAir && config.EnableHealing)
        {
            var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
            var medicaHealAmount = WHMActions.Medica.EstimateHealAmount(mind, det, wd, player.Level);

            var (injuredCount, _, _, _) = context.PartyHelper.CountPartyMembersNeedingAoEHeal(player, medicaHealAmount);

            if (injuredCount >= config.Healing.AoEHealMinTargets)
            {
                shouldUseThinAir = true;
                context.Debug.ThinAirState = "For AoE Heal";
            }
        }

        // Priority 3: High-cost single heal incoming
        if (!shouldUseThinAir && config.EnableHealing && player.Level >= WHMActions.CureII.MinLevel)
        {
            var target = context.PartyHelper.FindLowestHpPartyMember(player);
            if (target is not null)
            {
                var hpPercent = context.PartyHelper.GetHpPercent(target);
                if (hpPercent < 0.80f)
                {
                    shouldUseThinAir = true;
                    context.Debug.ThinAirState = "For Cure II";
                }
            }
        }

        if (!shouldUseThinAir)
        {
            context.Debug.ThinAirState = "Not needed";
            return false;
        }

        if (ActionExecutor.ExecuteOgcd(context, WHMActions.ThinAir, player.GameObjectId,
            player.Name?.TextValue ?? "Unknown", player.CurrentMp))
        {
            return true;
        }

        context.Debug.ThinAirState = "Execution failed";
        return false;
    }

    private bool TryExecutePresenceOfMind(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.PresenceOfMind, config,
            c => c.Buffs.EnablePresenceOfMind))
            return false;

        // Check if we should delay PoM for an incoming raise
        if (config.Buffs.DelayPoMForRaise && config.Resurrection.EnableRaise)
        {
            var deadMember = context.PartyHelper.FindDeadPartyMemberNeedingRaise(player);
            if (deadMember is not null)
            {
                // Check if Swiftcast is ready or coming soon
                var swiftcastReady = context.ActionService.IsActionReady(WHMActions.Swiftcast.ActionId);
                var swiftcastCooldown = context.ActionService.GetCooldownRemaining(WHMActions.Swiftcast.ActionId);

                // Don't use PoM if Swiftcast is about to be ready and we need to raise
                if (!swiftcastReady && swiftcastCooldown <= config.Buffs.PoMRaiseDelayCooldown)
                {
                    context.Debug.PoMState = $"Delayed for Raise (Swiftcast in {swiftcastCooldown:F1}s)";
                    return false;
                }
            }
        }

        // Check if we should wait to stack PoM with Assize
        if (config.Buffs.StackPoMWithAssize && player.Level >= WHMActions.Assize.MinLevel)
        {
            var assizeReady = context.ActionService.IsActionReady(WHMActions.Assize.ActionId);
            var assizeCooldown = context.ActionService.GetCooldownRemaining(WHMActions.Assize.ActionId);

            // If Assize is almost ready (within 5s), delay PoM to stack them
            if (!assizeReady && assizeCooldown <= 5f && assizeCooldown > 0)
            {
                context.Debug.PoMState = $"Waiting for Assize ({assizeCooldown:F1}s)";
                return false;
            }
        }

        if (ActionExecutor.ExecuteOgcd(context, WHMActions.PresenceOfMind, player.GameObjectId,
            player.Name?.TextValue ?? "Unknown", player.CurrentHp))
        {
            context.Debug.PoMState = "Executed";
            return true;
        }

        return false;
    }

    private bool TryExecuteAsylum(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.Healing.EnableAsylum)
        {
            context.Debug.AsylumState = "Disabled";
            return false;
        }

        if (player.Level < WHMActions.Asylum.MinLevel)
        {
            context.Debug.AsylumState = $"Level {player.Level} < {WHMActions.Asylum.MinLevel}";
            return false;
        }

        if (!context.ActionService.IsActionReady(WHMActions.Asylum.ActionId))
        {
            var cd = context.ActionService.GetCooldownRemaining(WHMActions.Asylum.ActionId);
            context.Debug.AsylumState = $"CD: {cd:F1}s";
            return false;
        }

        var tank = context.PartyHelper.FindTankInParty(player);
        Vector3 targetPosition;

        if (tank is not null)
        {
            var tankName = tank.Name?.TextValue ?? "Unknown";
            var distance = Vector3.Distance(player.Position, tank.Position);
            if (distance > WHMActions.Asylum.Range)
            {
                context.Debug.AsylumState = $"Tank out of range ({distance:F1}y > {WHMActions.Asylum.Range}y)";
                context.Debug.AsylumTarget = tankName;
                return false;
            }
            targetPosition = tank.Position;
            context.Debug.AsylumTarget = tankName;
        }
        else
        {
            targetPosition = player.Position;
            context.Debug.AsylumTarget = "Self";
        }

        if (ActionExecutor.ExecuteGroundTargeted(context, WHMActions.Asylum, targetPosition,
            context.Debug.AsylumTarget, tank?.CurrentHp ?? player.CurrentHp,
            $"Asylum (on {context.Debug.AsylumTarget})"))
        {
            context.Debug.AsylumState = "Executed";
            return true;
        }

        context.Debug.AsylumState = "Execution failed";
        return false;
    }

    private bool TryExecuteAssize(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.Assize, config,
            c => c.Healing.EnableAssize))
            return false;

        return ActionExecutor.ExecuteOgcd(context, WHMActions.Assize, player.GameObjectId,
            player.Name?.TextValue ?? "Unknown", player.CurrentHp);
    }

    private bool TryExecuteLucidDreaming(ApolloContext context)
    {
        var player = context.Player;
        var mpPercent = (float)player.CurrentMp / player.MaxMp;

        if (mpPercent >= 0.7f)
            return false;

        if (player.Level < 24)
            return false;

        if (!context.ActionService.IsActionReady(WHMActions.LucidDreaming.ActionId))
            return false;

        return ActionExecutor.ExecuteOgcd(context, WHMActions.LucidDreaming, player.GameObjectId,
            player.Name?.TextValue ?? "Unknown", player.CurrentMp);
    }

    private bool TryExecuteSurecast(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.RoleActions.EnableSurecast)
        {
            context.Debug.SurecastState = "Disabled";
            return false;
        }

        // Manual mode (0) - never auto-execute
        if (config.RoleActions.SurecastMode == 0)
        {
            context.Debug.SurecastState = "Manual mode";
            return false;
        }

        if (player.Level < WHMActions.Surecast.MinLevel)
        {
            context.Debug.SurecastState = $"Level {player.Level} < {WHMActions.Surecast.MinLevel}";
            return false;
        }

        if (StatusHelper.HasStatus(player, StatusHelper.StatusIds.Surecast))
        {
            context.Debug.SurecastState = "Already active";
            return false;
        }

        if (!context.ActionService.IsActionReady(WHMActions.Surecast.ActionId))
        {
            var cd = context.ActionService.GetCooldownRemaining(WHMActions.Surecast.ActionId);
            context.Debug.SurecastState = $"CD: {cd:F1}s";
            return false;
        }

        // Mode 1: Use on cooldown in combat
        if (config.RoleActions.SurecastMode == 1)
        {
            if (ActionExecutor.ExecuteOgcd(context, WHMActions.Surecast, player.GameObjectId,
                player.Name?.TextValue ?? "Unknown", player.CurrentHp))
            {
                context.Debug.SurecastState = "Executed";
                return true;
            }
        }

        context.Debug.SurecastState = "Ready";
        return false;
    }

    private void TryExecuteAetherialShift(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Buffs.EnableAetherialShift)
            return;

        if (!ActionValidator.IsAvailable(player, context.ActionService, WHMActions.AetherialShift))
            return;

        const float dashDistance = 15f;
        var spellRange = WHMActions.Stone.Range;
        var target = context.TargetingService.FindEnemy(
            config.Targeting.EnemyStrategy,
            spellRange + dashDistance,
            player);

        if (target is null)
            return;

        var distance = Vector3.Distance(player.Position, target.Position);

        if (distance <= spellRange)
            return;

        if (distance < 0.01f)
            return;

        var toTarget = Vector3.Normalize(target.Position - player.Position);
        var playerForward = new Vector3(
            MathF.Sin(player.Rotation),
            0,
            MathF.Cos(player.Rotation));
        var dot = Vector3.Dot(playerForward, new Vector3(toTarget.X, 0, toTarget.Z));

        if (dot < 0.7f)
            return;

        ActionExecutor.ExecuteOgcd(context, WHMActions.AetherialShift, player.GameObjectId,
            target.Name?.TextValue ?? "Unknown", target.CurrentHp,
            "Aetherial Shift (gap close)");
    }
}
