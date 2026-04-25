using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules.Healing;

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

    public void CollectCandidates(IAsclepiusContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableIxochole) return;
        if (player.Level < SGEActions.Ixochole.MinLevel) return;
        if (context.AddersgallStacks < 1) { context.Debug.IxocholeState = "No Addersgall"; return; }
        if (!context.ActionService.IsActionReady(SGEActions.Ixochole.ActionId)) { context.Debug.IxocholeState = "On CD"; return; }

        var (avgHp, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);
        if (injuredCount < config.AoEHealMinTargets) { context.Debug.IxocholeState = $"{injuredCount} < {config.AoEHealMinTargets} injured"; return; }
        if (avgHp > config.AoEHealThreshold) { context.Debug.IxocholeState = $"Avg HP {avgHp:P0} > {config.AoEHealThreshold:P0}"; return; }

        var action = SGEActions.Ixochole;
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, action.HealPotency, 0))
        {
            context.Debug.IxocholeState = "Skipped (remote AOE reserved)";
            return;
        }

        var capturedAvgHp = avgHp;
        var capturedInjuredCount = injuredCount;
        var capturedStacks = context.AddersgallStacks;

        scheduler.PushOgcd(AsclepiusAbilities.Ixochole, player.GameObjectId, priority: Priority,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.PlanningState = "Ixochole";
                context.Debug.IxocholeState = "Executing";
                context.LogAddersgallDecision(action.Name, capturedStacks, $"{capturedInjuredCount} injured");

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var shortReason = $"Ixochole - {capturedInjuredCount} injured at {capturedAvgHp:P0} ({capturedStacks} stacks)";
                    var factors = new[]
                    {
                        $"Party avg HP: {capturedAvgHp:P0}",
                        $"Injured count: {capturedInjuredCount}",
                        $"Addersgall stacks: {capturedStacks}",
                        "400 potency AoE heal",
                        "30s cooldown, instant",
                    };

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Ixochole",
                        Category = "Healing",
                        TargetName = "Party",
                        ShortReason = shortReason,
                        DetailedReason = $"Ixochole used for {capturedInjuredCount} injured party members at {capturedAvgHp:P0} average HP with {capturedStacks} Addersgall stacks. 400 potency AoE heal on a 30s cooldown. Great for burst AoE healing when the party takes sudden damage.",
                        Factors = factors,
                        Alternatives = _ixocholeAlternatives,
                        Tip = "Ixochole is your instant AoE heal! Use it for immediate party healing after raidwides. Kerachole provides ongoing healing via regen + mitigation, so use Ixochole for burst healing and Kerachole for sustained healing.",
                        ConceptId = SgeConcepts.IxocholeUsage,
                        Priority = ExplanationPriority.High,
                    });
                }
            });
    }
}
