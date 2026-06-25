# WolflineSquad TTT — GMod integration

Reference Lua that wires a Garry's Mod server to the website. Full endpoint reference is in
`GMOD_API.md` and `GMOD_AUTH.md` at the repo root.

## Install

Copy the `lua/` tree into a GMod addon (or `garrysmod/`), so you end up with:

```
garrysmod/lua/autorun/server/sv_wolflinesquad.lua
garrysmod/lua/autorun/client/cl_wolflinesquad.lua
```

Then edit the top of **`sv_wolflinesquad.lua`**:

```lua
WLSQ.Site   = "https://mwlp.dasmaffin.com"
WLSQ.ApiKey = "<one of the site's ApiPrivateKeys GUIDs>"
```

⚠️ **The API key must stay in the server file** (`autorun/server/`). Never put it in a shared or
client file — clients only ever receive short-lived, single-use login tokens.

## What it provides (server `WLSQ.` API)

| Function | Hits | Purpose |
|----------|------|---------|
| `WLSQ.ReportGoldenDeagle(shooter, victim, role)` | `POST /api/GoldenDeagleShots` | Log a golden-deagle shot |
| `WLSQ.SweepRewards()` (runs on a 60s timer) | `GET /rewards/pending/GarrysMod` + `POST /rewards/claim` | Grant + claim pending poll rewards |
| `WLSQ.OpenSiteFor(ply, "/Polls")` | `POST /auth/gmod/token` + net | Open the site in-game, already logged in |
| `WLSQ.UploadStats(dataset)` | `POST /api/Stats` | Upload the player-activity dataset |

## Wire it to your addons

The file ships with example triggers (chat commands `!polls` / `!web`) and currency hooks you
override:

```lua
function WLSQ.GrantNormalPoints(steamId64, amount)  -- your Pointshop/currency call here end
function WLSQ.GrantPremiumPoints(steamId64, amount) -- ... end
```

Call `WLSQ.ReportGoldenDeagle(...)` from your golden-deagle weapon hook, and `WLSQ.UploadStats(...)`
from your activity tracker with the full current dataset.
