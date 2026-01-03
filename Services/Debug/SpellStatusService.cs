using System.Collections.Generic;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Services.Action;

namespace Olympus.Services.Debug;

/// <summary>
/// Spell category for grouping in UI.
/// </summary>
public enum SpellCategory
{
    GcdHealSingle,
    GcdHealAoE,
    GcdHealHoT,
    OgcdHealSingle,
    OgcdHealAoE,
    GcdDamageSingle,
    GcdDamageAoE,
    GcdDoT,
    Utility
}

/// <summary>
/// Real-time status of a single spell.
/// </summary>
public sealed class SpellStatusEntry
{
    public uint ActionId { get; init; }
    public string Name { get; init; } = "";
    public byte MinLevel { get; init; }
    public SpellCategory Category { get; init; }
    public bool IsGCD { get; init; }
    public float CooldownTotal { get; init; }

    // Real-time status (updated each frame)
    public bool IsLevelSynced { get; set; }
    public bool IsReady { get; set; }
    public float CooldownRemaining { get; set; }
    public string? NotReadyReason { get; set; }
}

/// <summary>
/// Complete spell status snapshot for debug display.
/// </summary>
public sealed class SpellStatusSnapshot
{
    public byte PlayerLevel { get; init; }
    public List<SpellStatusEntry> Spells { get; init; } = new();
}

/// <summary>
/// Service that provides real-time status of all WHM spells for debug display.
/// </summary>
public sealed class SpellStatusService
{
    private readonly ActionService _actionService;
    private readonly List<SpellDefinition> _spellDefinitions;

    public SpellStatusService(ActionService actionService)
    {
        _actionService = actionService;
        _spellDefinitions = BuildSpellDefinitions();
    }

    /// <summary>
    /// Gets current status of all WHM spells.
    /// Called every frame when debug window is visible.
    /// </summary>
    public SpellStatusSnapshot GetSnapshot(byte playerLevel)
    {
        var spells = new List<SpellStatusEntry>();

        foreach (var def in _spellDefinitions)
        {
            var cooldownRemaining = _actionService.GetCooldownRemaining(def.ActionId);
            var isLevelSynced = playerLevel >= def.MinLevel;

            var entry = new SpellStatusEntry
            {
                ActionId = def.ActionId,
                Name = def.Name,
                MinLevel = def.MinLevel,
                Category = def.Category,
                IsGCD = def.IsGCD,
                CooldownTotal = def.CooldownTotal,
                IsLevelSynced = isLevelSynced,
                CooldownRemaining = cooldownRemaining,
                IsReady = false,
                NotReadyReason = null
            };

            // Determine ready status and reason
            if (!isLevelSynced)
            {
                entry.NotReadyReason = $"Lv{def.MinLevel}";
            }
            else if (cooldownRemaining > 0)
            {
                entry.NotReadyReason = $"{cooldownRemaining:F1}s";
            }
            else
            {
                entry.IsReady = _actionService.IsActionReady(def.ActionId);
                if (!entry.IsReady)
                {
                    entry.NotReadyReason = "Not Ready";
                }
            }

            spells.Add(entry);
        }

        return new SpellStatusSnapshot
        {
            PlayerLevel = playerLevel,
            Spells = spells
        };
    }

    /// <summary>
    /// Internal definition for building spell list.
    /// </summary>
    private sealed class SpellDefinition
    {
        public uint ActionId { get; init; }
        public string Name { get; init; } = "";
        public byte MinLevel { get; init; }
        public SpellCategory Category { get; init; }
        public bool IsGCD { get; init; }
        public float CooldownTotal { get; init; }
    }

    private static SpellDefinition CreateDef(ActionDefinition action, SpellCategory category)
    {
        return new SpellDefinition
        {
            ActionId = action.ActionId,
            Name = action.Name,
            MinLevel = action.MinLevel,
            Category = category,
            IsGCD = action.IsGCD,
            CooldownTotal = action.RecastTime
        };
    }

    private static List<SpellDefinition> BuildSpellDefinitions()
    {
        return new List<SpellDefinition>
        {
            // GCD Heals - Single
            CreateDef(WHMActions.Cure, SpellCategory.GcdHealSingle),
            CreateDef(WHMActions.CureII, SpellCategory.GcdHealSingle),
            CreateDef(WHMActions.AfflatusSolace, SpellCategory.GcdHealSingle),

            // GCD Heals - AoE
            CreateDef(WHMActions.Medica, SpellCategory.GcdHealAoE),
            CreateDef(WHMActions.MedicaII, SpellCategory.GcdHealAoE),
            CreateDef(WHMActions.MedicaIII, SpellCategory.GcdHealAoE),
            CreateDef(WHMActions.CureIII, SpellCategory.GcdHealAoE),
            CreateDef(WHMActions.AfflatusRapture, SpellCategory.GcdHealAoE),

            // GCD Heals - HoT
            CreateDef(WHMActions.Regen, SpellCategory.GcdHealHoT),

            // oGCD Heals - Single
            CreateDef(WHMActions.Benediction, SpellCategory.OgcdHealSingle),
            CreateDef(WHMActions.Tetragrammaton, SpellCategory.OgcdHealSingle),
            CreateDef(WHMActions.DivineBenison, SpellCategory.OgcdHealSingle),
            CreateDef(WHMActions.Aquaveil, SpellCategory.OgcdHealSingle),

            // oGCD Heals - AoE
            CreateDef(WHMActions.Asylum, SpellCategory.OgcdHealAoE),
            CreateDef(WHMActions.Assize, SpellCategory.OgcdHealAoE),
            CreateDef(WHMActions.PlenaryIndulgence, SpellCategory.OgcdHealAoE),
            CreateDef(WHMActions.Temperance, SpellCategory.OgcdHealAoE),
            CreateDef(WHMActions.LiturgyOfTheBell, SpellCategory.OgcdHealAoE),

            // GCD Damage - Single
            CreateDef(WHMActions.Stone, SpellCategory.GcdDamageSingle),
            CreateDef(WHMActions.StoneII, SpellCategory.GcdDamageSingle),
            CreateDef(WHMActions.StoneIII, SpellCategory.GcdDamageSingle),
            CreateDef(WHMActions.StoneIV, SpellCategory.GcdDamageSingle),
            CreateDef(WHMActions.Glare, SpellCategory.GcdDamageSingle),
            CreateDef(WHMActions.GlareIII, SpellCategory.GcdDamageSingle),
            CreateDef(WHMActions.GlareIV, SpellCategory.GcdDamageSingle),

            // GCD Damage - AoE
            CreateDef(WHMActions.Holy, SpellCategory.GcdDamageAoE),
            CreateDef(WHMActions.HolyIII, SpellCategory.GcdDamageAoE),
            CreateDef(WHMActions.AfflatusMisery, SpellCategory.GcdDamageAoE),

            // GCD DoT
            CreateDef(WHMActions.Aero, SpellCategory.GcdDoT),
            CreateDef(WHMActions.AeroII, SpellCategory.GcdDoT),
            CreateDef(WHMActions.Dia, SpellCategory.GcdDoT),

            // Utility
            CreateDef(WHMActions.Swiftcast, SpellCategory.Utility),
            CreateDef(WHMActions.LucidDreaming, SpellCategory.Utility),
            CreateDef(WHMActions.Surecast, SpellCategory.Utility),
            CreateDef(WHMActions.Rescue, SpellCategory.Utility),
            CreateDef(WHMActions.Esuna, SpellCategory.Utility),
            CreateDef(WHMActions.Raise, SpellCategory.Utility),
            CreateDef(WHMActions.PresenceOfMind, SpellCategory.Utility),
            CreateDef(WHMActions.AetherialShift, SpellCategory.Utility),
        };
    }
}
