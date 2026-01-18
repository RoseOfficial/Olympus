using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Rotation.IrisCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Cache;
using Olympus.Services.Debuff;
using Olympus.Services.Prediction;
using Olympus.Services.Resource;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Timeline;
using static Olympus.Data.PCTActions;

namespace Olympus.Rotation.IrisCore.Context;

/// <summary>
/// Pictomancer-specific context implementation.
/// Provides all state needed for Iris rotation modules.
/// </summary>
public sealed class IrisContext : IIrisContext
{
    #region IRotationContext Implementation

    public IPlayerCharacter Player { get; }
    public bool InCombat { get; }
    public bool IsMoving { get; }
    public bool CanExecuteGcd { get; }
    public bool CanExecuteOgcd { get; }

    public IActionService ActionService { get; }
    public ActionTracker ActionTracker { get; }
    public ICombatEventService CombatEventService { get; }
    public IDamageIntakeService DamageIntakeService { get; }
    public IDamageTrendService DamageTrendService { get; }
    public IFrameScopedCache FrameCache { get; }
    public Configuration Configuration { get; }
    public IDebuffDetectionService DebuffDetectionService { get; }
    public IHpPredictionService HpPredictionService { get; }
    public IMpForecastService MpForecastService { get; }
    public IPlayerStatsService PlayerStatsService { get; }
    public ITargetingService TargetingService { get; }
    public ITimelineService? TimelineService { get; }

    public IObjectTable ObjectTable { get; }
    public IPartyList PartyList { get; }
    public IPluginLog? Log { get; }

    public (float avgHpPercent, float lowestHpPercent, int injuredCount) PartyHealthMetrics { get; }

    #endregion

    #region ICasterDpsRotationContext Implementation

    public int CurrentMp { get; }
    public int MaxMp { get; }
    public float MpPercent { get; }

    public bool IsCasting { get; }
    public float CastRemaining { get; }
    public bool CanSlidecast { get; }

    public bool HasSwiftcast { get; }
    public bool HasTriplecast { get; } // PCT doesn't have Triplecast
    public int TriplecastStacks { get; }
    public bool HasInstantCast { get; }

    #endregion

    #region IIrisContext Implementation

    // Palette Gauge
    public int PaletteGauge { get; }
    public bool CanUseSubtractivePalette { get; }

    // Paint stacks
    public int WhitePaint { get; }
    public bool HasWhitePaint { get; }
    public bool HasBlackPaint { get; }

    // Canvas state
    public bool HasCreatureCanvas { get; }
    public CreatureMotifType CreatureMotifType { get; }
    public bool HasWeaponCanvas { get; }
    public bool HasLandscapeCanvas { get; }

    // Muse state
    public int LivingMuseCharges { get; }
    public bool LivingMuseReady { get; }
    public bool StrikingMuseReady { get; }
    public bool StarryMuseReady { get; }

    // Portrait state
    public bool MogReady { get; }
    public bool MadeenReady { get; }

    // Hammer combo state
    public bool IsInHammerCombo { get; }
    public int HammerComboStep { get; }
    public int HammerTimeStacks { get; }

    // Base combo state
    public int BaseComboStep { get; }
    public bool IsInSubtractiveCombo { get; }

    // Buff state
    public bool HasStarryMuse { get; }
    public float StarryMuseRemaining { get; }
    public bool HasHyperphantasia { get; }
    public int HyperphantasiaStacks { get; }
    public bool HasInspiration { get; }
    public bool HasSubtractiveSpectrum { get; }
    public bool HasStarstruck { get; }
    public bool HasRainbowBright { get; }
    public bool HasHammerTime { get; }
    public bool HasSubtractivePalette { get; }
    public float SubtractivePaletteRemaining { get; }
    public bool HasMonochromeTones { get; }

    // Cooldown state
    public bool SubtractivePaletteReady { get; }
    public bool TemperaCoatReady { get; }
    public bool TemperaGrassaReady { get; }
    public bool SmudgeReady { get; }
    public bool SwiftcastReady { get; }
    public bool LucidDreamingReady { get; }

    // Helpers
    public IrisStatusHelper StatusHelper { get; }
    public IrisPartyHelper PartyHelper { get; }
    public IrisDebugState Debug { get; }

    // Utility
    public bool ShouldUseAoe { get; }
    public int NearbyEnemyCount { get; }
    public bool IsInBurstWindow { get; }
    public bool CanPaintMotif { get; }
    public bool NeedsCreatureMotif { get; }
    public bool NeedsWeaponMotif { get; }
    public bool NeedsLandscapeMotif { get; }

    #endregion

    private readonly IBattleChara? _currentTarget;

    public IrisContext(
        IPlayerCharacter player,
        bool inCombat,
        bool isMoving,
        bool canExecuteGcd,
        bool canExecuteOgcd,
        IActionService actionService,
        ActionTracker actionTracker,
        ICombatEventService combatEventService,
        IDamageIntakeService damageIntakeService,
        IDamageTrendService damageTrendService,
        IFrameScopedCache frameCache,
        Configuration configuration,
        IDebuffDetectionService debuffDetectionService,
        IHpPredictionService hpPredictionService,
        IMpForecastService mpForecastService,
        IPlayerStatsService playerStatsService,
        ITargetingService targetingService,
        IObjectTable objectTable,
        IPartyList partyList,
        IrisStatusHelper statusHelper,
        IrisPartyHelper partyHelper,
        IrisDebugState debugState,
        int paletteGauge,
        int whitePaint,
        bool hasBlackPaint,
        byte creatureMotif,
        bool hasWeaponCanvas,
        bool hasLandscapeCanvas,
        bool mogReady,
        bool madeenReady,
        uint comboAction,
        float comboTimer,
        ITimelineService? timelineService = null,
        IPluginLog? log = null)
    {
        Player = player;
        InCombat = inCombat;
        IsMoving = isMoving;
        CanExecuteGcd = canExecuteGcd;
        CanExecuteOgcd = canExecuteOgcd;
        ActionService = actionService;
        ActionTracker = actionTracker;
        CombatEventService = combatEventService;
        DamageIntakeService = damageIntakeService;
        DamageTrendService = damageTrendService;
        FrameCache = frameCache;
        Configuration = configuration;
        DebuffDetectionService = debuffDetectionService;
        HpPredictionService = hpPredictionService;
        MpForecastService = mpForecastService;
        PlayerStatsService = playerStatsService;
        TargetingService = targetingService;
        TimelineService = timelineService;
        ObjectTable = objectTable;
        PartyList = partyList;
        Log = log;

        StatusHelper = statusHelper;
        PartyHelper = partyHelper;
        Debug = debugState;

        // MP state
        CurrentMp = (int)player.CurrentMp;
        MaxMp = (int)player.MaxMp;
        MpPercent = MaxMp > 0 ? (float)CurrentMp / MaxMp : 0f;

        // Cast state
        IsCasting = player.IsCasting;
        CastRemaining = player.TotalCastTime - player.CurrentCastTime;
        CanSlidecast = IsCasting && CastRemaining <= 0.5f;

        // PCT doesn't have Triplecast
        HasTriplecast = false;
        TriplecastStacks = 0;

        // Palette Gauge
        PaletteGauge = paletteGauge;
        CanUseSubtractivePalette = paletteGauge >= 50;

        // Paint stacks
        WhitePaint = whitePaint;
        HasWhitePaint = whitePaint > 0;
        HasBlackPaint = hasBlackPaint;

        // Canvas state
        CreatureMotifType = (CreatureMotifType)creatureMotif;
        HasCreatureCanvas = creatureMotif != 0;
        HasWeaponCanvas = hasWeaponCanvas;
        HasLandscapeCanvas = hasLandscapeCanvas;

        // Portrait state
        MogReady = mogReady;
        MadeenReady = madeenReady;

        // Buff state
        HasSwiftcast = statusHelper.HasSwiftcast(player);
        HasStarryMuse = statusHelper.HasStarryMuse(player);
        StarryMuseRemaining = statusHelper.GetStarryMuseRemaining(player);
        HasHyperphantasia = statusHelper.HasHyperphantasia(player);
        HyperphantasiaStacks = statusHelper.GetHyperphantasiaStacks(player);
        HasInspiration = statusHelper.HasInspiration(player);
        HasSubtractiveSpectrum = statusHelper.HasSubtractiveSpectrum(player);
        HasStarstruck = statusHelper.HasStarstruck(player);
        HasRainbowBright = statusHelper.HasRainbowBright(player);
        HasHammerTime = statusHelper.HasHammerTime(player);
        HammerTimeStacks = statusHelper.GetHammerTimeStacks(player);
        HasSubtractivePalette = statusHelper.HasSubtractivePalette(player);
        SubtractivePaletteRemaining = statusHelper.GetSubtractivePaletteRemaining(player);
        HasMonochromeTones = statusHelper.HasMonochromeTones(player);

        // Instant cast check
        HasInstantCast = HasSwiftcast || HasRainbowBright;

        // Determine combo state
        DetermineComboState(comboAction, comboTimer, player.Level,
            out var baseStep, out var isSubtractive, out var hammerStep, out var inHammer);
        BaseComboStep = baseStep;
        IsInSubtractiveCombo = isSubtractive || HasSubtractivePalette;
        HammerComboStep = hammerStep;
        IsInHammerCombo = inHammer || HasHammerTime;

        // Calculate nearby enemies
        _currentTarget = targetingService.FindEnemy(
            configuration.Targeting.EnemyStrategy,
            FFXIVConstants.CasterTargetingRange,
            player);
        NearbyEnemyCount = CountNearbyEnemies(player, 5f);
        ShouldUseAoe = PCTActions.ShouldUseAoe(NearbyEnemyCount, player.Level);

        // Burst window check
        IsInBurstWindow = HasStarryMuse;

        // Motif needs
        CanPaintMotif = !inCombat || !IsCasting;
        NeedsCreatureMotif = !HasCreatureCanvas && player.Level >= PCTActions.CreatureMotif.MinLevel;
        NeedsWeaponMotif = !HasWeaponCanvas && player.Level >= PCTActions.WeaponMotif.MinLevel;
        NeedsLandscapeMotif = !HasLandscapeCanvas && player.Level >= PCTActions.LandscapeMotif.MinLevel;

        // Calculate party health metrics
        PartyHealthMetrics = CalculatePartyHealth(player);

        // Cooldown tracking
        var level = player.Level;
        StarryMuseReady = level >= PCTActions.StarryMuse.MinLevel &&
                         HasLandscapeCanvas &&
                         actionService.IsActionReady(PCTActions.StarryMuse.ActionId);
        LivingMuseReady = level >= PCTActions.LivingMuse.MinLevel &&
                         HasCreatureCanvas &&
                         actionService.IsActionReady(PCTActions.LivingMuse.ActionId);
        LivingMuseCharges = level >= PCTActions.LivingMuse.MinLevel
            ? (int)actionService.GetCurrentCharges(PCTActions.LivingMuse.ActionId)
            : 0;
        StrikingMuseReady = level >= PCTActions.StrikingMuse.MinLevel &&
                          HasWeaponCanvas &&
                          actionService.IsActionReady(PCTActions.StrikingMuse.ActionId);
        SubtractivePaletteReady = level >= PCTActions.SubtractivePalette.MinLevel &&
                                 CanUseSubtractivePalette &&
                                 !HasSubtractivePalette &&
                                 actionService.IsActionReady(PCTActions.SubtractivePalette.ActionId);
        TemperaCoatReady = level >= PCTActions.TemperaCoat.MinLevel &&
                         actionService.IsActionReady(PCTActions.TemperaCoat.ActionId);
        TemperaGrassaReady = level >= PCTActions.TemperaGrassa.MinLevel &&
                           actionService.IsActionReady(PCTActions.TemperaGrassa.ActionId);
        SmudgeReady = level >= PCTActions.Smudge.MinLevel &&
                     actionService.IsActionReady(PCTActions.Smudge.ActionId);
        SwiftcastReady = level >= PCTActions.Swiftcast.MinLevel &&
                        actionService.IsActionReady(PCTActions.Swiftcast.ActionId);
        LucidDreamingReady = level >= PCTActions.LucidDreaming.MinLevel &&
                            actionService.IsActionReady(PCTActions.LucidDreaming.ActionId);

        // Update debug state
        UpdateDebugState();
    }

    private void DetermineComboState(uint comboAction, float comboTimer, byte level,
        out int baseStep, out bool isSubtractive, out int hammerStep, out bool inHammer)
    {
        baseStep = 0;
        isSubtractive = false;
        hammerStep = 0;
        inHammer = false;

        // No combo if timer expired
        if (comboTimer <= 0)
            return;

        // Check base combo actions
        if (comboAction == PCTActions.FireInRed.ActionId || comboAction == PCTActions.Fire2InRed.ActionId)
        {
            baseStep = 1;
            isSubtractive = false;
        }
        else if (comboAction == PCTActions.AeroInGreen.ActionId || comboAction == PCTActions.Aero2InGreen.ActionId)
        {
            baseStep = 2;
            isSubtractive = false;
        }
        // Check subtractive combo actions
        else if (comboAction == PCTActions.BlizzardInCyan.ActionId || comboAction == PCTActions.Blizzard2InCyan.ActionId)
        {
            baseStep = 1;
            isSubtractive = true;
        }
        else if (comboAction == PCTActions.StoneInYellow.ActionId || comboAction == PCTActions.Stone2InYellow.ActionId)
        {
            baseStep = 2;
            isSubtractive = true;
        }
        // Check hammer combo
        else if (comboAction == PCTActions.HammerStamp.ActionId)
        {
            hammerStep = 1;
            inHammer = true;
        }
        else if (comboAction == PCTActions.HammerBrush.ActionId)
        {
            hammerStep = 2;
            inHammer = true;
        }
    }

    private int CountNearbyEnemies(IPlayerCharacter player, float radius)
    {
        var count = 0;
        var playerPos = player.Position;

        foreach (var obj in ObjectTable)
        {
            if (obj is not IBattleNpc npc)
                continue;

            // Skip non-hostile NPCs
            if (npc.SubKind != 5) // BattleNpcSubKind.Enemy
                continue;

            // Skip dead enemies
            if (npc.CurrentHp == 0)
                continue;

            var distance = System.Numerics.Vector3.Distance(playerPos, npc.Position);
            if (distance <= radius + 25f) // Add targeting range
                count++;
        }

        return count;
    }

    private (float avgHpPercent, float lowestHpPercent, int injuredCount) CalculatePartyHealth(IPlayerCharacter player)
    {
        var totalHp = 0f;
        var lowestHp = 1f;
        var injuredCount = 0;
        var memberCount = 0;

        foreach (var member in PartyHelper.GetAllPartyMembers(player))
        {
            var hp = PartyHelper.GetHpPercent(member);
            totalHp += hp;
            memberCount++;

            if (hp < lowestHp)
                lowestHp = hp;

            if (hp < 0.95f)
                injuredCount++;
        }

        var avgHp = memberCount > 0 ? totalHp / memberCount : 1f;
        return (avgHp, lowestHp, injuredCount);
    }

    private void UpdateDebugState()
    {
        // Palette Gauge
        Debug.PaletteGauge = PaletteGauge;
        Debug.CanUseSubtractive = CanUseSubtractivePalette;

        // Paint stacks
        Debug.WhitePaint = WhitePaint;
        Debug.HasBlackPaint = HasBlackPaint;

        // Canvas state
        Debug.CreatureMotif = CreatureMotifType switch
        {
            CreatureMotifType.None => "None",
            CreatureMotifType.Pom => "Pom",
            CreatureMotifType.Wing => "Wing",
            CreatureMotifType.Claw => "Claw",
            CreatureMotifType.Maw => "Maw",
            _ => "Unknown"
        };
        Debug.HasCreatureCanvas = HasCreatureCanvas;
        Debug.HasWeaponCanvas = HasWeaponCanvas;
        Debug.HasLandscapeCanvas = HasLandscapeCanvas;

        // Portrait state
        Debug.MogReady = MogReady;
        Debug.MadeenReady = MadeenReady;

        // Hammer combo
        Debug.IsInHammerCombo = IsInHammerCombo;
        Debug.HammerComboStep = HammerComboStep;
        Debug.HammerComboStepName = HammerComboStep switch
        {
            0 => "Stamp",
            1 => "Brush",
            2 => "Polish",
            _ => "None"
        };

        // Base combo
        Debug.BaseComboStep = BaseComboStep;
        Debug.IsInSubtractiveCombo = IsInSubtractiveCombo;

        // Buff state
        Debug.HasStarryMuse = HasStarryMuse;
        Debug.StarryMuseRemaining = StarryMuseRemaining;
        Debug.HasHyperphantasia = HasHyperphantasia;
        Debug.HyperphantasiaStacks = HyperphantasiaStacks;
        Debug.HasInspiration = HasInspiration;
        Debug.HasSubtractiveSpectrum = HasSubtractiveSpectrum;
        Debug.HasRainbowBright = HasRainbowBright;
        Debug.HasStarstruck = HasStarstruck;
        Debug.HasSwiftcast = HasSwiftcast;
        Debug.HasHammerTime = HasHammerTime;
        Debug.HammerTimeStacks = HammerTimeStacks;

        // Cooldown state
        Debug.StarryMuseReady = StarryMuseReady;
        Debug.LivingMuseReady = LivingMuseReady;
        Debug.LivingMuseCharges = LivingMuseCharges;
        Debug.StrikingMuseReady = StrikingMuseReady;
        Debug.SubtractivePaletteReady = SubtractivePaletteReady;
        Debug.TemperaCoatReady = TemperaCoatReady;
        Debug.TemperaGrassaReady = TemperaGrassaReady;
        Debug.SmudgeReady = SmudgeReady;

        // Resources
        Debug.CurrentMp = CurrentMp;
        Debug.MaxMp = MaxMp;

        // Target
        Debug.CurrentTarget = _currentTarget?.Name?.TextValue ?? "None";
        Debug.NearbyEnemies = NearbyEnemyCount;

        // Phase determination
        if (HasStarstruck)
            Debug.Phase = "Star Prism";
        else if (HasRainbowBright)
            Debug.Phase = "Rainbow Drip";
        else if (IsInHammerCombo || HasHammerTime)
            Debug.Phase = $"Hammer ({Debug.HammerComboStepName})";
        else if (HasStarryMuse)
            Debug.Phase = "Burst";
        else if (IsInSubtractiveCombo)
            Debug.Phase = "Subtractive";
        else if (InCombat)
            Debug.Phase = "Base Combo";
        else if (NeedsCreatureMotif || NeedsWeaponMotif || NeedsLandscapeMotif)
            Debug.Phase = "Painting";
        else
            Debug.Phase = "Waiting";
    }
}
