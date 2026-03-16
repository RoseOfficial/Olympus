using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.IrisCore.Context;
using Olympus.Services.Training;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.IrisCore.Modules;

/// <summary>
/// Handles Pictomancer oGCD abilities.
/// Manages Muses, Portraits, Subtractive Palette, and utility oGCDs.
/// </summary>
public sealed class BuffModule : IIrisModule
{
    public int Priority => 20; // Higher priority than damage (lower number = higher priority)
    public string Name => "Buff";

    public bool TryExecute(IIrisContext context, bool isMoving)
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

        // Priority 1: Star Prism finisher during Starstruck (this is actually a GCD but instant)
        // This is handled in DamageModule

        // Priority 2: Portraits - use immediately when ready
        if (TryPortrait(context, target))
            return true;

        // Priority 3: Starry Muse (align with raid buffs, use off cooldown)
        if (TryStarryMuse(context))
            return true;

        // Priority 4: Living Muse (dump charges during burst)
        if (TryLivingMuse(context, target))
            return true;

        // Priority 5: Striking Muse (use when hammer painted)
        if (TryStrikingMuse(context))
            return true;

        // Priority 6: Subtractive Palette (when gauge >= 50 and not already active)
        if (TrySubtractivePalette(context))
            return true;

        // Priority 7: Lucid Dreaming (when MP < 70%)
        if (TryLucidDreaming(context))
            return true;

        // Priority 8: Tempera Coat for mitigation
        // (Not automated - save for tankbusters)

        context.Debug.BuffState = "No oGCD needed";
        return false;
    }

    public void UpdateDebugState(IIrisContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Timeline Awareness

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a phase transition is expected within the window.
    /// </summary>
    private bool ShouldHoldBurstForPhase(IIrisContext context, float windowSeconds = 8f)
    {
        var nextPhase = context.TimelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;

        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }

    #endregion

    #region oGCD Actions

    private bool TryPortrait(IIrisContext context, IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        // Check Madeen first (higher priority, Lv.96+)
        if (context.MadeenReady && level >= PCTActions.RetributionOfTheMadeen.MinLevel)
        {
            if (context.ActionService.ExecuteOgcd(PCTActions.RetributionOfTheMadeen, target.GameObjectId))
            {
                context.Debug.PlannedAction = PCTActions.RetributionOfTheMadeen.Name;
                context.Debug.BuffState = "Madeen";

                // Training Mode integration
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PCTActions.RetributionOfTheMadeen.ActionId, PCTActions.RetributionOfTheMadeen.Name)
                    .AsCasterBurst()
                    .Target(target.Name?.TextValue)
                    .Reason("Madeen - high damage portrait",
                        "Retribution of the Madeen is your most powerful portrait ability, available after " +
                        "summoning 4 Living Muses. Use it immediately when ready for massive burst damage.")
                    .Factors($"Palette Gauge: {context.PaletteGauge}", context.IsInBurstWindow ? "In burst window" : "Outside burst",
                        $"Muse Charges: {context.LivingMuseCharges}")
                    .Alternatives("Hold for burst window", "Wait for Mog")
                    .Tip("Use Madeen immediately when it becomes available - it's your highest damage oGCD.")
                    .Concept(PctConcepts.CreatureMotifs)
                    .Record();

                context.TrainingService?.RecordConceptApplication(PctConcepts.CreatureMotifs, true, "Madeen portrait used");

                return true;
            }
        }

        // Check Mog of the Ages
        if (context.MogReady && level >= PCTActions.MogOfTheAges.MinLevel)
        {
            if (context.ActionService.ExecuteOgcd(PCTActions.MogOfTheAges, target.GameObjectId))
            {
                context.Debug.PlannedAction = PCTActions.MogOfTheAges.Name;
                context.Debug.BuffState = "Mog";

                // Training Mode integration
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PCTActions.MogOfTheAges.ActionId, PCTActions.MogOfTheAges.Name)
                    .AsCasterBurst()
                    .Target(target.Name?.TextValue)
                    .Reason("Mog - portrait ability",
                        "Mog of the Ages becomes available after summoning 2 Living Muses. Use it immediately " +
                        "when ready as it's high burst damage. Building toward Madeen requires 2 more Muses.")
                    .Factors($"Palette Gauge: {context.PaletteGauge}", context.IsInBurstWindow ? "In burst window" : "Outside burst",
                        $"Muse Charges: {context.LivingMuseCharges}")
                    .Alternatives("Hold for burst window", "Wait for more enemies")
                    .Tip("Use Mog immediately when ready. It builds toward Madeen for even more damage.")
                    .Concept(PctConcepts.CreatureMotifs)
                    .Record();

                context.TrainingService?.RecordConceptApplication(PctConcepts.CreatureMotifs, true, "Mog portrait used");

                return true;
            }
        }

        return false;
    }

    private bool TryStarryMuse(IIrisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.StarryMuse.MinLevel)
            return false;

        if (!context.StarryMuseReady)
            return false;

        // Need landscape canvas painted
        if (!context.HasLandscapeCanvas)
        {
            context.Debug.BuffState = "Need Landscape for Starry";
            return false;
        }

        // Don't use if already active
        if (context.HasStarryMuse)
            return false;

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Starry Muse (phase soon)";
            return false;
        }

        // Party coordination: Synchronize with other Olympus instances
        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            // Check if our buffs are aligned with remote instances
            // If significantly desynced (e.g., death recovery), use independently
            if (!partyCoord.IsRaidBuffAligned(PCTActions.StarryMuse.ActionId))
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

            // Announce our intent to use Starry Muse
            partyCoord.AnnounceRaidBuffIntent(PCTActions.StarryMuse.ActionId);
        }

        if (context.ActionService.ExecuteOgcd(PCTActions.StarryMuse, player.GameObjectId))
        {
            context.Debug.PlannedAction = PCTActions.StarryMuse.Name;
            context.Debug.BuffState = "Starry Muse (burst)";

            // Notify coordination service that we used the raid buff
            partyCoord?.OnRaidBuffUsed(PCTActions.StarryMuse.ActionId, 120_000);

            // Training Mode integration
            TrainingHelper.Decision(context.TrainingService)
                .Action(PCTActions.StarryMuse.ActionId, PCTActions.StarryMuse.Name)
                .AsRaidBuff()
                .Reason("Starry Muse - party damage buff",
                    "Starry Muse is your 2-minute raid buff that increases damage for you and your party. " +
                    "It requires a painted Landscape (Starry Sky) canvas and grants Hyperphantasia stacks.")
                .Factors($"Landscape Canvas: Ready", $"Palette Gauge: {context.PaletteGauge}",
                    partyCoord != null ? "Party coordination active" : "Solo mode")
                .Alternatives("Hold for phase transition", "Wait for party buffs")
                .Tip("Align Starry Muse with other raid buffs when possible. Always paint Landscape before burst.")
                .Concept(PctConcepts.StarryMuseBurst)
                .Record();

            context.TrainingService?.RecordConceptApplication(PctConcepts.StarryMuseBurst, true, "Raid buff activated");

            return true;
        }

        return false;
    }

    private bool TryLivingMuse(IIrisContext context, IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.LivingMuse.MinLevel)
            return false;

        if (!context.LivingMuseReady)
            return false;

        // Need creature canvas painted
        if (!context.HasCreatureCanvas)
        {
            context.Debug.BuffState = "Need Creature for Muse";
            return false;
        }

        // Get the appropriate muse action based on creature type
        var museAction = PCTActions.GetLivingMuse(context.CreatureMotifType);

        if (context.ActionService.ExecuteOgcd(museAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = museAction.Name;
            context.Debug.BuffState = $"Living Muse ({context.LivingMuseCharges - 1} charges)";

            // Training Mode integration
            TrainingHelper.Decision(context.TrainingService)
                .Action(museAction.ActionId, museAction.Name)
                .AsCasterDamage()
                .Target(target.Name?.TextValue)
                .Reason("Living Muse - creature summon",
                    $"Living Muse summons your painted creature ({context.CreatureMotifType}) to deal damage. " +
                    "It has 2 charges and builds toward portrait abilities (Mog after 2, Madeen after 4).")
                .Factors($"Creature Type: {context.CreatureMotifType}", $"Charges: {context.LivingMuseCharges}",
                    context.IsInBurstWindow ? "In burst window" : "Outside burst")
                .Alternatives("Hold charges for burst", "Wait for portrait ready")
                .Tip("Don't cap on Living Muse charges. Each summon builds toward portraits.")
                .Concept(PctConcepts.LivingMuse)
                .Record();

            context.TrainingService?.RecordConceptApplication(PctConcepts.LivingMuse, true, "Living Muse summoned");

            return true;
        }

        return false;
    }

    private bool TryStrikingMuse(IIrisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.StrikingMuse.MinLevel)
            return false;

        if (!context.StrikingMuseReady)
            return false;

        // Need weapon canvas painted
        if (!context.HasWeaponCanvas)
        {
            context.Debug.BuffState = "Need Weapon for Striking";
            return false;
        }

        // Don't use if already have Hammer Time
        if (context.HasHammerTime)
            return false;

        if (context.ActionService.ExecuteOgcd(PCTActions.StrikingMuse, player.GameObjectId))
        {
            context.Debug.PlannedAction = PCTActions.StrikingMuse.Name;
            context.Debug.BuffState = "Striking Muse";

            // Training Mode integration
            TrainingHelper.Decision(context.TrainingService)
                .Action(PCTActions.StrikingMuse.ActionId, PCTActions.StrikingMuse.Name)
                .AsCasterDamage()
                .Target(null)
                .Reason("Striking Muse - hammer combo enabler",
                    "Striking Muse consumes your painted Weapon (Hammer) canvas and grants Hammer Time, " +
                    "enabling the powerful hammer combo (Stamp → Brush → Polish). All hammer hits are instant.")
                .Factors("Weapon Canvas: Ready", $"Palette Gauge: {context.PaletteGauge}",
                    context.IsInBurstWindow ? "In burst window" : "Outside burst")
                .Alternatives("Hold for burst window", "Wait for better timing")
                .Tip("Use Striking Muse when Hammer canvas is ready. The hammer combo is high damage and instant.")
                .Concept(PctConcepts.StrikingMuse)
                .Record();

            context.TrainingService?.RecordConceptApplication(PctConcepts.StrikingMuse, true, "Hammer Time activated");

            return true;
        }

        return false;
    }

    private bool TrySubtractivePalette(IIrisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.SubtractivePalette.MinLevel)
            return false;

        if (!context.SubtractivePaletteReady)
            return false;

        // Already have subtractive buff
        if (context.HasSubtractivePalette)
            return false;

        // Need 50+ gauge
        if (!context.CanUseSubtractivePalette)
        {
            context.Debug.BuffState = $"Need 50 gauge ({context.PaletteGauge})";
            return false;
        }

        // Prefer to use during burst window
        // But don't overcap gauge (use at 75+ regardless)
        // SubtractiveSpectrum is a free enhanced sequence — bypass burst hold to avoid missing it
        if (!context.IsInBurstWindow && context.PaletteGauge < 75 && !context.HasSubtractiveSpectrum)
        {
            context.Debug.BuffState = "Hold Subtractive for burst";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(PCTActions.SubtractivePalette, player.GameObjectId))
        {
            context.Debug.PlannedAction = PCTActions.SubtractivePalette.Name;
            context.Debug.BuffState = "Subtractive Palette";

            // Training Mode integration
            TrainingHelper.Decision(context.TrainingService)
                .Action(PCTActions.SubtractivePalette.ActionId, PCTActions.SubtractivePalette.Name)
                .AsCasterResource("Palette Gauge", context.PaletteGauge)
                .Reason("Subtractive Palette - enhanced combo",
                    "Subtractive Palette consumes 50 Palette Gauge to enable the subtractive combo " +
                    "(Cyan → Yellow → Magenta). This is higher damage than the base combo. Don't overcap gauge.")
                .Factors($"Palette Gauge: {context.PaletteGauge}", context.IsInBurstWindow ? "In burst (use now)" : "Outside burst",
                    context.PaletteGauge >= 75 ? "Overcap risk" : "Gauge healthy")
                .Alternatives("Hold for burst window", "Use base combo instead")
                .Tip("Use Subtractive Palette at 50+ gauge during burst. At 75+ gauge, use immediately to prevent waste.")
                .Concept(PctConcepts.SubtractivePalette)
                .Record();

            context.TrainingService?.RecordConceptApplication(PctConcepts.SubtractivePalette, true, "Subtractive combo enabled");

            return true;
        }

        return false;
    }

    private bool TryLucidDreaming(IIrisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PCTActions.LucidDreaming.MinLevel)
            return false;

        if (!context.LucidDreamingReady)
            return false;

        // Use when MP is below 70%
        if (context.MpPercent > 0.7f)
            return false;

        if (context.ActionService.ExecuteOgcd(PCTActions.LucidDreaming, player.GameObjectId))
        {
            context.Debug.PlannedAction = PCTActions.LucidDreaming.Name;
            context.Debug.BuffState = "Lucid Dreaming (MP)";

            // Training Mode integration
            TrainingHelper.Decision(context.TrainingService)
                .Action(PCTActions.LucidDreaming.ActionId, PCTActions.LucidDreaming.Name)
                .AsCasterResource("MP", context.CurrentMp)
                .Reason("Lucid Dreaming - MP recovery",
                    "Lucid Dreaming restores MP over time. Use when below 70% MP to avoid running out " +
                    "during long fights. Pictomancer uses moderate MP but needs management.")
                .Factors($"Current MP: {context.CurrentMp}", $"MP%: {context.MpPercent:P0}",
                    $"Palette Gauge: {context.PaletteGauge}")
                .Alternatives("Wait for lower MP", "Ignore if fight ending")
                .Tip("Use Lucid Dreaming proactively around 70% MP - don't wait until you're empty.")
                .Concept(PctConcepts.PaletteGauge)
                .Record();

            context.TrainingService?.RecordConceptApplication(PctConcepts.PaletteGauge, true, "Lucid Dreaming used for MP");

            return true;
        }

        return false;
    }

    #endregion
}
