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
- **No further migrations needed** for anything after `AddPollsAndRewards` — the reward payload trim,
  `/{rewardType}` filter, header auth, ViewPolls default, and all UI work are code-only.

## Code (needs app redeploy + restart)
All in the current build; the running instance must be restarted/redeployed:
- Persistent Steam login cookie (`WolflineLogin`) + session-rehydration middleware + Data Protection keys.
- Polls system (Basic / MultiSelect / Ranking, voting UI, results, management) + **ViewPolls default-on**.
- Reward system + GMod API: `GET /rewards/pending`, `GET /rewards/pending/{rewardType}`,
  `POST /rewards/claim` — all auth via `X-Api-Key` header (POSTs also accept body key).
- In-game auto-login: `POST /auth/gmod/token` (mint, key-protected) + `GET /auth/gmod` (consume) +
  `GmodAuthTokenService` (Data-Protection-signed, 60s, single-use via MemoryCache). No DB migration.
- Live permissions: per-user rights cache (`UserRightsCache`/MemoryCache) refreshed into the session by
  `LoginCookieMiddleware` each request and invalidated on write by `UserRightService`. Admin rights
  changes take effect on the user's next request (no re-login). No DB migration.
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

## Runtime / host
- `DataProtection-Keys/` is created under the app content root at runtime (gitignored). The host path
  must be writable and **persisted across redeploys**, or login cookies invalidate on each deploy.
- Prod `appsettings.Production.json` (gitignored) must have: DB host `5.182.204.32:27000`, Steam API
  key, and GUID `ApiPrivateKeys`. (Set as of 2026-06-23.)

## Repo-only (no deploy)
- `GMOD_API.md`, `GMOD_AUTH.md`, `DEPLOY.md` — docs.
- `gmod/` — reference GMod Lua (server + client) that consumes the APIs; runs on the game server, not the website.
