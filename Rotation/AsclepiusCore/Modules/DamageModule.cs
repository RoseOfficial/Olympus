using System;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.Common.Modules;
using Olympus.Services.Training;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// SGE-specific damage module.
/// Handles Dosis, Eukrasian Dosis (DoT), Phlegma, Toxikon, Dyskrasia, and Psyche.
/// Extends base damage logic with SGE-unique mechanics: Eukrasia-based DoT, charge-based Phlegma,
/// Addersting-based Toxikon for movement.
/// </summary>
public sealed class DamageModule : BaseDamageModule<IAsclepiusContext>, IAsclepiusModule
{
    #region Base Class Overrides - Configuration Properties

    protected override bool IsDamageEnabled(IAsclepiusContext context) =>
        context.Configuration.EnableDamage;

    protected override bool IsDoTEnabled(IAsclepiusContext context) =>
        context.Configuration.EnableDoT;

    protected override bool IsAoEDamageEnabled(IAsclepiusContext context) =>
        context.Configuration.Sage.EnableAoEDamage;

    protected override int AoEMinTargets(IAsclepiusContext context) =>
        context.Configuration.Sage.AoEDamageMinTargets;

    protected override float DoTRefreshThreshold(IAsclepiusContext context) =>
        FFXIVConstants.DotRefreshThreshold;

    #endregion

    #region Base Class Overrides - Action Methods

    protected override uint GetDoTStatusId(IAsclepiusContext context) =>
        SGEActions.GetDotStatusId(context.Player.Level);

    protected override ActionDefinition? GetDoTAction(IAsclepiusContext context) =>
        SGEActions.GetDotForLevel(context.Player.Level);

    protected override ActionDefinition? GetAoEDamageAction(IAsclepiusContext context) =>
        SGEActions.GetAoEDamageGcdForLevel(context.Player.Level);

    protected override ActionDefinition GetSingleTargetAction(IAsclepiusContext context, bool isMoving) =>
        SGEActions.GetDamageGcdForLevel(context.Player.Level);

    #endregion

    #region Base Class Overrides - Debug State

    protected override void SetDpsState(IAsclepiusContext context, string state) =>
        context.Debug.DpsState = state;

    protected override void SetAoEDpsState(IAsclepiusContext context, string state) =>
        context.Debug.AoEDpsState = state;

    protected override void SetAoEDpsEnemyCount(IAsclepiusContext context, int count) =>
        context.Debug.AoEDpsEnemyCount = count;

    protected override void SetPlannedAction(IAsclepiusContext context, string action) =>
        context.Debug.PlannedAction = action;

    #endregion

    #region Base Class Overrides - Behavioral

    /// <summary>
    /// SGE DPS doesn't block other modules (healers continue to check other priorities).
    /// </summary>
    protected override bool BlocksOnExecution => false;

    /// <summary>
    /// SGE oGCD damage: Psyche and Eukrasia activation for DoT.
    /// </summary>
    protected override bool TryOgcdDamage(IAsclepiusContext context)
    {
        if (TryPsyche(context))
            return true;

        // Activate Eukrasia for DoT during oGCD window.
        // Uses FindEnemy (current-target aware) instead of FindEnemyNeedingDot
        // to avoid cache/filter issues with GetValidEnemies on striking dummies.
        if (!context.HasEukrasia && IsDoTEnabled(context))
        {
            var player = context.Player;
            if (player.Level >= SGEActions.EukrasianDosis.MinLevel)
            {
                var dotAction = GetDoTAction(context);
                if (dotAction != null)
                {
                    var enemy = context.TargetingService.FindEnemy(
                        context.Configuration.Targeting.EnemyStrategy,
                        dotAction.Range,
                        player);

                    if (enemy != null)
                    {
                        // Check if this enemy actually needs the DoT
                        var dotStatusId = GetDoTStatusId(context);
                        bool needsDot = true;
                        if (enemy.StatusList != null)
                        {
                            foreach (var status in enemy.StatusList)
                            {
                                if (status.StatusId == dotStatusId && status.RemainingTime > DoTRefreshThreshold(context))
                                {
                                    needsDot = false;
                                    break;
                                }
                            }
                        }

                        if (needsDot && context.ActionService.ExecuteOgcd(SGEActions.Eukrasia, player.GameObjectId))
                        {
                            SetPlannedAction(context, "Eukrasia");
                            SetDpsState(context, "Eukrasia for DoT");
                            context.Debug.EukrasiaState = "Activating";
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// SGE special GCD damage: Phlegma (charge-based) and Toxikon when moving.
    /// </summary>
    protected override bool TrySpecialDamage(IAsclepiusContext context, bool isMoving)
    {
        // Priority 1: Phlegma (high potency, instant, charges)
        if (TryPhlegma(context))
            return true;

        // Priority 2: Toxikon while moving (consumes Addersting)
        if (isMoving && TryToxikon(context))
            return true;

        return false;
    }

    /// <summary>
    /// SGE DoT requires activating Eukrasia first (oGCD), then applying Eukrasian Dosis (GCD).
    /// Override to handle this unique two-step process.
    /// </summary>
    protected override bool TryDoT(IAsclepiusContext context)
    {
        if (!IsDoTEnabled(context))
            return false;

        var player = context.Player;
        if (player.Level < SGEActions.EukrasianDosis.MinLevel)
            return false;

        var dotAction = GetDoTAction(context);
        if (dotAction == null)
            return false;

        var dotStatusId = GetDoTStatusId(context);

        // If we have Eukrasia, apply the DoT
        if (context.HasEukrasia)
        {
            var enemy = context.TargetingService.FindEnemy(
                context.Configuration.Targeting.EnemyStrategy,
                dotAction.Range,
                player);

            if (enemy == null)
                return false;

            if (context.ActionService.ExecuteGcd(dotAction, enemy.GameObjectId))
            {
                SetPlannedAction(context, dotAction.Name);
                SetDpsState(context, "DoT Applied");

                // Training mode: capture explanation
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = enemy.Name?.TextValue ?? "Unknown";
                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = dotAction.ActionId,
                        ActionName = dotAction.Name,
                        Category = "Damage",
                        TargetName = targetName,
                        ShortReason = $"Eukrasian Dosis DoT applied on {targetName}",
                        DetailedReason = $"Applied Eukrasian Dosis on {targetName} after activating Eukrasia. This is SGE's primary DoT - the Eukrasia oGCD converts the next cast into its enhanced Eukrasian version, which applies a long-duration DoT tick that deals damage passively while you continue healing or dealing damage.",
                        Factors = new[]
                        {
                            "Eukrasia was active - must apply immediately",
                            "DoT was not present or near expiry",
                            "Deals damage over 30s passively",
                            "Enables full Dosis rotation",
                        },
                        Alternatives = new[]
                        {
                            "Already committed - Eukrasia was active",
                            "Delay if target is about to die",
                        },
                        Tip = "Always apply Eukrasian Dosis immediately after activating Eukrasia - the buff expires if unused! DoT uptime is a significant portion of SGE's overall DPS.",
                        ConceptId = SgeConcepts.EukrasianDosisUsage,
                        Priority = ExplanationPriority.Normal,
                    });
                }

                return true;
            }

            return false;
        }

        // Check if we need to apply/refresh DoT
        var target = context.TargetingService.FindEnemyNeedingDot(
            dotStatusId,
            DoTRefreshThreshold(context),
            dotAction.Range,
            player);

        if (target == null)
        {
            context.Debug.DoTState = "Active";
            return false;
        }

        // Activate Eukrasia for DoT — attempt directly without CanExecuteOgcd gate.
        // Eukrasia is instant with no animation lock; the game will reject if truly unavailable.
        var eukrasiaAction = SGEActions.Eukrasia;
        if (context.ActionService.ExecuteOgcd(eukrasiaAction, player.GameObjectId))
        {
            SetPlannedAction(context, eukrasiaAction.Name);
            SetDpsState(context, "Eukrasia for DoT");
            context.Debug.EukrasiaState = "Activating";
            return true;
        }

        return false;
    }

    /// <summary>
    /// SGE cannot cast Dosis while moving (has cast time).
    /// Use Toxikon instead for movement damage.
    /// </summary>
    protected override bool CanSingleTarget(IAsclepiusContext context, bool isMoving) => !isMoving;

    /// <summary>
    /// SGE movement damage: Toxikon (instant cast, uses Addersting).
    /// </summary>
    protected override bool TryMovementDamage(IAsclepiusContext context)
    {
        return TryToxikon(context);
    }

    #endregion

    #region SGE-Specific Methods

    private bool TryPsyche(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePsyche)
            return false;

        if (player.Level < SGEActions.Psyche.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Psyche.ActionId))
        {
            context.Debug.PsycheState = "On CD";
            return false;
        }

        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SGEActions.Psyche.Range,
            player);

        if (enemy == null)
        {
            context.Debug.PsycheState = "No target";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(SGEActions.Psyche, enemy.GameObjectId))
        {
            SetPlannedAction(context, SGEActions.Psyche.Name);
            SetDpsState(context, "Psyche");
            context.Debug.PsycheState = "Executing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = enemy.Name?.TextValue ?? "Unknown";
                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = SGEActions.Psyche.ActionId,
                    ActionName = "Psyche",
                    Category = "Damage",
                    TargetName = targetName,
                    ShortReason = $"Psyche oGCD damage on {targetName}",
                    DetailedReason = $"Psyche used on {targetName} - SGE's oGCD damage cooldown. Psyche deals high potency damage in an oGCD slot, meaning it deals damage without interrupting your GCD rotation. Always use on cooldown for optimal DPS.",
                    Factors = new[]
                    {
                        "Psyche is off cooldown",
                        "Enemy in range",
                        "oGCD slot available",
                        "High potency damage without using GCD",
                    },
                    Alternatives = new[]
                    {
                        "Nothing - use Psyche on cooldown",
                        "Delay only if you need the oGCD for Eukrasia/healing",
                    },
                    Tip = "Psyche is one of SGE's best oGCD damage tools! Weave it between GCDs on cooldown. It doesn't cost Addersgall, so there's no reason to hold it unless you need that oGCD slot for healing.",
                    ConceptId = SgeConcepts.PsycheUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryPhlegma(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePhlegma)
            return false;

        var phlegmaAction = SGEActions.GetPhlegmaForLevel(player.Level);
        if (phlegmaAction == null)
        {
            context.Debug.PhlegmaState = "Level too low";
            return false;
        }

        // Check charges
        var charges = context.ActionService.GetCurrentCharges(phlegmaAction.ActionId);
        if (charges < 1)
        {
            context.Debug.PhlegmaState = "No charges";
            return false;
        }

        // Use if we'd overcap charges
        var maxCharges = 2;
        var rechargingTime = context.ActionService.GetCooldownRemaining(phlegmaAction.ActionId);
        var shouldUse = charges >= maxCharges || (charges == maxCharges - 1 && rechargingTime < 5f);

        if (!shouldUse && charges < maxCharges)
        {
            context.Debug.PhlegmaState = $"Saving ({charges}/{maxCharges})";
            return false;
        }

        // Find enemy in range (Phlegma is close range)
        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            phlegmaAction.Range,
            player);

        if (enemy == null)
        {
            context.Debug.PhlegmaState = "Out of range";
            return false;
        }

        if (context.ActionService.ExecuteGcd(phlegmaAction, enemy.GameObjectId))
        {
            SetPlannedAction(context, phlegmaAction.Name);
            SetDpsState(context, phlegmaAction.Name);
            context.Debug.PhlegmaState = "Executing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = enemy.Name?.TextValue ?? "Unknown";
                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = phlegmaAction.ActionId,
                    ActionName = phlegmaAction.Name,
                    Category = "Damage",
                    TargetName = targetName,
                    ShortReason = $"{phlegmaAction.Name} on {targetName} ({charges}/{maxCharges} charges)",
                    DetailedReason = $"Used {phlegmaAction.Name} on {targetName}. Phlegma is SGE's highest potency instant GCD damage skill and must be used at melee range. Current charges: {charges}/{maxCharges}. Spending charges now to avoid overcapping - always use at 2 stacks or when one is about to recharge.",
                    Factors = new[]
                    {
                        $"Phlegma charges: {charges}/{maxCharges}",
                        "Highest single-target GCD potency for SGE",
                        "Instant cast - no cast time",
                        "Must be in melee range (6y)",
                        "Overcap prevention",
                    },
                    Alternatives = new[]
                    {
                        "Dosis (longer range, lower potency)",
                        "Save for boss phase (if adds incoming soon)",
                    },
                    Tip = "Phlegma is your best GCD damage skill! It requires melee range (6 yalms), so plan positioning. Use charges before they overcap - holding 2 charges wastes DPS when the third would be generated.",
                    ConceptId = SgeConcepts.PhlegmaUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    private bool TryToxikon(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableToxikon)
            return false;

        var toxikonAction = SGEActions.GetToxikonForLevel(player.Level);
        if (toxikonAction == null)
        {
            context.Debug.ToxikonState = "Level too low";
            return false;
        }

        // Requires Addersting
        if (context.AdderstingStacks < 1)
        {
            context.Debug.ToxikonState = "No Addersting";
            return false;
        }

        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            toxikonAction.Range,
            player);

        if (enemy == null)
        {
            context.Debug.ToxikonState = "No target";
            return false;
        }

        if (context.ActionService.ExecuteGcd(toxikonAction, enemy.GameObjectId))
        {
            SetPlannedAction(context, toxikonAction.Name);
            SetDpsState(context, toxikonAction.Name);
            context.Debug.ToxikonState = "Executing";

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = enemy.Name?.TextValue ?? "Unknown";
                var adderstingStacks = context.AdderstingStacks;
                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = toxikonAction.ActionId,
                    ActionName = toxikonAction.Name,
                    Category = "Damage",
                    TargetName = targetName,
                    ShortReason = $"{toxikonAction.Name} while moving ({adderstingStacks} Addersting)",
                    DetailedReason = $"Used {toxikonAction.Name} on {targetName} while moving. Toxikon is an instant-cast damage spell that consumes Addersting stacks. Addersting is generated when an Eukrasian Diagnosis shield is fully absorbed by damage. This allows continued DPS output during movement mechanics.",
                    Factors = new[]
                    {
                        "Currently moving - cannot cast Dosis (cast time)",
                        $"Addersting stacks available: {adderstingStacks}",
                        "Instant cast - usable during movement",
                        "Generated from broken E.Diagnosis shields",
                    },
                    Alternatives = new[]
                    {
                        "Phlegma (if charges available and in melee range)",
                        "Hold DPS until movement ends",
                    },
                    Tip = "Toxikon is SGE's answer to movement mechanics! Apply E.Diagnosis shields on tanks to generate Addersting, then spend those stacks on Toxikon during movement. It's instant cast, so you never have to stop DPS during movement!",
                    ConceptId = SgeConcepts.ToxikonUsage,
                    Priority = ExplanationPriority.Normal,
                });
            }

            return true;
        }

        return false;
    }

    #endregion

    public override void UpdateDebugState(IAsclepiusContext context)
    {
        var player = context.Player;

        // Update DoT state
        var dotAction = SGEActions.GetDotForLevel(player.Level);
        var dotStatusId = SGEActions.GetDotStatusId(player.Level);

        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            dotAction.Range,
            player);

        if (enemy != null)
        {
            var dotRemaining = GetStatusRemainingTime(enemy, dotStatusId, player.GameObjectId);
            context.Debug.DoTRemaining = dotRemaining;
            context.Debug.DoTState = dotRemaining > 0 ? $"{dotRemaining:F1}s" : "Not applied";
        }
        else
        {
            context.Debug.DoTState = "No target";
        }

        // Phlegma charges
        var phlegmaAction = SGEActions.GetPhlegmaForLevel(player.Level);
        if (phlegmaAction != null)
        {
            var charges = (int)context.ActionService.GetCurrentCharges(phlegmaAction.ActionId);
            context.Debug.PhlegmaCharges = charges;
            context.Debug.PhlegmaState = charges > 0 ? $"{charges} charges" : "No charges";
        }

        // Addersting for Toxikon
        context.Debug.AdderstingStacks = context.AdderstingStacks;
        context.Debug.ToxikonState = context.AdderstingStacks > 0 ? $"{context.AdderstingStacks} stacks" : "No Addersting";
    }

    #region Helpers

    private float GetStatusRemainingTime(IBattleChara target, uint statusId, ulong sourceId)
    {
        if (target.StatusList == null)
            return 0f;

        foreach (var status in target.StatusList)
        {
            if (status.StatusId == statusId && status.SourceId == (uint)sourceId)
            {
                return status.RemainingTime;
            }
        }

        return 0f;
    }

    #endregion
}
