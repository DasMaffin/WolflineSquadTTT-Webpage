# WolflineSquad TTT - GMod WebSocket Push API

Real-time channel for pushing commands **from the website to a Garry's Mod server**. Use it when you
change state on the site (e.g. equip/unequip a Pointshop item, grant something) and want the live game to
reflect it immediately, instead of waiting for the server to poll.

This complements the polling API in [`GMOD_API.md`](GMOD_API.md). Polling is pull (server asks the site);
this is **push** (site tells the server). Language-agnostic - implement the server side in whatever stack
the website runs.

## Connection model

- **The GMod server is the WebSocket *client*. The website is the WebSocket *server*.** GMod opens and
  maintains the connection; you accept it and push frames down it.
- GMod connects to:
  ```
  wss://<your-domain>/ws/gmod
  ```
  (`ws://` for plain HTTP dev, `wss://` for HTTPS prod - GMod derives it from its configured base URL.)
- GMod **auto-reconnects** roughly every 10 seconds if the socket drops, so you can restart your endpoint
  freely. Don't assume a connection is permanent.
- One connection **per GMod server**. If you run several servers they each open their own socket.

> Requires the `gmsv_gwsockets` binary module on the GMod server. If it's missing the server logs a notice
> and simply runs without push (polling still works). That's the server operator's concern, not yours.

## Authentication

On the WebSocket **handshake (the HTTP upgrade request)**, GMod sends:

```
X-Api-Key: <the private API GUID>
```

This is the **same key** as the rest of the API (see `GMOD_API.md`). Your `/ws/gmod` endpoint **must**:

1. Read the `X-Api-Key` header from the upgrade request.
2. Validate it (must be a known, allowed GUID).
3. **Reject / close** the connection if missing or invalid (e.g. respond `401` to the upgrade, or accept
   then immediately close).

Only an authenticated GMod server should get an open socket. If you run multiple servers, you can also use
the key (or a per-server id you assign) to know *which* server each socket belongs to, so you can target
pushes.

## Message format (site -> GMod)

To trigger something in-game, send **one text frame** containing a JSON object:

```json
{ "hook": "MaffinAPI_PointshopReloadInventory", "args": ["76561198000000000"] }
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `hook` | string | yes | The GMod hook name to fire. **Must start with `MaffinAPI_`** - the server refuses anything else (so the site can't trigger arbitrary engine/gamemode hooks). |
| `args` | array | no | Positional arguments, applied in order. |

The GMod server runs `hook.Run(hook, args[0], args[1], ...)`. Any addon on the server that subscribed to
that hook name receives the call. The dispatcher is **generic**: to add a new feature later, an addon
subscribes to a new `MaffinAPI_*` hook and you push that name - no change to this protocol.

### JSON -> Lua type mapping

| JSON | GMod (Lua) |
|------|-----------|
| string | string |
| number | number |
| `true` / `false` | boolean |
| array | sequential table |
| object | table |

**Numbers caveat:** a Lua number can't hold a SteamID64 precisely (it exceeds 2^53). **Always send
SteamID64 as a string** (the 17-digit `765...`), never a JSON number.

## Available hooks (the contract)

| Hook | Args | Effect |
|------|------|--------|
| `MaffinAPI_PointshopReloadInventory` | `[steamId64: string]` | Reloads that player's Pointshop 2 inventory + equipment from the database, live. |

### `MaffinAPI_PointshopReloadInventory`

After you commit your SQL change for a player (equip/unequip/grant/remove a Pointshop item), push:

```json
{ "hook": "MaffinAPI_PointshopReloadInventory", "args": ["<SteamID64>"] }
```

- **Online player:** the server re-reads their inventory + equipment from the DB and applies it live
  (cosmetics update, the in-game inventory UI refreshes). The website is the writer; the game just
  re-syncs from the DB.
- **Offline player:** no-op. The DB is already correct and Pointshop loads it on the player's next join.
  So it's always safe to push regardless of whether the player is on.

## Reliability

- **Fire-and-forget.** If the socket happened to be down when you pushed, the message is lost. For the
  Pointshop reload that's fine - the database is the source of truth, and the player's next relog (or any
  later push) reconciles it.
- **Do not rely on push for must-deliver actions.** For anything that must not be missed (e.g. paying out
  currency), keep a polled "pending" queue as the backbone (like the reward system in `GMOD_API.md`) and
  treat push purely as a latency optimization.
- **Idempotency.** Pushing the same reload twice is harmless (it just re-syncs to the same DB state), so
  retries are safe.

## Server-side implementation checklist

1. Expose a WebSocket endpoint at `/ws/gmod`.
2. On the upgrade request, read + validate `X-Api-Key`; reject if bad.
3. Keep the connection open; track it (optionally by key/server id).
4. When site state changes, **commit your SQL first**, then send the JSON frame to the relevant server's
   socket.
5. Tolerate reconnects (GMod reconnects ~every 10s); a fresh socket may appear at any time.

### Pseudocode (any language)

```
on websocket upgrade request:
    key = request.header["X-Api-Key"]
    if not isValidGuid(key) or key not in allowedKeys:
        reject(401)
    else:
        accept()
        registerConnection(serverFor(key), socket)

# elsewhere, when a player's pointshop data changes on the site:
function onPointshopChanged(steamId64, serverId):
    commitSqlChange(...)                       # write the DB first
    socket = connectionFor(serverId)
    if socket and socket.isOpen:
        socket.send(jsonEncode({
            hook: "MaffinAPI_PointshopReloadInventory",
            args: [ String(steamId64) ]        # MUST be a string
        }))
    # if no socket / closed: nothing to do, the server reloads on next join
```

## Notes

- The GMod side currently only **receives** (beyond the auth header it sends nothing back). You don't need
  to handle inbound application messages from GMod - just authenticate, hold the socket open, and push.
- The `/ws/gmod` path is the default; it's configurable on the GMod side if it ever collides with another route.
- Keep the API key secret - it's the only thing standing between the open internet and your server's game
  state. Treat the WS handshake auth as seriously as the REST endpoints.
