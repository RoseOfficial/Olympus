using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.EchidnaCore.Abilities;
using Olympus.Rotation.EchidnaCore.Context;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Services.Training;

namespace Olympus.Rotation.EchidnaCore.Modules;

/// <summary>
/// Handles the Viper damage rotation (scheduler-driven).
/// Manages Reawaken sequences, twinblade combos, dual wield combos, resource building.
/// </summary>
public sealed class DamageModule : IEchidnaModule
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

    public bool TryExecute(IEchidnaContext context, bool isMoving) => false;

    public void UpdateDebugState(IEchidnaContext context) { }

    public void CollectCandidates(IEchidnaContext context, RotationScheduler scheduler, bool isMoving)
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
            VPRActions.SteelFangs.ActionId,
            player);
        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return;
        }

        var aoeEnabled = context.Configuration.Viper.EnableAoERotation;
        var aoeThreshold = context.Configuration.Viper.AoEMinTargets;
        var rawEnemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = rawEnemyCount;
        var enemyCount = aoeEnabled ? rawEnemyCount : 0;
        var useAoe = enemyCount >= aoeThreshold;

        // oGCDs
        TryPushLegacyOgcd(context, scheduler, target);
        TryPushPoisedOgcd(context, scheduler, target, useAoe);
        TryPushUncoiledOgcd(context, scheduler, target);
        TryPushRoleActions(context, scheduler, target);

        // GCDs (priority order)
        if (context.IsReawakened)
            TryPushReawakenGcd(context, scheduler, target);
        TryPushReawaken(context, scheduler, target);
        TryPushTwinbladeCombo(context, scheduler, target, enemyCount);
        TryPushUncoiledFury(context, scheduler, target, false /* TODO isMoving */);
        TryPushVicewinder(context, scheduler, target, useAoe, forceUse: false);
        if (ShouldRefreshNoxiousGnash(context))
            TryPushVicewinder(context, scheduler, target, useAoe, forceUse: true);
        if (useAoe) TryPushAoeDualWieldCombo(context, scheduler, target, enemyCount);
        else TryPushSingleTargetDualWieldCombo(context, scheduler, target);
        TryPushWrithingSnap(context, scheduler, target, false /* TODO isMoving */);
    }

    #region oGCDs

    private void TryPushLegacyOgcd(IEchidnaContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var action = VPRActions.GetLegacyOgcd(context.SerpentCombo);
        if (action == null) return;
        var level = context.Player.Level;
        if (level < action.MinLevel) return;
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        var ability = action.ActionId switch
        {
            var id when id == VPRActions.FirstLegacy.ActionId => EchidnaAbilities.FirstLegacy,
            var id when id == VPRActions.SecondLegacy.ActionId => EchidnaAbilities.SecondLegacy,
            var id when id == VPRActions.ThirdLegacy.ActionId => EchidnaAbilities.ThirdLegacy,
            var id when id == VPRActions.FourthLegacy.ActionId => EchidnaAbilities.FourthLegacy,
            _ => EchidnaAbilities.FirstLegacy,
        };

        scheduler.PushOgcd(ability, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = action.Name;

                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsMeleeDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Using {action.Name} (SerpentCombo follow-up)",
                        "Legacy oGCDs are weaved between Generation GCDs during Reawaken. " +
                        "Death Rattle fires after ST finishers; Last Lash fires after AoE finishers.")
                    .Factors(new[] { $"SerpentCombo: {context.SerpentCombo}" })
                    .Alternatives(new[] { "No reason to hold" })
                    .Tip("Always weave the SerpentCombo follow-up immediately when available.")
                    .Concept("vpr.generation_sequence")
                    .Record();
                context.TrainingService?.RecordConceptApplication("vpr.generation_sequence", true, "SerpentCombo oGCD");
            });
    }

    private void TryPushPoisedOgcd(IEchidnaContext context, RotationScheduler scheduler, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        if (context.HasPoisedForTwinfang)
        {
            var action = useAoe ? VPRActions.TwinfangBite : VPRActions.Twinfang;
            var ability = useAoe ? EchidnaAbilities.TwinfangBite : EchidnaAbilities.Twinfang;
            if (level >= action.MinLevel && context.ActionService.IsActionReady(action.ActionId))
            {
                scheduler.PushOgcd(ability, target.GameObjectId, priority: 2,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = action.Name;
                        context.Debug.DamageState = action.Name;

                        TrainingHelper.Decision(context.TrainingService)
                            .Action(action.ActionId, action.Name)
                            .AsMeleeDamage()
                            .Target(target.Name?.TextValue ?? "Target")
                            .Reason($"Using {action.Name} (Poised for Twinfang proc)",
                                "Twinfang/TwinfangBite are oGCDs granted by using Hunter's Coil/Den in twinblade combos. " +
                                "Weave immediately when the proc appears for maximum damage.")
                            .Factors(new[] { "Poised for Twinfang active", "oGCD window available" })
                            .Alternatives(new[] { "No reason to hold" })
                            .Tip("Always weave Twinfang immediately when Poised for Twinfang is active.")
                            .Concept("vpr.twinfang_twinblood")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("vpr.twinfang_twinblood", true, "Twinblade oGCD");
                    });
            }
        }

        if (context.HasPoisedForTwinblood)
        {
            var action = useAoe ? VPRActions.TwinbloodBite : VPRActions.Twinblood;
            var ability = useAoe ? EchidnaAbilities.TwinbloodBite : EchidnaAbilities.Twinblood;
            if (level >= action.MinLevel && context.ActionService.IsActionReady(action.ActionId))
            {
                scheduler.PushOgcd(ability, target.GameObjectId, priority: 2,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = action.Name;
                        context.Debug.DamageState = action.Name;

                        TrainingHelper.Decision(context.TrainingService)
                            .Action(action.ActionId, action.Name)
                            .AsMeleeDamage()
                            .Target(target.Name?.TextValue ?? "Target")
                            .Reason($"Using {action.Name} (Poised for Twinblood proc)",
                                "Twinblood/TwinbloodBite are oGCDs granted by using Swiftskin's Coil/Den in twinblade combos. " +
                                "Weave immediately when the proc appears for maximum damage.")
                            .Factors(new[] { "Poised for Twinblood active", "oGCD window available" })
                            .Alternatives(new[] { "No reason to hold" })
                            .Tip("Always weave Twinblood immediately when Poised for Twinblood is active.")
                            .Concept("vpr.twinfang_twinblood")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("vpr.twinfang_twinblood", true, "Twinblade oGCD");
                    });
            }
        }
    }

    private void TryPushUncoiledOgcd(IEchidnaContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var level = context.Player.Level;

        if (level >= VPRActions.UncoiledTwinfang.MinLevel
            && context.ActionService.IsActionReady(VPRActions.UncoiledTwinfang.ActionId))
        {
            scheduler.PushOgcd(EchidnaAbilities.UncoiledTwinfang, target.GameObjectId, priority: 3,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = VPRActions.UncoiledTwinfang.Name;
                    context.Debug.DamageState = "Uncoiled Twinfang";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(VPRActions.UncoiledTwinfang.ActionId, VPRActions.UncoiledTwinfang.Name)
                        .AsMeleeDamage()
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason("Using Uncoiled Twinfang (Uncoiled Fury follow-up)",
                            "Uncoiled Twinfang is the first oGCD follow-up after Uncoiled Fury. " +
                            "Weave immediately for bonus damage during the Uncoiled Fury sequence.")
                        .Factors(new[] { "Uncoiled Fury active", "oGCD window available" })
                        .Alternatives(new[] { "No reason to hold" })
                        .Tip("After Uncoiled Fury, weave Twinfang then Twinblood for the full sequence.")
                        .Concept("vpr.uncoiled_fury")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("vpr.uncoiled_fury", true, "Uncoiled follow-up");
                });
        }

        if (level >= VPRActions.UncoiledTwinblood.MinLevel
            && context.ActionService.IsActionReady(VPRActions.UncoiledTwinblood.ActionId))
        {
            scheduler.PushOgcd(EchidnaAbilities.UncoiledTwinblood, target.GameObjectId, priority: 3,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = VPRActions.UncoiledTwinblood.Name;
                    context.Debug.DamageState = "Uncoiled Twinblood";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(VPRActions.UncoiledTwinblood.ActionId, VPRActions.UncoiledTwinblood.Name)
                        .AsMeleeDamage()
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason("Using Uncoiled Twinblood (Uncoiled Fury follow-up)",
                            "Uncoiled Twinblood is the second oGCD follow-up after Uncoiled Fury. " +
                            "Completes the Uncoiled Fury sequence for maximum damage.")
                        .Factors(new[] { "Uncoiled Twinfang used", "oGCD window available" })
                        .Alternatives(new[] { "No reason to hold" })
                        .Tip("Always complete the full Uncoiled Fury → Twinfang → Twinblood sequence.")
                        .Concept("vpr.uncoiled_fury")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("vpr.uncoiled_fury", true, "Uncoiled follow-up");
                });
        }
    }

    private void TryPushRoleActions(IEchidnaContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;
        var hpPercent = player.MaxHp > 0 ? (float)player.CurrentHp / player.MaxHp : 1f;

        if (level >= RoleActions.SecondWind.MinLevel
            && context.Configuration.Viper.EnableSecondWind
            && hpPercent < context.Configuration.Viper.SecondWindHpThreshold
            && context.ActionService.IsActionReady(RoleActions.SecondWind.ActionId))
        {
            scheduler.PushOgcd(EchidnaAbilities.SecondWind, player.GameObjectId, priority: 6,
                onDispatched: _ => context.Debug.PlannedAction = RoleActions.SecondWind.Name);
        }

        if (level >= RoleActions.Bloodbath.MinLevel
            && context.Configuration.Viper.EnableBloodbath
            && hpPercent < context.Configuration.Viper.BloodbathHpThreshold
            && !context.StatusHelper.HasBloodbath(player)
            && context.ActionService.IsActionReady(RoleActions.Bloodbath.ActionId))
        {
            scheduler.PushOgcd(EchidnaAbilities.Bloodbath, player.GameObjectId, priority: 6,
                onDispatched: _ => context.Debug.PlannedAction = RoleActions.Bloodbath.Name);
        }

        if (level >= RoleActions.Feint.MinLevel
            && context.Configuration.Viper.EnableFeint
            && context.ActionService.IsActionReady(RoleActions.Feint.ActionId))
        {
            var partyCoord = context.PartyCoordinationService;
            var coordConfig = context.Configuration.PartyCoordination;
            var feintBlockedByRemote = coordConfig.EnableCooldownCoordination &&
                partyCoord?.WasActionUsedByOther(RoleActions.Feint.ActionId, 15f) == true;

            if (!feintBlockedByRemote)
            {
                scheduler.PushOgcd(EchidnaAbilities.Feint, target.GameObjectId, priority: 6,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = RoleActions.Feint.Name;
                        partyCoord?.OnCooldownUsed(RoleActions.Feint.ActionId, 90_000);
                    });
            }
        }

        if (level >= RoleActions.TrueNorth.MinLevel
            && context.Configuration.Viper.EnableTrueNorth
            && !context.HasTrueNorth
            && !context.IsAtRear && !context.IsAtFlank
            && !context.TargetHasPositionalImmunity
            && context.ActionService.IsActionReady(RoleActions.TrueNorth.ActionId))
        {
            scheduler.PushOgcd(EchidnaAbilities.TrueNorth, player.GameObjectId, priority: 6,
                onDispatched: _ => context.Debug.PlannedAction = RoleActions.TrueNorth.Name);
        }
    }

    #endregion

    #region GCDs

    private void TryPushReawaken(IEchidnaContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Viper.EnableReawaken) return;
        var player = context.Player;
        if (player.Level < VPRActions.Reawaken.MinLevel) return;
        if (context.IsReawakened) return;
        if (context.SerpentOffering < context.Configuration.Viper.AnguineMinStacks && !context.HasReadyToReawaken) return;
        if (!context.Configuration.Viper.UseReawakenDuringBurst && _burstWindowService?.IsInBurstWindow == true && !context.HasReadyToReawaken) return;
        if (context.Configuration.Viper.EnableBurstPooling && context.Configuration.Viper.SaveAnguineForBurst && ShouldHoldForBurst(8f) && !context.HasReadyToReawaken) return;
        if (!context.HasHuntersInstinct || context.HuntersInstinctRemaining < 10f) return;
        if (!context.HasSwiftscaled || context.SwiftscaledRemaining < 10f) return;
        if (!context.HasNoxiousGnash || context.NoxiousGnashRemaining < 10f) return;
        if (!context.ActionService.IsActionReady(VPRActions.Reawaken.ActionId)) return;

        scheduler.PushGcd(EchidnaAbilities.Reawaken, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = VPRActions.Reawaken.Name;
                context.Debug.DamageState = "Entering Reawaken";

                var entryReason = context.HasReadyToReawaken ? "Ready to Reawaken proc (free entry)" :
                                  $"Serpent Offering: {context.SerpentOffering}/50";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(VPRActions.Reawaken.ActionId, VPRActions.Reawaken.Name)
                    .AsMeleeBurst()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Entering Reawaken ({entryReason})",
                        "Reawaken is VPR's burst phase. Grants 5 Anguine Tribute stacks for Generation GCDs. " +
                        "Each Generation grants a Legacy oGCD for weaving. Finish with Ouroboros.")
                    .Factors(new[] { entryReason, $"Hunter's Instinct: {context.HuntersInstinctRemaining:F1}s", $"Swiftscaled: {context.SwiftscaledRemaining:F1}s", $"Noxious Gnash: {context.NoxiousGnashRemaining:F1}s" })
                    .Alternatives(new[] { "Wait for buff refresh", "Wait for Serpent's Ire" })
                    .Tip("Enter Reawaken with good buff duration. Use after Serpent's Ire for Ready to Reawaken proc.")
                    .Concept("vpr.reawaken_entry")
                    .Record();
                context.TrainingService?.RecordConceptApplication("vpr.reawaken_entry", true, "Burst phase entry");
            });
    }

    private void TryPushReawakenGcd(IEchidnaContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Viper.EnableGenerationAbilities) return;
        var level = context.Player.Level;

        var action = VPRActions.GetGenerationGcd(context.AnguineTribute);
        var isOuroboros = false;
        if (context.AnguineTribute == 1 && level >= VPRActions.Ouroboros.MinLevel)
        {
            if (!context.Configuration.Viper.EnableOuroboros) return;
            action = VPRActions.Ouroboros;
            isOuroboros = true;
        }
        if (level < action.MinLevel) return;
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        var ability = action.ActionId switch
        {
            var id when id == VPRActions.FirstGeneration.ActionId => EchidnaAbilities.FirstGeneration,
            var id when id == VPRActions.SecondGeneration.ActionId => EchidnaAbilities.SecondGeneration,
            var id when id == VPRActions.ThirdGeneration.ActionId => EchidnaAbilities.ThirdGeneration,
            var id when id == VPRActions.FourthGeneration.ActionId => EchidnaAbilities.FourthGeneration,
            var id when id == VPRActions.Ouroboros.ActionId => EchidnaAbilities.Ouroboros,
            _ => EchidnaAbilities.FirstGeneration,
        };

        scheduler.PushGcd(ability, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (Tribute: {context.AnguineTribute})";

                if (isOuroboros)
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(action.ActionId, action.Name)
                        .AsMeleeDamage()
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason("Using Ouroboros (Reawaken finisher)",
                            "Ouroboros is the powerful finisher that ends the Reawaken phase. " +
                            "Use at 1 Anguine Tribute remaining after all Generation GCDs.")
                        .Factors(new[] { "1 Anguine Tribute remaining", "Ending Reawaken phase" })
                        .Alternatives(new[] { "Use more Generations first" })
                        .Tip("Ouroboros ends Reawaken. Make sure you've used all 4 Generation GCDs first.")
                        .Concept("vpr.generation_sequence")
                        .Record();
                }
                else
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(action.ActionId, action.Name)
                        .AsMeleeDamage()
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Using {action.Name} (Anguine Tribute: {context.AnguineTribute})",
                            "Generation GCDs are your Reawaken burst rotation. Each consumes 1 Anguine Tribute " +
                            "and grants a Legacy oGCD for weaving. Execute all 4 Generations before Ouroboros.")
                        .Factors(new[] { $"Anguine Tribute: {context.AnguineTribute}", "Reawaken active" })
                        .Alternatives(new[] { "No reason to hold during Reawaken" })
                        .Tip("Weave Legacy oGCDs between Generation GCDs for maximum burst damage.")
                        .Concept("vpr.generation_sequence")
                        .Record();
                }
                context.TrainingService?.RecordConceptApplication("vpr.generation_sequence", true,
                    isOuroboros ? "Reawaken finisher" : "Reawaken GCD");
            });
    }

    private void TryPushTwinbladeCombo(IEchidnaContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        if (!context.Configuration.Viper.EnableTwinbladeCombo) return;
        var level = context.Player.Level;

        switch (context.DreadCombo)
        {
            case VPRActions.DreadCombo.DreadwindyReady:
            case VPRActions.DreadCombo.HunterCoilReady:
                if (level >= VPRActions.HuntersCoil.MinLevel
                    && context.ActionService.IsActionReady(VPRActions.HuntersCoil.ActionId))
                {
                    scheduler.PushGcd(EchidnaAbilities.HuntersCoil, target.GameObjectId, priority: 2,
                        onDispatched: _ =>
                        {
                            context.Debug.PlannedAction = VPRActions.HuntersCoil.Name;
                            context.Debug.DamageState = "Hunter's Coil (Twinblade)";

                            TrainingHelper.Decision(context.TrainingService)
                                .Action(VPRActions.HuntersCoil.ActionId, VPRActions.HuntersCoil.Name)
                                .AsMeleeDamage()
                                .Target(target.Name?.TextValue ?? "Target")
                                .Reason("Using Hunter's Coil (Twinblade combo)",
                                    "Hunter's Coil is part of the twinblade combo after Vicewinder. " +
                                    "Grants Poised for Twinfang for an oGCD weave opportunity.")
                                .Factors(new[] { $"DreadCombo: {context.DreadCombo}", "Twinblade combo active" })
                                .Alternatives(new[] { "Use Swiftskin's Coil instead" })
                                .Tip("Hunter's Coil grants Twinfang proc. Weave it before your next GCD.")
                                .Concept("vpr.dread_combo")
                                .Record();
                            context.TrainingService?.RecordConceptApplication("vpr.dread_combo", true, "Twinblade combo");
                        });
                    return;
                }
                if (level >= VPRActions.SwiftskinsCoil.MinLevel
                    && context.ActionService.IsActionReady(VPRActions.SwiftskinsCoil.ActionId))
                {
                    scheduler.PushGcd(EchidnaAbilities.SwiftskinsCoil, target.GameObjectId, priority: 2,
                        onDispatched: _ =>
                        {
                            context.Debug.PlannedAction = VPRActions.SwiftskinsCoil.Name;
                            context.Debug.DamageState = "Swiftskin's Coil (Twinblade)";
                            TrainingHelper.Decision(context.TrainingService)
                                .Action(VPRActions.SwiftskinsCoil.ActionId, VPRActions.SwiftskinsCoil.Name)
                                .AsMeleeDamage()
                                .Target(target.Name?.TextValue ?? "Target")
                                .Reason("Using Swiftskin's Coil (Twinblade combo)",
                                    "Swiftskin's Coil is part of the twinblade combo after Vicewinder. " +
                                    "Grants Poised for Twinblood for an oGCD weave opportunity.")
                                .Factors(new[] { $"DreadCombo: {context.DreadCombo}", "Twinblade combo active" })
                                .Alternatives(new[] { "Use Hunter's Coil instead" })
                                .Tip("Swiftskin's Coil grants Twinblood proc. Weave it before your next GCD.")
                                .Concept("vpr.dread_combo")
                                .Record();
                            context.TrainingService?.RecordConceptApplication("vpr.dread_combo", true, "Twinblade combo");
                        });
                }
                break;

            case VPRActions.DreadCombo.SwiftskinCoilReady:
                if (level >= VPRActions.SwiftskinsCoil.MinLevel
                    && context.ActionService.IsActionReady(VPRActions.SwiftskinsCoil.ActionId))
                {
                    scheduler.PushGcd(EchidnaAbilities.SwiftskinsCoil, target.GameObjectId, priority: 2,
                        onDispatched: _ =>
                        {
                            context.Debug.PlannedAction = VPRActions.SwiftskinsCoil.Name;
                            context.Debug.DamageState = "Swiftskin's Coil (Twinblade)";
                            TrainingHelper.Decision(context.TrainingService)
                                .Action(VPRActions.SwiftskinsCoil.ActionId, VPRActions.SwiftskinsCoil.Name)
                                .AsMeleeDamage()
                                .Target(target.Name?.TextValue ?? "Target")
                                .Reason("Using Swiftskin's Coil (Twinblade combo continuation)",
                                    "Swiftskin's Coil completes the twinblade combo. " +
                                    "Grants Poised for Twinblood for an oGCD weave opportunity.")
                                .Factors(new[] { "SwiftskinCoilReady state", "Continuing twinblade combo" })
                                .Alternatives(new[] { "No alternatives - continue combo" })
                                .Tip("Complete the twinblade combo and weave the Twinblood proc.")
                                .Concept("vpr.dread_combo")
                                .Record();
                            context.TrainingService?.RecordConceptApplication("vpr.dread_combo", true, "Twinblade combo");
                        });
                }
                break;

            case VPRActions.DreadCombo.PitReady:
            case VPRActions.DreadCombo.HunterDenReady:
                if (level >= VPRActions.HuntersDen.MinLevel
                    && context.ActionService.IsActionReady(VPRActions.HuntersDen.ActionId))
                {
                    scheduler.PushGcd(EchidnaAbilities.HuntersDen, context.Player.GameObjectId, priority: 2,
                        onDispatched: _ =>
                        {
                            context.Debug.PlannedAction = VPRActions.HuntersDen.Name;
                            context.Debug.DamageState = "Hunter's Den (AoE Twinblade)";
                            TrainingHelper.Decision(context.TrainingService)
                                .Action(VPRActions.HuntersDen.ActionId, VPRActions.HuntersDen.Name)
                                .AsAoE(enemyCount)
                                .Reason("Using Hunter's Den (AoE Twinblade combo)",
                                    "Hunter's Den is the AoE version of Hunter's Coil. " +
                                    "Use in AoE situations after Vicepit. Grants Poised for TwinfangBite.")
                                .Factors(new[] { $"Enemies: {enemyCount}", "AoE twinblade combo" })
                                .Alternatives(new[] { "Use Swiftskin's Den instead" })
                                .Tip("Hunter's Den grants TwinfangBite proc for AoE oGCD damage.")
                                .Concept("vpr.dread_combo")
                                .Record();
                            context.TrainingService?.RecordConceptApplication("vpr.dread_combo", true, "AoE Twinblade");
                        });
                }
                break;

            case VPRActions.DreadCombo.SwiftskinDenReady:
                if (level >= VPRActions.SwiftskinsDen.MinLevel
                    && context.ActionService.IsActionReady(VPRActions.SwiftskinsDen.ActionId))
                {
                    scheduler.PushGcd(EchidnaAbilities.SwiftskinsDen, context.Player.GameObjectId, priority: 2,
                        onDispatched: _ =>
                        {
                            context.Debug.PlannedAction = VPRActions.SwiftskinsDen.Name;
                            context.Debug.DamageState = "Swiftskin's Den (AoE Twinblade)";
                            TrainingHelper.Decision(context.TrainingService)
                                .Action(VPRActions.SwiftskinsDen.ActionId, VPRActions.SwiftskinsDen.Name)
                                .AsAoE(enemyCount)
                                .Reason("Using Swiftskin's Den (AoE Twinblade combo)",
                                    "Swiftskin's Den completes the AoE twinblade combo. " +
                                    "Grants Poised for TwinbloodBite for AoE oGCD damage.")
                                .Factors(new[] { $"Enemies: {enemyCount}", "AoE twinblade continuation" })
                                .Alternatives(new[] { "No alternatives - continue combo" })
                                .Tip("Complete the AoE twinblade combo and weave the TwinbloodBite proc.")
                                .Concept("vpr.dread_combo")
                                .Record();
                            context.TrainingService?.RecordConceptApplication("vpr.dread_combo", true, "AoE Twinblade");
                        });
                }
                break;
        }
    }

    private void TryPushVicewinder(IEchidnaContext context, RotationScheduler scheduler, IBattleChara target, bool useAoe, bool forceUse)
    {
        var level = context.Player.Level;
        if (context.DreadCombo != VPRActions.DreadCombo.None && !forceUse) return;

        if (useAoe && level >= VPRActions.Vicepit.MinLevel
            && context.ActionService.IsActionReady(VPRActions.Vicepit.ActionId))
        {
            scheduler.PushGcd(EchidnaAbilities.Vicepit, context.Player.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = VPRActions.Vicepit.Name;
                    context.Debug.DamageState = "Vicepit (AoE Twinblade start)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(VPRActions.Vicepit.ActionId, VPRActions.Vicepit.Name)
                        .AsAoE(0)
                        .Reason("Using Vicepit (AoE Twinblade starter)",
                            "Vicepit starts the AoE twinblade combo. Applies Noxious Gnash to targets " +
                            "and grants +1 Rattling Coil stack. Follow with Hunter's Den / Swiftskin's Den.")
                        .Factors(new[] { $"Rattling Coils: {context.RattlingCoils}" })
                        .Alternatives(new[] { "Use Vicewinder for ST", "Continue dual wield combo" })
                        .Tip("Vicepit applies Noxious Gnash and builds Rattling Coils for Uncoiled Fury.")
                        .Concept("vpr.vicewinder")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("vpr.vicewinder", true, "AoE Twinblade starter");
                    context.TrainingService?.RecordConceptApplication("vpr.noxious_gnash", true, "Debuff application");
                });
            return;
        }

        if (level >= VPRActions.Vicewinder.MinLevel
            && context.ActionService.IsActionReady(VPRActions.Vicewinder.ActionId))
        {
            scheduler.PushGcd(EchidnaAbilities.Vicewinder, target.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = VPRActions.Vicewinder.Name;
                    context.Debug.DamageState = "Vicewinder (Twinblade start)";
                    var reason = forceUse ? "Refreshing Noxious Gnash" : "Starting twinblade combo";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(VPRActions.Vicewinder.ActionId, VPRActions.Vicewinder.Name)
                        .AsMeleeDamage()
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Using Vicewinder ({reason})",
                            "Vicewinder starts the twinblade combo. Applies/refreshes Noxious Gnash (+10% damage debuff) " +
                            "and grants +1 Rattling Coil stack. Follow with Hunter's Coil / Swiftskin's Coil.")
                        .Factors(new[] { $"Noxious Gnash: {(context.HasNoxiousGnash ? $"{context.NoxiousGnashRemaining:F1}s" : "Not applied")}", $"Rattling Coils: {context.RattlingCoils}" })
                        .Alternatives(new[] { "Continue dual wield combo", "Wait for charge" })
                        .Tip("Vicewinder is your main Noxious Gnash applicator. Keep the debuff active at all times.")
                        .Concept("vpr.vicewinder")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("vpr.vicewinder", true, "Twinblade starter");
                    context.TrainingService?.RecordConceptApplication("vpr.noxious_gnash", true, "Debuff application");
                });
        }
    }

    private void TryPushUncoiledFury(IEchidnaContext context, RotationScheduler scheduler, IBattleChara target, bool isMoving)
    {
        if (!context.Configuration.Viper.EnableUncoiledFury) return;
        var player = context.Player;
        if (player.Level < VPRActions.UncoiledFury.MinLevel) return;
        if (context.RattlingCoils <= 0) return;
        if (context.IsReawakened) return;

        var rattlingCoilMax = context.Configuration.Viper.RattlingCoilMinStacks;
        bool shouldUse = !DistanceHelper.IsActionInRange(VPRActions.SteelFangs.ActionId, player, target)
                         || context.RattlingCoils >= rattlingCoilMax
                         || isMoving;
        if (!shouldUse) return;
        if (context.Configuration.Viper.EnableBurstPooling && context.Configuration.Viper.SaveRattlingCoilForBurst
            && ShouldHoldForBurst(8f) && context.RattlingCoils < rattlingCoilMax) return;
        if (!context.ActionService.IsActionReady(VPRActions.UncoiledFury.ActionId)) return;

        scheduler.PushGcd(EchidnaAbilities.UncoiledFury, target.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = VPRActions.UncoiledFury.Name;
                context.Debug.DamageState = $"Uncoiled Fury (Coils: {context.RattlingCoils})";
                var reason = context.RattlingCoils >= 3 ? "Coils capped (prevent overcap)"
                           : isMoving ? "Movement GCD"
                           : "Ranged GCD option";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(VPRActions.UncoiledFury.ActionId, VPRActions.UncoiledFury.Name)
                    .AsMeleeResource("Rattling Coils", context.RattlingCoils)
                    .Reason($"Using Uncoiled Fury ({reason})",
                        "Uncoiled Fury consumes 1 Rattling Coil for a ranged GCD. Use during movement, " +
                        "at range, or when capped on coils. Follow with Uncoiled Twinfang → Twinblood.")
                    .Factors(new[] { $"Rattling Coils: {context.RattlingCoils}", reason })
                    .Alternatives(new[] { "Save for movement", "Use melee GCDs when in range" })
                    .Tip("Uncoiled Fury is your movement tool. Save coils for forced disengages when possible.")
                    .Concept("vpr.rattling_coil")
                    .Record();
                context.TrainingService?.RecordConceptApplication("vpr.rattling_coil", true, "Coil spending");
            });
    }

    private void TryPushSingleTargetDualWieldCombo(IEchidnaContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var level = context.Player.Level;
        ActionDefinition action;
        AbilityBehavior ability;
        bool isPositional = false;

        if (context.ComboStep == 2)
        {
            if (context.LastComboAction == VPRActions.HuntersSting.ActionId)
            {
                var maintainVenoms = context.Configuration.Viper.MaintainVenoms;
                var optimizePositionals = context.Configuration.Viper.OptimizeVenomPositionals;
                if (maintainVenoms && optimizePositionals && context.HasHindstungVenom)
                    { action = VPRActions.HindstingStrike; ability = EchidnaAbilities.HindstingStrike; }
                else if (maintainVenoms && context.HasFlankstungVenom)
                    { action = VPRActions.FlankstingStrike; ability = EchidnaAbilities.FlankstingStrike; }
                else
                    { action = VPRActions.FlankstingStrike; ability = EchidnaAbilities.FlankstingStrike; }
                isPositional = true;
            }
            else if (context.LastComboAction == VPRActions.SwiftskinsString.ActionId)
            {
                var maintainVenoms = context.Configuration.Viper.MaintainVenoms;
                var optimizePositionals = context.Configuration.Viper.OptimizeVenomPositionals;
                if (maintainVenoms && optimizePositionals && context.HasHindsbaneVenom)
                    { action = VPRActions.HindsbaneFang; ability = EchidnaAbilities.HindsbaneFang; }
                else if (maintainVenoms && context.HasFlanksbaneVenom)
                    { action = VPRActions.FlanksbaneFang; ability = EchidnaAbilities.FlanksbaneFang; }
                else
                    { action = VPRActions.FlanksbaneFang; ability = EchidnaAbilities.FlanksbaneFang; }
                isPositional = true;
            }
            else { action = GetStarterAction(context); ability = MapStarter(action); }
        }
        else if (context.ComboStep == 1)
        {
            if (context.LastComboAction == VPRActions.SteelFangs.ActionId) { action = VPRActions.HuntersSting; ability = EchidnaAbilities.HuntersSting; }
            else if (context.LastComboAction == VPRActions.ReavingFangs.ActionId) { action = VPRActions.SwiftskinsString; ability = EchidnaAbilities.SwiftskinsString; }
            else { action = GetStarterAction(context); ability = MapStarter(action); }
        }
        else
        {
            action = GetStarterAction(context);
            ability = MapStarter(action);
        }

        if (level < action.MinLevel) { action = VPRActions.SteelFangs; ability = EchidnaAbilities.SteelFangs; }
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        if (isPositional && context.Configuration.Viper.EnforcePositionals)
        {
            var isRearFinisher = action == VPRActions.HindstingStrike || action == VPRActions.HindsbaneFang;
            bool positionalOk = isRearFinisher
                ? (context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity)
                : (context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity);
            if (!positionalOk && !context.Configuration.Viper.AllowPositionalLoss) return;
        }

        var actionRef = action;
        var abilityRef = ability;
        var positionalRef = isPositional;
        scheduler.PushGcd(abilityRef, target.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = actionRef.Name;
                context.Debug.DamageState = $"{actionRef.Name} (combo {context.ComboStep + 1})";

                if (positionalRef)
                {
                    var isRear = actionRef == VPRActions.HindstingStrike || actionRef == VPRActions.HindsbaneFang;
                    var positionalName = isRear ? "rear" : "flank";
                    var hitPositional = isRear ? context.IsAtRear : context.IsAtFlank;
                    hitPositional = hitPositional || context.HasTrueNorth || context.TargetHasPositionalImmunity;

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(actionRef.ActionId, actionRef.Name)
                        .AsPositional(hitPositional, positionalName)
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Using {actionRef.Name} (dual wield finisher)",
                            $"{actionRef.Name} is a {positionalName} positional finisher. Venom buffs indicate which positional " +
                            "to use: Hindstung/Hindsbane = rear, Flankstung/Flanksbane = flank.")
                        .Factors(new[] { $"Combo step: {context.ComboStep + 1}", hitPositional ? "Positional hit" : "Positional missed", $"Positional: {positionalName}" })
                        .Alternatives(new[] { "Use other finisher for different positional" })
                        .Tip("Follow venom buffs to know which positional is required. Use True North if out of position.")
                        .Concept("vpr.positionals")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("vpr.positionals", hitPositional, "Dual wield positional");
                }
                else if (context.ComboStep == 1)
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(actionRef.ActionId, actionRef.Name)
                        .AsCombo(context.ComboStep + 1)
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Using {actionRef.Name} (combo continuation)",
                            $"{actionRef.Name} continues the dual wield combo and maintains your buffs. " +
                            "Hunter's Sting path = Hunter's Instinct, Swiftskin's Sting path = Swiftscaled.")
                        .Factors(new[] { $"Combo step: {context.ComboStep + 1}", "Continuing combo" })
                        .Alternatives(new[] { "Use other path for different buff" })
                        .Tip("Alternate between Steel/Reaving paths to maintain both Hunter's Instinct and Swiftscaled.")
                        .Concept("vpr.buff_cycling")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("vpr.buff_cycling", true, "Buff maintenance");
                }
                else
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(actionRef.ActionId, actionRef.Name)
                        .AsCombo(context.ComboStep + 1)
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Using {actionRef.Name} (combo starter)",
                            $"{actionRef.Name} starts the dual wield combo. Steel path refreshes Hunter's Instinct, " +
                            "Reaving path refreshes Swiftscaled. Enhanced versions (Honed Steel/Reavers) deal more damage.")
                        .Factors(new[] { $"Honed Steel: {context.HasHonedSteel}", $"Honed Reavers: {context.HasHonedReavers}" })
                        .Alternatives(new[] { "Use other starter for different buff path" })
                        .Tip("Start with the enhanced version when available. Prioritize the buff with shorter duration.")
                        .Concept("vpr.combo_basics")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("vpr.combo_basics", true, "Combo starter");
                }
            });
    }

    private void TryPushAoeDualWieldCombo(IEchidnaContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        var level = context.Player.Level;
        ActionDefinition action;
        AbilityBehavior ability;

        if (context.ComboStep == 2)
        {
            if (context.LastComboAction == VPRActions.HuntersBite.ActionId) { action = VPRActions.JaggedMaw; ability = EchidnaAbilities.JaggedMaw; }
            else if (context.LastComboAction == VPRActions.SwiftskinsBite.ActionId) { action = VPRActions.BloodiedMaw; ability = EchidnaAbilities.BloodiedMaw; }
            else { action = GetAoeStarterAction(context); ability = MapAoeStarter(action); }
        }
        else if (context.ComboStep == 1)
        {
            if (context.LastComboAction == VPRActions.SteelMaw.ActionId) { action = VPRActions.HuntersBite; ability = EchidnaAbilities.HuntersBite; }
            else if (context.LastComboAction == VPRActions.ReavingMaw.ActionId) { action = VPRActions.SwiftskinsBite; ability = EchidnaAbilities.SwiftskinsBite; }
            else { action = GetAoeStarterAction(context); ability = MapAoeStarter(action); }
        }
        else
        {
            action = GetAoeStarterAction(context);
            ability = MapAoeStarter(action);
        }

        if (level < action.MinLevel) return;
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        var actionRef = action;
        scheduler.PushGcd(ability, context.Player.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = actionRef.Name;
                context.Debug.DamageState = $"{actionRef.Name} (AoE combo {context.ComboStep + 1})";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(actionRef.ActionId, actionRef.Name)
                    .AsAoE(enemyCount)
                    .Reason($"Using {actionRef.Name} (AoE combo step {context.ComboStep + 1})",
                        "VPR's AoE combo mirrors the single-target rotation. Steel Maw → Hunter's Bite → Jagged Maw " +
                        "or Reaving Maw → Swiftskin's Bite → Bloodied Maw. Maintains buffs and builds Serpent Offering.")
                    .Factors(new[] { $"Enemies: {enemyCount}", $"Combo step: {context.ComboStep + 1}" })
                    .Alternatives(new[] { "Use ST combo for single target" })
                    .Tip("Use AoE combo at 3+ enemies. Same buff rotation logic as single-target.")
                    .Concept("vpr.combo_basics")
                    .Record();
                context.TrainingService?.RecordConceptApplication("vpr.combo_basics", true, "AoE combo");
            });
    }

    private void TryPushWrithingSnap(IEchidnaContext context, RotationScheduler scheduler, IBattleChara target, bool isMoving)
    {
        var player = context.Player;
        if (player.Level < VPRActions.WrithingSnap.MinLevel) return;

        bool outOfRange = !DistanceHelper.IsActionInRange(VPRActions.SteelFangs.ActionId, player, target);
        if (!outOfRange && !isMoving) return;
        if (context.RattlingCoils > 0) return;
        if (!context.ActionService.IsActionReady(VPRActions.WrithingSnap.ActionId)) return;

        scheduler.PushGcd(EchidnaAbilities.WrithingSnap, target.GameObjectId, priority: 8,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = VPRActions.WrithingSnap.Name;
                context.Debug.DamageState = "Writhing Snap (ranged filler)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(VPRActions.WrithingSnap.ActionId, VPRActions.WrithingSnap.Name)
                    .AsMeleeDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Using Writhing Snap (out of melee range, no Rattling Coils)",
                        "Writhing Snap is VPR's basic ranged GCD filler. Use only when out of melee range " +
                        "and no Rattling Coils are available for Uncoiled Fury.")
                    .Factors(new[] { outOfRange ? "Out of melee range" : "Moving", $"Rattling Coils: {context.RattlingCoils}" })
                    .Alternatives(new[] { "Return to melee range", "Use Uncoiled Fury if coils available" })
                    .Tip("Prioritize Uncoiled Fury over Writhing Snap whenever possible.")
                    .Concept("vpr.rattling_coil")
                    .Record();
            });
    }

    #endregion

    #region Helpers

    private static ActionDefinition GetStarterAction(IEchidnaContext context)
    {
        if (context.HasHonedReavers) return VPRActions.ReavingFangs;
        if (context.HasHonedSteel) return VPRActions.SteelFangs;
        if (!context.HasHuntersInstinct || context.HuntersInstinctRemaining < context.SwiftscaledRemaining)
            return VPRActions.SteelFangs;
        return VPRActions.ReavingFangs;
    }

    private static ActionDefinition GetAoeStarterAction(IEchidnaContext context)
    {
        if (context.HasHonedReavers) return VPRActions.ReavingMaw;
        if (context.HasHonedSteel) return VPRActions.SteelMaw;
        if (!context.HasHuntersInstinct || context.HuntersInstinctRemaining < context.SwiftscaledRemaining)
            return VPRActions.SteelMaw;
        return VPRActions.ReavingMaw;
    }

    private static AbilityBehavior MapStarter(ActionDefinition action)
    {
        if (action == VPRActions.SteelFangs) return EchidnaAbilities.SteelFangs;
        if (action == VPRActions.ReavingFangs) return EchidnaAbilities.ReavingFangs;
        return EchidnaAbilities.SteelFangs;
    }

    private static AbilityBehavior MapAoeStarter(ActionDefinition action)
    {
        if (action == VPRActions.SteelMaw) return EchidnaAbilities.SteelMaw;
        if (action == VPRActions.ReavingMaw) return EchidnaAbilities.ReavingMaw;
        return EchidnaAbilities.SteelMaw;
    }

    private static bool ShouldRefreshNoxiousGnash(IEchidnaContext context)
    {
        return !context.HasNoxiousGnash || context.NoxiousGnashRemaining < 5f;
    }

    #endregion
}
