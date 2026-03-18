using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.PersephoneCore.Context;
using Olympus.Services.Party;
using Olympus.Services.Training;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.PersephoneCore.Modules;

/// <summary>
/// Handles Summoner oGCD buffs and abilities.
/// Manages Enkindle, Energy Drain, Searing Light, Mountain Buster, and Astral Flow abilities.
/// </summary>
public sealed class BuffModule : IPersephoneModule
{
    public int Priority => 20; // Higher priority than damage (lower number = higher priority)
    public string Name => "Buff";

    public bool TryExecute(IPersephoneContext context, bool isMoving)
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

        // Priority 1: Enkindle during demi-summon phases (high damage)
        if (TryEnkindle(context, target))
            return true;

        // Priority 2: Astral Flow abilities (Deathflare/Sunflare for damage, Rekindle for heals)
        if (TryAstralFlow(context, target))
            return true;

        // Priority 3: Mountain Buster during Titan phase (use after each Topaz Rite)
        if (TryMountainBuster(context, target))
            return true;

        // Priority 4: Searing Light (align with demi-summon or raid buffs)
        if (TrySearingLight(context))
            return true;

        // Priority 5: Searing Flash (during Searing Light window)
        if (TrySearingFlash(context, target))
            return true;

        // Priority 6: Energy Drain (generate Aetherflow when empty)
        if (TryEnergyDrain(context, target))
            return true;

        // Priority 7: Necrotize/Fester (spend Aetherflow, prefer burst windows)
        if (TryAetherflowSpender(context, target))
            return true;

        // Priority 8: Lucid Dreaming (MP management)
        if (TryLucidDreaming(context))
            return true;

        context.Debug.BuffState = "No oGCD needed";
        return false;
    }

    public void UpdateDebugState(IPersephoneContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Timeline Awareness

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a phase transition is expected within the window.
    /// </summary>
    private bool ShouldHoldBurstForPhase(IPersephoneContext context, float windowSeconds = 8f)
    {
        var nextPhase = context.TimelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;

        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }

    #endregion

    #region oGCD Actions

    private bool TryEnkindle(IPersephoneContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (!context.Configuration.Summoner.EnableEnkindle)
            return false;

        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        // Only use during demi-summon phases
        if (!context.IsDemiSummonActive)
            return false;

        // Only use once per demi-summon phase
        if (context.HasUsedEnkindleThisPhase)
            return false;

        // Check if we have a valid Enkindle action
        var enkindleAction = SMNActions.GetEnkindleAction(
            context.IsBahamutActive,
            context.IsPhoenixActive,
            context.IsSolarBahamutActive);

        if (enkindleAction == null || level < enkindleAction.MinLevel)
            return false;

        if (!context.EnkindleReady)
            return false;

        // Use Enkindle during demi-summon phase
        if (context.ActionService.ExecuteOgcd(enkindleAction, target.GameObjectId))
        {
            context.MarkEnkindleUsed();
            context.Debug.PlannedAction = enkindleAction.Name;
            context.Debug.BuffState = $"{enkindleAction.Name} (Enkindle)";

            // Training Mode recording
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var demiType = context.IsBahamutActive ? "Bahamut" :
                               context.IsPhoenixActive ? "Phoenix" : "Solar Bahamut";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(enkindleAction.ActionId, enkindleAction.Name)
                    .AsCasterBurst()
                    .Target(target.Name?.TextValue)
                    .Reason($"Enkindle during {demiType} phase",
                        $"Enkindle is your highest-potency oGCD during demi-summon phases. " +
                        $"It can only be used once per demi-summon, so use it as soon as possible " +
                        $"to maximize damage during the 15-second window.")
                    .Factors($"{demiType} active", $"Timer: {context.DemiSummonTimer:F1}s", $"GCDs left: {context.DemiSummonGcdsRemaining}")
                    .Alternatives("Wait for raid buffs (risky)")
                    .Tip("Use Enkindle early in demi phase - don't risk losing it to phase transitions.")
                    .Concept(SmnConcepts.Enkindle)
                    .Record();

                context.TrainingService.RecordConceptApplication(
                    SmnConcepts.Enkindle, true, $"Used {enkindleAction.Name} during {demiType}");
            }

            return true;
        }

        return false;
    }

    private bool TryAstralFlow(IPersephoneContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (!context.Configuration.Summoner.EnableAstralFlow)
            return false;

        var player = context.Player;
        var level = player.Level;

        // Only use during demi-summon phases
        if (!context.IsDemiSummonActive)
            return false;

        // Only use once per demi-summon phase
        if (context.HasUsedAstralFlowThisPhase)
            return false;

        // Get the appropriate Astral Flow action
        var astralFlowAction = SMNActions.GetAstralFlowAction(
            context.IsBahamutActive,
            context.IsPhoenixActive,
            context.IsSolarBahamutActive);

        if (astralFlowAction == null || level < astralFlowAction.MinLevel)
            return false;

        if (!context.AstralFlowReady)
            return false;

        // For Rekindle (Phoenix), find a party member to heal
        if (context.IsPhoenixActive)
        {
            var rekindleTarget = context.PartyHelper.FindRekindleTarget(player, 0.9f);
            if (rekindleTarget != null)
            {
                if (context.ActionService.ExecuteOgcd(SMNActions.Rekindle, rekindleTarget.GameObjectId))
                {
                    context.Debug.PlannedAction = SMNActions.Rekindle.Name;
                    context.Debug.BuffState = "Rekindle (healing)";
                    context.MarkAstralFlowUsed();

                    // Training Mode recording
                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(SMNActions.Rekindle.ActionId, SMNActions.Rekindle.Name)
                            .AsSummon("Phoenix")
                            .Reason("Rekindle on injured party member",
                                "Rekindle is Phoenix's unique Astral Flow ability that provides instant healing plus a heal-over-time. " +
                                "Prioritize injured party members to maximize healing value during Phoenix phase.")
                            .Factors("Phoenix phase active", $"Target HP: {rekindleTarget.Name?.TextValue}")
                            .Alternatives("Use on tank for preventive healing")
                            .Tip("Rekindle's HoT continues even after Phoenix phase ends - use it on whoever needs healing most.")
                            .Concept(SmnConcepts.AstralFlow)
                            .Record();

                        context.TrainingService.RecordConceptApplication(
                            SmnConcepts.AstralFlow, true, "Rekindle used on injured ally");
                        context.TrainingService.RecordConceptApplication(
                            SmnConcepts.PhoenixPhase, true, "Phoenix healing ability used");
                    }

                    return true;
                }
            }
            // If no one needs healing, still use it on self or lowest HP
            var lowestMember = context.PartyHelper.GetLowestHpMember(player) ?? player;
            if (context.ActionService.ExecuteOgcd(SMNActions.Rekindle, lowestMember.GameObjectId))
            {
                context.Debug.PlannedAction = SMNActions.Rekindle.Name;
                context.Debug.BuffState = "Rekindle (preventive)";
                context.MarkAstralFlowUsed();

                // Training Mode recording
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SMNActions.Rekindle.ActionId, SMNActions.Rekindle.Name)
                        .AsSummon("Phoenix")
                        .Reason("Rekindle for preventive healing",
                            "No party member is currently injured, so using Rekindle preventively on the lowest HP target. " +
                            "The HoT effect will provide value when damage comes in.")
                        .Factors("Phoenix phase active", "No urgent healing needed", $"Target: {lowestMember.Name?.TextValue}")
                        .Alternatives("Hold for upcoming damage (risky)")
                        .Tip("Always use Rekindle during Phoenix phase - don't let it go to waste.")
                        .Concept(SmnConcepts.AstralFlow)
                        .Record();

                    context.TrainingService.RecordConceptApplication(
                        SmnConcepts.AstralFlow, true, "Rekindle used preventively");
                }

                return true;
            }
            return false;
        }

        // For Deathflare/Sunflare, use on enemy target
        if (target == null)
            return false;

        if (context.ActionService.ExecuteOgcd(astralFlowAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = astralFlowAction.Name;
            context.Debug.BuffState = $"{astralFlowAction.Name} (Astral Flow)";
            context.MarkAstralFlowUsed();

            // Training Mode recording
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var demiType = context.IsBahamutActive ? "Bahamut" : "Solar Bahamut";
                var phaseConcept = context.IsBahamutActive ? SmnConcepts.BahamutPhase : SmnConcepts.SolarBahamutPhase;

                TrainingHelper.Decision(context.TrainingService)
                    .Action(astralFlowAction.ActionId, astralFlowAction.Name)
                    .AsCasterBurst()
                    .Target(target.Name?.TextValue)
                    .Reason($"{astralFlowAction.Name} during {demiType} phase",
                        $"{astralFlowAction.Name} is a high-potency AoE oGCD that can only be used once per {demiType} phase. " +
                        "Use it as soon as possible to ensure you don't lose it to phase transitions or boss jumps.")
                    .Factors($"{demiType} active", $"Timer: {context.DemiSummonTimer:F1}s")
                    .Alternatives("Wait for more enemies to spawn (risky)")
                    .Tip($"Use {astralFlowAction.Name} early in demi phase - the AoE damage is a bonus, not a requirement.")
                    .Concept(SmnConcepts.AstralFlow)
                    .Record();

                context.TrainingService.RecordConceptApplication(
                    SmnConcepts.AstralFlow, true, $"Used {astralFlowAction.Name}");
                context.TrainingService.RecordConceptApplication(
                    phaseConcept, true, $"{demiType} Astral Flow used");
            }

            return true;
        }

        return false;
    }

    private bool TryMountainBuster(IPersephoneContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < SMNActions.MountainBuster.MinLevel)
            return false;

        // Mountain Buster requires Titan's Favor buff
        if (!context.HasTitansFavor)
            return false;

        // Use Mountain Buster immediately when available
        if (context.ActionService.ExecuteOgcd(SMNActions.MountainBuster, target.GameObjectId))
        {
            context.Debug.PlannedAction = SMNActions.MountainBuster.Name;
            context.Debug.BuffState = "Mountain Buster";

            // Training Mode recording
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                TrainingHelper.Decision(context.TrainingService)
                    .Action(SMNActions.MountainBuster.ActionId, SMNActions.MountainBuster.Name)
                    .AsCasterResource("Titan's Favor", 1)
                    .Target(target.Name?.TextValue)
                    .Reason("Mountain Buster from Titan's Favor",
                        "Mountain Buster is an instant oGCD granted by Titan's Favor buff after each Topaz Rite/Catastrophe. " +
                        "Use it immediately - it's free damage that doesn't interfere with your GCD rotation.")
                    .Factors("Titan's Favor active", "Titan attunement phase")
                    .Alternatives("None - always use immediately")
                    .Tip("Weave Mountain Buster after each Topaz Rite for maximum Titan phase DPS.")
                    .Concept(SmnConcepts.MountainBuster)
                    .Record();

                context.TrainingService.RecordConceptApplication(
                    SmnConcepts.MountainBuster, true, "Mountain Buster used");
                context.TrainingService.RecordConceptApplication(
                    SmnConcepts.TitanPhase, true, "Titan Favor ability used");
            }

            return true;
        }

        return false;
    }

    private bool TrySearingLight(IPersephoneContext context)
    {
        if (!context.Configuration.Summoner.EnableSearingLight)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < SMNActions.SearingLight.MinLevel)
            return false;

        if (!context.SearingLightReady)
            return false;

        // Don't use if already active
        if (context.HasSearingLight)
            return false;

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Searing Light (phase soon)";
            return false;
        }

        // Best used during demi-summon phases for burst alignment
        // Also good to align with party buffs (2-minute windows)
        if (!context.IsDemiSummonActive)
        {
            context.Debug.BuffState = "Hold Searing Light for demi";
            return false;
        }

        // Party coordination: Synchronize with other Olympus instances
        var partyCoord = context.PartyCoordinationService;
        if (partyCoord != null && partyCoord.IsPartyCoordinationEnabled &&
            context.Configuration.PartyCoordination.EnableRaidBuffCoordination)
        {
            // Check if our buffs are aligned with remote instances
            // If significantly desynced (e.g., death recovery), use independently
            if (!partyCoord.IsRaidBuffAligned(SMNActions.SearingLight.ActionId))
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

            // Announce our intent to use Searing Light
            partyCoord.AnnounceRaidBuffIntent(SMNActions.SearingLight.ActionId);
        }

        if (context.ActionService.ExecuteOgcd(SMNActions.SearingLight, player.GameObjectId))
        {
            context.Debug.PlannedAction = SMNActions.SearingLight.Name;
            context.Debug.BuffState = "Searing Light (burst)";

            // Notify coordination service that we used the raid buff
            partyCoord?.OnRaidBuffUsed(SMNActions.SearingLight.ActionId, 120_000);

            // Training Mode recording
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var demiType = context.IsBahamutActive ? "Bahamut" :
                               context.IsPhoenixActive ? "Phoenix" :
                               context.IsSolarBahamutActive ? "Solar Bahamut" : "demi-summon";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(SMNActions.SearingLight.ActionId, SMNActions.SearingLight.Name)
                    .AsRaidBuff()
                    .Reason("Searing Light during demi-summon burst",
                        "Searing Light is a party-wide 5% damage buff on a 120s cooldown. Align it with demi-summon phases " +
                        "to maximize both your burst damage (Enkindle, Astral Flow) and party buff uptime.")
                    .Factors($"{demiType} active", "2-minute cooldown alignment", "Party buff window")
                    .Alternatives("Wait for party alignment (only if heavily desynced)")
                    .Tip("Use Searing Light at the start of demi-summon phases for maximum party value.")
                    .Concept(SmnConcepts.SearingLight)
                    .Record();

                context.TrainingService.RecordConceptApplication(
                    SmnConcepts.SearingLight, true, "Raid buff used during burst");
                context.TrainingService.RecordConceptApplication(
                    SmnConcepts.PartyCoordination, true, "Burst window coordination");
            }

            return true;
        }

        return false;
    }

    private bool TrySearingFlash(IPersephoneContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < SMNActions.SearingFlash.MinLevel)
            return false;

        // Requires Searing Light to be active
        if (!context.HasSearingLight)
            return false;

        // Check if action is ready (should be once per Searing Light)
        if (!context.ActionService.IsActionReady(SMNActions.SearingFlash.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(SMNActions.SearingFlash, target.GameObjectId))
        {
            context.Debug.PlannedAction = SMNActions.SearingFlash.Name;
            context.Debug.BuffState = "Searing Flash";

            // Training Mode recording
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                TrainingHelper.Decision(context.TrainingService)
                    .Action(SMNActions.SearingFlash.ActionId, SMNActions.SearingFlash.Name)
                    .AsCasterBurst()
                    .Target(target.Name?.TextValue)
                    .Reason("Searing Flash during Searing Light",
                        "Searing Flash is a free AoE oGCD that becomes available once per Searing Light window. " +
                        "It's instant damage that doesn't consume resources - always use it when available.")
                    .Factors("Searing Light active", $"Time remaining: {context.SearingLightRemaining:F1}s")
                    .Alternatives("None - always use during Searing Light")
                    .Tip("Searing Flash is free damage - never let a Searing Light window end without using it.")
                    .Concept(SmnConcepts.SearingFlash)
                    .Record();

                context.TrainingService.RecordConceptApplication(
                    SmnConcepts.SearingFlash, true, "Searing Flash used");
            }

            return true;
        }

        return false;
    }

    private bool TryEnergyDrain(IPersephoneContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (!context.Configuration.Summoner.EnableEnergyDrain)
            return false;

        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        if (level < SMNActions.EnergyDrain.MinLevel)
            return false;

        if (!context.EnergyDrainReady)
            return false;

        // Only use when Aetherflow is empty
        if (context.HasAetherflow)
        {
            context.Debug.BuffState = "Have Aetherflow, hold Energy Drain";
            return false;
        }

        // Count enemies for AoE version
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        var useAoe = enemyCount >= 3 && level >= SMNActions.EnergySiphon.MinLevel;
        var action = useAoe ? SMNActions.EnergySiphon : SMNActions.EnergyDrain;

        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.BuffState = $"{action.Name} (+2 Aetherflow)";

            // Training Mode recording
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsCasterResource("Aetherflow", 0)
                    .Target(target.Name?.TextValue)
                    .Reason($"{action.Name} to generate Aetherflow",
                        $"{action.Name} generates 2 Aetherflow stacks on a 60s cooldown. Use it when empty to enable " +
                        "Fester/Necrotize (single target) or Painflare (AoE) for additional burst damage.")
                    .Factors("Aetherflow stacks: 0", "Cooldown ready", useAoe ? "3+ enemies nearby" : "Single target")
                    .Alternatives("None - use when empty and ready")
                    .Tip("Use Energy Drain/Siphon on cooldown when Aetherflow is empty for consistent damage.")
                    .Concept(SmnConcepts.EnergyDrainUsage)
                    .Record();

                context.TrainingService.RecordConceptApplication(
                    SmnConcepts.EnergyDrainUsage, true, "Generated Aetherflow stacks");
                context.TrainingService.RecordConceptApplication(
                    SmnConcepts.AetherflowStacks, true, "Aetherflow refilled");
            }

            return true;
        }

        return false;
    }

    private bool TryAetherflowSpender(IPersephoneContext context, Dalamud.Game.ClientState.Objects.Types.IBattleChara? target)
    {
        if (target == null)
            return false;

        var player = context.Player;
        var level = player.Level;

        // Need Aetherflow stacks to spend
        if (!context.HasAetherflow)
            return false;

        // Prefer to spend during burst windows (demi-summon + Searing Light)
        // But always spend before Energy Drain comes off cooldown
        var energyDrainSoon = !context.EnergyDrainReady &&
                              context.ActionService.GetCooldownRemaining(SMNActions.EnergyDrain.ActionId) < 5f;

        // During burst, spend freely
        var inBurst = context.IsDemiSummonActive || context.HasSearingLight;

        // Must spend if Energy Drain is coming off cooldown soon
        if (!inBurst && !energyDrainSoon)
        {
            context.Debug.BuffState = "Hold Aetherflow for burst";
            return false;
        }

        // Count enemies for AoE decision
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        var useAoe = enemyCount >= 3;
        var action = SMNActions.GetAetherflowSpenderAoe(level);
        if (!useAoe)
            action = SMNActions.GetAetherflowSpenderST(level);

        if (level < action.MinLevel)
            return false;

        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.BuffState = $"{action.Name} (Aetherflow: {context.AetherflowStacks - 1})";

            // Training Mode recording
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var reason = inBurst ? "burst window" : "Energy Drain coming off cooldown";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsCasterResource("Aetherflow", context.AetherflowStacks)
                    .Target(target.Name?.TextValue)
                    .Reason($"{action.Name} spent during {reason}",
                        $"Aetherflow spenders ({action.Name}) deal significant oGCD damage. Prefer using them during " +
                        "burst windows (demi-summon + Searing Light) for maximum value. Always spend before Energy Drain " +
                        "comes off cooldown to avoid wasting stacks.")
                    .Factors($"Aetherflow: {context.AetherflowStacks}", inBurst ? "Burst window active" : "Energy Drain soon", useAoe ? "AoE mode" : "Single target")
                    .Alternatives(inBurst ? "None during burst" : "Wait for burst (if ED not imminent)")
                    .Tip("Spend Aetherflow during burst windows for maximum damage, but never overcap.")
                    .Concept(SmnConcepts.FesterNecrotize)
                    .Record();

                context.TrainingService.RecordConceptApplication(
                    SmnConcepts.FesterNecrotize, true, "Aetherflow spent efficiently");
                context.TrainingService.RecordConceptApplication(
                    SmnConcepts.AetherflowTiming, true, inBurst ? "Burst window spending" : "Prevented overcap");
            }

            return true;
        }

        return false;
    }

    private bool TryLucidDreaming(IPersephoneContext context)
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

            // Training Mode recording (no specific SMN concept for MP management)
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.LucidDreaming.ActionId, RoleActions.LucidDreaming.Name)
                    .AsCasterResource("MP", context.CurrentMp)
                    .Reason("Lucid Dreaming for MP recovery",
                        "Lucid Dreaming restores MP over time. Use it around 70% MP to ensure you never run dry. " +
                        "Summoner is less MP-intensive than other casters, but still benefits from consistent MP management.")
                    .Factors($"MP: {context.MpPercent * 100:F0}%", "Below 70% threshold")
                    .Alternatives("Wait until lower (risky)")
                    .Tip("Use Lucid Dreaming proactively around 70% MP to maintain consistent casting.")
                    .Concept(SmnConcepts.RuinSpells) // Using RuinSpells as closest concept for filler/resource management
                    .Record();

                context.TrainingService.RecordConceptApplication(
                    SmnConcepts.RuinSpells, true, "MP management for filler casts");
            }

            return true;
        }

        return false;
    }

    #endregion
}
