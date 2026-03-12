# Quick Toggle Overlay Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a small always-visible floating ImGui overlay with three checkboxes (Rotation, Healing, Damage) that users can click mid-combat without opening MainWindow.

**Architecture:** New `OverlayWindow` follows the existing `HintOverlay` pattern — inherits `Window`, no title bar, draggable, visibility/initial-position persisted in a new `OverlayConfig` config block. `MainWindow` gains an "Overlay" button that toggles it via an `Action` delegate, consistent with all other nav buttons.

**Tech Stack:** C# 12, Dalamud.NET.Sdk 14.0.1, ImGui (Dalamud bindings), xUnit for tests. Build/test via `dotnet build` / `dotnet test` — Claude cannot run these; ask user to verify.

**Spec:** `Docs/superpowers/specs/2026-03-12-quick-toggle-overlay-design.md`

---

## Chunk 1: Config Layer

### Task 1: Add `OverlayConfig`

**Files:**
- Create: `Config/OverlayConfig.cs`
- Test: `Olympus.Tests/Config/OverlayConfigTests.cs`

- [ ] **Step 1: Write the failing test**

  Create `Olympus.Tests/Config/OverlayConfigTests.cs`:

  ```csharp
  namespace Olympus.Tests.Config;

  public class OverlayConfigTests
  {
      [Fact]
      public void DefaultOverlayConfig_IsVisible()
      {
          var config = new Olympus.Config.OverlayConfig();
          Assert.True(config.IsVisible);
      }

      [Fact]
      public void DefaultOverlayConfig_HasNonZeroPosition()
      {
          var config = new Olympus.Config.OverlayConfig();
          Assert.Equal(100f, config.X);
          Assert.Equal(100f, config.Y);
      }
  }
  ```

- [ ] **Step 2: Ask user to run tests and confirm they fail**

  ```
  dotnet test --filter "FullyQualifiedName~OverlayConfigTests"
  ```
  Expected: compile error — `OverlayConfig` not found yet.

- [ ] **Step 3: Create `Config/OverlayConfig.cs`**

  ```csharp
  namespace Olympus.Config;

  public sealed class OverlayConfig
  {
      public bool IsVisible { get; set; } = true;
      public float X { get; set; } = 100f;
      public float Y { get; set; } = 100f;
  }
  ```

- [ ] **Step 4: Ask user to run tests and confirm they pass**

  ```
  dotnet test --filter "FullyQualifiedName~OverlayConfigTests"
  ```
  Expected: 2 tests pass.

- [ ] **Step 5: Commit**

  ```bash
  git add Config/OverlayConfig.cs Olympus.Tests/Config/OverlayConfigTests.cs
  git commit -m "Add OverlayConfig with IsVisible and initial position defaults"
  ```

---

### Task 2: Wire `OverlayConfig` into `Configuration`

**Files:**
- Modify: `Configuration.cs` (add property, update `ResetToDefaults`)
- Modify: `Olympus.Tests/ConfigurationTests.cs` (add two tests)

- [ ] **Step 1: Write failing tests**

  Append to the `#region Default Values` block in `Olympus.Tests/ConfigurationTests.cs`:

  ```csharp
  [Fact]
  public void DefaultConfiguration_HasOverlayVisible()
  {
      var config = new Configuration();
      Assert.True(config.Overlay.IsVisible);
  }
  ```

  Append to the `#region ResetToDefaults - Spell Toggles` block (not the State Preservation region — that is for values that survive a reset):

  ```csharp
  [Fact]
  public void ResetToDefaults_ResetsOverlayConfig()
  {
      var config = new Configuration();
      config.Overlay.IsVisible = false;
      config.Overlay.X = 999f;
      config.Overlay.Y = 888f;

      config.ResetToDefaults();

      Assert.True(config.Overlay.IsVisible);
      Assert.Equal(100f, config.Overlay.X);
      Assert.Equal(100f, config.Overlay.Y);
  }
  ```

- [ ] **Step 2: Ask user to run tests and confirm they fail**

  ```
  dotnet test --filter "FullyQualifiedName~ConfigurationTests"
  ```
  Expected: compile error — `Configuration` has no `Overlay` property yet.

- [ ] **Step 3: Add `Overlay` property to `Configuration.cs`**

  In `Configuration.cs`, add after the `Training` property (line ~78):

  ```csharp
  public OverlayConfig Overlay { get; set; } = new();
  ```

  Add the corresponding `using` if needed — `OverlayConfig` is in `Olympus.Config`, which `Configuration.cs` already imports via `using Olympus.Config;`.

- [ ] **Step 4: Update `ResetToDefaults()` in `Configuration.cs`**

  Inside `ResetToDefaults()`, add alongside the other nested config resets (after `Training = new TrainingConfig();`, line ~157):

  ```csharp
  Overlay = new OverlayConfig();
  ```

  Note: `Overlay.IsVisible` is **not** preserved (unlike `MainWindowVisible`). Resetting to `true` is safe since it's the default state.

- [ ] **Step 5: Ask user to run tests and confirm they pass**

  ```
  dotnet test --filter "FullyQualifiedName~ConfigurationTests"
  ```
  Expected: all existing + 2 new tests pass.

- [ ] **Step 6: Commit**

  ```bash
  git add Configuration.cs Olympus.Tests/ConfigurationTests.cs
  git commit -m "Add Overlay config property and reset support to Configuration"
  ```

---

## Chunk 2: UI Layer

### Task 3: Create `OverlayWindow`

**Files:**
- Create: `Windows/OverlayWindow.cs`

No unit tests — ImGui rendering code cannot be meaningfully unit-tested. Verification is visual (run the plugin).

- [ ] **Step 1: Create `Windows/OverlayWindow.cs`**

  ```csharp
  using System;
  using System.Numerics;
  using Dalamud.Bindings.ImGui;
  using Dalamud.Interface.Windowing;
  using Olympus.Config;

  namespace Olympus.Windows;

  public sealed class OverlayWindow : Window
  {
      private readonly Configuration configuration;
      private readonly Action saveConfiguration;

      private static readonly Vector4 EnabledColor = new(0.4f, 0.8f, 0.4f, 1f);
      private static readonly Vector4 DisabledColor = new(0.5f, 0.5f, 0.5f, 1f);

      public OverlayWindow(Configuration configuration, Action saveConfiguration)
          : base(
              "##OlympusOverlay",
              ImGuiWindowFlags.NoTitleBar
              | ImGuiWindowFlags.NoResize
              | ImGuiWindowFlags.NoScrollbar
              | ImGuiWindowFlags.NoScrollWithMouse
              | ImGuiWindowFlags.NoCollapse
              | ImGuiWindowFlags.AlwaysAutoResize
              | ImGuiWindowFlags.NoFocusOnAppearing
              | ImGuiWindowFlags.NoNav)
      {
          this.configuration = configuration;
          this.saveConfiguration = saveConfiguration;

          Position = new Vector2(configuration.Overlay.X, configuration.Overlay.Y);
          PositionCondition = ImGuiCond.FirstUseEver;
      }

      public override void Draw()
      {
          ImGui.TextDisabled("Olympus");
          ImGui.Separator();

          DrawToggle("Rotation", ref configuration.Enabled);
          DrawToggle("Healing", ref configuration.EnableHealing);
          DrawToggle("Damage", ref configuration.EnableDamage);
      }

      private void DrawToggle(string label, ref bool value)
      {
          ImGui.PushStyleColor(ImGuiCol.Text, value ? EnabledColor : DisabledColor);
          if (ImGui.Checkbox(label, ref value))
              saveConfiguration();
          ImGui.PopStyleColor();
      }
  }
  ```

- [ ] **Step 2: Ask user to run build and confirm it compiles**

  ```
  dotnet build
  ```
  Expected: no errors.

- [ ] **Step 3: Commit**

  ```bash
  git add Windows/OverlayWindow.cs
  git commit -m "Add OverlayWindow with Rotation/Healing/Damage toggle checkboxes"
  ```

---

### Task 4: Wire `OverlayWindow` into `Plugin.cs`

**Files:**
- Modify: `Plugin.cs`

- [ ] **Step 1: Add field declaration**

  In `Plugin.cs`, add alongside the other window fields (around line 110):

  ```csharp
  private readonly OverlayWindow overlayWindow;
  ```

- [ ] **Step 2: Instantiate and register the window**

  After `this.hintOverlay = new HintOverlay(...)` (around line 273), add:

  ```csharp
  this.overlayWindow = new OverlayWindow(configuration, SaveConfiguration);
  ```

  After `windowSystem.AddWindow(hintOverlay);` (around line 293), add:

  ```csharp
  windowSystem.AddWindow(overlayWindow);
  overlayWindow.IsOpen = configuration.Overlay.IsVisible;
  ```

- [ ] **Step 3: Update `SaveConfiguration()`**

  In `SaveConfiguration()` (around line 376), add alongside the other window visibility syncs:

  ```csharp
  configuration.Overlay.IsVisible = overlayWindow.IsOpen;
  ```

  Full method after change:
  ```csharp
  private void SaveConfiguration()
  {
      configuration.MainWindowVisible = mainWindow.IsOpen;
      mainWindow.RespectCloseHotkey = !configuration.PreventEscapeClose;
      pluginInterface.UiBuilder.DisableCutsceneUiHide = configuration.ShowDuringCutscenes;
      configuration.Debug.DebugWindowVisible = debugWindow.IsOpen;
      configuration.Analytics.AnalyticsWindowVisible = analyticsWindow.IsOpen;
      configuration.Training.TrainingWindowVisible = trainingWindow.IsOpen;
      configuration.Overlay.IsVisible = overlayWindow.IsOpen;
      pluginInterface.SavePluginConfig(configuration);
  }
  ```

- [ ] **Step 4: Add `OpenOverlayUI` toggle method**

  Alongside `OpenConfigUI`, `OpenDebugUI`, etc. (around line 399), add:

  ```csharp
  private void OpenOverlayUI() => overlayWindow.Toggle();
  ```

- [ ] **Step 5: Pass `OpenOverlayUI` to `MainWindow` constructor**

  Update the `MainWindow` constructor call (around line 268) to include the new action. The full call becomes:

  ```csharp
  this.mainWindow = new MainWindow(
      configuration,
      SaveConfiguration,
      OpenConfigUI,
      OpenDebugUI,
      OpenAnalyticsUI,
      OpenTrainingUI,
      OpenOverlayUI,
      PluginVersion,
      rotationManager);
  ```

- [ ] **Step 6: Defer commit to after Task 5**

  Do NOT commit yet — `Plugin.cs` passes `OpenOverlayUI` to `MainWindow` but `MainWindow` doesn't accept it yet. The project won't compile until Task 5 is complete. Commit both tasks together in Task 5 Step 6.

---

### Task 5: Update `MainWindow` constructor and add Overlay button

**Files:**
- Modify: `Windows/MainWindow.cs`

- [ ] **Step 1: Add `openOverlay` parameter to constructor**

  Update `MainWindow.cs` constructor signature to include `Action openOverlay` after `openTraining`:

  ```csharp
  public MainWindow(
      Configuration configuration,
      Action saveConfiguration,
      Action openSettings,
      Action openDebug,
      Action openAnalytics,
      Action openTraining,
      Action openOverlay,
      string version,
      RotationManager rotationManager)
      : base($"Olympus v{version}", ImGuiWindowFlags.NoCollapse)
  {
      this.configuration = configuration;
      this.saveConfiguration = saveConfiguration;
      this.openSettings = openSettings;
      this.openDebug = openDebug;
      this.openAnalytics = openAnalytics;
      this.openTraining = openTraining;
      this.openOverlay = openOverlay;
      this.rotationManager = rotationManager;
      // ... rest unchanged
  }
  ```

- [ ] **Step 2: Add field**

  In the field declarations at the top of `MainWindow.cs`, add:

  ```csharp
  private readonly Action openOverlay;
  ```

- [ ] **Step 3: Add Overlay button in `Draw()`**

  In `Draw()`, add the Overlay button after the existing Settings button and before Analytics:

  ```csharp
  if (ImGui.Button("Overlay", new Vector2(-1, 0)))
  {
      openOverlay();
  }
  ```

- [ ] **Step 4: Ask user to run build and confirm it compiles**

  ```
  dotnet build
  ```
  Expected: clean build, no errors.

- [ ] **Step 5: Ask user to run all tests**

  ```
  dotnet test
  ```
  Expected: all existing + new tests pass.

- [ ] **Step 6: Commit Tasks 4 and 5 together**

  ```bash
  git add Plugin.cs Windows/MainWindow.cs
  git commit -m "Wire OverlayWindow into Plugin and add Overlay button to MainWindow"
  ```

---

### Task 6: Version bump, CHANGELOG, and release tag

**Files:**
- Modify: `Olympus.csproj`
- Modify: `Plugin.cs` (`PluginVersion` constant)
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Bump version in `Olympus.csproj`**

  Change `<Version>4.9.8</Version>` to `<Version>4.9.9</Version>`.

- [ ] **Step 2: Bump `PluginVersion` in `Plugin.cs`**

  Change `public const string PluginVersion = "4.9.8";` to `"4.9.9"`.

- [ ] **Step 3: Update `CHANGELOG.md`**

  Move `<!-- LATEST-START -->` and `<!-- LATEST-END -->` markers to wrap only the new v4.9.9 entry. Add the new entry at the top of the changelog body:

  ```markdown
  <!-- LATEST-START -->
  ## v4.9.9

  - Add quick toggle overlay: a compact always-visible floating window with Rotation, Healing, and Damage checkboxes for fast mid-combat control
  <!-- LATEST-END -->
  ```

- [ ] **Step 4: Commit version files**

  ```bash
  git add Olympus.csproj Plugin.cs CHANGELOG.md
  git commit -m "Bump version to 4.9.9, update CHANGELOG"
  ```

- [ ] **Step 5: Push and tag**

  ```bash
  git push origin main
  git tag v4.9.9
  git push origin v4.9.9
  ```

  CI will build the release ZIP, create the GitHub Release, commit updated `repo.json`, and send the Discord notification.
