using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.RoleActionHelpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.IrisCore.Abilities;
using Olympus.Rotation.IrisCore.Context;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.IrisCore.Modules;

/// <summary>
/// Handles Pictomancer oGCD abilities (scheduler-driven).
/// Manages Muses, Portraits, Subtractive Palette, utility oGCDs.
/// </summary>
public sealed class BuffModule : IIrisModule
{
    public int Priority => 30;
    public string Name => "Buff";

    private readonly IBurstWindowService? _burstWindowService;

    public BuffModule(IBurstWindowService? burstWindowService = null)
    {
        _burstWindowService = burstWindowService;
    }

    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

    public bool TryExecute(IIrisContext context, bool isMoving) => false;

    public void UpdateDebugState(IIrisContext context) { }

    public void CollectCandidates(IIrisContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return;
        }

        var player = context.Player;
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.CasterTargetingRange,
            player);

        TryPushPortrait(context, scheduler, target);
        TryPushStarryMuse(context, scheduler);
        TryPushLivingMuse(context, scheduler, target);
        TryPushStrikingMuse(context, scheduler);
        TryPushSubtractivePalette(context, scheduler);
        TryPushLucidDreaming(context, scheduler);
        TryPushTemperaGrassa(context, scheduler);
        TryPushTemperaCoat(context, scheduler);
        TryPushSmudge(context, scheduler, isMoving);
        TryPushSwiftcast(context, scheduler, isMoving);
    }

    private void TryPushPortrait(IIrisContext context, RotationScheduler scheduler, IBattleChara? target)
    {
        if (!context.Configuration.Pictomancer.EnablePortraits) return;
        if (target == null) return;
        var level = context.Player.Level;

        if (context.MadeenReady && level >= PCTActions.RetributionOfTheMadeen.MinLevel)
        {
            scheduler.PushOgcd(IrisAbilities.RetributionOfTheMadeen, target.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = PCTActions.RetributionOfTheMadeen.Name;
                    context.Debug.BuffState = "Madeen";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(PCTActions.RetributionOfTheMadeen.ActionId, PCTActions.RetributionOfTheMadeen.Name)
                        .AsCasterBurst().Target(target.Name?.TextValue)
                        .Reason("Madeen - high damage portrait",
                            "Retribution of the Madeen is your most powerful portrait ability.")
                        .Factors($"Palette Gauge: {context.PaletteGauge}", $"Muse Charges: {context.LivingMuseCharges}")
                        .Alternatives("Hold for burst window")
                        .Tip("Use Madeen immediately when it becomes available.")
                        .Concept(PctConcepts.CreatureMotifs)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(PctConcepts.CreatureMotifs, true, "Madeen portrait used");
                });
            return;
        }

        if (context.MogReady && level >= PCTActions.MogOfTheAges.MinLevel)
        {
            scheduler.PushOgcd(IrisAbilities.MogOfTheAges, target.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = PCTActions.MogOfTheAges.Name;
                    context.Debug.BuffState = "Mog";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(PCTActions.MogOfTheAges.ActionId, PCTActions.MogOfTheAges.Name)
                        .AsCasterBurst().Target(target.Name?.TextValue)
                        .Reason("Mog - portrait ability",
                            "Mog of the Ages becomes available after summoning 2 Living Muses.")
                        .Factors($"Palette Gauge: {context.PaletteGauge}", $"Muse Charges: {context.LivingMuseCharges}")
                        .Alternatives("Hold for burst window")
                        .Tip("Use Mog immediately when ready.")
                        .Concept(PctConcepts.CreatureMotifs)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(PctConcepts.CreatureMotifs, true, "Mog portrait used");
                });
        }
    }

    private void TryPushStarryMuse(IIrisContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Pictomancer.EnableStarryMuse) return;
        var player = context.Player;
        if (player.Level < PCTActions.StarryMuse.MinLevel) return;
        if (!context.StarryMuseReady) return;
        if (!context.HasLandscapeCanvas)
        {
            context.Debug.BuffState = "Need Landscape for Starry";
            return;
        }
        if (context.HasStarryMuse) return;
        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = "Holding Starry Muse (phase soon)";
            return;
        }
        if (ShouldHoldForBurst(context.Configuration.Pictomancer.StarryMuseHoldTime))
        {
            context.Debug.BuffState = "Holding Starry Muse for burst";
            return;
        }

        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled
            && context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            if (!partyCoord.IsRaidBuffAligned(PCTActions.StarryMuse.ActionId))
                context.Debug.BuffState = "Raid buffs desynced, using independently";
            else if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
                context.Debug.BuffState = "Aligning with party burst";
            partyCoord.AnnounceRaidBuffIntent(PCTActions.StarryMuse.ActionId);
        }

        scheduler.PushOgcd(IrisAbilities.StarryMuse, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PCTActions.StarryMuse.Name;
                context.Debug.BuffState = "Starry Muse (burst)";
                partyCoord?.OnRaidBuffUsed(PCTActions.StarryMuse.ActionId, 120_000);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PCTActions.StarryMuse.ActionId, PCTActions.StarryMuse.Name)
                    .AsRaidBuff()
                    .Reason("Starry Muse - party damage buff",
                        "Starry Muse is your 2-minute raid buff that increases damage for you and your party.")
                    .Factors("Landscape Canvas Ready", $"Palette Gauge: {context.PaletteGauge}")
                    .Alternatives("Hold for phase transition")
                    .Tip("Always paint Landscape before burst.")
                    .Concept(PctConcepts.StarryMuseBurst)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PctConcepts.StarryMuseBurst, true, "Raid buff activated");
            });
    }

    private void TryPushLivingMuse(IIrisContext context, RotationScheduler scheduler, IBattleChara? target)
    {
        if (!context.Configuration.Pictomancer.EnableLivingMuse) return;
        if (target == null) return;
        var player = context.Player;
        if (player.Level < PCTActions.LivingMuse.MinLevel) return;
        if (!context.LivingMuseReady) return;
        if (!context.HasCreatureCanvas) return;

        var museAction = PCTActions.GetLivingMuse(context.CreatureMotifType);
        var ability = museAction.ActionId switch
        {
            var id when id == PCTActions.PomMuse.ActionId => IrisAbilities.PomMuse,
            var id when id == PCTActions.WingedMuse.ActionId => IrisAbilities.WingedMuse,
            var id when id == PCTActions.ClawedMuse.ActionId => IrisAbilities.ClawedMuse,
            var id when id == PCTActions.FangedMuse.ActionId => IrisAbilities.FangedMuse,
            _ => IrisAbilities.PomMuse,
        };

        scheduler.PushOgcd(ability, target.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = museAction.Name;
                context.Debug.BuffState = $"Living Muse ({context.LivingMuseCharges - 1} charges)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(museAction.ActionId, museAction.Name)
                    .AsCasterDamage().Target(target.Name?.TextValue)
                    .Reason("Living Muse - creature summon",
                        "Living Muse summons your painted creature to deal damage.")
                    .Factors($"Creature Type: {context.CreatureMotifType}", $"Charges: {context.LivingMuseCharges}")
                    .Alternatives("Hold charges for burst")
                    .Tip("Don't cap on Living Muse charges.")
                    .Concept(PctConcepts.LivingMuse)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PctConcepts.LivingMuse, true, "Living Muse summoned");
            });
    }

    private void TryPushStrikingMuse(IIrisContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Pictomancer.EnableSteelMuse) return;
        var player = context.Player;
        if (player.Level < PCTActions.StrikingMuse.MinLevel) return;
        if (!context.StrikingMuseReady) return;
        if (!context.HasWeaponCanvas) return;
        if (context.HasHammerTime) return;

        scheduler.PushOgcd(IrisAbilities.StrikingMuse, player.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PCTActions.StrikingMuse.Name;
                context.Debug.BuffState = "Striking Muse";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PCTActions.StrikingMuse.ActionId, PCTActions.StrikingMuse.Name)
                    .AsCasterDamage()
                    .Reason("Striking Muse - hammer combo enabler",
                        "Striking Muse consumes Weapon canvas and grants Hammer Time.")
                    .Factors("Weapon Canvas: Ready", $"Palette Gauge: {context.PaletteGauge}")
                    .Alternatives("Hold for burst window")
                    .Tip("Use Striking Muse when Hammer canvas is ready.")
                    .Concept(PctConcepts.StrikingMuse)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PctConcepts.StrikingMuse, true, "Hammer Time activated");
            });
    }

    private void TryPushSubtractivePalette(IIrisContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Pictomancer.EnableSubtractivePalette) return;
        var player = context.Player;
        if (player.Level < PCTActions.SubtractivePalette.MinLevel) return;
        if (!context.SubtractivePaletteReady) return;
        if (context.HasSubtractivePalette) return;
        if (context.HasSubtractiveSpectrum) return;
        if (!context.CanUseSubtractivePalette) return;
        // SavePaletteForComet: defer if Comet will be available soon and gauge is not overflowing
        if (context.Configuration.Pictomancer.SavePaletteForComet
            && context.Configuration.Pictomancer.EnableCometInBlack
            && !context.HasBlackPaint
            && context.PaletteGauge < 90)
        {
            context.Debug.BuffState = "Hold Subtractive (saving for Comet)";
            return;
        }
        if (!context.IsInBurstWindow && context.PaletteGauge < 75)
        {
            context.Debug.BuffState = "Hold Subtractive for burst";
            return;
        }

        scheduler.PushOgcd(IrisAbilities.SubtractivePalette, player.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PCTActions.SubtractivePalette.Name;
                context.Debug.BuffState = "Subtractive Palette";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(PCTActions.SubtractivePalette.ActionId, PCTActions.SubtractivePalette.Name)
                    .AsCasterResource("Palette Gauge", context.PaletteGauge)
                    .Reason("Subtractive Palette - enhanced combo",
                        "Subtractive Palette consumes 50 Palette Gauge to enable the subtractive combo.")
                    .Factors($"Palette Gauge: {context.PaletteGauge}")
                    .Alternatives("Hold for burst window")
                    .Tip("Use Subtractive Palette at 50+ gauge during burst.")
                    .Concept(PctConcepts.SubtractivePalette)
                    .Record();
                context.TrainingService?.RecordConceptApplication(PctConcepts.SubtractivePalette, true, "Subtractive combo enabled");
            });
    }

    private void TryPushLucidDreaming(IIrisContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.CasterShared.EnableLucidDreaming) return;

        RoleActionPushers.TryPushLucidDreaming(
            context, scheduler, IrisAbilities.LucidDreaming,
            mpThresholdPct: context.Configuration.CasterShared.LucidDreamingThreshold,
            priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.LucidDreaming.Name;
                context.Debug.BuffState = "Lucid Dreaming (MP)";
            });
    }

    private void TryPushTemperaGrassa(IIrisContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        if (player.Level < PCTActions.TemperaGrassa.MinLevel) return;
        if (!context.Configuration.Pictomancer.EnableTemperaGrassa) return;
        if (!context.TemperaGrassaReady) return;

        var playerHpPct = player.MaxHp > 0 ? (float)player.CurrentHp / player.MaxHp : 1f;
        var partyAvgHp = context.PartyHealthMetrics.avgHpPercent;
        if (playerHpPct > 0.85f && partyAvgHp > 0.85f) return;

        scheduler.PushOgcd(IrisAbilities.TemperaGrassa, player.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PCTActions.TemperaGrassa.Name;
                context.Debug.BuffState = "Tempera Grassa (party mitigation)";
            });
    }

    private void TryPushTemperaCoat(IIrisContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        if (player.Level < PCTActions.TemperaCoat.MinLevel) return;
        if (!context.Configuration.Pictomancer.EnableTemperaCoat) return;
        if (!context.TemperaCoatReady) return;

        var playerHpPct = player.MaxHp > 0 ? (float)player.CurrentHp / player.MaxHp : 1f;
        if (playerHpPct > 0.80f) return;

        scheduler.PushOgcd(IrisAbilities.TemperaCoat, player.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PCTActions.TemperaCoat.Name;
                context.Debug.BuffState = "Tempera Coat (self mitigation)";
            });
    }

    private void TryPushSmudge(IIrisContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.Configuration.Pictomancer.EnableSmudge) return;
        if (!isMoving) return;
        if (context.Player.Level < PCTActions.Smudge.MinLevel) return;
        if (!context.SmudgeReady) return;

        scheduler.PushOgcd(IrisAbilities.Smudge, context.Player.GameObjectId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = PCTActions.Smudge.Name;
                context.Debug.BuffState = "Smudge (movement)";
            });
    }

    private void TryPushSwiftcast(IIrisContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!isMoving) return;
        if (context.Player.Level < RoleActions.Swiftcast.MinLevel) return;
        if (!context.SwiftcastReady) return;
        if (context.HasInstantCast || context.HasWhitePaint || context.HasBlackPaint
            || context.HasRainbowBright || context.HasStarstruck || context.HasHammerTime) return;

        scheduler.PushOgcd(IrisAbilities.Swiftcast, context.Player.GameObjectId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.Swiftcast.Name;
                context.Debug.BuffState = "Swiftcast (movement)";
            });
    }
}
