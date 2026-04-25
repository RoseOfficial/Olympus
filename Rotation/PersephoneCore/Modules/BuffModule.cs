using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.PersephoneCore.Abilities;
using Olympus.Rotation.PersephoneCore.Context;
using Olympus.Services;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.PersephoneCore.Modules;

/// <summary>
/// Handles Summoner oGCD buffs and abilities (scheduler-driven).
/// Manages Enkindle, Energy Drain, Searing Light, Mountain Buster, Astral Flow, Lucid.
/// </summary>
public sealed class BuffModule : IPersephoneModule
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

    public bool TryExecute(IPersephoneContext context, bool isMoving) => false;

    public void UpdateDebugState(IPersephoneContext context) { }

    public void CollectCandidates(IPersephoneContext context, RotationScheduler scheduler, bool isMoving)
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

        TryPushEnkindle(context, scheduler, target);
        TryPushAstralFlow(context, scheduler, target);
        TryPushMountainBuster(context, scheduler, target);
        TryPushSearingLight(context, scheduler);
        TryPushSearingFlash(context, scheduler, target);
        TryPushEnergyDrain(context, scheduler, target);
        TryPushAetherflowSpender(context, scheduler, target);
        TryPushLucidDreaming(context, scheduler);
    }

    private void TryPushEnkindle(IPersephoneContext context, RotationScheduler scheduler, IBattleChara? target)
    {
        if (!context.Configuration.Summoner.EnableEnkindle) return;
        if (target == null) return;
        if (!context.IsDemiSummonActive) return;
        if (context.HasUsedEnkindleThisPhase) return;

        var enkindleAction = SMNActions.GetEnkindleAction(
            context.IsBahamutActive, context.IsPhoenixActive, context.IsSolarBahamutActive);
        if (enkindleAction == null || context.Player.Level < enkindleAction.MinLevel) return;
        if (!context.EnkindleReady) return;

        var ability = enkindleAction.ActionId switch
        {
            var id when id == SMNActions.EnkindleBahamut.ActionId => PersephoneAbilities.EnkindleBahamut,
            var id when id == SMNActions.EnkindlePhoenix.ActionId => PersephoneAbilities.EnkindlePhoenix,
            var id when id == SMNActions.EnkindleSolarBahamut.ActionId => PersephoneAbilities.EnkindleSolarBahamut,
            _ => PersephoneAbilities.EnkindleBahamut,
        };

        scheduler.PushOgcd(ability, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.MarkEnkindleUsed();
                context.Debug.PlannedAction = enkindleAction.Name;
                context.Debug.BuffState = $"{enkindleAction.Name} (Enkindle)";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var demiType = context.IsBahamutActive ? "Bahamut" : context.IsPhoenixActive ? "Phoenix" : "Solar Bahamut";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(enkindleAction.ActionId, enkindleAction.Name)
                        .AsCasterBurst().Target(target.Name?.TextValue)
                        .Reason($"Enkindle during {demiType} phase",
                            "Enkindle is your highest-potency oGCD during demi-summon phases. Use early to avoid losing to phase transitions.")
                        .Factors($"{demiType} active", $"Timer: {context.DemiSummonTimer:F1}s")
                        .Alternatives("Wait for raid buffs (risky)")
                        .Tip("Use Enkindle early in demi phase.")
                        .Concept(SmnConcepts.Enkindle)
                        .Record();
                    context.TrainingService.RecordConceptApplication(SmnConcepts.Enkindle, true, $"Used {enkindleAction.Name}");
                }
            });
    }

    private void TryPushAstralFlow(IPersephoneContext context, RotationScheduler scheduler, IBattleChara? target)
    {
        if (!context.Configuration.Summoner.EnableAstralFlow) return;
        if (!context.IsDemiSummonActive) return;
        if (context.HasUsedAstralFlowThisPhase) return;

        var astralFlowAction = SMNActions.GetAstralFlowAction(
            context.IsBahamutActive, context.IsPhoenixActive, context.IsSolarBahamutActive);
        if (astralFlowAction == null || context.Player.Level < astralFlowAction.MinLevel) return;
        if (!context.AstralFlowReady) return;

        // Phoenix → Rekindle on injured ally
        if (context.IsPhoenixActive)
        {
            var rekindleTarget = context.PartyHelper.FindRekindleTarget(context.Player, 0.9f)
                                 ?? context.PartyHelper.GetLowestHpMember(context.Player)
                                 ?? context.Player;
            scheduler.PushOgcd(PersephoneAbilities.Rekindle, rekindleTarget.GameObjectId, priority: 1,
                onDispatched: _ =>
                {
                    context.MarkAstralFlowUsed();
                    context.Debug.PlannedAction = SMNActions.Rekindle.Name;
                    context.Debug.BuffState = "Rekindle";
                });
            return;
        }

        if (target == null) return;
        var ability = astralFlowAction.ActionId switch
        {
            var id when id == SMNActions.Deathflare.ActionId => PersephoneAbilities.Deathflare,
            var id when id == SMNActions.Sunflare.ActionId => PersephoneAbilities.Sunflare,
            _ => PersephoneAbilities.Deathflare,
        };
        scheduler.PushOgcd(ability, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.MarkAstralFlowUsed();
                context.Debug.PlannedAction = astralFlowAction.Name;
                context.Debug.BuffState = $"{astralFlowAction.Name} (Astral Flow)";

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var demiType = context.IsBahamutActive ? "Bahamut" : "Solar Bahamut";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(astralFlowAction.ActionId, astralFlowAction.Name)
                        .AsCasterBurst().Target(target.Name?.TextValue)
                        .Reason($"{astralFlowAction.Name} during {demiType} phase",
                            $"{astralFlowAction.Name} is a high-potency AoE oGCD usable once per demi phase.")
                        .Factors($"{demiType} active", $"Timer: {context.DemiSummonTimer:F1}s")
                        .Alternatives("Wait for more enemies (risky)")
                        .Tip($"Use {astralFlowAction.Name} early in demi phase.")
                        .Concept(SmnConcepts.AstralFlow)
                        .Record();
                    context.TrainingService.RecordConceptApplication(SmnConcepts.AstralFlow, true, $"Used {astralFlowAction.Name}");
                }
            });
    }

    private void TryPushMountainBuster(IPersephoneContext context, RotationScheduler scheduler, IBattleChara? target)
    {
        if (!context.Configuration.Summoner.EnableMountainBuster) return;
        if (target == null) return;
        if (context.Player.Level < SMNActions.MountainBuster.MinLevel) return;
        if (!context.HasTitansFavor) return;

        scheduler.PushOgcd(PersephoneAbilities.MountainBuster, target.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = SMNActions.MountainBuster.Name;
                context.Debug.BuffState = "Mountain Buster";
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SMNActions.MountainBuster.ActionId, SMNActions.MountainBuster.Name)
                        .AsCasterResource("Titan's Favor", 1)
                        .Target(target.Name?.TextValue)
                        .Reason("Mountain Buster from Titan's Favor",
                            "Mountain Buster is an instant oGCD granted by Titan's Favor.")
                        .Factors("Titan's Favor active")
                        .Alternatives("None - always use immediately")
                        .Tip("Weave Mountain Buster after each Topaz Rite.")
                        .Concept(SmnConcepts.MountainBuster)
                        .Record();
                    context.TrainingService.RecordConceptApplication(SmnConcepts.MountainBuster, true, "Mountain Buster used");
                }
            });
    }

    private void TryPushSearingLight(IPersephoneContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Summoner.EnableSearingLight) return;
        var player = context.Player;
        if (player.Level < SMNActions.SearingLight.MinLevel) return;
        if (!context.SearingLightReady) return;
        if (context.HasSearingLight) return;
        if (BurstHoldHelper.ShouldHoldForPhaseTransition(context.TimelineService))
        {
            context.Debug.BuffState = "Holding Searing Light (phase soon)";
            return;
        }
        if (ShouldHoldForBurst(context.Configuration.Summoner.SearingLightHoldTime))
        {
            context.Debug.BuffState = "Holding Searing Light for burst";
            return;
        }
        if (!context.IsDemiSummonActive)
        {
            context.Debug.BuffState = "Hold Searing Light for demi";
            return;
        }

        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled
            && context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            if (!partyCoord.IsRaidBuffAligned(SMNActions.SearingLight.ActionId))
                context.Debug.BuffState = "Raid buffs desynced, using independently";
            else if (partyCoord.HasPendingRaidBuffIntent(
                context.Configuration.PartyCoordination.RaidBuffAlignmentWindowSeconds))
                context.Debug.BuffState = "Aligning with party burst";
            partyCoord.AnnounceRaidBuffIntent(SMNActions.SearingLight.ActionId);
        }

        scheduler.PushOgcd(PersephoneAbilities.SearingLight, player.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = SMNActions.SearingLight.Name;
                context.Debug.BuffState = "Searing Light (burst)";
                partyCoord?.OnRaidBuffUsed(SMNActions.SearingLight.ActionId, 120_000);

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SMNActions.SearingLight.ActionId, SMNActions.SearingLight.Name)
                        .AsRaidBuff()
                        .Reason("Searing Light during demi-summon burst",
                            "Searing Light is a party-wide 5% damage buff on a 120s cooldown.")
                        .Factors("Demi-summon active", "2-minute cooldown alignment")
                        .Alternatives("Wait for party alignment")
                        .Tip("Use Searing Light at the start of demi-summon phases.")
                        .Concept(SmnConcepts.SearingLight)
                        .Record();
                    context.TrainingService.RecordConceptApplication(SmnConcepts.SearingLight, true, "Raid buff used during burst");
                }
            });
    }

    private void TryPushSearingFlash(IPersephoneContext context, RotationScheduler scheduler, IBattleChara? target)
    {
        if (!context.Configuration.Summoner.EnableSearingFlash) return;
        if (target == null) return;
        if (context.Player.Level < SMNActions.SearingFlash.MinLevel) return;
        if (!context.HasSearingLight) return;
        if (!context.ActionService.IsActionReady(SMNActions.SearingFlash.ActionId)) return;

        scheduler.PushOgcd(PersephoneAbilities.SearingFlash, target.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = SMNActions.SearingFlash.Name;
                context.Debug.BuffState = "Searing Flash";
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SMNActions.SearingFlash.ActionId, SMNActions.SearingFlash.Name)
                        .AsCasterBurst().Target(target.Name?.TextValue)
                        .Reason("Searing Flash during Searing Light",
                            "Searing Flash is a free AoE oGCD that becomes available once per Searing Light window.")
                        .Factors("Searing Light active")
                        .Alternatives("None - always use during Searing Light")
                        .Tip("Searing Flash is free damage - never let a Searing Light window end without using it.")
                        .Concept(SmnConcepts.SearingFlash)
                        .Record();
                    context.TrainingService.RecordConceptApplication(SmnConcepts.SearingFlash, true, "Searing Flash used");
                }
            });
    }

    private void TryPushEnergyDrain(IPersephoneContext context, RotationScheduler scheduler, IBattleChara? target)
    {
        if (!context.Configuration.Summoner.EnableEnergyDrain) return;
        if (target == null) return;
        var player = context.Player;
        if (player.Level < SMNActions.EnergyDrain.MinLevel) return;
        if (!context.EnergyDrainReady) return;
        if (context.HasAetherflow) return;

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        var useAoe = enemyCount >= 3 && player.Level >= SMNActions.EnergySiphon.MinLevel;
        var action = useAoe ? SMNActions.EnergySiphon : SMNActions.EnergyDrain;
        var ability = useAoe ? PersephoneAbilities.EnergySiphon : PersephoneAbilities.EnergyDrain;

        scheduler.PushOgcd(ability, target.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.BuffState = $"{action.Name} (+2 Aetherflow)";
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(action.ActionId, action.Name)
                        .AsCasterResource("Aetherflow", 0)
                        .Target(target.Name?.TextValue)
                        .Reason($"{action.Name} to generate Aetherflow",
                            "Generates 2 Aetherflow stacks on a 60s cooldown.")
                        .Factors("Aetherflow stacks: 0", "Cooldown ready")
                        .Alternatives("None - use when empty and ready")
                        .Tip("Use Energy Drain/Siphon on cooldown when Aetherflow is empty.")
                        .Concept(SmnConcepts.EnergyDrainUsage)
                        .Record();
                    context.TrainingService.RecordConceptApplication(SmnConcepts.EnergyDrainUsage, true, "Generated Aetherflow stacks");
                }
            });
    }

    private void TryPushAetherflowSpender(IPersephoneContext context, RotationScheduler scheduler, IBattleChara? target)
    {
        if (!context.Configuration.Summoner.EnableFester) return;
        if (target == null) return;
        var player = context.Player;
        if (!context.HasAetherflow) return;
        if (context.AetherflowStacks <= context.Configuration.Summoner.AetherflowReserve) return;

        var energyDrainSoon = !context.EnergyDrainReady
            && context.ActionService.GetCooldownRemaining(SMNActions.EnergyDrain.ActionId) < 5f;
        var inBurst = context.IsDemiSummonActive || context.HasSearingLight;
        if (!inBurst && !energyDrainSoon)
        {
            context.Debug.BuffState = "Hold Aetherflow for burst";
            return;
        }
        if (context.Configuration.Summoner.EnableBurstPooling && ShouldHoldForBurst(10f) && !inBurst)
        {
            context.Debug.BuffState = "Hold Aetherflow for imminent burst";
            return;
        }

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        var useAoe = enemyCount >= 3;
        var action = useAoe ? SMNActions.GetAetherflowSpenderAoe(player.Level) : SMNActions.GetAetherflowSpenderST(player.Level);
        var ability = action.ActionId switch
        {
            var id when id == SMNActions.Necrotize.ActionId => PersephoneAbilities.Necrotize,
            var id when id == SMNActions.Fester.ActionId => PersephoneAbilities.Fester,
            var id when id == SMNActions.Painflare.ActionId => PersephoneAbilities.Painflare,
            _ => PersephoneAbilities.Fester,
        };
        if (player.Level < action.MinLevel) return;

        scheduler.PushOgcd(ability, target.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.BuffState = $"{action.Name} (Aetherflow: {context.AetherflowStacks - 1})";
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var reason = inBurst ? "burst window" : "Energy Drain coming off cooldown";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(action.ActionId, action.Name)
                        .AsCasterResource("Aetherflow", context.AetherflowStacks)
                        .Target(target.Name?.TextValue)
                        .Reason($"{action.Name} spent during {reason}",
                            "Aetherflow spenders deal significant oGCD damage. Prefer using during burst windows.")
                        .Factors($"Aetherflow: {context.AetherflowStacks}", inBurst ? "Burst window" : "Energy Drain soon")
                        .Alternatives("Wait for burst (if ED not imminent)")
                        .Tip("Spend Aetherflow during burst windows for maximum damage.")
                        .Concept(SmnConcepts.FesterNecrotize)
                        .Record();
                    context.TrainingService.RecordConceptApplication(SmnConcepts.FesterNecrotize, true, "Aetherflow spent efficiently");
                }
            });
    }

    private void TryPushLucidDreaming(IPersephoneContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Summoner.EnableLucidDreaming) return;
        var player = context.Player;
        if (player.Level < RoleActions.LucidDreaming.MinLevel) return;
        if (!context.LucidDreamingReady) return;
        if (context.MpPercent > context.Configuration.Summoner.LucidDreamingThreshold) return;

        scheduler.PushOgcd(PersephoneAbilities.LucidDreaming, player.GameObjectId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.LucidDreaming.Name;
                context.Debug.BuffState = "Lucid Dreaming (MP)";
            });
    }
}
