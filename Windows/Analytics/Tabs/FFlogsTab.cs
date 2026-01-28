using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Localization;
using Olympus.Services.FFLogs;

namespace Olympus.Windows.Analytics.Tabs;

/// <summary>
/// FFLogs tab: displays FFLogs rankings and comparisons.
/// </summary>
public static class FFlogsTab
{
    // Colors
    private static readonly Vector4 GoodColor = new(0.3f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 WarningColor = new(0.9f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 BadColor = new(0.9f, 0.3f, 0.3f, 1.0f);
    private static readonly Vector4 NeutralColor = new(0.7f, 0.7f, 0.7f, 1.0f);
    private static readonly Vector4 InfoColor = new(0.4f, 0.7f, 1.0f, 1.0f);
    private static readonly Vector4 HeaderColor = new(1.0f, 0.8f, 0.4f, 1.0f);

    // State for async operations
    private static bool isLoading;
    private static string? statusMessage;
    private static FFlogsZoneRanking? currentRankings;
    private static DateTime lastRefresh = DateTime.MinValue;

    // Input buffers for setup
    private static string clientIdInput = "";
    private static string clientSecretInput = "";
    private static string characterNameInput = "";
    private static string serverSlugInput = "";
    private static int regionIndex;
    private static readonly string[] Regions = { "NA", "EU", "JP", "OCE" };

    public static void Draw(IFFlogsService? fflogsService, FFlogsConfig config)
    {
        if (fflogsService == null)
        {
            ImGui.TextColored(WarningColor, Loc.T(LocalizedStrings.Analytics.FFlogsServiceNotInit, "FFLogs service not initialized."));
            return;
        }

        // Sync input buffers with config on first draw
        if (string.IsNullOrEmpty(clientIdInput) && !string.IsNullOrEmpty(config.ClientId))
        {
            clientIdInput = config.ClientId;
            clientSecretInput = config.ClientSecret;
            characterNameInput = config.CharacterName;
            serverSlugInput = config.ServerSlug;
            regionIndex = Array.IndexOf(Regions, config.Region);
            if (regionIndex < 0) regionIndex = 0;
        }

        // Show setup wizard if not configured
        if (!config.IsConfigured)
        {
            DrawSetupWizard(fflogsService, config);
            return;
        }

        // Show character binding if not configured
        if (!config.HasCharacterBinding)
        {
            DrawCharacterBinding(fflogsService, config);
            return;
        }

        // Main FFLogs display
        DrawFFlogsData(fflogsService, config);
    }

    private static void DrawSetupWizard(IFFlogsService fflogsService, FFlogsConfig config)
    {
        ImGui.TextColored(HeaderColor, Loc.T(LocalizedStrings.Analytics.FFlogsSetup, "FFLogs Setup"));
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextWrapped(Loc.T(LocalizedStrings.Analytics.FFlogsIntro, "To use FFLogs integration, you need to create an API client on FFLogs."));
        ImGui.Spacing();

        ImGui.TextColored(InfoColor, Loc.T(LocalizedStrings.Analytics.FFlogsStep1, "Step 1: Create FFLogs API Client"));
        ImGui.BulletText(Loc.T(LocalizedStrings.Analytics.FFlogsStep1Url, "Go to: https://www.fflogs.com/api/clients/"));
        ImGui.BulletText(Loc.T(LocalizedStrings.Analytics.FFlogsStep1Create, "Click 'Create Client'"));
        ImGui.BulletText(Loc.T(LocalizedStrings.Analytics.FFlogsStep1Name, "Set any name (e.g., 'Olympus Plugin')"));
        ImGui.BulletText(Loc.T(LocalizedStrings.Analytics.FFlogsStep1Redirect, "Set redirect URL to: http://localhost"));
        ImGui.BulletText(Loc.T(LocalizedStrings.Analytics.FFlogsStep1Public, "Check 'Public Client' if available"));
        ImGui.Spacing();

        ImGui.TextColored(InfoColor, Loc.T(LocalizedStrings.Analytics.FFlogsStep2, "Step 2: Enter Credentials"));
        ImGui.Spacing();

        ImGui.SetNextItemWidth(300);
        ImGui.InputText(Loc.T(LocalizedStrings.Analytics.ClientId, "Client ID"), ref clientIdInput, 256);

        ImGui.SetNextItemWidth(300);
        ImGui.InputText(Loc.T(LocalizedStrings.Analytics.ClientSecret, "Client Secret"), ref clientSecretInput, 256, ImGuiInputTextFlags.Password);

        ImGui.Spacing();

        if (ImGui.Button(Loc.T(LocalizedStrings.Analytics.SaveCredentials, "Save Credentials")))
        {
            config.ClientId = clientIdInput.Trim();
            config.ClientSecret = clientSecretInput.Trim();
            statusMessage = Loc.T(LocalizedStrings.Analytics.CredentialsSaved, "Credentials saved. Testing connection...");
            _ = TestConnectionAsync(fflogsService);
        }

        if (!string.IsNullOrEmpty(statusMessage))
        {
            ImGui.Spacing();
            var color = statusMessage.Contains("success", StringComparison.OrdinalIgnoreCase) ? GoodColor : WarningColor;
            ImGui.TextColored(color, statusMessage);
        }

        if (!string.IsNullOrEmpty(fflogsService.LastError))
        {
            ImGui.Spacing();
            ImGui.TextColored(BadColor, Loc.TFormat(LocalizedStrings.Analytics.ErrorPrefix, "Error: {0}", fflogsService.LastError));
        }
    }

    private static void DrawCharacterBinding(IFFlogsService fflogsService, FFlogsConfig config)
    {
        ImGui.TextColored(HeaderColor, Loc.T(LocalizedStrings.Analytics.CharacterBinding, "Character Binding"));
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextWrapped(Loc.T(LocalizedStrings.Analytics.CharacterBindingIntro, "Enter your character information to view FFLogs rankings."));
        ImGui.Spacing();

        ImGui.SetNextItemWidth(200);
        ImGui.InputText(Loc.T(LocalizedStrings.Analytics.CharacterName, "Character Name"), ref characterNameInput, 64);

        ImGui.SetNextItemWidth(200);
        ImGui.InputText(Loc.T(LocalizedStrings.Analytics.Server, "Server (e.g., gilgamesh)"), ref serverSlugInput, 64);

        ImGui.SetNextItemWidth(100);
        ImGui.Combo(Loc.T(LocalizedStrings.Analytics.Region, "Region"), ref regionIndex, Regions, Regions.Length);

        ImGui.Spacing();

        var canBind = !string.IsNullOrWhiteSpace(characterNameInput) && !string.IsNullOrWhiteSpace(serverSlugInput);
        if (!canBind)
            ImGui.BeginDisabled();

        if (ImGui.Button(Loc.T(LocalizedStrings.Analytics.BindCharacter, "Bind Character")))
        {
            config.CharacterName = characterNameInput.Trim();
            config.ServerSlug = serverSlugInput.Trim().ToLowerInvariant();
            config.Region = Regions[regionIndex];
            statusMessage = Loc.T(LocalizedStrings.Analytics.LookingUpCharacter, "Looking up character...");
            _ = LookupCharacterAsync(fflogsService, config);
        }

        if (!canBind)
            ImGui.EndDisabled();

        ImGui.SameLine();

        if (ImGui.Button(Loc.T(LocalizedStrings.Analytics.ChangeCredentials, "Change Credentials")))
        {
            config.ClientId = "";
            config.ClientSecret = "";
            clientIdInput = "";
            clientSecretInput = "";
        }

        if (isLoading)
        {
            ImGui.Spacing();
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Analytics.Loading, "Loading..."));
        }

        if (!string.IsNullOrEmpty(statusMessage))
        {
            ImGui.Spacing();
            var color = statusMessage.Contains("success", StringComparison.OrdinalIgnoreCase) || statusMessage.Contains("found", StringComparison.OrdinalIgnoreCase) ? GoodColor : WarningColor;
            ImGui.TextColored(color, statusMessage);
        }

        if (!string.IsNullOrEmpty(fflogsService.LastError))
        {
            ImGui.Spacing();
            ImGui.TextColored(BadColor, Loc.TFormat(LocalizedStrings.Analytics.ErrorPrefix, "Error: {0}", fflogsService.LastError));
        }
    }

    private static void DrawFFlogsData(IFFlogsService fflogsService, FFlogsConfig config)
    {
        // Header with character info and status
        DrawHeader(fflogsService, config);
        ImGui.Separator();
        ImGui.Spacing();

        // Auto-refresh if stale
        if (currentRankings == null && !isLoading && (DateTime.Now - lastRefresh).TotalMinutes > 1)
        {
            _ = RefreshRankingsAsync(fflogsService, config);
        }

        if (isLoading)
        {
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Analytics.LoadingRankings, "Loading rankings..."));
            return;
        }

        if (currentRankings == null)
        {
            if (!string.IsNullOrEmpty(fflogsService.LastError))
            {
                ImGui.TextColored(BadColor, Loc.TFormat(LocalizedStrings.Analytics.ErrorPrefix, "Error: {0}", fflogsService.LastError));
            }
            else
            {
                ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Analytics.NoRankingsData, "No rankings data available. Click Refresh to load."));
            }
            return;
        }

        // All Stars summary
        DrawAllStarsSummary(currentRankings);
        ImGui.Spacing();

        // Encounter rankings table
        DrawEncounterRankings(currentRankings);
    }

    private static void DrawHeader(IFFlogsService fflogsService, FFlogsConfig config)
    {
        // Character info
        ImGui.TextColored(HeaderColor, Loc.T(LocalizedStrings.Analytics.FFlogsIntegration, "FFLogs Integration"));
        ImGui.Text(Loc.TFormat(LocalizedStrings.Analytics.CharacterFormat, "Character: {0} @ {1} ({2})",
            config.CharacterName, config.ServerSlug.ToUpperInvariant(), config.Region));

        // Status and rate limit
        var rateLimit = fflogsService.RateLimitInfo;
        if (rateLimit != null)
        {
            var statusColor = rateLimit.IsLowOnPoints ? WarningColor : GoodColor;
            ImGui.TextColored(statusColor, Loc.TFormat(LocalizedStrings.Analytics.StatusConnectedPoints,
                "Status: Connected ({0}/{1} points remaining)",
                $"{rateLimit.PointsRemaining:N0}", $"{rateLimit.PointsLimit:N0}"));
        }
        else
        {
            var statusColor = fflogsService.IsAuthenticated ? GoodColor : NeutralColor;
            var statusText = fflogsService.IsAuthenticated
                ? Loc.T(LocalizedStrings.Analytics.StatusConnected, "Status: Connected")
                : Loc.T(LocalizedStrings.Analytics.StatusNotConnected, "Status: Not connected");
            ImGui.TextColored(statusColor, statusText);
        }

        // Action buttons
        if (ImGui.Button(Loc.T(LocalizedStrings.Analytics.Refresh, "Refresh")))
        {
            _ = RefreshRankingsAsync(fflogsService, config);
        }

        ImGui.SameLine();

        if (ImGui.Button(Loc.T(LocalizedStrings.Analytics.ChangeCharacter, "Change Character")))
        {
            config.CharacterName = "";
            config.ServerSlug = "";
            config.CachedCharacterId = null;
            characterNameInput = "";
            serverSlugInput = "";
            currentRankings = null;
        }
    }

    private static void DrawAllStarsSummary(FFlogsZoneRanking rankings)
    {
        ImGui.TextColored(InfoColor, Loc.TFormat(LocalizedStrings.Analytics.CurrentZone, "Current Zone: {0}", rankings.ZoneName));

        if (rankings.AllStarsPoints > 0)
        {
            var rankColor = GetPercentileColor(100 - rankings.AllStarsRankPercent);
            ImGui.Text(Loc.TFormat(LocalizedStrings.Analytics.AllStarsFormat,
                "All Stars: {0} pts (Rank #{1} | Top {2}%)",
                $"{rankings.AllStarsPoints:N0}", $"{rankings.AllStarsRank:N0}", rankings.AllStarsRankPercent.ToString()));
        }
        else
        {
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Analytics.NoAllStars, "No All Stars ranking available."));
        }
    }

    private static void DrawEncounterRankings(FFlogsZoneRanking rankings)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Analytics.EncounterRankings, "Encounter Rankings"));

        if (rankings.Encounters.Count == 0)
        {
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Analytics.NoEncounterData, "No encounter data available."));
            return;
        }

        if (ImGui.BeginTable("FFLogsEncounters", 5, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Analytics.Boss, "Boss"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Analytics.Best, "Best"), ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Analytics.Median, "Median"), ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Analytics.Kills, "Kills"), ImGuiTableColumnFlags.WidthFixed, 50);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Analytics.Trend, "Trend"), ImGuiTableColumnFlags.WidthFixed, 60);

            ImGui.TableHeadersRow();

            foreach (var encounter in rankings.Encounters)
            {
                ImGui.TableNextRow();

                // Boss name
                ImGui.TableNextColumn();
                ImGui.Text(TruncateName(encounter.EncounterName, 25));

                // Best percentile
                ImGui.TableNextColumn();
                var bestColor = GetPercentileColor(encounter.BestPercentile);
                ImGui.TextColored(bestColor, $"{encounter.BestPercentile:F1}%");

                // Median percentile
                ImGui.TableNextColumn();
                var medianColor = GetPercentileColor(encounter.MedianPercentile);
                ImGui.TextColored(medianColor, $"{encounter.MedianPercentile:F1}%");

                // Total kills
                ImGui.TableNextColumn();
                ImGui.Text($"{encounter.TotalKills}");

                // Trend
                ImGui.TableNextColumn();
                var trend = encounter.PercentileTrend;
                if (Math.Abs(trend) < 0.1f)
                {
                    ImGui.TextColored(NeutralColor, "→ 0.0%");
                }
                else if (trend > 0)
                {
                    ImGui.TextColored(GoodColor, $"↑ +{trend:F1}%");
                }
                else
                {
                    ImGui.TextColored(BadColor, $"↓ {trend:F1}%");
                }
            }

            ImGui.EndTable();
        }
    }

    private static async Task TestConnectionAsync(IFFlogsService fflogsService)
    {
        isLoading = true;
        try
        {
            var success = await fflogsService.TestConnectionAsync();
            statusMessage = success
                ? Loc.T(LocalizedStrings.Analytics.ConnectionSuccessful, "Connection successful!")
                : Loc.T(LocalizedStrings.Analytics.ConnectionFailed, "Connection failed. Check your credentials.");
        }
        catch (Exception ex)
        {
            statusMessage = Loc.TFormat(LocalizedStrings.Analytics.ErrorPrefix, "Error: {0}", ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    private static async Task LookupCharacterAsync(IFFlogsService fflogsService, FFlogsConfig config)
    {
        isLoading = true;
        statusMessage = Loc.T(LocalizedStrings.Analytics.LookingUpCharacter, "Looking up character...");

        try
        {
            var result = await fflogsService.GetCharacterAsync(config.CharacterName, config.ServerSlug, config.Region);
            if (result.Success && result.Data != null)
            {
                config.CachedCharacterId = result.Data.Id;
                statusMessage = Loc.TFormat(LocalizedStrings.Analytics.CharacterFound, "Character found! FFLogs ID: {0}", result.Data.Id.ToString());

                // Load initial rankings
                await RefreshRankingsAsync(fflogsService, config);
            }
            else
            {
                statusMessage = result.Error ?? Loc.T(LocalizedStrings.Analytics.CharacterNotFound, "Character not found.");
            }
        }
        catch (Exception ex)
        {
            statusMessage = Loc.TFormat(LocalizedStrings.Analytics.ErrorPrefix, "Error: {0}", ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    private static async Task RefreshRankingsAsync(IFFlogsService fflogsService, FFlogsConfig config)
    {
        if (!config.CachedCharacterId.HasValue)
        {
            // Need to look up character first
            await LookupCharacterAsync(fflogsService, config);
            return;
        }

        isLoading = true;
        lastRefresh = DateTime.Now;

        try
        {
            var result = await fflogsService.GetZoneRankingsAsync(
                config.CachedCharacterId.Value,
                FFlogsEncounterIds.ArcadionSavageZone);

            if (result.Success && result.Data != null)
            {
                currentRankings = result.Data;
                statusMessage = null;
            }
            else
            {
                statusMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            statusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private static Vector4 GetPercentileColor(float percentile) => percentile switch
    {
        >= 99f => new Vector4(0.9f, 0.5f, 0.9f, 1.0f),  // Pink (99+)
        >= 95f => new Vector4(1.0f, 0.6f, 0.2f, 1.0f),  // Orange (95-98)
        >= 75f => new Vector4(0.6f, 0.3f, 0.9f, 1.0f),  // Purple (75-94)
        >= 50f => new Vector4(0.2f, 0.4f, 1.0f, 1.0f),  // Blue (50-74)
        >= 25f => new Vector4(0.2f, 0.8f, 0.2f, 1.0f),  // Green (25-49)
        _ => new Vector4(0.7f, 0.7f, 0.7f, 1.0f)        // Gray (<25)
    };

    private static string TruncateName(string name, int maxLength)
    {
        if (string.IsNullOrEmpty(name) || name.Length <= maxLength)
            return name;
        return name[..(maxLength - 3)] + "...";
    }
}
