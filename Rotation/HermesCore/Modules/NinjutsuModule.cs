using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.HermesCore.Context;
using Olympus.Rotation.HermesCore.Helpers;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.HermesCore.Modules;

/// <summary>
/// Handles Ninja mudra sequences and Ninjutsu execution.
/// Highest priority module - must complete mudra sequences once started.
/// </summary>
public sealed class NinjutsuModule : IHermesModule
{
    public int Priority => 10; // Highest priority - mudras must complete
    public string Name => "Ninjutsu";

    public bool TryExecute(IHermesContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.NinjutsuState = "Not in combat";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Find target
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            20f, // Ninjutsu has 20-25y range
            player);

        if (target == null && !context.MudraHelper.IsSequenceActive)
        {
            context.Debug.NinjutsuState = "No target";
            return false;
        }

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);

        // If we're in the middle of a mudra sequence, continue it
        // (IsSequenceActive auto-resets after 7s if the game's mudra timed out)
        if (context.MudraHelper.IsSequenceActive)
        {
            return ContinueMudraSequence(context, target);
        }

        // If mudra is on cooldown or we can't start a sequence, let other modules handle it
        if (!context.CanExecuteOgcd && !context.CanExecuteGcd)
        {
            context.Debug.NinjutsuState = "Waiting for action window";
            return false;
        }

        // Handle Ten Chi Jin mode
        if (context.HasTenChiJin)
        {
            return HandleTenChiJin(context, target, enemyCount);
        }

        // Decide whether to start a new Ninjutsu
        if (ShouldStartNinjutsu(context, level, enemyCount))
        {
            return StartNinjutsuSequence(context, target, enemyCount);
        }

        context.Debug.NinjutsuState = "No Ninjutsu needed";
        return false;
    }

    public void UpdateDebugState(IHermesContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Mudra Sequence

    private bool ContinueMudraSequence(IHermesContext context, IBattleChara? target)
    {
        var mudraHelper = context.MudraHelper;

        // Ready to execute the Ninjutsu
        if (mudraHelper.IsReadyToExecute)
        {
            if (context.CanExecuteGcd)
            {
                return ExecuteNinjutsu(context, target);
            }
            context.Debug.NinjutsuState = "Waiting for GCD to execute Ninjutsu";
            return true; // Block other modules
        }

        // Need to input more mudras. Don't gate on CanExecuteOgcd — mid-sequence mudras
        // use a 0.5s shared recast that doesn't participate in the normal double-weave
        // system. InputNextMudra checks GetActionStatus directly to know if the mudra
        // is actually usable (accounts for animation lock and the 0.5s recast).
        return InputNextMudra(context);
    }

    private unsafe bool InputNextMudra(IHermesContext context)
    {
        var mudraHelper = context.MudraHelper;
        var nextMudra = mudraHelper.GetNextMudra();

        if (nextMudra == NINActions.MudraType.None)
        {
            context.Debug.NinjutsuState = "Invalid mudra sequence";
            mudraHelper.Reset();
            return false;
        }

        var mudraAction = NINActions.GetMudraAction(nextMudra);

        // Mid-sequence mudras share a 0.5s recast but the charge is already consumed by
        // the first mudra press. IsActionReady uses GetCurrentCharges which returns 0 after
        // the first mudra, causing a permanent lockup. Use GetActionStatus directly —
        // it returns 0 when the action is actually usable regardless of charge state.
        var actionManager = SafeGameAccess.GetActionManager(null);
        if (actionManager == null)
            return true; // Block other modules while we wait

        if (actionManager->GetActionStatus(ActionType.Action, mudraAction.ActionId) != 0)
        {
            context.Debug.NinjutsuState = $"Waiting for {mudraAction.Name}";
            return true;
        }

        if (actionManager->UseAction(ActionType.Action, mudraAction.ActionId, context.Player.GameObjectId))
        {
            mudraHelper.AdvanceSequence();
            context.Debug.PlannedAction = mudraAction.Name;
            context.Debug.NinjutsuState = $"Input {mudraAction.Name} ({mudraHelper.MudraCount}/{mudraHelper.GetRequiredMudraCount()})";
            return true;
        }

        return false;
    }

    private unsafe bool ExecuteNinjutsu(IHermesContext context, IBattleChara? target)
    {
        var mudraHelper = context.MudraHelper;
        var targetNinjutsu = mudraHelper.TargetNinjutsu;

        // Get the display action for debug/training (the specific ninjutsu we expect to fire)
        var ninjutsuAction = GetNinjutsuAction(targetNinjutsu, context.HasKassatsu, context.Player.Level);

        if (ninjutsuAction == null)
        {
            context.Debug.NinjutsuState = "Invalid Ninjutsu";
            mudraHelper.Reset();
            return false;
        }

        var targetId = target?.GameObjectId ?? context.Player.GameObjectId;

        // Self-targeted Ninjutsu (Huton, buffs)
        if (targetNinjutsu == NINActions.NinjutsuType.Huton)
        {
            targetId = context.Player.GameObjectId;
        }

        // Ninjutsu results (Raiton, Suiton, etc.) are replacement actions for the base
        // Ninjutsu action (2260). UseAction rejects replacement IDs but GetActionStatus
        // accepts them. Use the base Ninjutsu ID for UseAction — the game resolves it
        // internally to whatever the mudra sequence produced.
        var actionManager = SafeGameAccess.GetActionManager(null);
        if (actionManager == null)
        {
            context.Debug.NinjutsuState = "ActionManager unavailable";
            return true;
        }

        var adjustedId = actionManager->GetAdjustedActionId(NINActions.Ninjutsu.ActionId);
        if (actionManager->GetActionStatus(ActionType.Action, adjustedId) != 0)
        {
            context.Debug.NinjutsuState = $"Waiting for {ninjutsuAction.Name}";
            return true;
        }

        if (actionManager->UseAction(ActionType.Action, NINActions.Ninjutsu.ActionId, targetId))
        {
            context.Debug.PlannedAction = ninjutsuAction.Name;
            context.Debug.NinjutsuState = $"Executed {ninjutsuAction.Name}";
            mudraHelper.CompleteSequence();

            // Training: Record Ninjutsu execution
            var ninjutsuType = GetNinjutsuDescription(targetNinjutsu, context.HasKassatsu);
            var conceptId = GetNinjutsuConceptId(targetNinjutsu);
            TrainingHelper.Decision(context.TrainingService)
                .Action(ninjutsuAction.ActionId, ninjutsuAction.Name)
                .AsMeleeDamage()
                .Target(target?.Name?.TextValue ?? "Self")
                .Reason($"Executing {ninjutsuAction.Name} ({ninjutsuType})",
                    GetNinjutsuExplanation(targetNinjutsu, context.HasKassatsu))
                .Factors(GetNinjutsuFactors(targetNinjutsu, context))
                .Alternatives(GetNinjutsuAlternatives(targetNinjutsu))
                .Tip(GetNinjutsuTip(targetNinjutsu))
                .Concept(conceptId)
                .Record();
            context.TrainingService?.RecordConceptApplication(conceptId, true, ninjutsuType);

            return true;
        }

        return false;
    }

    private static string GetNinjutsuDescription(NINActions.NinjutsuType ninjutsu, bool hasKassatsu)
    {
        if (hasKassatsu)
        {
            return ninjutsu switch
            {
                NINActions.NinjutsuType.Katon or NINActions.NinjutsuType.GokaMekkyaku => "Enhanced AoE fire damage",
                NINActions.NinjutsuType.Hyoton or NINActions.NinjutsuType.HyoshoRanryu => "Enhanced ice burst",
                _ => "Kassatsu-enhanced"
            };
        }
        return ninjutsu switch
        {
            NINActions.NinjutsuType.FumaShuriken => "Ranged damage",
            NINActions.NinjutsuType.Raiton => "Single-target lightning",
            NINActions.NinjutsuType.Katon => "AoE fire damage",
            NINActions.NinjutsuType.Hyoton => "Ice damage + bind",
            NINActions.NinjutsuType.Huton => "Speed buff (obsolete)",
            NINActions.NinjutsuType.Doton => "Ground AoE DoT",
            NINActions.NinjutsuType.Suiton => "Setup for Kunai's Bane",
            _ => "Ninjutsu"
        };
    }

    private static string GetNinjutsuConceptId(NINActions.NinjutsuType ninjutsu)
    {
        return ninjutsu switch
        {
            NINActions.NinjutsuType.Suiton => NinConcepts.Suiton,
            NINActions.NinjutsuType.Raiton => NinConcepts.RaijuProcs, // Raiton → Raiju proc
            NINActions.NinjutsuType.Katon or NINActions.NinjutsuType.GokaMekkyaku => NinConcepts.AoeNinjutsu,
            NINActions.NinjutsuType.HyoshoRanryu => NinConcepts.Kassatsu,
            NINActions.NinjutsuType.Doton => NinConcepts.AoeNinjutsu,
            _ => NinConcepts.MudraSystem
        };
    }

    private static string GetNinjutsuExplanation(NINActions.NinjutsuType ninjutsu, bool hasKassatsu)
    {
        if (hasKassatsu)
        {
            return "Kassatsu enhances your next Ninjutsu. Hyosho Ranryu (from Hyoton combo) is highest ST damage. " +
                   "Goka Mekkyaku (from Katon combo) is best for AoE. Always use Kassatsu before these for maximum damage.";
        }
        return ninjutsu switch
        {
            NINActions.NinjutsuType.Suiton => "Suiton enables Kunai's Bane, your main burst window. The buff lasts 20s, so time it well.",
            NINActions.NinjutsuType.Raiton => "Raiton is your primary ST Ninjutsu. It also grants Raiju Ready for a free GCD follow-up.",
            NINActions.NinjutsuType.Katon => "Katon is your AoE Ninjutsu. Use on 3+ enemies. Enhanced to Goka Mekkyaku with Kassatsu.",
            NINActions.NinjutsuType.Doton => "Doton creates a ground AoE that deals damage over time. Best for trash packs that will stay in the zone.",
            _ => "Ninjutsu are executed by inputting mudra combinations (Ten, Chi, Jin) in specific sequences."
        };
    }

    private static string[] GetNinjutsuFactors(NINActions.NinjutsuType ninjutsu, IHermesContext context)
    {
        return ninjutsu switch
        {
            NINActions.NinjutsuType.Suiton => new[] { "Kunai's Bane ready", "Burst window preparation", "20s buff duration" },
            NINActions.NinjutsuType.Raiton => new[] { "ST damage priority", "Grants Raiju Ready", context.HasKassatsu ? "Kassatsu active" : "Standard Raiton" },
            NINActions.NinjutsuType.Katon => new[] { "3+ enemies detected", "AoE damage optimal", context.HasKassatsu ? "Kassatsu → Goka Mekkyaku" : "Standard Katon" },
            _ => new[] { "Mudra sequence complete", "Ninjutsu ready", "GCD available" }
        };
    }

    private static string[] GetNinjutsuAlternatives(NINActions.NinjutsuType ninjutsu)
    {
        return ninjutsu switch
        {
            NINActions.NinjutsuType.Suiton => new[] { "Use Raiton (loses burst window)", "Wait for later (if Kunai's Bane not ready)" },
            NINActions.NinjutsuType.Raiton => new[] { "Use Suiton (if burst coming)", "Use Katon (only if AoE)" },
            _ => new[] { "Different Ninjutsu (situational)", "Abort sequence (wastes mudra CD)" }
        };
    }

    private static string GetNinjutsuTip(NINActions.NinjutsuType ninjutsu)
    {
        return ninjutsu switch
        {
            NINActions.NinjutsuType.Suiton => "Time Suiton so Kunai's Bane is ready when the buff is applied.",
            NINActions.NinjutsuType.Raiton => "Raiton → Raiju is free damage. Use Raiju before it expires!",
            NINActions.NinjutsuType.Katon => "With Kassatsu, this becomes Goka Mekkyaku for massive AoE burst.",
            _ => "Master your mudra sequences - muscle memory makes NIN much smoother."
        };
    }

    private static Models.Action.ActionDefinition? GetNinjutsuAction(
        NINActions.NinjutsuType ninjutsu,
        bool hasKassatsu,
        byte level)
    {
        // Kassatsu upgrades
        if (hasKassatsu)
        {
            return ninjutsu switch
            {
                NINActions.NinjutsuType.Katon or NINActions.NinjutsuType.GokaMekkyaku
                    when level >= NINActions.GokaMekkyaku.MinLevel => NINActions.GokaMekkyaku,
                NINActions.NinjutsuType.Hyoton or NINActions.NinjutsuType.HyoshoRanryu
                    when level >= NINActions.HyoshoRanryu.MinLevel => NINActions.HyoshoRanryu,
                NINActions.NinjutsuType.Raiton => NINActions.Raiton, // Kassatsu Raiton is just better damage
                _ => GetBaseNinjutsuAction(ninjutsu)
            };
        }

        return GetBaseNinjutsuAction(ninjutsu);
    }

    private static Models.Action.ActionDefinition? GetBaseNinjutsuAction(NINActions.NinjutsuType ninjutsu)
    {
        return ninjutsu switch
        {
            NINActions.NinjutsuType.FumaShuriken => NINActions.FumaShuriken,
            NINActions.NinjutsuType.Raiton => NINActions.Raiton,
            NINActions.NinjutsuType.Katon => NINActions.Katon,
            NINActions.NinjutsuType.Hyoton => NINActions.Hyoton,
            NINActions.NinjutsuType.Huton => NINActions.Huton,
            NINActions.NinjutsuType.Doton => NINActions.Doton,
            NINActions.NinjutsuType.Suiton => NINActions.Suiton,
            NINActions.NinjutsuType.GokaMekkyaku => NINActions.GokaMekkyaku,
            NINActions.NinjutsuType.HyoshoRanryu => NINActions.HyoshoRanryu,
            _ => null
        };
    }

    #endregion

    #region Start Ninjutsu

    private bool ShouldStartNinjutsu(IHermesContext context, byte level, int enemyCount)
    {
        if (!context.Configuration.Ninja.EnableNinjutsu) return false;
        // Can't use Ninjutsu below level 30
        if (level < NINActions.Ten.MinLevel)
            return false;

        // Check if mudra is on cooldown
        if (!context.ActionService.IsActionReady(NINActions.Ten.ActionId))
            return false;

        // Don't start during Kassatsu if we're not ready to burst
        if (context.HasKassatsu)
            return true; // Always use Kassatsu when available

        // Need Suiton for Kunai's Bane window
        if (NeedsSuiton(context, level))
            return true;

        // Standard Ninjutsu usage
        return context.CanExecuteOgcd;
    }

    private bool NeedsSuiton(IHermesContext context, byte level)
    {
        // Can't use Suiton below level 45
        if (level < NINActions.Suiton.MinLevel)
            return false;

        // Already have Suiton
        if (context.HasSuiton)
            return false;

        // Check if Kunai's Bane (or Trick Attack) is coming off cooldown soon
        var kunaiAction = level >= NINActions.KunaisBane.MinLevel
            ? NINActions.KunaisBane
            : NINActions.TrickAttack;

        // If Kunai's Bane/Trick Attack is ready or nearly ready, we need Suiton
        return context.ActionService.IsActionReady(kunaiAction.ActionId);
    }

    private bool StartNinjutsuSequence(IHermesContext context, IBattleChara? target, int enemyCount)
    {
        var level = context.Player.Level;
        var needsSuiton = NeedsSuiton(context, level);

        // Determine which Ninjutsu to use
        var ninjutsu = MudraHelper.GetRecommendedNinjutsu(
            level,
            context.HasKassatsu,
            needsSuiton,
            enemyCount,
            context.Configuration.Ninja.UseDotonForAoE,
            context.Configuration.Ninja.DotonMinTargets);

        if (ninjutsu == NINActions.NinjutsuType.None)
        {
            context.Debug.NinjutsuState = "No recommended Ninjutsu";
            return false;
        }

        // Start the sequence
        context.MudraHelper.StartSequence(ninjutsu);
        context.Debug.NinjutsuState = $"Starting {ninjutsu}";

        // Input the first mudra
        return InputNextMudra(context);
    }

    #endregion

    #region Ten Chi Jin

    private unsafe bool HandleTenChiJin(IHermesContext context, IBattleChara? target, int enemyCount)
    {
        if (!context.Configuration.Ninja.EnableTenChiJin) return false;

        // Ten Chi Jin replaces mudra buttons with ninjutsu actions:
        //   Ten → Fuma Shuriken, Chi → Raiton/Katon, Jin → Suiton/Doton
        // UseAction rejects replacement IDs, so pass base mudra IDs (Ten/Chi/Jin).
        // The game resolves them internally during TCJ.
        if (!context.CanExecuteGcd)
        {
            context.Debug.NinjutsuState = "TCJ: Waiting for GCD";
            return true;
        }

        var stacks = context.TenChiJinStacks;
        var targetId = target?.GameObjectId ?? context.Player.GameObjectId;

        uint baseActionId;
        Models.Action.ActionDefinition displayAction;
        string actionName;

        // TCJ sequence based on remaining stacks — use base mudra IDs
        if (stacks >= 3)
        {
            baseActionId = NINActions.Ten.ActionId;
            displayAction = NINActions.FumaShuriken;
            actionName = "TCJ: Fuma Shuriken";
        }
        else if (stacks == 2)
        {
            baseActionId = NINActions.Chi.ActionId;
            if (enemyCount >= context.Configuration.Ninja.AoEMinTargets)
            {
                displayAction = NINActions.Katon;
                actionName = "TCJ: Katon";
            }
            else
            {
                displayAction = NINActions.Raiton;
                actionName = "TCJ: Raiton";
            }
        }
        else if (stacks == 1)
        {
            baseActionId = NINActions.Jin.ActionId;
            if (enemyCount >= context.Configuration.Ninja.AoEMinTargets)
            {
                displayAction = NINActions.Doton;
                actionName = "TCJ: Doton";
            }
            else
            {
                displayAction = NINActions.Suiton;
                actionName = "TCJ: Suiton";
            }
        }
        else
        {
            context.Debug.NinjutsuState = "TCJ complete";
            return false;
        }

        // Set debug state for what we're attempting (visible even when native call is unavailable)
        context.Debug.NinjutsuState = $"TCJ: Waiting for {displayAction.Name}";

        var actionManager = SafeGameAccess.GetActionManager(null);
        if (actionManager == null)
            return true;

        // Check readiness via the adjusted (replacement) ID, execute with the base mudra ID
        var adjustedId = actionManager->GetAdjustedActionId(baseActionId);
        if (actionManager->GetActionStatus(ActionType.Action, adjustedId) != 0)
            return true;

        if (actionManager->UseAction(ActionType.Action, baseActionId, targetId))
        {
            context.Debug.PlannedAction = displayAction.Name;
            context.Debug.NinjutsuState = actionName;

            // Training: Record Ten Chi Jin action
            var tcjConceptId = stacks == 1
                ? (enemyCount >= context.Configuration.Ninja.AoEMinTargets ? NinConcepts.AoeNinjutsu : NinConcepts.Suiton)
                : NinConcepts.TcjOptimization;
            TrainingHelper.Decision(context.TrainingService)
                .Action(displayAction.ActionId, displayAction.Name)
                .AsMeleeBurst()
                .Target(target?.Name?.TextValue ?? "Target")
                .Reason($"TCJ step {4 - stacks}/3: {displayAction.Name}",
                    "Ten Chi Jin lets you use three Ninjutsu instantly. The standard sequence is: " +
                    "Fuma Shuriken (Ten) → Raiton/Katon (Chi) → Suiton/Doton (Jin). " +
                    "Do NOT move during TCJ — any movement cancels the entire effect. " +
                    "Use this combo inside your Kunai's Bane burst window for maximum damage.")
                .Factors(new[] { "TCJ active", $"Step {4 - stacks} of 3", $"{enemyCount} enemies nearby" })
                .Alternatives(new[] { "Cannot deviate — TCJ sequences are locked in order" })
                .Tip("TCJ is cancelled by movement. Cast it when you're safe to stand still for ~3 GCDs.")
                .Concept(tcjConceptId)
                .Record();
            context.TrainingService?.RecordConceptApplication(tcjConceptId, true, $"TCJ step {4 - stacks}");

            return true;
        }

        context.Debug.NinjutsuState = $"TCJ: Waiting for {displayAction.Name}";
        return true;
    }

    #endregion
}
