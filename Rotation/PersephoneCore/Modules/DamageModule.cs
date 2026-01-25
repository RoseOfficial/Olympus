using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.PersephoneCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.PersephoneCore.Modules;

/// <summary>
/// Handles the Summoner damage rotation.
/// Manages demi-summon phases, primal attunements, and filler spells.
/// </summary>
public sealed class DamageModule : IPersephoneModule
{
    public int Priority => 30; // Lower priority than buffs (higher number = lower priority)
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    public bool TryExecute(IPersephoneContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.DamageState = "Not in combat";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Find target
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.CasterTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return false;
        }

        // Count nearby enemies for AoE decisions
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = enemyCount;

        if (!context.CanExecuteGcd)
        {
            context.Debug.DamageState = "GCD not ready";
            return false;
        }

        var useAoe = enemyCount >= AoeThreshold;

        // === PRIORITY 1: DEMI-SUMMON PHASE GCDs ===
        if (context.IsDemiSummonActive)
        {
            if (TryDemiSummonGcd(context, target, useAoe))
                return true;
        }

        // === PRIORITY 2: PRIMAL ATTUNEMENT GCDs (Gemshine) ===
        if (context.IsIfritAttuned || context.IsTitanAttuned || context.IsGarudaAttuned)
        {
            if (TryAttunementGcd(context, target, useAoe, isMoving))
                return true;
        }

        // === PRIORITY 3: PRIMAL FAVOR ABILITIES ===
        if (TryPrimalFavor(context, target, isMoving))
            return true;

        // === PRIORITY 4: SUMMON NEXT PRIMAL ===
        if (context.PrimalsAvailable > 0 && !context.IsDemiSummonActive)
        {
            if (TrySummonPrimal(context))
                return true;
        }

        // === PRIORITY 5: SUMMON DEMI (when no primals available) ===
        if (context.PrimalsAvailable == 0 && !context.IsDemiSummonActive)
        {
            if (TrySummonDemi(context))
                return true;
        }

        // === PRIORITY 6: RUIN IV (Further Ruin proc) ===
        // Only use between phases, not during demi-summon
        if (!context.IsDemiSummonActive && context.HasFurtherRuin)
        {
            if (TryRuin4(context, target))
                return true;
        }

        // === PRIORITY 7: FILLER (Ruin III / Tri-disaster) ===
        if (TryFillerGcd(context, target, useAoe, isMoving))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(IPersephoneContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Demi-Summon Phase

    private bool TryDemiSummonGcd(IPersephoneContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        // Get the appropriate demi-summon GCD
        var action = SMNActions.GetDemiSummonGcd(
            context.IsBahamutActive,
            context.IsSolarBahamutActive,
            useAoe);

        if (level < action.MinLevel)
        {
            // Fallback to Ruin III if demi GCD not unlocked
            action = useAoe ? SMNActions.GetAoeSpell(level) : SMNActions.GetRuinSpell(level);
        }

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (Demi phase)";

            // Training Mode recording
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var demiType = context.IsBahamutActive ? "Bahamut" :
                               context.IsPhoenixActive ? "Phoenix" : "Solar Bahamut";
                var phaseConcept = context.IsBahamutActive ? SmnConcepts.BahamutPhase :
                                   context.IsPhoenixActive ? SmnConcepts.PhoenixPhase :
                                   SmnConcepts.SolarBahamutPhase;

                CasterTrainingHelper.RecordSummonDecision(
                    context.TrainingService,
                    action.ActionId,
                    action.Name,
                    demiType,
                    $"{action.Name} during {demiType} phase",
                    $"During {demiType} phase, your normal GCDs are replaced with powerful summon-specific attacks. " +
                    $"{action.Name} deals high potency while your demi-summon also attacks alongside you.",
                    new[] { $"{demiType} active", $"Timer: {context.DemiSummonTimer:F1}s", $"GCDs left: {context.DemiSummonGcdsRemaining}" },
                    new[] { "None - always use demi GCDs during demi phase" },
                    $"Maximize GCDs during demi phase - each GCD triggers your {demiType}'s attack.",
                    SmnConcepts.DemiPhases);

                context.TrainingService.RecordConceptApplication(
                    SmnConcepts.DemiPhases, true, "Demi-summon GCD used");
                context.TrainingService.RecordConceptApplication(
                    phaseConcept, true, $"{demiType} GCD rotation");
            }

            return true;
        }

        return false;
    }

    #endregion

    #region Primal Attunement

    private bool TryAttunementGcd(IPersephoneContext context, IBattleChara target, bool useAoe, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        // Get Gemshine action based on current attunement
        var action = SMNActions.GetGemshinAction(context.CurrentAttunement, useAoe);
        if (action == null)
            return false;

        // Ruby Rite has a cast time - check for movement
        if (context.IsIfritAttuned && isMoving && !context.HasInstantCast && !context.CanSlidecast)
        {
            // Use Swiftcast for Ruby Rite if moving
            if (context.SwiftcastReady)
            {
                if (context.ActionService.ExecuteOgcd(SMNActions.Swiftcast, player.GameObjectId))
                {
                    context.Debug.DamageState = "Swiftcast for Ruby";
                    return true;
                }
            }
            // Otherwise, skip and use movement filler
            context.Debug.DamageState = "Moving, need instant for Ruby";
            return false;
        }

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} ({context.Debug.AttunementName} {context.AttunementStacks - 1})";

            // Training Mode recording
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var primalType = context.IsIfritAttuned ? "Ifrit" :
                                 context.IsTitanAttuned ? "Titan" : "Garuda";
                var phaseConcept = context.IsIfritAttuned ? SmnConcepts.IfritPhase :
                                   context.IsTitanAttuned ? SmnConcepts.TitanPhase :
                                   SmnConcepts.GarudaPhase;
                var stacksRemaining = context.AttunementStacks - 1;

                CasterTrainingHelper.RecordDamageDecision(
                    context.TrainingService,
                    action.ActionId,
                    action.Name,
                    target.Name?.TextValue,
                    $"{action.Name} - {primalType} attunement",
                    $"Gemshine attacks ({action.Name}) consume attunement stacks. Each primal has a unique attack pattern: " +
                    "Ifrit has 2 hard-hitting casts, Titan has 4 instant attacks, Garuda has 4 casted attacks with DoT.",
                    new[] { $"{primalType} attuned", $"Stacks: {context.AttunementStacks} → {stacksRemaining}", $"Timer: {context.AttunementTimer:F1}s" },
                    new[] { "None - spend all attunement stacks" },
                    $"Use all {primalType} attunement stacks before summoning the next primal.",
                    SmnConcepts.AttunementSystem);

                context.TrainingService.RecordConceptApplication(
                    SmnConcepts.AttunementSystem, true, "Attunement stack spent");
                context.TrainingService.RecordConceptApplication(
                    phaseConcept, true, $"{primalType} Gemshine used");
            }

            return true;
        }

        return false;
    }

    #endregion

    #region Primal Favor Abilities

    private bool TryPrimalFavor(IPersephoneContext context, IBattleChara target, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        // Crimson Cyclone + Strike (Ifrit's Favor) - gap closer combo
        if (context.HasIfritsFavor && level >= SMNActions.CrimsonCyclone.MinLevel)
        {
            // Crimson Cyclone is a gap closer, safe to use when moving
            if (context.ActionService.ExecuteGcd(SMNActions.CrimsonCyclone, target.GameObjectId))
            {
                context.Debug.PlannedAction = SMNActions.CrimsonCyclone.Name;
                context.Debug.DamageState = "Crimson Cyclone (gap closer)";

                // Training Mode recording
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    CasterTrainingHelper.RecordDamageDecision(
                        context.TrainingService,
                        SMNActions.CrimsonCyclone.ActionId,
                        SMNActions.CrimsonCyclone.Name,
                        target.Name?.TextValue,
                        "Crimson Cyclone - Ifrit's Favor gap closer",
                        "Crimson Cyclone is a gap closer that dashes to the target and grants Crimson Strike as a follow-up. " +
                        "It's instant cast, making it excellent for movement. Always follow up with Crimson Strike.",
                        new[] { "Ifrit's Favor active", "Gap closer + instant" },
                        new[] { "None - always use when available" },
                        "Use Crimson Cyclone for movement - it's a gap closer that doesn't interrupt your rotation.",
                        SmnConcepts.CrimsonCyclone);

                    context.TrainingService.RecordConceptApplication(
                        SmnConcepts.CrimsonCyclone, true, "Ifrit gap closer used");
                    context.TrainingService.RecordConceptApplication(
                        SmnConcepts.FavorTiming, true, "Ifrit's Favor ability used");
                }

                return true;
            }
        }

        // Crimson Strike follow-up (after Crimson Cyclone)
        // This is handled by the game automatically when Crimson Cyclone changes to Strike

        // Slipstream (Garuda's Favor) - channeled ability
        if (context.HasGarudasFavor && level >= SMNActions.Slipstream.MinLevel)
        {
            // Slipstream is a cast, check for movement
            if (isMoving && !context.HasInstantCast && !context.CanSlidecast)
            {
                // Use Swiftcast for Slipstream if available
                if (context.SwiftcastReady)
                {
                    if (context.ActionService.ExecuteOgcd(SMNActions.Swiftcast, player.GameObjectId))
                    {
                        context.Debug.DamageState = "Swiftcast for Slipstream";

                        // Training Mode recording
                        if (context.TrainingService?.IsTrainingEnabled == true)
                        {
                            CasterTrainingHelper.RecordMovementDecision(
                                context.TrainingService,
                                SMNActions.Swiftcast.ActionId,
                                SMNActions.Swiftcast.Name,
                                target.Name?.TextValue,
                                "Swiftcast for Slipstream while moving",
                                "Slipstream has a long cast time. Using Swiftcast allows you to use it while moving, " +
                                "ensuring you don't lose Garuda's Favor buff to movement requirements.",
                                new[] { "Garuda's Favor active", "Currently moving", "No instant cast ready" },
                                new[] { "Wait to stop moving (may lose buff)" },
                                "Swiftcast is valuable for Slipstream during movement-heavy phases.",
                                SmnConcepts.Slipstream);

                            context.TrainingService.RecordConceptApplication(
                                SmnConcepts.Slipstream, true, "Swiftcast for movement");
                        }

                        return true;
                    }
                }
                context.Debug.DamageState = "Moving, hold Slipstream";
                return false;
            }

            if (context.ActionService.ExecuteGcd(SMNActions.Slipstream, target.GameObjectId))
            {
                context.Debug.PlannedAction = SMNActions.Slipstream.Name;
                context.Debug.DamageState = "Slipstream";

                // Training Mode recording
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    CasterTrainingHelper.RecordDamageDecision(
                        context.TrainingService,
                        SMNActions.Slipstream.ActionId,
                        SMNActions.Slipstream.Name,
                        target.Name?.TextValue,
                        "Slipstream - Garuda's Favor DoT zone",
                        "Slipstream places a ground DoT that deals damage over time to enemies in the area. " +
                        "It's Garuda's signature ability and provides sustained damage after the initial hit.",
                        new[] { "Garuda's Favor active", context.HasSwiftcast ? "Instant via Swiftcast" : "Stationary to cast" },
                        new[] { "None - always use Slipstream" },
                        "Position Slipstream where enemies will stay - the DoT provides significant damage.",
                        SmnConcepts.Slipstream);

                    context.TrainingService.RecordConceptApplication(
                        SmnConcepts.Slipstream, true, "Garuda DoT zone placed");
                    context.TrainingService.RecordConceptApplication(
                        SmnConcepts.FavorTiming, true, "Garuda's Favor ability used");
                }

                return true;
            }
        }

        return false;
    }

    #endregion

    #region Summon Primal

    private bool TrySummonPrimal(IPersephoneContext context)
    {
        var player = context.Player;
        var level = player.Level;

        // Don't summon if we're in attunement or demi phase
        if (context.IsDemiSummonActive || context.AttunementStacks > 0)
            return false;

        // Primal priority for opener: Titan > Garuda > Ifrit
        // Titan first for instant GCDs during burst
        // After opener, order matters less but Titan is good for movement

        if (context.CanSummonTitan && level >= SMNActions.SummonTitan.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(SMNActions.SummonTitan, player.GameObjectId))
            {
                context.Debug.PlannedAction = "Summon Titan";
                context.Debug.DamageState = "Summon Titan";

                // Training Mode recording
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    RecordPrimalSummon(context, "Titan", SmnConcepts.TitanPhase,
                        "Titan has 4 instant Topaz Rite attacks plus Mountain Buster oGCDs. " +
                        "Excellent for movement phases due to all instant casts.");
                }

                return true;
            }
        }

        if (context.CanSummonGaruda && level >= SMNActions.SummonGaruda.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(SMNActions.SummonGaruda, player.GameObjectId))
            {
                context.Debug.PlannedAction = "Summon Garuda";
                context.Debug.DamageState = "Summon Garuda";

                // Training Mode recording
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    RecordPrimalSummon(context, "Garuda", SmnConcepts.GarudaPhase,
                        "Garuda has 4 casted Emerald Rite attacks plus Slipstream ground DoT. " +
                        "Best for stationary phases due to cast times.");
                }

                return true;
            }
        }

        if (context.CanSummonIfrit && level >= SMNActions.SummonIfrit.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(SMNActions.SummonIfrit, player.GameObjectId))
            {
                context.Debug.PlannedAction = "Summon Ifrit";
                context.Debug.DamageState = "Summon Ifrit";

                // Training Mode recording
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    RecordPrimalSummon(context, "Ifrit", SmnConcepts.IfritPhase,
                        "Ifrit has 2 high-potency casted Ruby Rite attacks plus Crimson Cyclone gap closer. " +
                        "Highest potency per attack but requires stationary casting.");
                }

                return true;
            }
        }

        return false;
    }

    private void RecordPrimalSummon(IPersephoneContext context, string primalName, string phaseConcept, string description)
    {
        CasterTrainingHelper.RecordSummonDecision(
            context.TrainingService,
            0, // Action ID varies by primal
            $"Summon {primalName}",
            primalName,
            $"Summoning {primalName} primal",
            description,
            new[] { $"{primalName} available", $"Primals remaining: {context.PrimalsAvailable}" },
            new[] { "Summon different primal based on fight needs" },
            "Summon order matters: Titan for movement, Garuda for stationary, Ifrit for burst.",
            SmnConcepts.PrimalOrder);

        context.TrainingService?.RecordConceptApplication(
            SmnConcepts.PrimalOrder, true, $"Summoned {primalName}");
        context.TrainingService?.RecordConceptApplication(
            phaseConcept, true, $"{primalName} phase started");
    }

    #endregion

    #region Summon Demi

    private bool TrySummonDemi(IPersephoneContext context)
    {
        var player = context.Player;
        var level = player.Level;

        // Don't summon if we still have primals or attunement
        if (context.PrimalsAvailable > 0 || context.AttunementStacks > 0)
            return false;

        // Check which demi-summon is available
        // At level 100, Solar Bahamut replaces every other Bahamut
        if (level >= SMNActions.SummonSolarBahamut.MinLevel)
        {
            // Try Solar Bahamut first (the game handles the alternation)
            if (context.ActionService.IsActionReady(SMNActions.SummonSolarBahamut.ActionId))
            {
                if (context.ActionService.ExecuteGcd(SMNActions.SummonSolarBahamut, player.GameObjectId))
                {
                    context.Debug.PlannedAction = SMNActions.SummonSolarBahamut.Name;
                    context.Debug.DamageState = "Summon Solar Bahamut";

                    // Training Mode recording
                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        RecordDemiSummon(context, "Solar Bahamut", SmnConcepts.SolarBahamutPhase,
                            "Solar Bahamut is the enhanced version of Bahamut at level 100. " +
                            "It alternates with Phoenix and provides Sunflare (AoE Astral Flow) instead of Deathflare.");
                    }

                    return true;
                }
            }
        }

        // Phoenix (Lv.80+)
        if (level >= SMNActions.SummonPhoenix.MinLevel)
        {
            if (context.ActionService.IsActionReady(SMNActions.SummonPhoenix.ActionId))
            {
                if (context.ActionService.ExecuteGcd(SMNActions.SummonPhoenix, player.GameObjectId))
                {
                    context.Debug.PlannedAction = SMNActions.SummonPhoenix.Name;
                    context.Debug.DamageState = "Summon Phoenix";

                    // Training Mode recording
                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        RecordDemiSummon(context, "Phoenix", SmnConcepts.PhoenixPhase,
                            "Phoenix provides Rekindle (healing Astral Flow) and replaces Bahamut every other summon. " +
                            "The healing utility makes it valuable during high-damage phases.");
                    }

                    return true;
                }
            }
        }

        // Bahamut (Lv.70+)
        if (level >= SMNActions.SummonBahamut.MinLevel)
        {
            if (context.ActionService.IsActionReady(SMNActions.SummonBahamut.ActionId))
            {
                if (context.ActionService.ExecuteGcd(SMNActions.SummonBahamut, player.GameObjectId))
                {
                    context.Debug.PlannedAction = SMNActions.SummonBahamut.Name;
                    context.Debug.DamageState = "Summon Bahamut";

                    // Training Mode recording
                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        RecordDemiSummon(context, "Bahamut", SmnConcepts.BahamutPhase,
                            "Bahamut is your primary demi-summon that provides Deathflare (AoE Astral Flow). " +
                            "Align Searing Light with Bahamut for maximum burst damage.");
                    }

                    return true;
                }
            }
        }

        return false;
    }

    private void RecordDemiSummon(IPersephoneContext context, string demiName, string phaseConcept, string description)
    {
        CasterTrainingHelper.RecordSummonDecision(
            context.TrainingService,
            0, // Action ID varies by demi
            $"Summon {demiName}",
            demiName,
            $"Summoning {demiName} for burst phase",
            description,
            new[] { "All primals used", "Demi-summon ready", "15-second burst window" },
            new[] { "Hold for better timing (risky in most cases)" },
            $"Summon {demiName} immediately when ready - the 15-second window is your biggest burst.",
            SmnConcepts.DemiPhases,
            ExplanationPriority.High);

        context.TrainingService?.RecordConceptApplication(
            SmnConcepts.DemiPhases, true, $"Summoned {demiName}");
        context.TrainingService?.RecordConceptApplication(
            phaseConcept, true, $"{demiName} burst phase started");
    }

    #endregion

    #region Ruin IV

    private bool TryRuin4(IPersephoneContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < SMNActions.Ruin4.MinLevel)
            return false;

        if (!context.HasFurtherRuin)
            return false;

        // Don't use during demi-summon phase (waste of GCDs)
        if (context.IsDemiSummonActive)
            return false;

        // Use if Further Ruin is about to expire
        if (context.FurtherRuinRemaining < 5f)
        {
            if (context.ActionService.ExecuteGcd(SMNActions.Ruin4, target.GameObjectId))
            {
                context.Debug.PlannedAction = SMNActions.Ruin4.Name;
                context.Debug.DamageState = $"Ruin IV (expiring: {context.FurtherRuinRemaining:F1}s)";

                // Training Mode recording
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    CasterTrainingHelper.RecordProcDecision(
                        context.TrainingService,
                        SMNActions.Ruin4.ActionId,
                        SMNActions.Ruin4.Name,
                        "Further Ruin",
                        target.Name?.TextValue,
                        "Ruin IV - proc expiring soon",
                        "Further Ruin procs are granted by Energy Drain/Siphon and expire after 60 seconds. " +
                        "Using Ruin IV before it expires ensures you don't waste the proc. It's also instant cast.",
                        new[] { $"Proc expiring: {context.FurtherRuinRemaining:F1}s", "Instant cast" },
                        new[] { "None - always use before expiring" },
                        "Track Further Ruin duration - use before it expires to avoid wasting procs.",
                        SmnConcepts.RuinIvProcs);

                    context.TrainingService.RecordConceptApplication(
                        SmnConcepts.RuinIvProcs, true, "Used expiring proc");
                }

                return true;
            }
        }

        // Use as filler between phases
        if (!context.IsDemiSummonActive && context.PrimalsAvailable == 0 && context.AttunementStacks == 0)
        {
            if (context.ActionService.ExecuteGcd(SMNActions.Ruin4, target.GameObjectId))
            {
                context.Debug.PlannedAction = SMNActions.Ruin4.Name;
                context.Debug.DamageState = "Ruin IV (filler)";

                // Training Mode recording
                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    CasterTrainingHelper.RecordProcDecision(
                        context.TrainingService,
                        SMNActions.Ruin4.ActionId,
                        SMNActions.Ruin4.Name,
                        "Further Ruin",
                        target.Name?.TextValue,
                        "Ruin IV as filler between phases",
                        "Between primal and demi-summon phases, use Ruin IV procs as filler. " +
                        "It's higher potency than Ruin III and instant cast, making it ideal filler.",
                        new[] { "No primals available", "No demi active", "Waiting for summons" },
                        new[] { "Use Ruin III if no procs" },
                        "Save Ruin IV for filler windows - it's your best non-burst GCD.",
                        SmnConcepts.RuinIvProcs);

                    context.TrainingService.RecordConceptApplication(
                        SmnConcepts.RuinIvProcs, true, "Filler Ruin IV used");
                }

                return true;
            }
        }

        return false;
    }

    #endregion

    #region Filler GCDs

    private bool TryFillerGcd(IPersephoneContext context, IBattleChara target, bool useAoe, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        // Ruin III has a cast time - use Ruin II for movement if needed
        if (isMoving && !context.HasInstantCast && !context.CanSlidecast)
        {
            // Use Ruin IV if available (instant)
            if (context.HasFurtherRuin && level >= SMNActions.Ruin4.MinLevel)
            {
                if (context.ActionService.ExecuteGcd(SMNActions.Ruin4, target.GameObjectId))
                {
                    context.Debug.PlannedAction = SMNActions.Ruin4.Name;
                    context.Debug.DamageState = "Ruin IV (movement)";

                    // Training Mode recording
                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        CasterTrainingHelper.RecordMovementDecision(
                            context.TrainingService,
                            SMNActions.Ruin4.ActionId,
                            SMNActions.Ruin4.Name,
                            target.Name?.TextValue,
                            "Ruin IV for movement",
                            "Using Ruin IV proc during movement. It's instant cast and higher potency than Ruin II, " +
                            "making it your best movement option when available.",
                            new[] { "Moving", "Further Ruin proc active", "Instant cast" },
                            new[] { "Ruin II if no proc" },
                            "Prioritize Ruin IV procs for movement - save them when you know movement is coming.",
                            SmnConcepts.RuinIvProcs);

                        context.TrainingService.RecordConceptApplication(
                            SmnConcepts.RuinIvProcs, true, "Movement Ruin IV");
                    }

                    return true;
                }
            }

            // Use Ruin II for movement (instant, lower potency)
            if (level >= SMNActions.Ruin2.MinLevel)
            {
                if (context.ActionService.ExecuteGcd(SMNActions.Ruin2, target.GameObjectId))
                {
                    context.Debug.PlannedAction = SMNActions.Ruin2.Name;
                    context.Debug.DamageState = "Ruin II (movement)";

                    // Training Mode recording
                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        CasterTrainingHelper.RecordMovementDecision(
                            context.TrainingService,
                            SMNActions.Ruin2.ActionId,
                            SMNActions.Ruin2.Name,
                            target.Name?.TextValue,
                            "Ruin II for movement (no procs)",
                            "Using Ruin II during movement because no Ruin IV procs are available. " +
                            "Ruin II is lower potency but instant, maintaining GCD uptime.",
                            new[] { "Moving", "No instant procs available" },
                            new[] { "None - last resort movement option" },
                            "Try to have Ruin IV procs or primal movement options for heavy movement phases.",
                            SmnConcepts.RuinSpells);

                        context.TrainingService.RecordConceptApplication(
                            SmnConcepts.RuinSpells, false, "Had to use Ruin II for movement");
                    }

                    return true;
                }
            }
        }

        // Standard filler
        var action = useAoe ? SMNActions.GetAoeSpell(level) : SMNActions.GetRuinSpell(level);

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = action.Name;

            // Training Mode recording
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var concept = useAoe ? SmnConcepts.AoeRotation : SmnConcepts.RuinSpells;
                CasterTrainingHelper.RecordDamageDecision(
                    context.TrainingService,
                    action.ActionId,
                    action.Name,
                    target.Name?.TextValue,
                    useAoe ? "AoE filler spell" : "Single target filler",
                    useAoe
                        ? "Using AoE Ruin spell as filler between primal/demi phases. At 3+ enemies, AoE is more efficient."
                        : "Using Ruin III as filler between primal and demi-summon phases. This is your lowest priority GCD.",
                    new[] { useAoe ? $"{context.TargetingService.CountEnemiesInRange(5f, context.Player)} enemies" : "Single target", "No summons active" },
                    new[] { "Use Ruin IV if proc available" },
                    useAoe
                        ? "Switch to AoE Ruin at 3+ enemies for better efficiency."
                        : "Minimize Ruin III usage by optimizing summon uptime.",
                    concept,
                    ExplanationPriority.Low);

                context.TrainingService.RecordConceptApplication(
                    concept, true, "Filler GCD used");
            }

            return true;
        }

        return false;
    }

    #endregion
}
