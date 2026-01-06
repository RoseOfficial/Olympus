namespace Olympus.Data;

/// <summary>
/// Centralized registry of FFXIV job IDs.
/// Use this instead of hardcoding job IDs throughout the codebase.
/// </summary>
public static class JobRegistry
{
    // Healers
    public const uint WhiteMage = 24;
    public const uint Conjurer = 6;
    public const uint Scholar = 28;
    public const uint Arcanist = 26;
    public const uint Astrologian = 33;
    public const uint Sage = 40;

    // Tanks
    public const uint Paladin = 19;
    public const uint Gladiator = 1;
    public const uint Warrior = 21;
    public const uint Marauder = 3;
    public const uint DarkKnight = 32;
    public const uint Gunbreaker = 37;

    // Melee DPS
    public const uint Monk = 20;
    public const uint Pugilist = 2;
    public const uint Dragoon = 22;
    public const uint Lancer = 4;
    public const uint Ninja = 30;
    public const uint Rogue = 29;
    public const uint Samurai = 34;
    public const uint Reaper = 39;
    public const uint Viper = 41;

    // Ranged Physical DPS
    public const uint Bard = 23;
    public const uint Archer = 5;
    public const uint Machinist = 31;
    public const uint Dancer = 38;

    // Casters
    public const uint BlackMage = 25;
    public const uint Thaumaturge = 7;
    public const uint Summoner = 27;
    public const uint RedMage = 35;
    public const uint Pictomancer = 42;

    /// <summary>
    /// Returns true if the job is a healer (WHM, SCH, AST, SGE).
    /// </summary>
    public static bool IsHealer(uint jobId) => jobId is
        WhiteMage or Conjurer or
        Scholar or Arcanist or
        Astrologian or
        Sage;

    /// <summary>
    /// Returns true if the job is a tank (PLD, WAR, DRK, GNB).
    /// </summary>
    public static bool IsTank(uint jobId) => jobId is
        Paladin or Gladiator or
        Warrior or Marauder or
        DarkKnight or
        Gunbreaker;

    /// <summary>
    /// Returns true if the job is a White Mage or Conjurer.
    /// </summary>
    public static bool IsWhiteMage(uint jobId) => jobId is WhiteMage or Conjurer;

    /// <summary>
    /// Returns true if the job is a Scholar or Arcanist.
    /// </summary>
    public static bool IsScholar(uint jobId) => jobId is Scholar or Arcanist;

    /// <summary>
    /// Returns true if the job is an Astrologian.
    /// </summary>
    public static bool IsAstrologian(uint jobId) => jobId == Astrologian;

    /// <summary>
    /// Returns true if the job is a Sage.
    /// </summary>
    public static bool IsSage(uint jobId) => jobId == Sage;
}
