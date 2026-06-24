# WolflineSquad TTT — Garry's Mod Server API

Reference for the endpoints the **Garry's Mod (Lua) server** uses to talk to the website.
There are three: one to report Golden Deagle shots, and two for the poll reward system.

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `POST` | `/api/GoldenDeagleShots` | API key | Record a Golden Deagle shot event |
| `GET`  | `/rewards/pending` | API key | List **all** unclaimed rewards |
| `GET`  | `/rewards/pending/{rewardType}` | API key | List unclaimed rewards of one type (e.g. `GarrysMod`) |
| `POST` | `/rewards/claim` | API key | Mark rewards as handed out |

## Base URL

- **Production:** `https://mwlp.dasmaffin.com`
- **Local dev:** `http://localhost:5000`

All paths below are relative to the base URL. Use HTTPS in production.

---

## Authentication

Every endpoint requires a **private API key**. Send it in the **`X-Api-Key` header**:

```
X-Api-Key: 11111111-1111-1111-1111-111111111111
```

- The key must be a **GUID**.
- Valid keys are configured server-side under `ApiPrivateKeys` in `appsettings.{Environment}.json`.
  Ask the site operator for a key; it is **not** published here.
- For convenience, the `POST` endpoints also accept the key as an `apiPrivateKey` field in the JSON
  body instead of the header. The header is preferred and is the only option for `GET`.

### Auth error responses

| Status | Meaning |
|--------|---------|
| `401 Unauthorized` | No key supplied (missing/blank `X-Api-Key` header and no body key), or it isn't a valid GUID |
| `403 Forbidden` | Key is a valid GUID but not in the server's allowed list |
| `500` | Server has no API keys configured (operator misconfiguration) |

---

## 1. Record a Golden Deagle shot

**`POST /api/GoldenDeagleShots`** — auth required.

Appends one shot event to the server's data store. Used to investigate ghosting / targeted
harassment.

### Request body

```json
{
  "Player": "76561198000000000",
  "Timestamp": 1750000000,
  "ShotAt": "76561198111111111",
  "VictimWas": 2
}
```

| Field | Type | Description |
|-------|------|-------------|
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
    headers = { ["X-Api-Key"] = API_KEY },
    body    = util.TableToJSON({
        Player    = shooter:SteamID64(),
        Timestamp = os.time(),
        ShotAt    = victim:SteamID64(),
        VictimWas = victimRoleId
    }),
    success = function(code, body) end,
    failed  = function(reason) print("[WLSQ] shot report failed: " .. reason) end
})
```

---

## 2. List unclaimed rewards

**`GET /rewards/pending`** — auth required. Returns **every** unclaimed reward across all players.

**`GET /rewards/pending/{rewardType}`** — same, but only rewards of one type, so your server pulls
only what it can hand out (e.g. `GET /rewards/pending/GarrysMod`). `{rewardType}` is the reward
type name (case-insensitive); currently the only value is `GarrysMod`.

Either form returns an empty array `[]` when there's nothing pending.

### Response — `200 OK`

```json
[
  {
    "id": 12,
    "steamId": "76561198000000000",
    "rewardType": "GarrysMod",
    "normalPoints": 100,
    "premiumPoints": 5
  }
]
```

| Field | Type | Description |
|-------|------|-------------|
| `id` | integer | **Reward-claim id** — send this back to mark it claimed (endpoint 3) |
| `steamId` | string | SteamID64 of the player to credit |
| `rewardType` | string | Reward type; currently always `"GarrysMod"` |
| `normalPoints` | integer | Normal currency to grant |
| `premiumPoints` | integer | Premium currency to grant |

The website only stores *what* to give; the game server decides *how* to grant the points.

### Lua example

```lua
http.Fetch(BASE_URL .. "/rewards/pending/GarrysMod",
    function(body, len, headers, code)
        if code ~= 200 then return end
        local rewards = util.JSONToTable(body) or {}
        local claimedIds = {}
        for _, r in ipairs(rewards) do
            local ply = player.GetBySteamID64(r.steamId)
            if IsValid(ply) then
                ply:AddPoints(r.normalPoints)              -- your currency hooks
                ply:AddPremiumPoints(r.premiumPoints)
            else
                GrantOffline(r.steamId, r.normalPoints, r.premiumPoints)  -- persist for later
            end
            table.insert(claimedIds, r.id)
        end
        if #claimedIds > 0 then MarkClaimed(claimedIds) end  -- see endpoint 3
    end,
    function(err) print("[WLSQ] reward fetch failed: " .. err) end,
    { ["X-Api-Key"] = API_KEY }
)
```

---

## 3. Mark rewards as claimed

**`POST /rewards/claim`** — auth required.

Call this **after** you've actually granted the points, so a reward can't be handed out twice.
Only rewards that are currently unclaimed are affected; unknown or already-claimed ids are ignored.

### Request body

```json
{
  "ids": [12, 13, 14]
}
```

| Field | Type | Description |
|-------|------|-------------|
| `ids` | array of integers | Reward-claim ids (the `id` values from endpoint 2). One id is fine: `[12]`. |

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
        headers = { ["X-Api-Key"] = API_KEY },
        body    = util.TableToJSON({ ids = ids }),
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
2. The GMod server periodically (e.g. on a timer) calls **`GET /rewards/pending/GarrysMod`** once to
   fetch all unclaimed rewards of its type.
3. For each reward, the server grants `normalPoints` / `premiumPoints` to the player identified by
   its `steamId`.
4. The server calls **`POST /rewards/claim`** with the granted `id`s so they aren't handed out again.

## Notes

- **SteamID format:** always SteamID64 (the long numeric form, `Player:SteamID64()`), in request
  bodies and responses.
- **JSON only:** use GMod's structured `HTTP({ ... })` with `type = "application/json"` for the
  POSTs. The simpler `http.Post` sends form-encoded data, which these endpoints do **not** accept.
- **Claim after granting:** grant points first, then claim. If a claim call fails, the reward stays
  pending and is returned again next sweep (so worst case it's granted on the next pass, never lost
  — make your grant step idempotent if you claim before granting).
- **Keep the key secret:** it's effectively the password for writing data; never ship it to clients.
