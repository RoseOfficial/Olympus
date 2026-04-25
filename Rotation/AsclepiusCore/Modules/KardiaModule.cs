using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Handles Kardia / Soteria / Philosophia for Sage (scheduler-driven).
/// Push priorities are 0-3 so Kardia placement wins against Resurrection (1-2).
/// </summary>
public sealed class KardiaModule : IAsclepiusModule
{
    public int Priority => 3;
    public string Name => "Kardia";

    public bool TryExecute(IAsclepiusContext context, bool isMoving) => false;

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        TryPushPlaceKardia(context, scheduler);
        TryPushEnsureKardiaOnTank(context, scheduler);
        TryPushSoteria(context, scheduler);
        TryPushPhilosophia(context, scheduler);
    }

    public void UpdateDebugState(IAsclepiusContext context)
    {
        context.Debug.KardiaTarget = context.HasKardiaPlaced
            ? $"ID: {context.KardiaTargetId}"
            : "None";
        context.Debug.KardiaState = context.HasKardiaPlaced ? "Active" : "Not placed";
        context.Debug.SoteriaStacks = context.KardiaManager.GetSoteriaStacks(context.Player);
        context.Debug.SoteriaState = context.HasSoteria ? "Active" : "Idle";
        context.Debug.PhilosophiaState = context.HasPhilosophia ? "Active" : "Idle";
    }

    private void TryPushPlaceKardia(IAsclepiusContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.AutoKardia) return;
        if (context.HasKardiaPlaced) return;
        if (player.Level < SGEActions.Kardia.MinLevel) return;

        var target = FindKardiaTarget(context);
        if (target == null) return;

        var capturedTarget = target;
        var action = SGEActions.Kardia;

        scheduler.PushOgcd(AsclepiusAbilities.Kardia, target.GameObjectId, priority: 0,
            onDispatched: _ =>
            {
                context.KardiaManager.RecordSwap(capturedTarget.GameObjectId);
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Placing Kardia";
                context.LogKardiaDecision(capturedTarget.Name?.TextValue ?? "Unknown", "Place", "Tank needs Kardia");

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    var isTank = context.PartyHelper.FindTankInParty(context.Player)?.GameObjectId == capturedTarget.GameObjectId;

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Kardia",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = $"Kardia placed on {targetName}" + (isTank ? " (tank)" : ""),
                        DetailedReason = $"Kardia placed on {targetName}. Kardia is SGE's signature ability - every time you deal damage, the Kardia target receives a 170 potency heal! This is PASSIVE healing that costs nothing. Always have Kardia active on someone taking damage (usually the tank).",
                        Factors = new[]
                        {
                            isTank ? "Target: Tank (primary damage taker)" : "Target: Lowest HP party member",
                            "170 potency heal per damaging GCD",
                            "No cooldown, instant swap",
                            "FUNDAMENTAL to SGE gameplay",
                        },
                        Alternatives = new[]
                        {
                            "Could place on different target",
                            "No alternatives - Kardia should ALWAYS be placed",
                        },
                        Tip = "Kardia is SGE's bread and butter! It provides constant healing while you DPS. Always keep it on someone - usually the tank. Swap it to other targets when needed for quick passive healing!",
                        ConceptId = SgeConcepts.KardiaManagement,
                        Priority = ExplanationPriority.High,
                    });
                }
            });
    }

    private void TryPushEnsureKardiaOnTank(IAsclepiusContext context, RotationScheduler scheduler)
    {
        if (!context.HasKardiaPlaced) return;
        if (!context.CanSwapKardia) return;

        var player = context.Player;
        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank == null) return;
        if (tank.GameObjectId == context.KardiaTargetId) return;

        var capturedTank = tank;
        var action = SGEActions.Kardia;

        scheduler.PushOgcd(AsclepiusAbilities.Kardia, tank.GameObjectId, priority: 0,
            onDispatched: _ =>
            {
                context.KardiaManager.RecordSwap(capturedTank.GameObjectId);
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Kardia -> Tank";
                context.LogKardiaDecision(capturedTank.Name?.TextValue ?? "Unknown", "EnsureTank", "Kardia not on tank, moving back");
            });
    }

    private void TryPushSoteria(IAsclepiusContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableSoteria) return;
        if (player.Level < SGEActions.Soteria.MinLevel) return;
        if (context.HasSoteria) return;
        if (!context.HasKardiaPlaced) return;
        if (!context.ActionService.IsActionReady(SGEActions.Soteria.ActionId)) return;

        var kardiaTarget = FindKardiaTargetById(context, context.KardiaTargetId);
        if (kardiaTarget == null) return;

        var hpPercent = kardiaTarget.MaxHp > 0 ? (float)kardiaTarget.CurrentHp / kardiaTarget.MaxHp : 1f;
        if (hpPercent > config.SoteriaThreshold) return;

        var capturedTarget = kardiaTarget;
        var capturedHpPercent = hpPercent;
        var action = SGEActions.Soteria;

        scheduler.PushOgcd(AsclepiusAbilities.Soteria, player.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Soteria";
                context.LogKardiaDecision(capturedTarget.Name?.TextValue ?? "Unknown", "Soteria", $"HP {capturedHpPercent:P0}");

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Soteria",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = $"Soteria - boosting Kardia heals (target at {capturedHpPercent:P0})",
                        DetailedReason = $"Soteria activated with Kardia target {targetName} at {capturedHpPercent:P0} HP. Soteria increases Kardia healing potency by 50% for 15 seconds (4 stacks consumed by your attacks).",
                        Factors = new[]
                        {
                            $"Kardia target HP: {capturedHpPercent:P0}",
                            $"Threshold: {config.SoteriaThreshold:P0}",
                            "50% Kardia potency boost (170 -> 255 per hit)",
                            "4 stacks over 15s",
                            "90s cooldown",
                        },
                        Alternatives = new[] { "Druochole (direct heal)", "Taurochole (heal + mit for tanks)", "Swap Kardia + continue DPS" },
                        Tip = "Soteria is FREE extra healing! It boosts Kardia by 50% for 4 attacks. Use it when your Kardia target is taking sustained damage.",
                        ConceptId = SgeConcepts.SoteriaUsage,
                        Priority = ExplanationPriority.Normal,
                    });
                }
            });
    }

    private void TryPushPhilosophia(IAsclepiusContext context, RotationScheduler scheduler)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePhilosophia) return;
        if (player.Level < SGEActions.Philosophia.MinLevel) return;
        if (context.HasPhilosophia) return;
        if (!context.ActionService.IsActionReady(SGEActions.Philosophia.ActionId)) return;

        var (avgHp, _, _) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.PhilosophiaThreshold) return;

        var capturedAvgHp = avgHp;
        var action = SGEActions.Philosophia;

        scheduler.PushOgcd(AsclepiusAbilities.Philosophia, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Philosophia";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Philosophia",
                        Category = "Healing",
                        TargetName = "Party",
                        ShortReason = $"Philosophia - party-wide Kardia (party at {capturedAvgHp:P0})",
                        DetailedReason = $"Philosophia activated with party at {capturedAvgHp:P0} average HP. For 20 seconds, your damaging attacks heal ALL party members for 100 potency (instead of just the Kardia target). This is incredible sustained party healing while you DPS!",
                        Factors = new[]
                        {
                            $"Party avg HP: {capturedAvgHp:P0}",
                            $"Threshold: {config.PhilosophiaThreshold:P0}",
                            "100 potency party heal per damaging attack",
                            "20s duration",
                            "180s cooldown",
                        },
                        Alternatives = new[] { "Kerachole (AoE regen + mit)", "Ixochole (instant AoE heal)", "Physis II (AoE HoT)" },
                        Tip = "Philosophia is AMAZING for sustained party healing! For 20 seconds, every attack you land heals the ENTIRE party.",
                        ConceptId = SgeConcepts.PhilosophiaUsage,
                        Priority = ExplanationPriority.High,
                    });
                }
            });
    }

    private IBattleChara? FindKardiaTarget(IAsclepiusContext context)
    {
        var player = context.Player;
        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank != null) return tank;
        var lowestHp = context.PartyHelper.FindLowestHpPartyMember(player);
        if (lowestHp != null && lowestHp.GameObjectId != player.GameObjectId) return lowestHp;
        return player;
    }

    private IBattleChara? FindKardiaTargetById(IAsclepiusContext context, ulong targetId)
    {
        if (targetId == 0) return null;
        foreach (var member in context.PartyHelper.GetAllPartyMembers(context.Player))
        {
            if (member.GameObjectId == targetId) return member;
        }
        return null;
    }
}
