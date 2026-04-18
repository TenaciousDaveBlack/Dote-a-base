using Dalamud.Configuration;

namespace DoteTracker;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public int LogSize { get; set; } = 100;

    /// <summary>Whether the main window was open when the plugin was last unloaded.</summary>
    public bool ShowUI { get; set; } = false;

    /// <summary>Maximum distance (in yalms) to include a player in the nearby list.</summary>
    public float ScanDistance { get; set; } = 50.0f;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
