using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using DoteTracker.Windows;
using EmoteLog.Data;
using EmoteLog.Hooks;
using EmoteLog.Utils;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.BannerHelper.Delegates;

namespace DoteTracker;

public sealed class Plugin : IDalamudPlugin
{
    // -------------------------------------------------------------------------
    // Service injection (Dalamud API 14)
    // -------------------------------------------------------------------------

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IObjectTable           ObjectTable      { get; private set; } = null!;
    [PluginService] internal static IClientState           ClientState      { get; private set; } = null!;
    [PluginService] internal static IChatGui               ChatGui          { get; private set; } = null!;
    [PluginService] internal static ITargetManager         TargetManager    { get; private set; } = null!;
    [PluginService] internal static ICommandManager        CommandManager   { get; private set; } = null!;
    [PluginService] internal static IPluginLog             PluginLog        { get; private set; } = null!;

    // -------------------------------------------------------------------------
    // Plugin state
    // -------------------------------------------------------------------------

    private const string CommandName = "/dotetracker";

    public  Configuration    Configuration { get; init; }
    internal DoteTrackerState DoteState    { get; } = new();

    private readonly WindowSystem windowSystem = new("DoteTracker");
    private readonly MainWindow   mainWindow;

    public EmoteReaderHooks EmoteReaderHooks { get; init; }
    public EmoteQueue EmoteQueue { get; init; }

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        PluginServices.Initialize(PluginInterface);

        EmoteReaderHooks = new EmoteReaderHooks();

        mainWindow = new MainWindow(this);
        windowSystem.AddWindow(mainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the DoteTracker window."
        });

        PluginInterface.UiBuilder.Draw       += DrawUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        if (Configuration.ShowUI)
            mainWindow.IsOpen = true;

        EmoteQueue = new EmoteQueue(this);

        PluginLog.Information("[DoteTracker] Plugin loaded.");
    }

    public void Dispose()
    {
        // Persist window visibility preference before unloading
        Configuration.ShowUI = mainWindow.IsOpen;
        Configuration.Save();

        PluginInterface.UiBuilder.Draw       -= DrawUI;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUI;

        windowSystem.RemoveAllWindows();
        mainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
        
        EmoteReaderHooks.Dispose();

        PluginLog.Information("[DoteTracker] Plugin unloaded.");
    }

    // -------------------------------------------------------------------------
    // Handlers
    // -------------------------------------------------------------------------

    private void OnCommand(string command, string args) => mainWindow.Toggle();

    private void DrawUI()       => windowSystem.Draw();
    private void ToggleMainUI() => mainWindow.Toggle();

}
