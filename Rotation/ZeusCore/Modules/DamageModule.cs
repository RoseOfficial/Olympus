using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.ZeusCore.Context;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Services.Training;

namespace Olympus.Rotation.ZeusCore.Modules;

/// <summary>
/// Handles the Dragoon damage rotation.
/// Manages combo execution, jump weaving, Life of the Dragon, and burst windows.
/// Extends BaseDpsDamageModule for shared damage module patterns.
/// </summary>
public sealed class DamageModule : BaseDpsDamageModule<IZeusContext>, IZeusModule
{
    public DamageModule(IBurstWindowService? burstWindowService = null, ISmartAoEService? smartAoEService = null) : base(burstWindowService, smartAoEService) { }

    #region Abstract Method Implementations

    /// <summary>
    /// Melee targeting range (3y) — used as fallback only.
    /// </summary>
    protected override float GetTargetingRange() => FFXIVConstants.MeleeTargetingRange;

    /// <summary>
    /// Use True Thrust to check melee range via game API for maximum accuracy.
    /// </summary>
    protected override uint GetRangeCheckActionId() => DRGActions.TrueThrust.ActionId;

    /// <summary>
    /// AoE count range for DRG (5y for melee AoE abilities).
    /// </summary>
    protected override float GetAoECountRange() => 5f;

    /// <summary>
    /// Sets the damage state in the debug display.
    /// </summary>
    protected override void SetDamageState(IZeusContext context, string state) =>
        context.Debug.DamageState = state;

    /// <summary>
    /// Sets the nearby enemy count in the debug display.
    /// </summary>
    protected override void SetNearbyEnemies(IZeusContext context, int count) =>
        context.Debug.NearbyEnemies = count;

    /// <summary>
    /// Sets the planned action name in the debug display.
    /// </summary>
    protected override void SetPlannedAction(IZeusContext context, string action) =>
        context.Debug.PlannedAction = action;

    protected override bool IsAoEEnabled(IZeusContext context) =>
        context.Configuration.Dragoon.EnableAoERotation;

    protected override int GetConfiguredAoEThreshold(IZeusContext context) =>
        context.Configuration.Dragoon.AoEMinTargets;

    /// <summary>
    /// oGCD damage for Dragoon - Life of the Dragon abilities, jumps, etc.
    /// </summary>
    protected override bool TryOgcdDamage(IZeusContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Priority 1: Mirage Dive (consume Dive Ready)
        if (TryMirageDive(context, target))
            return true;

        // Priority 2: Starcross (after Stardiver)
        if (TryStarcross(context, target))
            return true;

        // Priority 3: Rise of the Dragon (after Dragonfire Dive)
        if (TryRiseOfTheDragon(context, target))
            return true;

        // Priority 4: Wyrmwind Thrust (2 Firstmind stacks)
        if (TryWyrmwindThrust(context, target))
            return true;

        // Priority 5: Life of the Dragon actions
        if (context.IsLifeOfDragonActive)
        {
            // Nastrond (spam during Life)
            if (TryNastrond(context, target))
                return true;

            // Stardiver (Life finisher)
            if (TryStardiver(context, target))
                return true;
        }

        // Priority 6: Geirskogul (Life activation or just damage)
        if (TryGeirskogul(context, target))
            return true;

        // Priority 7: High Jump / Jump
        if (TryJump(context, target))
            return true;

        // Priority 8: Spineshatter Dive (gap closer / damage)
        if (TrySpineshatterDive(context, target))
            return true;

        // Priority 9: Dragonfire Dive (AoE damage)
        if (TryDragonfireDive(context, target))
            return true;

        return false;
    }

    private bool TryMirageDive(IZeusContext context, IBattleChara target)
    {
        if (!context.Configuration.Dragoon.EnableMirageDive) return false;

        var level = context.Player.Level;

        if (level < DRGActions.MirageDive.MinLevel)
            return false;

        if (!context.HasDiveReady)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.MirageDive.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.MirageDive, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.MirageDive.Name;
            context.Debug.DamageState = "Mirage Dive";

            TrainingHelper.Decision(context.TrainingService)
                .Action(DRGActions.MirageDive.ActionId, DRGActions.MirageDive.Name)
                .AsMeleeDamage()
                .Target(target.Name?.TextValue)
                .Reason("Mirage Dive to consume Dive Ready",
                    "Mirage Dive is a high-potency oGCD that becomes available after using High Jump (or Jump). " +
                    "Always use it promptly to consume the Dive Ready proc — letting it fall off wastes significant damage.")
                .Factors(new[] { "Dive Ready proc active", "High-potency follow-up to High Jump", "oGCD — fits in GCD window" })
                .Alternatives(new[] { "Delay (risks losing the proc)", "Skip (large damage loss)" })
                .Tip("Use Mirage Dive immediately after High Jump. The proc lasts 15 seconds but weave it in your next oGCD slot.")
                .Concept(DrgConcepts.MirageDive)
                .Record();
            context.TrainingService?.RecordConceptApplication(DrgConcepts.MirageDive, true, "Dive Ready proc consumed");

            return true;
        }

        return false;
    }

    private bool TryStarcross(IZeusContext context, IBattleChara target)
    {
        if (!context.Configuration.Dragoon.EnableStardiver) return false;
        var level = context.Player.Level;

        if (level < DRGActions.Starcross.MinLevel)
            return false;

        if (!context.HasStarcrossReady)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.Starcross.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.Starcross, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.Starcross.Name;
            context.Debug.DamageState = "Starcross";

            TrainingHelper.Decision(context.TrainingService)
                .Action(DRGActions.Starcross.ActionId, DRGActions.Starcross.Name)
                .AsMeleeBurst()
                .Target(target.Name?.TextValue)
                .Reason("Starcross follow-up after Stardiver",
                    "Starcross is a powerful oGCD that becomes available after Stardiver (Lv.100). " +
                    "It extends your Life of the Dragon burst with another high-potency hit. " +
                    "Use it immediately after Stardiver while still inside your Life window.")
                .Factors(new[] { "Starcross Ready proc active (from Stardiver)", "Part of Life of Dragon burst phase", "oGCD — no GCD cost" })
                .Alternatives(new[] { "Delay (risks missing Life window)", "Skip (significant damage loss)" })
                .Tip("Starcross always follows Stardiver in your Life of the Dragon rotation at Lv.100. Never skip it.")
                .Concept(DrgConcepts.Stardiver)
                .Record();
            context.TrainingService?.RecordConceptApplication(DrgConcepts.Stardiver, true, "Starcross follow-up used");

            return true;
        }

        return false;
    }

    private bool TryRiseOfTheDragon(IZeusContext context, IBattleChara target)
    {
        if (!context.Configuration.Dragoon.EnableDragonfireDive) return false;
        var level = context.Player.Level;

        if (level < DRGActions.RiseOfTheDragon.MinLevel)
            return false;

        if (!context.HasDraconianFire)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.RiseOfTheDragon.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.RiseOfTheDragon, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.RiseOfTheDragon.Name;
            context.Debug.DamageState = "Rise of the Dragon";

            TrainingHelper.Decision(context.TrainingService)
                .Action(DRGActions.RiseOfTheDragon.ActionId, DRGActions.RiseOfTheDragon.Name)
                .AsMeleeBurst()
                .Target(target.Name?.TextValue)
                .Reason("Rise of the Dragon follow-up after Dragonfire Dive",
                    "Rise of the Dragon is an oGCD follow-up that becomes available after Dragonfire Dive (Lv.92+). " +
                    "It grants the Draconian Fire proc and delivers significant AoE damage. Use it immediately after Dragonfire Dive.")
                .Factors(new[] { "Draconian Fire proc active (from Dragonfire Dive)", "High-potency AoE follow-up", "oGCD — weave with next GCD" })
                .Alternatives(new[] { "Delay (risks proc expiry)", "Skip (wastes Dragonfire Dive value)" })
                .Tip("Rise of the Dragon is always the oGCD follow-up to Dragonfire Dive. Use it before the Draconian Fire proc expires.")
                .Concept(DrgConcepts.DragonfireDive)
                .Record();
            context.TrainingService?.RecordConceptApplication(DrgConcepts.DragonfireDive, true, "Rise of the Dragon follow-up used");

            return true;
        }

        return false;
    }

    private bool TryWyrmwindThrust(IZeusContext context, IBattleChara target)
    {
        if (!context.Configuration.Dragoon.EnableWyrmwindThrust) return false;

        var level = context.Player.Level;

        if (level < DRGActions.WyrmwindThrust.MinLevel)
            return false;

        // Need 2 Firstmind's Focus
        if (context.FirstmindsFocus < 2)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.WyrmwindThrust.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.WyrmwindThrust, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.WyrmwindThrust.Name;
            context.Debug.DamageState = $"Wyrmwind Thrust ({context.FirstmindsFocus} focus)";

            TrainingHelper.Decision(context.TrainingService)
                .Action(DRGActions.WyrmwindThrust.ActionId, DRGActions.WyrmwindThrust.Name)
                .AsMeleeResource("Firstmind's Focus", context.FirstmindsFocus)
                .Target(target.Name?.TextValue)
                .Reason($"Wyrmwind Thrust at {context.FirstmindsFocus} Firstmind's Focus stacks",
                    "Wyrmwind Thrust is a line AoE oGCD that requires 2 Firstmind's Focus stacks. " +
                    "These stacks are generated by certain combo finishers (Heavens' Thrust, Drakesbane, Coerthan Torment). " +
                    "Spend both stacks promptly to avoid overcapping when the next finisher hits.")
                .Factors(new[] { $"{context.FirstmindsFocus} Firstmind's Focus stacks (max 2)", "High-potency line AoE oGCD", "Avoid overcapping stacks" })
                .Alternatives(new[] { "Hold for burst window (risks overcap)", "Use earlier on 1 stack (not possible — requires 2)" })
                .Tip("You generate 1 Firstmind's Focus per finisher combo. Spend at 2 stacks before the next finisher to prevent overcap.")
                .Concept(DrgConcepts.WyrmwindThrust)
                .Record();
            context.TrainingService?.RecordConceptApplication(DrgConcepts.WyrmwindThrust, true, "Firstmind's Focus spent at 2 stacks");

            return true;
        }

        return false;
    }

    private bool TryNastrond(IZeusContext context, IBattleChara target)
    {
        if (!context.Configuration.Dragoon.EnableNastrond) return false;

        var level = context.Player.Level;

        if (level < DRGActions.Nastrond.MinLevel)
            return false;

        if (!context.IsLifeOfDragonActive)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.Nastrond.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRGActions.Nastrond, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.Nastrond.Name;
            context.Debug.DamageState = $"Nastrond (Life: {context.LifeOfDragonRemaining:F1}s)";

            TrainingHelper.Decision(context.TrainingService)
                .Action(DRGActions.Nastrond.ActionId, DRGActions.Nastrond.Name)
                .AsMeleeBurst()
                .Target(target.Name?.TextValue)
                .Reason($"Nastrond during Life of Dragon ({context.LifeOfDragonRemaining:F1}s remaining)",
                    "Nastrond is a high-potency line AoE oGCD available only during Life of the Dragon. " +
                    "Spam it on cooldown throughout the Life window — it replaces Geirskogul while Life is active. " +
                    "Try to use 3 Nastronds before Stardiver for maximum burst value.")
                .Factors(new[] { $"Life of Dragon active ({context.LifeOfDragonRemaining:F1}s)", "Replaces Geirskogul during Life", "Short cooldown — spam on cooldown" })
                .Alternatives(new[] { "Use Geirskogul instead (not available during Life)", "Hold (wastes the Life window)" })
                .Tip("During Life of the Dragon, prioritize Nastrond on cooldown before spending the window on Stardiver.")
                .Concept(DrgConcepts.Nastrond)
                .Record();
            context.TrainingService?.RecordConceptApplication(DrgConcepts.Nastrond, true, "Nastrond used during Life of Dragon");

            return true;
        }

        return false;
    }

    private bool TryStardiver(IZeusContext context, IBattleChara target)
    {
        if (!context.Configuration.Dragoon.EnableStardiver) return false;

        var level = context.Player.Level;

        if (level < DRGActions.Stardiver.MinLevel)
            return false;

        if (!context.IsLifeOfDragonActive)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.Stardiver.ActionId))
            return false;

        // Safety gate: Stardiver is a long animation-locked dive that physically moves
        // the player to the target. Blocks during spread markers / gaze mechanics.
        if (context.TargetingService.GapCloserSafety.ShouldBlockGapCloser(target, context.Player))
        {
            context.Debug.DamageState = $"Stardiver blocked: {context.TargetingService.GapCloserSafety.LastBlockReason}";
            return false;
        }

        // Use Stardiver during Life window
        // Prefer using it after Nastronds when possible
        if (context.ActionService.ExecuteOgcd(DRGActions.Stardiver, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.Stardiver.Name;
            context.Debug.DamageState = $"Stardiver (Life: {context.LifeOfDragonRemaining:F1}s)";

            // Training: Record Stardiver decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(DRGActions.Stardiver.ActionId, DRGActions.Stardiver.Name)
                .AsMeleeBurst()
                .Target(target.Name?.TextValue)
                .Reason($"Stardiver during Life of Dragon ({context.LifeOfDragonRemaining:F1}s remaining)",
                    "Stardiver is DRG's highest potency single attack, available only during Life of the Dragon. " +
                    "This massive dive attack deals enormous AoE damage. At Lv.100, it also grants Starcross Ready " +
                    "for a follow-up attack. Time it within your Life window after using some Nastronds.")
                .Factors(new[] {
                    $"Life of Dragon active ({context.LifeOfDragonRemaining:F1}s)",
                    "Highest potency attack",
                    "Grants Starcross Ready at Lv.100"
                })
                .Alternatives(new[] { "Wait for more Nastronds (risk Life expiring)", "Use earlier (might miss buff alignment)" })
                .Tip("Stardiver is a long animation - don't use it if Life of Dragon is about to expire!")
                .Concept("drg_stardiver")
                .Record();
            context.TrainingService?.RecordConceptApplication("drg_stardiver", true, "Life of Dragon burst");

            return true;
        }

        return false;
    }

    private bool TryGeirskogul(IZeusContext context, IBattleChara target)
    {
        if (!context.Configuration.Dragoon.EnableGeirskogul) return false;

        var level = context.Player.Level;

        if (level < DRGActions.Geirskogul.MinLevel)
            return false;

        // Don't use during Life of the Dragon (Nastrond replaces it)
        if (context.IsLifeOfDragonActive)
            return false;

        // Hold Geirskogul for burst when it would activate Life of the Dragon and burst is imminent
        if (context.Configuration.Dragoon.EnableBurstPooling && ShouldHoldForBurst(8f) && context.EyeCount == 2)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.Geirskogul.ActionId))
            return false;

        // Use Geirskogul - at 2 eyes, this enters Life of the Dragon
        if (context.ActionService.ExecuteOgcd(DRGActions.Geirskogul, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.Geirskogul.Name;
            var eyeInfo = context.EyeCount >= 2 ? " (entering Life!)" : $" ({context.EyeCount} eyes)";
            context.Debug.DamageState = $"Geirskogul{eyeInfo}";

            // Training: Record Geirskogul decision
            var enteringLife = context.EyeCount >= 2;
            TrainingHelper.Decision(context.TrainingService)
                .Action(DRGActions.Geirskogul.ActionId, DRGActions.Geirskogul.Name)
                .AsMeleeResource("Dragon Eye", context.EyeCount)
                .Reason(enteringLife
                        ? "Geirskogul at 2 eyes - entering Life of the Dragon!"
                        : $"Geirskogul for damage ({context.EyeCount} eyes)",
                    enteringLife
                        ? "Geirskogul at 2 Dragon Eyes enters Life of the Dragon, a 30-second window " +
                          "where you can use Nastrond (line AoE damage) and Stardiver (massive dive attack). " +
                          "This is your strongest burst phase - try to align it with raid buffs!"
                        : "Geirskogul deals line AoE damage and adds 1 Dragon Eye. " +
                          "At 2 eyes, the next Geirskogul will enter Life of the Dragon.")
                .Factors(enteringLife
                    ? new[] { "2 Dragon Eyes ready", "Lance Charge and buffs aligned", "Entering Life of Dragon for Nastrond/Stardiver" }
                    : new[] { $"{context.EyeCount} Dragon Eye(s)", "Building toward Life of Dragon", "30s cooldown allows frequent use" })
                .Alternatives(new[] { "Hold for buff alignment (minor optimization)", "Use anyway for consistent damage" })
                .Tip("Life of the Dragon is your biggest damage window. Try to enter it during Lance Charge + Battle Litany.")
                .Concept(enteringLife ? "drg_life_of_dragon" : "drg_eye_gauge")
                .Record();
            if (enteringLife)
                context.TrainingService?.RecordConceptApplication("drg_life_of_dragon", true, "Entering Life of Dragon");
            else
                context.TrainingService?.RecordConceptApplication("drg_eye_gauge", true, "Building Dragon Eyes");

            return true;
        }

        return false;
    }

    private bool TryJump(IZeusContext context, IBattleChara target)
    {
        if (!context.Configuration.Dragoon.EnableJumps) return false;

        var level = context.Player.Level;

        if (level < DRGActions.Jump.MinLevel)
            return false;

        var jumpAction = DRGActions.GetJumpAction((byte)level);

        if (!context.ActionService.IsActionReady(jumpAction.ActionId))
            return false;

        // Safety gate: all DRG jumps physically move the player to the target's location
        // — dangerous during spread markers, ground AoE at the destination, or gaze mechanics.
        if (context.TargetingService.GapCloserSafety.ShouldBlockGapCloser(target, context.Player))
        {
            context.Debug.DamageState = $"{jumpAction.Name} blocked: {context.TargetingService.GapCloserSafety.LastBlockReason}";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(jumpAction, target.GameObjectId))
        {
            context.Debug.PlannedAction = jumpAction.Name;
            context.Debug.DamageState = jumpAction.Name;

            // Training: Record High Jump decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(jumpAction.ActionId, jumpAction.Name)
                .AsMeleeDamage()
                .Target(target.Name?.TextValue)
                .Reason($"{jumpAction.Name} for damage and Dive Ready proc",
                    $"{jumpAction.Name} is DRG's signature ability that deals high damage and grants Dive Ready, " +
                    "allowing you to use Mirage Dive. At level 74+, High Jump replaces Jump with higher potency. " +
                    "Each Jump also grants 1 Dragon Eye toward Life of the Dragon.")
                .Factors(new[] { "30s cooldown ready", "Grants Dive Ready for Mirage Dive", "Builds Dragon Eye gauge" })
                .Alternatives(new[] { "Hold for better positioning (rarely worth it)", "Use other oGCDs first (might delay eye build)" })
                .Tip("Jump abilities are a key part of DRG's rotation. Use on cooldown to maximize Dragon Eye generation.")
                .Concept("drg_high_jump")
                .Record();
            context.TrainingService?.RecordConceptApplication("drg_high_jump", true, "Jump ability usage");

            return true;
        }

        return false;
    }

    private bool TrySpineshatterDive(IZeusContext context, IBattleChara target)
    {
        if (!context.Configuration.Dragoon.EnableSpineshatterDive) return false;

        var level = context.Player.Level;

        if (level < DRGActions.SpineshatterDive.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.SpineshatterDive.ActionId))
            return false;

        // Safety gate: Spineshatter Dive physically moves the player to the target.
        if (context.TargetingService.GapCloserSafety.ShouldBlockGapCloser(target, context.Player))
        {
            context.Debug.DamageState = $"Spineshatter Dive blocked: {context.TargetingService.GapCloserSafety.LastBlockReason}";
            return false;
        }

        // Use as damage oGCD (also works as gap closer)
        if (context.ActionService.ExecuteOgcd(DRGActions.SpineshatterDive, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.SpineshatterDive.Name;
            context.Debug.DamageState = "Spineshatter Dive";

            TrainingHelper.Decision(context.TrainingService)
                .Action(DRGActions.SpineshatterDive.ActionId, DRGActions.SpineshatterDive.Name)
                .AsMeleeDamage()
                .Target(target.Name?.TextValue)
                .Reason("Spineshatter Dive for damage",
                    "Spineshatter Dive is a gap-closer oGCD that also deals solid damage. " +
                    "Use it on cooldown for consistent damage output. It can also reposition you to melee range when needed.")
                .Factors(new[] { "On cooldown and ready", "Consistent damage oGCD", "Can gap-close if out of melee range" })
                .Alternatives(new[] { "Save as gap closer (only worth it when very far from target)" })
                .Tip("Spineshatter Dive has 2 charges at higher levels. Use both on cooldown during and outside burst windows.")
                .Concept(DrgConcepts.SpineshatterDive)
                .Record();
            context.TrainingService?.RecordConceptApplication(DrgConcepts.SpineshatterDive, true, "Spineshatter Dive used on cooldown");

            return true;
        }

        return false;
    }

    private bool TryDragonfireDive(IZeusContext context, IBattleChara target)
    {
        if (!context.Configuration.Dragoon.EnableDragonfireDive) return false;

        var level = context.Player.Level;

        if (level < DRGActions.DragonfireDive.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(DRGActions.DragonfireDive.ActionId))
            return false;

        // Safety gate: Dragonfire Dive physically moves the player and is AoE — dangerous
        // during spread markers, moving away, or if target isn't the user's selection.
        if (context.TargetingService.GapCloserSafety.ShouldBlockGapCloser(target, context.Player))
        {
            context.Debug.DamageState = $"Dragonfire Dive blocked: {context.TargetingService.GapCloserSafety.LastBlockReason}";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(DRGActions.DragonfireDive, target.GameObjectId))
        {
            context.Debug.PlannedAction = DRGActions.DragonfireDive.Name;
            context.Debug.DamageState = "Dragonfire Dive";

            TrainingHelper.Decision(context.TrainingService)
                .Action(DRGActions.DragonfireDive.ActionId, DRGActions.DragonfireDive.Name)
                .AsMeleeDamage()
                .Target(target.Name?.TextValue)
                .Reason("Dragonfire Dive for AoE damage",
                    "Dragonfire Dive is a high-potency AoE oGCD. At Lv.92+, it also grants the Draconian Fire proc, " +
                    "enabling Rise of the Dragon as a follow-up. Use it on cooldown as part of your standard oGCD rotation.")
                .Factors(new[] { "On cooldown and ready", "High-potency AoE oGCD", "Grants Draconian Fire at Lv.92+ for Rise of the Dragon follow-up" })
                .Alternatives(new[] { "Hold for burst window (minor optimization if aligned)", "Skip (significant damage loss)" })
                .Tip("Dragonfire Dive should be followed by Rise of the Dragon (Lv.92+). Don't let the Draconian Fire proc expire.")
                .Concept(DrgConcepts.DragonfireDive)
                .Record();
            context.TrainingService?.RecordConceptApplication(DrgConcepts.DragonfireDive, true, "Dragonfire Dive used on cooldown");

            return true;
        }

        return false;
    }

    /// <summary>
    /// Main GCD damage rotation for Dragoon.
    /// Handles positional procs and combo rotation (ST/AoE).
    /// </summary>
    protected override bool TryGcdDamage(IZeusContext context, IBattleChara target, int enemyCount, bool isMoving)
    {
        // Priority 1: Positional procs (Fang and Claw, Wheeling Thrust, Drakesbane)
        if (TryPositionalProc(context, target))
            return true;

        // Priority 2: Continue combo based on enemy count
        if (ShouldUseAoE(enemyCount))
        {
            if (TryAoeCombo(context, target))
                return true;
        }
        else
        {
            if (TrySingleTargetCombo(context, target))
                return true;
        }

        return false;
    }

    #endregion

    #region Positional Procs

    private bool TryPositionalProc(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;

        // At level 92+, Drakesbane replaces both Fang and Claw and Wheeling Thrust
        if (level >= DRGActions.Drakesbane.MinLevel)
        {
            // Either proc grants Drakesbane
            if (context.HasFangAndClawBared || context.HasWheelInMotion)
            {
                if (context.ActionService.IsActionReady(DRGActions.Drakesbane.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(DRGActions.Drakesbane, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = DRGActions.Drakesbane.Name;
                        context.Debug.DamageState = "Drakesbane";

                        TrainingHelper.Decision(context.TrainingService)
                            .Action(DRGActions.Drakesbane.ActionId, DRGActions.Drakesbane.Name)
                            .AsMeleeDamage()
                            .Target(target.Name?.TextValue)
                            .Reason("Drakesbane to consume positional proc",
                                "At Lv.92+, Drakesbane replaces both Fang and Claw and Wheeling Thrust. " +
                                "It is unlocked by either the Fang and Claw Bared or Wheel in Motion proc from your previous combo step. " +
                                "Drakesbane has no positional requirement — use it as soon as the proc is active.")
                            .Factors(new[] { "Positional proc active (Fang and Claw Bared or Wheel in Motion)", "No positional requirement at Lv.92+", "High-potency combo finisher" })
                            .Alternatives(new[] { "Delay (risks dropping the combo)", "Skip (loses finisher damage)" })
                            .Tip("Drakesbane is your primary combo finisher at Lv.92+. It replaces the positional finishers and grants Firstmind's Focus.")
                            .Concept(DrgConcepts.Positionals)
                            .Record();
                        context.TrainingService?.RecordConceptApplication(DrgConcepts.Positionals, true, "Drakesbane finisher used");

                        return true;
                    }
                }
            }
            return false;
        }

        // Fang and Claw (flank positional)
        if (context.HasFangAndClawBared && level >= DRGActions.FangAndClaw.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRGActions.FangAndClaw.ActionId))
            {
                var positionalOk = context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                if (context.ActionService.ExecuteGcd(DRGActions.FangAndClaw, target.GameObjectId))
                {
                    context.Debug.PlannedAction = DRGActions.FangAndClaw.Name;
                    context.Debug.DamageState = $"Fang and Claw {(positionalOk ? "(flank OK)" : "(flank!)")}";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DRGActions.FangAndClaw.ActionId, DRGActions.FangAndClaw.Name)
                        .AsPositional(positionalOk, "flank")
                        .Target(target.Name?.TextValue)
                        .Reason(positionalOk
                            ? "Fang and Claw from flank position (positional hit)"
                            : "Fang and Claw — not at flank (positional missed)",
                            "Fang and Claw is a flank positional GCD that extends the Heavens' Thrust combo chain. " +
                            "Position yourself at the target's flank (left or right side) to gain the bonus potency. " +
                            "Use True North if you cannot reach the correct position.")
                        .Factors(positionalOk
                            ? new[] { "Fang and Claw Bared proc active", "Flank position achieved", "Bonus potency applied" }
                            : new[] { "Fang and Claw Bared proc active", "Not at flank — positional missed", "Consider True North next time" })
                        .Alternatives(new[] { "Use True North to guarantee flank (if off cooldown)", "Adjust position pre-emptively" })
                        .Tip("Fang and Claw requires flanking the target. Check your position before the Vorpal Thrust combo step.")
                        .Concept(DrgConcepts.Positionals)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(DrgConcepts.Positionals, positionalOk, positionalOk ? "Flank positional hit" : "Flank positional missed");

                    return true;
                }
            }
        }

        // Wheeling Thrust (rear positional)
        if (context.HasWheelInMotion && level >= DRGActions.WheelingThrust.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRGActions.WheelingThrust.ActionId))
            {
                var positionalOk = context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                if (context.ActionService.ExecuteGcd(DRGActions.WheelingThrust, target.GameObjectId))
                {
                    context.Debug.PlannedAction = DRGActions.WheelingThrust.Name;
                    context.Debug.DamageState = $"Wheeling Thrust {(positionalOk ? "(rear OK)" : "(rear!)")}";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DRGActions.WheelingThrust.ActionId, DRGActions.WheelingThrust.Name)
                        .AsPositional(positionalOk, "rear")
                        .Target(target.Name?.TextValue)
                        .Reason(positionalOk
                            ? "Wheeling Thrust from rear position (positional hit)"
                            : "Wheeling Thrust — not at rear (positional missed)",
                            "Wheeling Thrust is a rear positional GCD that extends the Chaos Thrust combo chain. " +
                            "Position yourself at the target's rear to gain bonus potency. " +
                            "Use True North if you cannot reach the rear of the target.")
                        .Factors(positionalOk
                            ? new[] { "Wheel in Motion proc active", "Rear position achieved", "Bonus potency applied" }
                            : new[] { "Wheel in Motion proc active", "Not at rear — positional missed", "Consider True North next time" })
                        .Alternatives(new[] { "Use True North to guarantee rear (if off cooldown)", "Adjust position pre-emptively" })
                        .Tip("Wheeling Thrust requires being at the target's rear. Coordinate with Chaos Thrust to manage rear positioning.")
                        .Concept(DrgConcepts.Positionals)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(DrgConcepts.Positionals, positionalOk, positionalOk ? "Rear positional hit" : "Rear positional missed");

                    return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Single-Target Combo

    private bool TrySingleTargetCombo(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;
        var lastAction = context.LastComboAction;
        var comboActive = context.ComboTimeRemaining > 0;

        // After Vorpal Thrust -> Heavens' Thrust / Full Thrust
        if (comboActive && lastAction == DRGActions.VorpalThrust.ActionId)
        {
            var finisher = DRGActions.GetVorpalFinisher((byte)level);
            if (context.ActionService.IsActionReady(finisher.ActionId))
            {
                if (context.ActionService.ExecuteGcd(finisher, target.GameObjectId))
                {
                    context.Debug.PlannedAction = finisher.Name;
                    context.Debug.DamageState = finisher.Name;

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(finisher.ActionId, finisher.Name)
                        .AsCombo(3)
                        .Target(target.Name?.TextValue)
                        .Reason($"{finisher.Name} — combo step 3 (Vorpal Thrust finisher)",
                            $"{finisher.Name} is the high-potency finisher of the Heavens' Thrust combo chain " +
                            "(True Thrust → Vorpal Thrust → " + finisher.Name + "). " +
                            "It grants the Fang and Claw Bared proc (or Drakesbane at Lv.92+) and 1 Firstmind's Focus stack. " +
                            "Always pair with Life Surge for a guaranteed critical hit.")
                        .Factors(new[] { "Vorpal Thrust combo active", "Step 3 of Heavens' Thrust chain", "Grants Fang and Claw Bared / Drakesbane at Lv.92+" })
                        .Alternatives(new[] { "Drop combo and restart (large potency loss)", "Switch to Disembowel line (if DoT urgently needs refresh)" })
                        .Tip("Use Life Surge before this combo step for a guaranteed critical hit at maximum potency.")
                        .Concept(DrgConcepts.ComboBasics)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(DrgConcepts.ComboBasics, true, "Heavens' Thrust chain finisher");

                    return true;
                }
            }
        }

        // After Disembowel -> Chaotic Spring / Chaos Thrust (rear positional)
        if (comboActive && lastAction == DRGActions.Disembowel.ActionId)
        {
            var finisher = DRGActions.GetDisembowelFinisher((byte)level);
            if (context.ActionService.IsActionReady(finisher.ActionId))
            {
                var positionalOk = context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                if (context.ActionService.ExecuteGcd(finisher, target.GameObjectId))
                {
                    context.Debug.PlannedAction = finisher.Name;
                    context.Debug.DamageState = $"{finisher.Name} {(positionalOk ? "(rear OK)" : "(rear!)")}";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(finisher.ActionId, finisher.Name)
                        .AsPositional(positionalOk, "rear")
                        .Target(target.Name?.TextValue)
                        .Reason(positionalOk
                            ? $"{finisher.Name} — rear positional hit (combo step 3)"
                            : $"{finisher.Name} — rear positional missed (combo step 3)",
                            $"{finisher.Name} is the finisher of the Chaos Thrust combo chain " +
                            "(True Thrust → Disembowel → " + finisher.Name + "). " +
                            "It applies a damage-over-time debuff and refreshes Power Surge. Must be used from the rear for bonus potency.")
                        .Factors(positionalOk
                            ? new[] { "Disembowel combo active", "Rear position achieved", "DoT applied/refreshed", "Power Surge refreshed" }
                            : new[] { "Disembowel combo active", "Rear positional missed", "DoT applied/refreshed", "Power Surge refreshed" })
                        .Alternatives(new[] { "Use True North for guaranteed rear (if available)", "Reposition before the combo step" })
                        .Tip($"{finisher.Name} requires rear positioning. Plan ahead — reposition during the Disembowel step.")
                        .Concept(DrgConcepts.DotMaintenance)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(DrgConcepts.DotMaintenance, true, "Chaos Thrust DoT refreshed");

                    return true;
                }
            }
        }

        // After True Thrust -> Choose Vorpal or Disembowel
        if (comboActive && lastAction == DRGActions.TrueThrust.ActionId)
        {
            // Prefer Disembowel line if:
            // 1. Power Surge is missing or about to expire (< 10s)
            // 2. DoT is missing or about to expire (< 5s)
            var needsPowerSurge = !context.HasPowerSurge || context.PowerSurgeRemaining < 10f;
            var needsDot = !context.HasDotOnTarget || context.DotRemaining < 5f;

            if ((needsPowerSurge || needsDot) && level >= DRGActions.Disembowel.MinLevel)
            {
                if (context.ActionService.IsActionReady(DRGActions.Disembowel.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(DRGActions.Disembowel, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = DRGActions.Disembowel.Name;
                        context.Debug.DamageState = $"Disembowel (PS: {context.PowerSurgeRemaining:F1}s)";

                        var psReason = needsPowerSurge ? $"Power Surge expires in {context.PowerSurgeRemaining:F1}s" : "DoT expires soon";
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(DRGActions.Disembowel.ActionId, DRGActions.Disembowel.Name)
                            .AsCombo(2)
                            .Target(target.Name?.TextValue)
                            .Reason($"Disembowel to refresh Power Surge/DoT ({psReason})",
                                "Disembowel is combo step 2 of the Chaos Thrust chain (True Thrust → Disembowel → Chaotic Spring/Chaos Thrust). " +
                                "It grants Power Surge (+10% damage for 30s) and leads to the DoT finisher. " +
                                "Use this line when Power Surge or the DoT is about to expire.")
                            .Factors(new[] { psReason, "True Thrust combo active", "Power Surge/DoT maintenance priority" })
                            .Alternatives(new[] { "Use Vorpal Thrust instead (higher damage but skips DoT refresh — risky if DoT is low)" })
                            .Tip("Alternate between the Heavens' Thrust and Chaos Thrust lines, prioritizing Disembowel when Power Surge or DoT runs low.")
                            .Concept(DrgConcepts.PowerSurge)
                            .Record();
                        context.TrainingService?.RecordConceptApplication(DrgConcepts.PowerSurge, true, "Disembowel chosen to maintain Power Surge/DoT");

                        return true;
                    }
                }
            }

            // Otherwise use Vorpal Thrust for damage
            if (level >= DRGActions.VorpalThrust.MinLevel)
            {
                if (context.ActionService.IsActionReady(DRGActions.VorpalThrust.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(DRGActions.VorpalThrust, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = DRGActions.VorpalThrust.Name;
                        context.Debug.DamageState = "Vorpal Thrust";

                        TrainingHelper.Decision(context.TrainingService)
                            .Action(DRGActions.VorpalThrust.ActionId, DRGActions.VorpalThrust.Name)
                            .AsCombo(2)
                            .Target(target.Name?.TextValue)
                            .Reason("Vorpal Thrust — combo step 2 (Heavens' Thrust line)",
                                "Vorpal Thrust is combo step 2 of the Heavens' Thrust chain (True Thrust → Vorpal Thrust → Heavens' Thrust). " +
                                "Choose this line when Power Surge and the DoT are healthy (remaining time is comfortable). " +
                                "The Heavens' Thrust line deals higher direct damage than the Disembowel line.")
                            .Factors(new[] { "True Thrust combo active", "Power Surge and DoT not urgently needed", "Higher direct damage line" })
                            .Alternatives(new[] { "Use Disembowel instead if Power Surge < 10s or DoT < 5s" })
                            .Tip("Alternate between Heavens' Thrust and Chaos Thrust lines to keep Power Surge and DoT uptime near 100%.")
                            .Concept(DrgConcepts.ComboBasics)
                            .Record();
                        context.TrainingService?.RecordConceptApplication(DrgConcepts.ComboBasics, true, "Vorpal Thrust chosen for Heavens' Thrust line");

                        return true;
                    }
                }
            }
        }

        // Start combo with True Thrust
        if (context.ActionService.IsActionReady(DRGActions.TrueThrust.ActionId))
        {
            if (context.ActionService.ExecuteGcd(DRGActions.TrueThrust, target.GameObjectId))
            {
                context.Debug.PlannedAction = DRGActions.TrueThrust.Name;
                context.Debug.DamageState = "True Thrust";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(DRGActions.TrueThrust.ActionId, DRGActions.TrueThrust.Name)
                    .AsCombo(1)
                    .Target(target.Name?.TextValue)
                    .Reason("True Thrust — starting combo step 1",
                        "True Thrust is DRG's primary combo starter. Every single-target rotation begins here. " +
                        "It opens two combo chains: Vorpal Thrust → Heavens' Thrust (pure damage) and " +
                        "Disembowel → Chaotic Spring/Chaos Thrust (DoT + Power Surge maintenance). " +
                        "Alternate between the two chains for maximum uptime.")
                    .Factors(new[] { "No active combo — starting fresh", "Combo starter for both ST chains", "Always the correct opener when no combo is active" })
                    .Alternatives(new[] { "Use AoE combo if 3+ enemies nearby (Doom Spike)" })
                    .Tip("True Thrust is always step 1. After it, choose Vorpal Thrust (high damage) or Disembowel (DoT/Power Surge maintenance) based on buff timers.")
                    .Concept(DrgConcepts.ComboBasics)
                    .Record();
                context.TrainingService?.RecordConceptApplication(DrgConcepts.ComboBasics, true, "True Thrust combo started");

                return true;
            }
        }

        return false;
    }

    #endregion

    #region AoE Combo

    private bool TryAoeCombo(IZeusContext context, IBattleChara target)
    {
        var level = context.Player.Level;
        var lastAction = context.LastComboAction;
        var comboActive = context.ComboTimeRemaining > 0;

        // After Sonic Thrust -> Coerthan Torment
        if (comboActive && lastAction == DRGActions.SonicThrust.ActionId &&
            level >= DRGActions.CoerthanTorment.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRGActions.CoerthanTorment.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRGActions.CoerthanTorment, target.GameObjectId))
                {
                    context.Debug.PlannedAction = DRGActions.CoerthanTorment.Name;
                    context.Debug.DamageState = "Coerthan Torment";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DRGActions.CoerthanTorment.ActionId, DRGActions.CoerthanTorment.Name)
                        .AsAoE(3)
                        .Target(target.Name?.TextValue)
                        .Reason("Coerthan Torment — AoE combo step 3 (finisher)",
                            "Coerthan Torment is the AoE combo finisher (Doom Spike → Sonic Thrust → Coerthan Torment). " +
                            "It hits all nearby enemies and grants 1 Firstmind's Focus stack. " +
                            "Use Life Surge before it for a guaranteed critical hit on multiple targets.")
                        .Factors(new[] { "Sonic Thrust combo active", "3+ enemies nearby", "AoE finisher — grants Firstmind's Focus" })
                        .Alternatives(new[] { "Switch to ST if enemies dropped below 3", "Hold (loses the combo)" })
                        .Tip("Coerthan Torment grants Firstmind's Focus — stay on the AoE combo when 3+ enemies are present.")
                        .Concept(DrgConcepts.AoeRotation)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(DrgConcepts.AoeRotation, true, "AoE finisher Coerthan Torment used");

                    return true;
                }
            }
        }

        // After Doom Spike -> Sonic Thrust
        if (comboActive && lastAction == DRGActions.DoomSpike.ActionId &&
            level >= DRGActions.SonicThrust.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRGActions.SonicThrust.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRGActions.SonicThrust, target.GameObjectId))
                {
                    context.Debug.PlannedAction = DRGActions.SonicThrust.Name;
                    context.Debug.DamageState = "Sonic Thrust";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DRGActions.SonicThrust.ActionId, DRGActions.SonicThrust.Name)
                        .AsAoE(3)
                        .Target(target.Name?.TextValue)
                        .Reason("Sonic Thrust — AoE combo step 2",
                            "Sonic Thrust is AoE combo step 2 (Doom Spike → Sonic Thrust → Coerthan Torment). " +
                            "It hits all nearby enemies, refreshes the AoE DoT, and continues toward the Coerthan Torment finisher.")
                        .Factors(new[] { "Doom Spike combo active", "3+ enemies nearby", "Refreshes AoE DoT" })
                        .Alternatives(new[] { "Restart with single-target if enemies dropped below 3" })
                        .Tip("Follow Sonic Thrust immediately with Coerthan Torment to complete the AoE finisher chain.")
                        .Concept(DrgConcepts.AoeRotation)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(DrgConcepts.AoeRotation, true, "AoE step 2 Sonic Thrust used");

                    return true;
                }
            }
        }

        // Start AoE combo with Doom Spike
        if (level >= DRGActions.DoomSpike.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRGActions.DoomSpike.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRGActions.DoomSpike, target.GameObjectId))
                {
                    context.Debug.PlannedAction = DRGActions.DoomSpike.Name;
                    context.Debug.DamageState = "Doom Spike";

                    TrainingHelper.Decision(context.TrainingService)
                        .Action(DRGActions.DoomSpike.ActionId, DRGActions.DoomSpike.Name)
                        .AsAoE(3)
                        .Target(target.Name?.TextValue)
                        .Reason("Doom Spike — starting AoE combo (3+ enemies)",
                            "Doom Spike is DRG's AoE combo starter (Doom Spike → Sonic Thrust → Coerthan Torment). " +
                            "Use this chain instead of the single-target rotation when 3 or more enemies are in range. " +
                            "The full AoE combo deals more total damage than single-target against 3+ targets.")
                        .Factors(new[] { "3+ enemies nearby", "AoE combo starter", "Full chain: Doom Spike → Sonic Thrust → Coerthan Torment" })
                        .Alternatives(new[] { "Single-target combo (if fewer than 3 enemies)", "Check enemy count before committing" })
                        .Tip("Switch to the AoE rotation at 3+ enemies. Complete the full chain to Coerthan Torment for Firstmind's Focus.")
                        .Concept(DrgConcepts.AoeRotation)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(DrgConcepts.AoeRotation, true, "AoE combo started with Doom Spike");

                    return true;
                }
            }
        }

        // Fall back to single target if AoE not available
        return TrySingleTargetCombo(context, target);
    }

    #endregion

    #region Smart AoE

    protected override uint GetNextDirectionalAoEActionId(IZeusContext context, IBattleChara target, int enemyCount)
    {
        if (enemyCount < AoeThreshold) return 0;
        if (context.Player.Level >= DRGActions.DoomSpike.MinLevel)
            return DRGActions.DoomSpike.ActionId; // Line AoE
        return 0;
    }

    #endregion
}
