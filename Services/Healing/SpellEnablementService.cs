using System;
using System.Collections.Generic;

namespace Olympus.Services.Healing;

/// <summary>
/// Consolidates spell enablement checking from configuration.
/// Uses a dictionary-based registry pattern for maintainability and multi-job scalability.
/// </summary>
public class SpellEnablementService : ISpellEnablementService
{
    private readonly Dictionary<uint, Func<bool>> _spellEnablementRegistry;

    public SpellEnablementService(Configuration configuration)
    {
        // Initialize registry with all known WHM spells and their config bindings
        _spellEnablementRegistry = new Dictionary<uint, Func<bool>>
        {
            // Healing - Single Target
            { 120, () => configuration.Healing.EnableCure },              // Cure
            { 135, () => configuration.Healing.EnableCureII },            // Cure II
            { 137, () => configuration.Healing.EnableRegen },             // Regen
            { 16531, () => configuration.Healing.EnableAfflatusSolace },  // Afflatus Solace
            { 3570, () => configuration.Healing.EnableTetragrammaton },   // Tetragrammaton
            { 140, () => configuration.Healing.EnableBenediction },       // Benediction

            // Healing - AoE
            { 131, () => configuration.Healing.EnableCureIII },           // Cure III
            { 124, () => configuration.Healing.EnableMedica },            // Medica
            { 133, () => configuration.Healing.EnableMedicaII },          // Medica II
            { 37010, () => configuration.Healing.EnableMedicaIII },       // Medica III
            { 16534, () => configuration.Healing.EnableAfflatusRapture }, // Afflatus Rapture
            { 3571, () => configuration.Healing.EnableAssize },           // Assize
            { 3569, () => configuration.Healing.EnableAsylum },           // Asylum

            // Damage
            { 119, () => configuration.Damage.EnableStone },              // Stone
            { 127, () => configuration.Damage.EnableStoneII },            // Stone II
            { 3568, () => configuration.Damage.EnableStoneIII },          // Stone III
            { 7431, () => configuration.Damage.EnableStoneIV },           // Stone IV
            { 16533, () => configuration.Damage.EnableGlare },            // Glare
            { 25859, () => configuration.Damage.EnableGlareIII },         // Glare III
            { 37009, () => configuration.Damage.EnableGlareIV },          // Glare IV
            { 139, () => configuration.Damage.EnableHoly },               // Holy
            { 25860, () => configuration.Damage.EnableHolyIII },          // Holy III
            { 16535, () => configuration.Damage.EnableAfflatusMisery },   // Afflatus Misery

            // DoT
            { 121, () => configuration.Dot.EnableAero },                  // Aero
            { 132, () => configuration.Dot.EnableAeroII },                // Aero II
            { 16532, () => configuration.Dot.EnableDia },                 // Dia

            // Defensive
            { 7432, () => configuration.Defensive.EnableDivineBenison },  // Divine Benison
            { 7433, () => configuration.Defensive.EnablePlenaryIndulgence }, // Plenary Indulgence
            { 16536, () => configuration.Defensive.EnableTemperance },    // Temperance
            { 25861, () => configuration.Defensive.EnableAquaveil },      // Aquaveil
            { 25862, () => configuration.Defensive.EnableLiturgyOfTheBell }, // Liturgy of the Bell
            { 37011, () => configuration.Defensive.EnableDivineCaress },  // Divine Caress

            // Buffs
            { 136, () => configuration.Buffs.EnablePresenceOfMind },      // Presence of Mind
            { 7430, () => configuration.Buffs.EnableThinAir },            // Thin Air
            { 37012, () => configuration.Buffs.EnableAetherialShift },    // Aetherial Shift

            // Role Actions
            { 7568, () => configuration.RoleActions.EnableEsuna },        // Esuna

            // Resurrection
            { 125, () => configuration.Resurrection.EnableRaise },        // Raise
        };
    }

    /// <summary>
    /// Checks if a spell is enabled in the configuration.
    /// Covers all WHM healing, damage, defensive, and role actions.
    /// </summary>
    public bool IsSpellEnabled(uint actionId)
    {
        return _spellEnablementRegistry.TryGetValue(actionId, out var enabledCheck)
            ? enabledCheck()
            : true; // Default to enabled for unknown spells
    }
}
