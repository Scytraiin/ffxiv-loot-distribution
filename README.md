# FFXIV Loot History

Workspace for the `Loot Distribution Info` Dalamud plugin.

The repo contains the plugin, unit tests, local Docker validation, release notes, and custom repo metadata.

## Projects

- `LootDistributionInfo/` - the Dalamud plugin.
- `LootDistributionInfo.Tests/` - unit tests for parser, history, roll tracking, and metadata behavior.
- `scyt.repo.json` - custom Dalamud repository metadata.
- `release-notes/` - release notes used for GitHub releases.

## Plugin

`Loot Distribution Info` watches loot-related chat/log messages and keeps a searchable in-game history of:

- when loot was seen
- where it dropped
- who received it
- what item/currency was involved

Open it in game with:

```text
/lootinfo
```

Open settings with:

```text
/lootinfo config
```

## Local Validation

The Docker workflow keeps local validation reproducible without requiring a global .NET install on the host.

Run from the workspace root:

```bash
docker build -t loot-distribution-info-ci .
docker run --rm loot-distribution-info-ci
```

To build the actual plugin package, mount a Dalamud dev folder:

```bash
docker run --rm \
  -v "/path/to/your/Hooks/dev:/dalamud:ro" \
  -v "$PWD/out:/out" \
  loot-distribution-info-ci
```

Build output is exported to:

```text
out/plugin/
```

Release-ready artifacts are exported to:

```text
out/release/
```

## Release

Current release target:

- tag: `v2.0.0-beta`
- package: `out/release/latest.zip`
- repo metadata: `out/release/scyt.repo.json`

Manual release flow:

1. Update the plugin version and release notes.
2. Build with a valid Dalamud dev folder.
3. Test in game.
4. Create the GitHub release tag.
5. Upload `latest.zip`.
6. Publish the updated `scyt.repo.json`.

## Status

- License: GPL-3.0, see `LICENSE`.
- Dalamud API target: 15.
- Current plugin version: `2.0.0-beta`.
- Runtime build validated against local Dalamud `15.0.0.2/dev`.

For more detail, see:

- `LootDistributionInfo/README.md`
- `Feature_detail.md`
- `Architecture.md`
