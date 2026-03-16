using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.KratosCore.Context;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.KratosCore.Modules;

/// <summary>
/// Handles the Monk damage rotation.
/// Manages form cycling, positional optimization, Chakra spending, and burst windows.
/// Extends BaseDpsDamageModule for shared damage module patterns.
/// </summary>
public sealed class DamageModule : BaseDpsDamageModule<IKratosContext>, IKratosModule
{
    public DamageModule(IBurstWindowService? burstWindowService = null) : base(burstWindowService) { }

    #region Abstract Method Implementations

    /// <summary>
    /// Melee targeting range (3y) — used as fallback only.
    /// </summary>
    protected override float GetTargetingRange() => FFXIVConstants.MeleeTargetingRange;

    /// <summary>
    /// Use Bootshine to check melee range via game API for maximum accuracy.
    /// </summary>
    protected override uint GetRangeCheckActionId() => MNKActions.Bootshine.ActionId;

    /// <summary>
    /// AoE count range for MNK (5y for melee AoE abilities).
    /// </summary>
    protected override float GetAoECountRange() => 5f;

    /// <summary>
    /// Sets the damage state in the debug display.
    /// </summary>
    protected override void SetDamageState(IKratosContext context, string state) =>
        context.Debug.DamageState = state;

    /// <summary>
    /// Sets the nearby enemy count in the debug display.
    /// </summary>
    protected override void SetNearbyEnemies(IKratosContext context, int count) =>
        context.Debug.NearbyEnemies = count;

    /// <summary>
    /// Sets the planned action name in the debug display.
    /// </summary>
    protected override void SetPlannedAction(IKratosContext context, string action) =>
        context.Debug.PlannedAction = action;

    protected override bool IsAoEEnabled(IKratosContext context) =>
        context.Configuration.Monk.EnableAoERotation;

    protected override int GetConfiguredAoEThreshold(IKratosContext context) =>
        context.Configuration.Monk.AoEMinTargets;

    /// <summary>
    /// oGCD damage for Monk - Chakra spenders and Thunderclap.
    /// </summary>
    protected override bool TryOgcdDamage(IKratosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Priority 1: The Forbidden Chakra / Enlightenment (5 Chakra)
        if (TryChakraSpender(context, target, enemyCount))
            return true;

        // Priority 2: Thunderclap (gap closer)
        if (TryThunderclap(context, target))
            return true;

        return false;
    }

    private bool TryChakraSpender(IKratosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Need 5 Chakra to spend
        if (context.Chakra < 5)
            return false;

        // Hold Chakra for burst when not at risk of capping (50 = max chakra, hold below 45 if burst imminent)
        if (context.Configuration.Monk.EnableBurstPooling && ShouldHoldForBurst(8f) && context.Chakra < 45)
            return false;

        // Choose ST or AoE based on enemy count
        if (enemyCount >= AoeThreshold && level >= MNKActions.HowlingFist.MinLevel)
        {
            // Use AoE Chakra spender
            var aoeAction = MNKActions.GetAoeChakraSpender(level);
            if (context.ActionService.IsActionReady(aoeAction.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(aoeAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = aoeAction.Name;
                    context.Debug.DamageState = $"{aoeAction.Name} ({enemyCount} enemies)";

                    // Training: Record AoE Chakra spender decision
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(aoeAction.ActionId, aoeAction.Name)
                        .AsAoE(enemyCount)
                        .Target($"{enemyCount} enemies")
                        .Reason($"Spending 5 Chakra on {aoeAction.Name} ({enemyCount} enemies)",
                            "Enlightenment/Howling Fist is the AoE Chakra spender. " +
                            "Use at 5 Chakra stacks to avoid overcapping. High oGCD priority.")
                        .Factors(new[] { "5 Chakra stacks", $"{enemyCount} enemies", "Avoiding overcap" })
                        .Alternatives(new[] { "Use Forbidden Chakra (fewer enemies)", "Hold for burst (risky)" })
                        .Tip("Always spend Chakra at 5 stacks. AoE at 3+ enemies.")
                        .Concept("mnk_chakra_gauge")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("mnk_chakra_gauge", true, "AoE Chakra spending");

                    return true;
                }
            }
        }
        else if (level >= MNKActions.SteelPeak.MinLevel)
        {
            // Use ST Chakra spender
            var stAction = MNKActions.GetChakraSpender(level);
            if (context.ActionService.IsActionReady(stAction.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(stAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = stAction.Name;
                    context.Debug.DamageState = stAction.Name;

                    // Training: Record ST Chakra spender decision
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(stAction.ActionId, stAction.Name)
                        .AsMeleeResource("Chakra", context.Chakra)
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Spending 5 Chakra on {stAction.Name}",
                            "Forbidden Chakra is MNK's ST Chakra spender. " +
                            "Use at 5 stacks to avoid overcapping. Weave during burst windows.")
                        .Factors(new[] { "5 Chakra stacks", "ST damage filler", "Avoiding overcap" })
                        .Alternatives(new[] { "Use Enlightenment (3+ enemies)", "Hold for burst (risky)" })
                        .Tip("Chakra generates passively and from crits. Spend at 5 stacks.")
                        .Concept("mnk_chakra_gauge")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("mnk_chakra_gauge", true, "ST Chakra spending");

                    return true;
                }
            }
        }

        return false;
    }

    private bool TryThunderclap(IKratosContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MNKActions.Thunderclap.MinLevel)
            return false;

        // Only use to close gap if out of melee range and within 20y
        if (DistanceHelper.IsActionInRange(MNKActions.Bootshine.ActionId, player, target))
            return false;

        var dx = player.Position.X - target.Position.X;
        var dz = player.Position.Z - target.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dz * dz);
        if (distance > 20f)
            return false;

        if (!context.ActionService.IsActionReady(MNKActions.Thunderclap.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(MNKActions.Thunderclap, target.GameObjectId))
        {
            context.Debug.PlannedAction = MNKActions.Thunderclap.Name;
            context.Debug.DamageState = "Thunderclap (gap close)";

            // Training: Record Thunderclap gap-close decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(MNKActions.Thunderclap.ActionId, MNKActions.Thunderclap.Name)
                .AsMeleeDamage()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason("Thunderclap to close gap and re-enter melee range",
                    "Thunderclap is MNK's gap closer. Use when out of melee range to quickly return to the target. " +
                    "Has a 30y maximum range and shares charges.")
                .Factors(new[] { "Out of melee range", "Target within 20y", "Thunderclap ready" })
                .Alternatives(new[] { "Sprint + run in (slower)", "Wait for target to move closer" })
                .Tip("Thunderclap keeps you in melee range. Use freely when the gap opens.")
                .Concept("mnk_positionals")
                .Record();
            context.TrainingService?.RecordConceptApplication("mnk_positionals", true, "Gap close");

            return true;
        }

        return false;
    }

    /// <summary>
    /// Main GCD damage rotation for Monk.
    /// Handles Masterful Blitz, Rumination procs, Perfect Balance actions, and form rotation.
    /// </summary>
    protected override bool TryGcdDamage(IKratosContext context, IBattleChara target, int enemyCount, bool isMoving)
    {
        // Priority 1: Masterful Blitz variants (3 Beast Chakra accumulated)
        if (TryMasterfulBlitz(context, target, enemyCount))
            return true;

        // Priority 2: Fire's Rumination proc (after Riddle of Fire ends)
        if (TryFiresRumination(context, target))
            return true;

        // Priority 3: Wind's Rumination proc (after Riddle of Wind ends)
        if (TryWindsRumination(context, target))
            return true;

        // Priority 4: Perfect Balance form selection
        if (TryPerfectBalanceAction(context, target, enemyCount))
            return true;

        // Priority 5: Form-based rotation
        if (TryFormRotation(context, target, enemyCount))
            return true;

        return false;
    }

    #endregion

    #region Masterful Blitz

    private bool TryMasterfulBlitz(IKratosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Blitz actions start at level 60 (Elixir Field/Flint Strike)
        if (level < MNKActions.ElixirField.MinLevel)
            return false;

        // Need 3 Beast Chakra to use Masterful Blitz
        if (context.BeastChakraCount < 3)
            return false;

        // Get the appropriate Blitz action based on Beast Chakra composition
        var blitzAction = MNKActions.GetBlitzAction(
            (byte)level,
            context.HasLunarNadi,
            context.HasSolarNadi,
            (MNKActions.BeastChakraType)context.BeastChakra1,
            (MNKActions.BeastChakraType)context.BeastChakra2,
            (MNKActions.BeastChakraType)context.BeastChakra3);

        if (blitzAction == null)
            return false;

        if (!context.ActionService.IsActionReady(blitzAction.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(blitzAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = blitzAction.Name;
            context.Debug.DamageState = $"{blitzAction.Name} (Blitz)";

            // Training: Record Blitz decision
            var blitzType = GetBlitzType(blitzAction);
            TrainingHelper.Decision(context.TrainingService)
                .Action(blitzAction.ActionId, blitzAction.Name)
                .AsMeleeBurst()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason($"Executing {blitzAction.Name} (3 Beast Chakra accumulated)",
                    $"{blitzType.explanation}")
                .Factors(new[] { "3 Beast Chakra ready", blitzType.nadiState, "Blitz available" })
                .Alternatives(new[] { "Can't - must use Blitz when available" })
                .Tip(blitzType.tip)
                .Concept("mnk_beast_chakra")
                .Record();
            context.TrainingService?.RecordConceptApplication("mnk_beast_chakra", true, $"Blitz: {blitzAction.Name}");

            return true;
        }

        return false;
    }

    private static (string explanation, string nadiState, string tip) GetBlitzType(ActionDefinition blitz)
    {
        return blitz.ActionId switch
        {
            25765 => ( // Phantom Rush
                "Phantom Rush is MNK's ultimate Blitz. Requires both Nadi. Highest potency attack.",
                "Both Nadi active",
                "Phantom Rush is your goal. Build Lunar → Solar → Phantom Rush."),
            25766 => ( // Rising Phoenix
                "Rising Phoenix grants Solar Nadi. Use after you have Lunar Nadi.",
                "Lunar Nadi active",
                "Rising Phoenix needs 2 same + 1 different Beast Chakra."),
            25764 => ( // Elixir Field
                "Elixir Field grants Lunar Nadi. Use when you need Lunar.",
                "No Lunar Nadi",
                "Elixir Field needs 3 different Beast Chakra types."),
            _ => (
                "Blitz attack - high damage from 3 Beast Chakra.",
                "Building Nadi",
                "Use Perfect Balance to build Beast Chakra quickly.")
        };
    }

    #endregion

    #region Rumination Procs

    private bool TryFiresRumination(IKratosContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MNKActions.FiresReply.MinLevel)
            return false;

        // Fire's Rumination proc is ready
        if (!context.HasFiresRumination)
            return false;

        if (!context.ActionService.IsActionReady(MNKActions.FiresReply.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(MNKActions.FiresReply, target.GameObjectId))
        {
            context.Debug.PlannedAction = MNKActions.FiresReply.Name;
            context.Debug.DamageState = "Fire's Reply";

            // Training: Record Fire's Reply decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(MNKActions.FiresReply.ActionId, MNKActions.FiresReply.Name)
                .AsMeleeDamage()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason("Using Fire's Reply proc (from Riddle of Fire)",
                    "Fire's Reply is a high-potency proc that appears after Riddle of Fire. " +
                    "Has a 30s window to use. Use before it expires for free damage.")
                .Factors(new[] { "Fire's Rumination proc active", "High potency GCD", "Free damage" })
                .Alternatives(new[] { "Let it expire (wastes damage)" })
                .Tip("Fire's Reply is free damage. Use it before the 30s window expires.")
                .Concept("mnk_riddle_of_fire")
                .Record();
            context.TrainingService?.RecordConceptApplication("mnk_riddle_of_fire", true, "Fire's Reply proc");

            return true;
        }

        return false;
    }

    private bool TryWindsRumination(IKratosContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MNKActions.WindsReply.MinLevel)
            return false;

        // Wind's Rumination proc is ready
        if (!context.HasWindsRumination)
            return false;

        if (!context.ActionService.IsActionReady(MNKActions.WindsReply.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(MNKActions.WindsReply, target.GameObjectId))
        {
            context.Debug.PlannedAction = MNKActions.WindsReply.Name;
            context.Debug.DamageState = "Wind's Reply";

            // Training: Record Wind's Reply decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(MNKActions.WindsReply.ActionId, MNKActions.WindsReply.Name)
                .AsMeleeDamage()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason("Using Wind's Reply proc (from Riddle of Wind)",
                    "Wind's Reply is a high-potency proc that appears after Riddle of Wind. " +
                    "Has a 30s window to use. Use before it expires for free damage.")
                .Factors(new[] { "Wind's Rumination proc active", "High potency GCD", "Free damage" })
                .Alternatives(new[] { "Let it expire (wastes damage)" })
                .Tip("Wind's Reply is free damage. Use it before the 30s window expires.")
                .Concept("mnk_riddle_of_wind")
                .Record();
            context.TrainingService?.RecordConceptApplication("mnk_riddle_of_wind", true, "Wind's Reply proc");

            return true;
        }

        return false;
    }

    #endregion

    #region Perfect Balance

    private bool TryPerfectBalanceAction(IKratosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Perfect Balance allows using any form GCD
        if (!context.HasPerfectBalance)
            return false;

        if (context.PerfectBalanceStacks <= 0)
            return false;

        // During Perfect Balance, select GCDs to build towards desired Blitz
        // Strategy: Depends on current Nadi state
        // - No Nadi: Build Lunar (3 different = Elixir Field)
        // - Lunar only: Build Solar (2 same + 1 different = Flint Strike/Rising Phoenix)
        // - Solar only: Build Lunar (3 different = Elixir Field)
        // - Both Nadi: Build 3 of same type for Phantom Rush

        var action = GetPerfectBalanceAction(context, enemyCount);
        if (action == null)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (PB: {context.PerfectBalanceStacks} stacks)";

            // Training: Record Perfect Balance GCD decision
            var pbInfo = GetPerfectBalanceExplanation(context);
            TrainingHelper.Decision(context.TrainingService)
                .Action(action.ActionId, action.Name)
                .AsMeleeBurst()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason($"Perfect Balance GCD: {action.Name} (building {pbInfo.target})",
                    pbInfo.explanation)
                .Factors(new[] { $"Perfect Balance ({context.PerfectBalanceStacks} stacks)", pbInfo.beastChakraState, $"Building {pbInfo.target}" })
                .Alternatives(new[] { "Wrong GCD (misbuilds Blitz)" })
                .Tip(pbInfo.tip)
                .Concept("mnk_perfect_balance")
                .Record();
            context.TrainingService?.RecordConceptApplication("mnk_perfect_balance", true, $"PB GCD: {action.Name}");

            return true;
        }

        return false;
    }

    private static (string target, string explanation, string beastChakraState, string tip) GetPerfectBalanceExplanation(IKratosContext context)
    {
        var chakraStr = $"Beast: {(context.BeastChakra1 != 0 ? "Opo" : "")} {(context.BeastChakra2 != 0 ? "Raptor" : "")} {(context.BeastChakra3 != 0 ? "Coeurl" : "")}".Trim();
        if (string.IsNullOrWhiteSpace(chakraStr)) chakraStr = "None";

        if (context.HasBothNadi)
            return ("Phantom Rush", "Both Nadi active - using same Beast Chakra type 3 times for Phantom Rush.", chakraStr, "Spam Opo-opo GCDs for highest potency Phantom Rush.");
        if (context.HasLunarNadi)
            return ("Solar Nadi", "Have Lunar, need Solar. Building 2 same + 1 different for Rising Phoenix.", chakraStr, "Use Opo-opo twice, then a different form.");
        return ("Lunar Nadi", "Need Lunar first. Building 3 different Beast Chakra for Elixir Field.", chakraStr, "Use Opo → Raptor → Coeurl for Elixir Field.");
    }

    private ActionDefinition? GetPerfectBalanceAction(IKratosContext context, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        // Count current Beast Chakra types
        var hasOpo = context.BeastChakra1 == 1 || context.BeastChakra2 == 1 || context.BeastChakra3 == 1;
        var hasRaptor = context.BeastChakra1 == 2 || context.BeastChakra2 == 2 || context.BeastChakra3 == 2;
        var hasCoeurl = context.BeastChakra1 == 3 || context.BeastChakra2 == 3 || context.BeastChakra3 == 3;

        // Determine what we're building towards
        if (context.HasBothNadi)
        {
            // Building for Phantom Rush - need 3 of same type
            // Use Opo-opo GCDs (highest potency)
            return useAoe
                ? (level >= MNKActions.ShadowOfTheDestroyer.MinLevel ? MNKActions.ShadowOfTheDestroyer : MNKActions.ArmOfTheDestroyer)
                : GetOpoOpoAction(context, level);
        }
        else if (context.HasLunarNadi)
        {
            // Have Lunar, need Solar - build 2 same + 1 different
            // Use Opo-opo twice, then something else
            if (!hasOpo || (hasOpo && !hasRaptor && !hasCoeurl))
            {
                return useAoe
                    ? (level >= MNKActions.ShadowOfTheDestroyer.MinLevel ? MNKActions.ShadowOfTheDestroyer : MNKActions.ArmOfTheDestroyer)
                    : GetOpoOpoAction(context, level);
            }
            else
            {
                // Use a different form to complete Solar
                return useAoe
                    ? (level >= MNKActions.FourPointFury.MinLevel ? MNKActions.FourPointFury : MNKActions.TwinSnakes)
                    : GetRaptorAction(context, level);
            }
        }
        else
        {
            // Need Lunar first - build 3 different types
            if (!hasOpo)
            {
                return useAoe
                    ? (level >= MNKActions.ShadowOfTheDestroyer.MinLevel ? MNKActions.ShadowOfTheDestroyer : MNKActions.ArmOfTheDestroyer)
                    : GetOpoOpoAction(context, level);
            }
            else if (!hasRaptor)
            {
                return useAoe
                    ? (level >= MNKActions.FourPointFury.MinLevel ? MNKActions.FourPointFury : MNKActions.TwinSnakes)
                    : GetRaptorAction(context, level);
            }
            else if (!hasCoeurl)
            {
                return useAoe
                    ? (level >= MNKActions.Rockbreaker.MinLevel ? MNKActions.Rockbreaker : MNKActions.SnapPunch)
                    : GetCoeurlAction(context, level);
            }
        }

        // Fallback to Opo-opo
        return useAoe
            ? (level >= MNKActions.ShadowOfTheDestroyer.MinLevel ? MNKActions.ShadowOfTheDestroyer : MNKActions.ArmOfTheDestroyer)
            : GetOpoOpoAction(context, level);
    }

    #endregion

    #region Form Rotation

    private bool TryFormRotation(IKratosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        // Determine current form and execute appropriate action
        var form = context.CurrentForm;

        // Formless Fist allows any form - start with Opo-opo
        if (context.HasFormlessFist || form == MonkForm.Formless || form == MonkForm.None)
        {
            return TryOpoOpoForm(context, target, useAoe);
        }

        switch (form)
        {
            case MonkForm.OpoOpo:
                return TryOpoOpoForm(context, target, useAoe);

            case MonkForm.Raptor:
                return TryRaptorForm(context, target, useAoe);

            case MonkForm.Coeurl:
                return TryCoeurlForm(context, target, useAoe);

            default:
                // No form - start with Opo-opo
                return TryOpoOpoForm(context, target, useAoe);
        }
    }

    private bool TryOpoOpoForm(IKratosContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        if (useAoe)
        {
            // AoE: Arm of the Destroyer / Shadow of the Destroyer
            var aoeAction = level >= MNKActions.ShadowOfTheDestroyer.MinLevel
                ? MNKActions.ShadowOfTheDestroyer
                : MNKActions.ArmOfTheDestroyer;

            if (context.ActionService.IsActionReady(aoeAction.ActionId))
            {
                if (context.ActionService.ExecuteGcd(aoeAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = aoeAction.Name;
                    context.Debug.DamageState = $"{aoeAction.Name} (Opo-opo)";

                    // Training: Record AoE Opo-opo form decision
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(aoeAction.ActionId, aoeAction.Name)
                        .AsAoE(0)
                        .Target($"AoE group")
                        .Reason($"Opo-opo AoE: {aoeAction.Name}",
                            "Shadow/Arm of the Destroyer is the Opo-opo AoE GCD. " +
                            "Use during form rotation when 3+ enemies are in range.")
                        .Factors(new[] { "Opo-opo form", "AoE situation", "Form rotation" })
                        .Alternatives(new[] { "Single-target Bootshine/Dragon Kick (fewer enemies)" })
                        .Tip("Use AoE GCDs when 3+ enemies are in melee range.")
                        .Concept("mnk_positionals")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("mnk_positionals", true, "AoE Opo-opo GCD");

                    return true;
                }
            }
        }

        // Single target: Choose based on procs and buffs
        var action = GetOpoOpoAction(context, level);

        if (context.ActionService.IsActionReady(action.ActionId))
        {
            bool isRearPositional = action == MNKActions.Bootshine || action == MNKActions.LeapingOpo;
            string positional = isRearPositional ? "(rear)" : "(flank)";
            string positionalName = isRearPositional ? "rear" : "flank";
            bool correctPositional = isRearPositional
                ? (context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity)
                : (context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity);

            if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} {positional}";

                // Training: Record Opo-opo positional decision
                var procStatus = context.HasLeadenFist ? "Leaden Fist active" : (context.HasOpooposFury ? "Opo-opo's Fury active" : "No proc");
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsPositional(correctPositional, positionalName)
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Opo-opo form: {action.Name} {(correctPositional ? positional : "(WRONG)")}",
                        action == MNKActions.DragonKick
                            ? "Dragon Kick (flank) grants Leaden Fist buff for your next Bootshine."
                            : "Bootshine/Leaping Opo (rear) consumes Leaden Fist for bonus damage.")
                    .Factors(new[] { "Opo-opo form", procStatus, correctPositional ? $"At {positionalName}" : $"Not at {positionalName}" })
                    .Alternatives(new[] { "Wrong positional (less damage)", "Wrong GCD (miss proc)" })
                    .Tip("Opo-opo: Dragon Kick (flank) → Bootshine (rear) alternation for Leaden Fist.")
                    .Concept("mnk_positionals")
                    .Record();
                context.TrainingService?.RecordConceptApplication("mnk_positionals", correctPositional, $"Opo-opo {positionalName}");

                return true;
            }
        }

        return false;
    }

    private bool TryRaptorForm(IKratosContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        if (useAoe)
        {
            // AoE: Four-point Fury
            if (level >= MNKActions.FourPointFury.MinLevel)
            {
                if (context.ActionService.IsActionReady(MNKActions.FourPointFury.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(MNKActions.FourPointFury, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = MNKActions.FourPointFury.Name;
                        context.Debug.DamageState = "Four-point Fury (Raptor)";

                        // Training: Record Four-point Fury AoE decision
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(MNKActions.FourPointFury.ActionId, MNKActions.FourPointFury.Name)
                            .AsAoE(0)
                            .Target("AoE group")
                            .Reason("Raptor AoE: Four-point Fury",
                                "Four-point Fury is the Raptor form AoE GCD. Also refreshes Disciplined Fist. " +
                                "Use when 3+ enemies are in range during Raptor form.")
                            .Factors(new[] { "Raptor form", "AoE situation", "Refreshes Disciplined Fist" })
                            .Alternatives(new[] { "Single-target True Strike/Twin Snakes (fewer enemies)" })
                            .Tip("Four-point Fury refreshes Disciplined Fist while doing AoE damage.")
                            .Concept("mnk_positionals")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("mnk_positionals", true, "AoE Raptor GCD");

                        return true;
                    }
                }
            }
        }

        // Single target: Choose based on Disciplined Fist duration
        var action = GetRaptorAction(context, level);

        if (context.ActionService.IsActionReady(action.ActionId))
        {
            bool isRearPositional = action == MNKActions.TrueStrike || action == MNKActions.RisingRaptor;
            string positional = isRearPositional ? "(rear)" : "(flank)";
            string positionalName = isRearPositional ? "rear" : "flank";
            bool correctPositional = isRearPositional
                ? (context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity)
                : (context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity);

            if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} {positional}";

                // Training: Record Raptor positional decision
                var buffStatus = $"Disciplined Fist: {(context.HasDisciplinedFist ? $"{context.DisciplinedFistRemaining:F1}s" : "missing")}";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsPositional(correctPositional, positionalName)
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Raptor form: {action.Name} {(correctPositional ? positional : "(WRONG)")}",
                        action == MNKActions.TwinSnakes
                            ? "Twin Snakes (flank) refreshes Disciplined Fist (+15% damage buff)."
                            : "True Strike/Rising Raptor (rear) is pure damage when buff is healthy.")
                    .Factors(new[] { "Raptor form", buffStatus, correctPositional ? $"At {positionalName}" : $"Not at {positionalName}" })
                    .Alternatives(new[] { "Let Disciplined Fist drop (lose 15% damage)", "Wrong positional (less damage)" })
                    .Tip("Raptor: Twin Snakes (flank) to refresh buff, True Strike (rear) for damage.")
                    .Concept("mnk_positionals")
                    .Record();
                context.TrainingService?.RecordConceptApplication("mnk_positionals", correctPositional, $"Raptor {positionalName}");

                return true;
            }
        }

        return false;
    }

    private bool TryCoeurlForm(IKratosContext context, IBattleChara target, bool useAoe)
    {
        var player = context.Player;
        var level = player.Level;

        if (useAoe)
        {
            // AoE: Rockbreaker
            if (level >= MNKActions.Rockbreaker.MinLevel)
            {
                if (context.ActionService.IsActionReady(MNKActions.Rockbreaker.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(MNKActions.Rockbreaker, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = MNKActions.Rockbreaker.Name;
                        context.Debug.DamageState = "Rockbreaker (Coeurl)";

                        // Training: Record Rockbreaker AoE decision
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(MNKActions.Rockbreaker.ActionId, MNKActions.Rockbreaker.Name)
                            .AsAoE(0)
                            .Target("AoE group")
                            .Reason("Coeurl AoE: Rockbreaker",
                                "Rockbreaker is the Coeurl form AoE GCD. " +
                                "Use when 3+ enemies are in range during Coeurl form.")
                            .Factors(new[] { "Coeurl form", "AoE situation", "Form rotation" })
                            .Alternatives(new[] { "Single-target Snap Punch/Demolish (fewer enemies)" })
                            .Tip("Rockbreaker is your Coeurl AoE GCD. Prioritize Demolish on single targets.")
                            .Concept("mnk_positionals")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("mnk_positionals", true, "AoE Coeurl GCD");

                        return true;
                    }
                }
            }
        }

        // Single target: Choose based on Demolish DoT
        var action = GetCoeurlAction(context, level);

        if (context.ActionService.IsActionReady(action.ActionId))
        {
            bool isRearPositional = action == MNKActions.Demolish || action == MNKActions.PouncingCoeurl;
            string positional = isRearPositional ? "(rear)" : "(flank)";
            string positionalName = isRearPositional ? "rear" : "flank";
            bool correctPositional = isRearPositional
                ? (context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity)
                : (context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity);

            if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} {positional}";

                // Training: Record Coeurl positional decision
                var dotStatus = $"Demolish: {(context.HasDemolishOnTarget ? $"{context.DemolishRemaining:F1}s" : "missing")}";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsPositional(correctPositional, positionalName)
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Coeurl form: {action.Name} {(correctPositional ? positional : "(WRONG)")}",
                        action == MNKActions.Demolish
                            ? "Demolish (rear) applies/refreshes DoT. Refresh when <3s remains."
                            : "Snap Punch/Pouncing Coeurl (flank) is pure damage when DoT is healthy.")
                    .Factors(new[] { "Coeurl form", dotStatus, correctPositional ? $"At {positionalName}" : $"Not at {positionalName}" })
                    .Alternatives(new[] { "Let Demolish drop (lose DoT damage)", "Wrong positional (less damage)" })
                    .Tip("Coeurl: Demolish (rear) to apply DoT, Snap Punch (flank) for damage.")
                    .Concept("mnk_positionals")
                    .Record();
                context.TrainingService?.RecordConceptApplication("mnk_positionals", correctPositional, $"Coeurl {positionalName}");

                return true;
            }
        }

        return false;
    }

    #endregion

    #region Action Selection Helpers

    private ActionDefinition GetOpoOpoAction(IKratosContext context, uint level)
    {
        // Dawntrail upgrades: Bootshine -> Leaping Opo, Dragon Kick -> (no upgrade)
        // Leaden Fist proc: Use Bootshine/Leaping Opo (rear)
        // Opo-opo's Fury proc: Skip Leaden Fist check
        // No proc: Use Dragon Kick (flank) to get Leaden Fist

        if (context.HasOpooposFury)
        {
            // With Opo-opo's Fury, prefer Leaping Opo/Bootshine for damage
            if (level >= MNKActions.LeapingOpo.MinLevel)
                return MNKActions.LeapingOpo;
            return MNKActions.Bootshine;
        }

        if (context.HasLeadenFist)
        {
            // Leaden Fist buff - use Bootshine/Leaping Opo to consume it
            if (level >= MNKActions.LeapingOpo.MinLevel)
                return MNKActions.LeapingOpo;
            return MNKActions.Bootshine;
        }

        // No Leaden Fist - use Dragon Kick to get it
        if (level >= MNKActions.DragonKick.MinLevel)
            return MNKActions.DragonKick;

        // Low level fallback
        return MNKActions.Bootshine;
    }

    private ActionDefinition GetRaptorAction(IKratosContext context, uint level)
    {
        // Dawntrail upgrades: True Strike -> Rising Raptor, Twin Snakes -> (no upgrade)
        // Raptor's Fury proc: Use any Raptor GCD
        // Disciplined Fist < 5s: Use Twin Snakes (flank) to refresh
        // Otherwise: Use True Strike/Rising Raptor (rear) for damage

        // Always maintain Disciplined Fist if it's low or missing
        if (!context.HasDisciplinedFist || context.DisciplinedFistRemaining < 5f)
        {
            if (level >= MNKActions.TwinSnakes.MinLevel)
                return MNKActions.TwinSnakes;
        }

        // With Raptor's Fury or sufficient buff, use damage action
        if (level >= MNKActions.RisingRaptor.MinLevel)
            return MNKActions.RisingRaptor;

        if (level >= MNKActions.TrueStrike.MinLevel)
            return MNKActions.TrueStrike;

        // Low level fallback
        return level >= MNKActions.TwinSnakes.MinLevel ? MNKActions.TwinSnakes : MNKActions.TrueStrike;
    }

    private ActionDefinition GetCoeurlAction(IKratosContext context, uint level)
    {
        // Dawntrail upgrades: Snap Punch -> Pouncing Coeurl, Demolish -> (no upgrade)
        // Coeurl's Fury proc: Use any Coeurl GCD
        // Demolish DoT < 3s: Use Demolish (rear) to refresh
        // Otherwise: Use Snap Punch/Pouncing Coeurl (flank) for damage

        // Apply/refresh Demolish if needed
        if (!context.HasDemolishOnTarget || context.DemolishRemaining < 3f)
        {
            if (level >= MNKActions.Demolish.MinLevel)
                return MNKActions.Demolish;
        }

        // With Coeurl's Fury or sufficient DoT, use damage action
        if (level >= MNKActions.PouncingCoeurl.MinLevel)
            return MNKActions.PouncingCoeurl;

        if (level >= MNKActions.SnapPunch.MinLevel)
            return MNKActions.SnapPunch;

        // Low level fallback
        return level >= MNKActions.Demolish.MinLevel ? MNKActions.Demolish : MNKActions.SnapPunch;
    }

    #endregion

    #region Smart AoE

    protected override uint GetNextDirectionalAoEActionId(IKratosContext context, IBattleChara target, int enemyCount)
    {
        if (enemyCount < AoeThreshold) return 0;
        var level = context.Player.Level;
        // Enlightenment / Howling Fist are line AoE Chakra spenders
        if (level >= MNKActions.Enlightenment.MinLevel)
            return MNKActions.Enlightenment.ActionId;
        if (level >= MNKActions.HowlingFist.MinLevel)
            return MNKActions.HowlingFist.ActionId;
        return 0;
    }

    #endregion
}
