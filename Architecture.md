# Loot History Architecture

This document explains how the plugin is structured at runtime and how the main pieces interact. It is intentionally focused on the current implementation rather than aspirational design.

## System Overview

The plugin has four major layers:

1. Dalamud integration and window wiring
2. Capture and enrichment
3. Persistence and in-memory history
4. UI browsing and diagnostics

## Main Components

| Component | Responsibility |
| --- | --- |
| `Plugin` | Creates services and windows, registers commands, hooks Dalamud UI callbacks |
| `LootCaptureService` | Owns chat/log subscriptions, matching, enrichment, dedupe, persistence, and debug events |
| `LootMatcher` | Parses raw loot-like text into a normalized loot payload |
| `LootRecipientResolver` | Resolves `who` from raw subject text plus structured payload/log data and party/alliance state |
| `ItemClassificationService` | Resolves item metadata from item id or normalized item name |
| `LootHistory` | Stores in-memory records and handles dedupe/trim behavior |
| `MainWindow` | Renders compact mode, full history browser, item details, and overview |
| `ConfigWindow` | Persists user preferences, column visibility, favorites defaults, and blacklist management |
| `DebugWindow` | Shows session-only debug events collected by the capture service |

## Runtime Flow

### Plugin Startup

```mermaid
flowchart TD
    A["Dalamud loads plugin"] --> B["Plugin constructor"]
    B --> C["Load and normalize Configuration"]
    C --> D["Create LootCaptureService"]
    D --> E["Subscribe to ChatMessage, LogMessage, TerritoryChanged"]
    B --> F["Create MainWindow, ConfigWindow, DebugWindow"]
    F --> G["Register windows in WindowSystem"]
    G --> H["Register /lootinfo command"]
    H --> I["Hook UiBuilder callbacks"]
```

### Capture Pipeline

```mermaid
flowchart LR
    A["ChatMessage or LogMessage"] --> B["LootCaptureService"]
    B --> C["Flatten text / extract raw line"]
    C --> D{"LootMatcher.TryMatch"}
    D -- "No match" --> E["Drop line"]
    D -- "Matched" --> F["BuildRecord"]
    F --> G["LootRecipientResolver"]
    F --> H["Item payload lookup"]
    F --> I["ItemClassificationService"]
    F --> J["Zone snapshot + loot type bucket"]
    G --> K["LootRecord.Normalize"]
    H --> K
    I --> K
    J --> K
    K --> L{"LootHistory.TryAdd dedupe"}
    L -- "Duplicate" --> M["Skip store"]
    L -- "New record" --> N["Persist configuration history"]
    N --> O["MainWindow / Overview / DebugWindow see updated state"]
```

### Recipient Resolution Strategy

```mermaid
flowchart TD
    A["Subject text from matched loot line"] --> B{"Is subject 'You'?"}
    B -- "Yes" --> C["Resolve to local player = Self"]
    B -- "No" --> D["Collect structured candidates"]
    D --> E["PlayerPayload world/name"]
    D --> F["ILogMessageEntity world/name"]
    E --> G["Compare against current party/alliance"]
    F --> G
    G --> H{"Exact name + world match?"}
    H -- "Yes" --> I["Party/Alliance verified"]
    H -- "No" --> J{"Unique base-name match?"}
    J -- "Yes" --> I
    J -- "No" --> K{"Looks like a real two-word name?"}
    K -- "Yes" --> L["Text-only recipient"]
    K -- "No" --> M["Unknown recipient"]
```

### Item Resolution Strategy

```mermaid
flowchart TD
    A["Matched item name from LootMatcher"] --> B{"ItemPayload present?"}
    B -- "Yes" --> C["Use ItemId from payload"]
    B -- "No" --> D["Normalize item text"]
    C --> E["Classify by item id"]
    D --> F["Exact item-sheet lookup by normalized name"]
    E --> G["Resolved icon, rarity, categories"]
    F --> G
    G --> H{"No sheet match?"}
    H -- "Yes" --> I["Keep row with Unknown classification"]
    H -- "No" --> J["Persist metadata on LootRecord"]
```

## Persistence Model

The plugin keeps one canonical loot record shape in memory and, when configured, persists a snapshot of that history into the plugin configuration.

```mermaid
flowchart LR
    A["Configuration.StoredRecords"] --> B["Configuration.Normalize / migrations"]
    B --> C["LootHistory in memory"]
    C --> D["New captures"]
    D --> E["Trim to MaxEntries"]
    E --> F["Snapshot back to Configuration.StoredRecords"]
    F --> G["IDalamudPluginInterface.SavePluginConfig"]
```

Important notes:

- history persistence is immediate after clear/capture/config changes
- debug events are session-only and never written into config
- favorites and blacklist are persisted as item id lists in configuration

## UI Architecture

### Window Responsibilities

```mermaid
flowchart TD
    A["WindowSystem"] --> B["MainWindow"]
    A --> C["ConfigWindow"]
    A --> D["DebugWindow"]

    B --> E["Compact view"]
    B --> F["Full browser view"]
    F --> G["Loot History tab"]
    F --> H["Item Details tab"]
    F --> I["Overview tab"]

    G --> J["LootHistoryBrowser.FilterAndSort"]
    G --> K["LootHistoryBrowser.Group"]
    H --> J
    I --> L["LootOverviewSummary.Build"]

    C --> M["Configuration changes"]
    M --> N["LootCaptureService.ApplyConfigurationChanges"]
    D --> O["DebugEventBuffer"]
```

### Full Browser Rendering Flow

```mermaid
flowchart TD
    A["MainWindow.Draw"] --> B{"Compact default enabled?"}
    B -- "Yes" --> C["Render compact search + 3-column table"]
    B -- "No" --> D["Collect non-blacklisted records"]
    D --> E["Build browse options from UI state"]
    E --> F["FilterAndSort"]
    F --> G["Group"]
    G --> H["Render header + toolbar"]
    H --> I["Render tabs"]
    I --> J["Loot History browser rows"]
    I --> K["Item Details table"]
    I --> L["Overview cards and buckets"]
```

## Data Shapes

### `LootRecord`

`LootRecord` is the central stored unit. The most important fields are:

- capture timestamp
- zone snapshot
- raw line
- recipient base/display/world fields
- quantity + item name
- loot type bucket
- optional item id/icon/rarity
- classification fields
- who-confidence/group source
- capture source

### `LootHistoryBrowseOptions`

The main browser does not mutate records. Instead it derives a visible slice from:

- search text
- quick filter
- sort mode
- recipient filter
- selected category
- selected zone
- favorite item ids

## Design Boundaries

The current architecture intentionally keeps these boundaries:

- matching and enrichment are offline/local only
- UI browsing never re-derives recipient or item metadata from live game state
- the full main window is richer, but compact mode stays deliberately minimal
- debug tooling is separate from the normal history browser
- no external APIs are required for core functionality

## File Map

Core files to read first:

- `LootDistributionInfo/Plugin.cs`
- `LootDistributionInfo/LootCaptureService.cs`
- `LootDistributionInfo/LootRecipientResolver.cs`
- `LootDistributionInfo/ItemClassificationService.cs`
- `LootDistributionInfo/LootHistoryBrowser.cs`
- `LootDistributionInfo/Windows/MainWindow.cs`
- `LootDistributionInfo/Windows/ConfigWindow.cs`

Supporting behavior:

- `LootDistributionInfo/LootMatcher.cs`
- `LootDistributionInfo/LootQuantityParser.cs`
- `LootDistributionInfo/LootOverviewSummary.cs`
- `LootDistributionInfo/Configuration.cs`
- `LootDistributionInfo/DebugEventBuffer.cs`
