namespace Olympus.Localization;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Dalamud.Game;
using Dalamud.Plugin.Services;

/// <summary>
/// Core localization service for Olympus.
/// Provides string lookup with fallback to English and caching for performance.
/// </summary>
public sealed class OlympusLocalization : IDisposable
{
    /// <summary>
    /// Singleton instance for easy access throughout the codebase.
    /// </summary>
    public static OlympusLocalization? Instance { get; private set; }

    /// <summary>
    /// Supported language codes mapped to FFXIV ClientLanguage.
    /// </summary>
    public static readonly IReadOnlyDictionary<ClientLanguage, string> LanguageCodes = new Dictionary<ClientLanguage, string>
    {
        { ClientLanguage.English, "en" },
        { ClientLanguage.Japanese, "ja" },
        { ClientLanguage.German, "de" },
        { ClientLanguage.French, "fr" },
    };

    /// <summary>
    /// Additional language codes for community translations (not in FFXIV client).
    /// </summary>
    public static readonly IReadOnlyList<string> CommunityLanguages = new[]
    {
        "zh",  // Chinese Simplified
        "tw",  // Chinese Traditional
        "ko",  // Korean
        "es",  // Spanish
        "pt",  // Portuguese
        "ru",  // Russian
    };

    private readonly IPluginLog log;
    private readonly IClientState clientState;
    private readonly Configuration configuration;

    private Dictionary<string, string> currentStrings = new();
    private Dictionary<string, string> fallbackStrings = new();
    private string currentLanguageCode = "en";

    /// <summary>
    /// Event raised when the language changes.
    /// </summary>
    public event Action<string>? OnLanguageChanged;

    /// <summary>
    /// Gets the current language code (e.g., "en", "ja", "de", "fr").
    /// </summary>
    public string CurrentLanguage => this.currentLanguageCode;

    /// <summary>
    /// Creates a new OlympusLocalization service.
    /// </summary>
    public OlympusLocalization(
        IClientState clientState,
        Configuration configuration,
        IPluginLog log)
    {
        this.clientState = clientState;
        this.configuration = configuration;
        this.log = log;

        // Set singleton instance
        Instance = this;

        // Load English as fallback (always available)
        this.fallbackStrings = LoadLanguageFile("en");

        // Determine initial language from game client or configuration override
        var initialLanguage = GetEffectiveLanguage();
        SetLanguage(initialLanguage);

        this.log.Information("OlympusLocalization initialized with language: {Language}", this.currentLanguageCode);
    }

    /// <summary>
    /// Gets the effective language code based on configuration override or client language.
    /// </summary>
    private string GetEffectiveLanguage()
    {
        // Check for manual override in configuration
        if (!string.IsNullOrEmpty(this.configuration.LanguageOverride))
        {
            return this.configuration.LanguageOverride;
        }

        // Use game client language
        if (LanguageCodes.TryGetValue(this.clientState.ClientLanguage, out var code))
        {
            return code;
        }

        // Default to English
        return "en";
    }

    /// <summary>
    /// Sets the current language and reloads strings.
    /// </summary>
    public void SetLanguage(string languageCode)
    {
        if (this.currentLanguageCode == languageCode && this.currentStrings.Count > 0)
            return;

        this.currentLanguageCode = languageCode;

        if (languageCode == "en")
        {
            // English uses fallback directly
            this.currentStrings = this.fallbackStrings;
        }
        else
        {
            // Load target language, falling back to English for missing keys
            this.currentStrings = LoadLanguageFile(languageCode);
        }

        this.log.Information("Language changed to: {Language} ({StringCount} strings loaded)",
            languageCode, this.currentStrings.Count);

        OnLanguageChanged?.Invoke(languageCode);
    }

    /// <summary>
    /// Loads a language file from embedded resources.
    /// </summary>
    private Dictionary<string, string> LoadLanguageFile(string languageCode)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Olympus.Localization.Loc.olympus_{languageCode}.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            this.log.Warning("Language file not found: {ResourceName}", resourceName);
            return new Dictionary<string, string>();
        }

        try
        {
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return data ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            this.log.Error(ex, "Error loading language file: {ResourceName}", resourceName);
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Gets a localized string by key, with fallback to English and then to the fallback value.
    /// This is the primary method for localization throughout the codebase.
    /// </summary>
    /// <param name="key">The localization key (e.g., "ui.main.status").</param>
    /// <param name="fallback">The fallback value if key is not found (usually the English text).</param>
    /// <returns>The localized string, or fallback if not found.</returns>
    public string T(string key, string fallback)
    {
        // Try current language first
        if (this.currentStrings.TryGetValue(key, out var value))
            return value;

        // Try English fallback
        if (this.currentLanguageCode != "en" && this.fallbackStrings.TryGetValue(key, out var fallbackValue))
            return fallbackValue;

        // Return provided fallback (usually English text inline)
        return fallback;
    }

    /// <summary>
    /// Gets a localized string with format arguments.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <param name="fallback">The fallback format string.</param>
    /// <param name="args">Format arguments.</param>
    /// <returns>The formatted localized string.</returns>
    public string TFormat(string key, string fallback, params object[] args)
    {
        var template = T(key, fallback);
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            // If format fails, return template as-is
            return template;
        }
    }

    /// <summary>
    /// Checks if a localization key exists in the current language or fallback.
    /// </summary>
    public bool HasKey(string key)
    {
        return this.currentStrings.ContainsKey(key) || this.fallbackStrings.ContainsKey(key);
    }

    /// <summary>
    /// Gets all available language codes (game + community).
    /// </summary>
    public static IEnumerable<string> GetAllLanguageCodes()
    {
        foreach (var code in LanguageCodes.Values)
            yield return code;

        foreach (var code in CommunityLanguages)
            yield return code;
    }

    /// <summary>
    /// Gets the display name for a language code.
    /// </summary>
    public static string GetLanguageDisplayName(string code)
    {
        return code switch
        {
            "en" => "English",
            "ja" => "Japanese",
            "de" => "German",
            "fr" => "French",
            "zh" => "Chinese (Simplified)",
            "tw" => "Chinese (Traditional)",
            "ko" => "Korean",
            "es" => "Spanish",
            "pt" => "Portuguese",
            "ru" => "Russian",
            _ => code,
        };
    }

    public void Dispose()
    {
        Instance = null;
    }
}

/// <summary>
/// Static helper class for convenient access to localization.
/// </summary>
public static class Loc
{
    /// <summary>
    /// Gets a localized string by key, with fallback.
    /// Shorthand for OlympusLocalization.Instance.T(key, fallback).
    /// </summary>
    public static string T(string key, string fallback)
    {
        return OlympusLocalization.Instance?.T(key, fallback) ?? fallback;
    }

    /// <summary>
    /// Gets a formatted localized string.
    /// Shorthand for OlympusLocalization.Instance.TFormat(key, fallback, args).
    /// </summary>
    public static string TFormat(string key, string fallback, params object[] args)
    {
        return OlympusLocalization.Instance?.TFormat(key, fallback, args) ?? string.Format(fallback, args);
    }
}
