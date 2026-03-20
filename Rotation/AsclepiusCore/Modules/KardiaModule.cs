using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Handles Kardia management for Sage.
/// Kardia places a buff on a target that heals them when the Sage deals damage.
/// Priority 3 - essential to have Kardia placed before combat.
/// </summary>
public sealed class KardiaModule : IAsclepiusModule
{
    public int Priority => 3; // Very high priority - Kardia is essential
    public string Name => "Kardia";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        // Priority 1: Place Kardia if not present
        if (context.CanExecuteOgcd && TryPlaceKardia(context))
            return true;

        // Priority 2: Soteria (boosted Kardia healing)
        if (context.CanExecuteOgcd && TrySoteria(context))
            return true;

        // Priority 3: Philosophia (party-wide Kardia)
        if (context.CanExecuteOgcd && TryPhilosophia(context))
            return true;

        // Priority 4: Swap Kardia to a more needy target
        if (context.CanExecuteOgcd && TrySwapKardia(context))
            return true;

        return false;
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

    private bool TryPlaceKardia(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.AutoKardia)
            return false;

        // Already have Kardia placed
        if (context.HasKardiaPlaced)
            return false;

        if (player.Level < SGEActions.Kardia.MinLevel)
            return false;

        // Find the main tank to place Kardia on
        var target = FindKardiaTarget(context);
        if (target == null)
            return false;

        var action = SGEActions.Kardia;
        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.KardiaManager.RecordSwap(target.GameObjectId);
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Placing Kardia";
            context.LogKardiaDecision(target.Name?.TextValue ?? "Unknown", "Place", "Tank needs Kardia");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var isTank = context.PartyHelper.FindTankInParty(player)?.GameObjectId == target.GameObjectId;

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

            return true;
        }

        return false;
    }

    private bool TrySoteria(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableSoteria)
            return false;

        if (player.Level < SGEActions.Soteria.MinLevel)
            return false;

        // Already have Soteria active
        if (context.HasSoteria)
            return false;

        // Must have Kardia placed
        if (!context.HasKardiaPlaced)
            return false;

        // Check cooldown
        if (!context.ActionService.IsActionReady(SGEActions.Soteria.ActionId))
            return false;

        // Use when Kardia target is low
        var kardiaTarget = FindKardiaTargetById(context, context.KardiaTargetId);
        if (kardiaTarget == null)
            return false;

        var hpPercent = kardiaTarget.MaxHp > 0 ? (float)kardiaTarget.CurrentHp / kardiaTarget.MaxHp : 1f;
        if (hpPercent > config.SoteriaThreshold)
            return false;

        var action = SGEActions.Soteria;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Soteria";
            context.LogKardiaDecision(kardiaTarget.Name?.TextValue ?? "Unknown", "Soteria", $"HP {hpPercent:P0}");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = kardiaTarget.Name?.TextValue ?? "Unknown";

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Soteria",
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = $"Soteria - boosting Kardia heals (target at {hpPercent:P0})",
                    DetailedReason = $"Soteria activated with Kardia target {targetName} at {hpPercent:P0} HP. Soteria increases Kardia healing potency by 50% for 15 seconds (4 stacks consumed by your attacks). Combined with normal DPS, this provides significant sustained healing without spending resources!",
                    Factors = new[]
                    {
                        $"Kardia target HP: {hpPercent:P0}",
                        $"Threshold: {config.SoteriaThreshold:P0}",
                        "50% Kardia potency boost (170 → 255 per hit)",
                        "4 stacks over 15s",
                        "90s cooldown",
                    },
                    Alternatives = new[]
                    {
                        "Druochole (direct heal)",
                        "Taurochole (heal + mit for tanks)",
                        "Swap Kardia + continue DPS",
                    },
                    Tip = "Soteria is FREE extra healing! It boosts Kardia by 50% for 4 attacks. Use it when your Kardia target is taking sustained damage (like during auto-attacks between busters). Great for passive tank healing!",
                    ConceptId = SgeConcepts.SoteriaUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryPhilosophia(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePhilosophia)
            return false;

        if (player.Level < SGEActions.Philosophia.MinLevel)
            return false;

        // Already have Philosophia active
        if (context.HasPhilosophia)
            return false;

        // Check cooldown
        if (!context.ActionService.IsActionReady(SGEActions.Philosophia.ActionId))
            return false;

        // Use when party HP is low - provides party-wide Kardia healing
        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (avgHp > config.PhilosophiaThreshold)
            return false;

        var action = SGEActions.Philosophia;
        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Philosophia";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Philosophia",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = $"Philosophia - party-wide Kardia (party at {avgHp:P0})",
                    DetailedReason = $"Philosophia activated with party at {avgHp:P0} average HP. For 20 seconds, your damaging attacks heal ALL party members for 100 potency (instead of just the Kardia target). This is incredible sustained party healing while you DPS!",
                    Factors = new[]
                    {
                        $"Party avg HP: {avgHp:P0}",
                        $"Threshold: {config.PhilosophiaThreshold:P0}",
                        "100 potency party heal per damaging attack",
                        "20s duration",
                        "180s cooldown",
                    },
                    Alternatives = new[]
                    {
                        "Kerachole (AoE regen + mit)",
                        "Ixochole (instant AoE heal)",
                        "Physis II (AoE HoT)",
                    },
                    Tip = "Philosophia is AMAZING for sustained party healing! For 20 seconds, every attack you land heals the ENTIRE party. Use during periods of sustained party damage - it's like having party-wide Kardia!",
                    ConceptId = SgeConcepts.PhilosophiaUsage,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }

    private bool TrySwapKardia(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.KardiaSwapEnabled)
            return false;

        if (!context.HasKardiaPlaced)
            return false;

        if (!context.CanSwapKardia)
            return false;

        // Get current Kardia target HP
        var currentTarget = FindKardiaTargetById(context, context.KardiaTargetId);
        if (currentTarget == null)
            return false;

        var currentHpPercent = currentTarget.MaxHp > 0
            ? (float)currentTarget.CurrentHp / currentTarget.MaxHp
            : 1f;

        // Find a better target
        var (newTarget, newHpPercent) = FindBetterKardiaTarget(context, context.KardiaTargetId);
        if (newTarget == null)
            return false;

        // Check if we should swap using the KardiaManager logic
        if (!context.KardiaManager.ShouldSwapKardia(currentHpPercent, newHpPercent, config.KardiaSwapThreshold))
            return false;

        var action = SGEActions.Kardia;
        if (context.ActionService.ExecuteOgcd(action, newTarget.GameObjectId))
        {
            context.KardiaManager.RecordSwap(newTarget.GameObjectId);
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Kardia Swap";
            context.LogKardiaDecision(newTarget.Name?.TextValue ?? "Unknown", "Swap",
                $"From {currentHpPercent:P0} to {newHpPercent:P0}");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var oldTargetName = currentTarget.Name?.TextValue ?? "Unknown";
                var newTargetName = newTarget.Name?.TextValue ?? "Unknown";

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Kardia (Swap)",
                    Category = "Healing",
                    TargetName = newTargetName,
                    ShortReason = $"Kardia swap: {oldTargetName} ({currentHpPercent:P0}) → {newTargetName} ({newHpPercent:P0})",
                    DetailedReason = $"Kardia swapped from {oldTargetName} ({currentHpPercent:P0} HP) to {newTargetName} ({newHpPercent:P0} HP). The new target has significantly lower HP and will benefit more from Kardia's passive healing. Kardia swaps are instant and free!",
                    Factors = new[]
                    {
                        $"Old target: {oldTargetName} at {currentHpPercent:P0}",
                        $"New target: {newTargetName} at {newHpPercent:P0}",
                        $"HP difference: {(currentHpPercent - newHpPercent):P0}",
                        $"Swap threshold: {config.KardiaSwapThreshold:P0}",
                    },
                    Alternatives = new[]
                    {
                        "Keep Kardia on current target",
                        "Direct heal instead of relying on Kardia",
                    },
                    Tip = "Smart Kardia swapping is part of SGE mastery! The swap is instant with no cooldown, so don't hesitate to move it to whoever needs it most. Just don't swap too frantically - let it tick a few times before moving again.",
                    ConceptId = SgeConcepts.KardiaTargetSelection,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private IBattleChara? FindKardiaTarget(IAsclepiusContext context)
    {
        var player = context.Player;

        // Priority 1: Tank in party
        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank != null)
            return tank;

        // Priority 2: Lowest HP party member (that isn't us)
        var lowestHp = context.PartyHelper.FindLowestHpPartyMember(player);
        if (lowestHp != null && lowestHp.GameObjectId != player.GameObjectId)
            return lowestHp;

        // Fallback: Self
        return player;
    }

    private IBattleChara? FindKardiaTargetById(IAsclepiusContext context, ulong targetId)
    {
        if (targetId == 0)
            return null;

        foreach (var member in context.PartyHelper.GetAllPartyMembers(context.Player))
        {
            if (member.GameObjectId == targetId)
                return member;
        }

        return null;
    }

    private (IBattleChara? target, float hpPercent) FindBetterKardiaTarget(
        IAsclepiusContext context,
        ulong currentTargetId)
    {
        IBattleChara? bestTarget = null;
        var lowestHp = 1f;

        foreach (var member in context.PartyHelper.GetAllPartyMembers(context.Player))
        {
            // Skip current target
            if (member.GameObjectId == currentTargetId)
                continue;

            // Skip self
            if (member.GameObjectId == context.Player.GameObjectId)
                continue;

            var hpPercent = member.MaxHp > 0 ? (float)member.CurrentHp / member.MaxHp : 1f;

            if (hpPercent < lowestHp)
            {
                lowestHp = hpPercent;
                bestTarget = member;
            }
        }

        return (bestTarget, lowestHp);
    }
}
