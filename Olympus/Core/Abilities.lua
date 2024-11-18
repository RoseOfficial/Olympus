local Abilities = {}

---Handle Sprint usage while moving
---@return boolean cast Whether Sprint was cast
function Abilities.HandleSprint()
    Debug.TrackFunctionStart("Olympus.Core.Abilities.HandleSprint")
    
    if Player:IsMoving() then
        Debug.Verbose(Debug.CATEGORIES.MOVEMENT, "Player is moving, checking Sprint")
        local sprintAction = ActionList:Get(1, Olympus.COMMON_SPELLS.SPRINT.id)
        if sprintAction and sprintAction.cdmax - sprintAction.cd <= 0.5 then
            Debug.Info(Debug.CATEGORIES.MOVEMENT, "Using Sprint")
            local result = sprintAction:Cast()
            Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleSprint")
            return result
        else
            Debug.Verbose(Debug.CATEGORIES.MOVEMENT, "Sprint not ready")
        end
    else
        Debug.Verbose(Debug.CATEGORIES.MOVEMENT, "Player not moving")
    end
    
    Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleSprint")
    return false
end

---Handle Lucid Dreaming usage for MP recovery
---@param mpThreshold number MP percentage threshold for using Lucid Dreaming
---@return boolean cast Whether Lucid Dreaming was cast
function Abilities.HandleLucidDreaming(mpThreshold)
    Debug.TrackFunctionStart("Olympus.Core.Abilities.HandleLucidDreaming")
    
    Debug.Info(Debug.CATEGORIES.SYSTEM, 
        string.format("MP Check - Current: %.1f%%, Threshold: %.1f%%", 
            Player.mp.percent, 
            mpThreshold))
            
    if Player.mp.percent < mpThreshold then
        Debug.Info(Debug.CATEGORIES.SYSTEM, "Using Lucid Dreaming for MP recovery")
        local result = Olympus.CastAction(Olympus.COMMON_SPELLS.LUCID_DREAMING)
        Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleLucidDreaming")
        return result
    end
    
    Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleLucidDreaming")
    return false
end

---Handle Swiftcast usage while moving in combat
---@return boolean cast Whether Swiftcast was cast
function Abilities.HandleSwiftcast()
    Debug.TrackFunctionStart("Olympus.Core.Abilities.HandleSwiftcast")
    
    if Player.incombat and Player:IsMoving() then
        Debug.Info(Debug.CATEGORIES.COMBAT, "Using Swiftcast while moving in combat")
        local result = Olympus.CastAction(Olympus.COMMON_SPELLS.SWIFTCAST)
        Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleSwiftcast")
        return result
    end
    
    Debug.Verbose(Debug.CATEGORIES.COMBAT, "Swiftcast conditions not met")
    Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleSwiftcast")
    return false
end

---Handle Esuna usage on debuffed party members
---@param range number Maximum range to consider party members
---@return boolean cast Whether Esuna was cast
function Abilities.HandleEsuna(range)
    Debug.TrackFunctionStart("Olympus.Core.Abilities.HandleEsuna")
    
    if Player.level < Olympus.COMMON_SPELLS.ESUNA.level then 
        Debug.Verbose(Debug.CATEGORIES.HEALING, "Level too low for Esuna")
        Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleEsuna")
        return false 
    end
    
    local party = Olympus.GetParty(range)
    if not table.valid(party) then 
        Debug.Verbose(Debug.CATEGORIES.HEALING, "No valid party members in range")
        Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleEsuna")
        return false 
    end
    
    for _, member in pairs(party) do
        if Olympus.IsDebuffable(member) and member.distance2d <= range then
            Debug.Info(Debug.CATEGORIES.HEALING, 
                string.format("Using Esuna on %s", member.name or "Unknown"))
            local result = Olympus.CastAction(Olympus.COMMON_SPELLS.ESUNA, member.id)
            Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleEsuna")
            return result
        end
    end
    
    Debug.Verbose(Debug.CATEGORIES.HEALING, "No party members need Esuna")
    Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleEsuna")
    return false
end

---Handle Raise usage on dead party members
---@param range number Maximum range to consider party members
---@return boolean cast Whether Raise was cast
function Abilities.HandleRaise(range)
    Debug.TrackFunctionStart("Olympus.Core.Abilities.HandleRaise")
    
    if Player.level < Olympus.COMMON_SPELLS.RAISE.level then 
        Debug.Verbose(Debug.CATEGORIES.HEALING, "Level too low for Raise")
        Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleRaise")
        return false 
    end
    
    if Player.mp.current < Olympus.COMMON_SPELLS.RAISE.mp then 
        Debug.Warn(Debug.CATEGORIES.HEALING, "Not enough MP for Raise")
        Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleRaise")
        return false 
    end
    
    local party = EntityList("myparty,dead,maxdistance=" .. range)
    if not table.valid(party) then 
        Debug.Verbose(Debug.CATEGORIES.HEALING, "No dead party members in range")
        Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleRaise")
        return false 
    end
    
    for _, member in pairs(party) do
        if not Olympus.HasBuff(member, Olympus.BUFF_IDS.RAISE) and member.distance2d <= range then
            -- Try to use Swiftcast if available
            if not Olympus.HasBuff(Player, Olympus.BUFF_IDS.SWIFTCAST) 
               and Player.level >= Olympus.COMMON_SPELLS.SWIFTCAST.level then
                Debug.Info(Debug.CATEGORIES.HEALING, "Using Swiftcast before Raise")
                Olympus.CastAction(Olympus.COMMON_SPELLS.SWIFTCAST)
            end
            Debug.Info(Debug.CATEGORIES.HEALING, 
                string.format("Raising %s", member.name or "Unknown"))
            local result = Olympus.CastAction(Olympus.COMMON_SPELLS.RAISE, member.id)
            Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleRaise")
            return result
        end
    end
    
    Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleRaise")
    return false
end

---Handle Surecast usage (placeholder for ACR-specific implementation)
---@return boolean cast Whether Surecast was cast
function Abilities.HandleSurecast()
    Debug.TrackFunctionStart("Olympus.Core.Abilities.HandleSurecast")
    
    if Player.level < Olympus.COMMON_SPELLS.SURECAST.level then 
        Debug.Verbose(Debug.CATEGORIES.COMBAT, "Level too low for Surecast")
        Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleSurecast")
        return false 
    end
    
    Debug.Verbose(Debug.CATEGORIES.COMBAT, "Surecast implementation pending")
    Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleSurecast")
    return false
end

---Handle Rescue usage (placeholder for ACR-specific implementation)
---@return boolean cast Whether Rescue was cast
function Abilities.HandleRescue()
    Debug.TrackFunctionStart("Olympus.Core.Abilities.HandleRescue")
    
    if Player.level < Olympus.COMMON_SPELLS.RESCUE.level then 
        Debug.Verbose(Debug.CATEGORIES.COMBAT, "Level too low for Rescue")
        Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleRescue")
        return false 
    end
    
    Debug.Verbose(Debug.CATEGORIES.COMBAT, "Rescue implementation pending")
    Debug.TrackFunctionEnd("Olympus.Core.Abilities.HandleRescue")
    return false
end

-- Initialize and expose the module
Olympus = Olympus or {}
Olympus.Core = Olympus.Core or {}
Olympus.Core.Abilities = Abilities

return Abilities