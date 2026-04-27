using System;
using System.Collections.Generic;
using System.Linq;
using Olympus.Localization;

namespace Olympus.Windows.Config;

/// <summary>
/// Information about a searchable setting.
/// </summary>
public readonly struct SettingInfo
{
    public readonly string Label;
    public readonly string? Description;

    public SettingInfo(string label, string? description = null)
    {
        this.Label = label;
        this.Description = description;
    }
}

/// <summary>
/// Registry of all searchable settings for the config window.
/// Maps search terms to sections that contain matching settings.
/// </summary>
public sealed class SettingRegistry
{
    private readonly Dictionary<ConfigSection, List<SettingInfo>> sectionSettings = new();

    public SettingRegistry()
    {
        BuildRegistry();
    }

    /// <summary>
    /// Searches for sections containing settings matching the query.
    /// </summary>
    public HashSet<ConfigSection> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new HashSet<ConfigSection>();

        var results = new HashSet<ConfigSection>();
        var lowerQuery = query.ToLowerInvariant();

        foreach (var (section, settings) in this.sectionSettings)
        {
            // Check section name
            var sectionName = GetSectionName(section);
            if (sectionName.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(section);
                continue;
            }

            // Check setting labels and descriptions
            foreach (var setting in settings)
            {
                if (setting.Label.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                    setting.Description?.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) == true)
                {
                    results.Add(section);
                    break;
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Gets settings in a section that match the query (for highlighting).
    /// </summary>
    public List<SettingInfo> GetMatchingSettings(ConfigSection section, string query)
    {
        if (string.IsNullOrWhiteSpace(query) || !this.sectionSettings.TryGetValue(section, out var settings))
            return new List<SettingInfo>();

        return settings.Where(s =>
            s.Label.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            s.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) == true).ToList();
    }

    /// <summary>
    /// Checks if a label or description matches the search query.
    /// </summary>
    public static bool IsMatch(string? text, string? query)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrEmpty(text))
            return false;

        return text.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetSectionName(ConfigSection section)
    {
        return section switch
        {
            ConfigSection.General => Loc.T(LocalizedStrings.Sidebar.GeneralItem, "General"),
            ConfigSection.Targeting => Loc.T(LocalizedStrings.Sidebar.Targeting, "Targeting"),
            ConfigSection.RoleActions => Loc.T(LocalizedStrings.Sidebar.RoleActions, "Role Actions"),
            ConfigSection.WhiteMage => Loc.T(LocalizedStrings.Sidebar.WhiteMage, "White Mage"),
            ConfigSection.Scholar => Loc.T(LocalizedStrings.Sidebar.Scholar, "Scholar"),
            ConfigSection.Astrologian => Loc.T(LocalizedStrings.Sidebar.Astrologian, "Astrologian"),
            ConfigSection.Sage => Loc.T(LocalizedStrings.Sidebar.Sage, "Sage"),
            ConfigSection.Paladin => Loc.T(LocalizedStrings.Sidebar.Paladin, "Paladin"),
            ConfigSection.Warrior => Loc.T(LocalizedStrings.Sidebar.Warrior, "Warrior"),
            ConfigSection.DarkKnight => Loc.T(LocalizedStrings.Sidebar.DarkKnight, "Dark Knight"),
            ConfigSection.Gunbreaker => Loc.T(LocalizedStrings.Sidebar.Gunbreaker, "Gunbreaker"),
            ConfigSection.Dragoon => Loc.T(LocalizedStrings.Sidebar.Dragoon, "Dragoon"),
            ConfigSection.Ninja => Loc.T(LocalizedStrings.Sidebar.Ninja, "Ninja"),
            ConfigSection.Samurai => Loc.T(LocalizedStrings.Sidebar.Samurai, "Samurai"),
            ConfigSection.Monk => Loc.T(LocalizedStrings.Sidebar.Monk, "Monk"),
            ConfigSection.Reaper => Loc.T(LocalizedStrings.Sidebar.Reaper, "Reaper"),
            ConfigSection.Viper => Loc.T(LocalizedStrings.Sidebar.Viper, "Viper"),
            ConfigSection.Machinist => Loc.T(LocalizedStrings.Sidebar.Machinist, "Machinist"),
            ConfigSection.Bard => Loc.T(LocalizedStrings.Sidebar.Bard, "Bard"),
            ConfigSection.Dancer => Loc.T(LocalizedStrings.Sidebar.Dancer, "Dancer"),
            ConfigSection.CasterShared => "Shared Caster",
            ConfigSection.BlackMage => Loc.T(LocalizedStrings.Sidebar.BlackMage, "Black Mage"),
            ConfigSection.Summoner => Loc.T(LocalizedStrings.Sidebar.Summoner, "Summoner"),
            ConfigSection.RedMage => Loc.T(LocalizedStrings.Sidebar.RedMage, "Red Mage"),
            ConfigSection.Pictomancer => Loc.T(LocalizedStrings.Sidebar.Pictomancer, "Pictomancer"),
            ConfigSection.Timeline => Loc.T(LocalizedStrings.Sidebar.Timeline, "Timeline"),
            _ => section.ToString()
        };
    }

    private void BuildRegistry()
    {
        // General section
        this.sectionSettings[ConfigSection.General] = new List<SettingInfo>
        {
            new("Enable Healing", "When enabled, healing spells will be cast automatically"),
            new("Enable Damage", "When enabled, damage spells will be cast automatically"),
            new("Enable DoT", "When enabled, DoT spells will be cast automatically"),
            new("Movement Tolerance", "How long to wait after movement before casting"),
            new("Privacy", "Telemetry"),
            new("Language", "Select language")
        };

        // Targeting section
        this.sectionSettings[ConfigSection.Targeting] = new List<SettingInfo>
        {
            new("Targeting Mode", "Lowest HP, Priority, Custom"),
            new("Enemy Strategy", "Lowest HP, Highest HP, Nearest, Tank Assist, Current Target, Focus Target"),
            new("Fallback to Lowest HP", "Fallback when no valid target"),
            new("Movement Tolerance", "Movement tolerance for targeting")
        };

        // Role Actions section
        this.sectionSettings[ConfigSection.RoleActions] = new List<SettingInfo>
        {
            new("Lucid Dreaming", "MP threshold for Lucid Dreaming"),
            new("Swiftcast", "Enable Swiftcast for instant casts"),
            new("Surecast", "Enable Surecast for knockback immunity"),
            new("Rescue", "Enable Rescue to pull party members"),
            new("Esuna", "Enable Esuna to cleanse debuffs"),
            new("Raise", "Enable resurrection spells")
        };

        // White Mage section
        this.sectionSettings[ConfigSection.WhiteMage] = new List<SettingInfo>
        {
            // Healing
            new("Cure", "Single-target heal"),
            new("Cure II", "Single-target heal (higher potency)"),
            new("Cure III", "Targeted AoE heal"),
            new("Medica", "AoE heal"),
            new("Medica II", "AoE heal with HoT"),
            new("Medica III", "AoE heal (higher potency)"),
            new("Afflatus Solace", "Lily single-target heal"),
            new("Afflatus Rapture", "Lily AoE heal"),
            new("Lily Strategy", "Aggressive, Balanced, Conservative, Disabled"),
            new("Tetragrammaton", "oGCD instant heal"),
            new("Benediction", "Full HP restore"),
            new("Assize", "oGCD AoE heal and damage"),
            new("Regen", "Single-target HoT"),
            new("Asylum", "Ground AoE HoT"),
            // Buffs
            new("Presence of Mind", "Speed buff"),
            new("Thin Air", "MP cost reduction"),
            new("Aetherial Shift", "Gap closer dash"),
            // Thresholds
            new("oGCD Emergency", "Emergency threshold for oGCD heals"),
            new("GCD Emergency", "Emergency threshold for GCD heals"),
            new("Benediction Threshold", "HP threshold for Benediction"),
            new("AoE Min Targets", "Minimum targets for AoE heal"),
            // Advanced
            new("Damage-Based Triage", "Prioritize healing targets taking damage"),
            new("Triage Preset", "Balanced, Tank Focus, Spread Damage, RaidWide, Custom"),
            new("Assize Healing", "Use Assize as healing oGCD"),
            new("Preemptive Healing", "Heal before damage spikes"),
            new("Timeline Predictions", "Use fight timelines"),
            new("Scored Heal Selection", "Multi-factor scoring"),
            // Defensive
            new("Divine Benison", "Single-target shield"),
            new("Aquaveil", "Damage reduction shield"),
            new("Plenary Indulgence", "Party mitigation"),
            new("Temperance", "Party mitigation and healing boost"),
            new("Liturgy of the Bell", "Ground AoE reactive heal"),
            new("Divine Caress", "AoE shield after Temperance"),
            new("Defensive Threshold", "Party HP for defensives"),
            // Damage
            new("DPS Priority", "Heal First, Balanced, DPS First"),
            new("Stone", "Single-target damage"),
            new("Glare", "Single-target damage (higher level)"),
            new("Holy", "AoE damage with stun"),
            new("Afflatus Misery", "Blood Lily damage spell"),
            new("AoE Min Enemies", "Minimum enemies for AoE damage"),
            // DoT
            new("Aero", "Single-target DoT"),
            new("Dia", "Single-target DoT (higher level)")
        };

        // Scholar section
        this.sectionSettings[ConfigSection.Scholar] = new List<SettingInfo>
        {
            new("Physick", "Single-target GCD heal"),
            new("Adloquium", "Single-target shield"),
            new("Succor", "AoE shield"),
            new("Lustrate", "oGCD instant heal"),
            new("Excogitation", "Delayed auto-heal"),
            new("Sacred Soil", "Ground AoE mitigation"),
            new("Indomitability", "oGCD AoE heal"),
            new("Whispering Dawn", "Fairy AoE HoT"),
            new("Fey Blessing", "Fairy AoE heal"),
            new("Fey Illumination", "Healing boost"),
            new("Aetherpact", "Fairy tether heal"),
            new("Dissipation", "Sacrifice fairy for Aetherflow"),
            new("Recitation", "Guaranteed crit heal"),
            new("Seraph", "Summon Seraph"),
            new("Emergency Tactics", "Convert shield to heal"),
            new("Deployment Tactics", "Spread shield to party"),
            new("Expedient", "Sprint and mitigation"),
            new("Protraction", "HP and healing increase"),
            new("Broil", "Single-target damage"),
            new("Art of War", "AoE damage"),
            new("Ruin II", "Instant damage"),
            new("Bio", "Single-target DoT"),
            new("Chain Stratagem", "Crit vulnerability debuff")
        };

        // Astrologian section
        this.sectionSettings[ConfigSection.Astrologian] = new List<SettingInfo>
        {
            new("Benefic", "Single-target heal"),
            new("Benefic II", "Single-target heal (higher potency)"),
            new("Helios", "AoE heal"),
            new("Aspected Benefic", "Single-target HoT"),
            new("Aspected Helios", "AoE HoT"),
            new("Essential Dignity", "oGCD instant heal"),
            new("Celestial Intersection", "Single-target shield/HoT"),
            new("Celestial Opposition", "oGCD AoE heal"),
            new("Collective Unconscious", "Channeled mitigation"),
            new("Earthly Star", "Delayed AoE heal"),
            new("Horoscope", "Delayed AoE heal"),
            new("Neutral Sect", "Shield on healing spells"),
            new("Exaltation", "Single-target mitigation"),
            new("Macrocosmos", "Delayed AoE heal"),
            new("Draw", "Draw a card"),
            new("Play", "Play drawn card"),
            new("Minor Arcana", "Lord/Lady of Crowns"),
            new("Divination", "Party damage buff"),
            new("Lightspeed", "Instant cast buff"),
            new("Malefic", "Single-target damage"),
            new("Gravity", "AoE damage"),
            new("Combust", "Single-target DoT")
        };

        // Sage section
        this.sectionSettings[ConfigSection.Sage] = new List<SettingInfo>
        {
            new("Diagnosis", "Single-target heal"),
            new("Prognosis", "AoE heal"),
            new("Eukrasian Diagnosis", "Single-target shield"),
            new("Eukrasian Prognosis", "AoE shield"),
            new("Druochole", "oGCD instant heal"),
            new("Ixochole", "oGCD AoE heal"),
            new("Taurochole", "oGCD heal with mitigation"),
            new("Kerachole", "Ground AoE mitigation"),
            new("Haima", "Single-target shield stack"),
            new("Panhaima", "Party shield stack"),
            new("Holos", "Party mitigation"),
            new("Physis", "Healing boost"),
            new("Zoe", "Next GCD healing boost"),
            new("Krasis", "Single-target healing received boost"),
            new("Pepsis", "Convert shields to heals"),
            new("Soteria", "Kardia healing boost"),
            new("Kardia", "Passive healing link"),
            new("Dosis", "Single-target damage"),
            new("Phlegma", "Instant AoE damage"),
            new("Dyskrasia", "AoE damage"),
            new("Toxikon", "Instant damage from Addersting"),
            new("Pneuma", "Line AoE heal and damage"),
            new("Eukrasian Dosis", "Single-target DoT")
        };

        // Paladin section
        this.sectionSettings[ConfigSection.Paladin] = new List<SettingInfo>
        {
            new("Hallowed Ground", "Invulnerability"),
            new("Sentinel", "30% damage reduction"),
            new("Bulwark", "Block rate increase"),
            new("Sheltron", "Block with healing"),
            new("Cover", "Protect party member"),
            new("Divine Veil", "Party shield"),
            new("Passage of Arms", "Party mitigation"),
            new("Intervention", "Single-target mitigation"),
            new("Holy Sheltron", "Enhanced Sheltron"),
            new("Knight's Resolve", "Party damage reduction"),
            new("Knight's Benediction", "Party healing"),
            new("Fight or Flight", "Damage buff"),
            new("Requiescat", "Magic phase buff"),
            new("Atonement", "Combo finisher"),
            new("Supplication", "Atonement follow-up"),
            new("Sepulchre", "Atonement finisher"),
            new("Confiteor", "Magic combo finisher"),
            new("Blade of Faith", "Confiteor follow-up"),
            new("Blade of Truth", "Faith follow-up"),
            new("Blade of Valor", "Truth follow-up"),
            new("Holy Spirit", "Single-target magic"),
            new("Holy Circle", "AoE magic"),
            new("Clemency", "Self-heal"),
            new("Goring Blade", "DoT combo")
        };

        // Warrior section
        this.sectionSettings[ConfigSection.Warrior] = new List<SettingInfo>
        {
            new("Holmgang", "Invulnerability"),
            new("Vengeance", "30% damage reduction"),
            new("Thrill of Battle", "HP and healing increase"),
            new("Raw Intuition", "Self-heal on damage"),
            new("Bloodwhetting", "Enhanced Raw Intuition"),
            new("Shake It Off", "Party shield"),
            new("Nascent Flash", "Single-target mitigation"),
            new("Inner Release", "Unlimited Beast Gauge"),
            new("Infuriate", "Gauge generation"),
            new("Inner Chaos", "Guaranteed crit"),
            new("Primal Rend", "Inner Release finisher"),
            new("Primal Ruination", "Rend follow-up"),
            new("Fell Cleave", "Beast Gauge spender"),
            new("Decimate", "AoE Beast Gauge spender"),
            new("Upheaval", "oGCD damage"),
            new("Onslaught", "Gap closer"),
            new("Equilibrium", "Self-heal")
        };

        // Dark Knight section
        this.sectionSettings[ConfigSection.DarkKnight] = new List<SettingInfo>
        {
            new("Living Dead", "Invulnerability"),
            new("Shadow Wall", "30% damage reduction"),
            new("Oblation", "10% damage reduction"),
            new("Dark Mind", "Magic damage reduction"),
            new("Dark Missionary", "Party magic mitigation"),
            new("The Blackest Night", "Shield"),
            new("Delirium", "Unlimited Blood Gauge"),
            new("Blood Weapon", "MP and Blood generation"),
            new("Bloodspiller", "Blood Gauge spender"),
            new("Quietus", "AoE Blood spender"),
            new("Carve and Spit", "oGCD damage"),
            new("Abyssal Drain", "AoE with self-heal"),
            new("Salted Earth", "Ground DoT"),
            new("Salt and Darkness", "Salted Earth follow-up"),
            new("Plunge", "Gap closer"),
            new("Living Shadow", "Summon shadow"),
            new("Shadowbringer", "Line AoE damage"),
            new("Edge of Shadow", "oGCD damage"),
            new("Flood of Shadow", "AoE oGCD damage"),
            new("Scarlet Delirium", "Delirium combo"),
            new("Comeuppance", "Delirium follow-up"),
            new("Torcleaver", "Delirium finisher"),
            new("Disesteem", "Living Shadow finisher")
        };

        // Gunbreaker section
        this.sectionSettings[ConfigSection.Gunbreaker] = new List<SettingInfo>
        {
            new("Superbolide", "Invulnerability"),
            new("Nebula", "30% damage reduction"),
            new("Camouflage", "Parry rate increase"),
            new("Aurora", "HoT"),
            new("Heart of Stone", "15% damage reduction"),
            new("Heart of Corundum", "Enhanced Heart of Stone"),
            new("Heart of Light", "Party magic mitigation"),
            new("No Mercy", "Damage buff"),
            new("Bloodfest", "Cartridge generation"),
            new("Gnashing Fang", "Cartridge combo"),
            new("Savage Claw", "Fang follow-up"),
            new("Wicked Talon", "Claw follow-up"),
            new("Jugular Rip", "Fang oGCD"),
            new("Abdomen Tear", "Claw oGCD"),
            new("Eye Gouge", "Talon oGCD"),
            new("Hypervelocity", "Burst Strike follow-up"),
            new("Burst Strike", "Cartridge spender"),
            new("Fated Circle", "AoE Cartridge spender"),
            new("Double Down", "AoE Cartridge spender"),
            new("Sonic Break", "DoT"),
            new("Bow Shock", "AoE DoT"),
            new("Blasting Zone", "oGCD damage"),
            new("Continuation", "Combo follow-ups"),
            new("Rough Divide", "Gap closer"),
            new("Reign of Beasts", "Enhanced combo"),
            new("Noble Blood", "Reign follow-up"),
            new("Lion Heart", "Blood follow-up")
        };

        // Dragoon section
        this.sectionSettings[ConfigSection.Dragoon] = new List<SettingInfo>
        {
            new("Jump", "Gap closer with damage"),
            new("High Jump", "Enhanced Jump"),
            new("Stardiver", "Life of the Dragon finisher"),
            new("Geirskogul", "Enter Life of the Dragon"),
            new("Nastrond", "Life of the Dragon damage"),
            new("Mirage Dive", "Eye gauge generation"),
            new("Lance Charge", "Damage buff"),
            new("Battle Litany", "Party crit buff"),
            new("Life Surge", "Guaranteed crit"),
            new("Dragon Sight", "Damage buff (legacy)"),
            new("Dragonfire Dive", "AoE gap closer"),
            new("Spineshatter Dive", "Gap closer"),
            new("Wyrmwind Thrust", "Firstminds' Focus spender"),
            new("Rise of the Dragon", "Stardiver follow-up"),
            new("Starcross", "Stardiver follow-up"),
            new("Hold Jumps for Burst", "Save jumps for buff windows"),
            new("Align Battle Litany", "Coordinate with party"),
            new("Positionals", "Enforce positional requirements")
        };

        // Ninja section
        this.sectionSettings[ConfigSection.Ninja] = new List<SettingInfo>
        {
            new("Mudra", "Ninjutsu preparation"),
            new("Ninjutsu", "Execute mudra combination"),
            new("Ten Chi Jin", "Triple Ninjutsu"),
            new("Kunai's Bane", "Vulnerability debuff"),
            new("Trick Attack", "Vulnerability debuff (legacy)"),
            new("Mug", "Damage and Ninki generation"),
            new("Kassatsu", "Enhanced Ninjutsu"),
            new("Bunshin", "Shadow clones"),
            new("Phantom Kamaitachi", "Bunshin follow-up"),
            new("Dream Within a Dream", "oGCD damage"),
            new("Bhavacakra", "Ninki spender"),
            new("Hellfrog Medium", "AoE Ninki spender"),
            new("Meisui", "Convert Suiton to Ninki"),
            new("Forked Raiju", "Raiju combo"),
            new("Fleeting Raiju", "Raiju combo"),
            new("Raiton", "Lightning Ninjutsu"),
            new("Katon", "Fire AoE Ninjutsu"),
            new("Hyoton", "Ice Ninjutsu"),
            new("Huton", "Speed buff Ninjutsu"),
            new("Doton", "Ground DoT Ninjutsu"),
            new("Suiton", "Water Ninjutsu (enables Trick)"),
            new("Deathfrog Medium", "Enhanced Hellfrog"),
            new("Zesho Meppo", "Enhanced Bhavacakra"),
            new("Tenri Jindo", "TCJ finisher")
        };

        // Samurai section
        this.sectionSettings[ConfigSection.Samurai] = new List<SettingInfo>
        {
            new("Iaijutsu", "Sen-based attacks"),
            new("Higanbana", "Single Sen DoT"),
            new("Tenka Goken", "Two Sen AoE"),
            new("Midare Setsugekka", "Three Sen damage"),
            new("Kaeshi Higanbana", "Instant Higanbana"),
            new("Kaeshi Goken", "Instant Tenka Goken"),
            new("Kaeshi Setsugekka", "Instant Midare"),
            new("Meikyo Shisui", "Instant combo finishers"),
            new("Ikishoten", "Kenki generation"),
            new("Shoha", "Meditation spender"),
            new("Ogi Namikiri", "Ikishoten follow-up"),
            new("Kaeshi Namikiri", "Instant Namikiri"),
            new("Shinten", "Kenki spender"),
            new("Kyuten", "AoE Kenki spender"),
            new("Senei", "High damage Kenki"),
            new("Guren", "AoE high damage Kenki"),
            new("Hissatsu Gyoten", "Gap closer"),
            new("Hissatsu Yaten", "Backstep"),
            new("Hagakure", "Convert Sen to Kenki"),
            new("Third Eye", "Damage reduction"),
            new("Tengentsu", "Enhanced Third Eye"),
            new("Zanshin", "Namikiri follow-up")
        };

        // Monk section
        this.sectionSettings[ConfigSection.Monk] = new List<SettingInfo>
        {
            new("Bootshine", "Opo-opo form"),
            new("True Strike", "Raptor form"),
            new("Snap Punch", "Coeurl form"),
            new("Dragon Kick", "Opo-opo blunt"),
            new("Twin Snakes", "Raptor buff"),
            new("Demolish", "Coeurl DoT"),
            new("Perfect Balance", "Form-free attacks"),
            new("Brotherhood", "Party buff"),
            new("Riddle of Fire", "Damage buff"),
            new("Riddle of Earth", "Damage reduction"),
            new("Riddle of Wind", "Auto-attack buff"),
            new("Masterful Blitz", "Beast Chakra spender"),
            new("Elixir Field", "Lunar Nadi AoE"),
            new("Celestial Revolution", "Solar Nadi"),
            new("Rising Phoenix", "Both Nadi"),
            new("Phantom Rush", "Both Nadi follow-up"),
            new("The Forbidden Chakra", "Chakra spender"),
            new("Howling Fist", "AoE Chakra spender"),
            new("Steel Peak", "oGCD damage"),
            new("Thunderclap", "Gap closer"),
            new("Anatman", "Form maintenance"),
            new("Six-sided Star", "Ranged attack"),
            new("Form Shift", "Change form")
        };

        // Reaper section
        this.sectionSettings[ConfigSection.Reaper] = new List<SettingInfo>
        {
            new("Soul Slice", "Soul gauge generation"),
            new("Soul Scythe", "AoE Soul generation"),
            new("Blood Stalk", "Soul Reaver enabler"),
            new("Grim Swathe", "AoE Soul Reaver enabler"),
            new("Unveiled Gibbet", "Soul Reaver combo"),
            new("Unveiled Gallows", "Soul Reaver combo"),
            new("Void Reaping", "Enshroud combo"),
            new("Cross Reaping", "Enshroud combo"),
            new("Enshroud", "Enter Enshroud"),
            new("Communio", "Enshroud finisher"),
            new("Lemure's Slice", "Enshroud oGCD"),
            new("Lemure's Scythe", "AoE Enshroud oGCD"),
            new("Arcane Circle", "Party buff"),
            new("Plentiful Harvest", "Immortal Sacrifice spender"),
            new("Gluttony", "Soul gauge spender"),
            new("Perfectio", "Enhanced Communio"),
            new("Sacrificium", "Enshroud burst"),
            new("Hell's Ingress", "Forward dash"),
            new("Hell's Egress", "Backward dash"),
            new("Regress", "Return to Threshold"),
            new("Arcane Crest", "Shield and HoT"),
            new("Whorl of Death", "AoE DoT"),
            new("Shadow of Death", "Single DoT")
        };

        // Viper section
        this.sectionSettings[ConfigSection.Viper] = new List<SettingInfo>
        {
            new("Steel Fangs", "Dual wield combo"),
            new("Dread Fangs", "Dual wield combo"),
            new("Hunter's Sting", "Twinblade combo"),
            new("Swiftskin's Sting", "Twinblade combo"),
            new("Flanksting Strike", "Positional combo"),
            new("Flanksbane Fang", "Positional combo"),
            new("Hindsting Strike", "Positional combo"),
            new("Hindsbane Fang", "Positional combo"),
            new("Reawaken", "Burst phase"),
            new("Generation", "Reawaken combo"),
            new("Legacy", "Reawaken finisher"),
            new("Ouroboros", "Ultimate finisher"),
            new("Serpent's Ire", "Rattling Coil generation"),
            new("Uncoiled Fury", "Rattling Coil spender"),
            new("Uncoiled Twinfang", "Fury follow-up"),
            new("Uncoiled Twinblood", "Fury follow-up"),
            new("Slither", "Gap closer"),
            new("Writhing Snap", "Ranged attack"),
            new("Twinfang Bite", "oGCD follow-up"),
            new("Twinblood Bite", "oGCD follow-up"),
            new("Death Rattle", "oGCD finisher"),
            new("Last Lash", "AoE oGCD")
        };

        // Machinist section
        this.sectionSettings[ConfigSection.Machinist] = new List<SettingInfo>
        {
            new("Split Shot", "Basic combo"),
            new("Slug Shot", "Combo follow-up"),
            new("Clean Shot", "Combo finisher"),
            new("Heated Split Shot", "Enhanced combo"),
            new("Heated Slug Shot", "Enhanced follow-up"),
            new("Heated Clean Shot", "Enhanced finisher"),
            new("Drill", "High damage GCD"),
            new("Bioblaster", "AoE DoT"),
            new("Air Anchor", "High damage GCD"),
            new("Chain Saw", "High damage GCD"),
            new("Excavator", "Enhanced Chain Saw"),
            new("Full Metal Field", "Enhanced Barrel Stabilizer"),
            new("Hypercharge", "Heat spender"),
            new("Heat Blast", "Hypercharge GCD"),
            new("Auto Crossbow", "AoE Hypercharge GCD"),
            new("Blazing Shot", "Enhanced Heat Blast"),
            new("Wildfire", "Damage burst"),
            new("Reassemble", "Guaranteed crit/DH"),
            new("Barrel Stabilizer", "Heat generation"),
            new("Gauss Round", "oGCD damage"),
            new("Ricochet", "AoE oGCD damage"),
            new("Double Check", "Enhanced Gauss Round"),
            new("Checkmate", "Enhanced Ricochet"),
            new("Automaton Queen", "Summon turret"),
            new("Queen Overdrive", "Queen burst"),
            new("Pile Bunker", "Queen finisher"),
            new("Crowned Collider", "Enhanced Pile Bunker")
        };

        // Bard section
        this.sectionSettings[ConfigSection.Bard] = new List<SettingInfo>
        {
            new("Heavy Shot", "Basic GCD"),
            new("Straight Shot", "Proc GCD"),
            new("Burst Shot", "Enhanced Heavy Shot"),
            new("Refulgent Arrow", "Enhanced Straight Shot"),
            new("Venomous Bite", "Single DoT"),
            new("Windbite", "Single DoT"),
            new("Caustic Bite", "Enhanced Venomous Bite"),
            new("Stormbite", "Enhanced Windbite"),
            new("Iron Jaws", "Refresh DoTs"),
            new("Apex Arrow", "Soul Voice spender"),
            new("Blast Arrow", "Apex follow-up"),
            new("Resonant Arrow", "Enhanced Barrage"),
            new("Radiant Encore", "Radiant Finale follow-up"),
            new("Mage's Ballad", "Song (DoT reset)"),
            new("Army's Paeon", "Song (speed buff)"),
            new("Wanderer's Minuet", "Song (crit buff)"),
            new("Pitch Perfect", "Minuet proc spender"),
            new("Battle Voice", "Party DH buff"),
            new("Raging Strikes", "Damage buff"),
            new("Radiant Finale", "Party damage buff"),
            new("Barrage", "Triple hit"),
            new("Sidewinder", "oGCD damage"),
            new("Empyreal Arrow", "oGCD damage"),
            new("Bloodletter", "oGCD damage (Mage's Ballad)"),
            new("Rain of Death", "AoE oGCD (Mage's Ballad)"),
            new("Heartbreak Shot", "Enhanced Bloodletter"),
            new("Nature's Minne", "Healing received buff"),
            new("The Warden's Paean", "Cleanse"),
            new("Troubadour", "Party mitigation"),
            new("Quick Nock", "AoE GCD"),
            new("Ladonsbite", "Enhanced Quick Nock"),
            new("Shadowbite", "AoE proc GCD")
        };

        // Dancer section
        this.sectionSettings[ConfigSection.Dancer] = new List<SettingInfo>
        {
            new("Cascade", "Basic combo"),
            new("Fountain", "Combo follow-up"),
            new("Reverse Cascade", "Proc GCD"),
            new("Fountainfall", "Proc GCD"),
            new("Windmill", "AoE combo"),
            new("Bladeshower", "AoE follow-up"),
            new("Rising Windmill", "AoE proc"),
            new("Bloodshower", "AoE proc"),
            new("Standard Step", "Dance buff"),
            new("Standard Finish", "Dance damage"),
            new("Technical Step", "Party buff dance"),
            new("Technical Finish", "Party damage"),
            new("Tillana", "Post-Technical finisher"),
            new("Starfall Dance", "Enhanced Flourish"),
            new("Last Dance", "Post-Standard finisher"),
            new("Dance of the Dawn", "Ultimate finisher"),
            new("Finishing Move", "Standard follow-up"),
            new("Devilment", "Crit/DH buff"),
            new("Flourish", "Proc enabler"),
            new("Improvisation", "Party healing"),
            new("Fan Dance", "Feather spender"),
            new("Fan Dance II", "AoE Feather spender"),
            new("Fan Dance III", "Enhanced Fan Dance"),
            new("Fan Dance IV", "Post-Flourish"),
            new("Saber Dance", "Esprit spender"),
            new("Dance Partner", "Closed Position target"),
            new("En Avant", "Gap closer"),
            new("Curing Waltz", "AoE heal"),
            new("Shield Samba", "Party mitigation")
        };

        // Caster shared section
        this.sectionSettings[ConfigSection.CasterShared] = new List<SettingInfo>
        {
            new("Lucid Dreaming", "MP threshold for Lucid Dreaming")
        };

        // Black Mage section
        this.sectionSettings[ConfigSection.BlackMage] = new List<SettingInfo>
        {
            new("Fire", "Astral Fire spell"),
            new("Fire II", "AoE Fire"),
            new("Fire III", "Astral Fire instant"),
            new("Fire IV", "Main damage spell"),
            new("Flare", "AoE Astral finisher"),
            new("Flare Star", "Enhanced Flare"),
            new("Despair", "Astral finisher"),
            new("Blizzard", "Umbral Ice spell"),
            new("Blizzard II", "AoE Blizzard"),
            new("Blizzard III", "Umbral Ice instant"),
            new("Blizzard IV", "Umbral Hearts"),
            new("Freeze", "AoE Umbral spell"),
            new("High Fire II", "Enhanced Fire II"),
            new("High Blizzard II", "Enhanced Blizzard II"),
            new("Paradox", "Phase transition spell"),
            new("Thunder", "Single DoT"),
            new("Thunder II", "AoE DoT"),
            new("Thunder III", "Enhanced Thunder"),
            new("Thunder IV", "Enhanced Thunder II"),
            new("High Thunder", "Further enhanced Thunder"),
            new("High Thunder II", "Further enhanced Thunder II"),
            new("Xenoglossy", "Polyglot spender"),
            new("Foul", "AoE Polyglot spender"),
            new("Ley Lines", "Speed buff zone"),
            new("Between the Lines", "Teleport to Ley Lines"),
            new("Aetherial Manipulation", "Teleport to party member"),
            new("Triplecast", "Three instant casts"),
            new("Sharpcast", "Guaranteed proc"),
            new("Amplifier", "Polyglot generation"),
            new("Manaward", "Magic shield"),
            new("Manafont", "MP restoration"),
            new("Transpose", "Swap element"),
            new("Umbral Soul", "Maintain Umbral Ice")
        };

        // Summoner section
        this.sectionSettings[ConfigSection.Summoner] = new List<SettingInfo>
        {
            new("Summon Bahamut", "Demi-summon"),
            new("Summon Phoenix", "Demi-summon"),
            new("Summon Solar Bahamut", "Enhanced Demi"),
            new("Enkindle Bahamut", "Demi burst"),
            new("Enkindle Phoenix", "Demi burst"),
            new("Enkindle Solar Bahamut", "Enhanced burst"),
            new("Astral Impulse", "Bahamut GCD"),
            new("Astral Flare", "Bahamut AoE"),
            new("Deathflare", "Bahamut finisher"),
            new("Fountain of Fire", "Phoenix GCD"),
            new("Brand of Purgatory", "Phoenix AoE"),
            new("Rekindle", "Phoenix heal"),
            new("Umbral Impulse", "Solar Bahamut GCD"),
            new("Umbral Flare", "Solar Bahamut AoE"),
            new("Sunflare", "Solar Bahamut finisher"),
            new("Lux Solaris", "Solar Bahamut heal"),
            new("Summon Garuda", "Wind primal"),
            new("Summon Titan", "Earth primal"),
            new("Summon Ifrit", "Fire primal"),
            new("Slipstream", "Garuda instant"),
            new("Mountain Buster", "Titan instant"),
            new("Crimson Cyclone", "Ifrit gap closer"),
            new("Crimson Strike", "Ifrit follow-up"),
            new("Ruin", "Basic GCD"),
            new("Ruin II", "Instant GCD"),
            new("Ruin III", "Enhanced Ruin"),
            new("Ruin IV", "Proc GCD"),
            new("Outburst", "AoE GCD"),
            new("Tri-disaster", "Enhanced Outburst"),
            new("Energy Drain", "oGCD damage"),
            new("Energy Siphon", "AoE oGCD damage"),
            new("Fester", "oGCD damage"),
            new("Painflare", "AoE oGCD damage"),
            new("Necrotize", "Enhanced Fester"),
            new("Searing Flash", "Enhanced oGCD"),
            new("Searing Light", "Party damage buff"),
            new("Radiant Aegis", "Shield")
        };

        // Red Mage section
        this.sectionSettings[ConfigSection.RedMage] = new List<SettingInfo>
        {
            new("Jolt", "Basic spell"),
            new("Jolt II", "Enhanced Jolt"),
            new("Jolt III", "Further enhanced Jolt"),
            new("Verthunder", "Black mana spell"),
            new("Verthunder II", "AoE Black mana"),
            new("Verthunder III", "Enhanced Verthunder"),
            new("Veraero", "White mana spell"),
            new("Veraero II", "AoE White mana"),
            new("Veraero III", "Enhanced Veraero"),
            new("Verfire", "Black mana proc"),
            new("Verstone", "White mana proc"),
            new("Scatter", "AoE spell"),
            new("Impact", "Enhanced Scatter"),
            new("Grand Impact", "Post-Acceleration"),
            new("Riposte", "Melee combo"),
            new("Zwerchhau", "Melee follow-up"),
            new("Redoublement", "Melee finisher"),
            new("Enchanted Riposte", "Mana melee combo"),
            new("Enchanted Zwerchhau", "Mana melee follow-up"),
            new("Enchanted Redoublement", "Mana melee finisher"),
            new("Moulinet", "AoE melee"),
            new("Enchanted Moulinet", "Mana AoE melee"),
            new("Verflare", "Black finisher"),
            new("Verholy", "White finisher"),
            new("Scorch", "Post-finisher"),
            new("Resolution", "Ultimate finisher"),
            new("Prefulgence", "Enhanced Resolution"),
            new("Corps-a-corps", "Gap closer"),
            new("Engagement", "oGCD damage"),
            new("Displacement", "Backstep damage"),
            new("Fleche", "oGCD damage"),
            new("Contre Sixte", "AoE oGCD damage"),
            new("Acceleration", "Instant cast enabler"),
            new("Manafication", "Mana generation"),
            new("Embolden", "Party damage buff"),
            new("Magick Barrier", "Party mitigation"),
            new("Vercure", "Single-target heal"),
            new("Verraise", "Resurrection")
        };

        // Pictomancer section
        this.sectionSettings[ConfigSection.Pictomancer] = new List<SettingInfo>
        {
            new("Fire in Red", "Basic damage spell"),
            new("Aero in Green", "Basic damage spell"),
            new("Water in Blue", "Basic damage spell"),
            new("Fire II in Red", "AoE damage"),
            new("Aero II in Green", "AoE damage"),
            new("Water II in Blue", "AoE damage"),
            new("Blizzard in Cyan", "Enhanced damage"),
            new("Stone in Yellow", "Enhanced damage"),
            new("Thunder in Magenta", "Enhanced damage"),
            new("Blizzard II in Cyan", "Enhanced AoE"),
            new("Stone II in Yellow", "Enhanced AoE"),
            new("Thunder II in Magenta", "Enhanced AoE"),
            new("Holy in White", "Comet spender"),
            new("Comet in Black", "Post-Subtractive"),
            new("Rainbow Drip", "High damage spell"),
            new("Star Prism", "Ultimate finisher"),
            new("Creature Motif", "Paint creature"),
            new("Pom Motif", "Pom creature"),
            new("Wing Motif", "Wing creature"),
            new("Claw Motif", "Claw creature"),
            new("Maw Motif", "Maw creature"),
            new("Living Muse", "Summon creature"),
            new("Pom Muse", "Summon Pom"),
            new("Winged Muse", "Summon Wing"),
            new("Clawed Muse", "Summon Claw"),
            new("Fanged Muse", "Summon Maw"),
            new("Mog of the Ages", "Creature finisher"),
            new("Retribution of the Madeen", "Enhanced finisher"),
            new("Weapon Motif", "Paint weapon"),
            new("Hammer Motif", "Hammer weapon"),
            new("Striking Muse", "Use weapon"),
            new("Hammer Stamp", "Hammer combo"),
            new("Hammer Brush", "Hammer follow-up"),
            new("Polishing Hammer", "Hammer finisher"),
            new("Landscape Motif", "Paint landscape"),
            new("Starry Sky Motif", "Starry landscape"),
            new("Starry Muse", "Party buff"),
            new("Subtractive Palette", "Color change"),
            new("Smudge", "Movement tool"),
            new("Tempera Coat", "Shield"),
            new("Tempera Grassa", "Party shield")
        };

        // Timeline section
        this.sectionSettings[ConfigSection.Timeline] = new List<SettingInfo>
        {
            new("Enable Timeline Predictions", "Use fight timelines for precise mechanic timing"),
            new("Confidence Threshold", "Minimum confidence required before trusting timeline predictions"),
            new("Block Casts Before Mechanics", "Stop hardcast damage spells when a mechanic will hit before the cast completes")
        };
    }
}
