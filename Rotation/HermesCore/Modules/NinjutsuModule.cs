using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HermesCore.Context;
using Olympus.Rotation.HermesCore.Helpers;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.HermesCore.Modules;

/// <summary>
/// Handles Ninja mudra sequences and Ninjutsu execution (scheduler-driven).
/// Bypasses the scheduler queue for mudra/ninjutsu/TCJ dispatch because UseAction
/// rejects the replacement action IDs the ninjutsu chain produces — fires raw via
/// ActionManager from CollectCandidates. Other modules push to scheduler normally.
/// </summary>
public sealed class NinjutsuModule : IHermesModule
{
    public int Priority => 10;
    public string Name => "Ninjutsu";

    public bool TryExecute(IHermesContext context, bool isMoving) => false;

    public void UpdateDebugState(IHermesContext context) { }

    public void CollectCandidates(IHermesContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.NinjutsuState = "Not in combat";
            return;
        }

        var player = context.Player;
        var level = player.Level;
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            20f,
            player);

        if (target == null && !context.MudraHelper.IsSequenceActive)
        {
            context.Debug.NinjutsuState = "No target";
            return;
        }

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);

        if (context.MudraHelper.IsSequenceActive)
        {
            ContinueMudraSequence(context, target);
            return;
        }

        if (context.HasTenChiJin)
        {
            HandleTenChiJin(context, target, enemyCount);
            return;
        }

        if (ShouldStartNinjutsu(context, level, enemyCount))
        {
            StartNinjutsuSequence(context, target, enemyCount);
        }
    }

    private void ContinueMudraSequence(IHermesContext context, IBattleChara? target)
    {
        var mudraHelper = context.MudraHelper;

        if (mudraHelper.IsReadyToExecute)
        {
            if (context.CanExecuteGcd)
            {
                ExecuteNinjutsu(context, target);
                return;
            }
            context.Debug.NinjutsuState = "Waiting for GCD to execute Ninjutsu";
            return;
        }

        InputNextMudra(context);
    }

    private unsafe void InputNextMudra(IHermesContext context)
    {
        var mudraHelper = context.MudraHelper;
        var nextMudra = mudraHelper.GetNextMudra();
        if (nextMudra == NINActions.MudraType.None)
        {
            context.Debug.NinjutsuState = "Invalid mudra sequence";
            mudraHelper.Reset();
            return;
        }

        var mudraAction = NINActions.GetMudraAction(nextMudra);
        var actionManager = SafeGameAccess.GetActionManager(null);
        if (actionManager == null) return;

        if (actionManager->GetActionStatus(ActionType.Action, mudraAction.ActionId) != 0)
        {
            context.Debug.NinjutsuState = $"Waiting for {mudraAction.Name}";
            return;
        }

        if (actionManager->UseAction(ActionType.Action, mudraAction.ActionId, context.Player.GameObjectId))
        {
            mudraHelper.AdvanceSequence();
            context.Debug.PlannedAction = mudraAction.Name;
            context.Debug.NinjutsuState = $"Input {mudraAction.Name} ({mudraHelper.MudraCount}/{mudraHelper.GetRequiredMudraCount()})";
        }
    }

    private unsafe void ExecuteNinjutsu(IHermesContext context, IBattleChara? target)
    {
        var mudraHelper = context.MudraHelper;
        var targetNinjutsu = mudraHelper.TargetNinjutsu;

        var ninjutsuAction = GetNinjutsuAction(targetNinjutsu, context.HasKassatsu, context.Player.Level);
        if (ninjutsuAction == null)
        {
            context.Debug.NinjutsuState = "Invalid Ninjutsu";
            mudraHelper.Reset();
            return;
        }

        var targetId = target?.GameObjectId ?? context.Player.GameObjectId;
        if (targetNinjutsu == NINActions.NinjutsuType.Huton) targetId = context.Player.GameObjectId;

        var actionManager = SafeGameAccess.GetActionManager(null);
        if (actionManager == null)
        {
            context.Debug.NinjutsuState = "ActionManager unavailable";
            return;
        }

        var adjustedId = actionManager->GetAdjustedActionId(NINActions.Ninjutsu.ActionId);
        if (actionManager->GetActionStatus(ActionType.Action, adjustedId) != 0)
        {
            context.Debug.NinjutsuState = $"Waiting for {ninjutsuAction.Name}";
            return;
        }

        if (actionManager->UseAction(ActionType.Action, NINActions.Ninjutsu.ActionId, targetId))
        {
            context.Debug.PlannedAction = ninjutsuAction.Name;
            context.Debug.NinjutsuState = $"Executed {ninjutsuAction.Name}";
            mudraHelper.CompleteSequence();

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
        }
    }

    private bool ShouldStartNinjutsu(IHermesContext context, byte level, int enemyCount)
    {
        if (!context.Configuration.Ninja.EnableNinjutsu) return false;
        if (level < NINActions.Ten.MinLevel) return false;
        if (!context.ActionService.IsActionReady(NINActions.Ten.ActionId)) return false;
        if (context.HasKassatsu) return true;
        if (NeedsSuiton(context, level)) return true;
        return context.CanExecuteOgcd;
    }

    private bool NeedsSuiton(IHermesContext context, byte level)
    {
        if (level < NINActions.Suiton.MinLevel) return false;
        if (context.HasSuiton) return false;
        var kunaiAction = level >= NINActions.KunaisBane.MinLevel ? NINActions.KunaisBane : NINActions.TrickAttack;
        return context.ActionService.IsActionReady(kunaiAction.ActionId);
    }

    private void StartNinjutsuSequence(IHermesContext context, IBattleChara? target, int enemyCount)
    {
        var level = context.Player.Level;
        var needsSuiton = NeedsSuiton(context, level);

        var ninjutsu = MudraHelper.GetRecommendedNinjutsu(
            level, context.HasKassatsu, needsSuiton, enemyCount,
            context.Configuration.Ninja.UseDotonForAoE,
            context.Configuration.Ninja.DotonMinTargets);

        if (ninjutsu == NINActions.NinjutsuType.None)
        {
            context.Debug.NinjutsuState = "No recommended Ninjutsu";
            return;
        }

        context.MudraHelper.StartSequence(ninjutsu);
        context.Debug.NinjutsuState = $"Starting {ninjutsu}";

        InputNextMudra(context);
    }

    private unsafe void HandleTenChiJin(IHermesContext context, IBattleChara? target, int enemyCount)
    {
        if (!context.Configuration.Ninja.EnableTenChiJin) return;
        if (!context.CanExecuteGcd)
        {
            context.Debug.NinjutsuState = "TCJ: Waiting for GCD";
            return;
        }

        var stacks = context.TenChiJinStacks;
        var targetId = target?.GameObjectId ?? context.Player.GameObjectId;

        uint baseActionId;
        Models.Action.ActionDefinition displayAction;
        string actionName;

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
            return;
        }

        context.Debug.NinjutsuState = $"TCJ: Waiting for {displayAction.Name}";

        var actionManager = SafeGameAccess.GetActionManager(null);
        if (actionManager == null) return;

        var adjustedId = actionManager->GetAdjustedActionId(baseActionId);
        if (actionManager->GetActionStatus(ActionType.Action, adjustedId) != 0) return;

        if (actionManager->UseAction(ActionType.Action, baseActionId, targetId))
        {
            context.Debug.PlannedAction = displayAction.Name;
            context.Debug.NinjutsuState = actionName;

            var tcjConceptId = stacks == 1
                ? (enemyCount >= context.Configuration.Ninja.AoEMinTargets ? NinConcepts.AoeNinjutsu : NinConcepts.Suiton)
                : NinConcepts.TcjOptimization;
            TrainingHelper.Decision(context.TrainingService)
                .Action(displayAction.ActionId, displayAction.Name)
                .AsMeleeBurst()
                .Target(target?.Name?.TextValue ?? "Target")
                .Reason($"TCJ step {4 - stacks}/3: {displayAction.Name}",
                    "Ten Chi Jin lets you use three Ninjutsu instantly. Standard sequence: Fuma Shuriken (Ten) → " +
                    "Raiton/Katon (Chi) → Suiton/Doton (Jin). Movement cancels TCJ.")
                .Factors(new[] { "TCJ active", $"Step {4 - stacks} of 3", $"{enemyCount} enemies nearby" })
                .Alternatives(new[] { "Cannot deviate — TCJ sequences are locked in order" })
                .Tip("TCJ is cancelled by movement.")
                .Concept(tcjConceptId)
                .Record();
            context.TrainingService?.RecordConceptApplication(tcjConceptId, true, $"TCJ step {4 - stacks}");
        }
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

    private static string GetNinjutsuConceptId(NINActions.NinjutsuType ninjutsu) => ninjutsu switch
    {
        NINActions.NinjutsuType.Suiton => NinConcepts.Suiton,
        NINActions.NinjutsuType.Raiton => NinConcepts.RaijuProcs,
        NINActions.NinjutsuType.Katon or NINActions.NinjutsuType.GokaMekkyaku => NinConcepts.AoeNinjutsu,
        NINActions.NinjutsuType.HyoshoRanryu => NinConcepts.Kassatsu,
        NINActions.NinjutsuType.Doton => NinConcepts.AoeNinjutsu,
        _ => NinConcepts.MudraSystem
    };

    private static string GetNinjutsuExplanation(NINActions.NinjutsuType ninjutsu, bool hasKassatsu)
    {
        if (hasKassatsu)
            return "Kassatsu enhances your next Ninjutsu. Hyosho Ranryu (from Hyoton combo) is highest ST damage.";
        return ninjutsu switch
        {
            NINActions.NinjutsuType.Suiton => "Suiton enables Kunai's Bane.",
            NINActions.NinjutsuType.Raiton => "Raiton is your primary ST Ninjutsu. Grants Raiju Ready.",
            NINActions.NinjutsuType.Katon => "Katon is your AoE Ninjutsu.",
            NINActions.NinjutsuType.Doton => "Doton creates a ground AoE DoT.",
            _ => "Ninjutsu are executed by inputting mudra combinations."
        };
    }

    private static string[] GetNinjutsuFactors(NINActions.NinjutsuType ninjutsu, IHermesContext context) => ninjutsu switch
    {
        NINActions.NinjutsuType.Suiton => new[] { "Kunai's Bane ready", "Burst window preparation" },
        NINActions.NinjutsuType.Raiton => new[] { "ST damage priority", "Grants Raiju Ready", context.HasKassatsu ? "Kassatsu active" : "Standard Raiton" },
        NINActions.NinjutsuType.Katon => new[] { "3+ enemies detected", context.HasKassatsu ? "Kassatsu → Goka Mekkyaku" : "Standard Katon" },
        _ => new[] { "Mudra sequence complete", "Ninjutsu ready" }
    };

    private static string[] GetNinjutsuAlternatives(NINActions.NinjutsuType ninjutsu) => ninjutsu switch
    {
        NINActions.NinjutsuType.Suiton => new[] { "Use Raiton (loses burst window)" },
        NINActions.NinjutsuType.Raiton => new[] { "Use Suiton (if burst coming)" },
        _ => new[] { "Different Ninjutsu (situational)" }
    };

    private static string GetNinjutsuTip(NINActions.NinjutsuType ninjutsu) => ninjutsu switch
    {
        NINActions.NinjutsuType.Suiton => "Time Suiton so Kunai's Bane is ready when the buff is applied.",
        NINActions.NinjutsuType.Raiton => "Raiton → Raiju is free damage.",
        NINActions.NinjutsuType.Katon => "With Kassatsu, this becomes Goka Mekkyaku.",
        _ => "Master your mudra sequences."
    };

    private static Models.Action.ActionDefinition? GetNinjutsuAction(
        NINActions.NinjutsuType ninjutsu, bool hasKassatsu, byte level)
    {
        if (hasKassatsu)
        {
            return ninjutsu switch
            {
                NINActions.NinjutsuType.Katon or NINActions.NinjutsuType.GokaMekkyaku
                    when level >= NINActions.GokaMekkyaku.MinLevel => NINActions.GokaMekkyaku,
                NINActions.NinjutsuType.Hyoton or NINActions.NinjutsuType.HyoshoRanryu
                    when level >= NINActions.HyoshoRanryu.MinLevel => NINActions.HyoshoRanryu,
                NINActions.NinjutsuType.Raiton => NINActions.Raiton,
                _ => GetBaseNinjutsuAction(ninjutsu)
            };
        }
        return GetBaseNinjutsuAction(ninjutsu);
    }

    private static Models.Action.ActionDefinition? GetBaseNinjutsuAction(NINActions.NinjutsuType ninjutsu) => ninjutsu switch
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
