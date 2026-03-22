# FFYIV Workspace

> Workspace for the Loot Distribution Info Dalamud plugin, its tests, and its release tooling.

---

## Overview

This workspace currently contains one active plugin project:

- `Loot Distribution Info`

It also contains:

- a standalone test project for the pure matching/history logic
- a custom Dalamud repository JSON
- Docker-based validation support
- tracked feature documentation in `Feature_detail.md`

---

## Active Projects

### Plugin

- `LootDistributionInfo/`

This is the actual Dalamud plugin project. It captures loot-like chat/log lines and shows them in a small in-game history window.

### Tests

- `LootDistributionInfo.Tests/`

This project contains unit tests for the pure logic:

- matcher behavior
- dedupe behavior
- retention trimming
- repository metadata alignment

### Custom Repo Metadata

- `scyt.repo.json`

This is the custom Dalamud repository file intended for later hosting.

---

## Docker Validation

Workspace root includes Docker support for isolated validation:

- `Dockerfile`
- `.dockerignore`

The Docker workflow is for:

- restoring packages
- running tests
- building the plugin when a valid Dalamud dev folder is mounted

It is **not** for:

- running the plugin in the game
- replacing actual Dalamud runtime testing
- installing .NET globally on the host

### Commands

Run from:

- workspace root

```bash
docker build -t loot-distribution-info-ci .
docker run --rm loot-distribution-info-ci
docker run --rm -v "$PWD/out:/out" loot-distribution-info-ci
```

If you have a real Dalamud `Hooks/dev` folder:

```bash
docker run --rm \
  -v "/path/to/your/Hooks/dev:/dalamud:ro" \
  -v "$PWD/out:/out" \
  loot-distribution-info-ci
```

---

## Suggested Workflow

1. Work inside `LootDistributionInfo`.
2. Run the logic tests in `LootDistributionInfo.Tests`.
3. Use Docker for isolated validation when useful.
4. Test the plugin in a real Dalamud game environment.
5. Publish `latest.zip` and `scyt.repo.json` when a release is ready.

---

## Status

This workspace is set up for iterative plugin development.

License:

- GPL-3.0
- see `LICENSE`
- implementation remains clean-room even where feature direction was inspired by other loot-tracking plugins

Current state:

- plugin project exists
- tests exist
- custom repo metadata exists
- Docker validation exists
- release-ready artifacts can be exported to `out/release`
- the current release target is `v0.2.1-alpha`

For plugin-specific details, see:

- `LootDistributionInfo/README.md`
- `Feature_detail.md`
