using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace HolopalmPlus;

[BepInPlugin("org.saerielle.exocolonist.holopalmplus", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Exocolonist.exe")]
public class HolopalmPlusPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        var harmony = new Harmony("HolopalmPlus");
        harmony.PatchAll();
        Logger.LogInfo("HolopalmPlus patches done.");
        ModInstance.instance = this;
    }

    public void Log(string message)
    {
        Logger.LogInfo(message);
    }
}
