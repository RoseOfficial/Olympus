using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Windows;

namespace Olympus.Windows.Config.Shared;

/// <summary>
/// Config section for Draw Helper — world-space visual overlays.
/// </summary>
public sealed class DrawHelperSection
{
    private readonly Configuration config;
    private readonly Action save;

    public DrawHelperSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        var dh = config.DrawHelper;

        // Master toggle
        ImGui.Separator();
        ImGui.Text("Draw Helper");
        var drawingEnabled = dh.DrawingEnabled;
        if (ImGui.Checkbox("Enable Drawing", ref drawingEnabled)) { dh.DrawingEnabled = drawingEnabled; save(); }

        if (!dh.DrawingEnabled)
        {
            ImGui.TextDisabled("Enable drawing to configure options below.");
            return;
        }

        ImGui.Spacing();

        // Pictomancy backend
        ImGui.Separator();
        ImGui.Text("Rendering");
        var usePicto = dh.UsePictomancy;
        if (ImGui.Checkbox("Use Pictomancy (3D rendering)", ref usePicto)) { dh.UsePictomancy = usePicto; save(); }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Requires Pictomancy plugin. Falls back gracefully if unavailable.");

        var alpha = dh.PictomancyMaxAlpha;
        if (ImGui.SliderFloat("Max Alpha", ref alpha, 0.1f, 1f, "%.2f")) { dh.PictomancyMaxAlpha = alpha; save(); }

        var clipUi = dh.PictomancyClipNativeUI;
        if (ImGui.Checkbox("Clip to game UI", ref clipUi)) { dh.PictomancyClipNativeUI = clipUi; save(); }

        ImGui.Spacing();

        // Enemy hitboxes
        ImGui.Separator();
        ImGui.Text("Enemy Hitboxes");
        var showHitboxes = dh.ShowEnemyHitboxes;
        if (ImGui.Checkbox("Show enemy hitboxes", ref showHitboxes)) { dh.ShowEnemyHitboxes = showHitboxes; save(); }
        if (dh.ShowEnemyHitboxes)
            ColorPicker("Hitbox Color", dh.EnemyHitboxColor, v => { dh.EnemyHitboxColor = v; save(); });

        ImGui.Spacing();

        // Melee range
        ImGui.Separator();
        ImGui.Text("Melee Range");
        var showMelee = dh.ShowMeleeRange;
        if (ImGui.Checkbox("Show melee range at target", ref showMelee)) { dh.ShowMeleeRange = showMelee; save(); }
        if (dh.ShowMeleeRange)
        {
            var fade = dh.MeleeRangeFade;
            if (ImGui.Checkbox("Fade when in range", ref fade)) { dh.MeleeRangeFade = fade; save(); }
            ColorPicker("In Range", dh.MeleeRangeColor, v => { dh.MeleeRangeColor = v; save(); });
            ColorPicker("Out of Range", dh.MeleeRangeOutOfRangeColor, v => { dh.MeleeRangeOutOfRangeColor = v; save(); });
        }

        ImGui.Spacing();

        // Ranged range
        ImGui.Separator();
        ImGui.Text("Ranged Range");
        var showRanged = dh.ShowRangedRange;
        if (ImGui.Checkbox("Show ranged range at target", ref showRanged)) { dh.ShowRangedRange = showRanged; save(); }
        if (dh.ShowRangedRange)
        {
            ImGui.TextDisabled("Auto-detects 25y range for all ranged/caster jobs.");
            ColorPicker("In Range##ranged", dh.RangedRangeColor, v => { dh.RangedRangeColor = v; save(); });
            ColorPicker("Out of Range##ranged", dh.RangedRangeOutOfRangeColor, v => { dh.RangedRangeOutOfRangeColor = v; save(); });
        }

        ImGui.Spacing();

        // Positionals
        ImGui.Separator();
        ImGui.Text("Positionals");
        var showPos = dh.ShowPositionals;
        if (ImGui.Checkbox("Show positional zones at target", ref showPos)) { dh.ShowPositionals = showPos; save(); }
        if (dh.ShowPositionals)
        {
            ColorPicker("Rear", dh.PositionalRearColor, v => { dh.PositionalRearColor = v; save(); });
            ColorPicker("Flank", dh.PositionalFlankColor, v => { dh.PositionalFlankColor = v; save(); });
        }

    }

    private static void ColorPicker(string label, uint currentColor, Action<uint> setter)
    {
        var c = ImGui.ColorConvertU32ToFloat4(currentColor);
        if (ImGui.ColorEdit4(label, ref c, ImGuiColorEditFlags.AlphaBar))
            setter(ImGui.ColorConvertFloat4ToU32(c));
    }
}
