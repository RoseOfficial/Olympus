using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Olympus.Localization;
using Olympus.Models.Action;
using Olympus.Services.Action;

namespace Olympus.Windows;

/// <summary>
/// Visual feed showing actions Olympus has just executed.
/// Fades icons over a configurable window so players can see what the bot pressed.
/// </summary>
public sealed class ActionFeedWindow : Window, IDisposable
{
    private readonly Configuration _configuration;
    private readonly Action _saveConfiguration;
    private readonly ActionService _actionService;
    private readonly ITextureProvider _textureProvider;

    private readonly Queue<Entry> _entries = new();
    private readonly object _entriesLock = new();

    private readonly Action<ActionExecutedEvent> _onActionExecuted;

    private readonly struct Entry
    {
        public readonly uint ActionId;
        public readonly string Name;
        public readonly bool IsGcd;
        public readonly DateTime Timestamp;

        public Entry(ActionExecutedEvent e)
        {
            ActionId = e.ActionId;
            Name = e.ActionName;
            IsGcd = e.IsGcd;
            Timestamp = e.TimestampUtc;
        }
    }

    public ActionFeedWindow(
        Configuration configuration,
        Action saveConfiguration,
        ActionService actionService,
        ITextureProvider textureProvider)
        : base(
            "##OlympusActionFeed",
            ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoNav)
    {
        _configuration = configuration;
        _saveConfiguration = saveConfiguration;
        _actionService = actionService;
        _textureProvider = textureProvider;

        Position = new Vector2(configuration.ActionFeed.X, configuration.ActionFeed.Y);
        PositionCondition = ImGuiCond.FirstUseEver;

        _onActionExecuted = OnActionExecuted;
        _actionService.ActionExecuted += _onActionExecuted;
    }

    public void Dispose()
    {
        _actionService.ActionExecuted -= _onActionExecuted;
    }

    private void OnActionExecuted(ActionExecutedEvent evt)
    {
        lock (_entriesLock)
        {
            _entries.Enqueue(new Entry(evt));
            var max = Math.Max(1, _configuration.ActionFeed.MaxIcons);
            while (_entries.Count > max)
                _entries.Dequeue();
        }
    }

    public override bool DrawConditions()
    {
        if (!_configuration.ActionFeed.IsVisible)
            return false;

        lock (_entriesLock)
        {
            if (_entries.Count == 0)
                return false;
        }

        return true;
    }

    public override void PreDraw()
    {
        PruneExpired();
    }

    private void PruneExpired()
    {
        var duration = _configuration.ActionFeed.DurationSeconds;
        var now = DateTime.UtcNow;
        lock (_entriesLock)
        {
            while (_entries.Count > 0)
            {
                var age = (now - _entries.Peek().Timestamp).TotalSeconds;
                if (age > duration)
                    _entries.Dequeue();
                else
                    break;
            }
        }
    }

    public override void Draw()
    {
        var cfg = _configuration.ActionFeed;
        var iconSize = Math.Max(16f, cfg.IconSize);
        var duration = Math.Max(0.25f, cfg.DurationSeconds);
        var now = DateTime.UtcNow;

        Entry[] snapshot;
        lock (_entriesLock)
        {
            snapshot = _entries.ToArray();
        }

        for (var i = 0; i < snapshot.Length; i++)
        {
            var entry = snapshot[i];
            var age = (float)(now - entry.Timestamp).TotalSeconds;
            var fade = Math.Clamp(1f - (age / duration), 0f, 1f);
            if (fade <= 0f) continue;

            if (i > 0) ImGui.SameLine(0, 4);

            var data = GameDataLocalizer.Instance?.GetActionTooltipData(entry.ActionId);
            var iconId = data?.IconId ?? 0u;

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            var cursor = ImGui.GetCursorScreenPos();

            if (iconId != 0)
            {
                var wrap = _textureProvider.GetFromGameIcon(new GameIconLookup(iconId)).GetWrapOrEmpty();
                ImGui.Image(wrap.Handle, new Vector2(iconSize, iconSize), Vector2.Zero, Vector2.One, new Vector4(1f, 1f, 1f, fade));
            }
            else
            {
                ImGui.Dummy(new Vector2(iconSize, iconSize));
            }

            if (ImGui.IsItemHovered())
            {
                var secondsAgo = Math.Max(0f, age);
                ImGui.BeginTooltip();
                ImGui.Text(entry.Name);
                ImGui.TextDisabled($"{(entry.IsGcd ? "GCD" : "oGCD")}  \u2022  ID {entry.ActionId}  \u2022  {secondsAgo:F1}s ago");
                ImGui.EndTooltip();
            }

            // Border tint: green for GCD, blue for oGCD.
            var borderColor = entry.IsGcd
                ? new Vector4(0.4f, 0.9f, 0.4f, fade)
                : new Vector4(0.4f, 0.75f, 1.0f, fade);
            drawList.AddRect(
                cursor,
                cursor + new Vector2(iconSize, iconSize),
                ImGui.ColorConvertFloat4ToU32(borderColor),
                2f,
                ImDrawFlags.None,
                2f);

            if (cfg.ShowLabels)
            {
                ImGui.SameLine(0, 4);
                ImGui.TextColored(new Vector4(1f, 1f, 1f, fade), entry.Name);
            }
        }

        // Persist drag position
        var windowPos = ImGui.GetWindowPos();
        if (Math.Abs(windowPos.X - cfg.X) > 0.5f || Math.Abs(windowPos.Y - cfg.Y) > 0.5f)
        {
            cfg.X = windowPos.X;
            cfg.Y = windowPos.Y;
            _saveConfiguration();
        }
    }
}
