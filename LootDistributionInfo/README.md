# Loot Distribution Info

Loot Distribution Info is a minimal Dalamud plugin that listens to chat and log output, keeps lines that look like loot acquisition messages, and shows the captured history in a small in-game window.

## What v1 does

- Watches `ChatMessage` and `LogMessage` through Dalamud.
- Keeps lines that contain `obtain`, `obtained`, or `obtains`.
- Stores captured lines in memory and, by default, persists them across sessions.
- Shows a newest-first history list in the plugin window.
- Supports `/lootinfo` and `/lootinfo config`.

## What v1 does not do

- It does not hook packets or game memory beyond standard Dalamud chat/log services.
- It does not rewrite, suppress, or re-print chat lines.
- It does not try to build a perfect loot parser yet.
- It does not support export or multi-language matching in this first version.

## Matching behavior

The matcher is intentionally broad in v1. It normalizes each incoming line and checks for the words:

- `obtain`
- `obtained`
- `obtains`

That means it should catch common lines like:

- `You obtain 368 gil.`
- `You obtain a bottle of desert saffron.`
- `Player X obtains a loot item.`

Because this is a simple wildcard-style matcher, some false positives are possible. The current goal is broad capture with low complexity.

## Repository file for Dalamud

The workspace root contains a custom repository file named `scyt.repo.json`.

When you eventually host it, add the hosted URL to Dalamud's custom plugin repositories. The file currently uses dummy GitHub URLs and must be updated before any real publishing:

- `https://github.com/example/LootDistributionInfo`
- dummy release ZIP URLs under that same repo

## Local development

Project files:

- plugin: `/Users/rene.lackenbucher/Documents/FFYIV/LootDistributionInfo`
- custom repo metadata: `/Users/rene.lackenbucher/Documents/FFYIV/scyt.repo.json`
- tests: `/Users/rene.lackenbucher/Documents/FFYIV/LootDistributionInfo.Tests`

Typical workflow:

1. Replace the dummy GitHub URLs in the manifest and `scyt.repo.json`.
2. Build the plugin with the local .NET SDK and Dalamud toolchain installed.
3. Host the plugin ZIP and `scyt.repo.json`.
4. Add the hosted repo URL to Dalamud.

## Docker Validation

Docker support is additive infrastructure around this project. It does not change the plugin structure, plugin behavior, matcher logic, repo metadata shape, or the existing tests.

Use these commands from `/Users/rene.lackenbucher/Documents/FFYIV`:

```bash
docker build -t loot-distribution-info-ci .
docker run --rm loot-distribution-info-ci
docker run --rm -v "$PWD/out:/out" loot-distribution-info-ci
```

What this Docker workflow does:

- restores NuGet packages inside the container
- runs the unit tests
- builds the Windows-targeted plugin with Windows targeting enabled when a valid `DALAMUD_HOME` is mounted
- optionally exports the compiled build output to `./out/plugin`

What it does not do:

- it does not install .NET on the host system
- it does not run the plugin inside FFXIV or Dalamud
- it does not change the existing project structure

Notes:

- package restore happens inside Docker and needs network access
- plugin compilation requires a real Dalamud `Hooks/dev` folder to be mounted into the container as `/dalamud`
- if you have a local Dalamud installation, the command shape is:

```bash
docker run --rm \
  -v "/path/to/your/Hooks/dev:/dalamud:ro" \
  -v "$PWD/out:/out" \
  loot-distribution-info-ci
```

- the optional export command writes compiled plugin output to `./out/plugin`
- if you only want test validation, `docker run --rm loot-distribution-info-ci` is still useful and will stop after the tests with a clear message about the missing Dalamud dev folder

## Testing

The test project covers:

- wildcard matcher positive and negative cases
- dedupe behavior across chat/log capture
- retention trimming
- repository metadata alignment between `scyt.repo.json` and `LootDistributionInfo.json`

## Commands

- `/lootinfo` opens the main window
- `/lootinfo config` opens the config window
