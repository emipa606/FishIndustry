using Mlie;
using UnityEngine;
using Verse;

namespace FishIndustry;

public class Controller : Mod
{
    public static Settings settings;
    public static string currentVersion;

    public Controller(ModContentPack content)
        : base(content)
    {
        GetSettings<Settings>();
        currentVersion = VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
    }

    public override string SettingsCategory()
    {
        return "Fish Industry";
    }

    public void Save()
    {
        LoadedModManager.GetMod<Controller>().WriteSettings();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Settings.DoSettingsWindowContents(inRect);
    }
}