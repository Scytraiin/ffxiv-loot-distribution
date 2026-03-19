# Loot Distribution Info

> A small Dalamud plugin that watches chat and log output for loot-acquisition lines and keeps a clean, searchable history in-game.

---

## At a Glance

| Topic | Details |
| --- | --- |
| Plugin | `Loot Distribution Info` |
| Command | `/lootinfo` |
| Settings | `/lootinfo config` |
| Goal | Capture lines like `You obtain ...` and show them in a simple history window |
| Current approach | Broad wildcard-style text matching on loot verbs |
| Scope | Read-only observation through standard Dalamud services |

---

## What It Does

- Watches `ChatMessage` and `LogMessage` through Dalamud.
- Detects lines containing loot-style verbs such as `obtain`, `obtained`, and `obtains`.
- Stores captured lines in memory and, by default, persists them across sessions.
- Shows a newest-first history list in the plugin window.
- Lets you filter the captured history and clear it when needed.

## What It Does Not Do

- It does not hook packets or use unsupported runtime tricks.
- It does not suppress, rewrite, or re-print chat lines.
- It does not attempt perfect semantic parsing yet.
- It does not support export or multi-language matching in this first version.

---

## How Matching Works

Version 1 intentionally favors simplicity over precision.

The matcher normalizes each incoming line and looks for broad loot-style verbs:

- `obtain`
- `obtained`
- `obtains`

That means it should catch common lines such as:

- `You obtain 368 gil.`
- `You obtain a bottle of desert saffron.`
- `Player X obtains a loot item.`

Because the matching is intentionally broad, some false positives are possible. That tradeoff is deliberate for the first release.

---

## In-Game Usage

### Main Window

- Run `/lootinfo` to open the plugin window.
- The main view shows captured loot-like lines in a newest-first list.
- Use the filter field to narrow the list.
- Use `Clear history` to wipe the current captured history.

### Settings

Run `/lootinfo config` to open the settings window.

Available settings:

- `Retain history between sessions`
- `Max stored entries`

---

## Repository for Dalamud

The workspace root contains a custom repository file:

- `scyt.repo.json`

That file is meant to be hosted later and added to Dalamud as a custom plugin repository.

Current state:

- the GitHub URLs are placeholders
- the release ZIP URLs are placeholders
- they must be replaced before real publishing

Current placeholder base URL:

- `https://github.com/example/LootDistributionInfo`

---

## Project Layout

- plugin: `LootDistributionInfo/`
- tests: `LootDistributionInfo.Tests/`
- custom repo metadata: `scyt.repo.json`

---

## Local Development Workflow

1. Replace the placeholder GitHub URLs in the manifest and `scyt.repo.json`.
2. Build the plugin with a real Dalamud-capable environment.
3. Test it in game through a dev plugin path or through a hosted custom repo.
4. Publish the plugin ZIP and `scyt.repo.json` once the behavior is stable.

---

## Docker Validation

Docker support in this workspace is additive infrastructure only.

It does **not**:

- change plugin behavior
- change matcher logic
- change repository metadata shape
- install .NET on your host system
- run the plugin inside FFXIV

It **does**:

- restore packages inside the container
- run the unit tests
- attempt to build the Windows-targeted plugin when a real `DALAMUD_HOME` is mounted
- optionally export build output

### Commands

Run these commands from:

- `workspace root`

```bash
docker build -t loot-distribution-info-ci .
docker run --rm loot-distribution-info-ci
docker run --rm -v "$PWD/out:/out" loot-distribution-info-ci
```

### When You Have a Real Dalamud Dev Folder

If you have a valid Dalamud `Hooks/dev` folder, mount it into the container as `/dalamud`:

```bash
docker run --rm \
  -v "/path/to/your/Hooks/dev:/dalamud:ro" \
  -v "$PWD/out:/out" \
  loot-distribution-info-ci
```

### Notes

- Package restore happens inside Docker and needs network access.
- The plugin build requires a real Dalamud `Hooks/dev` folder.
- Without that folder, the container still runs the tests and then stops with a clear message.
- Optional exported build output is written to `./out/plugin`.

---

## GitHub Actions

This workspace includes a hosted GitHub Actions workflow for safe, fast validation.

### `CI`

The hosted CI workflow is safe to run on normal GitHub-hosted runners.

It does:

- restore and run the unit tests
- build the Docker validation image as a workflow smoke check

It does not:

- build the actual Dalamud plugin package
- create release assets

### Recommended Release Flow

For now, release packaging is intentionally kept out of GitHub Actions.

Reason:

- `Dalamud.NET.Sdk` packaging depends on a real Dalamud `Hooks/dev` environment
- the reference repo in this workspace does not provide a reusable GitHub Actions solution for that
- forcing a self-hosted Windows runner adds operational overhead before the plugin itself is stable

Recommended flow:

1. Use GitHub Actions `CI` for every push and pull request.
2. Do runtime testing in a real Dalamud game environment.
3. Build the release package manually on the machine that has the working Dalamud dev environment.
4. Upload the ZIP to GitHub Releases.
5. Update `scyt.repo.json` with the real release URLs.

That keeps automation focused on what is reliable today and avoids a half-working release pipeline.

### Manual Release Checklist

When you are ready to publish a version:

1. Update the version in `LootDistributionInfo.csproj`.
2. Build the plugin on the machine that has a valid Dalamud dev setup.
3. Confirm the output ZIP and generated manifest are correct.
4. Create a GitHub release and upload the ZIP.
5. Update `scyt.repo.json` so:
   - `RepoUrl` points to the real GitHub repository
   - `AssemblyVersion` and `TestingAssemblyVersion` match the release
   - download links point to the uploaded ZIP
   - `LastUpdate` is refreshed
   - `Changelog` contains the release summary
6. Host or publish the updated `scyt.repo.json`.

---

## Test Coverage

The current test project covers:

- positive matcher cases
- negative matcher cases
- dedupe behavior across chat/log capture
- retention trimming
- metadata alignment between `scyt.repo.json` and `LootDistributionInfo.json`

---

## Commands Reference

- `/lootinfo` opens the main window
- `/lootinfo config` opens the config window

---

## Current Status

This project is in a pragmatic v1 state:

- the plugin structure exists
- the capture logic exists
- the custom repo metadata exists
- Docker-based validation exists
- publishing URLs are still placeholders

That makes it ready for iterative testing, not final public release.
