# Loot Distribution Info

Dalamud plugin for tracking loot messages in game.

It listens to Dalamud chat/log events, stores matching loot lines, enriches them with zone/player/item context where possible, and shows the result in a searchable history window.

License: GPL-3.0.

## Quick Info

| Item | Value |
| --- | --- |
| Command | `/lootinfo` |
| Settings | `/lootinfo config` |
| Dalamud API | 15 |
| Current version | `2.0.0-beta` |
| Release tag | `v2.0.0-beta` |

## Features

- Captures loot-style chat/log messages.
- Tracks zone, timestamp, recipient, quantity, and item text.
- Resolves item metadata when payloads or game data make that possible.
- Groups, filters, sorts, and searches the captured history.
- Supports favorites, item blacklist, compact mode, and configurable columns.
- Tracks Need/Greed/Pass roll messages and attaches them to matching loot records.
- Includes a debug window for checking capture/parser behavior in game.

## Matching Notes

The matcher is intentionally practical rather than magical.

It catches common loot lines such as:

```text
You obtain 368 gil.
You obtain a bottle of desert saffron.
Player X obtains a loot item.
```

Structured payloads are preferred when Dalamud exposes them. Text parsing is kept as fallback because not every log line carries the same useful payloads.

The raw line is always stored so odd cases can still be inspected later.

## In-Game Usage

Run:

```text
/lootinfo
```

The main window has:

- a compact monitor view
- a full history browser
- item detail tables
- overview stats

Run:

```text
/lootinfo config
```

Useful settings include:

- save history between sessions
- max history size
- compact mode default
- item icons/tooltips
- debug tools
- default grouping, sorting, and quick filter
- visible columns
- hidden categories and blacklist entries

## Custom Repo

The custom repository metadata lives at the workspace root:

```text
scyt.repo.json
```

Current release target:

- repository: `https://github.com/Scytraiin/ffxiv-loot-distribution`
- tag: `v2.0.0-beta`
- asset name: `latest.zip`
- generated release package: `out/release/latest.zip`
- generated repo metadata: `out/release/scyt.repo.json`

## Development

From the workspace root:

```bash
docker build -t loot-distribution-info-ci .
docker run --rm loot-distribution-info-ci
```

To build the plugin package, mount a valid Dalamud dev folder:

```bash
docker run --rm \
  -v "/path/to/your/Hooks/dev:/dalamud:ro" \
  -v "$PWD/out:/out" \
  loot-distribution-info-ci
```

Output:

- `out/plugin/` - raw build output from the container
- `out/release/latest.zip` - release ZIP for GitHub
- `out/release/scyt.repo.json` - repo metadata to publish

## Release Checklist

1. Update `LootDistributionInfo.csproj`.
2. Update `scyt.repo.json`.
3. Add release notes under `release-notes/`.
4. Build with the local Dalamud dev folder.
5. Smoke test in game.
6. Create the GitHub release.
7. Upload `latest.zip`.
8. Publish the updated repo JSON.

## Test Coverage

The test project covers:

- loot matcher behavior
- quantity parsing
- recipient resolution
- roll extraction and pending-roll matching
- history dedupe and trimming
- browser/filter logic
- repository metadata alignment
