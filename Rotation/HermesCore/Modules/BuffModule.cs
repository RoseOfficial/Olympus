using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.HermesCore.Context;
using Olympus.Services.Training;
using Olympus.Services.Party;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.HermesCore.Modules;

/// <summary>
/// Handles Ninja buff management.
/// Manages Mug/Dokumori, Kassatsu, Ten Chi Jin, Bunshin, Meisui.
/// </summary>
public sealed class BuffModule : IHermesModule
{
    public int Priority => 20; // After Ninjutsu, before damage
    public string Name => "Buff";

    // Threshold for Ninki spending
    private const int NinkiThreshold = 50;

    public bool TryExecute(IHermesContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return false;
        }

        // Only use buff actions during oGCD windows
        if (!context.CanExecuteOgcd)
            return false;

        // Don't use buffs during mudra sequences
        if (context.IsMudraActive)
        {
            context.Debug.BuffState = "Mudra active";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Priority 1: Tenri Jindo (after Kunai's Bane)
        if (TryTenriJindo(context))
            return true;

        // Priority 2: Kunai's Bane / Trick Attack (main burst window)
        if (TryKunaisBane(context))
            return true;

        // Priority 3: Mug / Dokumori (damage + Ninki)
        if (TryMug(context))
            return true;

        // Priority 4: Kassatsu (enhanced Ninjutsu)
        if (TryKassatsu(context))
            return true;

        // Priority 5: Ten Chi Jin (triple Ninjutsu)
        if (TryTenChiJin(context))
            return true;

        // Priority 6: Bunshin (shadow clone)
        if (TryBunshin(context))
            return true;

        // Priority 7: Meisui (Suiton to Ninki conversion)
        if (TryMeisui(context))
            return true;

        return false;
    }

    public void UpdateDebugState(IHermesContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Timeline Awareness

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a phase transition is expected within the window.
    /// </summary>
    private bool ShouldHoldBurstForPhase(IHermesContext context, float windowSeconds = 8f)
    {
        var nextPhase = context.TimelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;

        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }

    #endregion

    #region Kunai's Bane / Trick Attack

    private bool TryKunaisBane(IHermesContext context)
    {
        if (!context.Configuration.Ninja.EnableKunaisBane)
            return false;

        var player = context.Player;
        var level = player.Level;

        // Need Suiton buff for Kunai's Bane/Trick Attack
        if (!context.HasSuiton)
        {
            context.Debug.BuffState = "Need Suiton for burst";
            return false;
        }

        // Get the appropriate action
        var action = level >= NINActions.KunaisBane.MinLevel
            ? NINActions.KunaisBane
            : (level >= NINActions.TrickAttack.MinLevel ? NINActions.TrickAttack : null);

        if (action == null)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
        {
            context.Debug.BuffState = $"{action.Name} on cooldown";
            return false;
        }

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = $"Holding {action.Name} (phase soon)";
            return false;
        }

        // Party coordination: Align with party burst window
        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            // Check if party is about to burst - if so, execute to align
            if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
            {
                context.Debug.BuffState = $"Aligning {action.Name} with party burst";
                // Fall through to execute - we want to burst WITH the party
            }

            // Announce our intent to use Kunai's Bane burst
            partyCoord.AnnounceRaidBuffIntent(NINActions.KunaisBane.ActionId);
        }

        // Find target
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            action.Range,
            player);

        if (target == null)
            return false;

        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.BuffState = $"Activating {action.Name}";

            // Notify coordination service that we used the burst
            partyCoord?.OnRaidBuffUsed(NINActions.KunaisBane.ActionId, 120_000);

            // Training: Record Kunai's Bane decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(action.ActionId, action.Name)
                .AsMeleeBurst()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason($"Activating {action.Name} (+5% damage taken debuff)",
                    "Kunai's Bane is NIN's main burst window. It applies a debuff that increases damage taken by 5% for 15 seconds. " +
                    "Use it with Suiton up, then dump all your high-potency abilities during this window. " +
                    "Coordinate with other raid buffs (Trick Attack timing) for maximum party damage.")
                .Factors(new[] { "Suiton buff active", "120s cooldown ready", "Starting burst window" })
                .Alternatives(new[] { "Wait for other raid buffs (risk delaying too long)", "Use Meisui instead (loses burst window)" })
                .Tip("Kunai's Bane is your most important ability. Plan your Ninjutsu and Ninki around its cooldown.")
                .Concept(NinConcepts.KunaisBane)
                .Record();
            context.TrainingService?.RecordConceptApplication(NinConcepts.KunaisBane, true, "Burst window activation");

            return true;
        }

        return false;
    }

    #endregion

    #region Tenri Jindo

    private bool TryTenriJindo(IHermesContext context)
    {
        var level = context.Player.Level;

        if (level < NINActions.TenriJindo.MinLevel)
            return false;

        if (!context.HasTenriJindoReady)
            return false;

        if (!context.ActionService.IsActionReady(NINActions.TenriJindo.ActionId))
        {
            context.Debug.BuffState = "Tenri Jindo not ready";
            return false;
        }

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            NINActions.TenriJindo.Range,
            context.Player);

        if (target == null)
            return false;

        if (context.ActionService.ExecuteOgcd(NINActions.TenriJindo, target.GameObjectId))
        {
            context.Debug.PlannedAction = NINActions.TenriJindo.Name;
            context.Debug.BuffState = "Tenri Jindo";

            // Training: Record Tenri Jindo decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(NINActions.TenriJindo.ActionId, NINActions.TenriJindo.Name)
                .AsMeleeDamage()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason("Using Tenri Jindo (proc from Kunai's Bane)",
                    "Tenri Jindo is a powerful follow-up attack that becomes available after using Kunai's Bane. " +
                    "It has high potency and should be used immediately during your burst window.")
                .Factors(new[] { "Tenri Jindo Ready proc active", "Within burst window" })
                .Alternatives(new[] { "Delay for weaving (loses proc if too slow)" })
                .Tip("Always use Tenri Jindo immediately after Kunai's Bane to maximize burst damage.")
                .Concept(NinConcepts.TenriJindo)
                .Record();
            context.TrainingService?.RecordConceptApplication(NinConcepts.TenriJindo, true, "Burst follow-up");

            return true;
        }

        return false;
    }

    #endregion

    #region Mug / Dokumori

    private bool TryMug(IHermesContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < NINActions.Mug.MinLevel)
            return false;

        var action = NINActions.GetMugAction(level);

        // Already have Dokumori debuff
        if (level >= NINActions.Dokumori.MinLevel && context.HasDokumoriOnTarget)
        {
            context.Debug.BuffState = $"Dokumori active ({context.DokumoriRemaining:F1}s)";
            return false;
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
        {
            context.Debug.BuffState = $"{action.Name} on cooldown";
            return false;
        }

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            action.Range,
            player);

        if (target == null)
            return false;

        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.BuffState = $"Activating {action.Name}";

            // Training: Record Mug/Dokumori decision
            var isDokumori = level >= NINActions.Dokumori.MinLevel;
            TrainingHelper.Decision(context.TrainingService)
                .Action(action.ActionId, action.Name)
                .AsMeleeDamage()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(isDokumori ? "Applying Dokumori debuff (+5% damage)" : "Using Mug for Ninki generation",
                    isDokumori
                        ? "Dokumori applies a debuff increasing damage taken by 5% and generates 40 Ninki. Use on cooldown for consistent damage and gauge generation."
                        : "Mug deals damage and generates 40 Ninki. Use on cooldown to maintain Ninki flow.")
                .Factors(new[] { "120s cooldown ready", isDokumori ? "Debuff not active" : "Ninki generation", "Damage + utility" })
                .Alternatives(new[] { "Hold for burst (not recommended)", "Delay if dying soon" })
                .Tip("Use Mug/Dokumori on cooldown for consistent Ninki generation and damage debuff.")
                .Concept(NinConcepts.MugDokumori)
                .Record();
            context.TrainingService?.RecordConceptApplication(NinConcepts.MugDokumori, true, "Cooldown management");

            return true;
        }

        return false;
    }

    #endregion

    #region Kassatsu

    private bool TryKassatsu(IHermesContext context)
    {
        var level = context.Player.Level;

        if (level < NINActions.Kassatsu.MinLevel)
            return false;

        // Already have Kassatsu
        if (context.HasKassatsu)
        {
            context.Debug.BuffState = "Kassatsu active";
            return false;
        }

        if (!context.ActionService.IsActionReady(NINActions.Kassatsu.ActionId))
        {
            context.Debug.BuffState = "Kassatsu on cooldown";
            return false;
        }

        // Optimal: Use Kassatsu during burst window
        // But don't hold it too long
        if (context.ActionService.ExecuteOgcd(NINActions.Kassatsu, context.Player.GameObjectId))
        {
            context.Debug.PlannedAction = NINActions.Kassatsu.Name;
            context.Debug.BuffState = "Activating Kassatsu";

            // Training: Record Kassatsu decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(NINActions.Kassatsu.ActionId, NINActions.Kassatsu.Name)
                .AsMeleeBurst()
                .Target("Self")
                .Reason("Activating Kassatsu for enhanced Ninjutsu",
                    "Kassatsu enhances your next Ninjutsu, upgrading Katon to Goka Mekkyaku (AoE) or Hyoton to Hyosho Ranryu (ST). " +
                    "It also restores a mudra charge. Use during burst windows for maximum damage, ideally with Kunai's Bane active.")
                .Factors(new[] { "60s cooldown ready", "Burst window active or imminent", "No current Kassatsu buff" })
                .Alternatives(new[] { "Wait for Kunai's Bane (minor optimization)", "Use outside burst (acceptable if would overcap)" })
                .Tip("Kassatsu → Hyosho Ranryu (ST) or Goka Mekkyaku (AoE) is your highest potency Ninjutsu combo.")
                .Concept(NinConcepts.Kassatsu)
                .Record();
            context.TrainingService?.RecordConceptApplication(NinConcepts.Kassatsu, true, "Enhanced Ninjutsu setup");

            return true;
        }

        return false;
    }

    #endregion

    #region Ten Chi Jin

    private bool TryTenChiJin(IHermesContext context)
    {
        var level = context.Player.Level;

        if (level < NINActions.TenChiJin.MinLevel)
            return false;

        // Already have TCJ active
        if (context.HasTenChiJin)
        {
            context.Debug.BuffState = $"TCJ active ({context.TenChiJinStacks} stacks)";
            return false;
        }

        // Don't use TCJ while moving
        if (context.IsMoving)
        {
            context.Debug.BuffState = "TCJ: Don't use while moving";
            return false;
        }

        if (!context.ActionService.IsActionReady(NINActions.TenChiJin.ActionId))
        {
            context.Debug.BuffState = "TCJ on cooldown";
            return false;
        }

        // Best used during burst window when Kunai's Bane is active
        // But don't hold too long
        if (context.ActionService.ExecuteOgcd(NINActions.TenChiJin, context.Player.GameObjectId))
        {
            context.Debug.PlannedAction = NINActions.TenChiJin.Name;
            context.Debug.BuffState = "Activating TCJ";

            // Training: Record Ten Chi Jin decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(NINActions.TenChiJin.ActionId, NINActions.TenChiJin.Name)
                .AsMeleeBurst()
                .Target("Self")
                .Reason("Activating Ten Chi Jin for triple Ninjutsu burst",
                    "Ten Chi Jin allows you to execute three Ninjutsu in rapid succession: Fuma Shuriken → Raiton → Suiton (or Katon for AoE). " +
                    "This is massive burst damage. IMPORTANT: You cannot move during TCJ or it will cancel! " +
                    "Use during Kunai's Bane window when you're safe to stand still.")
                .Factors(new[] { "120s cooldown ready", "Not moving", "Burst window active", "Safe to stand still" })
                .Alternatives(new[] { "Wait for safety (movement cancels TCJ)", "Use outside burst (loses significant damage)" })
                .Tip("TCJ is cancelled by ANY movement. Plan ahead and use it when you know you can stand still.")
                .Concept(NinConcepts.TenChiJin)
                .Record();
            context.TrainingService?.RecordConceptApplication(NinConcepts.TenChiJin, true, "Triple Ninjutsu burst");

            return true;
        }

        return false;
    }

    #endregion

    #region Bunshin

    private bool TryBunshin(IHermesContext context)
    {
        var level = context.Player.Level;

        if (level < NINActions.Bunshin.MinLevel)
            return false;

        // Already have Bunshin
        if (context.HasBunshin)
        {
            context.Debug.BuffState = $"Bunshin active ({context.BunshinStacks} stacks)";
            return false;
        }

        // Need 50 Ninki for Bunshin
        if (context.Ninki < NinkiThreshold)
        {
            context.Debug.BuffState = $"Need {NinkiThreshold - context.Ninki} more Ninki for Bunshin";
            return false;
        }

        if (!context.ActionService.IsActionReady(NINActions.Bunshin.ActionId))
        {
            context.Debug.BuffState = "Bunshin on cooldown";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(NINActions.Bunshin, context.Player.GameObjectId))
        {
            context.Debug.PlannedAction = NINActions.Bunshin.Name;
            context.Debug.BuffState = "Activating Bunshin";

            // Training: Record Bunshin decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(NINActions.Bunshin.ActionId, NINActions.Bunshin.Name)
                .AsMeleeResource("Ninki", context.Ninki)
                .Target("Self")
                .Reason($"Spending {NinkiThreshold} Ninki for Bunshin shadow clone",
                    "Bunshin creates a shadow clone that attacks alongside you for 5 weaponskills. " +
                    "It generates Phantom Kamaitachi Ready for a powerful follow-up attack. " +
                    "Costs 50 Ninki, so manage your gauge to have Ninki available when off cooldown.")
                .Factors(new[] { $"Ninki >= {NinkiThreshold}", "90s cooldown ready", "Will enable Phantom Kamaitachi" })
                .Alternatives(new[] { "Use Bhavacakra instead (if capping Ninki)", "Save for burst (if close to Kunai's Bane)" })
                .Tip("Bunshin → Phantom Kamaitachi is a potent combo. Prioritize having Ninki for Bunshin cooldowns.")
                .Concept(NinConcepts.Bunshin)
                .Record();
            context.TrainingService?.RecordConceptApplication(NinConcepts.Bunshin, true, "Shadow clone activation");

            return true;
        }

        return false;
    }

    #endregion

    #region Meisui

    private bool TryMeisui(IHermesContext context)
    {
        var level = context.Player.Level;

        if (level < NINActions.Meisui.MinLevel)
            return false;

        // Need Suiton for Meisui
        if (!context.HasSuiton)
        {
            return false;
        }

        // Don't use Meisui if Kunai's Bane is ready (save Suiton for it)
        var kunaiAction = level >= NINActions.KunaisBane.MinLevel
            ? NINActions.KunaisBane
            : NINActions.TrickAttack;
        if (context.ActionService.IsActionReady(kunaiAction.ActionId))
        {
            context.Debug.BuffState = "Save Suiton for burst";
            return false;
        }

        if (!context.ActionService.IsActionReady(NINActions.Meisui.ActionId))
        {
            context.Debug.BuffState = "Meisui on cooldown";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(NINActions.Meisui, context.Player.GameObjectId))
        {
            context.Debug.PlannedAction = NINActions.Meisui.Name;
            context.Debug.BuffState = "Activating Meisui";

            // Training: Record Meisui decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(NINActions.Meisui.ActionId, NINActions.Meisui.Name)
                .AsMeleeResource("Suiton", 1)
                .Target("Self")
                .Reason("Converting Suiton buff to Ninki (Meisui)",
                    "Meisui consumes Suiton buff to grant 50 Ninki and enhances your next Bhavacakra/Zesho Meppu. " +
                    "ONLY use when Kunai's Bane is on cooldown - otherwise you need Suiton for burst! " +
                    "This converts a 'wasted' Suiton into gauge when the buff would otherwise expire.")
                .Factors(new[] { "Suiton buff active", "Kunai's Bane on cooldown", "Ninki generation needed" })
                .Alternatives(new[] { "Save Suiton for Kunai's Bane (if ready soon)", "Let Suiton expire (wastes potential Ninki)" })
                .Tip("Meisui turns leftover Suiton into value. Never use it when Kunai's Bane is ready!")
                .Concept(NinConcepts.Meisui)
                .Record();
            context.TrainingService?.RecordConceptApplication(NinConcepts.Meisui, true, "Suiton conversion");

            return true;
        }

        return false;
    }

    #endregion
}
