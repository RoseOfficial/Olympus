using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
using Olympus.Rotation.EchidnaCore.Context;
using Olympus.Windows.Debug;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Viper tab: Echidna-specific debug info including Serpent Offerings, Reawaken, and venom tracking.
/// </summary>
public static class EchidnaTab
{
    public static void Draw(EchidnaDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.ViperNotActive, "Viper rotation not active."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.SwitchToViper, "Switch to Viper to see debug info."));
            return;
        }

        // Gauge Section
        DrawGaugeSection(state);
        ImGui.Spacing();

        // Reawaken Section
        DrawReawakenSection(state);
        ImGui.Spacing();

        // Buffs Section
        DrawBuffSection(state);
        ImGui.Spacing();

        // Venom Section
        DrawVenomSection(state);
        ImGui.Spacing();

        // Positional Section
        DrawPositionalSection(state);
    }

    private static void DrawGaugeSection(EchidnaDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Gauge, "Gauge"), "VprGaugeTable", () =>
        {
            // Serpent Offering
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SerpentOffering, "Serpent Offering:"));
            ImGui.TableNextColumn();
            var offeringPercent = state.SerpentOffering / 100f;
            ImGui.ProgressBar(offeringPercent, new Vector2(-1, 0), $"{state.SerpentOffering}/100");

            // Rattling Coils
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.RattlingCoils, "Rattling Coils:"));
            ImGui.TableNextColumn();
            var coilColor = state.RattlingCoils >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(coilColor, $"{state.RattlingCoils}/3");

            // Combo
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Combo, "Combo:"));
            ImGui.TableNextColumn();
            var comboColor = state.ComboStep > 0 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(comboColor, state.ComboStep > 0 ? $"Step {state.ComboStep}" : Loc.T(LocalizedStrings.Debug.NoneLabel, "None"));

            // Dread Combo
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.DreadCombo, "Dread Combo:"));
            ImGui.TableNextColumn();
            var dreadColor = state.DreadCombo != 0 ? new Vector4(0.8f, 0.5f, 1f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(dreadColor, state.DreadCombo.ToString());
        }, 140f);
    }

    private static void DrawReawakenSection(EchidnaDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Reawaken, "Reawaken"), "VprReawakenTable", () =>
        {
            // Reawaken State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Reawaken, "Reawaken:"));
            ImGui.TableNextColumn();
            var reawakenColor = state.IsReawakened ? new Vector4(0.5f, 1f, 0.8f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(reawakenColor, state.GetReawakenState());

            // Anguine Tribute
            if (state.IsReawakened)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(Loc.T(LocalizedStrings.Debug.AnguineTribute, "Anguine Tribute:"));
                ImGui.TableNextColumn();
                var tributeColor = state.AnguineTribute > 0 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
                ImGui.TextColored(tributeColor, $"{state.AnguineTribute}");
            }

            // Ready to Reawaken
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ReadyToReawaken, "Ready to Reawaken:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasReadyToReawaken);
        }, 140f);
    }

    private static void DrawBuffSection(EchidnaDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Buffs, "Buffs"), "VprBuffTable", () =>
        {
            // Hunter's Instinct
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.HuntersInstinct, "Hunter's Instinct:"));
            ImGui.TableNextColumn();
            if (state.HasHuntersInstinct)
            {
                var color = state.HuntersInstinctRemaining < 5f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.HuntersInstinctRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            // Swiftscaled
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Swiftscaled, "Swiftscaled:"));
            ImGui.TableNextColumn();
            if (state.HasSwiftscaled)
            {
                var color = state.SwiftscaledRemaining < 5f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.SwiftscaledRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            // Noxious Gnash
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.NoxiousGnash, "Noxious Gnash:"));
            ImGui.TableNextColumn();
            if (state.HasNoxiousGnash)
            {
                var color = state.NoxiousGnashRemaining < 6f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.NoxiousGnashRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.NotApplied, "Not applied"));
            }

            // Honed Steel/Reavers
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.HonedSteel, "Honed Steel:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasHonedSteel);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.HonedReavers, "Honed Reavers:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasHonedReavers);
        }, 140f);
    }

    private static void DrawVenomSection(EchidnaDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Venom, "Venom"), "VprVenomTable", () =>
        {
            // Current Venom
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ActiveVenom, "Active Venom:"));
            ImGui.TableNextColumn();
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.8f, 1f), state.GetVenomState());

            // Twinfang/Twinblood Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TwinfangReady, "Twinfang Ready:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasPoisedForTwinfang);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TwinbloodReady, "Twinblood Ready:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasPoisedForTwinblood);
        }, 140f);
    }

    private static void DrawPositionalSection(EchidnaDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Positional, "Positional"), "VprPositionalTable", () =>
        {
            // Current Position
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Position, "Position:"));
            ImGui.TableNextColumn();
            if (state.TargetHasPositionalImmunity)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), Loc.T(LocalizedStrings.Debug.ImmuneOmni, "Immune (omni)"));
            }
            else if (state.IsAtRear)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Rear, "Rear"));
            }
            else if (state.IsAtFlank)
            {
                ImGui.TextColored(new Vector4(1f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Flank, "Flank"));
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Front, "Front"));
            }

            // True North
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TrueNorth, "True North:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasTrueNorth);

            // Target
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TargetLabel, "Target:"));
            ImGui.TableNextColumn();
            ImGui.Text(string.IsNullOrEmpty(state.CurrentTarget) ? Loc.T(LocalizedStrings.Debug.NoneLabel, "None") : state.CurrentTarget);

            // Nearby Enemies
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.NearbyEnemies, "Nearby Enemies:"));
            ImGui.TableNextColumn();
            var aoeColor = state.NearbyEnemies >= 3 ? new Vector4(1f, 0.6f, 0.2f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(aoeColor, $"{state.NearbyEnemies}");
        }, 140f);
    }

    private static void DrawProcStatus(bool hasProc)
    {
        if (hasProc)
        {
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Ready, "Ready"));
        }
        else
        {
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.No, "No"));
        }
    }
}
