Olympus = Olympus or {}

-- Constants
Olympus.BUFF_IDS = {
    SWIFTCAST = 167,
    -- Add other buff IDs here
}

Olympus.COMMON_SPELLS = {
    SPRINT = { id = 3, level = 1, isGCD = false, mp = 0 },
    SWIFTCAST = { id = 7561, level = 18, isGCD = false, cooldown = 60, mp = 0 },
    SURECAST = { id = 7559, level = 44, isGCD = false, cooldown = 120, mp = 0 },
    RESCUE = { id = 7571, level = 48, isGCD = false, cooldown = 120, range = 30, mp = 0 },
    ESUNA = { id = 7568, level = 10, isGCD = true, range = 30, mp = 400 },
    RAISE = { id = 125, level = 12, isGCD = true, range = 30, mp = 2400 },
    REPOSE = { id = 16560, level = 8, isGCD = true, range = 30, mp = 400 }
}

-- Initialize core systems
function Olympus.Initialize()
    Debug.TrackFunctionStart("Olympus.Initialize")
    
    -- Initialize performance monitoring with default settings
    Olympus.Performance.SetThresholds(0.016, true)
    
    -- Add debug status check
    Debug.Info(Debug.CATEGORIES.SYSTEM, string.format("Debug Status - Enabled: %s, Level: %d, Performance Category: %s", 
        tostring(Debug.enabled),
        Debug.level,
        tostring(Debug.categoryEnabled.PERFORMANCE)))
    Debug.Info(Debug.CATEGORIES.SYSTEM, "Performance monitoring initialized")
    
    -- Initialize dungeon system if available
    if Olympus.Dungeons then
        Olympus.Dungeons.Initialize()
        Debug.Info(Debug.CATEGORIES.SYSTEM, "Dungeon system initialized")
    end
    
    Debug.TrackFunctionEnd("Olympus.Initialize")
end

-- Expose Combat functions directly on Olympus for backward compatibility
Olympus.CastAction = function(action, targetId, priority)
    return Olympus.Combat.CastAction(action, targetId, priority)
end

Olympus.HasBuff = function(entity, buffId, ownerId)
    return Olympus.Combat.HasBuff(entity, buffId, ownerId)
end

Olympus.GetHighestLevelSpell = function(spells)
    return Olympus.Combat.GetHighestLevelSpell(spells)
end

Olympus.IsReady = function(spell, spellDef)
    return Olympus.Combat.IsReady(spell, spellDef)
end

Olympus.CanWeaveSpell = function(action)
    return Olympus.Combat.CanWeaveSpell(action)
end

-- Performance function wrappers
Olympus.StartFrameTimeTracking = function()
    return Olympus.Performance.StartFrameTimeTracking()
end

Olympus.IsFrameBudgetExceeded = function()
    return Olympus.Performance.IsFrameBudgetExceeded()
end

Olympus.SetPerformanceThresholds = function(frameTimeThreshold, skipLowPriority)
    return Olympus.Performance.SetThresholds(frameTimeThreshold, skipLowPriority)
end

return Olympus
