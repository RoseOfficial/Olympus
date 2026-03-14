using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using Olympus.Services.Targeting;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Smart AoE debug tab: AoE Targeting Lab + prediction accuracy tracking.
/// </summary>
public sealed class SmartAoETab
{
    private readonly AoETracker _tracker;
    private readonly DrawCanvas _canvas;
    private readonly IObjectTable _objectTable;

    private float _addFakeRadius = 2f;
    private string _addFakeName = "Fake Enemy";

    public SmartAoETab(AoETracker tracker, DrawCanvas canvas, IObjectTable objectTable)
    {
        _tracker = tracker;
        _canvas = canvas;
        _objectTable = objectTable;
    }

    public void Draw(Configuration config)
    {
        // ── AoE Targeting Lab ──
        ImGui.Separator();
        ImGui.Text("AoE Targeting Lab");

        ImGui.Checkbox("Enable Test Mode", ref _canvas.TestModeEnabled);
        if (_canvas.TestModeEnabled)
        {
            ImGui.Checkbox("Use real player position", ref _canvas.UseRealPlayer);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Off = mouse cursor projected to ground");

            // Shape selector
            var isCone = _canvas.ShapeMode == DrawCanvas.TestShapeMode.Cone;
            if (ImGui.RadioButton("Cone", isCone)) _canvas.ShapeMode = DrawCanvas.TestShapeMode.Cone;
            ImGui.SameLine();
            if (ImGui.RadioButton("Rect/Line", !isCone)) _canvas.ShapeMode = DrawCanvas.TestShapeMode.Rect;

            if (_canvas.ShapeMode == DrawCanvas.TestShapeMode.Cone)
            {
                ImGui.SliderFloat("Cone Angle (deg)", ref _canvas.ConeAngle, 10f, 360f, "%.0f");
                ImGui.SliderFloat("Cone Range (y)", ref _canvas.ConeRange, 1f, 30f, "%.1f");
            }
            else
            {
                ImGui.SliderFloat("Rect Width (y)", ref _canvas.RectWidth, 1f, 20f, "%.1f");
                ImGui.SliderFloat("Rect Length (y)", ref _canvas.RectLength, 1f, 40f, "%.1f");
            }

            ImGui.SliderFloat("Sim Player Hitbox", ref _canvas.SimHitboxRadius, 0.1f, 2f, "%.2f");

            // Fake enemies
            ImGui.Spacing();
            ImGui.Text("Fake Enemies:");
            ImGui.InputText("Name##fake", ref _addFakeName, 64);
            ImGui.SliderFloat("Hitbox##fake", ref _addFakeRadius, 0.5f, 10f, "%.1f");

            var player = _objectTable.LocalPlayer;
            if (ImGui.Button("Place at Player") && player != null)
            {
                _canvas.FakeEnemies.Add(new DrawCanvas.FakeEnemy
                {
                    Position = player.Position,
                    Radius = _addFakeRadius,
                    Name = _addFakeName,
                });
            }
            ImGui.SameLine();
            if (_canvas.FakeEnemies.Count > 0 && ImGui.Button("Clear All"))
                _canvas.FakeEnemies.Clear();

            for (var i = 0; i < _canvas.FakeEnemies.Count; i++)
            {
                var fake = _canvas.FakeEnemies[i];
                ImGui.PushID(i);
                if (ImGui.SmallButton("X"))
                {
                    _canvas.FakeEnemies.RemoveAt(i);
                    ImGui.PopID();
                    break;
                }
                ImGui.SameLine();
                ImGui.Text($"{fake.Name} r={fake.Radius:F1} ({fake.Position.X:F0},{fake.Position.Z:F0})");
                ImGui.PopID();
            }
        }

        ImGui.Spacing();

        // ── AoE State ──
        if (IsSectionVisible(config, "SmartAoEState"))
        {
            ImGui.Separator();
            ImGui.Text("AoE State");
            if (_tracker.LastResult.HasValue)
            {
                var r = _tracker.LastResult.Value;
                ImGui.Text($"Shape: {r.Shape}  Hits: {r.HitCount}  Angle: {r.OptimalAngle * 180f / System.MathF.PI:F1}\u00b0");
                ImGui.Text($"Position: {_tracker.LastPlayerPosition.X:F1}, {_tracker.LastPlayerPosition.Z:F1}");
            }
            else
            {
                ImGui.TextDisabled("No AoE computation yet");
            }
            ImGui.Spacing();
        }

        // ── Prediction Accuracy ──
        if (IsSectionVisible(config, "SmartAoEAccuracy"))
        {
            ImGui.Separator();
            ImGui.Text("Prediction Accuracy");
            ImGui.Text($"Total: {_tracker.TotalPredictions}  Correct: {_tracker.TotalCorrect}  Rate: {_tracker.AccuracyRate:F1}%%");

            if (_tracker.History.Count > 0 &&
                ImGui.BeginTable("AoEHistory", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
                    new Vector2(0, 200)))
            {
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 60f);
                ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Shape", ImGuiTableColumnFlags.WidthFixed, 50f);
                ImGui.TableSetupColumn("Pred", ImGuiTableColumnFlags.WidthFixed, 40f);
                ImGui.TableSetupColumn("Actual", ImGuiTableColumnFlags.WidthFixed, 45f);
                ImGui.TableSetupColumn("OK", ImGuiTableColumnFlags.WidthFixed, 25f);
                ImGui.TableHeadersRow();

                for (var i = _tracker.History.Count - 1; i >= 0; i--)
                {
                    var h = _tracker.History[i];
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn(); ImGui.Text(h.Timestamp.ToString("HH:mm:ss"));
                    ImGui.TableNextColumn(); ImGui.Text(h.ActionName);
                    ImGui.TableNextColumn(); ImGui.Text(h.Shape.ToString());
                    ImGui.TableNextColumn(); ImGui.Text(h.PredictedHits.ToString());
                    ImGui.TableNextColumn(); ImGui.Text(h.Resolved ? h.ActualHits.ToString() : "...");
                    ImGui.TableNextColumn();
                    if (h.Resolved)
                    {
                        var ok = h.PredictedHits == h.ActualHits;
                        ImGui.TextColored(ok ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0.3f, 0.3f, 1), ok ? "Y" : "N");
                    }
                }

                ImGui.EndTable();
            }

            if (ImGui.SmallButton("Reset"))
                _tracker.Reset();
        }
    }

    private static bool IsSectionVisible(Configuration config, string key)
    {
        if (config.Debug.DebugSectionVisibility.TryGetValue(key, out var visible))
            return visible;
        return true;
    }
}
