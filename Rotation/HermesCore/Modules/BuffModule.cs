using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HermesCore.Abilities;
using Olympus.Rotation.HermesCore.Context;
using Olympus.Services;
using Olympus.Services.Training;
using Olympus.Services.Party;

namespace Olympus.Rotation.HermesCore.Modules;

/// <summary>
/// Handles Ninja buff management (scheduler-driven).
/// Manages Mug/Dokumori, Kassatsu, Ten Chi Jin, Bunshin, Meisui, Kunai's Bane, Tenri Jindo.
/// </summary>
public sealed class BuffModule : IHermesModule
{
    public int Priority => 20;
    public string Name => "Buff";

    private readonly IBurstWindowService? _burstWindowService;
    private const int NinkiThreshold = 50;

    public BuffModule(IBurstWindowService? burstWindowService = null)
    {
        _burstWindowService = burstWindowService;
    }

    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

    public bool TryExecute(IHermesContext context, bool isMoving) => false;

    public void UpdateDebugState(IHermesContext context) { }

    public void CollectCandidates(IHermesContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return;
        }
        if (context.IsMudraActive)
        {
            context.Debug.BuffState = "Mudra active";
            return;
        }

        TryPushTenriJindo(context, scheduler);
        TryPushKunaisBane(context, scheduler);
        TryPushMug(context, scheduler);
        TryPushKassatsu(context, scheduler);
        TryPushTenChiJin(context, scheduler);
        TryPushBunshin(context, scheduler);
        TryPushMeisui(context, scheduler);
    }

    private void TryPushKunaisBane(IHermesContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Ninja.EnableKunaisBane) return;
        var player = context.Player;
        var level = player.Level;
        if (!context.HasSuiton) return;

        var action = level >= NINActions.KunaisBane.MinLevel ? NINActions.KunaisBane
                   : level >= NINActions.TrickAttack.MinLevel ? NINActions.TrickAttack : null;
        if (action == null) return;
        if (!context.ActionService.IsActionReady(action.ActionId)) return;
        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = $"Holding {action.Name} (phase soon)";
            return;
        }
        if (ShouldHoldForBurst(context.Configuration.Ninja.KunaisBaneHoldTime))
        {
            context.Debug.BuffState = $"Holding {action.Name} for burst";
            return;
        }

        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled
            && context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
                context.Debug.BuffState = $"Aligning {action.Name} with party burst";
            partyCoord.AnnounceRaidBuffIntent(NINActions.KunaisBane.ActionId);
        }

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy, action.Range, player);
        if (target == null) return;

        var ability = action == NINActions.KunaisBane ? HermesAbilities.KunaisBane : HermesAbilities.TrickAttack;

        scheduler.PushOgcd(ability, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.BuffState = $"Activating {action.Name}";
                partyCoord?.OnRaidBuffUsed(NINActions.KunaisBane.ActionId, 120_000);
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsMeleeBurst().Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Activating {action.Name} (+5% damage taken debuff)",
                        "Kunai's Bane is NIN's main burst window.")
                    .Factors("Suiton buff active", "120s cooldown ready")
                    .Alternatives("Wait for other raid buffs")
                    .Tip("Plan your Ninjutsu and Ninki around its cooldown.")
                    .Concept(NinConcepts.KunaisBane)
                    .Record();
                context.TrainingService?.RecordConceptApplication(NinConcepts.KunaisBane, true, "Burst window activation");
            });
    }

    private void TryPushTenriJindo(IHermesContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Ninja.EnableTenriJindo) return;
        var player = context.Player;
        if (player.Level < NINActions.TenriJindo.MinLevel) return;
        if (!context.HasTenriJindoReady) return;
        if (!context.ActionService.IsActionReady(NINActions.TenriJindo.ActionId)) return;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy, NINActions.TenriJindo.Range, player);
        if (target == null) return;

        scheduler.PushOgcd(HermesAbilities.TenriJindo, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = NINActions.TenriJindo.Name;
                context.Debug.BuffState = "Tenri Jindo";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(NINActions.TenriJindo.ActionId, NINActions.TenriJindo.Name)
                    .AsMeleeDamage().Target(target.Name?.TextValue ?? "Target")
                    .Reason("Using Tenri Jindo (proc from Kunai's Bane)",
                        "Tenri Jindo is a powerful follow-up after Kunai's Bane.")
                    .Factors("Tenri Jindo Ready proc active")
                    .Alternatives("Delay (loses proc)")
                    .Tip("Always use Tenri Jindo immediately after Kunai's Bane.")
                    .Concept(NinConcepts.TenriJindo)
                    .Record();
                context.TrainingService?.RecordConceptApplication(NinConcepts.TenriJindo, true, "Burst follow-up");
            });
    }

    private void TryPushMug(IHermesContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Ninja.EnableMug) return;
        var player = context.Player;
        var level = player.Level;
        if (level < NINActions.Mug.MinLevel) return;

        var action = NINActions.GetMugAction(level);
        if (level >= NINActions.Dokumori.MinLevel && context.HasDokumoriOnTarget) return;
        if (!context.ActionService.IsActionReady(action.ActionId)) return;
        if (ShouldHoldForBurst()) return;

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy, action.Range, player);
        if (target == null) return;

        var ability = action == NINActions.Dokumori ? HermesAbilities.Dokumori : HermesAbilities.Mug;

        scheduler.PushOgcd(ability, target.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.BuffState = $"Activating {action.Name}";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsMeleeDamage().Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Using {action.Name}",
                        "Dokumori applies a debuff (+5% damage taken) and grants 40 Ninki.")
                    .Factors("120s cooldown ready")
                    .Alternatives("Hold for burst")
                    .Tip("Use Mug/Dokumori on cooldown.")
                    .Concept(NinConcepts.MugDokumori)
                    .Record();
                context.TrainingService?.RecordConceptApplication(NinConcepts.MugDokumori, true, "Cooldown management");
            });
    }

    private void TryPushKassatsu(IHermesContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Ninja.EnableKassatsu) return;
        var player = context.Player;
        if (player.Level < NINActions.Kassatsu.MinLevel) return;
        if (context.HasKassatsu) return;
        if (!context.ActionService.IsActionReady(NINActions.Kassatsu.ActionId)) return;

        scheduler.PushOgcd(HermesAbilities.Kassatsu, player.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = NINActions.Kassatsu.Name;
                context.Debug.BuffState = "Activating Kassatsu";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(NINActions.Kassatsu.ActionId, NINActions.Kassatsu.Name)
                    .AsMeleeBurst().Target("Self")
                    .Reason("Activating Kassatsu for enhanced Ninjutsu",
                        "Kassatsu enhances your next Ninjutsu.")
                    .Factors("60s cooldown ready")
                    .Alternatives("Wait for Kunai's Bane")
                    .Tip("Kassatsu → Hyosho Ranryu or Goka Mekkyaku is your highest potency Ninjutsu combo.")
                    .Concept(NinConcepts.Kassatsu)
                    .Record();
                context.TrainingService?.RecordConceptApplication(NinConcepts.Kassatsu, true, "Enhanced Ninjutsu setup");
            });
    }

    private void TryPushTenChiJin(IHermesContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Ninja.EnableTenChiJin) return;
        var player = context.Player;
        if (player.Level < NINActions.TenChiJin.MinLevel) return;
        if (context.HasTenChiJin) return;
        if (context.IsMoving) return;
        if (!context.ActionService.IsActionReady(NINActions.TenChiJin.ActionId)) return;

        scheduler.PushOgcd(HermesAbilities.TenChiJin, player.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = NINActions.TenChiJin.Name;
                context.Debug.BuffState = "Activating TCJ";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(NINActions.TenChiJin.ActionId, NINActions.TenChiJin.Name)
                    .AsMeleeBurst().Target("Self")
                    .Reason("Activating Ten Chi Jin for triple Ninjutsu burst",
                        "TCJ allows three Ninjutsu in rapid succession. Movement cancels TCJ.")
                    .Factors("120s cooldown ready", "Not moving")
                    .Alternatives("Wait for safety")
                    .Tip("TCJ is cancelled by ANY movement.")
                    .Concept(NinConcepts.TenChiJin)
                    .Record();
                context.TrainingService?.RecordConceptApplication(NinConcepts.TenChiJin, true, "Triple Ninjutsu burst");
            });
    }

    private void TryPushBunshin(IHermesContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Ninja.EnableBunshin) return;
        var player = context.Player;
        if (player.Level < NINActions.Bunshin.MinLevel) return;
        if (context.HasBunshin) return;
        if (context.Ninki < NinkiThreshold) return;
        if (!context.ActionService.IsActionReady(NINActions.Bunshin.ActionId)) return;

        scheduler.PushOgcd(HermesAbilities.Bunshin, player.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = NINActions.Bunshin.Name;
                context.Debug.BuffState = "Activating Bunshin";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(NINActions.Bunshin.ActionId, NINActions.Bunshin.Name)
                    .AsMeleeResource("Ninki", context.Ninki).Target("Self")
                    .Reason($"Spending {NinkiThreshold} Ninki for Bunshin shadow clone",
                        "Bunshin creates a shadow clone for 5 weaponskills.")
                    .Factors($"Ninki >= {NinkiThreshold}", "90s cooldown ready")
                    .Alternatives("Use Bhavacakra (if capping)")
                    .Tip("Bunshin → Phantom Kamaitachi is a potent combo.")
                    .Concept(NinConcepts.Bunshin)
                    .Record();
                context.TrainingService?.RecordConceptApplication(NinConcepts.Bunshin, true, "Shadow clone activation");
            });
    }

    private void TryPushMeisui(IHermesContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Ninja.EnableMeisui) return;
        var player = context.Player;
        var level = player.Level;
        if (level < NINActions.Meisui.MinLevel) return;
        if (!context.HasSuiton) return;

        var kunaiAction = level >= NINActions.KunaisBane.MinLevel ? NINActions.KunaisBane : NINActions.TrickAttack;
        if (context.ActionService.IsActionReady(kunaiAction.ActionId))
        {
            context.Debug.BuffState = "Save Suiton for burst";
            return;
        }
        if (!context.ActionService.IsActionReady(NINActions.Meisui.ActionId)) return;

        scheduler.PushOgcd(HermesAbilities.Meisui, player.GameObjectId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = NINActions.Meisui.Name;
                context.Debug.BuffState = "Activating Meisui";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(NINActions.Meisui.ActionId, NINActions.Meisui.Name)
                    .AsMeleeResource("Suiton", 1).Target("Self")
                    .Reason("Converting Suiton buff to Ninki (Meisui)",
                        "Meisui consumes Suiton to grant 50 Ninki and enhance next Bhavacakra.")
                    .Factors("Suiton buff active", "Kunai's Bane on cooldown")
                    .Alternatives("Save Suiton for Kunai's Bane")
                    .Tip("Never use Meisui when Kunai's Bane is ready.")
                    .Concept(NinConcepts.Meisui)
                    .Record();
                context.TrainingService?.RecordConceptApplication(NinConcepts.Meisui, true, "Suiton conversion");
            });
    }
}
