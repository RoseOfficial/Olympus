using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.PersephoneCore.Context;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Services.Training;

namespace Olympus.Rotation.PersephoneCore.Modules;

/// <summary>
/// Handles the Summoner damage rotation.
/// Manages demi-summon phases, primal attunements, and filler spells.
/// </summary>
public sealed class DamageModule : BaseDpsDamageModule<IPersephoneContext>, IPersephoneModule
{
    public DamageModule(IBurstWindowService? burstWindowService = null, ISmartAoEService? smartAoEService = null) : base(burstWindowService, smartAoEService) { }

    #region Abstract Method Implementations

    protected override float GetTargetingRange() => FFXIVConstants.CasterTargetingRange;

    protected override float GetAoECountRange() => 5f;

    protected override void SetDamageState(IPersephoneContext context, string state) =>
        context.Debug.DamageState = state;

    protected override void SetNearbyEnemies(IPersephoneContext context, int count) =>
        context.Debug.NearbyEnemies = count;

    protected override void SetPlannedAction(IPersephoneContext context, string action) =>
        context.Debug.PlannedAction = action;

    protected override bool IsAoEEnabled(IPersephoneContext context) =>
        context.Configuration.Summoner.EnableAoERotation;

    protected override int GetConfiguredAoEThreshold(IPersephoneContext context) =>
        context.Configuration.Summoner.AoEMinTargets;

    /// <summary>
    /// SMN has no damage oGCDs - all abilities are in the GCD phase or BuffModule.
    /// </summary>
    protected override bool TryOgcdDamage(IPersephoneContext context, IBattleChara target, int enemyCount)
    {
        return false;
    }

    protected override bool TryGcdDamage(IPersephoneContext context, IBattleChara target, int enemyCount, bool isMoving)
    {
        var useAoe = ShouldUseAoE(enemyCount);

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

        return false;
    }

    #endregion

    #region Demi-Summon Phase

    private bool TryDemiSummonGcd(IPersephoneContext context, IBattleChara target, bool useAoe)
    {
        if (context.IsBahamutActive && !context.Configuration.Summoner.EnableBahamut)
            return false;
        if (context.IsBahamutActive && !context.Configuration.Summoner.EnableAstralAbilities)
            return false;
        if (context.IsPhoenixActive && !context.Configuration.Summoner.EnablePhoenix)
            return false;
        if (context.IsPhoenixActive && !context.Configuration.Summoner.EnableFountainAbilities)
            return false;
        if (context.IsSolarBahamutActive && !context.Configuration.Summoner.EnableSolarBahamut)
            return false;

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

                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsSummon(demiType)
                    .Target(target.Name?.TextValue)
                    .Reason($"{action.Name} during {demiType} phase",
                        $"During {demiType} phase, your normal GCDs are replaced with powerful summon-specific attacks. " +
                        $"{action.Name} deals high potency while your demi-summon also attacks alongside you.")
                    .Factors($"{demiType} active", $"Timer: {context.DemiSummonTimer:F1}s", $"GCDs left: {context.DemiSummonGcdsRemaining}")
                    .Alternatives("None - always use demi GCDs during demi phase")
                    .Tip($"Maximize GCDs during demi phase - each GCD triggers your {demiType}'s attack.")
                    .Concept(SmnConcepts.DemiPhases)
                    .Record();

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
        if (!context.Configuration.Summoner.EnablePrimalAbilities)
            return false;

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
                if (context.ActionService.ExecuteOgcd(RoleActions.Swiftcast, player.GameObjectId))
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

                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsCasterDamage()
                    .Target(target.Name?.TextValue)
                    .Reason($"{action.Name} - {primalType} attunement",
                        $"Gemshine attacks ({action.Name}) consume attunement stacks. Each primal has a unique attack pattern: " +
                        "Ifrit has 2 hard-hitting casts, Titan has 4 instant attacks, Garuda has 4 casted attacks with DoT.")
                    .Factors($"{primalType} attuned", $"Stacks: {context.AttunementStacks} → {stacksRemaining}", $"Timer: {context.AttunementTimer:F1}s")
                    .Alternatives("None - spend all attunement stacks")
                    .Tip($"Use all {primalType} attunement stacks before summoning the next primal.")
                    .Concept(SmnConcepts.AttunementSystem)
                    .Record();

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
        if (!context.Configuration.Summoner.EnablePrimalAbilities)
            return false;

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
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SMNActions.CrimsonCyclone.ActionId, SMNActions.CrimsonCyclone.Name)
                        .AsCasterDamage()
                        .Target(target.Name?.TextValue)
                        .Reason("Crimson Cyclone - Ifrit's Favor gap closer",
                            "Crimson Cyclone is a gap closer that dashes to the target and grants Crimson Strike as a follow-up. " +
                            "It's instant cast, making it excellent for movement. Always follow up with Crimson Strike.")
                        .Factors("Ifrit's Favor active", "Gap closer + instant")
                        .Alternatives("None - always use when available")
                        .Tip("Use Crimson Cyclone for movement - it's a gap closer that doesn't interrupt your rotation.")
                        .Concept(SmnConcepts.CrimsonCyclone)
                        .Record();

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
                    if (context.ActionService.ExecuteOgcd(RoleActions.Swiftcast, player.GameObjectId))
                    {
                        context.Debug.DamageState = "Swiftcast for Slipstream";

                        // Training Mode recording
                        if (context.TrainingService?.IsTrainingEnabled == true)
                        {
                            TrainingHelper.Decision(context.TrainingService)
                                .Action(RoleActions.Swiftcast.ActionId, RoleActions.Swiftcast.Name)
                                .AsMovement()
                                .Target(target.Name?.TextValue)
                                .Reason("Swiftcast for Slipstream while moving",
                                    "Slipstream has a long cast time. Using Swiftcast allows you to use it while moving, " +
                                    "ensuring you don't lose Garuda's Favor buff to movement requirements.")
                                .Factors("Garuda's Favor active", "Currently moving", "No instant cast ready")
                                .Alternatives("Wait to stop moving (may lose buff)")
                                .Tip("Swiftcast is valuable for Slipstream during movement-heavy phases.")
                                .Concept(SmnConcepts.Slipstream)
                                .Record();

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
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SMNActions.Slipstream.ActionId, SMNActions.Slipstream.Name)
                        .AsCasterDamage()
                        .Target(target.Name?.TextValue)
                        .Reason("Slipstream - Garuda's Favor DoT zone",
                            "Slipstream places a ground DoT that deals damage over time to enemies in the area. " +
                            "It's Garuda's signature ability and provides sustained damage after the initial hit.")
                        .Factors("Garuda's Favor active", context.HasSwiftcast ? "Instant via Swiftcast" : "Stationary to cast")
                        .Alternatives("None - always use Slipstream")
                        .Tip("Position Slipstream where enemies will stay - the DoT provides significant damage.")
                        .Concept(SmnConcepts.Slipstream)
                        .Record();

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

        if (context.Configuration.Summoner.EnableTitan && context.CanSummonTitan && level >= SMNActions.SummonTitan.MinLevel)
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

        if (context.Configuration.Summoner.EnableGaruda && context.CanSummonGaruda && level >= SMNActions.SummonGaruda.MinLevel)
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

        if (context.Configuration.Summoner.EnableIfrit && context.CanSummonIfrit && level >= SMNActions.SummonIfrit.MinLevel)
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
        TrainingHelper.Decision(context.TrainingService)
            .Action(0, $"Summon {primalName}") // Action ID varies by primal
            .AsSummon(primalName)
            .Reason($"Summoning {primalName} primal", description)
            .Factors($"{primalName} available", $"Primals remaining: {context.PrimalsAvailable}")
            .Alternatives("Summon different primal based on fight needs")
            .Tip("Summon order matters: Titan for movement, Garuda for stationary, Ifrit for burst.")
            .Concept(SmnConcepts.PrimalOrder)
            .Record();

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
        // Note: Skip IsActionReady for demi summons — GetCurrentCharges doesn't reliably
        // report availability for replacement actions (Phoenix/Solar Bahamut).
        // ExecuteGcd checks GetActionStatus internally, which handles replacements correctly.
        if (context.Configuration.Summoner.EnableSolarBahamut && level >= SMNActions.SummonSolarBahamut.MinLevel)
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

        // Phoenix (Lv.80+)
        if (context.Configuration.Summoner.EnablePhoenix && level >= SMNActions.SummonPhoenix.MinLevel)
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

        // Bahamut (Lv.70+)
        if (context.Configuration.Summoner.EnableBahamut && level >= SMNActions.SummonBahamut.MinLevel)
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

        // Aethercharge (Lv.6-69) — resets primals without summoning a demi
        if (level >= SMNActions.Aethercharge.MinLevel && level < SMNActions.SummonBahamut.MinLevel)
        {
            if (context.ActionService.ExecuteGcd(SMNActions.Aethercharge, player.GameObjectId))
            {
                context.Debug.PlannedAction = SMNActions.Aethercharge.Name;
                context.Debug.DamageState = "Aethercharge (reset primals)";
                return true;
            }
        }

        return false;
    }

    private void RecordDemiSummon(IPersephoneContext context, string demiName, string phaseConcept, string description)
    {
        TrainingHelper.Decision(context.TrainingService)
            .Action(0, $"Summon {demiName}") // Action ID varies by demi
            .AsSummon(demiName)
            .Priority(ExplanationPriority.High)
            .Reason($"Summoning {demiName} for burst phase", description)
            .Factors("All primals used", "Demi-summon ready", "15-second burst window")
            .Alternatives("Hold for better timing (risky in most cases)")
            .Tip($"Summon {demiName} immediately when ready - the 15-second window is your biggest burst.")
            .Concept(SmnConcepts.DemiPhases)
            .Record();

        context.TrainingService?.RecordConceptApplication(
            SmnConcepts.DemiPhases, true, $"Summoned {demiName}");
        context.TrainingService?.RecordConceptApplication(
            phaseConcept, true, $"{demiName} burst phase started");
    }

    #endregion

    #region Ruin IV

    private bool TryRuin4(IPersephoneContext context, IBattleChara target)
    {
        if (!context.Configuration.Summoner.EnableRuinIV)
            return false;

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
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SMNActions.Ruin4.ActionId, SMNActions.Ruin4.Name)
                        .AsCasterProc("Further Ruin")
                        .Target(target.Name?.TextValue)
                        .Reason("Ruin IV - proc expiring soon",
                            "Further Ruin procs are granted by Energy Drain/Siphon and expire after 60 seconds. " +
                            "Using Ruin IV before it expires ensures you don't waste the proc. It's also instant cast.")
                        .Factors($"Proc expiring: {context.FurtherRuinRemaining:F1}s", "Instant cast")
                        .Alternatives("None - always use before expiring")
                        .Tip("Track Further Ruin duration - use before it expires to avoid wasting procs.")
                        .Concept(SmnConcepts.RuinIvProcs)
                        .Record();

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
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(SMNActions.Ruin4.ActionId, SMNActions.Ruin4.Name)
                        .AsCasterProc("Further Ruin")
                        .Target(target.Name?.TextValue)
                        .Reason("Ruin IV as filler between phases",
                            "Between primal and demi-summon phases, use Ruin IV procs as filler. " +
                            "It's higher potency than Ruin III and instant cast, making it ideal filler.")
                        .Factors("No primals available", "No demi active", "Waiting for summons")
                        .Alternatives("Use Ruin III if no procs")
                        .Tip("Save Ruin IV for filler windows - it's your best non-burst GCD.")
                        .Concept(SmnConcepts.RuinIvProcs)
                        .Record();

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
        if (!context.Configuration.Summoner.EnableRuin)
            return false;

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
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(SMNActions.Ruin4.ActionId, SMNActions.Ruin4.Name)
                            .AsMovement()
                            .Target(target.Name?.TextValue)
                            .Reason("Ruin IV for movement",
                                "Using Ruin IV proc during movement. It's instant cast and higher potency than Ruin II, " +
                                "making it your best movement option when available.")
                            .Factors("Moving", "Further Ruin proc active", "Instant cast")
                            .Alternatives("Ruin II if no proc")
                            .Tip("Prioritize Ruin IV procs for movement - save them when you know movement is coming.")
                            .Concept(SmnConcepts.RuinIvProcs)
                            .Record();

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
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(SMNActions.Ruin2.ActionId, SMNActions.Ruin2.Name)
                            .AsMovement()
                            .Target(target.Name?.TextValue)
                            .Reason("Ruin II for movement (no procs)",
                                "Using Ruin II during movement because no Ruin IV procs are available. " +
                                "Ruin II is lower potency but instant, maintaining GCD uptime.")
                            .Factors("Moving", "No instant procs available")
                            .Alternatives("None - last resort movement option")
                            .Tip("Try to have Ruin IV procs or primal movement options for heavy movement phases.")
                            .Concept(SmnConcepts.RuinSpells)
                            .Record();

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
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsCasterDamage()
                    .Priority(ExplanationPriority.Low)
                    .Target(target.Name?.TextValue)
                    .Reason(useAoe ? "AoE filler spell" : "Single target filler",
                        useAoe
                            ? "Using AoE Ruin spell as filler between primal/demi phases. At 3+ enemies, AoE is more efficient."
                            : "Using Ruin III as filler between primal and demi-summon phases. This is your lowest priority GCD.")
                    .Factors(useAoe ? $"{context.TargetingService.CountEnemiesInRange(5f, context.Player)} enemies" : "Single target", "No summons active")
                    .Alternatives("Use Ruin IV if proc available")
                    .Tip(useAoe
                        ? "Switch to AoE Ruin at 3+ enemies for better efficiency."
                        : "Minimize Ruin III usage by optimizing summon uptime.")
                    .Concept(concept)
                    .Record();

                context.TrainingService.RecordConceptApplication(
                    concept, true, "Filler GCD used");
            }

            return true;
        }

        return false;
    }

    #endregion
}
