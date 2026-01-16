namespace Olympus.Timeline.Models;

/// <summary>
/// Categorizes timeline entries by their combat significance.
/// Used by rotation modules to make intelligent cooldown decisions.
/// </summary>
public enum TimelineEntryType : byte
{
    /// <summary>
    /// Generic ability or cast without special categorization.
    /// </summary>
    Ability = 0,

    /// <summary>
    /// Party-wide damage that hits all members.
    /// Triggers pre-shields and party mitigation cooldowns.
    /// </summary>
    Raidwide = 1,

    /// <summary>
    /// Heavy single-target damage on the tank.
    /// Triggers tank cooldowns and tank-specific healing.
    /// </summary>
    TankBuster = 2,

    /// <summary>
    /// Fight phase transition (boss goes untargetable, adds spawn, etc.).
    /// May reset timeline position via jump directive.
    /// </summary>
    Phase = 3,

    /// <summary>
    /// Internal sync point for timeline alignment.
    /// Not displayed to users but used for combat state tracking.
    /// </summary>
    Sync = 4,

    /// <summary>
    /// Cast bar that must complete before damage resolves.
    /// Provides advance warning for preparation.
    /// </summary>
    Cast = 5,

    /// <summary>
    /// Enrage or hard DPS check mechanic.
    /// Used for timing warnings.
    /// </summary>
    Enrage = 6,

    /// <summary>
    /// Add phase or additional enemy spawn.
    /// </summary>
    Adds = 7,

    /// <summary>
    /// Movement-heavy mechanic requiring positioning.
    /// Healers may prefer instant casts during this period.
    /// </summary>
    Movement = 8,

    /// <summary>
    /// Stack mechanic requiring players to group.
    /// </summary>
    Stack = 9,

    /// <summary>
    /// Spread mechanic requiring players to separate.
    /// </summary>
    Spread = 10,
}
