# FFYIV Workspace

> Workspace for a Dalamud plugin experiment centered around loot-line capture, local references, and lightweight build validation.

---

## Overview

This workspace currently contains one active plugin project:

- `Loot Distribution Info`

It also contains:

- a standalone test project for the pure matching/history logic
- a custom Dalamud repository JSON
- Docker-based validation support
- local reference codebases kept for research and comparison

---

## Active Projects

### Plugin

- `/Users/rene.lackenbucher/Documents/FFYIV/LootDistributionInfo`

This is the actual Dalamud plugin project. It captures loot-like chat/log lines and shows them in a small in-game history window.

### Tests

- `/Users/rene.lackenbucher/Documents/FFYIV/LootDistributionInfo.Tests`

This project contains unit tests for the pure logic:

- matcher behavior
- dedupe behavior
- retention trimming
- repository metadata alignment

### Custom Repo Metadata

- `/Users/rene.lackenbucher/Documents/FFYIV/scyt.repo.json`

This is the custom Dalamud repository file intended for later hosting.

---

## Reference Material

These folders are present as local references only:

- `/Users/rene.lackenbucher/Documents/FFYIV/Dalamud-master`
- `/Users/rene.lackenbucher/Documents/FFYIV/FFXIV-ProximityVoiceChat-master`

They are **not** part of the active plugin implementation.

They exist to provide:

- API reference material
- project layout examples
- examples of larger real-world Dalamud plugin structure

---

## Docker Validation

Workspace root includes Docker support for isolated validation:

- `/Users/rene.lackenbucher/Documents/FFYIV/Dockerfile`
- `/Users/rene.lackenbucher/Documents/FFYIV/.dockerignore`

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

- `/Users/rene.lackenbucher/Documents/FFYIV`

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
5. Replace placeholder repo URLs before publishing.

---

## Status

This workspace is set up for iterative plugin development.

Current state:

- plugin project exists
- tests exist
- custom repo metadata exists
- Docker validation exists
- publishing URLs are still placeholders

For plugin-specific details, see:

- `/Users/rene.lackenbucher/Documents/FFYIV/LootDistributionInfo/README.md`
