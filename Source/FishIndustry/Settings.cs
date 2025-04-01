using UnityEngine;
using Verse;

namespace FishIndustry;

public class Settings : ModSettings
{
    public static float fishRespawnRateFactor = 1f;

    public static float fishBreedQuantityFactor = 1f;

    public static bool biomeRestrictionsIsEnabled = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref fishRespawnRateFactor, "fishRespawnRateFactor", 1f);
        Scribe_Values.Look(ref fishBreedQuantityFactor, "fishBreedQuantityFactor", 1f);
        Scribe_Values.Look(ref biomeRestrictionsIsEnabled, "biomeRestrictionsIsEnabled", true);
    }

    public static void DoSettingsWindowContents(Rect inRect)
    {
        var listing_Standard = new Listing_Standard
        {
            ColumnWidth = inRect.width / 2f
        };
        listing_Standard.Begin(inRect);
        listing_Standard.Label(
            "FishIndustry.Settings_FishRespawnRateFactorLabel".Translate(fishRespawnRateFactor.ToString("0.0")), -1f,
            "FishIndustry.Settings_FishRespawnRateFactorDesc".Translate());
        fishRespawnRateFactor = listing_Standard.Slider(fishRespawnRateFactor, 0.1f, 100f);
        listing_Standard.Label(
            "FishIndustry.Settings_FishBreedQuantityFactorLabel".Translate(fishBreedQuantityFactor.ToString("0.0")),
            -1f, "FishIndustry.Settings_FishBreedQuantityFactorDesc".Translate());
        fishBreedQuantityFactor = listing_Standard.Slider(fishBreedQuantityFactor, 0.1f, 100f);
        listing_Standard.Gap();
        listing_Standard.CheckboxLabeled("FishIndustry.Settings_BiomeRestrictionLabel".Translate(),
            ref biomeRestrictionsIsEnabled, "FishIndustry.Settings_BiomeRestrictionDesc".Translate());
        listing_Standard.Gap();
        if (listing_Standard.ButtonText("Reset default values"))
        {
            fishRespawnRateFactor = 1f;
            fishBreedQuantityFactor = 1f;
            fishBreedQuantityFactor = 1f;
            biomeRestrictionsIsEnabled = true;
        }

        if (Controller.currentVersion != null)
        {
            listing_Standard.Gap();
            GUI.contentColor = Color.gray;
            listing_Standard.Label("FishIndustry.Settings_CurrentModVersion".Translate(Controller.currentVersion));
            GUI.contentColor = Color.white;
        }

        listing_Standard.End();
    }
}