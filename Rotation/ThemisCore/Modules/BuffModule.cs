using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.ThemisCore.Abilities;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.ThemisCore.Modules;

/// <summary>
/// Handles the Paladin buff rotation.
/// Manages Fight or Flight and Requiescat timing for optimal damage windows.
/// </summary>
public sealed class BuffModule : BaseTankBuffModule<IThemisContext>, IThemisModule
{
    private readonly IBurstWindowService? _burstWindowService;

    public BuffModule(IBurstWindowService? burstWindowService = null)
    {
        _burstWindowService = burstWindowService;
    }

    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

    #region Abstract Method Implementations

    protected override ActionDefinition GetTankStanceAction() => PLDActions.IronWill;

    protected override bool HasJobTankStance(IThemisContext context) => context.HasTankStance;

    protected override void SetBuffState(IThemisContext context, string state)
        => context.Debug.BuffState = state;

    protected override void SetPlannedAction(IThemisContext context, string action)
        => context.Debug.PlannedAction = action;

    #endregion

    public override bool TryExecute(IThemisContext context, bool isMoving) => false;

    public override void UpdateDebugState(IThemisContext context)
    {
        // Debug state updated during CollectCandidates
    }

    #region CollectCandidates (scheduler path)

    public void CollectCandidates(IThemisContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return;
        }

        TryPushTankStance(context, scheduler);

        if (!context.Configuration.Tank.EnableDamage)
        {
            context.Debug.BuffState = "Damage disabled";
            return;
        }

        TryPushFightOrFlight(context, scheduler);
        TryPushRequiescat(context, scheduler);
    }

    private void TryPushTankStance(IThemisContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        if (player.Level < PLDActions.IronWill.MinLevel) return;
        if (!context.Configuration.Tank.AutoTankStance)
        {
            context.Debug.BuffState = "AutoTankStance disabled";
            return;
        }
        if (context.HasTankStance) return;
        if (!context.ActionService.IsActionReady(PLDActions.IronWill.ActionId)) return;

        scheduler.PushOgcd(ThemisAbilities.IronWill, player.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PLDActions.IronWill.Name;
                context.Debug.BuffState = "Enabling Iron Will";
            });
    }

    private void TryPushFightOrFlight(IThemisContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Tank.EnableFightOrFlight) return;

        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.FightOrFlight.MinLevel) return;

        if (context.HasFightOrFlight)
        {
            context.Debug.BuffState = $"FoF active ({context.FightOrFlightRemaining:F1}s)";
            return;
        }

        if (!context.ActionService.IsActionReady(PLDActions.FightOrFlight.ActionId)) return;

        if (context.HasRequiescat)
        {
            context.Debug.BuffState = "Waiting (Requiescat active)";
            return;
        }

        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            PLDActions.FastBlade.ActionId,
            player);

        if (target == null)
        {
            context.Debug.BuffState = "No target";
            return;
        }

        if (ShouldHoldForBurst(8f))
        {
            context.Debug.BuffState = "Holding Fight or Flight for burst";
            return;
        }

        var goodTiming = context.ComboStep <= 1 || context.HasSwordOath;
        if (!goodTiming) return;

        var targetName = target.Name?.TextValue;
        var hasSwordOath = context.HasSwordOath;

        scheduler.PushOgcd(ThemisAbilities.FightOrFlight, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PLDActions.FightOrFlight.Name;
                context.Debug.BuffState = "Fight or Flight activated";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.FightOrFlight.ActionId, PLDActions.FightOrFlight.Name)
                    .AsTankBurst()
                    .Target(targetName)
                    .Reason(
                        "Fight or Flight is your primary damage buff. Use it at the start of burst windows.",
                        "Fight or Flight provides +25% physical damage for 20 seconds.")
                    .Factors(hasSwordOath ? "Sword Oath stacks available" : "Combo at optimal position", "Requiescat not active", $"Target: {targetName}")
                    .Alternatives("Wait for better combo position", "Save for adds/phase transition (likely DPS loss)")
                    .Tip("Use Fight or Flight on cooldown at combo start.")
                    .Concept("pld_fight_or_flight")
                    .Record();
                context.TrainingService?.RecordConceptApplication("pld_fight_or_flight", true, "Activated at optimal timing");
            });
    }

    private void TryPushRequiescat(IThemisContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Tank.EnableRequiescat) return;

        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Requiescat.MinLevel) return;

        if (context.HasRequiescat)
        {
            context.Debug.BuffState = $"Requiescat active ({context.RequiescatStacks} stacks)";
            return;
        }

        if (!context.ActionService.IsActionReady(PLDActions.Requiescat.ActionId)) return;

        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            PLDActions.FastBlade.ActionId,
            player);

        if (target == null)
        {
            context.Debug.BuffState = "No target for Requiescat";
            return;
        }

        var fofOnCooldown = !context.ActionService.IsActionReady(PLDActions.FightOrFlight.ActionId);
        var fofAlmostOver = context.HasFightOrFlight && context.FightOrFlightRemaining < 5f;
        var comboReady = context.ComboStep <= 1;

        if (ShouldHoldForBurst(8f))
        {
            context.Debug.BuffState = "Holding Requiescat for burst";
            return;
        }

        if (!((fofOnCooldown || fofAlmostOver) && comboReady)) return;

        var targetName = target.Name?.TextValue;
        var fofRemaining = context.FightOrFlightRemaining;

        scheduler.PushOgcd(ThemisAbilities.Requiescat, target.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PLDActions.Requiescat.Name;
                context.Debug.BuffState = "Requiescat activated";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PLDActions.Requiescat.ActionId, PLDActions.Requiescat.Name)
                    .AsTankBurst()
                    .Target(targetName)
                    .Reason(
                        "Requiescat enables magic phase. Use after Fight or Flight.",
                        "Requiescat grants 4 stacks that power Holy Spirit/Circle and enable the Confiteor combo.")
                    .Factors(fofOnCooldown ? "Fight or Flight on cooldown" : $"Fight or Flight ending ({fofRemaining:F1}s)", "Combo at good position", $"Target: {targetName}")
                    .Alternatives("Wait for Fight or Flight window", "Use immediately off cooldown (may conflict with physical burst)")
                    .Tip("Alternate Fight or Flight and Requiescat windows.")
                    .Concept("pld_requiescat")
                    .Record();
                context.TrainingService?.RecordConceptApplication("pld_requiescat", true, "Activated after physical burst");
            });
    }

    #endregion

    #region Legacy Abstract Overrides (unused in scheduler path but required by base class)

    protected override bool TryJobSpecificBuffs(IThemisContext context) => false;
    protected override bool TryJobSpecificResourceGeneration(IThemisContext context) => false;

    #endregion
}
