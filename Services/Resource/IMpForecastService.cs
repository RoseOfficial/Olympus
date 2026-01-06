namespace Olympus.Services.Resource;

/// <summary>
/// Service for forecasting MP usage and managing MP conservation strategies.
/// Used to make intelligent decisions about Thin Air, Lucid Dreaming, and spell selection.
/// </summary>
public interface IMpForecastService
{
    /// <summary>
    /// Gets the player's current MP.
    /// </summary>
    int CurrentMp { get; }

    /// <summary>
    /// Gets the player's current MP as a percentage (0.0 to 1.0).
    /// </summary>
    float MpPercent { get; }

    /// <summary>
    /// Gets the player's maximum MP.
    /// </summary>
    int MaxMp { get; }

    /// <summary>
    /// Estimates seconds until out of MP based on recent casting patterns.
    /// </summary>
    /// <param name="reserveMp">MP to keep in reserve (e.g., for Raise).</param>
    /// <returns>Estimated seconds until MP reaches reserve level. Returns float.MaxValue if not casting.</returns>
    float SecondsUntilOom(int reserveMp = 2400);

    /// <summary>
    /// Gets the current MP regeneration rate per second.
    /// Includes natural regen and Lucid Dreaming bonus.
    /// </summary>
    float GetMpRegenRate();

    /// <summary>
    /// Gets the current MP consumption rate per second based on recent casts.
    /// </summary>
    float GetMpConsumptionRate();

    /// <summary>
    /// Gets the net MP change rate (regen - consumption).
    /// Positive = gaining MP, negative = losing MP.
    /// </summary>
    float GetNetMpRate();

    /// <summary>
    /// Checks if Lucid Dreaming is currently active.
    /// </summary>
    bool IsLucidDreamingActive { get; }

    /// <summary>
    /// Checks if the player is in MP conservation mode.
    /// Active when MP is low and trending downward.
    /// </summary>
    bool IsInConservationMode { get; }

    /// <summary>
    /// Records an MP expenditure for tracking consumption patterns.
    /// Called when a spell is cast.
    /// </summary>
    /// <param name="mpCost">The MP cost of the spell.</param>
    void RecordMpExpenditure(int mpCost);

    /// <summary>
    /// Updates the service with current player state.
    /// Should be called each frame.
    /// </summary>
    /// <param name="currentMp">Current MP.</param>
    /// <param name="maxMp">Maximum MP.</param>
    /// <param name="hasLucidDreaming">Whether Lucid Dreaming buff is active.</param>
    void Update(int currentMp, int maxMp, bool hasLucidDreaming);
}
