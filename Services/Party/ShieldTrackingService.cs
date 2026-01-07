using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Olympus.Models;

namespace Olympus.Services.Party;

/// <summary>
/// Tracks shields and mitigation buffs on all party members.
/// Scans party member status effects each frame to maintain current state.
/// </summary>
public sealed class ShieldTrackingService : IShieldTrackingService
{
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;
    private readonly IPluginLog _log;

    // Shield tracking: targetId -> list of active shields
    private readonly Dictionary<uint, List<ShieldInfo>> _shields = new();

    // Mitigation tracking: targetId -> list of active mitigations
    private readonly Dictionary<uint, List<MitigationBuff>> _mitigations = new();

    // Cache for GetAllShields/GetAllMitigations to avoid allocations
    private readonly Dictionary<uint, IReadOnlyList<ShieldInfo>> _shieldsReadOnly = new();
    private readonly Dictionary<uint, IReadOnlyList<MitigationBuff>> _mitigationsReadOnly = new();

    // Known shield status IDs for quick lookup
    private static readonly HashSet<uint> ShieldStatusIdSet = new()
    {
        ShieldStatusIds.DivineBenison,
        ShieldStatusIds.Galvanize,
        ShieldStatusIds.Catalyze,
        ShieldStatusIds.SeraphicVeil,
        ShieldStatusIds.EukrasianDiagnosis,
        ShieldStatusIds.EukrasianPrognosis,
        ShieldStatusIds.Sheltron,
        ShieldStatusIds.HolySheltron,
        ShieldStatusIds.Bloodwhetting,
        ShieldStatusIds.StemTheFlow,
        ShieldStatusIds.TheBlackestNight,
        ShieldStatusIds.HeartOfCorundum,
    };

    // Known mitigation status IDs for quick lookup
    private static readonly HashSet<uint> MitigationStatusIdSet = new()
    {
        MitigationStatusIds.Rampart,
        MitigationStatusIds.Reprisal,
        MitigationStatusIds.Sentinel,
        MitigationStatusIds.HallowedGround,
        MitigationStatusIds.PassageOfArms,
        MitigationStatusIds.Vengeance,
        MitigationStatusIds.Holmgang,
        MitigationStatusIds.ShadowWall,
        MitigationStatusIds.DarkMind,
        MitigationStatusIds.LivingDead,
        MitigationStatusIds.WalkingDead,
        MitigationStatusIds.DarkMissionary,
        MitigationStatusIds.Oblation,
        MitigationStatusIds.Nebula,
        MitigationStatusIds.Superbolide,
        MitigationStatusIds.HeartOfLight,
        MitigationStatusIds.HeartOfStone,
        MitigationStatusIds.Temperance,
        MitigationStatusIds.Expedient,
        MitigationStatusIds.Kerachole,
        MitigationStatusIds.Taurochole,
        MitigationStatusIds.Holos,
        MitigationStatusIds.CollectiveUnconscious,
        MitigationStatusIds.Troubadour,
        MitigationStatusIds.Tactician,
        MitigationStatusIds.ShieldSamba,
        MitigationStatusIds.Magick_Barrier,
        MitigationStatusIds.Bloodwhetting,
    };

    // Invulnerability status IDs
    private static readonly HashSet<uint> InvulnStatusIdSet = new()
    {
        MitigationStatusIds.HallowedGround,
        MitigationStatusIds.Holmgang,
        MitigationStatusIds.Superbolide,
        MitigationStatusIds.LivingDead,
        MitigationStatusIds.WalkingDead,
    };

    public ShieldTrackingService(IObjectTable objectTable, IPartyList partyList, IPluginLog log)
    {
        _objectTable = objectTable;
        _partyList = partyList;
        _log = log;
    }

    /// <summary>
    /// Updates shield and mitigation tracking for all party members.
    /// </summary>
    public void Update()
    {
        // Clear previous frame data
        _shields.Clear();
        _mitigations.Clear();

        // Scan party members
        foreach (var partyMember in _partyList)
        {
            if (partyMember?.GameObject is not IBattleChara chara)
                continue;

            var entityId = chara.EntityId;

            // Initialize lists for this target
            if (!_shields.ContainsKey(entityId))
                _shields[entityId] = new List<ShieldInfo>();
            if (!_mitigations.ContainsKey(entityId))
                _mitigations[entityId] = new List<MitigationBuff>();

            // Scan status effects
            if (chara.StatusList == null)
                continue;

            foreach (var status in chara.StatusList)
            {
                var statusId = status.StatusId;

                // Check if this is a shield
                if (ShieldStatusIdSet.Contains(statusId))
                {
                    var shieldValue = EstimateShieldValue(statusId, chara);
                    _shields[entityId].Add(new ShieldInfo
                    {
                        TargetId = entityId,
                        StatusId = statusId,
                        ShieldValue = shieldValue,
                        RemainingDuration = status.RemainingTime,
                        SourceId = status.SourceId,
                        Name = GetShieldName(statusId),
                        IsPercentageBased = IsPercentageBasedShield(statusId)
                    });
                }

                // Check if this is a mitigation
                if (MitigationStatusIdSet.Contains(statusId))
                {
                    var mitigationPercent = MitigationValues.GetMitigationPercent(statusId);
                    if (mitigationPercent > 0)
                    {
                        _mitigations[entityId].Add(new MitigationBuff
                        {
                            TargetId = entityId,
                            StatusId = statusId,
                            MitigationPercent = mitigationPercent,
                            RemainingDuration = status.RemainingTime,
                            SourceId = status.SourceId,
                            Name = MitigationValues.GetMitigationName(statusId),
                            IsSelfOnly = IsSelfOnlyMitigation(statusId)
                        });
                    }
                }
            }
        }

        // Also check the local player if not in party
        var localPlayer = _objectTable.LocalPlayer;
        if (localPlayer != null && _partyList.Length == 0)
        {
            var entityId = localPlayer.EntityId;

            if (!_shields.ContainsKey(entityId))
                _shields[entityId] = new List<ShieldInfo>();
            if (!_mitigations.ContainsKey(entityId))
                _mitigations[entityId] = new List<MitigationBuff>();

            if (localPlayer.StatusList != null)
            {
                foreach (var status in localPlayer.StatusList)
                {
                    var statusId = status.StatusId;

                    if (ShieldStatusIdSet.Contains(statusId))
                    {
                        var shieldValue = EstimateShieldValue(statusId, localPlayer);
                        _shields[entityId].Add(new ShieldInfo
                        {
                            TargetId = entityId,
                            StatusId = statusId,
                            ShieldValue = shieldValue,
                            RemainingDuration = status.RemainingTime,
                            SourceId = status.SourceId,
                            Name = GetShieldName(statusId),
                            IsPercentageBased = IsPercentageBasedShield(statusId)
                        });
                    }

                    if (MitigationStatusIdSet.Contains(statusId))
                    {
                        var mitigationPercent = MitigationValues.GetMitigationPercent(statusId);
                        if (mitigationPercent > 0)
                        {
                            _mitigations[entityId].Add(new MitigationBuff
                            {
                                TargetId = entityId,
                                StatusId = statusId,
                                MitigationPercent = mitigationPercent,
                                RemainingDuration = status.RemainingTime,
                                SourceId = status.SourceId,
                                Name = MitigationValues.GetMitigationName(statusId),
                                IsSelfOnly = IsSelfOnlyMitigation(statusId)
                            });
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Estimates the shield value for a given shield status.
    /// Some shields are percentage-based (Divine Benison = 15% max HP),
    /// others are potency-based (Galvanize).
    /// </summary>
    private int EstimateShieldValue(uint statusId, IBattleChara target)
    {
        var maxHp = (int)target.MaxHp;

        return statusId switch
        {
            // WHM shields
            ShieldStatusIds.DivineBenison => (int)(maxHp * 0.15f),  // 15% max HP

            // SCH shields - potency based, estimate at ~180% of heal potency for crit Adlo
            ShieldStatusIds.Galvanize => (int)(maxHp * 0.10f),      // Rough estimate
            ShieldStatusIds.Catalyze => (int)(maxHp * 0.20f),       // Crit Adlo shield
            ShieldStatusIds.SeraphicVeil => (int)(maxHp * 0.08f),   // Seraph shield

            // SGE shields
            ShieldStatusIds.EukrasianDiagnosis => (int)(maxHp * 0.18f),  // 180% potency shield
            ShieldStatusIds.EukrasianPrognosis => (int)(maxHp * 0.10f),  // Smaller AoE shield

            // Tank shields
            ShieldStatusIds.TheBlackestNight => (int)(maxHp * 0.25f),    // 25% max HP
            ShieldStatusIds.Bloodwhetting => (int)(maxHp * 0.04f),       // Small shield per hit
            ShieldStatusIds.StemTheFlow => (int)(maxHp * 0.04f),
            ShieldStatusIds.Sheltron => (int)(maxHp * 0.10f),            // Block-based, estimate
            ShieldStatusIds.HolySheltron => (int)(maxHp * 0.15f),
            ShieldStatusIds.HeartOfCorundum => (int)(maxHp * 0.10f),

            _ => (int)(maxHp * 0.10f)  // Default estimate
        };
    }

    /// <summary>
    /// Gets the display name for a shield status.
    /// </summary>
    private static string GetShieldName(uint statusId) => statusId switch
    {
        ShieldStatusIds.DivineBenison => "Divine Benison",
        ShieldStatusIds.Aquaveil => "Aquaveil",
        ShieldStatusIds.Galvanize => "Galvanize",
        ShieldStatusIds.Catalyze => "Catalyze",
        ShieldStatusIds.SeraphicVeil => "Seraphic Veil",
        ShieldStatusIds.EukrasianDiagnosis => "Eukrasian Diagnosis",
        ShieldStatusIds.EukrasianPrognosis => "Eukrasian Prognosis",
        ShieldStatusIds.TheBlackestNight => "The Blackest Night",
        ShieldStatusIds.Bloodwhetting => "Bloodwhetting",
        ShieldStatusIds.Sheltron => "Sheltron",
        ShieldStatusIds.HolySheltron => "Holy Sheltron",
        ShieldStatusIds.HeartOfCorundum => "Heart of Corundum",
        _ => $"Shield ({statusId})"
    };

    /// <summary>
    /// Returns true if the shield is percentage-based (scales with max HP).
    /// </summary>
    private static bool IsPercentageBasedShield(uint statusId) => statusId switch
    {
        ShieldStatusIds.DivineBenison => true,
        ShieldStatusIds.TheBlackestNight => true,
        _ => false
    };

    /// <summary>
    /// Returns true if the mitigation only affects the target (not party-wide).
    /// </summary>
    private static bool IsSelfOnlyMitigation(uint statusId) => statusId switch
    {
        MitigationStatusIds.Rampart => true,
        MitigationStatusIds.Sentinel => true,
        MitigationStatusIds.Vengeance => true,
        MitigationStatusIds.ShadowWall => true,
        MitigationStatusIds.DarkMind => true,
        MitigationStatusIds.Nebula => true,
        MitigationStatusIds.HeartOfStone => true,
        MitigationStatusIds.Oblation => true,
        MitigationStatusIds.Taurochole => true,
        MitigationStatusIds.HallowedGround => true,
        MitigationStatusIds.Holmgang => true,
        MitigationStatusIds.Superbolide => true,
        MitigationStatusIds.LivingDead => true,
        _ => false  // Party-wide mitigations
    };

    public IReadOnlyList<ShieldInfo> GetShields(uint targetId)
    {
        return _shields.TryGetValue(targetId, out var shields)
            ? shields
            : Array.Empty<ShieldInfo>();
    }

    public IReadOnlyList<MitigationBuff> GetMitigations(uint targetId)
    {
        return _mitigations.TryGetValue(targetId, out var mitigations)
            ? mitigations
            : Array.Empty<MitigationBuff>();
    }

    public int GetTotalShieldValue(uint targetId)
    {
        if (!_shields.TryGetValue(targetId, out var shields))
            return 0;

        return shields.Sum(s => s.ShieldValue);
    }

    public float GetCombinedMitigation(uint targetId)
    {
        if (!_mitigations.TryGetValue(targetId, out var mitigations) || mitigations.Count == 0)
            return 0f;

        // Mitigations stack multiplicatively
        // e.g., 20% + 10% = 1 - (0.8 * 0.9) = 0.28 (28% total)
        var remainingDamage = 1f;
        foreach (var mit in mitigations)
        {
            remainingDamage *= (1f - mit.MitigationPercent);
        }

        return 1f - remainingDamage;
    }

    public uint GetEffectiveHp(uint targetId, uint currentHp)
    {
        var shieldValue = GetTotalShieldValue(targetId);
        return (uint)(currentHp + shieldValue);
    }

    public uint GetEffectiveHpWithPending(uint targetId, uint currentHp, int pendingHeals)
    {
        var shieldValue = GetTotalShieldValue(targetId);
        return (uint)Math.Max(0, currentHp + pendingHeals + shieldValue);
    }

    public bool HasAnyShield(uint targetId)
    {
        return _shields.TryGetValue(targetId, out var shields) && shields.Count > 0;
    }

    public bool HasShield(uint targetId, uint statusId)
    {
        return _shields.TryGetValue(targetId, out var shields)
            && shields.Any(s => s.StatusId == statusId);
    }

    public bool HasAnyMitigation(uint targetId)
    {
        return _mitigations.TryGetValue(targetId, out var mits) && mits.Count > 0;
    }

    public bool IsInvulnerable(uint targetId)
    {
        if (!_mitigations.TryGetValue(targetId, out var mitigations))
            return false;

        return mitigations.Any(m => InvulnStatusIdSet.Contains(m.StatusId));
    }

    public IReadOnlyDictionary<uint, IReadOnlyList<ShieldInfo>> GetAllShields()
    {
        _shieldsReadOnly.Clear();
        foreach (var kvp in _shields)
        {
            _shieldsReadOnly[kvp.Key] = kvp.Value;
        }
        return _shieldsReadOnly;
    }

    public IReadOnlyDictionary<uint, IReadOnlyList<MitigationBuff>> GetAllMitigations()
    {
        _mitigationsReadOnly.Clear();
        foreach (var kvp in _mitigations)
        {
            _mitigationsReadOnly[kvp.Key] = kvp.Value;
        }
        return _mitigationsReadOnly;
    }

    public void Clear()
    {
        _shields.Clear();
        _mitigations.Clear();
        _shieldsReadOnly.Clear();
        _mitigationsReadOnly.Clear();
    }
}
