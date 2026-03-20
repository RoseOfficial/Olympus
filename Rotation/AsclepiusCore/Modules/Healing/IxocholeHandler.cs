using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Handles Ixochole for Sage: instant AoE oGCD heal.
/// Priority 15 in the oGCD list.
/// </summary>
public sealed class IxocholeHandler : IHealingHandler
{
    private static readonly string[] _ixocholeAlternatives =
    {
        "Kerachole (AoE regen + mit)",
        "Physis II (AoE HoT + healing buff)",
        "Prognosis (GCD AoE heal)",
    };

    public int Priority => 15;
    public string Name => "Ixochole";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
        => TryIxochole(context);

    private bool TryIxochole(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableIxochole)
            return false;

        if (player.Level < SGEActions.Ixochole.MinLevel)
            return false;

        if (context.AddersgallStacks < 1)
        {
            context.Debug.IxocholeState = "No Addersgall";
            return false;
        }

        // Check cooldown
        if (!context.ActionService.IsActionReady(SGEActions.Ixochole.ActionId))
        {
            context.Debug.IxocholeState = "On CD";
            return false;
        }

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        if (injuredCount < config.AoEHealMinTargets)
        {
            context.Debug.IxocholeState = $"{injuredCount} < {config.AoEHealMinTargets} injured";
            return false;
        }

        if (avgHp > config.AoEHealThreshold)
        {
            context.Debug.IxocholeState = $"Avg HP {avgHp:P0} > {config.AoEHealThreshold:P0}";
            return false;
        }

        var action = SGEActions.Ixochole;

        // Check AoE coordination - prevent multiple healers from casting AoE heals simultaneously
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, action.HealPotency, 0))
        {
            context.Debug.IxocholeState = "Skipped (remote AOE reserved)";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.PlanningState = "Ixochole";
            context.Debug.IxocholeState = "Executing";
            context.LogAddersgallDecision(action.Name, context.AddersgallStacks, $"{injuredCount} injured");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var stacks = context.AddersgallStacks;

                var shortReason = $"Ixochole - {injuredCount} injured at {avgHp:P0} ({stacks} stacks)";

                var factors = new[]
                {
                    $"Party avg HP: {avgHp:P0}",
                    $"Injured count: {injuredCount}",
                    $"Addersgall stacks: {stacks}",
                    "400 potency AoE heal",
                    "30s cooldown, instant",
                };

                var alternatives = _ixocholeAlternatives;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = action.ActionId,
                    ActionName = "Ixochole",
                    Category = "Healing",
                    TargetName = "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"Ixochole used for {injuredCount} injured party members at {avgHp:P0} average HP with {stacks} Addersgall stacks. 400 potency AoE heal on a 30s cooldown. Great for burst AoE healing when the party takes sudden damage.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = "Ixochole is your instant AoE heal! Use it for immediate party healing after raidwides. Kerachole provides ongoing healing via regen + mitigation, so use Ixochole for burst healing and Kerachole for sustained healing.",
                    ConceptId = SgeConcepts.IxocholeUsage,
                    Priority = ExplanationPriority.High,
                });
            }

            return true;
        }

        return false;
    }
}
