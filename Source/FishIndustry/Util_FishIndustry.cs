using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FishIndustry;

public static class Util_FishIndustry
{
    private static readonly Dictionary<BiomeDef, List<PawnKindDef_FishSpecies>> fishSpeciesListDico = new();

    public static ThingDef FishingPierDef => ThingDef.Named("FishingPier");

    public static ThingDef AquacultureBasinDef => ThingDef.Named("AquacultureBasin");

    public static TerrainDef FishingPierFloorShallowWaterDef => TerrainDef.Named("FishingPierFloorShallowWater");

    public static TerrainDef FishingPierFloorMarshDef => TerrainDef.Named("FishingPierFloorMarsh");

    public static ThingDef MoteFishingRodNorthDef =>
        ModLister.OdysseyInstalled ? null : ThingDef.Named("Mote_FishingRodNorth");

    public static ThingDef MoteFishingRodEastDef =>
        ModLister.OdysseyInstalled ? null : ThingDef.Named("Mote_FishingRodEast");

    public static ThingDef MoteFishingRodSouthDef =>
        ModLister.OdysseyInstalled ? null : ThingDef.Named("Mote_FishingRodSouth");

    public static ThingDef MoteFishingRodWestDef =>
        ModLister.OdysseyInstalled ? null : ThingDef.Named("Mote_FishingRodWest");

    public static ThingDef MoteFishMashgonEastDef =>
        ModLister.OdysseyInstalled ? null : ThingDef.Named("Mote_FishMashgonEast");

    public static ThingDef MoteFishMashgonWestDef =>
        ModLister.OdysseyInstalled ? null : ThingDef.Named("Mote_FishMashgonWest");

    public static ThingDef MoteFishBluebladeEastDef =>
        ModLister.OdysseyInstalled ? null : ThingDef.Named("Mote_FishBluebladeEast");

    public static ThingDef MoteFishBluebladeWestDef =>
        ModLister.OdysseyInstalled ? null : ThingDef.Named("Mote_FishBluebladeWest");

    public static ThingDef MoteFishTailteethEastDef =>
        ModLister.OdysseyInstalled ? null : ThingDef.Named("Mote_FishTailteethEast");

    public static ThingDef MoteFishTailteethWestDef =>
        ModLister.OdysseyInstalled ? null : ThingDef.Named("Mote_FishTailteethWest");

    public static ThingDef OysterDef => ThingDef.Named("Oyster");

    public static ThingDef PearlDef => ThingDef.Named("Pearl");

    public static ThingDef RawCornDef => ThingDef.Named("RawCorn");

    public static ThingDef RawRiceDef => ThingDef.Named("RawRice");

    public static JobDef FishAtFishingPierJobDef =>
        ModLister.OdysseyInstalled ? null : DefDatabase<JobDef>.GetNamed("JobDef_FishAtFishingPier");

    public static JobDef FishAtFishingZoneJobDef =>
        ModLister.OdysseyInstalled ? null : DefDatabase<JobDef>.GetNamed("JobDef_FishAtFishingZone");

    public static JobDef AquacultureBasinHarvestJobDef =>
        DefDatabase<JobDef>.GetNamed("JobDef_AquacultureBasinHarvest");

    public static JobDef AquacultureBasinChangeSpeciesJobDef =>
        DefDatabase<JobDef>.GetNamed("JobDef_AquacultureBasinChangeSpecies");

    public static JobDef AquacultureBasinMaintainJobDef =>
        DefDatabase<JobDef>.GetNamed("JobDef_AquacultureBasinMaintain");

    public static ThingDef MashgonDef => ModLister.OdysseyInstalled ? null : ThingDef.Named("Mashgon");

    public static ThingDef BluebladeDef => ModLister.OdysseyInstalled ? null : ThingDef.Named("Blueblade");

    public static ThingDef TailteethDef => ModLister.OdysseyInstalled ? null : ThingDef.Named("Tailteeth");

    public static PawnKindDef MashgonPawnKindDef =>
        ModLister.OdysseyInstalled ? null : PawnKindDef.Named("PawnKindDefFishMashgon");

    public static PawnKindDef BluebladePawnKindDef =>
        ModLister.OdysseyInstalled ? null : PawnKindDef.Named("PawnKindDefFishBlueblade");

    public static PawnKindDef TailteethPawnKindDef =>
        ModLister.OdysseyInstalled ? null : PawnKindDef.Named("PawnKindDefFishTailteeth");

    public static string MashgonTexturePath =>
        ModLister.OdysseyInstalled
            ? null
            : PawnKindDef.Named("PawnKindDefFishMashgon").lifeStages[0].bodyGraphicData.texPath;

    public static string MashgonTexturePathWithChangeIcon => "Ui/Gizmos/MashgonWithChangeIcon";

    public static string BluebladeTexturePath =>
        ModLister.OdysseyInstalled
            ? null
            : PawnKindDef.Named("PawnKindDefFishBlueblade").lifeStages[0].bodyGraphicData.texPath;

    public static string BluebladeTexturePathWithChangeIcon => "Ui/Gizmos/BluebladeWithChangeIcon";

    public static string TailteethTexturePath =>
        ModLister.OdysseyInstalled
            ? null
            : PawnKindDef.Named("PawnKindDefFishTailteeth").lifeStages[0].bodyGraphicData.texPath;

    public static string TailteethTexturePathWithChangeIcon => "Ui/Gizmos/TailteethWithChangeIcon";

    public static List<PawnKindDef_FishSpecies> GetFishSpeciesList(BiomeDef biome)
    {
        if (!fishSpeciesListDico.ContainsKey(biome))
        {
            fishSpeciesListDico.Add(biome, BuildFishSpeciesListForMap(biome));
        }

        return fishSpeciesListDico.TryGetValue(biome);
    }

    private static List<PawnKindDef_FishSpecies> BuildFishSpeciesListForMap(BiomeDef biome)
    {
        var list = new List<PawnKindDef_FishSpecies>();
        foreach (var item in DefDatabase<PawnKindDef>.AllDefsListForReading)
        {
            if (item is not PawnKindDef_FishSpecies pawnKindDefFishSpecies)
            {
                continue;
            }

            if (!Settings.biomeRestrictionsIsEnabled || pawnKindDefFishSpecies.naturalBiomes.Contains(biome))
            {
                list.Add(pawnKindDefFishSpecies);
            }
        }

        return list;
    }
}