using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.PersephoneCore.Abilities;
using Olympus.Rotation.PersephoneCore.Context;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Services.Training;

namespace Olympus.Rotation.PersephoneCore.Modules;

/// <summary>
/// Handles the Summoner damage rotation (scheduler-driven).
/// Demi-summon GCDs use ReplacementBaseId so UseAction receives the base Ruin III ID
/// (the game upgrades server-side). The Aethercharge demi-summon entry uses raw
/// ActionManager directly because it requires the adjusted ID at the dispatch site.
/// </summary>
public sealed class DamageModule : IPersephoneModule
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

    public bool TryExecute(IPersephoneContext context, bool isMoving) => false;

    public void UpdateDebugState(IPersephoneContext context) { }

    public unsafe void CollectCandidates(IPersephoneContext context, RotationScheduler scheduler, bool isMoving)
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
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.CasterTargetingRange,
            player);
        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return;
        }

        // Carbuncle first
        if (!context.HasPetSummoned)
        {
            TryPushSummonCarbuncle(context, scheduler);
            return;
        }

        var aoeEnabled = context.Configuration.Summoner.EnableAoERotation;
        var aoeThreshold = context.Configuration.Summoner.AoEMinTargets;
        var rawEnemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = rawEnemyCount;
        var enemyCount = aoeEnabled ? rawEnemyCount : 0;
        var useAoe = enemyCount >= aoeThreshold;

        if (context.IsDemiSummonActive)
            TryPushDemiSummonGcd(context, scheduler, target, useAoe);

        if (context.IsIfritAttuned || context.IsTitanAttuned || context.IsGarudaAttuned)
            TryPushAttunementGcd(context, scheduler, target, useAoe, isMoving);

        TryPushPrimalFavor(context, scheduler, target, isMoving);

        if (context.PrimalsAvailable > 0 && !context.IsDemiSummonActive)
            TryPushSummonPrimal(context, scheduler, target);

        // Demi-summon entry (Aethercharge → Bahamut/Phoenix/Solar Bahamut) uses raw ActionManager
        // because UseAction needs the adjusted ID at dispatch time, not the base ID.
        if (!context.IsDemiSummonActive && context.AttunementStacks == 0
            && context.PrimalsAvailable == 0)
        {
            FireSummonDemiRaw(context, target);
        }

        if (!context.IsDemiSummonActive && context.HasFurtherRuin)
            TryPushRuin4(context, scheduler, target);

        TryPushFiller(context, scheduler, target, useAoe, isMoving);
    }

    private void TryPushSummonCarbuncle(IPersephoneContext context, RotationScheduler scheduler)
    {
        if (context.Player.Level < SMNActions.SummonCarbuncle.MinLevel) return;
        var castTime = context.HasInstantCast ? 0f : SMNActions.SummonCarbuncle.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }
        scheduler.PushGcd(PersephoneAbilities.SummonCarbuncle, context.Player.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = SMNActions.SummonCarbuncle.Name;
                context.Debug.DamageState = "Summon Carbuncle";
            });
    }

    private void TryPushDemiSummonGcd(IPersephoneContext context, RotationScheduler scheduler, IBattleChara target, bool useAoe)
    {
        if (context.IsBahamutActive && !context.Configuration.Summoner.EnableBahamut) return;
        if (context.IsPhoenixActive && !context.Configuration.Summoner.EnablePhoenix) return;
        if (context.IsSolarBahamutActive && !context.Configuration.Summoner.EnableSolarBahamut) return;

        var demiGcd = SMNActions.GetDemiSummonGcd(context.IsBahamutActive, context.IsSolarBahamutActive, useAoe);
        var ability = demiGcd.ActionId switch
        {
            var id when id == SMNActions.AstralImpulse.ActionId => PersephoneAbilities.AstralImpulse,
            var id when id == SMNActions.AstralFlare.ActionId => PersephoneAbilities.AstralFlare,
            var id when id == SMNActions.FountainOfFire.ActionId => PersephoneAbilities.FountainOfFire,
            var id when id == SMNActions.BrandOfPurgatory.ActionId => PersephoneAbilities.BrandOfPurgatory,
            var id when id == SMNActions.UmbralImpulse.ActionId => PersephoneAbilities.UmbralImpulse,
            var id when id == SMNActions.UmbralFlare.ActionId => PersephoneAbilities.UmbralFlare,
            _ => PersephoneAbilities.AstralImpulse,
        };

        scheduler.PushGcd(ability, target.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = demiGcd.Name;
                context.Debug.DamageState = $"{demiGcd.Name} (Demi phase)";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var demiType = context.IsBahamutActive ? "Bahamut" : context.IsPhoenixActive ? "Phoenix" : "Solar Bahamut";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(demiGcd.ActionId, demiGcd.Name)
                        .AsSummon(demiType).Target(target.Name?.TextValue)
                        .Reason($"{demiGcd.Name} during {demiType} phase",
                            $"During {demiType} phase, your normal GCDs are replaced with powerful summon-specific attacks.")
                        .Factors($"{demiType} active", $"Timer: {context.DemiSummonTimer:F1}s")
                        .Alternatives("None - always use demi GCDs")
                        .Tip($"Maximize GCDs during {demiType} phase.")
                        .Concept(SmnConcepts.DemiPhases)
                        .Record();
                    context.TrainingService.RecordConceptApplication(SmnConcepts.DemiPhases, true, "Demi-summon GCD used");
                }
            });
    }

    private void TryPushAttunementGcd(IPersephoneContext context, RotationScheduler scheduler, IBattleChara target, bool useAoe, bool isMoving)
    {
        if (!context.Configuration.Summoner.EnablePrimalAbilities) return;
        var player = context.Player;

        var action = SMNActions.GetGemshinAction(context.CurrentAttunement, useAoe);
        if (action == null) return;

        if (context.IsIfritAttuned && isMoving && !context.HasInstantCast && !context.CanSlidecast)
        {
            if (context.SwiftcastReady)
            {
                scheduler.PushOgcd(PersephoneAbilities.Swiftcast, player.GameObjectId, priority: 7,
                    onDispatched: _ => context.Debug.DamageState = "Swiftcast for Ruby");
            }
            return;
        }

        var castTime = context.HasInstantCast ? 0f : action.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        var ability = action.ActionId switch
        {
            var id when id == SMNActions.RubyRite.ActionId => PersephoneAbilities.RubyRite,
            var id when id == SMNActions.RubyCatastrophe.ActionId => PersephoneAbilities.RubyCatastrophe,
            var id when id == SMNActions.TopazRite.ActionId => PersephoneAbilities.TopazRite,
            var id when id == SMNActions.TopazCatastrophe.ActionId => PersephoneAbilities.TopazCatastrophe,
            var id when id == SMNActions.EmeraldRite.ActionId => PersephoneAbilities.EmeraldRite,
            var id when id == SMNActions.EmeraldCatastrophe.ActionId => PersephoneAbilities.EmeraldCatastrophe,
            _ => PersephoneAbilities.RubyRite,
        };

        scheduler.PushGcd(ability, target.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} ({context.Debug.AttunementName} {context.AttunementStacks - 1})";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var primalType = context.IsIfritAttuned ? "Ifrit" : context.IsTitanAttuned ? "Titan" : "Garuda";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(action.ActionId, action.Name)
                        .AsCasterDamage().Target(target.Name?.TextValue)
                        .Reason($"{action.Name} - {primalType} attunement",
                            "Gemshine attacks consume attunement stacks.")
                        .Factors($"{primalType} attuned", $"Stacks: {context.AttunementStacks}")
                        .Alternatives("None - spend all attunement stacks")
                        .Tip($"Use all {primalType} attunement stacks before next primal.")
                        .Concept(SmnConcepts.AttunementSystem)
                        .Record();
                    context.TrainingService.RecordConceptApplication(SmnConcepts.AttunementSystem, true, "Attunement stack spent");
                }
            });
    }

    private void TryPushPrimalFavor(IPersephoneContext context, RotationScheduler scheduler, IBattleChara target, bool isMoving)
    {
        if (!context.Configuration.Summoner.EnablePrimalAbilities) return;
        var player = context.Player;
        var level = player.Level;

        // Crimson Cyclone
        if (context.HasIfritsFavor && level >= SMNActions.CrimsonCyclone.MinLevel)
        {
            scheduler.PushGcd(PersephoneAbilities.CrimsonCyclone, target.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = SMNActions.CrimsonCyclone.Name;
                    context.Debug.DamageState = "Crimson Cyclone (gap closer)";
                });
        }

        // Slipstream — has cast time, may need swiftcast
        if (context.HasGarudasFavor && level >= SMNActions.Slipstream.MinLevel)
        {
            if (isMoving && !context.HasInstantCast && !context.CanSlidecast)
            {
                if (context.SwiftcastReady)
                {
                    scheduler.PushOgcd(PersephoneAbilities.Swiftcast, player.GameObjectId, priority: 7,
                        onDispatched: _ => context.Debug.DamageState = "Swiftcast for Slipstream");
                }
                return;
            }

            var castTime = context.HasInstantCast ? 0f : SMNActions.Slipstream.CastTime;
            if (MechanicCastGate.ShouldBlock(context, castTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }

            scheduler.PushGcd(PersephoneAbilities.Slipstream, target.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = SMNActions.Slipstream.Name;
                    context.Debug.DamageState = "Slipstream";
                });
        }
    }

    private void TryPushSummonPrimal(IPersephoneContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;
        if (context.IsDemiSummonActive || context.AttunementStacks > 0) return;

        if (context.Configuration.Summoner.EnableTitan && context.CanSummonTitan && level >= SMNActions.SummonTitan.MinLevel)
        {
            scheduler.PushGcd(PersephoneAbilities.SummonTitan, target.GameObjectId, priority: 5,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = "Summon Titan";
                    context.Debug.DamageState = "Summon Titan";
                });
            return;
        }
        if (context.Configuration.Summoner.EnableGaruda && context.CanSummonGaruda && level >= SMNActions.SummonGaruda.MinLevel)
        {
            scheduler.PushGcd(PersephoneAbilities.SummonGaruda, target.GameObjectId, priority: 5,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = "Summon Garuda";
                    context.Debug.DamageState = "Summon Garuda";
                });
            return;
        }
        if (context.Configuration.Summoner.EnableIfrit && context.CanSummonIfrit && level >= SMNActions.SummonIfrit.MinLevel)
        {
            scheduler.PushGcd(PersephoneAbilities.SummonIfrit, target.GameObjectId, priority: 5,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = "Summon Ifrit";
                    context.Debug.DamageState = "Summon Ifrit";
                });
        }
    }

    /// <summary>
    /// Demi-summon entry (Aethercharge → Bahamut/Phoenix/Solar Bahamut). Bypasses scheduler
    /// because UseAction must receive the ADJUSTED action ID at dispatch time, which the
    /// scheduler's ReplacementBaseId path doesn't support. Fires immediately if conditions met.
    /// </summary>
    private unsafe void FireSummonDemiRaw(IPersephoneContext context, IBattleChara target)
    {
        var player = context.Player;
        if (player.Level < SMNActions.Aethercharge.MinLevel) return;

        var actionManager = SafeGameAccess.GetActionManager(null);
        if (actionManager == null) return;

        var baseId = SMNActions.Aethercharge.ActionId;
        var adjustedId = actionManager->GetAdjustedActionId(baseId);
        var status = actionManager->GetActionStatus(ActionType.Action, adjustedId);
        if (status != 0) return;

        if (!context.CanExecuteGcd) return;

        var result = actionManager->UseAction(ActionType.Action, adjustedId, target.GameObjectId);
        if (result)
        {
            context.Debug.DamageState = "Demi-Summon";
        }
    }

    private void TryPushRuin4(IPersephoneContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Summoner.EnableRuinIV) return;
        var player = context.Player;
        if (player.Level < SMNActions.Ruin4.MinLevel) return;
        if (!context.HasFurtherRuin) return;
        if (context.IsDemiSummonActive) return;

        // Expiring proc OR filler between phases
        bool expiring = context.FurtherRuinRemaining < 5f;
        bool filler = !context.IsDemiSummonActive && context.PrimalsAvailable == 0 && context.AttunementStacks == 0;
        if (!expiring && !filler) return;

        scheduler.PushGcd(PersephoneAbilities.Ruin4, target.GameObjectId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = SMNActions.Ruin4.Name;
                context.Debug.DamageState = expiring ? $"Ruin IV (expiring: {context.FurtherRuinRemaining:F1}s)" : "Ruin IV (filler)";
            });
    }

    private void TryPushFiller(IPersephoneContext context, RotationScheduler scheduler, IBattleChara target, bool useAoe, bool isMoving)
    {
        if (!context.Configuration.Summoner.EnableRuin) return;
        var player = context.Player;
        var level = player.Level;

        if (isMoving && !context.HasInstantCast && !context.CanSlidecast)
        {
            if (context.HasFurtherRuin && level >= SMNActions.Ruin4.MinLevel)
            {
                scheduler.PushGcd(PersephoneAbilities.Ruin4, target.GameObjectId, priority: 7,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = SMNActions.Ruin4.Name;
                        context.Debug.DamageState = "Ruin IV (movement)";
                    });
                return;
            }
            if (level >= SMNActions.Ruin2.MinLevel)
            {
                scheduler.PushGcd(PersephoneAbilities.Ruin2, target.GameObjectId, priority: 8,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = SMNActions.Ruin2.Name;
                        context.Debug.DamageState = "Ruin II (movement)";
                    });
                return;
            }
        }

        var action = useAoe ? SMNActions.GetAoeSpell(level) : SMNActions.GetRuinSpell(level);
        var ability = action.ActionId switch
        {
            var id when id == SMNActions.Ruin3.ActionId => PersephoneAbilities.Ruin3,
            var id when id == SMNActions.Ruin.ActionId => PersephoneAbilities.Ruin,
            var id when id == SMNActions.TriDisaster.ActionId => PersephoneAbilities.TriDisaster,
            var id when id == SMNActions.Outburst.ActionId => PersephoneAbilities.Outburst,
            _ => PersephoneAbilities.Ruin3,
        };

        var castTime = context.HasInstantCast ? 0f : action.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(ability, target.GameObjectId, priority: 9,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = action.Name;
            });
    }
}
