using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Olympus.Data;

namespace Olympus.Services.Astrologian;

/// <summary>
/// Tracks Earthly Star placement, maturation, and detonation readiness.
/// Earthly Star matures from Earthly Dominance (540 potency) to Giant Dominance (720 potency) after 10 seconds.
/// </summary>
public sealed class EarthlyStarService : IEarthlyStarService
{
    private readonly IObjectTable _objectTable;

    // Star state tracking
    private DateTime _starPlacedTime = DateTime.MinValue;
    private Vector3? _starPosition;
    private bool _isStarPlaced;

    /// <summary>
    /// Time required for Earthly Star to mature (become Giant Dominance).
    /// </summary>
    public const float MaturationTime = 10f;

    /// <summary>
    /// Total duration Earthly Star lasts before auto-detonating.
    /// </summary>
    public const float MaxDuration = 20f;

    /// <summary>
    /// Radius of Earthly Star effect.
    /// </summary>
    public const float StarRadius = 8f;

    public EarthlyStarService(IObjectTable objectTable)
    {
        _objectTable = objectTable;
    }

    /// <summary>
    /// Gets whether Earthly Star is currently placed.
    /// </summary>
    public bool IsStarPlaced => _isStarPlaced && (DateTime.UtcNow - _starPlacedTime).TotalSeconds < MaxDuration;

    /// <summary>
    /// Gets whether Earthly Star has matured (Giant Dominance - 720 potency).
    /// </summary>
    public bool IsStarMature => IsStarPlaced && (DateTime.UtcNow - _starPlacedTime).TotalSeconds >= MaturationTime;

    /// <summary>
    /// Gets the time remaining until Earthly Star matures.
    /// Returns 0 if already mature or not placed.
    /// </summary>
    public float TimeUntilMature
    {
        get
        {
            if (!IsStarPlaced)
                return 0f;

            var elapsed = (float)(DateTime.UtcNow - _starPlacedTime).TotalSeconds;
            if (elapsed >= MaturationTime)
                return 0f;

            return MaturationTime - elapsed;
        }
    }

    /// <summary>
    /// Gets the time remaining before Earthly Star auto-detonates.
    /// </summary>
    public float TimeRemaining
    {
        get
        {
            if (!IsStarPlaced)
                return 0f;

            var elapsed = (float)(DateTime.UtcNow - _starPlacedTime).TotalSeconds;
            return Math.Max(0f, MaxDuration - elapsed);
        }
    }

    /// <summary>
    /// Gets the position where Earthly Star is placed.
    /// </summary>
    public Vector3? StarPosition => IsStarPlaced ? _starPosition : null;

    /// <summary>
    /// Gets the heal potency of the current Earthly Star state.
    /// </summary>
    public int CurrentPotency => IsStarMature ? 720 : 540;

    /// <summary>
    /// Gets the damage potency of the current Earthly Star state.
    /// </summary>
    public int CurrentDamagePotency => IsStarMature ? 310 : 240;

    /// <summary>
    /// Called when Earthly Star is placed.
    /// </summary>
    /// <param name="position">The position where the star was placed.</param>
    public void OnStarPlaced(Vector3 position)
    {
        _isStarPlaced = true;
        _starPlacedTime = DateTime.UtcNow;
        _starPosition = position;
    }

    /// <summary>
    /// Called when Earthly Star is detonated or expires.
    /// </summary>
    public void OnStarDetonated()
    {
        _isStarPlaced = false;
        _starPosition = null;
    }

    /// <summary>
    /// Updates the service state. Call once per frame.
    /// </summary>
    public void Update()
    {
        // Auto-clear star if it has expired
        if (_isStarPlaced && (DateTime.UtcNow - _starPlacedTime).TotalSeconds >= MaxDuration)
        {
            OnStarDetonated();
        }

        // Could also scan ObjectTable for the actual ground effect here
        // to sync state with the game's actual star placement
    }

    /// <summary>
    /// Counts party members within Earthly Star's radius.
    /// </summary>
    /// <param name="partyMembers">The party members to check.</param>
    /// <returns>Number of party members in range of the star.</returns>
    public int CountPartyMembersInStar(IEnumerable<IBattleChara> partyMembers)
    {
        if (!IsStarPlaced || _starPosition == null)
            return 0;

        var starPos = _starPosition.Value;
        return partyMembers.Count(member =>
            member != null &&
            !member.IsDead &&
            Vector3.Distance(member.Position, starPos) <= StarRadius);
    }

    /// <summary>
    /// Checks if a specific position is within Earthly Star's radius.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <returns>True if the position is within the star's effect radius.</returns>
    public bool IsPositionInStar(Vector3 position)
    {
        if (!IsStarPlaced || _starPosition == null)
            return false;

        return Vector3.Distance(position, _starPosition.Value) <= StarRadius;
    }

    /// <summary>
    /// Gets whether we should detonate the star based on party health and star state.
    /// </summary>
    /// <param name="partyMembers">Party members to evaluate.</param>
    /// <param name="avgHpPercent">Average party HP percentage.</param>
    /// <param name="threshold">HP threshold for detonation.</param>
    /// <param name="minTargets">Minimum targets required.</param>
    /// <param name="emergencyThreshold">Emergency HP threshold to ignore maturity.</param>
    /// <param name="waitForMature">Whether to wait for Giant Dominance.</param>
    /// <returns>True if star should be detonated.</returns>
    public bool ShouldDetonate(
        IEnumerable<IBattleChara> partyMembers,
        float avgHpPercent,
        float threshold,
        int minTargets,
        float emergencyThreshold,
        bool waitForMature)
    {
        if (!IsStarPlaced)
            return false;

        var membersInRange = CountPartyMembersInStar(partyMembers);
        if (membersInRange < minTargets)
            return false;

        // Emergency detonation: party HP critical, don't wait for maturity
        if (avgHpPercent <= emergencyThreshold)
            return true;

        // Normal detonation: check threshold and maturity preference
        if (avgHpPercent <= threshold)
        {
            // If we want to wait for mature star, only detonate if mature or about to expire
            if (waitForMature)
            {
                return IsStarMature || TimeRemaining < 3f;
            }
            return true;
        }

        // Star is about to expire and we have injured party members
        if (TimeRemaining < 3f && avgHpPercent < 0.95f)
            return true;

        return false;
    }

    /// <summary>
    /// Gets debug information about the current star state.
    /// </summary>
    public string GetDebugInfo()
    {
        if (!IsStarPlaced)
            return "Star: Not Placed";

        var state = IsStarMature ? "Giant Dominance" : "Earthly Dominance";
        var timeInfo = IsStarMature
            ? $"{TimeRemaining:F1}s remaining"
            : $"{TimeUntilMature:F1}s to mature";

        return $"Star: {state} | {timeInfo} | Potency: {CurrentPotency}";
    }
}

/// <summary>
/// Interface for Earthly Star tracking service.
/// </summary>
public interface IEarthlyStarService
{
    /// <summary>
    /// Gets whether Earthly Star is currently placed.
    /// </summary>
    bool IsStarPlaced { get; }

    /// <summary>
    /// Gets whether Earthly Star has matured (Giant Dominance).
    /// </summary>
    bool IsStarMature { get; }

    /// <summary>
    /// Gets the time remaining until Earthly Star matures.
    /// </summary>
    float TimeUntilMature { get; }

    /// <summary>
    /// Gets the time remaining before Earthly Star expires.
    /// </summary>
    float TimeRemaining { get; }

    /// <summary>
    /// Gets the position where Earthly Star is placed.
    /// </summary>
    Vector3? StarPosition { get; }

    /// <summary>
    /// Gets the heal potency of the current Earthly Star state.
    /// </summary>
    int CurrentPotency { get; }

    /// <summary>
    /// Gets the damage potency of the current Earthly Star state.
    /// </summary>
    int CurrentDamagePotency { get; }

    /// <summary>
    /// Called when Earthly Star is placed.
    /// </summary>
    void OnStarPlaced(Vector3 position);

    /// <summary>
    /// Called when Earthly Star is detonated or expires.
    /// </summary>
    void OnStarDetonated();

    /// <summary>
    /// Updates the service state.
    /// </summary>
    void Update();

    /// <summary>
    /// Counts party members within Earthly Star's radius.
    /// </summary>
    int CountPartyMembersInStar(IEnumerable<IBattleChara> partyMembers);

    /// <summary>
    /// Checks if a position is within Earthly Star's radius.
    /// </summary>
    bool IsPositionInStar(Vector3 position);

    /// <summary>
    /// Gets whether we should detonate the star.
    /// </summary>
    bool ShouldDetonate(
        IEnumerable<IBattleChara> partyMembers,
        float avgHpPercent,
        float threshold,
        int minTargets,
        float emergencyThreshold,
        bool waitForMature);

    /// <summary>
    /// Gets debug information about the current star state.
    /// </summary>
    string GetDebugInfo();
}
