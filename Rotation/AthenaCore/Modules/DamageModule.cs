using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Services.Training;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Scholar-specific damage module.
/// Extends base damage logic with Chain Stratagem, Energy Drain, Baneful Impaction, and Aetherflow management.
/// </summary>
public sealed class DamageModule : BaseDamageModule<IAthenaContext>, IAthenaModule
{
    #region Base Class Overrides - Configuration Properties

    protected override bool IsDamageEnabled(IAthenaContext context) =>
        context.Configuration.Scholar.EnableSingleTargetDamage;

    protected override bool IsDoTEnabled(IAthenaContext context) =>
        context.Configuration.Scholar.EnableDot;

    protected override bool IsAoEDamageEnabled(IAthenaContext context) =>
        context.Configuration.Scholar.EnableAoEDamage;

    protected override int AoEMinTargets(IAthenaContext context) =>
        context.Configuration.Scholar.AoEDamageMinTargets;

    protected override float DoTRefreshThreshold(IAthenaContext context) =>
        context.Configuration.Scholar.DotRefreshThreshold;

    #endregion

    #region Base Class Overrides - Action Methods

    protected override uint GetDoTStatusId(IAthenaContext context) =>
        SCHActions.GetDotStatusId(context.Player.Level);

    protected override ActionDefinition? GetDoTAction(IAthenaContext context) =>
        SCHActions.GetDotForLevel(context.Player.Level);

    protected override ActionDefinition? GetAoEDamageAction(IAthenaContext context) =>
        SCHActions.GetAoEDamageForLevel(context.Player.Level);

    protected override ActionDefinition GetSingleTargetAction(IAthenaContext context, bool isMoving) =>
        SCHActions.GetDamageGcdForLevel(context.Player.Level, isMoving);

    #endregion

    #region Base Class Overrides - Debug State

    protected override void SetDpsState(IAthenaContext context, string state) =>
        context.Debug.DpsState = state;

    protected override void SetAoEDpsState(IAthenaContext context, string state) =>
        context.Debug.AoEDpsState = state;

    protected override void SetAoEDpsEnemyCount(IAthenaContext context, int count) =>
        context.Debug.AoEDpsEnemyCount = count;

    protected override void SetPlannedAction(IAthenaContext context, string action) =>
        context.Debug.PlannedAction = action;

    #endregion

    #region Base Class Overrides - Behavioral

    /// <summary>
    /// SCH oGCD damage: Chain Stratagem, Baneful Impaction, Energy Drain, Aetherflow.
    /// </summary>
    protected override bool TryOgcdDamage(IAthenaContext context)
    {
        // Priority 1: Chain Stratagem (raid buff)
        if (TryChainStratagem(context))
            return true;

        // Priority 2: Baneful Impaction (when Impact Imminent is active)
        if (TryBanefulImpaction(context))
            return true;

        // Priority 3: Energy Drain (dump Aetherflow for damage)
        if (TryEnergyDrain(context))
            return true;

        // Priority 4: Aetherflow (get stacks)
        if (TryAetherflow(context))
            return true;

        return false;
    }

    /// <summary>
    /// SCH movement damage: Ruin II is instant cast.
    /// </summary>
    protected override bool TryMovementDamage(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableRuinII)
            return false;

        if (player.Level < SCHActions.RuinII.MinLevel)
            return false;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SCHActions.RuinII.Range,
            player);

        if (target == null)
            return false;

        if (context.ActionService.ExecuteGcd(SCHActions.RuinII, target.GameObjectId))
        {
            SetPlannedAction(context, SCHActions.RuinII.Name);
            SetDpsState(context, "Ruin II (moving)");

            TrainingHelper.RecordDamageDecision(
                context.TrainingService,
                SCHActions.RuinII.ActionId, "Ruin II",
                target.Name?.TextValue,
                "Ruin II - instant cast while moving",
                "Ruin II is an instant-cast damage GCD used while moving. Scholar has limited instant-cast GCDs, so Ruin II fills downtime caused by movement. It deals slightly less potency than Broil but keeps you casting during movement-heavy mechanics.",
                new[] { "Player is moving", "Broil requires cast time", "Instant cast maintains GCD uptime" },
                new[] { "Broil (wait until movement stops)", "Swiftcast Broil (if available)", "Skip GCD and weave oGCDs" },
                "Ruin II is your primary instant-cast damage tool while moving. Use it during movement mechanics to maintain GCD uptime rather than losing DPS standing still.",
                SchConcepts.DpsOptimization,
                ExplanationPriority.Low);

            context.TrainingService?.RecordConceptApplication(SchConcepts.DpsOptimization, wasSuccessful: true);

            return true;
        }

        return false;
    }

    #endregion

    #region SCH-Specific oGCD Methods

    private bool TryChainStratagem(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableChainStratagem)
            return false;

        if (player.Level < SCHActions.ChainStratagem.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.ChainStratagem.ActionId))
            return false;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SCHActions.ChainStratagem.Range,
            player);

        if (target == null)
            return false;

        if (context.ActionService.ExecuteOgcd(SCHActions.ChainStratagem, target.GameObjectId))
        {
            SetPlannedAction(context, SCHActions.ChainStratagem.Name);
            SetDpsState(context, "Chain Stratagem");

            TrainingHelper.RecordBuffDecision(
                context.TrainingService,
                SCHActions.ChainStratagem.ActionId, "Chain Stratagem",
                target.Name?.TextValue,
                "Chain Stratagem - party damage buff",
                "Chain Stratagem increases the critical hit rate of all party members against the target by 10% for 15 seconds. This is SCH's primary raid buff and one of the most powerful damage contributions a Scholar makes. Use on cooldown during party burst windows.",
                new[] { "Action ready (off cooldown)", "Enemy target in range", "Party burst window alignment" },
                new[] { "Delay for better alignment", "Use during two-minute burst", "Save for boss vulnerability window" },
                "Chain Stratagem should be used on cooldown and aligned with party two-minute burst windows. Communicate with your party to ensure maximum buff overlap.",
                SchConcepts.ChainStratagemTiming,
                ExplanationPriority.High);

            context.TrainingService?.RecordConceptApplication(SchConcepts.ChainStratagemTiming, wasSuccessful: true);

            return true;
        }

        return false;
    }

    private bool TryBanefulImpaction(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableBanefulImpaction)
            return false;

        if (player.Level < SCHActions.BanefulImpaction.MinLevel)
            return false;

        // Check for Impact Imminent buff
        if (!context.StatusHelper.HasImpactImminent(player))
            return false;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SCHActions.BanefulImpaction.Range,
            player);

        if (target == null)
            return false;

        if (context.ActionService.ExecuteOgcd(SCHActions.BanefulImpaction, target.GameObjectId))
        {
            SetPlannedAction(context, SCHActions.BanefulImpaction.Name);
            SetDpsState(context, "Baneful Impaction");

            TrainingHelper.RecordDamageDecision(
                context.TrainingService,
                SCHActions.BanefulImpaction.ActionId, "Baneful Impaction",
                target.Name?.TextValue,
                "Baneful Impaction - consuming Impact Imminent proc",
                "Baneful Impaction is triggered by the Impact Imminent buff from Chain Stratagem. It delivers high-potency AoE damage as an oGCD. Always use Baneful Impaction before the Impact Imminent buff expires to avoid wasting Chain Stratagem's bonus effect.",
                new[] { "Impact Imminent buff active", "Chain Stratagem recently used", "High-potency oGCD damage available" },
                new[] { "Let it expire (wasted potency)", "Delay Baneful Impaction (risky)" },
                "Baneful Impaction is a free oGCD damage bonus from Chain Stratagem. Use it promptly after Chain Stratagem - the Impact Imminent buff has a short duration.",
                SchConcepts.ChainStratagemTiming,
                ExplanationPriority.High);

            context.TrainingService?.RecordConceptApplication(SchConcepts.ChainStratagemTiming, wasSuccessful: true);

            return true;
        }

        return false;
    }

    private bool TryEnergyDrain(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableEnergyDrain)
            return false;

        if (player.Level < SCHActions.EnergyDrain.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.EnergyDrain.ActionId))
            return false;

        var stacks = context.AetherflowService.CurrentStacks;
        if (stacks == 0)
            return false;

        // Decision based on Aetherflow strategy
        bool shouldDrain = config.AetherflowStrategy switch
        {
            AetherflowUsageStrategy.AggressiveDps => stacks > 0,
            AetherflowUsageStrategy.Balanced => ShouldDrainBalanced(context),
            AetherflowUsageStrategy.HealingPriority => ShouldDrainConservative(context),
            _ => false
        };

        if (!shouldDrain)
            return false;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SCHActions.EnergyDrain.Range,
            player);

        if (target == null)
            return false;

        if (context.ActionService.ExecuteOgcd(SCHActions.EnergyDrain, target.GameObjectId))
        {
            context.AetherflowService.ConsumeStack();
            SetPlannedAction(context, SCHActions.EnergyDrain.Name);
            SetDpsState(context, "Energy Drain");

            var stacksBeforeDrain = context.AetherflowService.CurrentStacks + 1; // +1 since we already consumed
            TrainingHelper.RecordResourceDecision(
                context.TrainingService,
                SCHActions.EnergyDrain.ActionId, "Energy Drain",
                target.Name?.TextValue,
                $"Energy Drain - dumping Aetherflow ({stacksBeforeDrain} stacks)",
                $"Energy Drain converts 1 Aetherflow stack into damage ({stacksBeforeDrain} stacks available). Used when the Aetherflow strategy allows it and party is not in urgent need of healing. Balancing Aetherflow between healing and DPS is a core SCH skill.",
                new[] { $"Aetherflow stacks: {stacksBeforeDrain}/3", $"Strategy: {config.AetherflowStrategy}", "Party health acceptable for stack usage" },
                new[] { "Save stacks for Lustrate/Indom", "Wait for healing need to pass", "Use Excogitation first (more efficient)" },
                "Energy Drain should be used when Aetherflow would otherwise overcap or when healing demand is low. Don't drain stacks if a heal window is imminent.",
                SchConcepts.EnergyDrainUsage,
                ExplanationPriority.Normal);

            context.TrainingService?.RecordConceptApplication(SchConcepts.EnergyDrainUsage, wasSuccessful: true);

            return true;
        }

        return false;
    }

    private bool TryAetherflow(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var player = context.Player;

        if (!config.EnableAetherflow)
            return false;

        if (player.Level < SCHActions.Aetherflow.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SCHActions.Aetherflow.ActionId))
            return false;

        // Only use when we have 0 stacks
        if (context.AetherflowService.CurrentStacks > 0)
            return false;

        if (context.ActionService.ExecuteOgcd(SCHActions.Aetherflow, player.GameObjectId))
        {
            SetPlannedAction(context, SCHActions.Aetherflow.Name);
            SetDpsState(context, "Aetherflow");

            TrainingHelper.RecordResourceDecision(
                context.TrainingService,
                SCHActions.Aetherflow.ActionId, "Aetherflow",
                null,
                "Aetherflow - refreshing stacks (0 remaining)",
                "Aetherflow refreshes your resource pool to 3 stacks when empty. These stacks power SCH's strongest heals (Lustrate, Excogitation, Indomitability, Sacred Soil) and Energy Drain for damage. Using Aetherflow on cooldown is essential for maintaining both healing output and DPS.",
                new[] { "Aetherflow stacks: 0 (empty)", "Aetherflow off cooldown", "Grants 3 stacks for healing and damage" },
                new[] { "Wait (stacks will stay at 0)", "Dissipation (if fairy available, for emergency stacks)" },
                "Aetherflow is the backbone of SCH's resource system. Use it as soon as stacks hit 0 to maximize efficiency. Pair its timing with burst windows for Energy Drain.",
                SchConcepts.AetherflowRefresh,
                ExplanationPriority.Normal);

            context.TrainingService?.RecordConceptApplication(SchConcepts.AetherflowRefresh, wasSuccessful: true);

            return true;
        }

        return false;
    }

    #endregion

    #region Aetherflow Strategy Helpers

    private bool ShouldDrainBalanced(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var stacks = context.AetherflowService.CurrentStacks;

        // Check if Aetherflow is coming off cooldown soon
        var aetherflowCd = context.AetherflowService.GetCooldownRemaining();
        if (aetherflowCd <= config.AetherflowDumpWindow && stacks > 0)
            return true;

        // Keep reserve for healing
        if (stacks <= config.AetherflowReserve)
            return false;

        // Party is healthy, safe to drain
        var (avgHp, lowestHp, _) = context.PartyHelper.CalculatePartyHealthMetrics(context.Player);
        return avgHp > 0.8f && lowestHp > 0.5f;
    }

    private bool ShouldDrainConservative(IAthenaContext context)
    {
        var config = context.Configuration.Scholar;
        var stacks = context.AetherflowService.CurrentStacks;

        // Only drain to prevent overcap
        var aetherflowCd = context.AetherflowService.GetCooldownRemaining();
        if (aetherflowCd <= config.AetherflowDumpWindow && stacks == 3)
            return true;

        return false;
    }

    #endregion

    public override void UpdateDebugState(IAthenaContext context)
    {
        context.Debug.AetherflowState = $"{context.AetherflowService.CurrentStacks}/3";
    }
}
