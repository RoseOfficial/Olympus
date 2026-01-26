using Olympus.Config;

namespace Olympus.Services.Training;

/// <summary>
/// Extension methods for DecisionBuilder providing role-specific shortcuts.
/// Each method sets the appropriate category, priority, and context for its role.
/// </summary>
public static class DecisionBuilderExtensions
{
    #region Universal Categories (All Roles)

    /// <summary>Marks as a damage decision.</summary>
    public static DecisionBuilder AsDamage(this DecisionBuilder b)
        => b.Category(DecisionCategory.Damage).Priority(ExplanationPriority.Low);

    /// <summary>Marks as a burst window decision.</summary>
    public static DecisionBuilder AsBurst(this DecisionBuilder b)
        => b.Category(DecisionCategory.BurstWindow).Priority(ExplanationPriority.Normal);

    /// <summary>Marks as a resource management decision with gauge context.</summary>
    public static DecisionBuilder AsResource(this DecisionBuilder b, string resourceName, int resourceValue)
        => b.Category(DecisionCategory.ResourceManagement)
            .Priority(ExplanationPriority.Normal)
            .Context(new DecisionContext { ResourceName = resourceName, ResourceValue = resourceValue });

    /// <summary>Marks as a utility decision.</summary>
    public static DecisionBuilder AsUtility(this DecisionBuilder b)
        => b.Category(DecisionCategory.Utility).Priority(ExplanationPriority.Normal);

    /// <summary>Marks as an interrupt decision.</summary>
    public static DecisionBuilder AsInterrupt(this DecisionBuilder b)
        => b.Category(DecisionCategory.Interrupt).Priority(ExplanationPriority.High);

    /// <summary>Marks as a raid buff decision.</summary>
    public static DecisionBuilder AsRaidBuff(this DecisionBuilder b)
        => b.Category(DecisionCategory.RaidBuff).Priority(ExplanationPriority.High);

    /// <summary>Marks as an AoE rotation decision.</summary>
    public static DecisionBuilder AsAoE(this DecisionBuilder b, int enemyCount)
        => b.Category(DecisionCategory.AoE(enemyCount))
            .Priority(ExplanationPriority.Low)
            .Context(new DecisionContext { EnemyCount = enemyCount });

    #endregion

    #region Tank Categories

    /// <summary>Marks as a mitigation decision with self HP context.</summary>
    public static DecisionBuilder AsMitigation(this DecisionBuilder b, float selfHpPercent)
        => b.Category(DecisionCategory.Mitigation)
            .Priority(ExplanationPriority.High)
            .Context(new DecisionContext { SelfHpPercent = selfHpPercent });

    /// <summary>Marks as an invulnerability decision (always critical priority).</summary>
    public static DecisionBuilder AsInvuln(this DecisionBuilder b, float selfHpPercent)
        => b.Category(DecisionCategory.Invulnerability)
            .Priority(ExplanationPriority.Critical)
            .Context(new DecisionContext { SelfHpPercent = selfHpPercent });

    /// <summary>Marks as a party mitigation decision.</summary>
    public static DecisionBuilder AsPartyMit(this DecisionBuilder b)
        => b.Category(DecisionCategory.PartyMitigation).Priority(ExplanationPriority.High);

    /// <summary>Marks as an enmity/aggro decision.</summary>
    public static DecisionBuilder AsEnmity(this DecisionBuilder b)
        => b.Category(DecisionCategory.Enmity).Priority(ExplanationPriority.High);

    /// <summary>Marks as a tank resource decision with gauge context.</summary>
    public static DecisionBuilder AsTankResource(this DecisionBuilder b, int gaugeValue)
        => b.Category(DecisionCategory.ResourceManagement)
            .Priority(ExplanationPriority.Normal)
            .Context(new DecisionContext { GaugeValue = gaugeValue });

    /// <summary>Marks as a tank burst decision.</summary>
    public static DecisionBuilder AsTankBurst(this DecisionBuilder b)
        => b.Category(DecisionCategory.BurstWindow).Priority(ExplanationPriority.Normal);

    /// <summary>Marks as a tank damage decision.</summary>
    public static DecisionBuilder AsTankDamage(this DecisionBuilder b)
        => b.Category(DecisionCategory.Damage).Priority(ExplanationPriority.Low);

    #endregion

    #region Healer Categories

    /// <summary>Marks as a healing decision with target HP context.</summary>
    public static DecisionBuilder AsHealing(this DecisionBuilder b, float targetHpPercent, int? healAmount = null)
        => b.Category(DecisionCategory.Healing)
            .Priority(ExplanationPriority.Normal)
            .Context(new DecisionContext { TargetHpPercent = targetHpPercent, HealAmount = healAmount });

    /// <summary>Marks as a defensive cooldown decision.</summary>
    public static DecisionBuilder AsDefensive(this DecisionBuilder b)
        => b.Category(DecisionCategory.Defensive).Priority(ExplanationPriority.High);

    /// <summary>Marks as a buff/oGCD weaving decision.</summary>
    public static DecisionBuilder AsBuff(this DecisionBuilder b)
        => b.Category(DecisionCategory.Buff).Priority(ExplanationPriority.Normal);

    #endregion

    #region Melee DPS Categories

    /// <summary>Marks as a positional decision with hit/miss context.</summary>
    public static DecisionBuilder AsPositional(this DecisionBuilder b, bool hitPositional, string position)
        => b.Category(DecisionCategory.Positional(hitPositional))
            .Priority(ExplanationPriority.Normal)
            .Context(new DecisionContext { HitPositional = hitPositional, Position = position });

    /// <summary>Marks as a combo decision with step context.</summary>
    public static DecisionBuilder AsCombo(this DecisionBuilder b, int comboStep)
        => b.Category(DecisionCategory.Combo(comboStep))
            .Priority(ExplanationPriority.Low)
            .Context(new DecisionContext { ComboStep = comboStep });

    /// <summary>Marks as a melee burst decision.</summary>
    public static DecisionBuilder AsMeleeBurst(this DecisionBuilder b)
        => b.Category(DecisionCategory.BurstWindow).Priority(ExplanationPriority.High);

    /// <summary>Marks as a melee damage decision.</summary>
    public static DecisionBuilder AsMeleeDamage(this DecisionBuilder b)
        => b.Category(DecisionCategory.Damage).Priority(ExplanationPriority.Low);

    /// <summary>Marks as a melee resource decision.</summary>
    public static DecisionBuilder AsMeleeResource(this DecisionBuilder b, string resourceName, int resourceValue)
        => b.Category(DecisionCategory.ResourceManagement)
            .Priority(ExplanationPriority.Normal)
            .Context(new DecisionContext { ResourceName = resourceName, ResourceValue = resourceValue });

    #endregion

    #region Ranged Physical DPS Categories

    /// <summary>Marks as a proc usage decision.</summary>
    public static DecisionBuilder AsProc(this DecisionBuilder b, string procName)
        => b.Category(DecisionCategory.Proc(procName))
            .Priority(ExplanationPriority.Normal)
            .Context(new DecisionContext { ProcName = procName });

    /// <summary>Marks as a song/dance decision.</summary>
    public static DecisionBuilder AsSong(this DecisionBuilder b, string songName, float songRemaining)
        => b.Category(DecisionCategory.Song(songName))
            .Priority(ExplanationPriority.Normal)
            .Context(new DecisionContext { CurrentSong = songName, SongRemaining = songRemaining });

    /// <summary>Marks as a DoT management decision.</summary>
    public static DecisionBuilder AsDot(this DecisionBuilder b, float dotRemaining)
        => b.Category(DecisionCategory.DotManagement)
            .Priority(ExplanationPriority.Normal)
            .Context(new DecisionContext { DotRemaining = dotRemaining });

    /// <summary>Marks as a ranged burst decision.</summary>
    public static DecisionBuilder AsRangedBurst(this DecisionBuilder b)
        => b.Category(DecisionCategory.BurstWindow).Priority(ExplanationPriority.High);

    /// <summary>Marks as a ranged damage decision.</summary>
    public static DecisionBuilder AsRangedDamage(this DecisionBuilder b)
        => b.Category(DecisionCategory.Damage).Priority(ExplanationPriority.Low);

    /// <summary>Marks as a ranged resource decision.</summary>
    public static DecisionBuilder AsRangedResource(this DecisionBuilder b, string resourceName, int resourceValue)
        => b.Category(DecisionCategory.ResourceManagement)
            .Priority(ExplanationPriority.Normal)
            .Context(new DecisionContext { ResourceName = resourceName, ResourceValue = resourceValue });

    #endregion

    #region Caster DPS Categories

    /// <summary>Marks as a phase/element transition decision.</summary>
    public static DecisionBuilder AsPhase(this DecisionBuilder b, string currentPhase, string nextPhase)
        => b.Category(DecisionCategory.Phase(currentPhase, nextPhase))
            .Priority(ExplanationPriority.Normal)
            .Context(new DecisionContext { CurrentPhase = currentPhase, NextPhase = nextPhase });

    /// <summary>Marks as a summon/pet decision.</summary>
    public static DecisionBuilder AsSummon(this DecisionBuilder b, string summonName)
        => b.Category(DecisionCategory.Summon(summonName))
            .Priority(ExplanationPriority.Normal)
            .Context(new DecisionContext { SummonName = summonName });

    /// <summary>Marks as a movement optimization decision.</summary>
    public static DecisionBuilder AsMovement(this DecisionBuilder b)
        => b.Category(DecisionCategory.MovementOptimization).Priority(ExplanationPriority.Normal);

    /// <summary>Marks as a caster melee combo decision (RDM).</summary>
    public static DecisionBuilder AsMeleeCombo(this DecisionBuilder b, int comboStep)
        => b.Category(DecisionCategory.MeleeCombo(comboStep))
            .Priority(ExplanationPriority.Normal)
            .Context(new DecisionContext { ComboStep = comboStep });

    /// <summary>Marks as a caster proc decision.</summary>
    public static DecisionBuilder AsCasterProc(this DecisionBuilder b, string procName)
        => b.Category(DecisionCategory.Proc(procName))
            .Priority(ExplanationPriority.Normal)
            .Context(new DecisionContext { ProcName = procName });

    /// <summary>Marks as a caster DoT decision.</summary>
    public static DecisionBuilder AsCasterDot(this DecisionBuilder b, float dotRemaining)
        => b.Category(DecisionCategory.DotManagement)
            .Priority(ExplanationPriority.Normal)
            .Context(new DecisionContext { DotRemaining = dotRemaining });

    /// <summary>Marks as a caster burst decision.</summary>
    public static DecisionBuilder AsCasterBurst(this DecisionBuilder b)
        => b.Category(DecisionCategory.BurstWindow).Priority(ExplanationPriority.High);

    /// <summary>Marks as a caster damage decision.</summary>
    public static DecisionBuilder AsCasterDamage(this DecisionBuilder b)
        => b.Category(DecisionCategory.Damage).Priority(ExplanationPriority.Low);

    /// <summary>Marks as a caster resource decision.</summary>
    public static DecisionBuilder AsCasterResource(this DecisionBuilder b, string resourceName, int resourceValue)
        => b.Category(DecisionCategory.ResourceManagement)
            .Priority(ExplanationPriority.Normal)
            .Context(new DecisionContext { ResourceName = resourceName, ResourceValue = resourceValue });

    #endregion
}
