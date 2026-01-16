using System;

namespace Olympus.Timeline.Models;

/// <summary>
/// Mutable runtime state for an active timeline.
/// Tracks current position, phase, and sync status during combat.
/// </summary>
public sealed class TimelineState
{
    /// <summary>
    /// The loaded fight timeline.
    /// </summary>
    public FightTimeline Timeline { get; }

    /// <summary>
    /// Current timeline position in seconds.
    /// Updated from combat duration with sync corrections.
    /// </summary>
    public float CurrentTime { get; private set; }

    /// <summary>
    /// Index of the current phase label entry.
    /// -1 if no phase has been reached yet.
    /// </summary>
    public int CurrentPhaseIndex { get; private set; } = -1;

    /// <summary>
    /// The name of the current phase (from label).
    /// Empty string if no phase has been reached.
    /// </summary>
    public string CurrentPhase { get; private set; } = string.Empty;

    /// <summary>
    /// Index of the last entry that was synced to.
    /// Used to prevent re-syncing to the same entry.
    /// </summary>
    public int LastSyncedIndex { get; private set; } = -1;

    /// <summary>
    /// Timestamp of the last sync event.
    /// Used for drift correction.
    /// </summary>
    public DateTime LastSyncTime { get; private set; } = DateTime.MinValue;

    /// <summary>
    /// Accumulated drift between combat time and timeline time.
    /// Positive = timeline is ahead of combat.
    /// </summary>
    public float DriftSeconds { get; private set; }

    /// <summary>
    /// Whether the timeline has been synced at least once.
    /// </summary>
    public bool HasSynced => LastSyncedIndex >= 0;

    /// <summary>
    /// Confidence score for current timeline position (0.0 to 1.0).
    /// Higher when recently synced, decreases over time without sync.
    /// </summary>
    public float Confidence { get; private set; }

    /// <summary>
    /// Creates a new timeline state for the given fight.
    /// </summary>
    public TimelineState(FightTimeline timeline)
    {
        Timeline = timeline;
    }

    /// <summary>
    /// Updates the timeline position based on combat duration.
    /// Does not perform sync - call TrySync separately.
    /// </summary>
    /// <param name="combatDurationSeconds">Current combat duration from CombatEventService.</param>
    public void UpdateTime(float combatDurationSeconds)
    {
        CurrentTime = combatDurationSeconds - DriftSeconds;

        // Decay confidence over time since last sync
        if (LastSyncTime != DateTime.MinValue)
        {
            var secondsSinceSync = (float)(DateTime.UtcNow - LastSyncTime).TotalSeconds;
            // Confidence decays from 1.0 to 0.5 over 30 seconds
            Confidence = Math.Clamp(1f - secondsSinceSync / 60f, 0.5f, 1f);
        }
        else
        {
            // No sync yet - low confidence
            Confidence = 0.3f;
        }
    }

    /// <summary>
    /// Attempts to sync the timeline to an observed action.
    /// </summary>
    /// <param name="actionId">The action ID that was observed.</param>
    /// <param name="combatTime">Current combat duration when the action was observed.</param>
    /// <returns>True if sync was performed, false otherwise.</returns>
    public bool TrySync(uint actionId, float combatTime)
    {
        if (!Timeline.SyncIndex.TryGetValue(actionId, out var indices))
            return false;

        // Find the best matching sync entry within window
        foreach (var index in indices)
        {
            if (index <= LastSyncedIndex)
                continue; // Already synced past this entry

            var entry = Timeline.Entries[index];
            if (entry.Sync is not { } sync)
                continue;

            // Check if current time is within sync window
            var expectedTime = entry.Timestamp;
            var windowStart = expectedTime - sync.WindowBefore;
            var windowEnd = expectedTime + sync.WindowAfter;

            if (combatTime >= windowStart && combatTime <= windowEnd)
            {
                // Sync! Adjust drift to align combat time with expected timeline time
                DriftSeconds = combatTime - expectedTime;
                CurrentTime = expectedTime;
                LastSyncedIndex = index;
                LastSyncTime = DateTime.UtcNow;
                Confidence = 1f;

                // Update phase if this entry has a label
                if (!string.IsNullOrEmpty(entry.Label))
                {
                    CurrentPhaseIndex = index;
                    CurrentPhase = entry.Label;
                }

                // Handle jump directive
                if (entry.HasJump)
                {
                    JumpToTime(entry.JumpTarget);
                }

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Jumps the timeline to a specific timestamp.
    /// Used for phase transitions.
    /// </summary>
    public void JumpToTime(float targetTime)
    {
        // When jumping, we need to recalculate drift
        // The new timeline time should be targetTime
        // So drift = combatTime - targetTime (will be calculated on next Update)
        CurrentTime = targetTime;

        // Find the phase at the target time
        for (var i = Timeline.Entries.Length - 1; i >= 0; i--)
        {
            var entry = Timeline.Entries[i];
            if (entry.Timestamp <= targetTime && !string.IsNullOrEmpty(entry.Label))
            {
                CurrentPhaseIndex = i;
                CurrentPhase = entry.Label;
                break;
            }
        }
    }

    /// <summary>
    /// Jumps the timeline to a labeled position.
    /// </summary>
    /// <param name="label">The label to jump to.</param>
    /// <returns>True if the label was found and jumped to.</returns>
    public bool JumpToLabel(string label)
    {
        var index = Timeline.FindLabelIndex(label);
        if (index < 0)
            return false;

        JumpToTime(Timeline.Entries[index].Timestamp);
        CurrentPhaseIndex = index;
        CurrentPhase = label;
        return true;
    }

    /// <summary>
    /// Resets the timeline state for a new pull.
    /// </summary>
    public void Reset()
    {
        CurrentTime = 0f;
        CurrentPhaseIndex = -1;
        CurrentPhase = string.Empty;
        LastSyncedIndex = -1;
        LastSyncTime = DateTime.MinValue;
        DriftSeconds = 0f;
        Confidence = 0.3f;
    }

    /// <summary>
    /// Forces a sync at the given time with 100% confidence.
    /// Used for simulation mode where timing is perfect.
    /// </summary>
    /// <param name="time">The timeline time to sync to.</param>
    public void ForceSync(float time)
    {
        CurrentTime = time;
        DriftSeconds = 0f;
        LastSyncTime = DateTime.UtcNow;
        Confidence = 1f;

        // Find the phase at this time
        for (var i = Timeline.Entries.Length - 1; i >= 0; i--)
        {
            var entry = Timeline.Entries[i];
            if (entry.Timestamp <= time && !string.IsNullOrEmpty(entry.Label))
            {
                CurrentPhaseIndex = i;
                CurrentPhase = entry.Label;
                break;
            }
        }
    }
}
