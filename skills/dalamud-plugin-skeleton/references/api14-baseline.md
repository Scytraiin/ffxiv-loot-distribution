# Dalamud API 14 Baseline

Snapshot date: 2026-03-11.

## Official baseline

- Use the current Dalamud plugin-development docs and the API 14 notes as the primary source of truth:
  - https://dalamud.dev/plugin-development/project-layout-and-configuration/
  - https://dalamud.dev/versions/api14/
  - https://dalamud.dev/faq/
- Prefer the official `SamplePlugin` repository for starter shape when the docs are ambiguous:
  - https://github.com/goatcorp/SamplePlugin
- Default to SDK-style C# projects with `Dalamud.NET.Sdk` and .NET 10.
- Keep the assembly name, project name, and manifest `InternalName` aligned unless the user explicitly wants otherwise.
- Keep a manifest file named `<InternalName>.json` unless the user explicitly asks to move metadata into the project file.

## Local workspace findings

- `Dalamud-master/` is the local framework source and targets `net10.0-windows`.
- `Dalamud-master/Dalamud/Plugin/IDalamudPlugin.cs` shows that a plugin entrypoint only needs to implement `IDalamudPlugin` and `IDisposable`.
- `Dalamud-master/Dalamud/Plugin/IDalamudPluginInterface.cs` exposes the baseline services used by a starter:
  - `UiBuilder`
  - `GetPluginConfig()` / `SavePluginConfig(...)`
  - `Create<T>(...)` / `Inject(...)`
- `Dalamud-master/Dalamud/Interface/UiBuilder.cs` shows the UI callbacks a starter should wire:
  - `Draw`
  - `OpenMainUi`
  - `OpenConfigUi`

## What the starter should include

- One plugin entrypoint class.
- One slash command with a non-empty help message.
- One configuration class implementing `IPluginConfiguration`.
- One main window and one config window wired through `WindowSystem`.
- `UiBuilder.Draw`, `UiBuilder.OpenMainUi`, and `UiBuilder.OpenConfigUi` handlers.
- A manifest with real `Name`, `Author`, `Punchline`, `Description`, and useful tags.

## What the starter should exclude by default

- External backends or web servers.
- Native DLL loading.
- WebRTC or other media stacks.
- A DI container like Ninject.
- Hooks, signatures, or FFXIVClientStructs-heavy code.

## Why this matters in this workspace

`FFXIV-ProximityVoiceChat-master/` is a good advanced reference for config persistence, commands, and window wiring, but it also carries plugin-specific complexity:

- WebRTC and native DLL loading
- a Node/Socket.IO signaling server
- Ninject-based composition
- audio and input subsystems

Do not copy that architecture into a baseline starter unless the user asks for those features.

## Validation reminders

- `Dalamud-master/Dalamud/Plugin/Internal/PluginValidator.cs` warns when plugins omit config or main UI callbacks.
- The same validator treats missing command help text, name, author, or an outdated API level as serious problems.
