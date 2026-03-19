---
name: dalamud-plugin-skeleton
description: "Scaffold or refresh a minimal Dalamud plugin starter for FFXIV using the current API 14 shape: SDK-style C# project, plugin manifest, IDalamudPlugin entrypoint, command registration, config persistence, and ImGui window wiring. Use when Codex needs to create a new Dalamud plugin skeleton, bootstrap a dev plugin project, port an older plugin to a cleaner baseline, or extract a lightweight starter from a more complex Dalamud plugin."
metadata:
  short-description: Generate Dalamud plugin starter code
---

# Dalamud Plugin Skeleton

## Overview

Use this skill to generate a clean Dalamud plugin baseline instead of rediscovering the same project layout, manifest fields, and UI wiring from docs each time.

Prefer the bundled starter assets and renderer script over copying code from complex plugins. In this workspace, treat `Dalamud-master` as the local API source and `FFXIV-ProximityVoiceChat-master` as an advanced example, not the default baseline.

## Decide Scope

- Read `references/api14-baseline.md` before editing files.
- Default to a minimal windowed plugin with one command, one config object, `UiBuilder.Draw`, `UiBuilder.OpenMainUi`, and `UiBuilder.OpenConfigUi`.
- Add IPC, hooks, native DLL loading, FFXIVClientStructs integration, external services, or custom DI only when the user explicitly asks.
- If the user wants features inspired by `FFXIV-ProximityVoiceChat-master`, add them incrementally; do not pull in its WebRTC, Ninject, or signaling-server stack by default.

## Scaffold a New Plugin

1. Inspect the target repo for an existing `.csproj`, manifest, and plugin entrypoint.
2. Choose the plugin identity values: `InternalName`, display `Name`, namespace, slash command, author, repo URL, and tags.
3. Run `scripts/scaffold_plugin.py` with an absolute target path and the chosen metadata.
4. Patch the generated files for repo-specific services, UI, or naming after rendering.
5. If the target repo already contains a plugin, merge the generated baseline pieces instead of replacing user code wholesale.

## Render Starter Files

```bash
scripts/scaffold_plugin.py \
  --target /absolute/path/to/MyPlugin \
  --internal-name MyPlugin \
  --plugin-name "My Plugin" \
  --author "Your Name" \
  --repo-url https://github.com/you/my-plugin \
  --command /myplugin \
  --tag utility \
  --force
```

- Use `--namespace` when the C# namespace should differ from `InternalName`.
- Use repeated `--tag` flags to seed the manifest with better search terms.
- Use `--sdk-version` only after verifying a newer official `Dalamud.NET.Sdk` release.

## Generated Baseline

- `Plugin.cs`: entrypoint, command registration, window system, UI callbacks, config load/save.
- `Configuration.cs`: persisted plugin settings implementing `IPluginConfiguration`.
- `Windows/MainWindow.cs` and `Windows/ConfigWindow.cs`: minimal ImGui windows.
- `<InternalName>.csproj`: SDK-style project pinned to the requested `Dalamud.NET.Sdk`.
- `<InternalName>.json`: basic plugin manifest metadata.

## Patch the Result

- Keep command help text non-empty.
- Keep both main and config UI callbacks registered.
- Replace the placeholder punchline and description before treating the plugin as shippable.
- Inject new services via constructor parameters or `IDalamudPluginInterface.Create<T>()` before reaching for a full DI container.
- Add project-specific files only after the baseline builds or at least matches the repo's existing style.

## Verify

- Re-read `references/api14-baseline.md` if you need the official baseline again.
- Run the repo's restore/build command when the target environment has the .NET toolchain.
- Confirm the manifest has real `Name`, `Author`, `Punchline`, `Description`, and tags.
- Confirm the slash command opens the main window and `UiBuilder.OpenConfigUi` opens the settings window.
