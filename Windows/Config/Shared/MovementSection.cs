using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Olympus.Localization;
using Olympus.Services.Movement;

namespace Olympus.Windows.Config.Shared;

/// <summary>
/// Renders the Movement config section: trash AoE avoidance + auto-interact, plus a status banner
/// when the RMIWalk hook failed to install.
/// </summary>
public sealed class MovementSection
{
    private readonly Configuration config;
    private readonly Action save;
    private readonly IRMIWalkHookService hook;

    public MovementSection(Configuration config, Action save, IRMIWalkHookService hook)
    {
        this.config = config;
        this.save = save;
        this.hook = hook;
    }

    public void Draw()
    {
        ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f),
            Loc.T(LocalizedStrings.Movement.MovementHeader, "Movement"));
        ImGui.Separator();

        if (!hook.HookInstalled)
        {
            ImGui.TextColored(new Vector4(0.9f, 0.3f, 0.3f, 1f),
                Loc.T(LocalizedStrings.Movement.HookUnavailableBanner,
                    "Movement hook unavailable for this game version. AoE avoidance disabled."));
            ImGui.Spacing();
        }

        DrawTrashAvoidance();
        ImGui.Spacing();
        DrawAutoInteract();
    }

    private void DrawTrashAvoidance()
    {
        if (!ConfigUIHelpers.SectionHeader(
                Loc.T(LocalizedStrings.Movement.EnableTrashAoEAvoidance, "Trash AoE avoidance"),
                "MovementTrashAoE"))
            return;

        ConfigUIHelpers.Toggle(
            Loc.T(LocalizedStrings.Movement.EnableTrashAoEAvoidance, "Enable trash AoE avoidance"),
            () => config.Movement.EnableTrashAoEAvoidance,
            v => config.Movement.EnableTrashAoEAvoidance = v,
            Loc.T(LocalizedStrings.Movement.EnableTrashAoEAvoidanceDesc,
                "Trash mobs only. Suspended during boss fights and high-end content (savage, ultimate, extreme, criterion, chaotic). Default off."),
            save);

        if (!config.Movement.EnableTrashAoEAvoidance)
            return;

        ConfigUIHelpers.BeginIndent();

        // Behavior subgroup
        ImGui.TextDisabled(Loc.T(LocalizedStrings.Movement.ReactionDelayLabel, "Reaction delay (ms)"));
        config.Movement.ReactionDelayMinMs = ConfigUIHelpers.IntSlider(
            Loc.T("movement.reaction_delay_min", "Min##rd"),
            config.Movement.ReactionDelayMinMs,
            50,
            config.Movement.ReactionDelayMaxMs,
            null,
            save,
            v => config.Movement.ReactionDelayMinMs = v);
        config.Movement.ReactionDelayMaxMs = ConfigUIHelpers.IntSlider(
            Loc.T("movement.reaction_delay_max", "Max##rd"),
            config.Movement.ReactionDelayMaxMs,
            config.Movement.ReactionDelayMinMs,
            2000,
            null,
            save,
            v => config.Movement.ReactionDelayMaxMs = v);

        ImGui.TextDisabled(Loc.T(LocalizedStrings.Movement.ArrivalToleranceLabel, "Arrival tolerance (yalms)"));
        config.Movement.ArrivalToleranceMinYalms = ConfigUIHelpers.FloatSlider(
            Loc.T("movement.arrival_tol_min", "Min##at"),
            config.Movement.ArrivalToleranceMinYalms,
            0f,
            config.Movement.ArrivalToleranceMaxYalms,
            "%.2f",
            null,
            save,
            v => config.Movement.ArrivalToleranceMinYalms = v);
        config.Movement.ArrivalToleranceMaxYalms = ConfigUIHelpers.FloatSlider(
            Loc.T("movement.arrival_tol_max", "Max##at"),
            config.Movement.ArrivalToleranceMaxYalms,
            config.Movement.ArrivalToleranceMinYalms,
            5f,
            "%.2f",
            null,
            save,
            v => config.Movement.ArrivalToleranceMaxYalms = v);

        ImGui.TextDisabled(Loc.T(LocalizedStrings.Movement.InterCastPauseLabel, "Inter-cast pause (ms)"));
        config.Movement.InterCastPauseMinMs = ConfigUIHelpers.IntSlider(
            Loc.T("movement.intercast_min", "Min##icp"),
            config.Movement.InterCastPauseMinMs,
            0,
            config.Movement.InterCastPauseMaxMs,
            null,
            save,
            v => config.Movement.InterCastPauseMinMs = v);
        config.Movement.InterCastPauseMaxMs = ConfigUIHelpers.IntSlider(
            Loc.T("movement.intercast_max", "Max##icp"),
            config.Movement.InterCastPauseMaxMs,
            config.Movement.InterCastPauseMinMs,
            2000,
            null,
            save,
            v => config.Movement.InterCastPauseMaxMs = v);

        config.Movement.DirectionalNoiseDegrees = ConfigUIHelpers.FloatSlider(
            Loc.T(LocalizedStrings.Movement.DirectionalNoiseLabel, "Directional noise (deg)"),
            config.Movement.DirectionalNoiseDegrees,
            0f, 30f, "%.1f",
            null,
            save,
            v => config.Movement.DirectionalNoiseDegrees = v);

        config.Movement.WalkVsSprintThresholdSeconds = ConfigUIHelpers.FloatSlider(
            Loc.T(LocalizedStrings.Movement.WalkVsSprintLabel, "Walk vs sprint threshold (s)"),
            config.Movement.WalkVsSprintThresholdSeconds,
            0.1f, 5f, "%.1f",
            null,
            save,
            v => config.Movement.WalkVsSprintThresholdSeconds = v);

        // Detection subgroup
        config.Movement.MaxThreatRangeYalms = ConfigUIHelpers.FloatSlider(
            Loc.T(LocalizedStrings.Movement.MaxThreatRangeLabel, "Max threat range (yalms)"),
            config.Movement.MaxThreatRangeYalms,
            5f, 60f, "%.1f",
            null,
            save,
            v => config.Movement.MaxThreatRangeYalms = v);

        // Advanced: raycast budget
        if (ConfigUIHelpers.SectionHeader(
                Loc.T("movement.advanced", "Advanced (detection)"),
                "MovementAdvanced",
                false))
        {
            ConfigUIHelpers.BeginIndent();

            config.Movement.RaycastBudgetPerFrame = ConfigUIHelpers.IntSlider(
                Loc.T("movement.raycast_budget", "Raycast budget/frame"),
                config.Movement.RaycastBudgetPerFrame,
                4, 128,
                null,
                save,
                v => config.Movement.RaycastBudgetPerFrame = v);

            ConfigUIHelpers.EndIndent();
        }

        // Boss ranks inline checkboxes
        ImGui.TextDisabled(Loc.T(LocalizedStrings.Movement.BossRanksLabel, "Boss-class ranks (skip avoidance)"));
        for (byte i = 1; i <= 8; i++)
        {
            var rank = i; // capture for closure
            var present = config.Movement.BossRanks.Contains(rank);
            // HighlightedCheckbox modifies present via ref and calls the save lambda when changed.
            if (ImGui.Checkbox($"Rank {rank}##bossrank{rank}", ref present))
            {
                if (present) config.Movement.BossRanks.Add(rank);
                else config.Movement.BossRanks.Remove(rank);
                save();
            }
            if (i < 8) ImGui.SameLine();
        }

        ConfigUIHelpers.EndIndent();
    }

    private void DrawAutoInteract()
    {
        if (!ConfigUIHelpers.SectionHeader(
                Loc.T(LocalizedStrings.Movement.EnableAutoInteract, "Auto-interact"),
                "MovementAutoInteract"))
            return;

        ConfigUIHelpers.Toggle(
            Loc.T(LocalizedStrings.Movement.EnableAutoInteract, "Enable auto-interact"),
            () => config.Movement.EnableAutoInteract,
            v => config.Movement.EnableAutoInteract = v,
            Loc.T(LocalizedStrings.Movement.EnableAutoInteractDesc,
                "Walks into a treasure coffer (or other allowed object kinds) and auto-opens it. Default off."),
            save);

        if (!config.Movement.EnableAutoInteract)
            return;

        ConfigUIHelpers.BeginIndent();

        ImGui.TextDisabled(Loc.T(LocalizedStrings.Movement.InteractKindsLabel, "Object kinds"));
        DrawKindCheckbox("Treasure", ObjectKind.Treasure);
        ImGui.SameLine();
        DrawKindCheckbox("EventObj", ObjectKind.EventObj);
        ImGui.SameLine();
        DrawKindCheckbox("EventNpc", ObjectKind.EventNpc);
        ImGui.SameLine();
        DrawKindCheckbox("GatheringPoint", ObjectKind.GatheringPoint);

        config.Movement.InteractRangeYalms = ConfigUIHelpers.FloatSlider(
            Loc.T(LocalizedStrings.Movement.InteractRangeLabel, "Range (yalms)"),
            config.Movement.InteractRangeYalms,
            1f, 6f, "%.1f",
            null,
            save,
            v => config.Movement.InteractRangeYalms = v);

        config.Movement.InteractCooldownSeconds = ConfigUIHelpers.FloatSlider(
            Loc.T(LocalizedStrings.Movement.InteractCooldownLabel, "Per-object cooldown (s)"),
            config.Movement.InteractCooldownSeconds,
            0.5f, 5f, "%.1f",
            null,
            save,
            v => config.Movement.InteractCooldownSeconds = v);

        ConfigUIHelpers.Toggle(
            Loc.T(LocalizedStrings.Movement.InteractInCombatLabel, "Allow in combat"),
            () => config.Movement.InteractInCombat,
            v => config.Movement.InteractInCombat = v,
            null,
            save);

        ConfigUIHelpers.EndIndent();
    }

    private void DrawKindCheckbox(string label, ObjectKind kind)
    {
        var present = config.Movement.InteractAllowedKinds.Contains(kind);
        // ImGui.Checkbox writes the new value into present via ref.
        if (ImGui.Checkbox($"{label}##kind{(int)kind}", ref present))
        {
            if (present) config.Movement.InteractAllowedKinds.Add(kind);
            else config.Movement.InteractAllowedKinds.Remove(kind);
            save();
        }
    }
}
