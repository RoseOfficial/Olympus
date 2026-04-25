using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HermesCore.Abilities;
using Olympus.Rotation.HermesCore.Context;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Services.Training;

namespace Olympus.Rotation.HermesCore.Modules;

/// <summary>
/// Handles the Ninja damage rotation (scheduler-driven).
/// Combo GCDs, Ninki spenders, Raiju, Phantom Kamaitachi.
/// Mudras and Ninjutsu execution live in NinjutsuModule (raw ActionManager).
/// </summary>
public sealed class DamageModule : IHermesModule
{
    public int Priority => 30;
    public string Name => "Damage";

    private readonly IBurstWindowService? _burstWindowService;
    private readonly ISmartAoEService? _smartAoEService;

    public DamageModule(IBurstWindowService? burstWindowService = null, ISmartAoEService? smartAoEService = null)
    {
        _burstWindowService = burstWindowService;
        _smartAoEService = smartAoEService;
    }

    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

    private const int KazematoiLowThreshold = 1;

    public bool TryExecute(IHermesContext context, bool isMoving) => false;

    public void UpdateDebugState(IHermesContext context) { }

    public void CollectCandidates(IHermesContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.DamageState = "Not in combat";
            return;
        }
        if (context.TargetingService.IsDamageTargetingPaused())
        {
            context.Debug.DamageState = "Paused (no target)";
            return;
        }
        if (context.Configuration.Targeting.SuppressDamageOnForcedMovement
            && PlayerSafetyHelper.IsForcedMovementActive(context.Player))
        {
            context.Debug.DamageState = "Paused (forced movement)";
            return;
        }

        var player = context.Player;
        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy,
            NINActions.SpinningEdge.ActionId,
            player);
        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return;
        }

        var aoeEnabled = context.Configuration.Ninja.EnableAoERotation;
        var aoeThreshold = context.Configuration.Ninja.AoEMinTargets;
        var rawEnemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = rawEnemyCount;
        var enemyCount = aoeEnabled ? rawEnemyCount : 0;

        // oGCDs: Ninki spenders
        TryPushNinkiSpender(context, scheduler, target, enemyCount);

        // GCDs
        TryPushRaiju(context, scheduler, target);
        TryPushPhantomKamaitachi(context, scheduler, target);
        TryPushComboRotation(context, scheduler, target, enemyCount);
    }

    #region Ninki spender

    private void TryPushNinkiSpender(IHermesContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var ninkiMinGauge = context.Configuration.Ninja.NinkiMinGauge;
        var ninkiOvercapThreshold = context.Configuration.Ninja.NinkiOvercapThreshold;

        if (context.Ninki < ninkiMinGauge) return;
        if (context.Configuration.Ninja.EnableBurstPooling && ShouldHoldForBurst(8f) && context.Ninki < ninkiOvercapThreshold) return;

        var aoeThreshold = context.Configuration.Ninja.AoEMinTargets;

        if (enemyCount >= aoeThreshold && level >= NINActions.HellfrogMedium.MinLevel)
        {
            if (!context.Configuration.Ninja.EnableHellfrogMedium) return;
            var aoeAction = NINActions.GetAoeNinkiSpender((byte)level, context.HasMeisui);
            if (!context.ActionService.IsActionReady(aoeAction.ActionId)) return;
            var ability = aoeAction == NINActions.DeathfrogMedium ? HermesAbilities.DeathfrogMedium : HermesAbilities.HellfrogMedium;

            scheduler.PushOgcd(ability, target.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = aoeAction.Name;
                    context.Debug.DamageState = $"{aoeAction.Name} ({enemyCount} enemies)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(aoeAction.ActionId, aoeAction.Name)
                        .AsAoE(enemyCount).Target($"{enemyCount} enemies")
                        .Reason($"Spending 50 Ninki on {aoeAction.Name}",
                            $"{aoeAction.Name} is the AoE Ninki spender.")
                        .Factors($"Ninki >= {ninkiMinGauge}", $"{enemyCount} enemies")
                        .Alternatives("Use Bhavacakra")
                        .Tip("In AoE, prefer Hellfrog Medium at 3+ targets.")
                        .Concept(NinConcepts.NinkiGauge)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(NinConcepts.NinkiGauge, true, "AoE Ninki spending");
                });
            return;
        }

        if (level >= NINActions.HellfrogMedium.MinLevel)
        {
            if (!context.Configuration.Ninja.EnableBhavacakra) return;
            var stAction = NINActions.GetNinkiSpender((byte)level, context.HasMeisui);
            if (!context.ActionService.IsActionReady(stAction.ActionId)) return;
            var ability = stAction == NINActions.ZeshoMeppo ? HermesAbilities.ZeshoMeppo : HermesAbilities.Bhavacakra;

            scheduler.PushOgcd(ability, target.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = stAction.Name;
                    context.Debug.DamageState = stAction.Name;
                    var meisuiNote = context.HasMeisui ? " (enhanced by Meisui)" : "";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(stAction.ActionId, stAction.Name)
                        .AsMeleeResource("Ninki", context.Ninki).Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Spending 50 Ninki on {stAction.Name}{meisuiNote}",
                            $"{stAction.Name} is your primary single-target Ninki spender.")
                        .Factors($"Ninki >= {ninkiMinGauge}", context.HasMeisui ? "Meisui buff active" : "Standard potency")
                        .Alternatives("Save for Bunshin")
                        .Tip("Spend Ninki before capping. Bunshin > Bhavacakra in priority.")
                        .Concept(NinConcepts.Bhavacakra)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(NinConcepts.Bhavacakra, true, "ST Ninki spending");
                });
        }
    }

    #endregion

    #region GCDs

    private void TryPushRaiju(IHermesContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Ninja.EnableRaiju) return;
        var player = context.Player;
        if (player.Level < NINActions.ForkedRaiju.MinLevel) return;
        if (!context.HasRaijuReady) return;

        ActionDefinition action;
        AbilityBehavior ability;
        if (!DistanceHelper.IsActionInRange(NINActions.SpinningEdge.ActionId, player, target))
        {
            action = NINActions.ForkedRaiju;
            ability = HermesAbilities.ForkedRaiju;
        }
        else
        {
            action = NINActions.FleetingRaiju;
            ability = HermesAbilities.FleetingRaiju;
        }
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        scheduler.PushGcd(ability, target.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (Raiju proc)";
                var isForked = action.ActionId == NINActions.ForkedRaiju.ActionId;
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsMeleeDamage().Target(target.Name?.TextValue ?? "Target")
                    .Reason(isForked ? "Using Forked Raiju (gap closer)" : "Using Fleeting Raiju (melee)",
                        "Raiju procs come from using Raiton.")
                    .Factors("Raiju Ready proc active", $"{context.RaijuStacks} stack(s) available")
                    .Alternatives(isForked ? "Walk to target" : "Use Forked for movement")
                    .Tip("Raiju procs are free damage.")
                    .Concept(NinConcepts.RaijuProcs)
                    .Record();
                context.TrainingService?.RecordConceptApplication(NinConcepts.RaijuProcs, true, "Raiju proc usage");
            });
    }

    private void TryPushPhantomKamaitachi(IHermesContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Ninja.EnablePhantomKamaitachi) return;
        if (context.Player.Level < NINActions.PhantomKamaitachi.MinLevel) return;
        if (!context.HasPhantomKamaitachiReady) return;
        if (!context.ActionService.IsActionReady(NINActions.PhantomKamaitachi.ActionId)) return;

        scheduler.PushGcd(HermesAbilities.PhantomKamaitachi, target.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = NINActions.PhantomKamaitachi.Name;
                context.Debug.DamageState = "Phantom Kamaitachi";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(NINActions.PhantomKamaitachi.ActionId, NINActions.PhantomKamaitachi.Name)
                    .AsMeleeDamage().Target(target.Name?.TextValue ?? "Target")
                    .Reason("Using Phantom Kamaitachi (Bunshin proc)",
                        "Phantom Kamaitachi is a high-potency proc from Bunshin.")
                    .Factors("Phantom Kamaitachi Ready proc")
                    .Alternatives("Don't let proc expire")
                    .Tip("Always use Phantom Kamaitachi after Bunshin.")
                    .Concept(NinConcepts.PhantomKamaitachi)
                    .Record();
                context.TrainingService?.RecordConceptApplication(NinConcepts.PhantomKamaitachi, true, "Bunshin follow-up");
            });
    }

    private void TryPushComboRotation(IHermesContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var aoeThreshold = context.Configuration.Ninja.AoEMinTargets;
        var useAoe = enemyCount >= aoeThreshold && level >= NINActions.DeathBlossom.MinLevel;
        if (useAoe) TryPushAoeCombo(context, scheduler, target, enemyCount);
        else TryPushSingleTargetCombo(context, scheduler, target);
    }

    private void TryPushSingleTargetCombo(IHermesContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;
        var comboStep = context.ComboStep;

        if (comboStep == 2 && context.LastComboAction == NINActions.GustSlash.ActionId)
        {
            TryPushComboFinisher(context, scheduler, target);
            return;
        }

        if (comboStep == 1 && context.LastComboAction == NINActions.SpinningEdge.ActionId
            && level >= NINActions.GustSlash.MinLevel
            && context.ActionService.IsActionReady(NINActions.GustSlash.ActionId))
        {
            scheduler.PushGcd(HermesAbilities.GustSlash, target.GameObjectId, priority: 5,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = NINActions.GustSlash.Name;
                    context.Debug.DamageState = "Gust Slash (Combo 2)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(NINActions.GustSlash.ActionId, NINActions.GustSlash.Name)
                        .AsCombo(2).Target(target.Name?.TextValue ?? "Target")
                        .Reason("Gust Slash — combo step 2",
                            "Gust Slash is the second hit in NIN's ST combo.")
                        .Factors("Combo step 2 active")
                        .Alternatives("Restart with Spinning Edge (breaks combo)")
                        .Tip("Maintain your 3-step combo.")
                        .Concept(NinConcepts.ComboBasics)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(NinConcepts.ComboBasics, true, "Combo step 2");
                });
            return;
        }

        if (context.ActionService.IsActionReady(NINActions.SpinningEdge.ActionId))
        {
            scheduler.PushGcd(HermesAbilities.SpinningEdge, target.GameObjectId, priority: 6,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = NINActions.SpinningEdge.Name;
                    context.Debug.DamageState = "Spinning Edge (Combo 1)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(NINActions.SpinningEdge.ActionId, NINActions.SpinningEdge.Name)
                        .AsCombo(1).Target(target.Name?.TextValue ?? "Target")
                        .Reason("Spinning Edge — starting the 3-hit ST combo",
                            "Spinning Edge starts NIN's single-target combo.")
                        .Factors("No higher-priority GCD available")
                        .Alternatives("Use Raiju if proc is active")
                        .Tip("Always complete the full 3-step combo.")
                        .Concept(NinConcepts.ComboBasics)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(NinConcepts.ComboBasics, true, "Combo step 1");
                });
        }
    }

    private void TryPushComboFinisher(IHermesContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var level = context.Player.Level;
        bool useArmorCrush = level >= NINActions.ArmorCrush.MinLevel && context.Kazematoi <= KazematoiLowThreshold;

        if (useArmorCrush && context.ActionService.IsActionReady(NINActions.ArmorCrush.ActionId))
        {
            bool correctPositional = context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity;
            scheduler.PushGcd(HermesAbilities.ArmorCrush, target.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = NINActions.ArmorCrush.Name;
                    context.Debug.DamageState = $"Armor Crush {(correctPositional ? "(flank)" : "(WRONG)")}";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(NINActions.ArmorCrush.ActionId, NINActions.ArmorCrush.Name)
                        .AsPositional(correctPositional, "Flank").Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Armor Crush for Kazematoi stacks ({context.Kazematoi} → {context.Kazematoi + 2})",
                            "Armor Crush is a flank positional that grants 2 Kazematoi stacks.")
                        .Factors($"Kazematoi low ({context.Kazematoi})", correctPositional ? "Correct flank" : "MISSED flank")
                        .Alternatives("Use Aeolian Edge")
                        .Tip("Armor Crush builds Kazematoi.")
                        .Concept(NinConcepts.KazematoiManagement)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(NinConcepts.KazematoiManagement, correctPositional, "Flank positional");
                });
            return;
        }

        if (level >= NINActions.AeolianEdge.MinLevel && context.ActionService.IsActionReady(NINActions.AeolianEdge.ActionId))
        {
            bool correctPositional = context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity;
            scheduler.PushGcd(HermesAbilities.AeolianEdge, target.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = NINActions.AeolianEdge.Name;
                    context.Debug.DamageState = $"Aeolian Edge {(correctPositional ? "(rear)" : "(WRONG)")} +Kaze:{context.Kazematoi}";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(NINActions.AeolianEdge.ActionId, NINActions.AeolianEdge.Name)
                        .AsPositional(correctPositional, "Rear").Target(target.Name?.TextValue ?? "Target")
                        .Reason("Aeolian Edge for damage",
                            "Aeolian Edge is a rear positional and your main combo finisher.")
                        .Factors($"Kazematoi available ({context.Kazematoi})", correctPositional ? "Correct rear" : "MISSED rear")
                        .Alternatives("Use Armor Crush if low Kazematoi")
                        .Tip("Aeolian Edge is your bread-and-butter finisher.")
                        .Concept(NinConcepts.Positionals)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(NinConcepts.Positionals, correctPositional, "Rear positional");
                });
            return;
        }

        // Low-level fallback: Gust Slash
        if (context.ActionService.IsActionReady(NINActions.GustSlash.ActionId))
        {
            scheduler.PushGcd(HermesAbilities.GustSlash, target.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = NINActions.GustSlash.Name;
                    context.Debug.DamageState = "Gust Slash (no finisher)";
                });
        }
    }

    private void TryPushAoeCombo(IHermesContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        var level = context.Player.Level;
        var comboStep = context.ComboStep;

        if (comboStep == 1 && context.LastComboAction == NINActions.DeathBlossom.ActionId
            && level >= NINActions.HakkeMujinsatsu.MinLevel
            && context.ActionService.IsActionReady(NINActions.HakkeMujinsatsu.ActionId))
        {
            scheduler.PushGcd(HermesAbilities.HakkeMujinsatsu, target.GameObjectId, priority: 5,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = NINActions.HakkeMujinsatsu.Name;
                    context.Debug.DamageState = "Hakke Mujinsatsu (AoE 2)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(NINActions.HakkeMujinsatsu.ActionId, NINActions.HakkeMujinsatsu.Name)
                        .AsAoE(enemyCount).Target($"{enemyCount} enemies")
                        .Reason("Hakke Mujinsatsu — AoE combo step 2",
                            "Follows Death Blossom in NIN's 2-hit AoE combo.")
                        .Factors($"{enemyCount} enemies nearby")
                        .Alternatives("Single-target combo (fewer enemies)")
                        .Tip("Stick to AoE combo at 3+ targets.")
                        .Concept(NinConcepts.AoeCombo)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(NinConcepts.AoeCombo, true, "AoE combo step 2");
                });
            return;
        }

        if (context.ActionService.IsActionReady(NINActions.DeathBlossom.ActionId))
        {
            scheduler.PushGcd(HermesAbilities.DeathBlossom, target.GameObjectId, priority: 6,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = NINActions.DeathBlossom.Name;
                    context.Debug.DamageState = "Death Blossom (AoE 1)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(NINActions.DeathBlossom.ActionId, NINActions.DeathBlossom.Name)
                        .AsAoE(enemyCount).Target($"{enemyCount} enemies")
                        .Reason("Death Blossom — starting AoE combo",
                            "Death Blossom is NIN's AoE combo starter.")
                        .Factors($"{enemyCount} enemies nearby")
                        .Alternatives("Use Spinning Edge (1-2 targets)")
                        .Tip("Switch to AoE combo at 3+ enemies.")
                        .Concept(NinConcepts.AoeCombo)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(NinConcepts.AoeCombo, true, "AoE combo step 1");
                });
        }
    }

    #endregion
}
