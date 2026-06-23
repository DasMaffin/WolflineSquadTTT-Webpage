# WolflineSquad TTT — Garry's Mod Server API

Reference for the endpoints the **Garry's Mod (Lua) server** uses to talk to the website.
There are three: one to report Golden Deagle shots, and two for the poll reward system.

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `POST` | `/api/GoldenDeagleShots` | API key | Record a Golden Deagle shot event |
| `GET`  | `/rewards/{steamId}` | **none (open)** | List a player's unclaimed poll rewards |
| `POST` | `/rewards/claim` | API key | Mark rewards as handed out |

## Base URL

- **Production:** `https://<your-deployed-domain>`
- **Local dev:** `http://localhost:5000`

All paths below are relative to the base URL. Use HTTPS in production.

---

## Authentication

Protected endpoints (`POST /api/GoldenDeagleShots` and `POST /rewards/claim`) require a
**private API key** sent **inside the JSON body** as a field named `apiPrivateKey`.

- The key must be a **GUID** (e.g. `11111111-1111-1111-1111-111111111111`).
- Valid keys are configured server-side under `ApiPrivateKeys` in `appsettings.{Environment}.json`.
  Ask the site operator for a key; it is **not** published here.
- The key goes in the body, **not** a header or query string.

The open endpoint (`GET /rewards/{steamId}`) needs no key — reading a player's pending rewards is
harmless, but **claiming** is locked down so players can't mark their own rewards as paid.

### Auth error responses

| Status | Meaning |
|--------|---------|
| `401 Unauthorized` | Body was empty, or `apiPrivateKey` was missing / not a valid GUID |
| `403 Forbidden` | `apiPrivateKey` is a valid GUID but not in the server's allowed list |
| `500` | Server has no API keys configured (operator misconfiguration) |

---

## 1. Record a Golden Deagle shot

**`POST /api/GoldenDeagleShots`** — auth required.

Appends one shot event to the server's data store. Used to investigate ghosting / targeted
harassment.

### Request body

```json
{
  "apiPrivateKey": "11111111-1111-1111-1111-111111111111",
  "Player": "76561198000000000",
  "Timestamp": 1750000000,
  "ShotAt": "76561198111111111",
  "VictimWas": 2
}
```

| Field | Type | Description |
|-------|------|-------------|
| `apiPrivateKey` | string (GUID) | Your API key |
| `Player` | string | SteamID64 of the shooter |
| `Timestamp` | integer | Unix time (seconds) of the shot |
| `ShotAt` | string | SteamID64 of the victim |
| `VictimWas` | integer | Role of the victim (your role code, e.g. innocent/traitor/detective) |

Field names are case-insensitive (`Player` and `player` both bind).

### Responses

- `200 OK` → `{ "status": "success" }`
- `400 Bad Request` → invalid/empty body
- See [auth errors](#auth-error-responses) for `401` / `403` / `500`

### Lua example

```lua
HTTP({
    method  = "POST",
    url     = BASE_URL .. "/api/GoldenDeagleShots",
    type    = "application/json",
    body    = util.TableToJSON({
        apiPrivateKey = API_KEY,
        Player        = shooter:SteamID64(),
        Timestamp     = os.time(),
        ShotAt        = victim:SteamID64(),
        VictimWas     = victimRoleId
    }),
    success = function(code, body) end,
    failed  = function(reason) print("[WLSQ] shot report failed: " .. reason) end
})
```

---

## 2. List a player's unclaimed rewards

**`GET /rewards/{steamId}`** — open, no auth.

`{steamId}` is the player's **SteamID64**. Returns every reward the player has earned (by
answering a poll that had a reward attached) and **not yet claimed**. Returns an empty array `[]`
if there are none or the player is unknown.

### Response — `200 OK`

```json
[
  {
    "id": 12,
    "reward": "Daily Poll Bonus",
    "rewardType": "GarrysMod",
    "normalPoints": 100,
    "premiumPoints": 5,
    "pollId": 3,
    "pollTitle": "Wie sehr stinkt Exe?",
    "createdAt": "2026-06-23T11:30:00Z"
  }
]
```

| Field | Type | Description |
|-------|------|-------------|
| `id` | integer | **Reward-claim id** — use this to mark it claimed (endpoint 3) |
| `reward` | string | Reward name |
| `rewardType` | string | Platform; currently always `"GarrysMod"` |
| `normalPoints` | integer | Normal currency to grant |
| `premiumPoints` | integer | Premium currency to grant |
| `pollId` | integer | The poll that earned the reward |
| `pollTitle` | string | The poll's title |
| `createdAt` | string (ISO-8601 UTC) | When the reward was earned |

The website only stores *what* to give; the game server decides *how* to grant the points.

### Lua example

```lua
http.Fetch(BASE_URL .. "/rewards/" .. ply:SteamID64(),
    function(body, len, headers, code)
        if code ~= 200 then return end
        local rewards = util.JSONToTable(body)
        local claimedIds = {}
        for _, r in ipairs(rewards) do
            ply:AddPoints(r.normalPoints)          -- your currency hooks
            ply:AddPremiumPoints(r.premiumPoints)
            table.insert(claimedIds, r.id)
        end
        if #claimedIds > 0 then MarkClaimed(claimedIds) end  -- see endpoint 3
    end,
    function(err) print("[WLSQ] reward fetch failed: " .. err) end
)
```

---

## 3. Mark rewards as claimed

**`POST /rewards/claim`** — auth required.

Call this **after** you've actually granted the points in-game, so the player can't collect the
same reward twice. Only rewards that are currently unclaimed are affected; unknown or
already-claimed ids are ignored.

### Request body

```json
{
  "apiPrivateKey": "11111111-1111-1111-1111-111111111111",
  "ids": [12, 13, 14]
}
```

| Field | Type | Description |
|-------|------|-------------|
| `apiPrivateKey` | string (GUID) | Your API key |
| `ids` | array of integers | Reward-claim ids (the `id` values from endpoint 2) |

### Responses

- `200 OK` → `{ "claimed": 3 }` — number of rewards actually marked claimed
- `400 Bad Request` → no ids provided
- See [auth errors](#auth-error-responses) for `401` / `403` / `500`

### Lua example

```lua
function MarkClaimed(ids)
    HTTP({
        method  = "POST",
        url     = BASE_URL .. "/rewards/claim",
        type    = "application/json",
        body    = util.TableToJSON({ apiPrivateKey = API_KEY, ids = ids }),
        success = function(code, body)
            if code == 200 then
                local data = util.JSONToTable(body)
                print("[WLSQ] claimed " .. data.claimed .. " reward(s)")
            end
        end,
        failed  = function(reason) print("[WLSQ] claim failed: " .. reason) end
    })
end
```

---

## Typical reward flow

1. A player answers a poll on the website that has a reward attached → the site records an
   unclaimed reward for that player's SteamID.
2. The GMod server periodically (e.g. on player spawn/connect) calls
   **`GET /rewards/{steamId64}`** to fetch pending rewards.
3. For each reward, the server grants `normalPoints` / `premiumPoints` to the player.
4. The server calls **`POST /rewards/claim`** with the granted `id`s so they aren't handed out again.

## Notes

- **SteamID format:** always SteamID64 (the long numeric form, `Player:SteamID64()`), both in
  request paths and bodies.
- **JSON only:** use GMod's structured `HTTP({ ... })` with `type = "application/json"`. The
  simpler `http.Post` sends form-encoded data, which these endpoints do **not** accept.
- **Claim after granting:** grant points first, then claim. If a claim call fails, the reward stays
  pending and will be returned again next fetch (so worst case a player gets it on the next pass,
  never lost — but make sure your grant step is idempotent if you claim before granting).
- **Keep the key secret:** it's effectively the password for writing data; never ship it to clients.
