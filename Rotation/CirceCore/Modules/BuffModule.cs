using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.CirceCore.Context;
using Olympus.Services;
using Olympus.Services.Training;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.CirceCore.Modules;

/// <summary>
/// Handles Red Mage oGCD buffs and abilities.
/// Manages Fleche, Contre Sixte, Embolden, Manafication, Corps-a-corps, Engagement, etc.
/// </summary>
public sealed class BuffModule : ICirceModule
{
    public int Priority => 20; // Higher priority than damage (lower number = higher priority)
    public string Name => "Buff";

    private readonly IBurstWindowService? _burstWindowService;

    public BuffModule(IBurstWindowService? burstWindowService = null)
    {
        _burstWindowService = burstWindowService;
    }

    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        _burstWindowService?.IsBurstImminent(thresholdSeconds) == true &&
        _burstWindowService?.IsInBurstWindow != true;

    public bool TryExecute(ICirceContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return false;
        }

        if (!context.CanExecuteOgcd)
        {
            context.Debug.BuffState = "oGCD not ready";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Find target for damage oGCDs
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.CasterTargetingRange,
            player);

        // Priority 1: Fleche (use on cooldown - high damage single target)
        if (TryFleche(context, target))
            return true;

        // Priority 2: Contre Sixte (use on cooldown - AoE damage)
        if (TryContreSixte(context, target))
            return true;

        // Priority 3: Vice of Thorns (when Thorned Flourish is active)
        if (TryViceOfThorns(context, target))
            return true;

        // Priority 4: Prefulgence (when Prefulgence Ready is active)
        if (TryPrefulgence(context, target))
            return true;

        // Priority 5: Embolden (align with melee combo or burst windows)
        if (TryEmbolden(context))
            return true;

        // Priority 6: Manafication (at ~50|50 mana, before melee combo)
        if (TryManafication(context))
            return true;

        // Priority 7: Corps-a-corps (during melee combo or when capped on charges)
        if (TryCorpsACorps(context, target))
            return true;

        // Priority 8: Engagement (during melee combo or when capped on charges)
        if (TryEngagement(context, target))
            return true;

        // Priority 9: Acceleration (when no procs and not in melee combo)
        if (TryAcceleration(context))
            return true;

        // Priority 10: Lucid Dreaming (when MP < 70%)
        if (TryLucidDreaming(context))
            return true;

        context.Debug.BuffState = "No oGCD needed";
        return false;
    }

    public void UpdateDebugState(ICirceContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Timeline Awareness

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a phase transition is expected within the window.
    /// </summary>
    private bool ShouldHoldBurstForPhase(ICirceContext context, float windowSeconds = 8f)
    {
        var nextPhase = context.TimelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;

        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }

    #endregion

    #region oGCD Actions

    private bool TryFleche(ICirceContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (!context.Configuration.RedMage.EnableFleche) return false;

        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.Fleche.MinLevel)
            return false;

        if (!context.FlecheReady)
            return false;

        // Use on cooldown - it's a high damage oGCD
        if (context.ActionService.ExecuteOgcd(RDMActions.Fleche, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Fleche.Name;
            context.Debug.BuffState = "Fleche";

            // Training Mode integration
            TrainingHelper.Decision(context.TrainingService)
                .Action(RDMActions.Fleche.ActionId, RDMActions.Fleche.Name)
                .AsCasterDamage()
                .Target(target.Name?.TextValue)
                .Reason("Fleche - high damage oGCD",
                    "Fleche is your primary single-target oGCD with a 25s cooldown. Use it on cooldown " +
                    "to maximize damage, weaving between GCDs. It doesn't interact with procs or mana.")
                .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}", "25s cooldown")
                .Alternatives("Hold for burst window", "Wait for better target")
                .Tip("Always use Fleche on cooldown - it's free damage with no resource cost.")
                .Concept(RdmConcepts.OgcdWeaving)
                .Record();

            context.TrainingService?.RecordConceptApplication(RdmConcepts.OgcdWeaving, true, "Fleche used on cooldown");

            return true;
        }

        return false;
    }

    private bool TryContreSixte(ICirceContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (!context.Configuration.RedMage.EnableContreSixte) return false;

        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.ContreSixte.MinLevel)
            return false;

        if (!context.ContreSixteReady)
            return false;

        // Use on cooldown - good single target and AoE damage
        if (context.ActionService.ExecuteOgcd(RDMActions.ContreSixte, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.ContreSixte.Name;
            context.Debug.BuffState = "Contre Sixte";

            // Training Mode integration
            TrainingHelper.Decision(context.TrainingService)
                .Action(RDMActions.ContreSixte.ActionId, RDMActions.ContreSixte.Name)
                .AsCasterDamage()
                .Target(target.Name?.TextValue)
                .Reason("Contre Sixte - AoE oGCD",
                    "Contre Sixte is your AoE oGCD with a 35s cooldown. It does good damage on single target " +
                    "and hits all enemies in range. Use it on cooldown alongside Fleche.")
                .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}", "35s cooldown")
                .Alternatives("Hold for burst window", "Wait for more enemies")
                .Tip("Use Contre Sixte on cooldown even on single target - the damage is worth it.")
                .Concept(RdmConcepts.OgcdWeaving)
                .Record();

            context.TrainingService?.RecordConceptApplication(RdmConcepts.OgcdWeaving, true, "Contre Sixte used on cooldown");

            return true;
        }

        return false;
    }

    private bool TryViceOfThorns(ICirceContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (!context.Configuration.RedMage.EnableViceOfThorns)
            return false;

        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.ViceOfThorns.MinLevel)
            return false;

        // Requires Thorned Flourish buff
        if (!context.HasThornedFlourish)
            return false;

        if (context.ActionService.ExecuteOgcd(RDMActions.ViceOfThorns, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.ViceOfThorns.Name;
            context.Debug.BuffState = "Vice of Thorns";

            // Training Mode integration
            TrainingHelper.Decision(context.TrainingService)
                .Action(RDMActions.ViceOfThorns.ActionId, RDMActions.ViceOfThorns.Name)
                .AsCasterBurst()
                .Target(target.Name?.TextValue)
                .Reason("Vice of Thorns - finisher proc",
                    "Vice of Thorns becomes available after using Verflare or Verholy, granting the " +
                    "Thorned Flourish buff. Use it immediately as part of your finisher sequence.")
                .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}", "Thorned Flourish active")
                .Alternatives("Continue finisher sequence", "Hold for movement")
                .Tip("Always use Vice of Thorns when available - it's free damage from your finisher combo.")
                .Concept(RdmConcepts.FinisherProcs)
                .Record();

            context.TrainingService?.RecordConceptApplication(RdmConcepts.FinisherProcs, true, "Vice of Thorns proc consumed");

            return true;
        }

        return false;
    }

    private bool TryPrefulgence(ICirceContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (!context.Configuration.RedMage.EnablePrefulgence)
            return false;

        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.Prefulgence.MinLevel)
            return false;

        // Requires Prefulgence Ready buff
        if (!context.HasPrefulgenceReady)
            return false;

        if (context.ActionService.ExecuteOgcd(RDMActions.Prefulgence, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Prefulgence.Name;
            context.Debug.BuffState = "Prefulgence";

            // Training Mode integration
            TrainingHelper.Decision(context.TrainingService)
                .Action(RDMActions.Prefulgence.ActionId, RDMActions.Prefulgence.Name)
                .AsCasterBurst()
                .Target(target.Name?.TextValue)
                .Reason("Prefulgence - finisher proc",
                    "Prefulgence becomes available after using Manafication with 6 stacks consumed. " +
                    "It's a high-damage oGCD that should be used immediately when ready.")
                .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}", "Prefulgence Ready active")
                .Alternatives("Continue burst phase", "Hold for movement")
                .Tip("Use Prefulgence immediately when it procs - it's your Manafication payoff ability.")
                .Concept(RdmConcepts.FinisherProcs)
                .Record();

            context.TrainingService?.RecordConceptApplication(RdmConcepts.FinisherProcs, true, "Prefulgence proc consumed");

            return true;
        }

        return false;
    }

    private bool TryEmbolden(ICirceContext context)
    {
        if (!context.Configuration.RedMage.EnableEmbolden)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.Embolden.MinLevel)
            return false;

        if (!context.EmboldenReady)
            return false;

        // Don't use if already active
        if (context.HasEmbolden)
            return false;

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Embolden (phase soon)";
            return false;
        }

        // Hold Embolden for imminent burst window
        if (ShouldHoldForBurst(context.Configuration.RedMage.EmboldenHoldTime))
        {
            context.Debug.BuffState = "Holding Embolden for burst";
            return false;
        }

        // Best used just before melee combo for burst alignment
        // Use when about to enter melee combo (50|50 mana ready)
        if (!context.CanStartMeleeCombo)
        {
            // Check if close to melee combo entry
            var lowerMana = context.LowerMana;
            if (lowerMana < 40)
            {
                context.Debug.BuffState = "Hold Embolden for melee combo";
                return false;
            }
        }

        // Party coordination: Synchronize with other Olympus instances
        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            // Check if our buffs are aligned with remote instances
            // If significantly desynced (e.g., death recovery), use independently
            if (!partyCoord.IsRaidBuffAligned(RDMActions.Embolden.ActionId))
            {
                context.Debug.BuffState = "Raid buffs desynced, using independently";
                // Fall through to execute - don't try to align when heavily desynced
            }
            // Check if another DPS is about to use a raid buff
            // If so, align our burst with theirs
            else if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
            {
                // Another player is about to burst - align with them
                context.Debug.BuffState = "Aligning with party burst";
                // Fall through to execute and announce our intent
            }

            // Announce our intent to use Embolden
            partyCoord.AnnounceRaidBuffIntent(RDMActions.Embolden.ActionId);
        }

        if (context.ActionService.ExecuteOgcd(RDMActions.Embolden, player.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Embolden.Name;
            context.Debug.BuffState = "Embolden (burst)";

            // Notify coordination service that we used the raid buff
            partyCoord?.OnRaidBuffUsed(RDMActions.Embolden.ActionId, 120_000);

            // Training Mode integration
            TrainingHelper.Decision(context.TrainingService)
                .Action(RDMActions.Embolden.ActionId, RDMActions.Embolden.Name)
                .AsRaidBuff()
                .Reason("Embolden - party damage buff",
                    "Embolden increases damage dealt by you and nearby party members by 5% for 20 seconds. " +
                    "Use it right before entering melee combo for maximum burst damage.")
                .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                        context.CanStartMeleeCombo ? "Melee combo ready" : "Building mana")
                .Alternatives("Wait for melee combo entry", "Hold for phase transition")
                .Tip("Align Embolden with your melee combo for maximum damage. Coordinate with other raid buffs when possible.")
                .Concept(RdmConcepts.Embolden)
                .Record();

            context.TrainingService?.RecordConceptApplication(RdmConcepts.Embolden, true, "Party buff activated");

            return true;
        }

        return false;
    }

    private bool TryManafication(ICirceContext context)
    {
        if (!context.Configuration.RedMage.EnableManafication)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.Manafication.MinLevel)
            return false;

        if (!context.ManaficationReady)
            return false;

        // Don't use if already active
        if (context.HasManafication)
            return false;

        // Don't use during melee combo
        if (context.IsInMeleeCombo)
            return false;

        // Don't use if already at 50|50 or higher (waste of mana)
        if (context.CanStartMeleeCombo)
            return false;

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Manafication (phase soon)";
            return false;
        }

        // Best used when at 40-50 mana to double it for melee combo entry
        // At level 60-89, it adds 50|50
        // At level 90+, it adds 50|50 and grants 6 Manafication stacks
        var lowerMana = context.LowerMana;

        // Ideal: Use at 40-50 to get 80-100 mana
        // At minimum: Use at 25 to get 50 mana (melee combo threshold)
        if (lowerMana < 25)
        {
            context.Debug.BuffState = "Hold Manafication (low mana)";
            return false;
        }

        // Also prefer to use with Embolden for burst alignment
        if (context.EmboldenReady && lowerMana < 40)
        {
            context.Debug.BuffState = "Hold Manafication for Embolden";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(RDMActions.Manafication, player.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Manafication.Name;
            context.Debug.BuffState = $"Manafication (mana: {context.BlackMana}|{context.WhiteMana})";

            // Training Mode integration
            TrainingHelper.Decision(context.TrainingService)
                .Action(RDMActions.Manafication.ActionId, RDMActions.Manafication.Name)
                .AsCasterResource("Mana", context.LowerMana)
                .Reason("Manafication - mana boost + damage buff",
                    "Manafication adds 50 to both Black and White Mana and grants 6 Manafication stacks " +
                    "that empower your melee combo. Use it at 40-50 mana to enable an immediate melee combo.")
                .Factors($"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}",
                        $"Lower Mana: {context.LowerMana}", "Will add 50|50")
                .Alternatives("Wait for more mana", "Align with Embolden")
                .Tip("Use Manafication at 40-50 mana for optimal value. Pair with Embolden for burst windows.")
                .Concept(RdmConcepts.Manafication)
                .Record();

            context.TrainingService?.RecordConceptApplication(RdmConcepts.Manafication, true, "Mana boosted for melee combo");

            return true;
        }

        return false;
    }

    private bool TryCorpsACorps(ICirceContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (!context.Configuration.RedMage.EnableCorpsACorps) return false;

        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.CorpsACorps.MinLevel)
            return false;

        if (context.CorpsACorpsCharges == 0)
            return false;

        // Use during melee combo for burst damage
        // Or use when capped on charges to avoid waste
        var inBurst = context.IsInMeleeCombo || context.HasEmbolden || context.HasManafication;
        var capped = context.CorpsACorpsCharges >= 2;

        if (!inBurst && !capped)
        {
            context.Debug.BuffState = "Hold Corps-a-corps for burst";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(RDMActions.CorpsACorps, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.CorpsACorps.Name;
            context.Debug.BuffState = $"Corps-a-corps ({context.CorpsACorpsCharges - 1} charges)";

            // Training Mode integration
            TrainingHelper.Decision(context.TrainingService)
                .Action(RDMActions.CorpsACorps.ActionId, RDMActions.CorpsACorps.Name)
                .AsMovement()
                .Target(target.Name?.TextValue)
                .Reason("Corps-a-corps - gap closer oGCD",
                    "Corps-a-corps is a gap closer with 2 charges. Use during burst windows (melee combo, " +
                    "Embolden, Manafication) or when capped on charges to avoid waste.")
                .Factors($"Charges: {context.CorpsACorpsCharges}", inBurst ? "In burst window" : "Capped charges",
                        $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}")
                .Alternatives("Hold for burst window", "Save for gap closing")
                .Tip("Dump charges during burst windows. Don't cap on charges - the damage adds up.")
                .Concept(RdmConcepts.MeleePositioning)
                .Record();

            context.TrainingService?.RecordConceptApplication(RdmConcepts.MeleePositioning, true, "Gap closer used in burst");

            return true;
        }

        return false;
    }

    private bool TryEngagement(ICirceContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (!context.Configuration.RedMage.EnableEngagement) return false;

        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.Engagement.MinLevel)
            return false;

        if (context.EngagementCharges == 0)
            return false;

        // Use during melee combo for burst damage
        // Or use when capped on charges to avoid waste
        var inBurst = context.IsInMeleeCombo || context.HasEmbolden || context.HasManafication;
        var capped = context.EngagementCharges >= 2;

        if (!inBurst && !capped)
        {
            context.Debug.BuffState = "Hold Engagement for burst";
            return false;
        }

        // Note: Using Engagement instead of Displacement for safety
        // Displacement backsteps which can be dangerous in fights
        if (context.ActionService.ExecuteOgcd(RDMActions.Engagement, target.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Engagement.Name;
            context.Debug.BuffState = $"Engagement ({context.EngagementCharges - 1} charges)";

            // Training Mode integration
            TrainingHelper.Decision(context.TrainingService)
                .Action(RDMActions.Engagement.ActionId, RDMActions.Engagement.Name)
                .AsMovement()
                .Target(target.Name?.TextValue)
                .Reason("Engagement - melee range oGCD",
                    "Engagement is a melee-range oGCD with 2 charges. Use during burst windows or when " +
                    "capped. Safer than Displacement which backsteps (can knock you into AoEs).")
                .Factors($"Charges: {context.EngagementCharges}", inBurst ? "In burst window" : "Capped charges",
                        $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}")
                .Alternatives("Hold for burst window", "Use Displacement for backflip")
                .Tip("Engagement is safer than Displacement. Dump charges during melee combo bursts.")
                .Concept(RdmConcepts.MeleePositioning)
                .Record();

            context.TrainingService?.RecordConceptApplication(RdmConcepts.MeleePositioning, true, "Engagement used safely");

            return true;
        }

        return false;
    }

    private bool TryAcceleration(ICirceContext context)
    {
        if (!context.Configuration.RedMage.EnableAcceleration) return false;

        var player = context.Player;
        var level = player.Level;

        if (level < RDMActions.Acceleration.MinLevel)
            return false;

        if (context.AccelerationCharges == 0)
            return false;

        // Don't use if already active
        if (context.HasAcceleration)
            return false;

        // Don't use during melee combo (procs aren't useful then)
        if (context.IsInMeleeCombo)
            return false;

        // Don't use if both procs are already active
        if (context.HasBothProcs)
        {
            context.Debug.BuffState = "Hold Acceleration (have procs)";
            return false;
        }

        // Use when no procs to guarantee one
        // Or use when capped on charges
        var noProcs = !context.HasVerfire && !context.HasVerstone;
        var capped = context.AccelerationCharges >= 2;

        if (!noProcs && !capped)
        {
            context.Debug.BuffState = "Hold Acceleration";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(RDMActions.Acceleration, player.GameObjectId))
        {
            context.Debug.PlannedAction = RDMActions.Acceleration.Name;
            context.Debug.BuffState = $"Acceleration ({context.AccelerationCharges - 1} charges)";

            // Training Mode integration
            TrainingHelper.Decision(context.TrainingService)
                .Action(RDMActions.Acceleration.ActionId, RDMActions.Acceleration.Name)
                .AsCasterProc("Acceleration")
                .Reason("Acceleration - guaranteed proc + instant cast",
                    "Acceleration makes your next Verthunder/Veraero instant and guarantees Verfire/Verstone " +
                    "procs. Use when you have no procs or when capped on charges.")
                .Factors($"Charges: {context.AccelerationCharges}", noProcs ? "No procs active" : "Capped charges",
                        context.HasVerfire ? "Has Verfire" : "No Verfire",
                        context.HasVerstone ? "Has Verstone" : "No Verstone")
                .Alternatives("Wait for proc usage", "Save for movement")
                .Tip("Use Acceleration when you have no procs to guarantee one. Don't cap charges.")
                .Concept(RdmConcepts.Acceleration)
                .Record();

            context.TrainingService?.RecordConceptApplication(RdmConcepts.Acceleration, true, "Guaranteed proc generation");

            return true;
        }

        return false;
    }

    private bool TryLucidDreaming(ICirceContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RoleActions.LucidDreaming.MinLevel)
            return false;

        if (!context.LucidDreamingReady)
            return false;

        // Use when MP is below 70%
        if (context.MpPercent > 0.7f)
            return false;

        if (context.ActionService.ExecuteOgcd(RoleActions.LucidDreaming, player.GameObjectId))
        {
            context.Debug.PlannedAction = RoleActions.LucidDreaming.Name;
            context.Debug.BuffState = "Lucid Dreaming (MP)";

            // Training Mode integration
            TrainingHelper.Decision(context.TrainingService)
                .Action(RoleActions.LucidDreaming.ActionId, RoleActions.LucidDreaming.Name)
                .AsCasterResource("MP", context.CurrentMp)
                .Reason("Lucid Dreaming - MP recovery",
                    "Lucid Dreaming restores MP over time. Use when below 70% MP to avoid running out " +
                    "during long fights. RDM uses less MP than other casters but still needs management.")
                .Factors($"Current MP: {context.CurrentMp}", $"MP%: {context.MpPercent:P0}",
                        $"Black Mana: {context.BlackMana}", $"White Mana: {context.WhiteMana}")
                .Alternatives("Wait for lower MP", "Ignore if fight ending")
                .Tip("Use Lucid Dreaming proactively around 70% MP - don't wait until you're empty.")
                .Concept(RdmConcepts.OgcdWeaving)
                .Record();

            context.TrainingService?.RecordConceptApplication(RdmConcepts.OgcdWeaving, true, "Lucid Dreaming used for MP");

            return true;
        }

        return false;
    }

    #endregion
}
