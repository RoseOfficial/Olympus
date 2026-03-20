using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Olympus.Models;
using Olympus.Models.Action;

namespace Olympus.Services.Targeting;

/// <summary>
/// Smart AoE service: uses Lumina Action sheet data to determine the optimal facing angle
/// for cone/line abilities. Falls back to standard circular targeting for other shapes.
/// </summary>
public sealed class SmartAoEService : ISmartAoEService, IDisposable
{
    private readonly ITargetingService _targetingService;
    private readonly IDataManager _dataManager;
    private readonly AoETracker _tracker;
    private readonly IPluginLog _log;
    private ICombatEventService? _subscribedCombatEventService;
    private uint _lastPredictedActionId;
    private bool _disposed;

    public SmartAoEService(ITargetingService targetingService, IDataManager dataManager, AoETracker tracker, IPluginLog log)
    {
        _targetingService = targetingService;
        _dataManager = dataManager;
        _tracker = tracker;
        _log = log;
    }

    /// <summary>
    /// Subscribe to CombatEventService to record actual hit counts.
    /// </summary>
    public void SubscribeToCombatEvents(ICombatEventService combatEventService)
    {
        _subscribedCombatEventService?.OnLocalAbilityResolved -= OnAbilityResolved;
        _subscribedCombatEventService = combatEventService;
        combatEventService.OnLocalAbilityResolved += OnAbilityResolved;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_subscribedCombatEventService != null)
            _subscribedCombatEventService.OnLocalAbilityResolved -= OnAbilityResolved;
    }

    private void OnAbilityResolved(uint actionId, int targetCount)
    {
        // Only record if this was the exact directional AoE action we predicted
        if (_lastPredictedActionId != 0 && actionId == _lastPredictedActionId && _tracker.History.Count > 0)
        {
            var last = _tracker.History[^1];
            if (!last.Resolved)
                _tracker.RecordActual(targetCount);
            _lastPredictedActionId = 0;
        }
    }

    /// <summary>
    /// Finds the best target for an AoE ability using Lumina data to determine shape.
    /// Updates AoETracker with the result.
    /// CastType: 2/5/6/7 = Circle, 3 = Cone, 4/8/12 = Rect/Line
    /// </summary>
    public AoEResult FindBestAoETarget(uint actionId, float maxRange, IPlayerCharacter player, bool recordPrediction = true)
    {
        var actionRow = _dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>()?.GetRowOrDefault(actionId);
        if (!actionRow.HasValue)
            return new AoEResult(null, 0, 0f, AoEShape.Circle);

        var action = actionRow.Value;
        var effectRange = (float)action.EffectRange;
        var xAxisModifier = (float)action.XAxisModifier;
        var castType = action.CastType;
        var actionName = action.Name.ToString();

        AoEResult result;

        switch (castType)
        {
            case 3: // Cone
            {
                // Cone half-angle: parse from Omen if available, default 45° (90° total)
                var halfAngle = DetermineConeHalfAngle(action);
                var (target, hitCount, angle) = _targetingService.FindBestConeAoETarget(
                    halfAngle, effectRange, maxRange, player);
                result = new AoEResult(target, hitCount, angle, AoEShape.Cone);
                break;
            }

            case 4 or 8 or 12: // Line / Rect
            {
                // Width from XAxisModifier, length from EffectRange
                var width = xAxisModifier > 0 ? xAxisModifier : 4f; // default 4y width
                var (target, hitCount, angle) = _targetingService.FindBestLineAoETarget(
                    width, effectRange, maxRange, player);
                result = new AoEResult(target, hitCount, angle, AoEShape.Line);
                break;
            }

            default: // Circle (CastType 2, 5, 6, 7, etc.)
            {
                var (target, hitCount) = _targetingService.FindBestAoETarget(effectRange, maxRange, player);
                var angle = 0f;
                if (target != null)
                {
                    var dx = target.Position.X - player.Position.X;
                    var dz = target.Position.Z - player.Position.Z;
                    angle = MathF.Atan2(dx, dz);
                }
                result = new AoEResult(target, hitCount, angle, AoEShape.Circle);
                break;
            }
        }

        // Update tracker
        _tracker.LastResult = result;
        _tracker.LastPlayerPosition = player.Position;

        if (recordPrediction && result.HitCount > 0)
        {
            _lastPredictedActionId = actionId;
            _tracker.RecordPrediction(
                string.IsNullOrEmpty(actionName) ? $"Action#{actionId}" : actionName,
                result.Shape, result.HitCount, player.Position, result.OptimalAngle);
        }

        return result;
    }

    /// <summary>
    /// Quick check: is this action a directional AoE (cone or line) based on Lumina CastType?
    /// </summary>
    public bool IsDirectionalAoE(uint actionId)
    {
        var actionRow = _dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>()?.GetRowOrDefault(actionId);
        if (!actionRow.HasValue) return false;
        return actionRow.Value.CastType is 3 or 4 or 8 or 12;
    }

    /// <summary>
    /// Quick check: is this action any kind of AoE (circle, cone, or line)?
    /// </summary>
    public bool IsAoE(uint actionId)
    {
        var actionRow = _dataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>()?.GetRowOrDefault(actionId);
        if (!actionRow.HasValue) return false;
        return actionRow.Value.CastType is >= 2 and not 1 && actionRow.Value.EffectRange > 0;
    }

    private static float DetermineConeHalfAngle(Lumina.Excel.Sheets.Action action)
    {
        // Try to parse cone angle from Omen path (e.g., "fan120" = 120° total)
        var omenPath = action.Omen.ValueNullable?.Path.ToString() ?? "";
        if (!string.IsNullOrEmpty(omenPath) && omenPath.Contains("fan", StringComparison.OrdinalIgnoreCase))
        {
            // Extract number after "fan"
            var idx = omenPath.IndexOf("fan", StringComparison.OrdinalIgnoreCase) + 3;
            var numStr = "";
            while (idx < omenPath.Length && char.IsDigit(omenPath[idx]))
            {
                numStr += omenPath[idx];
                idx++;
            }
            if (int.TryParse(numStr, out var degrees) && degrees > 0 && degrees <= 360)
                return degrees * 0.5f * MathF.PI / 180f;
        }

        // Default: 90° total (45° half-angle) — most common cone size
        return 45f * MathF.PI / 180f;
    }
}
