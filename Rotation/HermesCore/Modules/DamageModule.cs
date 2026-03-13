using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.HermesCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.HermesCore.Modules;

/// <summary>
/// Handles the Ninja damage rotation.
/// Manages combo GCDs, Ninki spenders, Raiju, and Phantom Kamaitachi.
/// Extends BaseDpsDamageModule for shared damage module patterns.
/// </summary>
public sealed class DamageModule : BaseDpsDamageModule<IHermesContext>, IHermesModule
{
    // Ninki threshold for spending
    private const int NinkiSpendThreshold = 50;

    // Kazematoi threshold for maintaining stacks
    private const int KazematoiLowThreshold = 1;

    #region Abstract Method Implementations

    /// <summary>
    /// Melee targeting range (3y).
    /// </summary>
    protected override float GetTargetingRange() => FFXIVConstants.MeleeTargetingRange;

    /// <summary>
    /// AoE count range for NIN (5y for melee AoE abilities).
    /// </summary>
    protected override float GetAoECountRange() => 5f;

    /// <summary>
    /// Sets the damage state in the debug display.
    /// </summary>
    protected override void SetDamageState(IHermesContext context, string state) =>
        context.Debug.DamageState = state;

    /// <summary>
    /// Sets the nearby enemy count in the debug display.
    /// </summary>
    protected override void SetNearbyEnemies(IHermesContext context, int count) =>
        context.Debug.NearbyEnemies = count;

    /// <summary>
    /// Sets the planned action name in the debug display.
    /// </summary>
    protected override void SetPlannedAction(IHermesContext context, string action) =>
        context.Debug.PlannedAction = action;

    /// <summary>
    /// oGCD damage for Ninja - Ninki spenders (Bhavacakra, Hellfrog).
    /// </summary>
    protected override bool TryOgcdDamage(IHermesContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Priority 1: Ninki spenders (50 Ninki required)
        if (TryNinkiSpender(context, target, enemyCount))
            return true;

        return false;
    }

    private bool TryNinkiSpender(IHermesContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Need 50 Ninki to spend
        if (context.Ninki < NinkiSpendThreshold)
            return false;

        // Choose ST or AoE based on enemy count
        if (enemyCount >= AoeThreshold && level >= NINActions.HellfrogMedium.MinLevel)
        {
            // Use AoE Ninki spender
            var aoeAction = NINActions.GetAoeNinkiSpender((byte)level, context.HasMeisui);
            if (context.ActionService.IsActionReady(aoeAction.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(aoeAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = aoeAction.Name;
                    context.Debug.DamageState = $"{aoeAction.Name} ({enemyCount} enemies)";

                    // Training: Record AoE Ninki spender
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(aoeAction.ActionId, aoeAction.Name)
                        .AsAoE(enemyCount)
                        .Target($"{enemyCount} enemies")
                        .Reason($"Spending 50 Ninki on {aoeAction.Name} ({enemyCount} enemies)",
                            $"{aoeAction.Name} is the AoE Ninki spender, dealing damage to all nearby enemies. " +
                            "Use when you have 3+ enemies. Spend Ninki regularly to avoid overcapping.")
                        .Factors(new[] { $"Ninki >= {NinkiSpendThreshold}", $"{enemyCount} enemies nearby", "AoE damage optimal" })
                        .Alternatives(new[] { "Use Bhavacakra (less total damage vs 3+)", "Hold for Bunshin (if coming soon)" })
                        .Tip("In AoE, Hellfrog Medium/Deathfrog Medium outperforms Bhavacakra at 3+ targets.")
                        .Concept("nin_ninki_gauge")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("nin_ninki_gauge", true, "AoE Ninki spending");

                    return true;
                }
            }
        }
        else if (level >= NINActions.HellfrogMedium.MinLevel)
        {
            // Use ST Ninki spender
            var stAction = NINActions.GetNinkiSpender((byte)level, context.HasMeisui);
            if (context.ActionService.IsActionReady(stAction.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(stAction, target.GameObjectId))
                {
                    context.Debug.PlannedAction = stAction.Name;
                    context.Debug.DamageState = stAction.Name;

                    // Training: Record ST Ninki spender
                    var meisuiNote = context.HasMeisui ? " (enhanced by Meisui)" : "";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(stAction.ActionId, stAction.Name)
                        .AsMeleeResource("Ninki", context.Ninki)
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Spending 50 Ninki on {stAction.Name}{meisuiNote}",
                            $"{stAction.Name} is your primary single-target Ninki spender. " +
                            "Use to avoid overcapping Ninki. If Meisui is active, the damage is enhanced. " +
                            "Prioritize Bunshin when it's ready, then spend excess Ninki on this.")
                        .Factors(new[] { $"Ninki >= {NinkiSpendThreshold}", "Single target damage", context.HasMeisui ? "Meisui buff active" : "Standard potency" })
                        .Alternatives(new[] { "Save for Bunshin (if coming soon)", "Overcap Ninki (wastes gauge)" })
                        .Tip("Spend Ninki before capping. Bunshin > Bhavacakra in priority, but don't sit on full gauge.")
                        .Concept("nin_ninki_gauge")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("nin_ninki_gauge", true, "ST Ninki spending");

                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Main GCD damage rotation for Ninja.
    /// Handles Raiju procs, Phantom Kamaitachi, and combo rotation.
    /// </summary>
    protected override bool TryGcdDamage(IHermesContext context, IBattleChara target, int enemyCount, bool isMoving)
    {
        // Priority 1: Raiju procs (from Raiton)
        if (TryRaiju(context, target))
            return true;

        // Priority 2: Phantom Kamaitachi (from Bunshin)
        if (TryPhantomKamaitachi(context, target))
            return true;

        // Priority 3: Combo rotation
        if (TryComboRotation(context, target, enemyCount))
            return true;

        return false;
    }

    #endregion

    #region Raiju

    private bool TryRaiju(IHermesContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < NINActions.ForkedRaiju.MinLevel)
            return false;

        // Need Raiju Ready buff
        if (!context.HasRaijuReady)
            return false;

        // Choose Forked (ranged gap closer) or Fleeting (melee) based on distance
        var dx = player.Position.X - target.Position.X;
        var dz = player.Position.Z - target.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dz * dz);

        Models.Action.ActionDefinition action;

        // Forked Raiju is a gap closer with 20y range
        // Fleeting Raiju is melee range
        if (distance > FFXIVConstants.MeleeTargetingRange + target.HitboxRadius)
        {
            action = NINActions.ForkedRaiju;
        }
        else
        {
            // At melee range, prefer Fleeting Raiju (same potency but doesn't move you)
            action = NINActions.FleetingRaiju;
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (Raiju proc)";

            // Training: Record Raiju decision
            var isForked = action.ActionId == NINActions.ForkedRaiju.ActionId;
            TrainingHelper.Decision(context.TrainingService)
                .Action(action.ActionId, action.Name)
                .AsMeleeDamage()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason(isForked ? "Using Forked Raiju (gap closer)" : "Using Fleeting Raiju (melee)",
                    "Raiju procs come from using Raiton. You have two options: Forked Raiju (20y gap closer) or Fleeting Raiju (melee). " +
                    "Both have the same potency. Use Forked when you need to close distance, Fleeting when already in melee. " +
                    "Raiju stacks can be held but use them before they expire.")
                .Factors(new[] { "Raiju Ready proc active", $"{context.RaijuStacks} stack(s) available", isForked ? "Out of melee range" : "In melee range" })
                .Alternatives(new[] { isForked ? "Walk to target (slower)" : "Use Forked for movement (unnecessary)" })
                .Tip("Raiju procs are free damage. Use them between your combo GCDs, ideally during burst windows.")
                .Concept("nin_raiju")
                .Record();
            context.TrainingService?.RecordConceptApplication("nin_raiju", true, "Raiju proc usage");

            return true;
        }

        return false;
    }

    #endregion

    #region Phantom Kamaitachi

    private bool TryPhantomKamaitachi(IHermesContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < NINActions.PhantomKamaitachi.MinLevel)
            return false;

        // Need Phantom Kamaitachi Ready buff
        if (!context.HasPhantomKamaitachiReady)
            return false;

        if (!context.ActionService.IsActionReady(NINActions.PhantomKamaitachi.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(NINActions.PhantomKamaitachi, target.GameObjectId))
        {
            context.Debug.PlannedAction = NINActions.PhantomKamaitachi.Name;
            context.Debug.DamageState = "Phantom Kamaitachi";

            // Training: Record Phantom Kamaitachi decision
            TrainingHelper.Decision(context.TrainingService)
                .Action(NINActions.PhantomKamaitachi.ActionId, NINActions.PhantomKamaitachi.Name)
                .AsMeleeDamage()
                .Target(target.Name?.TextValue ?? "Target")
                .Reason("Using Phantom Kamaitachi (Bunshin proc)",
                    "Phantom Kamaitachi is a powerful GCD that becomes available after using Bunshin. " +
                    "It has high potency (600) and does AoE damage. Use it before the Ready buff expires. " +
                    "This is part of the Bunshin → Phantom Kamaitachi combo.")
                .Factors(new[] { "Phantom Kamaitachi Ready proc", "From Bunshin usage", "High potency GCD" })
                .Alternatives(new[] { "Delay for burst (only if Bunshin was early)", "Let proc expire (wastes damage)" })
                .Tip("Always use Phantom Kamaitachi after Bunshin. It's free, high-potency damage.")
                .Concept("nin_phantom_kamaitachi")
                .Record();
            context.TrainingService?.RecordConceptApplication("nin_phantom_kamaitachi", true, "Bunshin follow-up");

            return true;
        }

        return false;
    }

    #endregion

    #region Combo Rotation

    private bool TryComboRotation(IHermesContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold && level >= NINActions.DeathBlossom.MinLevel;

        if (useAoe)
        {
            return TryAoeCombo(context, target);
        }
        else
        {
            return TrySingleTargetCombo(context, target);
        }
    }

    private bool TrySingleTargetCombo(IHermesContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;
        var comboStep = context.ComboStep;

        // Combo Step 2 -> Finisher
        if (comboStep == 2 && context.LastComboAction == NINActions.GustSlash.ActionId)
        {
            return TryComboFinisher(context, target);
        }

        // Combo Step 1 -> Gust Slash
        if (comboStep == 1 && context.LastComboAction == NINActions.SpinningEdge.ActionId && level >= NINActions.GustSlash.MinLevel)
        {
            if (context.ActionService.IsActionReady(NINActions.GustSlash.ActionId))
            {
                if (context.ActionService.ExecuteGcd(NINActions.GustSlash, target.GameObjectId))
                {
                    context.Debug.PlannedAction = NINActions.GustSlash.Name;
                    context.Debug.DamageState = "Gust Slash (Combo 2)";
                    return true;
                }
            }
        }

        // Start combo with Spinning Edge
        if (context.ActionService.IsActionReady(NINActions.SpinningEdge.ActionId))
        {
            if (context.ActionService.ExecuteGcd(NINActions.SpinningEdge, target.GameObjectId))
            {
                context.Debug.PlannedAction = NINActions.SpinningEdge.Name;
                context.Debug.DamageState = "Spinning Edge (Combo 1)";
                return true;
            }
        }

        return false;
    }

    private bool TryComboFinisher(IHermesContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Choose between Aeolian Edge (rear) and Armor Crush (flank)
        // Strategy:
        // - Aeolian Edge consumes Kazematoi for bonus potency
        // - Armor Crush grants Kazematoi stacks
        // - Need to maintain Kazematoi stacks

        bool useArmorCrush = false;

        if (level >= NINActions.ArmorCrush.MinLevel)
        {
            // Use Armor Crush if Kazematoi is low
            if (context.Kazematoi <= KazematoiLowThreshold)
            {
                useArmorCrush = true;
            }
            // Otherwise prefer Aeolian Edge (higher potency with Kazematoi)
        }

        if (useArmorCrush)
        {
            // Armor Crush - flank positional
            if (context.ActionService.IsActionReady(NINActions.ArmorCrush.ActionId))
            {
                bool correctPositional = context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                string positionalHint = correctPositional ? "(flank)" : "(WRONG)";

                if (context.ActionService.ExecuteGcd(NINActions.ArmorCrush, target.GameObjectId))
                {
                    context.Debug.PlannedAction = NINActions.ArmorCrush.Name;
                    context.Debug.DamageState = $"Armor Crush {positionalHint}";

                    // Training: Record Armor Crush with positional
                    var posReason = context.TargetHasPositionalImmunity ? "Target immune to positionals" :
                                   context.HasTrueNorth ? "True North active" :
                                   context.IsAtFlank ? "Correct flank position" : "MISSED flank positional";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(NINActions.ArmorCrush.ActionId, NINActions.ArmorCrush.Name)
                        .AsPositional(correctPositional, "Flank")
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Armor Crush for Kazematoi stacks ({context.Kazematoi} → {context.Kazematoi + 2})",
                            "Armor Crush is a flank positional that grants 2 Kazematoi stacks. " +
                            "Kazematoi enhances Aeolian Edge damage. Maintain 3-5 stacks for optimal DPS. " +
                            "Use Armor Crush when Kazematoi is low (0-1 stacks).")
                        .Factors(new[] { $"Kazematoi low ({context.Kazematoi} stacks)", "Need to build stacks", posReason })
                        .Alternatives(new[] { "Use Aeolian Edge (would run out of Kazematoi)", "Ignore positional (loses potency)" })
                        .Tip("Armor Crush builds Kazematoi. Aeolian Edge consumes it. Keep 3+ stacks when possible.")
                        .Concept("nin_positionals")
                        .Record();
                    context.TrainingService?.RecordConceptApplication("nin_positionals", correctPositional, "Flank positional");

                    return true;
                }
            }
        }
        else
        {
            // Aeolian Edge - rear positional
            if (level >= NINActions.AeolianEdge.MinLevel)
            {
                if (context.ActionService.IsActionReady(NINActions.AeolianEdge.ActionId))
                {
                    bool correctPositional = context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity;
                    string positionalHint = correctPositional ? "(rear)" : "(WRONG)";
                    string kazematoiHint = context.Kazematoi > 0 ? $" +Kaze:{context.Kazematoi}" : "";

                    if (context.ActionService.ExecuteGcd(NINActions.AeolianEdge, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = NINActions.AeolianEdge.Name;
                        context.Debug.DamageState = $"Aeolian Edge {positionalHint}{kazematoiHint}";

                        // Training: Record Aeolian Edge with positional
                        var posReason = context.TargetHasPositionalImmunity ? "Target immune to positionals" :
                                       context.HasTrueNorth ? "True North active" :
                                       context.IsAtRear ? "Correct rear position" : "MISSED rear positional";
                        var kazematoiBonus = context.Kazematoi > 0 ? $" (+{context.Kazematoi * 60} potency from Kazematoi)" : " (no Kazematoi bonus)";
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(NINActions.AeolianEdge.ActionId, NINActions.AeolianEdge.Name)
                            .AsPositional(correctPositional, "Rear")
                            .Target(target.Name?.TextValue ?? "Target")
                            .Reason($"Aeolian Edge for damage{kazematoiBonus}",
                                "Aeolian Edge is a rear positional and your main combo finisher. " +
                                "Consumes Kazematoi stacks for +60 potency per stack. " +
                                "Use when you have 3+ Kazematoi for maximum damage.")
                            .Factors(new[] { $"Kazematoi available ({context.Kazematoi} stacks)", "Primary damage finisher", posReason })
                            .Alternatives(new[] { "Use Armor Crush (if need Kazematoi stacks)", "Ignore positional (loses potency)" })
                            .Tip("Aeolian Edge is your bread-and-butter finisher. Keep Kazematoi up for bonus damage.")
                            .Concept("nin_positionals")
                            .Record();
                        context.TrainingService?.RecordConceptApplication("nin_positionals", correctPositional, "Rear positional");

                        return true;
                    }
                }
            }
        }

        // Fallback to Gust Slash if no finisher available (low level)
        if (context.ActionService.IsActionReady(NINActions.GustSlash.ActionId))
        {
            if (context.ActionService.ExecuteGcd(NINActions.GustSlash, target.GameObjectId))
            {
                context.Debug.PlannedAction = NINActions.GustSlash.Name;
                context.Debug.DamageState = "Gust Slash (no finisher)";
                return true;
            }
        }

        return false;
    }

    private bool TryAoeCombo(IHermesContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;
        var comboStep = context.ComboStep;

        // Combo Step 1 -> Hakke Mujinsatsu
        if (comboStep == 1 && context.LastComboAction == NINActions.DeathBlossom.ActionId && level >= NINActions.HakkeMujinsatsu.MinLevel)
        {
            if (context.ActionService.IsActionReady(NINActions.HakkeMujinsatsu.ActionId))
            {
                if (context.ActionService.ExecuteGcd(NINActions.HakkeMujinsatsu, target.GameObjectId))
                {
                    context.Debug.PlannedAction = NINActions.HakkeMujinsatsu.Name;
                    context.Debug.DamageState = "Hakke Mujinsatsu (AoE 2)";
                    return true;
                }
            }
        }

        // Start AoE combo with Death Blossom
        if (context.ActionService.IsActionReady(NINActions.DeathBlossom.ActionId))
        {
            if (context.ActionService.ExecuteGcd(NINActions.DeathBlossom, target.GameObjectId))
            {
                context.Debug.PlannedAction = NINActions.DeathBlossom.Name;
                context.Debug.DamageState = "Death Blossom (AoE 1)";
                return true;
            }
        }

        return false;
    }

    #endregion
}
