# Loot Distribution Info

> A Dalamud plugin that tracks loot messages and keeps a searchable in-game history of where the loot happened and who received it.

License: GPL-3.0. The implementation is clean-room even where feature direction was inspired by other loot-tracking plugins.

---

## At a Glance

| Topic | Details |
| --- | --- |
| Plugin | `Loot Distribution Info` |
| Command | `/lootinfo` |
| Settings | `/lootinfo config` |
| Goal | Capture loot messages and show `when / where / who / what` in a searchable history |
| Current approach | Broad loot matching with zone capture and recipient verification |
| Scope | Read-only observation through standard Dalamud services |

---

## What It Does

- Watches `ChatMessage` and `LogMessage` through Dalamud.
- Detects lines containing loot-style verbs such as `obtain`, `obtained`, and `obtains`.
- Resolves the current zone and snapshots it onto each loot record.
- Attempts to determine who got the loot, including party/alliance verification when possible.
- Resolves item metadata such as icon, rarity, and item classification when the game data allows it.
- Stores captured lines in memory and, by default, persists them across sessions.
- Shows a newest-first history list in the plugin window.
- Lets you filter the captured history and clear it when needed.
- Offers Debug Mode with a live debug log for capture and parser activity.

## What It Does Not Do

- It does not hook packets or use unsupported runtime tricks.
- It does not suppress, rewrite, or re-print chat lines.
- It does not attempt perfect semantic parsing for every system line.
- It does not support export or multi-language matching yet.

---

## How Matching Works

The matcher intentionally stays broad so it catches the common visible loot lines.

It normalizes each incoming line and looks for loot-style verbs:

- `obtain`
- `obtained`
- `obtains`

It then tries to enrich the match with:

- the current zone
- the recipient name
- a cleaned loot text

That means it should catch common lines such as:

- `You obtain 368 gil.`
- `You obtain a bottle of desert saffron.`
- `Player X obtains a loot item.`

Because the matching is intentionally broad, some false positives are still possible. The raw line is always kept as fallback context.

---

## In-Game Usage

### Main Window

- Run `/lootinfo` to open the plugin window.
- The main view shows captured loot records in a newest-first list.
- Use the filter field to narrow the list.
- Use `Clear history` to wipe the current captured history.
- When parsing is incomplete, the raw line remains visible so the event is still readable.

### Settings

Run `/lootinfo config` to open the settings window.

Available settings:

- `Save history between sessions`
- `History size`
- `Show debug tools`

---

## Repository for Dalamud

The workspace root contains a custom repository file:

- `scyt.repo.json`

Current state:

- the repository URL points to the real GitHub repository
- the custom repo JSON targets the current alpha release asset
- the release asset must be uploaded as `latest.zip`
- the icon asset is `assets/branding/Loot_History_v1.png`

Current release target:

- repo: `https://github.com/Scytraiin/ffxiv-loot-distribution`
- tag: `v0.2.0-beta`
- asset: `latest.zip`

---

## Project Layout

- plugin: `LootDistributionInfo/`
- tests: `LootDistributionInfo.Tests/`
- custom repo metadata: `scyt.repo.json`
- canonical feature reference: `Feature_detail.md`

---

## Local Development Workflow

1. Update the project version and release tag when publishing a new version.
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

- workspace root

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
- GitHub-hosted CI is great for tests and validation, but not enough for the final plugin package by itself
- keeping release packaging manual keeps the workflow predictable

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
- recipient parsing and verification confidence
- dedupe behavior across chat/log capture
- retention trimming
- metadata alignment between `scyt.repo.json` and `LootDistributionInfo.json`

---

## Commands Reference

- `/lootinfo` opens the main window
- `/lootinfo config` opens the config window

---

## Current Status

This project is in an active alpha release state:

- the loot history UI is implemented
- zone and recipient enrichment are implemented
- item classification, item icons, rarity styling, and overview stats are implemented
- Debug Mode is implemented
- Docker-based validation is working
- release artifacts can be generated locally and uploaded to GitHub Releases
