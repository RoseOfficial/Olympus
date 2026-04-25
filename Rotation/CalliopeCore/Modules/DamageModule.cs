using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.CalliopeCore.Abilities;
using Olympus.Rotation.CalliopeCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Services.Training;

namespace Olympus.Rotation.CalliopeCore.Modules;

/// <summary>
/// Handles the Bard damage rotation (scheduler-driven).
/// Manages procs, DoTs, Apex Arrow, filler GCDs, and Head Graze interrupt.
/// </summary>
public sealed class DamageModule : ICalliopeModule
{
    public int Priority => 30;
    public string Name => "Damage";

    private readonly IBurstWindowService? _burstWindowService;
    private readonly ISmartAoEService? _smartAoEService;

    public DamageModule(IBurstWindowService? burstWindowService = null, ISmartAoEService? smartAoEService = null)
    {
        _burstWindowService = burstWindowService;
        _smartAoEService = smartAoEService;
    }

    private bool IsInBurst => BurstHoldHelper.IsInBurst(_burstWindowService);

    public bool TryExecute(ICalliopeContext context, bool isMoving) => false;

    public void UpdateDebugState(ICalliopeContext context) { }

    public void CollectCandidates(ICalliopeContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.DamageState = "Not in combat";
            return;
        }
        if (context.TargetingService.IsDamageTargetingPaused())
        {
            context.Debug.DamageState = "Paused (no target)";
            return;
        }
        if (context.Configuration.Targeting.SuppressDamageOnForcedMovement
            && PlayerSafetyHelper.IsForcedMovementActive(context.Player))
        {
            context.Debug.DamageState = "Paused (forced movement)";
            return;
        }

        var player = context.Player;
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.RangedTargetingRange,
            player);
        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return;
        }

        var aoeEnabled = context.Configuration.Bard.EnableAoERotation;
        var aoeThreshold = context.Configuration.Bard.AoEMinTargets;
        var rawEnemyCount = context.TargetingService.CountEnemiesInRange(12f, player);
        context.Debug.NearbyEnemies = rawEnemyCount;
        var enemyCount = aoeEnabled ? rawEnemyCount : 0;

        // oGCD interrupt
        TryPushInterrupt(context, scheduler, target);

        // GCD priority order
        TryPushResonantArrow(context, scheduler, target);
        TryPushRadiantEncore(context, scheduler, target);
        TryPushBlastArrow(context, scheduler, target);
        TryPushBarragedRefulgent(context, scheduler, target);
        TryPushRefulgentArrow(context, scheduler, target, enemyCount);
        TryPushApexArrow(context, scheduler, target);
        TryPushIronJaws(context, scheduler, target);
        TryPushApplyDots(context, scheduler, target);
        TryPushSpreadDots(context, scheduler, target);
        TryPushFiller(context, scheduler, target, enemyCount);
    }

    #region GCDs

    private void TryPushResonantArrow(ICalliopeContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Bard.EnableResonantArrow) return;
        var player = context.Player;
        if (player.Level < BRDActions.ResonantArrow.MinLevel) return;
        if (!context.HasResonantArrowReady) return;
        if (!context.ActionService.IsActionReady(BRDActions.ResonantArrow.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : BRDActions.ResonantArrow.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(CalliopeAbilities.ResonantArrow, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = BRDActions.ResonantArrow.Name;
                context.Debug.DamageState = "Resonant Arrow (Barrage follow-up)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BRDActions.ResonantArrow.ActionId, BRDActions.ResonantArrow.Name)
                    .AsProc("Resonant Arrow Ready")
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Resonant Arrow (Barrage follow-up)",
                        "Resonant Arrow is granted after using Barraged Refulgent Arrow. High potency proc that must be used " +
                        "before it expires. Always part of the Barrage burst sequence.")
                    .Factors("Resonant Arrow Ready active", "Barrage sequence")
                    .Alternatives("No alternatives - must use before expiring")
                    .Tip("After Barrage → Refulgent Arrow, always follow with Resonant Arrow immediately.")
                    .Concept(BrdConcepts.ResonantArrow)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BrdConcepts.ResonantArrow, true, "Proc consumption");
                context.TrainingService?.RecordConceptApplication(BrdConcepts.Barrage, true, "Barrage sequence completion");
            });
    }

    private void TryPushRadiantEncore(ICalliopeContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Bard.EnableRadiantEncore) return;
        var player = context.Player;
        if (player.Level < BRDActions.RadiantEncore.MinLevel) return;
        if (!context.HasRadiantEncoreReady) return;
        if (!context.ActionService.IsActionReady(BRDActions.RadiantEncore.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : BRDActions.RadiantEncore.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(CalliopeAbilities.RadiantEncore, target.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = BRDActions.RadiantEncore.Name;
                context.Debug.DamageState = "Radiant Encore (RF follow-up)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BRDActions.RadiantEncore.ActionId, BRDActions.RadiantEncore.Name)
                    .AsProc("Radiant Encore Ready")
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Radiant Encore (Radiant Finale follow-up)",
                        "Radiant Encore is granted after using Radiant Finale. Potency scales with Coda used (same as RF). " +
                        "Must use before it expires. Part of the 2-minute burst sequence.")
                    .Factors("Radiant Encore Ready active", "Radiant Finale sequence")
                    .Alternatives("No alternatives - must use before expiring")
                    .Tip("After Radiant Finale, use Radiant Encore during the burst window for extra damage.")
                    .Concept(BrdConcepts.RadiantEncore)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BrdConcepts.RadiantEncore, true, "Proc consumption");
            });
    }

    private void TryPushBlastArrow(ICalliopeContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Bard.EnableBlastArrow) return;
        var player = context.Player;
        if (player.Level < BRDActions.BlastArrow.MinLevel) return;
        if (!context.HasBlastArrowReady) return;
        if (!context.ActionService.IsActionReady(BRDActions.BlastArrow.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : BRDActions.BlastArrow.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(CalliopeAbilities.BlastArrow, target.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = BRDActions.BlastArrow.Name;
                context.Debug.DamageState = "Blast Arrow (Apex follow-up)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BRDActions.BlastArrow.ActionId, BRDActions.BlastArrow.Name)
                    .AsProc("Blast Arrow Ready")
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Blast Arrow (Apex Arrow follow-up)",
                        "Blast Arrow is granted after using Apex Arrow at 80+ Soul Voice. Very high potency follow-up. " +
                        "Always use immediately after Apex Arrow. Part of the Soul Voice spending sequence.")
                    .Factors("Blast Arrow Ready active", "Apex Arrow sequence")
                    .Alternatives("No alternatives - must use before expiring")
                    .Tip("After Apex Arrow (80+ SV), always follow with Blast Arrow for massive damage.")
                    .Concept(BrdConcepts.BlastArrow)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BrdConcepts.BlastArrow, true, "Proc consumption");
                context.TrainingService?.RecordConceptApplication(BrdConcepts.SoulVoiceGauge, true, "Soul Voice spending");
            });
    }

    private void TryPushBarragedRefulgent(ICalliopeContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Bard.EnableBarrage) return;
        var level = context.Player.Level;
        if (!context.HasBarrage) return;
        if (!context.HasHawksEye) return;

        var action = BRDActions.GetProcAction((byte)level);
        var ability = action == BRDActions.RefulgentArrow ? CalliopeAbilities.RefulgentArrow : CalliopeAbilities.StraightShot;
        if (!context.ActionService.IsActionReady(action.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : action.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(ability, target.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = $"Barrage + {action.Name}";
                context.Debug.DamageState = "Barraged Refulgent Arrow";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, $"Barrage + {action.Name}")
                    .AsProc("Barrage + Hawk's Eye")
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Barraged Refulgent Arrow (triple damage!)",
                        "Barraged Refulgent Arrow hits 3 times for massive burst damage. This is BRD's biggest single hit. " +
                        "Always wait for Hawk's Eye proc after Barrage. Grants Resonant Arrow Ready.")
                    .Factors("Barrage active", "Hawk's Eye (Straight Shot Ready) active", context.HasRagingStrikes ? "RS active" : "")
                    .Alternatives("No alternatives - this is the optimal Barrage usage")
                    .Tip("Barrage + Refulgent Arrow is your biggest burst. Always use during Raging Strikes.")
                    .Concept(BrdConcepts.Barrage)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BrdConcepts.Barrage, true, "Barrage consumption");
                context.TrainingService?.RecordConceptApplication(BrdConcepts.RefulgentArrow, true, "Proc usage with Barrage");
                context.TrainingService?.RecordConceptApplication(BrdConcepts.StraightShotReady, true, "Hawk's Eye consumption");
            });
    }

    private void TryPushRefulgentArrow(ICalliopeContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        if (!context.Configuration.Bard.EnableRefulgentArrow) return;
        var level = context.Player.Level;
        if (!context.HasHawksEye) return;
        var aoeThreshold = context.Configuration.Bard.AoEMinTargets;

        if (enemyCount >= aoeThreshold && level >= BRDActions.Shadowbite.MinLevel
            && context.ActionService.IsActionReady(BRDActions.Shadowbite.ActionId))
        {
            var castTime = context.HasSwiftcast ? 0f : BRDActions.Shadowbite.CastTime;
            if (MechanicCastGate.ShouldBlock(context, castTime))
            {
                context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                return;
            }

            scheduler.PushGcd(CalliopeAbilities.Shadowbite, target.GameObjectId, priority: 4,
                onDispatched: _ =>
                {
                    context.Debug.PlannedAction = BRDActions.Shadowbite.Name;
                    context.Debug.DamageState = "Shadowbite (AoE proc)";
                    TrainingHelper.Decision(context.TrainingService)
                        .Action(BRDActions.Shadowbite.ActionId, BRDActions.Shadowbite.Name)
                        .AsAoE(enemyCount)
                        .Target(target.Name?.TextValue ?? "Target")
                        .Reason($"Shadowbite (AoE proc, {enemyCount} targets)",
                            "Shadowbite is the AoE version of Refulgent Arrow, consuming Hawk's Eye. " +
                            "Use at 3+ targets instead of Refulgent Arrow for better total damage.")
                        .Factors("Hawk's Eye active", $"Enemies: {enemyCount}")
                        .Alternatives("Use Refulgent for single target")
                        .Tip("At 3+ enemies, consume Hawk's Eye procs with Shadowbite instead of Refulgent Arrow.")
                        .Concept(BrdConcepts.StraightShotReady)
                        .Record();
                    context.TrainingService?.RecordConceptApplication(BrdConcepts.StraightShotReady, true, "AoE proc usage");
                });
            return;
        }

        var action = BRDActions.GetProcAction((byte)level);
        var ability = action == BRDActions.RefulgentArrow ? CalliopeAbilities.RefulgentArrow : CalliopeAbilities.StraightShot;
        if (!context.ActionService.IsActionReady(action.ActionId)) return;
        var stCastTime = context.HasSwiftcast ? 0f : action.CastTime;
        if (MechanicCastGate.ShouldBlock(context, stCastTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(ability, target.GameObjectId, priority: 4,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = "Refulgent Arrow (proc)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsProc("Hawk's Eye")
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Refulgent Arrow (Hawk's Eye proc)",
                        "Refulgent Arrow is BRD's proc GCD, replacing Burst Shot when Hawk's Eye (Straight Shot Ready) is active. " +
                        "Higher potency than Burst Shot. Procs randomly from Burst Shot or guaranteed from song mechanics.")
                    .Factors("Hawk's Eye active", "Single target")
                    .Alternatives("Continue using Burst Shot if no proc")
                    .Tip("Always use Refulgent Arrow when Hawk's Eye procs. It's free extra damage.")
                    .Concept(BrdConcepts.RefulgentArrow)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BrdConcepts.RefulgentArrow, true, "Proc consumption");
                context.TrainingService?.RecordConceptApplication(BrdConcepts.StraightShotReady, true, "Hawk's Eye consumption");
            });
    }

    private void TryPushApexArrow(ICalliopeContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Bard.EnableApexArrow) return;
        var player = context.Player;
        if (player.Level < BRDActions.ApexArrow.MinLevel) return;

        var brdCfg = context.Configuration.Bard;
        var normalThreshold = brdCfg.ApexArrowMinGauge;
        var burstThreshold = (brdCfg.EnableBurstPooling && brdCfg.UseApexDuringBurst && IsInBurst) ? 50 : normalThreshold;
        var apexThreshold = IsInBurst ? burstThreshold : normalThreshold;
        bool shouldUse = context.SoulVoice >= 100
                         || (context.SoulVoice >= apexThreshold &&
                             (IsInBurst || context.HasRagingStrikes
                              || !context.ActionService.IsActionReady(BRDActions.RagingStrikes.ActionId)));
        if (!shouldUse)
        {
            context.Debug.DamageState = $"Apex Arrow: {context.SoulVoice}/{normalThreshold}";
            return;
        }
        if (!context.ActionService.IsActionReady(BRDActions.ApexArrow.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : BRDActions.ApexArrow.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(CalliopeAbilities.ApexArrow, target.GameObjectId, priority: 5,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = BRDActions.ApexArrow.Name;
                context.Debug.DamageState = $"Apex Arrow ({context.SoulVoice} SV)";

                var apexReason = context.SoulVoice >= 100 ? "Preventing overcap"
                              : context.HasRagingStrikes ? "Burst window" : "80+ Soul Voice";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(BRDActions.ApexArrow.ActionId, BRDActions.ApexArrow.Name)
                    .AsRangedResource("Soul Voice", context.SoulVoice)
                    .Reason($"Apex Arrow ({context.SoulVoice} SV, {apexReason})",
                        "Apex Arrow spends Soul Voice gauge. Use at 80+ during burst for Blast Arrow follow-up (highest potency). " +
                        "Use at 100 to prevent overcapping. Soul Voice builds from song Repertoire procs.")
                    .Factors($"Soul Voice: {context.SoulVoice}/100", context.HasRagingStrikes ? "RS active" : "No burst buffs", "Grants Blast Arrow Ready at 80+")
                    .Alternatives("Wait for burst window", "Wait for 100 SV to avoid overcap")
                    .Tip("Use Apex Arrow at 80+ during burst windows, or at 100 to prevent overcapping. Always follow with Blast Arrow.")
                    .Concept(BrdConcepts.ApexArrow)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BrdConcepts.ApexArrow, true, "Soul Voice spending");
                context.TrainingService?.RecordConceptApplication(BrdConcepts.SoulVoiceGauge, true, "Gauge management");
                context.TrainingService?.RecordConceptApplication(BrdConcepts.SoulVoiceOvercapping, context.SoulVoice >= 100, "Overcap prevention");
            });
    }

    private void TryPushIronJaws(ICalliopeContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.Bard.EnableIronJaws) return;
        var player = context.Player;
        if (player.Level < BRDActions.IronJaws.MinLevel) return;
        if (!context.HasCausticBite || !context.HasStormbite) return;
        if (target.MaxHp > 0 && (float)target.CurrentHp / target.MaxHp < context.Configuration.Bard.DotMinTargetHp) return;

        var dotRefreshThreshold = context.Configuration.Bard.DotRefreshThreshold;
        const float dotRefreshMin = 3f;
        bool needsRefresh = context.CausticBiteRemaining <= dotRefreshThreshold || context.StormbiteRemaining <= dotRefreshThreshold;
        bool snapshotBuffs = context.HasRagingStrikes && context.CausticBiteRemaining < 20f;
        if (!needsRefresh && !snapshotBuffs) return;
        if (context.CausticBiteRemaining < dotRefreshMin || context.StormbiteRemaining < dotRefreshMin) needsRefresh = true;
        if (!needsRefresh && !snapshotBuffs) return;
        if (!context.ActionService.IsActionReady(BRDActions.IronJaws.ActionId)) return;
        var castTime = context.HasSwiftcast ? 0f : BRDActions.IronJaws.CastTime;
        if (MechanicCastGate.ShouldBlock(context, castTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(CalliopeAbilities.IronJaws, target.GameObjectId, priority: 6,
            onDispatched: _ =>
            {
                var minDotRemaining = System.Math.Min(context.CausticBiteRemaining, context.StormbiteRemaining);
                var ironJawsReason = snapshotBuffs ? "snapshot buffs" : "refresh";
                context.Debug.PlannedAction = BRDActions.IronJaws.Name;
                context.Debug.DamageState = $"Iron Jaws ({ironJawsReason})";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(BRDActions.IronJaws.ActionId, BRDActions.IronJaws.Name)
                    .AsDot(minDotRemaining)
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"Iron Jaws ({ironJawsReason}, DoTs: {minDotRemaining:F1}s)",
                        snapshotBuffs
                            ? "Iron Jaws refreshes both DoTs and snapshots current buffs. Use during Raging Strikes to extend buffed DoTs. " +
                              "This is a DPS gain even if DoTs have significant time remaining."
                            : "Iron Jaws refreshes both Caustic Bite and Stormbite with a single GCD. " +
                              "Refresh between 3-7s remaining to avoid letting DoTs fall off or clipping too early.")
                    .Factors($"Caustic Bite: {context.CausticBiteRemaining:F1}s", $"Stormbite: {context.StormbiteRemaining:F1}s", snapshotBuffs ? "RS active - snapshotting" : "Normal refresh")
                    .Alternatives("Wait for DoTs to drop lower", "Apply DoTs manually if missing")
                    .Tip(snapshotBuffs
                        ? "Snapshot Raging Strikes with Iron Jaws for 20s of buffed DoT damage."
                        : "Refresh DoTs with Iron Jaws between 3-7s remaining.")
                    .Concept(BrdConcepts.IronJaws)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BrdConcepts.IronJaws, true, snapshotBuffs ? "Buff snapshot" : "DoT refresh");
            });
    }

    private void TryPushApplyDots(ICalliopeContext context, RotationScheduler scheduler, IBattleChara target)
    {
        var level = context.Player.Level;
        if (target.MaxHp > 0 && (float)target.CurrentHp / target.MaxHp < context.Configuration.Bard.DotMinTargetHp) return;

        // Stormbite first
        if (!context.HasStormbite && context.Configuration.Bard.EnableStormbite && level >= BRDActions.Windbite.MinLevel)
        {
            var action = BRDActions.GetStormbite((byte)level);
            var ability = action == BRDActions.Stormbite ? CalliopeAbilities.Stormbite : CalliopeAbilities.Windbite;
            if (context.ActionService.IsActionReady(action.ActionId))
            {
                var castTime = context.HasSwiftcast ? 0f : action.CastTime;
                if (MechanicCastGate.ShouldBlock(context, castTime))
                {
                    context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                    return;
                }

                scheduler.PushGcd(ability, target.GameObjectId, priority: 7,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = action.Name;
                        context.Debug.DamageState = $"{action.Name} applied";
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(action.ActionId, action.Name)
                            .AsDot(0f)
                            .Target(target.Name?.TextValue ?? "Target")
                            .Reason($"{action.Name} applied (higher potency DoT)",
                                "Stormbite (Windbite upgrade) is BRD's higher potency DoT. Apply first when DoTs are missing. " +
                                "Both DoTs snapshot buffs when applied. Maintain 100% uptime on both DoTs.")
                            .Factors("DoT not on target", "Applied before Caustic Bite")
                            .Alternatives("Use Iron Jaws if both DoTs present")
                            .Tip("Apply Stormbite first, then Caustic Bite. Maintain both DoTs at all times.")
                            .Concept(BrdConcepts.Stormbite)
                            .Record();
                        context.TrainingService?.RecordConceptApplication(BrdConcepts.Stormbite, true, "DoT application");
                    });
                return;
            }
        }

        // Caustic Bite
        if (!context.HasCausticBite && context.Configuration.Bard.EnableCausticBite && level >= BRDActions.VenomousBite.MinLevel)
        {
            var action = BRDActions.GetCausticBite((byte)level);
            var ability = action == BRDActions.CausticBite ? CalliopeAbilities.CausticBite : CalliopeAbilities.VenomousBite;
            if (context.ActionService.IsActionReady(action.ActionId))
            {
                var castTime = context.HasSwiftcast ? 0f : action.CastTime;
                if (MechanicCastGate.ShouldBlock(context, castTime))
                {
                    context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                    return;
                }

                scheduler.PushGcd(ability, target.GameObjectId, priority: 7,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = action.Name;
                        context.Debug.DamageState = $"{action.Name} applied";
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(action.ActionId, action.Name)
                            .AsDot(0f)
                            .Target(target.Name?.TextValue ?? "Target")
                            .Reason($"{action.Name} applied (poison DoT)",
                                "Caustic Bite (Venomous Bite upgrade) is BRD's poison DoT. Apply second after Stormbite. " +
                                "Both DoTs snapshot buffs when applied. Maintain 100% uptime on both DoTs.")
                            .Factors("DoT not on target", "Applied after Stormbite")
                            .Alternatives("Use Iron Jaws if both DoTs present")
                            .Tip("Apply Caustic Bite after Stormbite. Keep both DoTs up at all times.")
                            .Concept(BrdConcepts.CausticBite)
                            .Record();
                        context.TrainingService?.RecordConceptApplication(BrdConcepts.CausticBite, true, "DoT application");
                    });
            }
        }
    }

    private void TryPushSpreadDots(ICalliopeContext context, RotationScheduler scheduler, IBattleChara primaryTarget)
    {
        var brdCfg = context.Configuration.Bard;
        if (!brdCfg.SpreadDots) return;
        var level = context.Player.Level;

        // Try to spread Stormbite to a secondary target
        if (brdCfg.EnableStormbite && level >= BRDActions.Windbite.MinLevel)
        {
            var stormbiteStatusId = BRDActions.GetStormbiteStatusId((byte)level);
            var spreadTarget = context.TargetingService.FindEnemyNeedingDot(
                stormbiteStatusId, 0f, FFXIVConstants.RangedTargetingRange, context.Player);
            if (spreadTarget != null && spreadTarget.GameObjectId != primaryTarget.GameObjectId
                && spreadTarget.MaxHp > 0
                && (float)spreadTarget.CurrentHp / spreadTarget.MaxHp >= brdCfg.DotMinTargetHp)
            {
                var action = BRDActions.GetStormbite((byte)level);
                var ability = action == BRDActions.Stormbite ? CalliopeAbilities.Stormbite : CalliopeAbilities.Windbite;
                if (context.ActionService.IsActionReady(action.ActionId))
                {
                    var castTime = context.HasSwiftcast ? 0f : action.CastTime;
                    if (!MechanicCastGate.ShouldBlock(context, castTime))
                    {
                        scheduler.PushGcd(ability, spreadTarget.GameObjectId, priority: 7,
                            onDispatched: _ =>
                            {
                                context.Debug.PlannedAction = action.Name;
                                context.Debug.DamageState = $"{action.Name} spread";
                            });
                        return;
                    }
                }
            }
        }

        // Try to spread Caustic Bite to a secondary target
        if (brdCfg.EnableCausticBite && level >= BRDActions.VenomousBite.MinLevel)
        {
            var causticStatusId = BRDActions.GetCausticBiteStatusId((byte)level);
            var spreadTarget = context.TargetingService.FindEnemyNeedingDot(
                causticStatusId, 0f, FFXIVConstants.RangedTargetingRange, context.Player);
            if (spreadTarget != null && spreadTarget.GameObjectId != primaryTarget.GameObjectId
                && spreadTarget.MaxHp > 0
                && (float)spreadTarget.CurrentHp / spreadTarget.MaxHp >= brdCfg.DotMinTargetHp)
            {
                var action = BRDActions.GetCausticBite((byte)level);
                var ability = action == BRDActions.CausticBite ? CalliopeAbilities.CausticBite : CalliopeAbilities.VenomousBite;
                if (context.ActionService.IsActionReady(action.ActionId))
                {
                    var castTime = context.HasSwiftcast ? 0f : action.CastTime;
                    if (!MechanicCastGate.ShouldBlock(context, castTime))
                    {
                        scheduler.PushGcd(ability, spreadTarget.GameObjectId, priority: 7,
                            onDispatched: _ =>
                            {
                                context.Debug.PlannedAction = action.Name;
                                context.Debug.DamageState = $"{action.Name} spread";
                            });
                    }
                }
            }
        }
    }

    private void TryPushFiller(ICalliopeContext context, RotationScheduler scheduler, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var aoeThreshold = context.Configuration.Bard.AoEMinTargets;

        if (enemyCount >= aoeThreshold && level >= BRDActions.QuickNock.MinLevel)
        {
            var aoeAction = BRDActions.GetAoeFiller((byte)level);
            var aoeAbility = aoeAction == BRDActions.Ladonsbite ? CalliopeAbilities.Ladonsbite : CalliopeAbilities.QuickNock;
            if (context.ActionService.IsActionReady(aoeAction.ActionId))
            {
                var castTime = context.HasSwiftcast ? 0f : aoeAction.CastTime;
                if (MechanicCastGate.ShouldBlock(context, castTime))
                {
                    context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
                    return;
                }

                scheduler.PushGcd(aoeAbility, player.GameObjectId, priority: 8,
                    onDispatched: _ =>
                    {
                        context.Debug.PlannedAction = aoeAction.Name;
                        context.Debug.DamageState = $"{aoeAction.Name} (AoE filler)";
                        TrainingHelper.Decision(context.TrainingService)
                            .Action(aoeAction.ActionId, aoeAction.Name)
                            .AsAoE(enemyCount)
                            .Reason($"{aoeAction.Name} (AoE filler, {enemyCount} targets)",
                                "Ladonsbite (Quick Nock upgrade) is BRD's AoE filler GCD. Use at 3+ enemies. " +
                                "Can proc Hawk's Eye for Shadowbite. Use Rain of Death for oGCDs in AoE.")
                            .Factors($"Enemies: {enemyCount}", "No higher priority actions")
                            .Alternatives("Use Burst Shot for single target")
                            .Tip("At 3+ enemies, spam Ladonsbite as your filler GCD instead of Burst Shot.")
                            .Concept(BrdConcepts.StraightShotReady)
                            .Record();
                        context.TrainingService?.RecordConceptApplication(BrdConcepts.StraightShotReady, false, "AoE filler usage");
                    });
                return;
            }
        }

        var action = BRDActions.GetFiller((byte)level);
        var ability = action == BRDActions.BurstShot ? CalliopeAbilities.BurstShot : CalliopeAbilities.HeavyShot;
        if (!context.ActionService.IsActionReady(action.ActionId)) return;
        var stCastTime = context.HasSwiftcast ? 0f : action.CastTime;
        if (MechanicCastGate.ShouldBlock(context, stCastTime))
        {
            context.Debug.DamageState = MechanicCastGate.FormatBlockedState(context);
            return;
        }

        scheduler.PushGcd(ability, target.GameObjectId, priority: 9,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = action.Name;
                context.Debug.DamageState = $"{action.Name} (filler)";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(action.ActionId, action.Name)
                    .AsRangedDamage()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason($"{action.Name} (single target filler)",
                        "Burst Shot (Heavy Shot upgrade) is BRD's single target filler GCD. Use when no procs are active " +
                        "and no higher priority actions are available. Can proc Hawk's Eye for Refulgent Arrow.")
                    .Factors("No procs active", "DoTs maintained", "No higher priority actions")
                    .Alternatives("Use Refulgent Arrow if Hawk's Eye procs")
                    .Tip("Burst Shot is your filler. It can proc Hawk's Eye, enabling Refulgent Arrow.")
                    .Concept(BrdConcepts.StraightShotReady)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BrdConcepts.StraightShotReady, false, "Filler usage");
            });
    }

    #endregion

    #region Interrupt

    private void TryPushInterrupt(ICalliopeContext context, RotationScheduler scheduler, IBattleChara target)
    {
        if (!context.Configuration.RangedShared.EnableHeadGraze) return;
        var player = context.Player;
        if (player.Level < RoleActions.HeadGraze.MinLevel) return;
        if (!target.IsCasting) return;
        if (!target.IsCastInterruptible) return;

        var targetId = target.EntityId;

        var delaySeed = (int)(target.EntityId * 2654435761u ^ (uint)(target.TotalCastTime * 1000f));
        var interruptDelay = 0.3f + ((delaySeed & 0xFFFF) / 65535f) * 0.4f;
        if (target.CurrentCastTime < interruptDelay) return;

        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;

        if (coordConfig.EnableInterruptCoordination
            && partyCoord?.IsInterruptTargetReservedByOther(targetId) == true)
        {
            context.Debug.DamageState = "Interrupt reserved by other";
            return;
        }

        if (!context.ActionService.IsActionReady(RoleActions.HeadGraze.ActionId)) return;

        var remainingCastTime = (target.TotalCastTime - target.CurrentCastTime) * 1000f;
        var castTimeMs = (int)remainingCastTime;

        if (coordConfig.EnableInterruptCoordination)
        {
            if (!partyCoord?.ReserveInterruptTarget(targetId, RoleActions.HeadGraze.ActionId, castTimeMs) ?? false)
            {
                context.Debug.DamageState = "Failed to reserve interrupt";
                return;
            }
        }

        scheduler.PushOgcd(CalliopeAbilities.HeadGraze, target.GameObjectId, priority: 0,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = RoleActions.HeadGraze.Name;
                context.Debug.DamageState = "Interrupted cast";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(RoleActions.HeadGraze.ActionId, RoleActions.HeadGraze.Name)
                    .AsInterrupt()
                    .Target(target.Name?.TextValue ?? "Target")
                    .Reason("Head Graze (interrupt)",
                        "Head Graze interrupts enemy casts. Use on interruptible abilities (indicated by flashing cast bar). " +
                        "Coordinate with party to avoid wasting multiple interrupts on the same cast.")
                    .Factors("Target casting interruptible ability", "30s cooldown ready")
                    .Alternatives("Let party member interrupt")
                    .Tip("Watch for interruptible casts. Some mechanics require interrupts to avoid party damage.")
                    .Concept(BrdConcepts.PartyUtility)
                    .Record();
                context.TrainingService?.RecordConceptApplication(BrdConcepts.PartyUtility, true, "Interrupt execution");
            });
    }

    #endregion
}
