# Quick Toggle Overlay — Design Spec

**Date:** 2026-03-12
**Status:** Approved

## Problem

Opening the MainWindow mid-fight to flip a toggle (e.g., disable healing during a tricky mechanic) is too slow and disruptive. Users need a minimal always-visible overlay they can click in one action.

## Solution

A small floating ImGui overlay window (`OverlayWindow`) with three toggle rows: Rotation (master enable), Healing, and Damage. It is always visible by default, draggable, and its position and visibility are persisted to config.

## Files

### New: `Config/OverlayConfig.cs`

```csharp
namespace Olympus.Config;

public sealed class OverlayConfig
{
    public bool IsVisible { get; set; } = true;
    public float X { get; set; } = 100f;
    public float Y { get; set; } = 100f;
}
```

### New: `Windows/OverlayWindow.cs`

- Inherits `Dalamud.Interface.Windowing.Window`
- Window name passed to base constructor: `"##OlympusOverlay"`
- Constructor takes `Configuration configuration`, `Action saveConfiguration`
- Window flags: `NoTitleBar | NoResize | NoScrollbar | NoScrollWithMouse | NoCollapse | AlwaysAutoResize | NoFocusOnAppearing | NoNav`
- No `NoMove` — window is draggable
- `PositionCondition = ImGuiCond.FirstUseEver`, initial position from `new Vector2(configuration.Overlay.X, configuration.Overlay.Y)`
- `OnOpen` and `OnClose` do NOT call `saveConfiguration()` — visibility is persisted by `SaveConfiguration()` in `Plugin.cs` (see below)

**`Draw()` layout:**

1. Small `"Olympus"` header label using `ImGui.TextDisabled()`
2. `ImGui.Separator()`
3. Three checkbox rows. Each row uses `ImGui.PushStyleColor(ImGuiCol.Text, color)` before the `ImGui.Checkbox` call and `ImGui.PopStyleColor()` after, where `color` is green `(0.4f, 0.8f, 0.4f, 1f)` when the value is `true` and dimmed `(0.5f, 0.5f, 0.5f, 1f)` when `false`. On any checkbox change, call `saveConfiguration()`.

| Row | Label | Config field |
|-----|-------|-------------|
| 1 | `"Rotation"` | `configuration.Enabled` |
| 2 | `"Healing"` | `configuration.EnableHealing` |
| 3 | `"Damage"` | `configuration.EnableDamage` |

### Modified: `Configuration.cs`

**Add property:**

```csharp
public OverlayConfig Overlay { get; set; } = new();
```

**Update `ResetToDefaults()`:** Add `Overlay = new OverlayConfig();` alongside the other nested config resets. `Overlay.IsVisible` is intentionally reset (unlike `MainWindowVisible` which is preserved — the overlay default visible state is `true` so resetting is safe).

### Modified: `Plugin.cs`

- Add `private readonly OverlayWindow overlayWindow;` field
- Instantiate after other windows:
  ```csharp
  this.overlayWindow = new OverlayWindow(configuration, SaveConfiguration);
  ```
- Register: `windowSystem.AddWindow(overlayWindow);`
- Set initial visibility: `overlayWindow.IsOpen = configuration.Overlay.IsVisible;`
- Add toggle method alongside the other `Open*UI` methods:
  ```csharp
  private void OpenOverlayUI() => overlayWindow.Toggle();
  ```
- Update `SaveConfiguration()` to sync overlay visibility, consistent with other windows:
  ```csharp
  configuration.Overlay.IsVisible = overlayWindow.IsOpen;
  ```
- Pass `OpenOverlayUI` to `MainWindow` constructor

### Modified: `MainWindow.cs`

- Add `Action openOverlay` parameter to constructor (same pattern as `openSettings`, `openDebug`, etc.)
- Store as `private readonly Action openOverlay;`
- Add `"Overlay"` button alongside existing Settings/Analytics/Training/Debug buttons:
  ```csharp
  if (ImGui.Button("Overlay", new Vector2(-1, 0)))
      openOverlay();
  ```

## Behavior

- Overlay is always rendered by the window system while the plugin is loaded
- Checkboxes take effect immediately (same frame) — no Apply button needed
- Position is set via `FirstUseEver` from `Overlay.X/Y` on first open; subsequent drags are **not** re-persisted (intentional — position is a one-time placement, not tracked across reloads)
- Visibility is persisted via `SaveConfiguration()` in `Plugin.cs` (the same path used by all other windows)

## Non-Goals

- No DoT toggle (rarely needed mid-combat)
- No job-specific toggles
- No lock/unlock mechanism (draggable is enough)
- No combat-only visibility mode
- No localization (consistent with DebugWindow approach)
