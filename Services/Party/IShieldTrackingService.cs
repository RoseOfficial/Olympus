using System.Collections.Generic;
using Olympus.Models;

namespace Olympus.Services.Party;

/// <summary>
/// Interface for tracking shields and mitigation buffs on party members.
/// Provides effective HP calculations that include shield values.
/// </summary>
public interface IShieldTrackingService
{
    /// <summary>
    /// Updates shield and mitigation tracking for all party members.
    /// Should be called each frame.
    /// </summary>
    void Update();

    /// <summary>
    /// Gets all active shields on a specific target.
    /// </summary>
    IReadOnlyList<ShieldInfo> GetShields(uint targetId);

    /// <summary>
    /// Gets all active mitigation buffs on a specific target.
    /// </summary>
    IReadOnlyList<MitigationBuff> GetMitigations(uint targetId);

    /// <summary>
    /// Gets the total shield value for a target (sum of all active shields).
    /// </summary>
    int GetTotalShieldValue(uint targetId);

    /// <summary>
    /// Gets the combined mitigation percentage for a target.
    /// Multiple mitigations stack multiplicatively, not additively.
    /// e.g., 20% + 10% = 1 - (0.8 * 0.9) = 28% total mitigation
    /// </summary>
    float GetCombinedMitigation(uint targetId);

    /// <summary>
    /// Gets the effective HP for a target (current HP + shields).
    /// </summary>
    uint GetEffectiveHp(uint targetId, uint currentHp);

    /// <summary>
    /// Gets the effective HP including pending heals and shields.
    /// </summary>
    uint GetEffectiveHpWithPending(uint targetId, uint currentHp, int pendingHeals);

    /// <summary>
    /// Checks if a target has any active shields.
    /// </summary>
    bool HasAnyShield(uint targetId);

    /// <summary>
    /// Checks if a target has a specific shield active.
    /// </summary>
    bool HasShield(uint targetId, uint statusId);

    /// <summary>
    /// Checks if a target has any mitigation active.
    /// </summary>
    bool HasAnyMitigation(uint targetId);

    /// <summary>
    /// Checks if a target has invulnerability (Hallowed Ground, Superbolide, etc.)
    /// </summary>
    bool IsInvulnerable(uint targetId);

    /// <summary>
    /// Gets a summary of all party shields for debugging.
    /// </summary>
    IReadOnlyDictionary<uint, IReadOnlyList<ShieldInfo>> GetAllShields();

    /// <summary>
    /// Gets a summary of all party mitigations for debugging.
    /// </summary>
    IReadOnlyDictionary<uint, IReadOnlyList<MitigationBuff>> GetAllMitigations();

    /// <summary>
    /// Clears all tracked shields and mitigations.
    /// Call on zone change or party change.
    /// </summary>
    void Clear();
}
