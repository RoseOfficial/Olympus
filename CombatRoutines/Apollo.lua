-- Apollo Combat Routine
-- A White Mage (WHM) combat routine for FFXIV
--------------------------------------------------------------------------------

--[[
    Table of Contents:
    1. Constants and Configuration
    2. Core System
    3. Combat Logic
    4. Healing Systems
    5. Damage Systems
    6. Utility Functions
    7. Event Handlers
    8. Initialization
--]]

--------------------------------------------------------------------------------
-- 1. Constants and Configuration
--------------------------------------------------------------------------------

Apollo = {}

-- Debug categories specific to Apollo
Apollo.DEBUG_CATEGORIES = {
    LILY = "Lily",
    HEALING_SINGLE = "SingleTargetHealing",
    HEALING_AOE = "AoEHealing",
    HEALING_EMERGENCY = "EmergencyHealing",
    MITIGATION = "Mitigation"
}

-- WHM-specific spell definitions with detailed documentation
Apollo.SPELLS = {
    -- Damage spells (GCD)
    -- Direct damage spells that evolve as the player levels up
    STONE = { id = 119, mp = 200, instant = false, range = 25, category = "Damage", level = 1, isGCD = true },
    STONE_II = { id = 127, mp = 200, instant = false, range = 25, category = "Damage", level = 18, isGCD = true },
    STONE_III = { id = 3568, mp = 400, instant = false, range = 25, category = "Damage", level = 54, isGCD = true },
    STONE_IV = { id = 7431, mp = 400, instant = false, range = 25, category = "Damage", level = 64, isGCD = true },
    GLARE = { id = 16533, mp = 400, instant = false, range = 25, category = "Damage", level = 72, isGCD = true },
    GLARE_III = { id = 25859, mp = 400, instant = false, range = 25, category = "Damage", level = 82, isGCD = true },

    -- Single target healing (GCD)
    -- Core healing spells for single target healing
    CURE = { id = 120, mp = 400, instant = false, range = 30, category = "Healing", level = 2, isGCD = true },
    CURE_II = { id = 135, mp = 1000, instant = false, range = 30, category = "Healing", level = 30, isGCD = true },
    CURE_III = { id = 131, mp = 1500, instant = false, range = 30, category = "Healing", level = 40, isGCD = true },
    REGEN = { id = 137, mp = 500, instant = true, range = 30, category = "Healing", level = 35, isGCD = true },

    -- Single target healing (oGCD)
    -- Emergency and supplementary healing abilities
    BENEDICTION = { id = 140, mp = 0, instant = true, range = 30, category = "Healing", level = 50, cooldown = 180, isGCD = false },
    TETRAGRAMMATON = { id = 3570, mp = 0, instant = true, range = 30, category = "Healing", level = 60, cooldown = 60, isGCD = false },
    DIVINE_BENISON = { id = 7432, mp = 0, instant = true, range = 30, category = "Healing", level = 66, cooldown = 30, isGCD = false },
    AQUAVEIL = { id = 25861, mp = 0, instant = true, range = 30, category = "Buff", level = 86, cooldown = 60, isGCD = false },

    -- AoE healing (GCD)
    -- Area effect healing spells
    MEDICA = { id = 124, mp = 1000, instant = false, range = 15, category = "Healing", level = 10, isGCD = true },
    MEDICA_II = { id = 133, mp = 1000, instant = false, range = 20, category = "Healing", level = 50, isGCD = true },

    -- AoE healing (oGCD)
    -- Area effect healing abilities and buffs
    ASYLUM = { id = 3569, mp = 0, instant = true, range = 30, category = "Healing", level = 52, cooldown = 90, isGCD = false },
    ASSIZE = { id = 3571, mp = 0, instant = true, range = 15, category = "Hybrid", level = 56, cooldown = 45, isGCD = false },
    PLENARY_INDULGENCE = { id = 7433, mp = 0, instant = true, range = 0, category = "Healing", level = 70, cooldown = 60, isGCD = false },
    LITURGY_OF_THE_BELL = { id = 25862, mp = 0, instant = true, range = 20, category = "Healing", level = 90, cooldown = 180, isGCD = false },
    TEMPERANCE = { id = 16536, mp = 0, instant = true, range = 0, category = "Buff", level = 80, cooldown = 120, isGCD = false },

    -- DoTs and AoE damage (GCD)
    -- Damage over time and area effect damage spells
    AERO = { id = 121, mp = 400, instant = true, range = 25, category = "Damage", level = 4, isGCD = true },
    AERO_II = { id = 132, mp = 400, instant = true, range = 25, category = "Damage", level = 46, isGCD = true },
    DIA = { id = 16532, mp = 400, instant = true, range = 25, category = "Damage", level = 72, isGCD = true },
    HOLY = { id = 139, mp = 400, instant = false, range = 8, category = "Damage", level = 45, isGCD = true },
    HOLY_III = { id = 25860, mp = 400, instant = false, range = 8, category = "Damage", level = 82, isGCD = true },

    -- Utility (oGCD)
    -- Support and utility abilities
    PRESENCE_OF_MIND = { id = 136, mp = 0, instant = true, range = 0, category = "Buff", level = 30, isGCD = false },
    THIN_AIR = { id = 7430, mp = 0, instant = true, range = 0, category = "Buff", level = 58, cooldown = 120, isGCD = false },
    AETHERIAL_SHIFT = { id = 37008, mp = 0, instant = true, range = 0, category = "Movement", level = 40, cooldown = 60, isGCD = false },

    -- Lily system (GCD)
    -- Special healing and damage abilities using the lily gauge
    AFFLATUS_SOLACE = { id = 16531, mp = 0, instant = true, range = 30, category = "Healing", level = 52, isGCD = true },
    AFFLATUS_RAPTURE = { id = 16534, mp = 0, instant = true, range = 20, category = "Healing", level = 76, isGCD = true },
    AFFLATUS_MISERY = { id = 16535, mp = 0, instant = false, range = 25, category = "Damage", level = 74, isGCD = true },

    LUCID_DREAMING = { id = 7562, mp = 0, instant = true, range = 0, category = "Utility", level = 70, cooldown = 60, isGCD = false }
}

-- WHM-specific buff IDs with descriptive comments
Apollo.BUFFS = {
    FREECURE = 155,      -- Allows free casting of Cure II
    MEDICA_II = 150,     -- AoE HoT effect
    REGEN = 158,         -- Single target HoT effect
    DIVINE_BENISON = 1218, -- Single target shield
    AQUAVEIL = 2708      -- Damage reduction buff
}

-- DoT buff IDs for tracking
Apollo.DOT_BUFFS = {
    [143] = true,  -- Aero
    [144] = true,  -- Aero II
    [1871] = true  -- Dia
}

-- MP Thresholds
Apollo.THRESHOLDS = {
    LUCID = 80,         -- Lucid Dreaming usage threshold
    NORMAL = 30,        -- Normal phase threshold (emergency level)
    AOE = 40,          -- AoE intensive phase threshold
    EMERGENCY = 30,    -- Emergency threshold
    CRITICAL = 15      -- Critical threshold - strict conservation
}

-- Settings with detailed explanatory comments
Apollo.SETTINGS = {
    -- Resource management
    MPThreshold = 80,           -- MP threshold for using MP recovery abilities
    HealingRange = 30,          -- Maximum range for healing spells

    -- Single target healing thresholds
    CureThreshold = 85,         -- HP threshold for using Cure (only used at low levels or when MP constrained)
    CureIIThreshold = 65,       -- HP threshold for using Cure II (primary single target heal)
    CureIIIThreshold = 50,      -- HP threshold for using Cure III (used for stack healing)
    RegenThreshold = 80,        -- HP threshold for applying Regen (proactive healing)
    BenedictionThreshold = 25,  -- HP threshold for using Benediction (emergency healing)
    TetragrammatonThreshold = 60, -- HP threshold for using Tetragrammaton (instant oGCD heal)
    BenisonThreshold = 90,      -- HP threshold for using Divine Benison (proactive shield)
    AquaveilThreshold = 85,     -- HP threshold for using Aquaveil (tank mitigation)

    -- AoE healing thresholds
    CureIIIMinTargets = 3,      -- Minimum targets for Cure III
    HolyMinTargets = 2,         -- Minimum targets for Holy (reduced for better dungeon efficiency)
    AsylumThreshold = 80,       -- HP threshold for using Asylum (ground AoE regen)
    AsylumMinTargets = 2,       -- Minimum targets for Asylum
    AssizeMinTargets = 1,       -- Minimum targets for Assize (reduced since it's also a damage ability)
    PlenaryThreshold = 65,      -- HP threshold for using Plenary Indulgence
    TemperanceThreshold = 70,   -- HP threshold for using Temperance
    LiturgyThreshold = 75,      -- HP threshold for using Liturgy of the Bell
    LiturgyMinTargets = 2       -- Minimum targets for Liturgy of the Bell
}

-- Spell toggle configuration
Apollo.SPELL_TOGGLES = {
    -- Initialize all spells as enabled by default
    enabled = {},
    -- Categories for organization in the GUI
    categories = {
        ["Damage"] = true,
        ["Healing"] = true,
        ["Buff"] = true,
        ["Utility"] = true,
        ["Movement"] = true
    }
}

-- Initialize all spells as enabled by default
for spellName, spell in pairs(Apollo.SPELLS) do
    Apollo.SPELL_TOGGLES.enabled[spellName] = true
end

-- Add common spells if they exist
if Olympus and Olympus.COMMON_SPELLS and type(Olympus.COMMON_SPELLS) == "table" then
    if Debug then
        Debug.Info(Debug.CATEGORIES.SYSTEM, "Adding common spells to Apollo spell list")
    end
    for name, spell in pairs(Olympus.COMMON_SPELLS) do
        Apollo.SPELLS[name] = spell
    end
end

-- State variables
Apollo.isRunning = false
Apollo.StrictHealing = false

Apollo.classes = {
    [FFXIV.JOBS.WHITEMAGE] = true,
    [FFXIV.JOBS.CONJURER] = true,
}

--------------------------------------------------------------------------------
-- 2. Core System
--------------------------------------------------------------------------------

-- Core system functions
function Apollo.Toggle()
    Apollo.isRunning = not Apollo.isRunning
    if Apollo.isRunning then
        Debug.Info(Debug.CATEGORIES.SYSTEM, "Apollo started")
    else
        Debug.Info(Debug.CATEGORIES.SYSTEM, "Apollo stopped")
    end
end

function Apollo.IsRunning()
    return Apollo.isRunning
end

function Apollo.IsSpellEnabled(spellName)
    return Apollo.SPELL_TOGGLES.enabled[spellName] == true
end

function Apollo.ToggleSpell(spellName)
    if Apollo.SPELLS[spellName] then
        Apollo.SPELL_TOGGLES.enabled[spellName] = not Apollo.SPELL_TOGGLES.enabled[spellName]
        Debug.Info(Debug.CATEGORIES.SYSTEM, string.format("Spell %s %s", 
            spellName, 
            Apollo.SPELL_TOGGLES.enabled[spellName] and "enabled" or "disabled"))
        return true
    end
    return false
end

function Apollo.GetSpellsByCategory(category)
    local spells = {}
    for name, spell in pairs(Apollo.SPELLS) do
        if spell.category == category then
            spells[name] = spell
        end
    end
    return spells
end

--------------------------------------------------------------------------------
-- 3. Combat Logic
--------------------------------------------------------------------------------

function Apollo.Cast()
    -- Only run if Apollo is enabled
    if not Apollo.IsRunning() then return false end

    -- MP Management (highest priority to prevent resource depletion)
    if Apollo.HandleMPConservation() then
        Olympus.Performance.IsFrameBudgetExceeded()
        return true 
    end

    -- Recovery and utility
    if Olympus.HandleSwiftcast() then 
        Olympus.Performance.IsFrameBudgetExceeded()
        return true 
    end
    if Olympus.HandleSurecast() then 
        Olympus.Performance.IsFrameBudgetExceeded()
        return true
    end
    if Olympus.HandleRescue() then 
        Olympus.Performance.IsFrameBudgetExceeded()
        return true 
    end
    if Olympus.HandleEsuna(Apollo.SETTINGS.HealingRange) then 
        Olympus.Performance.IsFrameBudgetExceeded()
        return true 
    end
    if Olympus.HandleRaise(Apollo.SETTINGS.HealingRange) then 
        Olympus.Performance.IsFrameBudgetExceeded()
        return true 
    end

    -- Core rotation with optimized priority
    if Apollo.HandleMovement() then 
        Olympus.Performance.IsFrameBudgetExceeded()
        return true 
    end
    if Apollo.HandleEmergencyHealing() then 
        Olympus.Performance.IsFrameBudgetExceeded()
        return true 
    end
    if Apollo.HandleBuffs() then
        Olympus.Performance.IsFrameBudgetExceeded()
        return true 
    end
    if Apollo.HandleMitigation() then 
        Olympus.Performance.IsFrameBudgetExceeded()
        return true 
    end
    if Apollo.HandleLilySystem() then 
        Olympus.Performance.IsFrameBudgetExceeded()
        return true 
    end
    
    -- Only handle non-essential healing if not in emergency MP state
    if Player.mp.percent > Apollo.THRESHOLDS.EMERGENCY then
        if Apollo.HandleAoEHealing() then 
            Olympus.Performance.IsFrameBudgetExceeded()
            return true 
        end
        if Apollo.HandleSingleTargetHealing() then 
            Olympus.Performance.IsFrameBudgetExceeded()
            return true 
        end
    else
        -- In emergency, only handle critical healing
        if Apollo.StrictHealing then
            local party = Olympus.GetParty(Apollo.SETTINGS.HealingRange)
            if table.valid(party) then
                for _, member in pairs(party) do
                    if member.hp.percent <= Apollo.SETTINGS.BenedictionThreshold then
                        if Apollo.HandleAoEHealing() then 
                            Olympus.Performance.IsFrameBudgetExceeded()
                            return true 
                        end
                        if Apollo.HandleSingleTargetHealing() then 
                            Olympus.Performance.IsFrameBudgetExceeded()
                            return true 
                        end
                        break
                    end
                end
            end
        end
    end
    
    -- Handle damage (continue until emergency threshold)
    if Player.mp.percent > Apollo.THRESHOLDS.EMERGENCY then
        if Apollo.HandleDamage() then 
            Olympus.Performance.IsFrameBudgetExceeded()
            return true
        end
    end

    -- Check frame budget before final return
    Olympus.Performance.IsFrameBudgetExceeded()
    return false
end

function Apollo.DetectFightPhase(party)
    Debug.TrackFunctionStart("Apollo.DetectFightPhase")
    
    if not table.valid(party) then
        Debug.Verbose(Debug.CATEGORIES.COMBAT, "No valid party, assuming normal phase")
        Debug.TrackFunctionEnd("Apollo.DetectFightPhase")
        return "NORMAL"
    end
    
    -- Count party members below AoE threshold
    local membersNeedingHeal = 0
    for _, member in pairs(party) do
        if member.hp.percent <= Apollo.SETTINGS.CureThreshold then
            membersNeedingHeal = membersNeedingHeal + 1
        end
    end
    
    -- Detect AoE intensive phase
    if membersNeedingHeal >= 3 then
        Debug.Info(Debug.CATEGORIES.COMBAT, "Detected AoE intensive phase")
        Debug.TrackFunctionEnd("Apollo.DetectFightPhase")
        return "AOE"
    end
    
    -- Detect emergency phase
    local lowestHP = 100
    for _, member in pairs(party) do
        if member.hp.percent < lowestHP then
            lowestHP = member.hp.percent
        end
    end
    
    if lowestHP <= Apollo.SETTINGS.BenedictionThreshold then
        Debug.Info(Debug.CATEGORIES.COMBAT, "Detected emergency phase")
        Debug.TrackFunctionEnd("Apollo.DetectFightPhase")
        return "EMERGENCY"
    end
    
    Debug.Verbose(Debug.CATEGORIES.COMBAT, "Normal combat phase")
    Debug.TrackFunctionEnd("Apollo.DetectFightPhase")
    return "NORMAL"
end

function Apollo.GetMPThreshold()
    Debug.TrackFunctionStart("Apollo.GetMPThreshold")
    
    local party = Olympus.GetParty(Apollo.SETTINGS.HealingRange)
    local phase = Apollo.DetectFightPhase(party)
    
    -- If MP is critically low, override phase threshold
    if Player.mp.percent <= Apollo.THRESHOLDS.CRITICAL then
        Debug.Info(Debug.CATEGORIES.COMBAT, "MP critically low, using emergency threshold")
        Debug.TrackFunctionEnd("Apollo.GetMPThreshold")
        return Apollo.THRESHOLDS.CRITICAL
    end
    
    local threshold = Apollo.THRESHOLDS[phase] or Apollo.THRESHOLDS.NORMAL
    Debug.Info(Debug.CATEGORIES.COMBAT, string.format("MP threshold for %s phase: %d", phase, threshold))
    
    Debug.TrackFunctionEnd("Apollo.GetMPThreshold")
    return threshold
end

function Apollo.ShouldUseThinAir(spellId)
    Debug.TrackFunctionStart("Apollo.ShouldUseThinAir")
    
    -- Don't use Thin Air if MP is healthy
    if Player.mp.percent > Apollo.THRESHOLDS.LUCID then
        Debug.Verbose(Debug.CATEGORIES.COMBAT, "MP healthy, saving Thin Air")
        Debug.TrackFunctionEnd("Apollo.ShouldUseThinAir")
        return false
    end
    
    -- Prioritize expensive spells
    if Apollo.EXPENSIVE_SPELLS[spellId] then
        Debug.Info(Debug.CATEGORIES.COMBAT, "Using Thin Air for expensive spell")
        Debug.TrackFunctionEnd("Apollo.ShouldUseThinAir")
        return true
    end
    
    -- Emergency MP conservation
    if Player.mp.percent <= Apollo.THRESHOLDS.EMERGENCY then
        Debug.Info(Debug.CATEGORIES.COMBAT, "Emergency MP conservation - using Thin Air")
        Debug.TrackFunctionEnd("Apollo.ShouldUseThinAir")
        return true
    end
    
    Debug.TrackFunctionEnd("Apollo.ShouldUseThinAir")
    return false
end

-- Spell MP costs for Thin Air optimization
Apollo.EXPENSIVE_SPELLS = {
    [Apollo.SPELLS.CURE_III.id] = true,     -- 1500 MP
    [Apollo.SPELLS.MEDICA.id] = true,       -- 1000 MP
    [Apollo.SPELLS.MEDICA_II.id] = true,    -- 1000 MP
    [Apollo.SPELLS.CURE_II.id] = true       -- 1000 MP
}

-- Handle MP conservation mode
function Apollo.HandleMPConservation()
    Debug.TrackFunctionStart("Apollo.HandleMPConservation")

    -- Check if we're below Lucid threshold and it's available
    if Player.mp.percent <= Apollo.THRESHOLDS.LUCID then
        local lucidDreaming = Apollo.SPELLS.LUCID_DREAMING
        
        Debug.Info(Debug.CATEGORIES.COMBAT, string.format(
            "Checking Lucid - MP: %d%%, Spell Ready: %s, Spell Enabled: %s",
            Player.mp.percent,
            tostring(Olympus.Combat.IsReady(lucidDreaming.id, lucidDreaming)),
            tostring(Apollo.IsSpellEnabled("LUCID_DREAMING"))
        ))

        if Apollo.IsSpellEnabled("LUCID_DREAMING") and Olympus.Combat.IsReady(lucidDreaming.id, lucidDreaming) then
            return Olympus.CastAction(lucidDreaming)
        end
    end
    
    Debug.TrackFunctionEnd("Apollo.HandleMPConservation")
    return false
end

function Apollo.HandleMovement()
    Debug.TrackFunctionStart("Apollo.HandleMovement")
    
    -- Handle Sprint (now only requires movement)
    if Player:IsMoving() then
        Debug.Verbose(Debug.CATEGORIES.MOVEMENT, "Player is moving, checking Sprint")
        if Olympus.HandleSprint() then 
            Debug.Info(Debug.CATEGORIES.MOVEMENT, "Sprint activated")
            Debug.TrackFunctionEnd("Apollo.HandleMovement")
            return true 
        end
    end

    -- Handle Aetherial Shift for emergency movement
    if Player.level < Apollo.SPELLS.AETHERIAL_SHIFT.level then
        Debug.Verbose(Debug.CATEGORIES.MOVEMENT, "Level too low for Aetherial Shift")
        Debug.TrackFunctionEnd("Apollo.HandleMovement")
        return false 
    end
    
    if Player.bound then
        Debug.Verbose(Debug.CATEGORIES.MOVEMENT, "Player is bound, cannot use Aetherial Shift")
        Debug.TrackFunctionEnd("Apollo.HandleMovement")
        return false 
    end

    local party = Olympus.GetParty(45)
    if table.valid(party) then
        Debug.Verbose(Debug.CATEGORIES.MOVEMENT, "Checking party members for Aetherial Shift")
        for _, member in pairs(party) do
            if member.hp.percent <= Apollo.SETTINGS.CureIIThreshold 
                and member.distance2d > Apollo.SETTINGS.HealingRange 
            and member.distance2d <= (Apollo.SETTINGS.HealingRange + 15) then
                Debug.Info(Debug.CATEGORIES.MOVEMENT, 
                    string.format("Using Aetherial Shift to reach %s (HP: %.1f%%, Distance: %.1f)", 
                        member.name or "Unknown",
                        member.hp.percent,
                        member.distance2d))
                local result = Olympus.CastAction(Apollo.SPELLS.AETHERIAL_SHIFT)
                Debug.TrackFunctionEnd("Apollo.HandleMovement")
                return result
            end
        end
    else
        Debug.Verbose(Debug.CATEGORIES.MOVEMENT, "No valid party members in extended range")
    end

    Debug.Verbose(Debug.CATEGORIES.MOVEMENT, "No movement actions needed")
    Debug.TrackFunctionEnd("Apollo.HandleMovement")
    return false
end

function Apollo.HandleLilySystem()
    Debug.TrackFunctionStart("Apollo.HandleLilySystem")
    
    -- Afflatus Misery (Blood Lily)
    if Player.level >= Apollo.SPELLS.AFFLATUS_MISERY.level then
        local bloodLilyStacks = Player.gauge[3]
        Debug.Info(Debug.CATEGORIES.COMBAT, 
            string.format("Blood Lily stacks: %d", bloodLilyStacks))
            
        if bloodLilyStacks >= 3 and Player.incombat then
            Debug.Info(Debug.CATEGORIES.COMBAT, "Blood Lily ready, looking for target")
            local target = Olympus.FindTargetForDamage(Apollo.DOT_BUFFS, Apollo.SPELLS.AFFLATUS_MISERY.range)
            if target then
                Debug.Info(Debug.CATEGORIES.COMBAT, 
                    string.format("Casting Afflatus Misery on %s", 
                        target.name or "Unknown"))
                if Olympus.CastAction(Apollo.SPELLS.AFFLATUS_MISERY, target.id) then 
                    Debug.TrackFunctionEnd("Apollo.HandleLilySystem")
                    return true 
                end
            else
                Debug.Verbose(Debug.CATEGORIES.COMBAT, "No valid target for Afflatus Misery")
            end
        end
    end

    -- Afflatus Rapture (AoE Lily)
    if Player.level >= Apollo.SPELLS.AFFLATUS_RAPTURE.level then
        local lilyStacks = Player.gauge[2]
        Debug.Info(Debug.CATEGORIES.HEALING, 
            string.format("Lily stacks: %d", lilyStacks))
            
        if lilyStacks >= 1 then
            local party = Olympus.GetParty(Apollo.SPELLS.AFFLATUS_RAPTURE.range)
            local membersNeedingHeal, _ = Olympus.HandleAoEHealCheck(party, Apollo.SETTINGS.CureThreshold, Apollo.SPELLS.AFFLATUS_RAPTURE.range)
            
            Debug.Info(Debug.CATEGORIES.HEALING, 
                string.format("Afflatus Rapture check - Members needing heal: %d", 
                    membersNeedingHeal))
                    
            if membersNeedingHeal >= 3 then
                Debug.Info(Debug.CATEGORIES.HEALING, "Casting Afflatus Rapture")
                if Olympus.CastAction(Apollo.SPELLS.AFFLATUS_RAPTURE) then 
                    Debug.TrackFunctionEnd("Apollo.HandleLilySystem")
                    return true 
                end
            end
        end
    end

    -- Afflatus Solace (Single Target Lily)
    if Player.level >= Apollo.SPELLS.AFFLATUS_SOLACE.level then
        local lilyStacks = Player.gauge[2]
        if lilyStacks >= 1 then
            Debug.Verbose(Debug.CATEGORIES.HEALING, "Checking for Afflatus Solace targets")
            
            local party = Olympus.GetParty(Apollo.SETTINGS.HealingRange)
            if table.valid(party) then
                local lowestHP = 100
                local lowestMember = nil
                for _, member in pairs(party) do
                    if member.hp.percent < lowestHP and member.distance2d <= Apollo.SPELLS.AFFLATUS_SOLACE.range then
                        lowestHP = member.hp.percent
                        lowestMember = member
                    end
                end
                
                -- Prioritize Afflatus Solace over Cure II when lilies are available
                if lowestMember and lowestHP <= Apollo.SETTINGS.CureIIThreshold then
                    Debug.Info(Debug.CATEGORIES.HEALING, 
                        string.format("Casting Afflatus Solace on %s (HP: %.1f%%)", 
                            lowestMember.name or "Unknown",
                            lowestHP))
                    if Olympus.CastAction(Apollo.SPELLS.AFFLATUS_SOLACE, lowestMember.id) then 
                        Debug.TrackFunctionEnd("Apollo.HandleLilySystem")
                        return true 
                    end
                else
                    Debug.Verbose(Debug.CATEGORIES.HEALING, "No suitable target for Afflatus Solace")
                end
            else
                Debug.Verbose(Debug.CATEGORIES.HEALING, "No valid party members in range")
            end
        else
            Debug.Verbose(Debug.CATEGORIES.HEALING, "No lily stacks available")
        end
    end

    Debug.Verbose(Debug.CATEGORIES.HEALING, "No lily actions needed")
    Debug.TrackFunctionEnd("Apollo.HandleLilySystem")
    return false
end

function Apollo.HandleStackHealing(party)
    if Player.level >= Apollo.SPELLS.CURE_III.level then
        local closeParty = Olympus.GetParty(10)
        local membersNeedingHeal, lowestMember = Olympus.HandleAoEHealCheck(closeParty, Apollo.SETTINGS.CureIIIThreshold, 10)
        Debug.Info(Debug.CATEGORIES.HEALING, 
            string.format("Cure III check - Close members needing heal: %d", membersNeedingHeal))
        if membersNeedingHeal >= Apollo.SETTINGS.CureIIIMinTargets and lowestMember then
            Apollo.Healing.Utils.HandleThinAir(Apollo.SPELLS.CURE_III.id)
            Debug.Info(Debug.CATEGORIES.HEALING, 
                string.format("Cure III target found: %s", lowestMember.name or "Unknown"))
            Apollo.SPELLS.CURE_III.isAoE = true
            if Olympus.CastAction(Apollo.SPELLS.CURE_III, lowestMember.id) then return true end
        end
    end
    return false
end

function Apollo.HandleGroundTargetedHealing(party)
    -- Asylum
    if Player.level >= Apollo.SPELLS.ASYLUM.level then
        Debug.Verbose(Debug.CATEGORIES.HEALING, "Checking Asylum conditions")
        Apollo.SPELLS.ASYLUM.isAoE = true
        if Apollo.HandleGroundTargetedSpell(Apollo.SPELLS.ASYLUM, party, Apollo.SETTINGS.AsylumThreshold, Apollo.SETTINGS.AsylumMinTargets) then
            return true
        end
    end

    -- Liturgy of the Bell
    if Player.level >= Apollo.SPELLS.LITURGY_OF_THE_BELL.level then
        Debug.Verbose(Debug.CATEGORIES.HEALING, "Checking Liturgy conditions")
        Apollo.SPELLS.LITURGY_OF_THE_BELL.isAoE = true
        if Apollo.HandleGroundTargetedSpell(Apollo.SPELLS.LITURGY_OF_THE_BELL, party, Apollo.SETTINGS.LiturgyThreshold, Apollo.SETTINGS.LiturgyMinTargets) then
            return true
        end
    end
    
    return false
end

function Apollo.HandleAoEHealing()
    Debug.TrackFunctionStart("Apollo.HandleAoEHealing")
    
    local party = Apollo.ValidateParty()
    if not party then 
        Debug.TrackFunctionEnd("Apollo.HandleAoEHealing")
        return false 
    end

    -- Skip non-essential AoE healing in strict healing mode
    if Apollo.StrictHealing then
        Debug.Info(Debug.CATEGORIES.HEALING, "Strict healing mode - skipping non-essential AoE healing")
        Debug.TrackFunctionEnd("Apollo.HandleAoEHealing")
        return false
    end

    -- Plenary Indulgence
    if Player.level >= Apollo.SPELLS.PLENARY_INDULGENCE.level then
        local membersNeedingHeal, _ = Olympus.HandleAoEHealCheck(party, Apollo.SETTINGS.PlenaryThreshold, Apollo.SETTINGS.HealingRange)
        Debug.Info(Debug.CATEGORIES.HEALING, 
            string.format("Plenary check - Members needing heal: %d", membersNeedingHeal))
        if membersNeedingHeal >= 2 then
            Apollo.SPELLS.PLENARY_INDULGENCE.isAoE = true
            if Olympus.CastAction(Apollo.SPELLS.PLENARY_INDULGENCE) then 
                Debug.TrackFunctionEnd("Apollo.HandleAoEHealing")
                return true 
            end
        end
    end

    -- Handle stack healing (Cure III)
    if Apollo.HandleStackHealing(party) then
        Debug.TrackFunctionEnd("Apollo.HandleAoEHealing")
        return true
    end

    -- Handle ground targeted healing (Asylum, Liturgy)
    if Apollo.HandleGroundTargetedHealing(party) then
        Debug.TrackFunctionEnd("Apollo.HandleAoEHealing")
        return true
    end

    -- Medica II and Medica
    local hasMedicaII = Olympus.Combat.HasBuff(Player, Apollo.BUFFS.MEDICA_II)
    local membersNeedingHeal, _ = Olympus.HandleAoEHealCheck(party, Apollo.SETTINGS.CureThreshold, Apollo.SPELLS.MEDICA_II.range)
    
    Debug.Info(Debug.CATEGORIES.HEALING, 
        string.format("Medica check - Members needing heal: %d, Medica II active: %s", 
            membersNeedingHeal,
            tostring(hasMedicaII)))

    if membersNeedingHeal >= 3 then
        if not hasMedicaII and Player.level >= Apollo.SPELLS.MEDICA_II.level then
            Apollo.HandleThinAir(Apollo.SPELLS.MEDICA_II.id)
            Debug.Info(Debug.CATEGORIES.HEALING, "Casting Medica II")
            Apollo.SPELLS.MEDICA_II.isAoE = true
            if Olympus.CastAction(Apollo.SPELLS.MEDICA_II) then 
                Debug.TrackFunctionEnd("Apollo.HandleAoEHealing")
                return true 
            end
        elseif hasMedicaII and Player.level >= Apollo.SPELLS.MEDICA.level then
            Apollo.HandleThinAir(Apollo.SPELLS.MEDICA.id)
            Debug.Info(Debug.CATEGORIES.HEALING, "Casting Medica")
            Apollo.SPELLS.MEDICA.isAoE = true
            if Olympus.CastAction(Apollo.SPELLS.MEDICA) then 
                Debug.TrackFunctionEnd("Apollo.HandleAoEHealing")
                return true 
            end
        end
    end

    Debug.Verbose(Debug.CATEGORIES.HEALING, "No AoE healing needed")
    Debug.TrackFunctionEnd("Apollo.HandleAoEHealing")
    return false
end

function Apollo.HandleRegen(member, memberHP)
    if not Apollo.StrictHealing and Player.level >= Apollo.SPELLS.REGEN.level 
       and memberHP <= Apollo.SETTINGS.RegenThreshold
       and not Olympus.Combat.HasBuff(member, Apollo.BUFFS.REGEN) then
        if member.role == "TANK" or memberHP <= (Apollo.SETTINGS.RegenThreshold - 10) then
            Debug.Info(Debug.CATEGORIES.HEALING, "Applying Regen")
            Apollo.SPELLS.REGEN.isAoE = false
            return Olympus.CastAction(Apollo.SPELLS.REGEN, member.id)
        end
    end
    return false
end

function Apollo.HandleCureSpells(member, memberHP)
    -- Cure II (primary single target heal)
    if memberHP <= Apollo.SETTINGS.CureIIThreshold and Player.level >= Apollo.SPELLS.CURE_II.level then
        Apollo.HandleThinAir(Apollo.SPELLS.CURE_II.id)
        Debug.Info(Debug.CATEGORIES.HEALING, "Casting Cure II")
        Apollo.SPELLS.CURE_II.isAoE = false
        if Olympus.CastAction(Apollo.SPELLS.CURE_II, member.id) then return true end
    end

    -- Cure (only use at low levels or when MP constrained)
    if (Player.level < Apollo.SPELLS.CURE_II.level or Player.mp.percent < Apollo.SETTINGS.MPThreshold) 
        and memberHP <= Apollo.SETTINGS.CureThreshold then
        -- Use Cure II if Freecure proc is active
        if Olympus.HasBuff(Player, Apollo.BUFFS.FREECURE) and Player.level >= Apollo.SPELLS.CURE_II.level then
            Debug.Info(Debug.CATEGORIES.HEALING, "Casting Cure II (Freecure)")
            Apollo.SPELLS.CURE_II.isAoE = false
            if Olympus.CastAction(Apollo.SPELLS.CURE_II, member.id) then return true end
        else
            Debug.Info(Debug.CATEGORIES.HEALING, "Casting Cure")
            Apollo.SPELLS.CURE.isAoE = false
            if Olympus.CastAction(Apollo.SPELLS.CURE, member.id) then return true end
        end
    end
    
    return false
end

function Apollo.HandleSingleTargetHealing()
    Debug.TrackFunctionStart("Apollo.SingleTargetHealing.Handle")
    
    local party = Apollo.ValidateParty()
    if not party then 
        Debug.TrackFunctionEnd("Apollo.SingleTargetHealing.Handle")
        return false 
    end

    local lowestMember, lowestHP = Apollo.FindLowestHealthMember(party)
    if lowestMember then
        -- Handle Regen
        if Apollo.HandleRegen(lowestMember, lowestHP) then
            Debug.TrackFunctionEnd("Apollo.SingleTargetHealing.Handle")
            return true
        end

        -- Handle Cure spells
        if Apollo.HandleCureSpells(lowestMember, lowestHP) then
            Debug.TrackFunctionEnd("Apollo.SingleTargetHealing.Handle")
            return true
        end
    else
        Debug.Verbose(Debug.CATEGORIES.HEALING, "No healing targets found")
    end

    Debug.TrackFunctionEnd("Apollo.SingleTargetHealing.Handle")
    return false
end

function Apollo.HandleEmergencyHealing()
    Debug.TrackFunctionStart("Apollo.EmergencyHealing.Handle")
    
    local party = Apollo.ValidateParty()
    if not party then 
        Debug.TrackFunctionEnd("Apollo.EmergencyHealing.Handle")
        return false 
    end

    -- Benediction
    if Player.level >= Apollo.SPELLS.BENEDICTION.level then
        Debug.Verbose(Debug.CATEGORIES.HEALING, "Checking for Benediction targets")
        for _, member in pairs(party) do
            if member.hp.percent <= Apollo.SETTINGS.BenedictionThreshold 
               and member.distance2d <= Apollo.SPELLS.BENEDICTION.range then
                Debug.Info(Debug.CATEGORIES.HEALING, 
                    string.format("Benediction target found: %s (HP: %.1f%%)", 
                        member.name or "Unknown",
                        member.hp.percent))
                Apollo.SPELLS.BENEDICTION.isAoE = false
                if Olympus.CastAction(Apollo.SPELLS.BENEDICTION, member.id) then 
                    Debug.TrackFunctionEnd("Apollo.EmergencyHealing.Handle")
                    return true 
                end
            end
        end
    end

    -- Tetragrammaton
    if Player.level >= Apollo.SPELLS.TETRAGRAMMATON.level then
        Debug.Verbose(Debug.CATEGORIES.HEALING, "Checking for Tetragrammaton targets")
        for _, member in pairs(party) do
            if member.hp.percent <= Apollo.SETTINGS.TetragrammatonThreshold 
               and member.distance2d <= Apollo.SPELLS.TETRAGRAMMATON.range then
                Debug.Info(Debug.CATEGORIES.HEALING, 
                    string.format("Tetragrammaton target found: %s (HP: %.1f%%)", 
                        member.name or "Unknown",
                        member.hp.percent))
                Apollo.SPELLS.TETRAGRAMMATON.isAoE = false
                if Olympus.CastAction(Apollo.SPELLS.TETRAGRAMMATON, member.id) then 
                    Debug.TrackFunctionEnd("Apollo.EmergencyHealing.Handle")
                    return true 
                end
            end
        end
    end

    Debug.Verbose(Debug.CATEGORIES.HEALING, "No emergency healing needed")
    Debug.TrackFunctionEnd("Apollo.EmergencyHealing.Handle")
    return false
end

function Apollo.HandleTankMitigation(party)
    -- Aquaveil
    if Player.level >= Apollo.SPELLS.AQUAVEIL.level then
        Debug.Verbose(Debug.CATEGORIES.HEALING, "Checking for Aquaveil targets")
        for _, member in pairs(party) do
            if member.hp.percent <= Apollo.SETTINGS.AquaveilThreshold 
               and member.distance2d <= Apollo.SPELLS.AQUAVEIL.range
               and not Olympus.Combat.HasBuff(member, Apollo.BUFFS.AQUAVEIL)
               and member.role == "TANK" then
                Debug.Info(Debug.CATEGORIES.HEALING, 
                    string.format("Aquaveil target found: %s (Tank, HP: %.1f%%)", 
                        member.name or "Unknown",
                        member.hp.percent))
                Apollo.SPELLS.AQUAVEIL.isAoE = false
                if Olympus.CastAction(Apollo.SPELLS.AQUAVEIL, member.id) then return true end
            end
        end
    end

    -- Divine Benison
    if Player.level >= Apollo.SPELLS.DIVINE_BENISON.level then
        Debug.Verbose(Debug.CATEGORIES.HEALING, "Checking for Divine Benison targets")
        for _, member in pairs(party) do
            if member.hp.percent <= Apollo.SETTINGS.BenisonThreshold 
               and member.distance2d <= Apollo.SPELLS.DIVINE_BENISON.range
               and not Olympus.Combat.HasBuff(member, Apollo.BUFFS.DIVINE_BENISON)
               and member.role == "TANK" then
                Debug.Info(Debug.CATEGORIES.HEALING, 
                    string.format("Divine Benison target found: %s (Tank, HP: %.1f%%)", 
                        member.name or "Unknown",
                        member.hp.percent))
                Apollo.SPELLS.DIVINE_BENISON.isAoE = false
                if Olympus.CastAction(Apollo.SPELLS.DIVINE_BENISON, member.id) then return true end
            end
        end
    end
    
    return false
end

function Apollo.HandleMitigation()
    Debug.TrackFunctionStart("Apollo.Mitigation.Handle")
    
    if not Player.incombat then 
        Debug.Verbose(Debug.CATEGORIES.HEALING, "Not in combat, skipping mitigation")
        Debug.TrackFunctionEnd("Apollo.Mitigation.Handle")
        return false 
    end

    local party = Apollo.ValidateParty()
    if not party then 
        Debug.TrackFunctionEnd("Apollo.Mitigation.Handle")
        return false 
    end

    -- Skip non-essential mitigation in strict healing mode
    if Apollo.StrictHealing then
        Debug.Info(Debug.CATEGORIES.HEALING, "Strict healing mode - skipping non-essential mitigation")
        Debug.TrackFunctionEnd("Apollo.Mitigation.Handle")
        return false
    end

    -- Temperance
    if Player.level >= Apollo.SPELLS.TEMPERANCE.level then
        local membersNeedingHeal, _ = Olympus.HandleAoEHealCheck(party, Apollo.SETTINGS.TemperanceThreshold, Apollo.SETTINGS.HealingRange)
        Debug.Info(Debug.CATEGORIES.HEALING, 
            string.format("Temperance check - Members needing heal: %d", membersNeedingHeal))
        if membersNeedingHeal >= 2 then
            Apollo.SPELLS.TEMPERANCE.isAoE = true
            if Olympus.CastAction(Apollo.SPELLS.TEMPERANCE) then 
                Debug.TrackFunctionEnd("Apollo.Mitigation.Handle")
                return true 
            end
        end
    end

    -- Handle tank-specific mitigation
    if Apollo.HandleTankMitigation(party) then
        Debug.TrackFunctionEnd("Apollo.Mitigation.Handle")
        return true
    end

    Debug.Verbose(Debug.CATEGORIES.HEALING, "No mitigation needed")
    Debug.TrackFunctionEnd("Apollo.Mitigation.Handle")
    return false
end

function Apollo.HandleBuffs()
    Debug.TrackFunctionStart("Apollo.HandleBuffs")
    
    if not Player.incombat then 
        Debug.Verbose(Debug.CATEGORIES.BUFFS, "Not in combat, skipping buffs")
        Debug.TrackFunctionEnd("Apollo.HandleBuffs")
        return false 
    end

    -- Presence of Mind
    if Player.level >= Apollo.SPELLS.PRESENCE_OF_MIND.level then
        local enemies = EntityList("alive,attackable,incombat,maxdistance=25")
        if table.valid(enemies) then
            Debug.Info(Debug.CATEGORIES.BUFFS, "Attempting to cast Presence of Mind")
            if Olympus.CastAction(Apollo.SPELLS.PRESENCE_OF_MIND) then 
                Debug.TrackFunctionEnd("Apollo.HandleBuffs")
                return true 
            end
        else
            Debug.Verbose(Debug.CATEGORIES.BUFFS, "No valid enemies for Presence of Mind")
        end
    else
        Debug.Verbose(Debug.CATEGORIES.BUFFS, "Level too low for Presence of Mind")
    end

    -- Thin Air
    if Player.level >= Apollo.SPELLS.THIN_AIR.level then
        if Player.mp.percent <= Apollo.SETTINGS.MPThreshold then
            Debug.Info(Debug.CATEGORIES.BUFFS, 
                string.format("MP below threshold (%.1f%%), attempting Thin Air", 
                    Player.mp.percent))
            if Olympus.CastAction(Apollo.SPELLS.THIN_AIR) then 
                Debug.TrackFunctionEnd("Apollo.HandleBuffs")
                return true 
            end
        else
            Debug.Verbose(Debug.CATEGORIES.BUFFS, 
                string.format("MP sufficient (%.1f%%), skipping Thin Air", 
                    Player.mp.percent))
        end
    else
        Debug.Verbose(Debug.CATEGORIES.BUFFS, "Level too low for Thin Air")
    end

    Debug.Verbose(Debug.CATEGORIES.BUFFS, "No buffs needed")
    Debug.TrackFunctionEnd("Apollo.HandleBuffs")
    return false
end

--------------------------------------------------------------------------------
-- 5. Damage Systems
--------------------------------------------------------------------------------

function Apollo.GetDamageSpell()
    Debug.TrackFunctionStart("Apollo.GetDamageSpell")
    
    local spells = { 
        Apollo.SPELLS.GLARE_III, 
        Apollo.SPELLS.GLARE, 
        Apollo.SPELLS.STONE_IV, 
        Apollo.SPELLS.STONE_III, 
        Apollo.SPELLS.STONE_II, 
        Apollo.SPELLS.STONE 
    }
    
    Debug.Verbose(Debug.CATEGORIES.COMBAT, "Getting highest level damage spell")
    local spell = Olympus.GetHighestLevelSpell(spells)
    
    Debug.Info(Debug.CATEGORIES.COMBAT, 
        string.format("Selected damage spell: %s (Level %d)", 
            spell.name or "Unknown",
            spell.level))
            
    Debug.TrackFunctionEnd("Apollo.GetDamageSpell")
    return spell
end

function Apollo.GetDoTSpell()
    Debug.TrackFunctionStart("Apollo.GetDoTSpell")
    
    local spells = { 
        Apollo.SPELLS.DIA, 
        Apollo.SPELLS.AERO_II, 
        Apollo.SPELLS.AERO 
    }
    
    Debug.Verbose(Debug.CATEGORIES.COMBAT, "Getting highest level DoT spell")
    local spell = Olympus.GetHighestLevelSpell(spells)
    
    Debug.Info(Debug.CATEGORIES.COMBAT, 
        string.format("Selected DoT spell: %s (Level %d)", 
            spell.name or "Unknown",
            spell.level))
            
    Debug.TrackFunctionEnd("Apollo.GetDoTSpell")
    return spell
end

function Apollo.HandleDoTs(target)
    Debug.TrackFunctionStart("Apollo.HandleDoTs")
    
    -- Check if target already has any DoT
    local hasAnyDoT = false
    for buffId, _ in pairs(Apollo.DOT_BUFFS) do
        if Olympus.Combat.HasBuff(target, buffId) then
            hasAnyDoT = true
            break
        end
    end

    if not hasAnyDoT then
        -- Dia
        if Player.level >= Apollo.SPELLS.DIA.level then
            Debug.Info(Debug.CATEGORIES.DAMAGE, 
                string.format("Applying Dia to %s", target.name or "Unknown"))
            Apollo.SPELLS.DIA.isAoE = false
            if Olympus.CastAction(Apollo.SPELLS.DIA, target.id) then 
                Debug.TrackFunctionEnd("Apollo.HandleDoTs")
                return true 
            end
        -- Aero II
        elseif Player.level >= Apollo.SPELLS.AERO_II.level then
            Debug.Info(Debug.CATEGORIES.DAMAGE, 
                string.format("Applying Aero II to %s", target.name or "Unknown"))
            Apollo.SPELLS.AERO_II.isAoE = false
            if Olympus.CastAction(Apollo.SPELLS.AERO_II, target.id) then 
                Debug.TrackFunctionEnd("Apollo.HandleDoTs")
                return true 
            end
        -- Aero
        elseif Player.level >= Apollo.SPELLS.AERO.level then
            Debug.Info(Debug.CATEGORIES.DAMAGE, 
                string.format("Applying Aero to %s", target.name or "Unknown"))
            Apollo.SPELLS.AERO.isAoE = false
            if Olympus.CastAction(Apollo.SPELLS.AERO, target.id) then 
                Debug.TrackFunctionEnd("Apollo.HandleDoTs")
                return true 
            end
        end
    end
    
    Debug.TrackFunctionEnd("Apollo.HandleDoTs")
    return false
end

function Apollo.HandleAoE(target)
    Debug.TrackFunctionStart("Apollo.HandleAoE")
    
    -- Holy
    if Player.level >= Apollo.SPELLS.HOLY.level then
        local enemies = EntityList("alive,attackable,incombat,maxdistance=8")
        if table.valid(enemies) and table.size(enemies) >= 3 then
            Debug.Info(Debug.CATEGORIES.DAMAGE, 
                string.format("Casting Holy on %d enemies", table.size(enemies)))
            Apollo.SPELLS.HOLY.isAoE = true
            if Olympus.CastAction(Apollo.SPELLS.HOLY) then 
                Debug.TrackFunctionEnd("Apollo.HandleAoE")
                return true 
            end
        end
    end
    
    -- Assize
    if Player.level >= Apollo.SPELLS.ASSIZE.level then
        local enemies = EntityList("alive,attackable,incombat,maxdistance=15")
        if table.valid(enemies) then
            Debug.Info(Debug.CATEGORIES.DAMAGE, "Casting Assize")
            Apollo.SPELLS.ASSIZE.isAoE = true
            if Olympus.CastAction(Apollo.SPELLS.ASSIZE) then 
                Debug.TrackFunctionEnd("Apollo.HandleAoE")
                return true 
            end
        end
    end
    
    Debug.TrackFunctionEnd("Apollo.HandleAoE")
    return false
end

function Apollo.HandleDamage()
    Debug.TrackFunctionStart("Apollo.HandleDamage")
    
    if not Player.incombat then 
        Debug.Verbose(Debug.CATEGORIES.DAMAGE, "Not in combat, skipping damage")
        Debug.TrackFunctionEnd("Apollo.HandleDamage")
        return false 
    end
    
    -- Skip damage in strict healing mode
    if Apollo.StrictHealing then
        Debug.Info(Debug.CATEGORIES.DAMAGE, "Strict healing mode - skipping damage")
        Debug.TrackFunctionEnd("Apollo.HandleDamage")
        return false
    end
    
    -- Find valid target
    local target = Olympus.FindTargetForDamage(Apollo.DOT_BUFFS, 25)
    if not target then
        Debug.Verbose(Debug.CATEGORIES.DAMAGE, "No valid damage target")
        Debug.TrackFunctionEnd("Apollo.HandleDamage")
        return false
    end
    
    -- Handle DoTs
    if Apollo.HandleDoTs(target) then
        Debug.TrackFunctionEnd("Apollo.HandleDamage")
        return true
    end
    
    -- Handle AoE damage
    if Apollo.HandleAoE(target) then
        Debug.TrackFunctionEnd("Apollo.HandleDamage")
        return true
    end
    
    -- Stone/Glare (single target damage)
    if Player.level >= Apollo.SPELLS.GLARE.level then
        Debug.Info(Debug.CATEGORIES.DAMAGE, 
            string.format("Casting Glare on %s", target.name or "Unknown"))
        Apollo.SPELLS.GLARE.isAoE = false
        if Olympus.CastAction(Apollo.SPELLS.GLARE, target.id) then 
            Debug.TrackFunctionEnd("Apollo.HandleDamage")
            return true 
        end
    elseif Player.level >= Apollo.SPELLS.STONE.level then
        Debug.Info(Debug.CATEGORIES.DAMAGE, 
            string.format("Casting Stone on %s", target.name or "Unknown"))
        Apollo.SPELLS.STONE.isAoE = false
        if Olympus.CastAction(Apollo.SPELLS.STONE, target.id) then 
            Debug.TrackFunctionEnd("Apollo.HandleDamage")
            return true 
        end
    end
    
    Debug.Verbose(Debug.CATEGORIES.DAMAGE, "No damage actions needed")
    Debug.TrackFunctionEnd("Apollo.HandleDamage")
    return false
end

-- Update ValidateParty to match the utilities version
function Apollo.ValidateParty(range)
    Debug.TrackFunctionStart("Apollo.ValidateParty")
    local party = Olympus.GetParty(range or Apollo.SETTINGS.HealingRange)
    if not table.valid(party) then 
        Debug.Verbose(Debug.CATEGORIES.HEALING, "No valid party members in range")
        Debug.TrackFunctionEnd("Apollo.ValidateParty")
        return nil
    end
    Debug.TrackFunctionEnd("Apollo.ValidateParty")
    return party
end

-- Update FindLowestHealthMember to match the utilities version
function Apollo.FindLowestHealthMember(party)
    Debug.TrackFunctionStart("Apollo.FindLowestHealthMember")
    local lowestHP = 100
    local lowestMember = nil
    
    for _, member in pairs(party) do
        if member.hp.percent < lowestHP and member.distance2d <= Apollo.SETTINGS.HealingRange then
            lowestHP = member.hp.percent
            lowestMember = member
        end
    end
    
    if lowestMember then
        Debug.Info(Debug.CATEGORIES.HEALING, 
            string.format("Lowest member: %s (HP: %.1f%%)", 
                lowestMember.name or "Unknown",
                lowestHP))
    end
    
    Debug.TrackFunctionEnd("Apollo.FindLowestHealthMember")
    return lowestMember, lowestHP
end

-- Handle Thin Air usage for expensive spells
function Apollo.HandleThinAir(spellId)
    Debug.TrackFunctionStart("Apollo.HandleThinAir")
    
    if Player.level >= Apollo.SPELLS.THIN_AIR.level 
        and Apollo.ShouldUseThinAir(spellId) 
        and Olympus.Combat.IsReady(Apollo.SPELLS.THIN_AIR.id, Apollo.SPELLS.THIN_AIR) then
        Debug.Info(Debug.CATEGORIES.COMBAT, "Using Thin Air before expensive spell")
        Olympus.CastAction(Apollo.SPELLS.THIN_AIR)
    end
    
    Debug.TrackFunctionEnd("Apollo.HandleThinAir")
end

function Apollo.HandleGroundTargetedSpell(spell, party, hpThreshold, minTargets)
    Debug.TrackFunctionStart("Apollo.HandleGroundTargetedSpell")
    
    local membersNeedingHeal, _ = Olympus.HandleAoEHealCheck(party, hpThreshold, spell.range)
    
    Debug.Info(Debug.CATEGORIES.HEALING, 
        string.format("Ground AoE check - Spell: %s, Members needing heal: %d, Required: %d", 
            spell.name or "Unknown",
            membersNeedingHeal,
            minTargets))

    if membersNeedingHeal >= minTargets then
        local centerX, centerZ = 0, 0
        local memberCount = 0

        for _, member in pairs(party) do
            if member.hp.percent <= hpThreshold then
                centerX = centerX + member.pos.x
                centerZ = centerZ + member.pos.z
                memberCount = memberCount + 1
            end
        end

        centerX = centerX / memberCount
        centerZ = centerZ / memberCount

        Debug.Info(Debug.CATEGORIES.HEALING, 
            string.format("Calculated AoE center position: X=%.2f, Z=%.2f", 
                centerX, 
                centerZ))

        local action = ActionList:Get(1, spell.id)
        if action and action:IsReady() then
            Debug.Info(Debug.CATEGORIES.HEALING, 
                string.format("Casting %s at calculated position", 
                    spell.name or "Unknown"))
            local result = action:Cast(centerX, Player.pos.y, centerZ)
            Debug.TrackFunctionEnd("Apollo.HandleGroundTargetedSpell")
            return result
        else
            Debug.Warn(Debug.CATEGORIES.HEALING, "Action not ready or invalid")
        end
    else
        Debug.Verbose(Debug.CATEGORIES.HEALING, "Not enough targets for ground AoE")
    end
    
    Debug.TrackFunctionEnd("Apollo.HandleGroundTargetedSpell")
    return false
end

-- Initialize Apollo
function Apollo.Initialize()
    Debug.TrackFunctionStart("Apollo.Initialize")
    
    -- Register event handlers
    RegisterEventHandler("Gameloop.Draw", Apollo.OnDraw, "Apollo.OnDraw")
    RegisterEventHandler("Gameloop.Update", Apollo.OnUpdate, "Apollo.OnUpdate")
    RegisterEventHandler("Module.Unload", Apollo.OnUnload, "Apollo.OnUnload")
    
    -- Initialize state
    Apollo.isRunning = false
    Apollo.StrictHealing = false
    
    -- Load saved settings with error handling
    local success = Apollo.LoadSettings()
    if not success then
        Debug.Warn(Debug.CATEGORIES.SYSTEM, "Failed to load settings, using defaults")
    end
    
    Debug.Info(Debug.CATEGORIES.SYSTEM, "Apollo initialized")
    Debug.TrackFunctionEnd("Apollo.Initialize")
    return true
end

-- Event Handlers
function Apollo.OnDraw()
    if not Apollo.isRunning then return end
    -- Handle UI drawing here
end

function Apollo.OnUpdate()
    if not Apollo.isRunning then return end
    Apollo.Cast()
end

function Apollo.OnUnload()
    -- Save settings
    Apollo.SaveSettings()
    
    -- Unregister events
    UnregisterEventHandler("Gameloop.Draw", "Apollo.OnDraw")
    UnregisterEventHandler("Gameloop.Update", "Apollo.OnUpdate")
    UnregisterEventHandler("Module.Unload", "Apollo.OnUnload")
end

-- Settings Management
function Apollo.LoadSettings()
    Debug.TrackFunctionStart("Apollo.LoadSettings")
    
    local success, settings = pcall(function()
        return FileLoad(GetLuaModsPath() .. [[/Project Remedy/Settings/Apollo.lua]])
    end)
    
    if success and settings then
        Apollo.SETTINGS = settings
        Debug.Info(Debug.CATEGORIES.SYSTEM, "Settings loaded")
        Debug.TrackFunctionEnd("Apollo.LoadSettings")
        return true
    else
        Debug.Info(Debug.CATEGORIES.SYSTEM, "No saved settings found or error loading, using defaults")
        Debug.TrackFunctionEnd("Apollo.LoadSettings")
        return false
    end
end

function Apollo.SaveSettings()
    Debug.TrackFunctionStart("Apollo.SaveSettings")
    
    local success, error = pcall(function()
        FileSave(GetLuaModsPath() .. [[/Project Remedy/Settings/Apollo.lua]], Apollo.SETTINGS)
    end)
    
    if success then
        Debug.Info(Debug.CATEGORIES.SYSTEM, "Settings saved")
    else
        Debug.Error(Debug.CATEGORIES.SYSTEM, "Failed to save settings: " .. tostring(error))
    end
    
    Debug.TrackFunctionEnd("Apollo.SaveSettings")
    return success
end

-- Initialize Apollo when the file is loaded, but only if dependencies are available
local init_success = Apollo.Initialize()
if not init_success then
    Debug.Error(Debug.CATEGORIES.SYSTEM, "Apollo failed to initialize properly")
end

return Apollo