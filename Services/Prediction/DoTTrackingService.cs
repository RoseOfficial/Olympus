using System;
using System.Collections.Generic;

namespace Olympus.Services.Prediction;

/// <summary>
/// Subscribes to local-player damage events and registers known DoT actions
/// with DamageIntakeService so the forecast engine includes future tick damage.
///
/// Per-tick damage values are hardcoded approximations and do not scale with
/// player stats. This is intentional for the triage use case.
/// </summary>
public sealed class DoTTrackingService : IDisposable
{
    private readonly ICombatEventService _ces;
    private readonly IDamageIntakeService _dis;

    /// <summary>
    /// Maps action ID to (estimatedDamagePerTick, durationSeconds).
    /// Values are approximate constants from FFXIV game data.
    /// </summary>
    private static readonly IReadOnlyDictionary<uint, (int DamagePerTick, float Duration)> KnownDoTs =
        new Dictionary<uint, (int, float)>
        {
            // WHM / CNJ
            { 121,   (35, 30f) },   // Aero
            { 132,   (50, 30f) },   // Aero II
            { 16532, (65, 30f) },   // Dia

            // BRD
            { 100,   (40, 45f) },   // Venomous Bite
            { 7406,  (45, 45f) },   // Caustic Bite
            { 113,   (35, 45f) },   // Windbite
            { 7407,  (50, 45f) },   // Stormbite

            // BLM
            { 144,   (45, 24f) },   // Thunder
            { 7420,  (55, 24f) },   // Thunder II
            { 153,   (70, 24f) },   // Thunder III
            { 7421,  (75, 21f) },   // Thunder IV
            { 25855, (80, 30f) },   // High Thunder
            { 25856, (85, 30f) },   // High Thunder II

            // DRG
            { 88,    (45, 24f) },   // Chaos Thrust
            { 25772, (50, 24f) },   // Chaotic Spring

            // NIN
            // Doton is an enemy-targeted ground AoE (not a party DoT), but it registers
            // here so the forecast engine accounts for the ticking damage enemies receive
            // from the local player's Doton during AoE phases.
            { 2270,  (80, 15f) },   // Doton

            // SAM
            { 7480,  (35, 60f) },   // Higanbana
            { 25779, (35, 60f) },   // Higanbana (upgrade)

            // MCH
            { 2872,  (35, 15f) },   // Bioblaster (DoT component)

            // MNK
            { 74,    (40, 15f) },   // Demolish

            // SCH / SMN offensive DoTs
            // Biolysis (17864) replaced Bio II in Endwalker; Bio II (179) was removed from SCH's kit.
            { 17864, (40, 30f) },   // Biolysis
            { 178,   (25, 30f) },   // Bio
        };

    public DoTTrackingService(ICombatEventService ces, IDamageIntakeService dis)
    {
        _ces = ces;
        _dis = dis;
        _ces.OnLocalPlayerDamageDealt += OnDamageDealt;
    }

    private void OnDamageDealt(uint targetEntityId, int damageAmount, uint actionId)
    {
        if (KnownDoTs.TryGetValue(actionId, out var dot))
            _dis.RegisterActiveDoT(targetEntityId, dot.DamagePerTick, dot.Duration);
    }

    public void Dispose() => _ces.OnLocalPlayerDamageDealt -= OnDamageDealt;
}
