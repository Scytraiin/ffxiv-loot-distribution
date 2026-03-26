# Loot History Feature Detail

This document is the canonical feature reference for the plugin. It describes the intended user-facing behavior of the current implementation and the important limits that shape it.

## Loot Capture

### What it does

Captures loot-like lines from Dalamud chat and log streams and turns them into persisted loot history records.

### How it appears in the UI

New loot entries appear in the `Loot History` and `Item Details` tabs in newest-first order.

### What data it depends on

- `ChatMessage`
- `LogMessage`
- the broad loot matcher based on `obtain`, `obtained`, and `obtains`

### Fallback behavior

If a line matches broadly but some fields cannot be enriched, the plugin still stores the event and keeps the original raw line.

### Limits / known gaps

- Matching is intentionally broad and may still allow some false positives.
- Multi-language support is not implemented.

## Zone Capture

### What it does

Snapshots the current zone name at the time the loot line is captured.

### How it appears in the UI

The zone is shown in the `Zone` column in all main history views.

### What data it depends on

- `IClientState.TerritoryType`
- `TerritoryType.PlaceName`

### Fallback behavior

If the zone cannot be resolved, the plugin shows `Unknown` or a territory fallback.

### Limits / known gaps

- The plugin stores the resolved place name only, not richer duty metadata.

## Recipient Detection And Group Labeling

### What it does

Tries to determine who received the loot and classifies the recipient into a simplified group label.

### How it appears in the UI

- `Who` column
- `Group` column with `Self`, `Party/Alliance`, or `Other`

### What data it depends on

- loot-line prefix text
- local player name
- current party/alliance member list

### Fallback behavior

If the name cannot be parsed confidently, the record remains visible and the raw line still explains the event.

### Limits / known gaps

- Only strong two-word name shapes are treated as text-based recipients.
- Normal UI intentionally compresses confidence into three group labels.

## Quantity Extraction

### What it does

Splits the captured loot text into a numeric quantity and an item name for normal loot rows.

### How it appears in the UI

- `Quantity` column
- `Loot` column without the leading amount

### What data it depends on

- parsed loot text after the obtain/obtained/obtains verb
- simple leading-number extraction

### Fallback behavior

If no explicit amount is present, the plugin stores `Quantity = 1` and keeps the item name as-is.

### Limits / known gaps

- Quantity extraction only looks for a leading integer followed by a space.
- It does not attempt plural normalization.

## Item Classification

### What it does

Classifies resolved items using the local game item sheet, primarily from `FilterGroup`, refined by `EquipSlotCategory` when appropriate.

### How it appears in the UI

- `Category`
- `Filter Group`
- `Equip Slot`
- `UI Category`
- `Search Category`
- `Sort Category`

### What data it depends on

- local item sheet lookup
- exact normalized item-name matching
- payload-backed item resolution when possible

### Fallback behavior

If the item cannot be resolved, the primary category becomes `Unknown` and lower-level item metadata remains empty.

### Limits / known gaps

- No fuzzy matching is used.
- `gil` and other unresolved rows are not synthesized into categories in this pass.

## Item Icons

### What it does

Shows the in-game icon for resolved items in the main loot views.

### How it appears in the UI

An `Icon` column is shown in both `Loot History` and `Item Details`.

### What data it depends on

- resolved `IconId`
- Dalamud `ITextureProvider`

### Fallback behavior

If the icon cannot be resolved or loaded, the row remains usable and shows a simple placeholder.

### Limits / known gaps

- Icons are only available when item identity resolution succeeds.

## Rarity Styling

### What it does

Applies item-name text color based on known item rarity.

### How it appears in the UI

Loot names in the history tables and overview item list are tinted by rarity when known.

### What data it depends on

- resolved item rarity from the local item sheet

### Fallback behavior

If rarity is unknown, the plugin uses standard text coloring.

### Limits / known gaps

- Only the known rarity tiers are specially colored.

## Item Tooltips

### What it does

Shows a compact metadata tooltip when hovering item icons or loot names.

### How it appears in the UI

Tooltip content includes:

- item name
- quantity
- HQ marker if present
- timestamp
- zone
- group label
- category
- filter group
- equip slot
- raw line

### What data it depends on

- resolved record metadata
- tooltip toggle setting

### Fallback behavior

If some metadata is unresolved, the tooltip still shows the fields that are known plus the raw line.

### Limits / known gaps

- The tooltip is a plugin summary, not a full game-native item tooltip.

## Blacklist

### What it does

Lets users hide specific resolved items from the normal history views.

### How it appears in the UI

- right-click item icon or loot name to hide the item
- manage blacklisted items in settings

### What data it depends on

- resolved `ItemId`
- persisted configuration blacklist

### Fallback behavior

If the row has no resolved `ItemId`, blacklist actions are not offered.

### Limits / known gaps

- Blacklisting is item-based only.
- It does not support text rules or category-wide hiding.

## Filters / Search

### What it does

Lets users narrow the visible history rows without deleting data.

### How it appears in the UI

Shared controls above the tabs:

- search box
- `Self only`
- group filter
- loot type filter
- category filter
- zone filter

### What data it depends on

- current in-memory/persisted records
- resolved category and zone fields

### Fallback behavior

If filters exclude all rows, the UI shows a no-match message instead of clearing history.

### Limits / known gaps

- Filters operate on stored text/metadata only and do not do fuzzy matching.

## Loot History Tab

### What it does

Shows the primary day-to-day loot table for recent browsing.

### How it appears in the UI

Columns:

- `Time`
- `Zone`
- `Who`
- `Group`
- `Quantity`
- `Icon`
- `Loot`
- `Raw Line`
- debug-only `Source`

### What data it depends on

- all captured/enriched loot records
- active filters
- blacklist state

### Fallback behavior

The raw line remains visible even when parsing is incomplete.

### Limits / known gaps

- This tab is intentionally a readable history view, not a full analytics view.
- Column visibility is user-configurable through settings.

## Item Details Tab

### What it does

Shows the same filtered rows as `Loot History`, but with extended item metadata columns.

### How it appears in the UI

Adds classification and item-category metadata on top of the core loot columns.

### What data it depends on

- resolved item-sheet metadata
- active filters

### Fallback behavior

Unresolved metadata fields remain empty while the row stays visible.

### Limits / known gaps

- This is still a table view, not a drill-down inspector.
- Column visibility is user-configurable through settings.

## Compact Mode

### What it does

Provides a reduced default main-window layout for quick monitoring.

### How it appears in the UI

The compact main window shows only:

- `Who`
- `Quantity`
- `Loot`

It keeps a minimal search/settings/clear toolbar and hides the full tabbed layout.

### What data it depends on

- the same stored loot records used by the full window
- the compact-mode default setting

### Fallback behavior

If no rows match the compact search, the window shows a normal no-match message instead of hiding the history.

### Limits / known gaps

- Compact mode is meant for quick monitoring, not detailed inspection.
- Full filters and metadata tabs are only available in the standard main-window mode.

## Overview Tab

### What it does

Provides a compact summary derived from locally stored history.

### How it appears in the UI

Shows:

- total entries
- unique items
- latest item timestamp
- top zones
- top categories
- top items
- rarity breakdown

### What data it depends on

- current visible record set after filters and blacklist

### Fallback behavior

If there is not enough data, sections show a clear no-data state.

### Limits / known gaps

- Overview is intentionally lightweight and does not try to replace a full analytics system.

## Debug Mode / Debug Window

### What it does

Exposes live capture/parser diagnostics for troubleshooting.

### How it appears in the UI

- settings toggle: `Show debug tools`
- optional debug log window
- extra `Source` column in the history tables when enabled

### What data it depends on

- session-only debug event buffer
- capture/parser lifecycle events

### Fallback behavior

If debug mode is off, debug events are not collected or shown.

### Limits / known gaps

- Debug events are not persisted between sessions.

## Persistence / Configuration

### What it does

Persists loot history and user preferences between sessions when enabled.

### How it appears in the UI

Settings include:

- `Save history between sessions`
- `History size`
- `Show debug tools`
- `Show item icons`
- `Show item tooltips`
- `Default to self-only filter`
- blacklist management

### What data it depends on

- plugin configuration storage
- normalized loot records

### Fallback behavior

If persistence is disabled, history is session-only.

### Limits / known gaps

- Roll sessions and debug buffers remain session-only by design.
