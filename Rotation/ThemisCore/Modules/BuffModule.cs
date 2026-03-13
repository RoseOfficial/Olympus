using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.ThemisCore.Modules;

/// <summary>
/// Handles the Paladin buff rotation.
/// Manages Fight or Flight and Requiescat timing for optimal damage windows.
/// </summary>
public sealed class BuffModule : BaseTankBuffModule<IThemisContext>, IThemisModule
{
    #region Abstract Method Implementations

    protected override ActionDefinition GetTankStanceAction() => PLDActions.IronWill;

    protected override bool HasJobTankStance(IThemisContext context) => context.HasTankStance;

    protected override void SetBuffState(IThemisContext context, string state)
        => context.Debug.BuffState = state;

    protected override void SetPlannedAction(IThemisContext context, string action)
        => context.Debug.PlannedAction = action;

    #endregion

    #region Overrides

    public override bool TryExecute(IThemisContext context, bool isMoving)
    {
        // Check if damage is enabled before any buff logic
        if (!context.Configuration.Tank.EnableDamage)
        {
            context.Debug.BuffState = "Disabled";
            return false;
        }

        return base.TryExecute(context, isMoving);
    }

    protected override bool TryJobSpecificBuffs(IThemisContext context)
    {
        // Priority 1: Fight or Flight (damage buff)
        if (TryFightOrFlight(context))
            return true;

        // Priority 2: Requiescat (magic phase enabler)
        if (TryRequiescat(context))
            return true;

        return false;
    }

    #endregion

    #region Fight or Flight

    private bool TryFightOrFlight(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.FightOrFlight.MinLevel)
            return false;

        // Don't use if already active
        if (context.HasFightOrFlight)
        {
            context.Debug.BuffState = $"FoF active ({context.FightOrFlightRemaining:F1}s)";
            return false;
        }

        // Check if ready
        if (!context.ActionService.IsActionReady(PLDActions.FightOrFlight.ActionId))
            return false;

        // Use Fight or Flight when:
        // 1. We're about to do burst (combo is ready)
        // 2. Not during magic phase (Requiescat active)
        // 3. Target is available

        // Don't use during Requiescat - it buffs physical damage
        if (context.HasRequiescat)
        {
            context.Debug.BuffState = "Waiting (Requiescat active)";
            return false;
        }

        // Find a target to verify we're in combat
        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            PLDActions.FastBlade.ActionId,
            player);

        if (target == null)
        {
            context.Debug.BuffState = "No target";
            return false;
        }

        // Optimal timing: Use at combo start or when Sword Oath stacks are available
        // This ensures we get maximum GCDs under the buff
        var goodTiming = context.ComboStep <= 1 || context.HasSwordOath;

        if (goodTiming)
        {
            if (context.ActionService.ExecuteOgcd(PLDActions.FightOrFlight, player.GameObjectId))
            {
                context.Debug.PlannedAction = PLDActions.FightOrFlight.Name;
                context.Debug.BuffState = "Fight or Flight activated";

                // Training: Record burst window activation
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.FightOrFlight.ActionId, PLDActions.FightOrFlight.Name)
                    .AsTankBurst()
                    .Target(target.Name?.TextValue)
                    .Reason(
                        "Fight or Flight is your primary damage buff. Use it at the start of burst windows to maximize GCDs under its effect.",
                        "Fight or Flight provides +25% physical damage for 20 seconds. Optimally, use it at combo start to get maximum physical GCDs under the buff.")
                    .Factors(context.HasSwordOath ? "Sword Oath stacks available for Atonement chain" : "Combo at optimal position", "No conflicting buffs (Requiescat not active)", $"Target available: {target.Name?.TextValue}")
                    .Alternatives("Wait for better combo position (may delay burst)", "Save for adds/phase transition (likely lose damage)")
                    .Tip("Use Fight or Flight on cooldown at combo start. Don't hold it for 'better' times - the DPS loss from delaying usually outweighs any benefit.")
                    .Concept("pld_fight_or_flight")
                    .Record();

                context.TrainingService?.RecordConceptApplication("pld_fight_or_flight", true, "Activated at optimal timing");

                return true;
            }
        }

        return false;
    }

    #endregion

    #region Requiescat

    private bool TryRequiescat(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Requiescat.MinLevel)
            return false;

        // Don't use if already active
        if (context.HasRequiescat)
        {
            context.Debug.BuffState = $"Requiescat active ({context.RequiescatStacks} stacks)";
            return false;
        }

        // Check if ready
        if (!context.ActionService.IsActionReady(PLDActions.Requiescat.ActionId))
            return false;

        // Find a target
        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            PLDActions.FastBlade.ActionId,
            player);

        if (target == null)
        {
            context.Debug.BuffState = "No target for Requiescat";
            return false;
        }

        // Use Requiescat when:
        // 1. Fight or Flight is on cooldown or has less than 5s remaining
        // 2. We have enough MP (though Requiescat makes spells free)
        // 3. Not in the middle of a physical combo

        // Ideal timing: After Fight or Flight window ends
        var fofOnCooldown = !context.ActionService.IsActionReady(PLDActions.FightOrFlight.ActionId);
        var fofAlmostOver = context.HasFightOrFlight && context.FightOrFlightRemaining < 5f;
        var comboReady = context.ComboStep <= 1;

        // Use when FoF is on cooldown and we're not mid-combo
        if ((fofOnCooldown || fofAlmostOver) && comboReady)
        {
            if (context.ActionService.ExecuteOgcd(PLDActions.Requiescat, target.GameObjectId))
            {
                context.Debug.PlannedAction = PLDActions.Requiescat.Name;
                context.Debug.BuffState = "Requiescat activated";

                // Training: Record magic phase activation
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.Requiescat.ActionId, PLDActions.Requiescat.Name)
                    .AsTankBurst()
                    .Target(target.Name?.TextValue)
                    .Reason(
                        "Requiescat enables your magic phase. Use it after Fight or Flight ends to alternate between physical and magic burst windows.",
                        "Requiescat grants 4 stacks that power Holy Spirit/Circle and enable the Confiteor combo. This is your secondary burst window.")
                    .Factors(fofOnCooldown ? "Fight or Flight on cooldown" : $"Fight or Flight ending ({context.FightOrFlightRemaining:F1}s)", "Combo at good position for transition", $"Target available: {target.Name?.TextValue}")
                    .Alternatives("Wait for Fight or Flight window (delays magic phase)", "Use immediately off cooldown (may conflict with physical burst)")
                    .Tip("Alternate Fight or Flight and Requiescat windows. After FoF ends, use Requiescat to maintain constant burst phases.")
                    .Concept("pld_requiescat")
                    .Record();

                context.TrainingService?.RecordConceptApplication("pld_requiescat", true, "Activated after physical burst");

                return true;
            }
        }

        return false;
    }

    #endregion
}
