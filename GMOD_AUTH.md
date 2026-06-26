# WolflineSquad TTT — In-Game Auto Login (Garry's Mod)

How to show the website inside GMod **already logged in**, without the player clicking the Steam
login button.

## Why it works this way

GMod's in-game browser is a sandboxed Chromium panel — it does **not** carry the player's Steam web
session and exposes no token to the page. And the client can't be trusted to say who it is (a player
could send any SteamID). So the **GMod server** — the only party that knows each player's real
SteamID — vouches for them:

1. **Server → site (mint):** the server calls the site **with the API key** and the player's SteamID,
   and gets back a short-lived **signed token**.
2. **Server → client:** the server sends that token to that one player.
3. **Client → site (consume):** the client opens the site with the token; the site verifies it,
   logs the embedded browser in, and shows the page.

Trust comes from the **API key** (only the server can mint) plus the site's **signature** on the
token (it can't be forged or edited). The token is valid for **~60 seconds**.

## Base URL

`https://mwlp.dasmaffin.com`

---

## Endpoints

### `POST /auth/gmod/token` — mint (server only)

Auth: send the API key in the **`X-Api-Key`** header (a GUID; same key system as the reward API).

Request body:
```json
{ "steamId": "76561198000000000" }
```

Responses:
- `200 OK` → `{ "token": "<opaque-url-safe-token>" }`
- `400 Bad Request` → `steamId` missing or not a valid SteamID64
- `401` / `403` → API key missing/invalid / not allowed

### `GET /auth/gmod?token=…&returnUrl=…` — consume (the in-game browser opens this)

- `token` — the value from the mint call.
- `returnUrl` *(optional)* — a **site-relative** path to land on after login, e.g. `/Polls`
  (external URLs are ignored). Defaults to the home page.

On success it sets the login session + the persistent login cookie and redirects to `returnUrl`.
An invalid/expired token simply redirects to the home page (logged out) — no error screen.

---

## GMod Lua

### Server  (`lua/.../sv_wlsq.lua`)

```lua
util.AddNetworkString("WLSQ_OpenSite")

local SITE    = "https://mwlp.dasmaffin.com"
local API_KEY = "REPLACE_WITH_YOUR_KEY"   -- SERVER-SIDE ONLY. Never send this to clients.

local function OpenSiteFor(ply, returnUrl)
    if not IsValid(ply) then return end

    HTTP({
        method  = "POST",
        url     = SITE .. "/auth/gmod/token",
        type    = "application/json",
        headers = { ["X-Api-Key"] = API_KEY },
        body    = util.TableToJSON({ steamId = ply:SteamID64() }),
        success = function(code, body)
            if code ~= 200 then print("[WLSQ] mint failed: HTTP " .. code) return end
            local data = util.JSONToTable(body or "")
            if not data or not data.token then return end

            net.Start("WLSQ_OpenSite")
                net.WriteString(data.token)
                net.WriteString(returnUrl or "/")
            net.Send(ply)
        end,
        failed  = function(err) print("[WLSQ] mint failed: " .. err) end
    })
end

-- Example trigger: a chat/console command that opens the polls page logged in.
concommand.Add("wlsq_polls", function(ply) OpenSiteFor(ply, "/Polls") end)
```

### Client  (`lua/.../cl_wlsq.lua`)

```lua
net.Receive("WLSQ_OpenSite", function()
    local token     = net.ReadString()
    local returnUrl = net.ReadString()

    local frame = vgui.Create("DFrame")
    frame:SetSize(1100, 750)
    frame:Center()
    frame:SetTitle("WolflineSquad")
    frame:MakePopup()

    local browser = vgui.Create("DHTML", frame)
    browser:Dock(FILL)
    browser:OpenURL(("https://mwlp.dasmaffin.com/auth/gmod?token=%s&returnUrl=%s")
        :format(token, returnUrl))
end)
```

The token is URL-safe, so it can go straight into the query string. If your `returnUrl` ever
contains special characters, URL-encode it.

---

## After login: navigation

You only need the token for the **one-time** handoff. Once `/auth/gmod` runs, the in-game browser
holds a normal login — an ASP.NET session cookie plus the 30-day `WolflineLogin` cookie. From then on
the browser **sends those cookies on every request automatically**, so the player can click links and
browse the whole site logged in with **no further tokens and no involvement from the GMod server**.
(If the server-side session times out, the next request silently re-establishes it from the
`WolflineLogin` cookie.)

So you do **not** intercept navigation or re-authenticate per link — just open the first page with the
token and let normal cookies carry the session. You'd only mint another token if that cookie is gone
(player cleared it, or it expired after 30 days).

## External links (Discord, Tebex, Workshop) — the `wlsq.openURL` bridge

*Internal* links work fine in-panel (cookies carry the session). But **external** links can't: a new
tab is impossible, and navigating in-place would replace the site with a third-party page the player
isn't logged into. So the website needs a way to push those out to a real browser.

The site already handles this **automatically when the session is GMod-authenticated** — it loads
`wwwroot/js/gmod-bridge.js`, which intercepts clicks on external links and calls a Lua function the
client must expose on the DHTML panel:

```lua
local browser = vgui.Create("DHTML", frame)
browser:Dock(FILL)

-- Expose wlsq.openURL(url) to the page's JavaScript. gui.OpenURL opens the Steam overlay
-- browser if the overlay is enabled, otherwise the player's default OS browser.
browser:AddFunction("wlsq", "openURL", function(url)
    if isstring(url) and (url:StartWith("http://") or url:StartWith("https://")) then
        gui.OpenURL(url)
    end
end)

browser:OpenURL(("https://mwlp.dasmaffin.com/auth/gmod?token=%s&returnUrl=%s"):format(token, returnUrl))
```

Notes:
- If the bridge isn't present (this Lua not deployed), the site falls back to default link behaviour —
  nothing breaks, external links just won't open out.
- `gui.OpenURL` decides overlay-vs-default-browser based on the player's Steam overlay setting; there's
  no Lua API to force one over the other.
- Validate the `url` is `http(s)` (as above) — the page is trusted (it's our own site), but it's cheap
  insurance against opening anything unexpected.
- Only external links (different host than the site) are routed out; internal ones navigate in-panel.

## Security notes

- **The API key never leaves the server.** Clients only ever receive the one-time, ~60-second token.
- **Mint requires the key**, so a player can't request a token for another SteamID.
- **The token is signed by the site** — tampering or changing the SteamID inside it fails validation.
- **Open the panel immediately** after receiving the token (the example does); it expires in ~60s.
- After consume, the embedded browser holds a normal login + the 30-day `WolflineLogin` cookie. GMod's
  browser persists cookies, so it stays logged in across navigations and game restarts.
- `returnUrl` must be a site-relative path; external redirects are rejected.
- Tokens are **single-use** and expire in ~60s — once consumed (or expired) the same token can't be
  replayed.
