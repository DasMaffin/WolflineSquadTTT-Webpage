--[[
    WolflineSquad TTT — website integration (SERVERSIDE)
    Place in:  garrysmod/lua/autorun/server/

    SECURITY: this file contains your API key. It must stay under autorun/server/ so it is
    NEVER sent to clients. Do not move it to a shared or client folder.

    Full API reference: GMOD_API.md and GMOD_AUTH.md in the website repo.
]]

WLSQ = WLSQ or {}

-- ===== CONFIG — EDIT THESE =====
WLSQ.Site   = "https://mwlp.dasmaffin.com"
WLSQ.ApiKey = "00000000-0000-0000-0000-000000000000"  -- one of the site's ApiPrivateKeys (a GUID)
-- ===============================

util.AddNetworkString("WLSQ_OpenSite")

-- Authenticated request helper. Key always goes in the X-Api-Key header.
local function request(method, path, bodyTbl, onOk, onFail)
    HTTP({
        method  = method,
        url     = WLSQ.Site .. path,
        headers = { ["X-Api-Key"] = WLSQ.ApiKey },
        type    = bodyTbl and "application/json" or nil,
        body    = bodyTbl and util.TableToJSON(bodyTbl) or nil,
        success = function(code, body)
            if code >= 200 and code < 300 then
                if onOk then onOk(code, body) end
            elseif onFail then
                onFail("HTTP " .. code .. (body and (": " .. body) or ""))
            end
        end,
        failed = function(reason) if onFail then onFail(reason) end end
    })
end

--========================================================================
-- 1. Golden Deagle shots  ->  POST /api/GoldenDeagleShots
--========================================================================
-- Call whenever a golden-deagle shot should be logged. victimRole = your role id (int).
function WLSQ.ReportGoldenDeagle(shooter, victim, victimRole)
    if not IsValid(shooter) or not IsValid(victim) then return end
    request("POST", "/api/GoldenDeagleShots", {
        Player    = shooter:SteamID64(),
        Timestamp = os.time(),
        ShotAt    = victim:SteamID64(),
        VictimWas = victimRole or 0
    }, nil, function(err) print("[WLSQ] golden deagle report failed: " .. err) end)
end

--========================================================================
-- 2/3. Poll rewards  ->  GET /rewards/pending/GarrysMod  +  POST /rewards/claim
--========================================================================
-- Override these to plug into your currency system (Pointshop, etc.). They get a SteamID64
-- string + amount; handle offline players (persist for next join) as needed.
WLSQ.GrantNormalPoints  = WLSQ.GrantNormalPoints  or function(steamId64, amount) end
WLSQ.GrantPremiumPoints = WLSQ.GrantPremiumPoints or function(steamId64, amount) end

function WLSQ.SweepRewards()
    request("GET", "/rewards/pending/GarrysMod", nil, function(_, body)
        local rewards = util.JSONToTable(body or "") or {}
        local claimedIds = {}
        for _, r in ipairs(rewards) do
            if (r.normalPoints  or 0) > 0 then WLSQ.GrantNormalPoints(r.steamId,  r.normalPoints) end
            if (r.premiumPoints or 0) > 0 then WLSQ.GrantPremiumPoints(r.steamId, r.premiumPoints) end
            claimedIds[#claimedIds + 1] = r.id
        end
        -- Only claim the ids we actually handed out, so nothing earned mid-sweep is lost.
        if #claimedIds > 0 then
            request("POST", "/rewards/claim", { ids = claimedIds }, nil,
                function(err) print("[WLSQ] reward claim failed: " .. err) end)
        end
    end, function(err) print("[WLSQ] reward fetch failed: " .. err) end)
end

-- Sweep every 60 seconds (tune to taste).
timer.Create("WLSQ_RewardSweep", 60, 0, function() WLSQ.SweepRewards() end)

--========================================================================
-- 4. In-game auto-login  ->  POST /auth/gmod/token  (then net to that client)
--========================================================================
-- Opens the website in the player's in-game browser, already logged in.
-- returnUrl = a site-relative path, e.g. "/Polls" or "/".
function WLSQ.OpenSiteFor(ply, returnUrl)
    if not IsValid(ply) then return end
    request("POST", "/auth/gmod/token", { steamId = ply:SteamID64() }, function(_, body)
        local data = util.JSONToTable(body or "")
        if not data or not data.token then return end
        net.Start("WLSQ_OpenSite")
            net.WriteString(data.token)
            net.WriteString(returnUrl or "/")
        net.Send(ply)
    end, function(err) print("[WLSQ] login token mint failed: " .. err) end)
end

-- Example chat commands: "!polls" / "!web" open those pages logged in.
hook.Add("PlayerSay", "WLSQ_OpenCommands", function(ply, text)
    local cmd = string.lower(string.Trim(text))
    if cmd == "!polls" then WLSQ.OpenSiteFor(ply, "/Polls") return "" end
    if cmd == "!web"   then WLSQ.OpenSiteFor(ply, "/")      return "" end
end)

--========================================================================
-- 5. Player-activity stats upload  ->  POST /api/Stats
--========================================================================
-- dataset = { [steamId64] = { { startTime=, endTime=, finishedReports=, playing=, activePlayers= }, ... }, ... }
-- FULL REPLACE: send your complete current dataset (it overwrites the site's copy).
function WLSQ.UploadStats(dataset)
    request("POST", "/api/Stats", dataset, function(_, body)
        print("[WLSQ] stats uploaded: " .. (body or ""))
    end, function(err) print("[WLSQ] stats upload failed: " .. err) end)
end
