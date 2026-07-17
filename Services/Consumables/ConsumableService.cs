using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Olympus.Data;
using Olympus.Services.Content;
using Olympus.Services.Pull;

namespace Olympus.Services.Consumables;

public sealed class ConsumableService : IConsumableService
{
    private const float BurstImminentThresholdSeconds = 5f;
    // Pre-pull: the opener burst window opens ~7.8s into combat; a 10s threshold covers it
    // so IsBurstImminent returns true when SyntheticSecondsUntilBurst = 7.8f (out of combat).
    private const float PrePullBurstImminentThresholdSeconds = 10f;

    private readonly ConsumablesConfig _config;
    private readonly IPullIntentService _pullIntent;
    private readonly IHighEndContentService _highEnd;
    private readonly IInventoryProbe _inventory;
    private readonly ITinctureCooldownProbe _cooldown;
    private readonly IChatGui? _chatGui;

    private bool _warningFiredThisFight;
    private bool _wasInCombat;

    public ConsumableService(
        ConsumablesConfig config,
        IPullIntentService pullIntent,
        IHighEndContentService highEnd,
        IInventoryProbe inventory,
        ITinctureCooldownProbe cooldown,
        IChatGui? chatGui)
    {
        _config = config;
        _pullIntent = pullIntent;
        _highEnd = highEnd;
        _inventory = inventory;
        _cooldown = cooldown;
        _chatGui = chatGui;
    }

    public bool IsTinctureReady(uint jobId)
    {
        if (!TryGetTinctureForJob(jobId, out _, out _)) return false;
        return _cooldown.GetTinctureCooldownRemaining() <= 0f;
    }

    public bool TryGetTinctureForJob(uint jobId, out uint itemId, out bool isHq)
    {
        itemId = 0;
        isHq = false;
        if (!ConsumableIds.TinctureByJob.TryGetValue(jobId, out var nqId)) return false;

        if (_inventory.GetItemCount(nqId + ConsumableIds.HqOffset) > 0)
        {
            itemId = nqId;
            isHq = true;
            return true;
        }
        if (_inventory.GetItemCount(nqId) > 0)
        {
            itemId = nqId;
            isHq = false;
            return true;
        }
        return false;
    }

    public bool ShouldUseTinctureNow(IBurstWindowService burstWindow, bool inCombat, bool prePullPhase)
    {
        if (!_config.EnableAutoTincture) return false;
        if (!_highEnd.IsHighEndZone) return false;

        // Pre-pull: use a wider imminent threshold (10s) to cover the 7.8s synthetic first-burst
        // offset returned by SyntheticSecondsUntilBurst when out of combat. In-combat: keep 5s.
        var imminentThreshold = prePullPhase
            ? PrePullBurstImminentThresholdSeconds
            : BurstImminentThresholdSeconds;
        var burstActive = burstWindow.IsInBurstWindow || burstWindow.IsBurstImminent(imminentThreshold);
        if (!burstActive) return false;

        if (_cooldown.GetTinctureCooldownRemaining() > 0f) return false;

        if (prePullPhase)
        {
            // Allow firing when a party countdown is active (countdown itself is the pull signal)
            // OR when PullIntent has already transitioned (cast-based or queue-based signal).
            return _pullIntent.Current != PullIntent.None
                   || _pullIntent.CountdownRemaining is <= 2.0f;
        }
        return inCombat;
    }

    public void OnTinctureSkippedDueToEmptyBag(uint jobId)
    {
        if (!_config.WarnOnEmptyInventory) return;
        if (_warningFiredThisFight) return;
        _warningFiredThisFight = true;

        if (_chatGui is null) return;
        if (!ConsumableIds.TinctureByJob.TryGetValue(jobId, out var nqId)) return;
        if (!ConsumableIds.StatLabel.TryGetValue(nqId, out var stat)) stat = "<unknown>";

        _chatGui.Print(new XivChatEntry
        {
            Type = XivChatType.Echo,
            Message = new SeString().Append($"[Olympus] No Tincture of {stat} in inventory. Auto-pot inactive this fight."),
        });
    }

    public void OnCombatStateChanged(bool inCombat)
    {
        if (inCombat && !_wasInCombat) _warningFiredThisFight = false;
        _wasInCombat = inCombat;
    }

    public void OnTerritoryChanged()
    {
        _warningFiredThisFight = false;
        _wasInCombat = false;
    }
}
