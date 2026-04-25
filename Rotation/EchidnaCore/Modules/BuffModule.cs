using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.EchidnaCore.Abilities;
using Olympus.Rotation.EchidnaCore.Context;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.EchidnaCore.Modules;

/// <summary>
/// Handles the Viper buff management (scheduler-driven).
/// Manages Serpent's Ire (party buff) and Reawaken timing setup.
/// </summary>
public sealed class BuffModule : IEchidnaModule
{
    public int Priority => 20;
    public string Name => "Buff";

    private readonly IBurstWindowService? _burstWindowService;

    public BuffModule(IBurstWindowService? burstWindowService = null)
    {
        _burstWindowService = burstWindowService;
    }

    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

    public bool TryExecute(IEchidnaContext context, bool isMoving) => false;

    public void UpdateDebugState(IEchidnaContext context) { }

    public void CollectCandidates(IEchidnaContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return;
        }

        TryPushSerpentsIre(context, scheduler);
    }

    private void TryPushSerpentsIre(IEchidnaContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Viper.EnableSerpentsIre) return;
        var player = context.Player;
        if (player.Level < VPRActions.SerpentsIre.MinLevel) return;
        if (!context.ActionService.IsActionReady(VPRActions.SerpentsIre.ActionId)) return;
        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = "Holding Serpent's Ire (phase soon)";
            return;
        }
        if (ShouldHoldForBurst(context.Configuration.Viper.SerpentsIreHoldTime))
        {
            context.Debug.BuffState = "Holding Serpent's Ire for burst";
            return;
        }
        if (!context.HasNoxiousGnash)
        {
            context.Debug.BuffState = "Waiting for Noxious Gnash";
            return;
        }
        if (!context.HasHuntersInstinct || !context.HasSwiftscaled)
        {
            context.Debug.BuffState = "Waiting for buffs";
            return;
        }

        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
                context.Debug.BuffState = "Aligning Serpent's Ire with party burst";
            partyCoord.AnnounceRaidBuffIntent(VPRActions.SerpentsIre.ActionId);
        }

        scheduler.PushOgcd(EchidnaAbilities.SerpentsIre, player.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = VPRActions.SerpentsIre.Name;
                context.Debug.BuffState = "Activating Serpent's Ire";
                partyCoord?.OnRaidBuffUsed(VPRActions.SerpentsIre.ActionId, 120_000);

                TrainingHelper.Decision(context.TrainingService)
                    .Action(VPRActions.SerpentsIre.ActionId, VPRActions.SerpentsIre.Name)
                    .AsMeleeBurst()
                    .Target(player.Name?.TextValue ?? "Self")
                    .Reason("Activating Serpent's Ire (party burst + Ready to Reawaken)",
                        "Serpent's Ire is VPR's 2-minute party buff. Grants Ready to Reawaken for a free Reawaken entry " +
                        "and +1 Rattling Coil stack. Use during raid buff windows for maximum party coordination.")
                    .Factors(new[] { "120s cooldown ready", "Noxious Gnash active", "Hunter's Instinct + Swiftscaled active" })
                    .Alternatives(new[] { "Hold for raid buff alignment", "Hold for phase timing" })
                    .Tip("Serpent's Ire grants Ready to Reawaken. Enter Reawaken immediately after for maximum burst damage.")
                    .Concept("vpr.serpents_ire")
                    .Record();
                context.TrainingService?.RecordConceptApplication("vpr.serpents_ire", true, "Party burst activation");
            });
    }
}
