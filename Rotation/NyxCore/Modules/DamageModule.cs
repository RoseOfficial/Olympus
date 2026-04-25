using Olympus.Data;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.NyxCore.Abilities;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.NyxCore.Modules;

/// <summary>
/// Handles the Dark Knight damage rotation (scheduler-driven).
/// </summary>
public sealed class DamageModule : INyxModule
{
    public int Priority => 30;
    public string Name => "Damage";

    public bool TryExecute(INyxContext context, bool isMoving) => false;
    public void UpdateDebugState(INyxContext context) { }

    public void CollectCandidates(INyxContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.Configuration.Tank.EnableDamage) { context.Debug.DamageState = "Disabled"; return; }
        if (!context.InCombat) { context.Debug.DamageState = "Not in combat"; return; }
        if (context.TargetingService.IsDamageTargetingPaused()) { context.Debug.DamageState = "Paused (no target)"; return; }

        var player = context.Player;
        var target = context.TargetingService.FindEnemyForAction(
            context.Configuration.Targeting.EnemyStrategy, DRKActions.HardSlash.ActionId, player);
        var engageTarget = target ?? context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy, 20f, player);

        if (engageTarget == null) { context.Debug.DamageState = "No target"; return; }

        // Out-of-melee
        if (target == null)
        {
            TryPushShadowstrideGapClose(context, scheduler, engageTarget.GameObjectId, engageTarget);
            TryPushUnmend(context, scheduler, engageTarget.GameObjectId);
            return;
        }

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);

        // oGCDs
        TryPushDarkArtsProc(context, scheduler, target.GameObjectId, enemyCount);
        TryPushShadowbringer(context, scheduler, target.GameObjectId);
        TryPushSaltedEarth(context, scheduler, player.GameObjectId);
        TryPushSaltAndDarkness(context, scheduler, player.GameObjectId);
        TryPushDarksideMaintenance(context, scheduler, target.GameObjectId, enemyCount);
        TryPushCarveAndSpit(context, scheduler, target.GameObjectId);
        TryPushAbyssalDrain(context, scheduler, target.GameObjectId, enemyCount);
        TryPushShadowstrideWeave(context, scheduler, target.GameObjectId, target);

        // GCDs
        TryPushDeliriumCombo(context, scheduler, target.GameObjectId, enemyCount);
        TryPushDisesteem(context, scheduler, target.GameObjectId);
        TryPushBloodSpender(context, scheduler, target.GameObjectId, enemyCount);
        TryPushCombo(context, scheduler, target.GameObjectId, enemyCount);
    }

    // --- Out-of-melee ---

    private void TryPushShadowstrideGapClose(INyxContext context, RotationScheduler scheduler, ulong targetId,
                                             Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        if (!context.Configuration.Tank.EnableShadowstride) return;
        var player = context.Player;
        if (player.Level < DRKActions.Shadowstride.MinLevel) return;
        if (!context.ActionService.IsActionReady(DRKActions.Shadowstride.ActionId)) return;
        if (context.TargetingService.GapCloserSafety.ShouldBlockGapCloser(target, player))
        {
            context.Debug.DamageState = $"Shadowstride blocked: {context.TargetingService.GapCloserSafety.LastBlockReason}";
            return;
        }

        scheduler.PushOgcd(NyxAbilities.Shadowstride, targetId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.Shadowstride.Name;
                context.Debug.DamageState = "Shadowstride (gap close)";
            });
    }

    private void TryPushUnmend(INyxContext context, RotationScheduler scheduler, ulong targetId)
    {
        var player = context.Player;
        if (player.Level < DRKActions.Unmend.MinLevel) return;
        if (!context.ActionService.IsActionReady(DRKActions.Unmend.ActionId)) return;

        scheduler.PushGcd(NyxAbilities.Unmend, targetId, priority: 10,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.Unmend.Name;
                context.Debug.DamageState = "Unmend (ranged)";
            });
    }

    // --- oGCDs ---

    private void TryPushDarkArtsProc(INyxContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        if (!context.HasDarkArts) return;
        var level = context.Player.Level;
        var action = context.Configuration.Tank.EnableAoEDamage && enemyCount >= context.Configuration.Tank.AoEMinTargets
            ? DRKActions.GetFloodAction(level) : DRKActions.GetEdgeAction(level);
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        var isAoe = context.Configuration.Tank.EnableAoEDamage && enemyCount >= context.Configuration.Tank.AoEMinTargets;
        var behavior = isAoe ? NyxAbilities.FloodOfShadow : NyxAbilities.EdgeOfShadow;
        scheduler.PushOgcd(behavior, targetId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = "Dark Arts proc!";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name).AsTankBurst()
                    .Reason("Dark Arts proc.", "Free Edge/Flood from TBN shield break.")
                    .Factors("Dark Arts active")
                    .Alternatives("Delay (expires)")
                    .Tip("Never let Dark Arts expire.")
                    .Concept(DrkConcepts.DarkArts).Record();
                context.TrainingService?.RecordConceptApplication(DrkConcepts.DarkArts, true);
            });
    }

    private void TryPushShadowbringer(INyxContext context, RotationScheduler scheduler, ulong targetId)
    {
        if (!context.Configuration.Tank.EnableShadowbringer) return;
        var player = context.Player;
        if (player.Level < DRKActions.Shadowbringer.MinLevel) return;
        if (!context.ActionService.IsActionReady(DRKActions.Shadowbringer.ActionId)) return;

        scheduler.PushOgcd(NyxAbilities.Shadowbringer, targetId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.Shadowbringer.Name;
                context.Debug.DamageState = "Shadowbringer";
            });
    }

    private void TryPushSaltedEarth(INyxContext context, RotationScheduler scheduler, ulong selfId)
    {
        if (!context.Configuration.Tank.EnableSaltedEarth) return;
        var player = context.Player;
        if (player.Level < DRKActions.SaltedEarth.MinLevel) return;
        if (context.HasSaltedEarth) return;
        if (!context.ActionService.IsActionReady(DRKActions.SaltedEarth.ActionId)) return;

        scheduler.PushOgcd(NyxAbilities.SaltedEarth, selfId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.SaltedEarth.Name;
                context.Debug.DamageState = "Salted Earth";
            });
    }

    private void TryPushSaltAndDarkness(INyxContext context, RotationScheduler scheduler, ulong selfId)
    {
        if (!context.Configuration.Tank.EnableSaltedEarth) return;
        var player = context.Player;
        if (player.Level < DRKActions.SaltAndDarkness.MinLevel) return;
        if (!context.HasSaltedEarth) return;
        if (!context.ActionService.IsActionReady(DRKActions.SaltAndDarkness.ActionId)) return;

        scheduler.PushOgcd(NyxAbilities.SaltAndDarkness, selfId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.SaltAndDarkness.Name;
                context.Debug.DamageState = "Salt and Darkness";
            });
    }

    private void TryPushDarksideMaintenance(INyxContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        if (!context.HasEnoughMpForEdge)
        {
            context.Debug.DamageState = "Low MP, can't maintain Darkside";
            return;
        }

        var level = context.Player.Level;
        var isAoe = context.Configuration.Tank.EnableAoEDamage && enemyCount >= context.Configuration.Tank.AoEMinTargets;
        var action = isAoe ? DRKActions.GetFloodAction(level) : DRKActions.GetEdgeAction(level);
        var behavior = isAoe ? NyxAbilities.FloodOfShadow : NyxAbilities.EdgeOfShadow;

        bool expiringSoon = context.HasDarkside && context.DarksideRemaining < 10f && context.DarksideRemaining > 0f;
        bool noDarkside = !context.HasDarkside;
        bool mpDump = context.CurrentMp >= 9400;
        if (!expiringSoon && !noDarkside && !mpDump) return;
        if (!context.ActionService.IsActionReady(action.ActionId)) return;

        var darksideRem = context.DarksideRemaining;
        var mp = context.CurrentMp;
        var reason = expiringSoon ? $"Darkside refresh ({darksideRem:F1}s)"
                   : noDarkside ? "Darkside activate"
                   : "MP dump";

        scheduler.PushOgcd(behavior, targetId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = reason;
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name).AsTankResource(mp)
                    .Reason(reason, "Edge/Flood extends Darkside and spends 3000 MP.")
                    .Factors($"Darkside: {darksideRem:F1}s", $"MP: {mp}")
                    .Alternatives("Let Darkside fall")
                    .Tip("Keep Darkside active always.")
                    .Concept(noDarkside ? DrkConcepts.Darkside : DrkConcepts.DarksideMaintenance).Record();
                context.TrainingService?.RecordConceptApplication(DrkConcepts.DarksideMaintenance, true);
            });
    }

    private void TryPushCarveAndSpit(INyxContext context, RotationScheduler scheduler, ulong targetId)
    {
        if (!context.Configuration.Tank.EnableCarveAndSpit) return;
        var player = context.Player;
        if (player.Level < DRKActions.CarveAndSpit.MinLevel) return;
        if (!context.ActionService.IsActionReady(DRKActions.CarveAndSpit.ActionId)) return;

        scheduler.PushOgcd(NyxAbilities.CarveAndSpit, targetId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.CarveAndSpit.Name;
                context.Debug.DamageState = "Carve and Spit";
            });
    }

    private void TryPushAbyssalDrain(INyxContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        if (!context.Configuration.Tank.EnableAbyssalDrain) return;
        var player = context.Player;
        if (player.Level < DRKActions.AbyssalDrain.MinLevel) return;
        if (enemyCount < 2) return;
        if (!context.ActionService.IsActionReady(DRKActions.AbyssalDrain.ActionId)) return;

        scheduler.PushOgcd(NyxAbilities.AbyssalDrain, targetId, priority: 6,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.AbyssalDrain.Name;
                context.Debug.DamageState = $"Abyssal Drain ({enemyCount} enemies)";
            });
    }

    private void TryPushShadowstrideWeave(INyxContext context, RotationScheduler scheduler, ulong targetId,
                                          Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
    {
        if (!context.Configuration.Tank.EnableShadowstride) return;
        var player = context.Player;
        if (player.Level < DRKActions.Shadowstride.MinLevel) return;
        if (!context.ActionService.IsActionReady(DRKActions.Shadowstride.ActionId)) return;
        if (context.TargetingService.GapCloserSafety.ShouldBlockGapCloser(target, player)) return;

        scheduler.PushOgcd(NyxAbilities.Shadowstride, targetId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.Shadowstride.Name;
                context.Debug.DamageState = "Shadowstride (weave)";
            });
    }

    // --- GCDs ---

    private void TryPushDeliriumCombo(INyxContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        var level = context.Player.Level;
        if (level < DRKActions.ScarletDelirium.MinLevel) return;
        if (!context.HasDelirium) return;

        // AoE: use Impalement (step 1 replacement for AoE)
        bool useAoE = context.Configuration.Tank.EnableAoEDamage &&
                      enemyCount >= context.Configuration.Tank.AoEMinTargets &&
                      level >= DRKActions.Impalement.MinLevel;

        if (useAoE && context.ActionService.IsActionReady(DRKActions.Impalement.ActionId))
        {
            scheduler.PushGcd(NyxAbilities.Impalement, targetId, priority: 2,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = DRKActions.Impalement.Name;
                    context.Debug.DamageState = $"Impalement ({enemyCount} enemies)";
                });
            return;
        }

        if (!context.ActionService.IsActionReady(DRKActions.ScarletDelirium.ActionId)) return;

        scheduler.PushGcd(NyxAbilities.ScarletDelirium, targetId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.ScarletDelirium.Name;
                context.Debug.DamageState = "Scarlet Delirium";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DRKActions.ScarletDelirium.ActionId, "Delirium Combo").AsTankBurst()
                    .Reason("Delirium combo during burst window.", "Scarlet Delirium → Comeuppance → Torcleaver.")
                    .Factors("Delirium active")
                    .Alternatives("Break combo (wastes burst)")
                    .Tip("Complete Delirium combo in order.")
                    .Concept(DrkConcepts.Delirium).Record();
                context.TrainingService?.RecordConceptApplication(DrkConcepts.Delirium, true);
            });
    }

    private void TryPushDisesteem(INyxContext context, RotationScheduler scheduler, ulong targetId)
    {
        var level = context.Player.Level;
        if (level < DRKActions.Disesteem.MinLevel) return;
        if (!context.HasScornfulEdge) return;
        if (!context.ActionService.IsActionReady(DRKActions.Disesteem.ActionId)) return;

        scheduler.PushGcd(NyxAbilities.Disesteem, targetId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.Disesteem.Name;
                context.Debug.DamageState = "Disesteem";
            });
    }

    private void TryPushBloodSpender(INyxContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        var level = context.Player.Level;

        // Lv.96+ prefers Scarlet Delirium combo during Delirium — skip Blood spender there
        if (context.HasDelirium && level >= DRKActions.ScarletDelirium.MinLevel) return;

        // Pre-Lv.96 Delirium grants free Bloodspiller
        bool free = context.HasDelirium && level >= DRKActions.Bloodspiller.MinLevel && level < DRKActions.ScarletDelirium.MinLevel;
        bool atCap = context.BloodGauge >= context.Configuration.Tank.BloodGaugeCap;
        if (!free && context.BloodGauge < 50 && !atCap) return;

        // AoE: Quietus
        bool useAoE = context.Configuration.Tank.EnableAoEDamage &&
                      enemyCount >= context.Configuration.Tank.AoEMinTargets &&
                      level >= DRKActions.Quietus.MinLevel;

        if (useAoE && context.ActionService.IsActionReady(DRKActions.Quietus.ActionId))
        {
            var gauge = context.BloodGauge;
            scheduler.PushGcd(NyxAbilities.Quietus, targetId, priority: 3,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = DRKActions.Quietus.Name;
                    context.Debug.DamageState = $"Quietus ({enemyCount}, {gauge} Blood)";
                });
            return;
        }

        if (level < DRKActions.Bloodspiller.MinLevel) return;
        if (!context.ActionService.IsActionReady(DRKActions.Bloodspiller.ActionId)) return;

        var g = context.BloodGauge;
        var isFree = free;
        scheduler.PushGcd(NyxAbilities.Bloodspiller, targetId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.Bloodspiller.Name;
                context.Debug.DamageState = isFree ? $"Bloodspiller (free, Delirium)" : $"Bloodspiller ({g} Blood)";
            });
    }

    private void TryPushCombo(INyxContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        var level = context.Player.Level;
        bool useAoE = context.Configuration.Tank.EnableAoEDamage &&
                      enemyCount >= context.Configuration.Tank.AoEMinTargets &&
                      level >= DRKActions.Unleash.MinLevel;

        if (useAoE)
        {
            TryPushAoECombo(context, scheduler, targetId, enemyCount);
        }
        else
        {
            TryPushSingleTargetCombo(context, scheduler, targetId);
        }
    }

    private void TryPushSingleTargetCombo(INyxContext context, RotationScheduler scheduler, ulong targetId)
    {
        var level = context.Player.Level;

        // Step 2: Souleater
        if (context.ComboStep == 2 && level >= DRKActions.Souleater.MinLevel)
        {
            if (!context.ActionService.IsActionReady(DRKActions.Souleater.ActionId)) return;
            scheduler.PushGcd(NyxAbilities.Souleater, targetId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = DRKActions.Souleater.Name;
                    context.Debug.DamageState = "Souleater (combo)";
                });
            return;
        }

        // Step 1: Syphon Strike — requires LastComboAction == HardSlash (ambiguous with AoE step 1 from Unleash)
        if (context.ComboStep == 1 &&
            context.LastComboAction == DRKActions.HardSlash.ActionId &&
            level >= DRKActions.SyphonStrike.MinLevel)
        {
            if (!context.ActionService.IsActionReady(DRKActions.SyphonStrike.ActionId)) return;
            scheduler.PushGcd(NyxAbilities.SyphonStrike, targetId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = DRKActions.SyphonStrike.Name;
                    context.Debug.DamageState = "Syphon Strike (combo)";
                });
            return;
        }

        // Starter: Hard Slash
        if (!context.ActionService.IsActionReady(DRKActions.HardSlash.ActionId)) return;
        scheduler.PushGcd(NyxAbilities.HardSlash, targetId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.HardSlash.Name;
                context.Debug.DamageState = "Hard Slash (start)";
            });
    }

    private void TryPushAoECombo(INyxContext context, RotationScheduler scheduler, ulong targetId, int enemyCount)
    {
        var level = context.Player.Level;

        // Step 1: Stalwart Soul — requires LastComboAction == Unleash (ambiguous with ST step 1 from Hard Slash)
        if (context.ComboStep == 1 &&
            context.LastComboAction == DRKActions.Unleash.ActionId &&
            level >= DRKActions.StalwartSoul.MinLevel)
        {
            if (!context.ActionService.IsActionReady(DRKActions.StalwartSoul.ActionId)) return;
            scheduler.PushGcd(NyxAbilities.StalwartSoul, targetId, priority: 7,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = DRKActions.StalwartSoul.Name;
                    context.Debug.DamageState = $"Stalwart Soul ({enemyCount} enemies)";
                });
            return;
        }

        // Starter: Unleash
        if (!context.ActionService.IsActionReady(DRKActions.Unleash.ActionId)) return;
        scheduler.PushGcd(NyxAbilities.Unleash, targetId, priority: 7,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.Unleash.Name;
                context.Debug.DamageState = $"Unleash ({enemyCount} enemies)";
            });
    }
}
