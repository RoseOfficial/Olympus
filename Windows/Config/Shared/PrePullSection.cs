using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;

namespace Olympus.Windows.Config.Shared;

/// <summary>
/// Renders the Pre-Pull config section: master toggle for countdown-keyed
/// ability automation. Individual per-job behaviors remain gated by their
/// own per-job toggles when this master is enabled.
/// </summary>
public sealed class PrePullSection
{
    private readonly Configuration config;
    private readonly Action save;

    public PrePullSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f),
            Loc.T(LocalizedStrings.PrePull.PrePullHeader, "Pre-Pull Actions"));
        ImGui.Separator();

        ConfigUIHelpers.Toggle(
            Loc.T(LocalizedStrings.PrePull.EnablePrePullActions, "Enable pre-pull ability automation"),
            () => config.PrePull.EnablePrePullActions,
            v => config.PrePull.EnablePrePullActions = v,
            Loc.T(LocalizedStrings.PrePull.EnablePrePullActionsDesc,
                "When enabled and a party countdown is active, Olympus prepares opener abilities " +
                "timed to the countdown: pre-cast GCDs (Fire III, Ruin III, Verthunder III, " +
                "Rainbow Drip, healer damage spells), oGCD preps (Regen, Benison, Recitation, " +
                "Reassemble, Meikyo Shisui), and sequence starters (Suiton, Soulsow, Form Shift). " +
                "A running countdown is the player's explicit pull signal. Per-job toggles are " +
                "still respected. Disable to handle pre-pull preparation manually."),
            save);
    }
}
