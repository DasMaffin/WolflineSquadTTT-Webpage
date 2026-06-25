# Deployment Checklist

Running list of what must be deployed for pending work to go live. **Append** new items as changes
are made; check items off once they're live. Most recent first.

## DB migrations
- **`AddPollsAndRewards`** — polls + rewards schema (Poll TPH table + discriminator, PollOption,
  UserPollOptionVote, Reward, RewardClaim).
  - Dev DB (`mwlp_webpage_dev`): ✅ applied 2026-06-23.
  - Prod DB (`mwlp_webpage_prod`): ⬜ apply only when deploying to Production (`dotnet ef database update`
    against ProdDb, or `MigrateAndUpdateDatabase.ps1 --prod`). ⚠️ Clear any old test rows in
    `Poll` / `PollOption` / `UserPollOptionVote` first — the new `Discriminator` backfill (`""`) leaves
    pre-existing rows unmappable.
- **`AddMarket`** (2026-06-25) — adds the `MarketListing` table (website DB) for the Pointshop 2 market
  (seller, escrowed kinv item id, denormalised item display, type/status, price, auction bid/bidder/end,
  sale outcome; indexes on `Status` + `SellerSteamId`). New-table-only.
  - Dev DB (`mwlp_webpage_dev`): ⬜ apply (`dotnet ef database update`).
  - Prod DB (`mwlp_webpage_prod`): ⬜ apply at deploy.
- The Pointshop 2 inventory page itself is read-only against the `GMod` DB and adds no migration; the
  inventory **unequip** and **market** features only need *runtime* write privileges on the game DB (below),
  not a schema migration there.

## Code (needs app redeploy + restart)
All in the current build; the running instance must be restarted/redeployed:
- Perf (2026-06-25): `_Layout` workshop thumbnails no longer hit the Steam API on every page render —
  `SteamService` caches each preview URL in `IMemoryCache` (12h) and fetches the batch in parallel. Speeds
  up **every** page (was the main per-page latency). No DB migration, no config.
- Perf — scroll/paint (2026-06-25): `site.css` no longer uses `background-attachment: fixed` (moved the
  darkened background onto one fixed composited `body::before` layer) and removed `backdrop-filter: blur`
  from the navbar/footer. Fixes janky scrolling/interaction on long pages (the inventory grid). Bar opacities
  nudged up (navbar .85, footer .9) to compensate for the dropped blur. CSS-only.
- Pointshop 2 inventory: `GET /pointshop2/inventory` (`PointShopController`, `[RequiresLogin]`) shows the
  signed-in user their own in-game inventory + wallet, read live from the `GMod` DB via `IPointShopService`
  (raw read-only SQL, MySqlConnector). New `RequiresLoginAttribute` (any logged-in user, no permission) and
  an "Inventory" nav link (logged-in only). **Requires a new `GModDb` connection string** (see Runtime/host).
  **No DB migration.**
  - View others (2026-06-25): `GET /pointshop2/inventory/{steamId}` gated by a new permission
    **`ViewInventories` (enum = 9)** in a new **"Pointshop 2"** permission group (auto-renders a tab in
    `/Admin/Permissions`). Same view, headed with the target player's name. Permission is just an enum value —
    **no DB migration**.
  - Privacy + GDPR updated: we display Pointshop data read live (not stored); viewable by the owner **and by
    authorized staff** holding `ViewInventories`.
  - Redesign (2026-06-25): in-game-style **slot grid** (unequipped items filling `inventories.numSlots`, in
    purchase order — no slot index is stored) + separate **equipment panel** (`ps2_equipmentslot`), three
    wallet currencies (Points / Premium / **EasterEggs**), Airdrop **currency-bundle** tiles (null-persistence
    `kinv_items`, amount from `data` JSON), and one **colour per item type**. Code-only, **no migration**.
  - Unequip + live reload (2026-06-25): double-clicking your own equipped item → `POST /pointshop2/unequip`
    (`[RequiresLogin]` + antiforgery; ownership enforced by the session SteamID) **writes the game DB**
    (returns the item to `kinv_items.inventory_id`, clears `ps2_equipmentslot.itemId`) then pushes a
    WebSocket hook so the server reloads. **Guard:** if no GMod socket is connected, it returns HTTP 503 with
    a "Server connection could not be established…" message (shown in a dialog) and **performs no DB write**.
    New **WebSocket server** `GET /ws/gmod` (`app.UseWebSockets()` + `IGmodSocketHub` singleton, API-key auth
    via `X-Api-Key` header / `?apiKey=`); pushes `{"hook":"MaffinAPI_PointshopReloadInventory","args":["<steam64>"]}`
    to all connected GMod servers. Contract in `GMOD_WEBSOCKET.md`. **No website DB migration.**
    - ⚠️ **The `GModDb` user now needs WRITE access**: `UPDATE` on `GMod.ps2_equipmentslot` and
      `GMod.kinv_items` (it was read-only `SELECT`). Until granted, unequip returns 400/throws.
    - ⚠️ **Test against `GModTest` first** (same-schema copy) before pointing at live `GMod` — this mutates
      real inventories and couldn't be tested from here (read-only access).
- Pointshop 2 market (2026-06-25): `/pointshop2/market` (`MarketController`, `[RequiresLogin]`) — list items
  for **fixed-price sale or auction**, buy, bid, cancel. Listings live in the website DB (`MarketListing` +
  `AddMarket` migration); the listed item is **escrowed in the game DB** (`kinv_items.inventory_id` cleared)
  and moves on sale; **points only**. Selling is launched from the inventory right-click → "Sell on market"
  modal. `AuctionCloserService` (hosted `BackgroundService`, 30 s) settles expired auctions. Every trade
  write is **socket-gated** (503 + dialog if no GMod server is connected) and pushes inventory-rebuild hooks
  to both players. "Market" nav link (logged-in). Needs game-DB write privileges (see Runtime/host). Privacy
  + GDPR updated.
- Persistent Steam login cookie (`WolflineLogin`) + session-rehydration middleware + Data Protection keys.
- Polls system (Basic / MultiSelect / Ranking, voting UI, results, management) + **ViewPolls default-on**.
- Reward system + GMod API: `GET /rewards/pending`, `GET /rewards/pending/{rewardType}`,
  `POST /rewards/claim` — all auth via `X-Api-Key` header (POSTs also accept body key).
- In-game auto-login: `POST /auth/gmod/token` (mint, key-protected) + `GET /auth/gmod` (consume) +
  `GmodAuthTokenService` (Data-Protection-signed, 60s, single-use via MemoryCache). No DB migration.
- Live permissions: per-user rights cache (`UserRightsCache`/MemoryCache) refreshed into the session by
  `LoginCookieMiddleware` each request and invalidated on write by `UserRightService`. Admin rights
  changes take effect on the user's next request (no re-login). No DB migration.
- Steam name resolution: global LRU cache (`ISteamNameCache` singleton, last 100 SteamIDs) shared across
  requests; Stats + poll-responses pages use batched `GetPrettyNamesAsync` (≤100 ids/call). No DB migration.
- UI/UX: `.poll-panel` styling, square-✕ radios/checkboxes, drag-to-rank (FLIP animation),
  Add-Poll client validation, button-visibility + modal-backdrop fixes.
- Home/layout: SteamID moved to the footer (bottom-right, every page); home page shows the logged-in
  user's unanswered open polls. No DB migration.
- Login gate: not-logged-in users hitting gated pages now go to `/auth/login` ("Please log in" + Steam
  button) and are returned to the original page after login (`returnUrl` threaded through Steam OpenID).
  No DB migration.
- Stats upload API: `POST /api/Stats` (key in `X-Api-Key` header) full-replaces `wwwroot/data/roundData.json`
  via `DataWriterService.WriteRoundData`. File-backed, **no DB migration**. (Ensure the host's `wwwroot/data`
  is writable + persisted.)
- Individual poll responses: new `ViewIndividualResponses` permission (enum=8) + `GET /Polls/Responses/{id}`
  (per-user answer table, SteamID→name resolved via batched `SteamService.GetPrettyNamesAsync`, ≤100 ids/call)
  + button on the Results page. Manage-Polls
  cards gained a "View results" button; the header gained a "Permissions" tab for `ManageRights` holders.
  **No DB migration** (permission is just an enum value). GDPR/Privacy updated (responses are not anonymous).

## Runtime / host
- `DataProtection-Keys/` is created under the app content root at runtime (gitignored). The host path
  must be writable and **persisted across redeploys**, or login cookies invalidate on each deploy.
- Prod `appsettings.Production.json` (gitignored) must have: DB host `5.182.204.32:27000`, Steam API
  key, and GUID `ApiPrivateKeys`. (Set as of 2026-06-23.)
- ⬜ **New `GModDb` connection string** (gitignored env settings, dev + prod) for the Pointshop inventory
  page — point it at the game DB (`Server=5.182.204.32;Port=27000;Database=GMod;User=...;Password=...`).
  Base `appsettings.json` carries a placeholder only. Privileges the user needs:
  `SELECT ON GMod.*` (read the inventory) **plus** `UPDATE` on `GMod.ps2_equipmentslot` (unequip),
  `GMod.kinv_items` (unequip + market escrow/transfer) and `GMod.ps2_wallet` (market points transfer).
  Still no DDL/migration on that schema.

## Repo-only (no deploy)
- `GMOD_API.md`, `GMOD_AUTH.md`, `DEPLOY.md` — docs.
- `gmod/` — reference GMod Lua (server + client) that consumes the APIs; runs on the game server, not the website.
