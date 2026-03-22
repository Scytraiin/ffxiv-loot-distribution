using System;

using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using LootDistributionInfo.Windows;

namespace LootDistributionInfo;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/lootinfo";

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commandManager;
    private readonly WindowSystem windowSystem = new("LootDistributionInfo");
    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;
    private readonly DebugWindow debugWindow;
    private readonly Configuration configuration;
    private readonly LootCaptureService lootCaptureService;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IChatGui chatGui,
        IClientState clientState,
        IDataManager dataManager,
        IPlayerState playerState,
        IPartyList partyList,
        ITextureProvider textureProvider,
        IPluginLog log)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;

        this.configuration = this.pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.configuration.Initialize(this.pluginInterface);
        this.lootCaptureService = new LootCaptureService(this.configuration, chatGui, clientState, dataManager, playerState, partyList, log);

        this.mainWindow = new MainWindow(this.lootCaptureService, this.configuration, textureProvider, this.OpenConfigUi, this.OpenDebugUi);
        this.configWindow = new ConfigWindow(this.configuration, this.lootCaptureService, this.OpenDebugUi);
        this.debugWindow = new DebugWindow(this.lootCaptureService);

        this.windowSystem.AddWindow(this.mainWindow);
        this.windowSystem.AddWindow(this.configWindow);
        this.windowSystem.AddWindow(this.debugWindow);

        this.commandManager.AddHandler(CommandName, new CommandInfo(this.OnCommand)
        {
            HelpMessage = "Open the loot history window.",
        });

        this.pluginInterface.UiBuilder.Draw += this.DrawUi;
        this.pluginInterface.UiBuilder.OpenMainUi += this.OpenMainUi;
        this.pluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;

        log.Information("Loot Distribution Info initialized.");
    }

    public void Dispose()
    {
        this.pluginInterface.UiBuilder.Draw -= this.DrawUi;
        this.pluginInterface.UiBuilder.OpenMainUi -= this.OpenMainUi;
        this.pluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;

        this.commandManager.RemoveHandler(CommandName);

        this.windowSystem.RemoveAllWindows();

        this.lootCaptureService.Dispose();
        this.mainWindow.Dispose();
        this.configWindow.Dispose();
        this.debugWindow.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        if (string.Equals(args.Trim(), "config", StringComparison.OrdinalIgnoreCase))
        {
            this.OpenConfigUi();
            return;
        }

        this.OpenMainUi();
    }

    private void DrawUi()
    {
        this.windowSystem.Draw();
    }

    private void OpenMainUi()
    {
        this.mainWindow.IsOpen = true;
    }

    private void OpenConfigUi()
    {
        this.configWindow.IsOpen = true;
    }

    private void OpenDebugUi()
    {
        if (this.configuration.DebugModeEnabled)
        {
            this.debugWindow.IsOpen = true;
        }
    }
}
