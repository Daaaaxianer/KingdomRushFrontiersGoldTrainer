local systems = package.loaded.systems
if type(systems) ~= 'table' or type(systems.health) ~= 'table' or type(systems.wave_spawn) ~= 'table' then return 0 end
if _G.__KRF_GOLD_LIVE then return 1 end
local native_get = assert(_G.__KR_GOLD_NATIVE_GET)
_G.__KR_GOLD_NATIVE_GET = nil
local signal = require('hump.signal')
local original_emit = signal.emit
local active_store
local multiplier = 1
local max_gold = 2147483647
local function poll_multiplier()
    local number = native_get()
    if type(number)=='number' and number>=1 and number<=10 then multiplier=number else multiplier=1 end
end

local function reward_emit(name, first, amount, ...)
    local store = active_store
    if store and (name == 'got-enemy-gold' or name == 'early-wave-called') then
        poll_multiplier()
        if multiplier > 1 and type(amount) == 'number' and amount > 0
                and type(store.player_gold) == 'number' then
            -- The unmodified game has already deposited the base reward.
            local bonus = math.min(amount * (multiplier - 1), math.max(0, max_gold - store.player_gold))
            store.player_gold = store.player_gold + bonus
            amount = amount + bonus
            if name == 'early-wave-called' then store.early_wave_reward = amount end
        end
    end
    return original_emit(name, first, amount, ...)
end

signal.emit = function(name, ...)
    if active_store and (name == 'got-enemy-gold' or name == 'early-wave-called') then
        return reward_emit(name, ...)
    end
    return original_emit(name, ...)
end

local function restore_context(previous, ...)
    active_store = previous
    return ...
end

local function wrap_update(system)
    local original = assert(system.on_update)
    system.on_update = function(self, dt, ts, store, ...)
        local previous = active_store
        active_store = store
        -- Both original update methods are ordinary non-yielding system updates.
        return restore_context(previous, original(self, dt, ts, store, ...))
    end
end

wrap_update(assert(systems.health))
wrap_update(assert(systems.wave_spawn))
_G.__KRF_GOLD_LIVE = true
return 1
