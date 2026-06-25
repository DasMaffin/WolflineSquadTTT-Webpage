# Pointshop 2 Market ↔ GMod server

How the website's player-to-player **market** interacts with your Garry's Mod server. The market itself
(listings, auctions, bids) lives entirely on the website; this doc is only about what touches the **game
database** and your server, so you can keep the live game in sync.

There are **no new GMod-facing HTTP endpoints** for the market. The only channel into the game is the
existing WebSocket reload hook (see [`GMOD_WEBSOCKET.md`](GMOD_WEBSOCKET.md)). Everything else is the
website writing the game DB directly and then telling your server to re-read it.

## The one thing your server must do

When the website completes a market action it pushes, to the affected player(s):

```json
{ "hook": "MaffinAPI_PointshopReloadInventory", "args": ["<SteamID64>"] }
```

Your server reloads **that player's inventory + wallet + slots from the database**. That's the whole
contract — the website has already written the DB; the hook just makes the in-game state catch up. The
same hook is used by the "unequip" feature. Fired for **both** the buyer and the seller on a sale.

> Treat the database as the source of truth. The hook is fire-and-forget (if the socket was down the
> message is lost, but the DB is already correct and the player re-syncs on next join). The website
> **refuses any market action while no server socket is connected**, so a write never happens without a
> server present to be notified.

## Currency

**Points only.** The market never touches `premiumPoints` or `EasterEggs`. A sale moves `ps2_wallet.points`
from buyer to seller.

## What the website writes in the game DB

All of this happens on the website side (its DB user needs the matching grants); your server just needs to
**tolerate these out-of-band changes and reload on the hook**.

| Table | When | Change |
|-------|------|--------|
| `kinv_items.inventory_id` | **List** (escrow) | set to `NULL` — item leaves the inventory, held in escrow while listed |
| `kinv_items.inventory_id` | **Buy / auction win** | set to the **buyer's** inventory id — item moves to the buyer |
| `kinv_items.inventory_id` | **Cancel / auction with no bids** | set back to the **seller's** inventory id — item returns |
| `ps2_wallet.points` | **Sale completes** | buyer `- price`, seller `+ price` (one transaction) |
| `maffinapi_item_slots` | **Item enters an inventory** (buy/return) | upsert a row: the player's **lowest free slot** |
| `maffinapi_item_slots` | **List** (escrow) | delete the item's row (slot freed) |

The website does **not** touch `ps2_equipmentslot` for the market (that's the separate unequip feature).

### Escrow model

A listed item is **escrowed**: its `kinv_items.inventory_id` is `NULL` while it's on the market. It is
**not in any inventory and not equipped** — it's owned/tracked only by the website's listing row until it
sells, is cancelled, or the auction ends. On sale it moves to the buyer; on cancel/no-bids it returns to
the seller.

> ⚠️ **Escrow depends on your server not reclaiming `NULL`-inventory items.** If your inventory-load logic
> re-inserts "loose" items (no `inventory_id`, not equipped) back into a player's inventory, escrowed items
> would reappear and could be duplicated/sold twice. Please verify on the test DB that an item with
> `inventory_id = NULL` (and not in `ps2_equipmentslot`) stays out of the inventory until the website
> restores it.

## `maffinapi_item_slots` conventions

This is your addon's table (`itemId` PK → `slot`). The website both reads it (to render the grid in real
positions) and writes it (to slot items it moves on market actions). For the two sides to agree:

- **`itemId` = `kinv_items.id`** (the item instance).
- **Slots are 1-based** (`1` = first slot).
- **A slot holds a stack** — multiple instances of the same item share one `slot` value; they render as a
  single tile with a count. The website assigns a new item the **lowest free slot number** for that player
  (it does **not** merge it into an existing identical stack — if you'd prefer auto-stacking on the game
  side, that's fine, the website just won't fight you).
- **Per-player** — slot numbers repeat across players; "used slots" are scoped to one player's inventory
  items.

Your server stays the authority for **in-game** moves (a player rearranging their own grid writes this
table); the website only writes it for **market** moves.

## Server-side checklist

1. Maintain the WebSocket connection and handle `MaffinAPI_PointshopReloadInventory` (see
   [`GMOD_WEBSOCKET.md`](GMOD_WEBSOCKET.md)) → reload that SteamID's inventory, wallet **and** slots from the DB.
2. Keep `maffinapi_item_slots` in sync for in-game inventory moves, matching the conventions above.
3. Treat the DB as source of truth — the website mutates `kinv_items`, `ps2_wallet`, and
   `maffinapi_item_slots` between reloads.
4. Make sure escrowed items (`inventory_id = NULL`, not equipped) are **not** auto-reclaimed into the
   inventory.

## Consistency notes (website side, for context)

- The website creates the listing row **first**, then escrows the item — so a failure can't leave an item
  escrowed without a listing.
- A sale's game-DB writes (points + item move + slot) run in a **single transaction**, then the reload
  hooks fire.
- A fixed-price listing is atomically claimed before transfer, so two buyers can't both take it.
