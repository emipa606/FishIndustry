using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FishIndustry;

public static class Util_FishIndustry
{
    public static readonly Dictionary<BiomeDef, List<PawnKindDef_FishSpecies>> fishSpeciesListDico =
        new Dictionary<BiomeDef, List<PawnKindDef_FishSpecies>>();

    public static ThingDef FishingPierDef => ThingDef.Named("FishingPier");

    public static ThingDef AquacultureBasinDef => ThingDef.Named("AquacultureBasin");

    public static TerrainDef FishingPierFloorShallowWaterDef => TerrainDef.Named("FishingPierFloorShallowWater");

    public static TerrainDef FishingPierFloorMarshDef => TerrainDef.Named("FishingPierFloorMarsh");

    public static ThingDef MoteFishingRodNorthDef => ThingDef.Named("Mote_FishingRodNorth");

    public static ThingDef MoteFishingRodEastDef => ThingDef.Named("Mote_FishingRodEast");

    public static ThingDef MoteFishingRodSouthDef => ThingDef.Named("Mote_FishingRodSouth");

    public static ThingDef MoteFishingRodWestDef => ThingDef.Named("Mote_FishingRodWest");

    public static ThingDef MoteFishMashgonEastDef => ThingDef.Named("Mote_FishMashgonEast");

    public static ThingDef MoteFishMashgonWestDef => ThingDef.Named("Mote_FishMashgonWest");

    public static ThingDef MoteFishBluebladeEastDef => ThingDef.Named("Mote_FishBluebladeEast");

    public static ThingDef MoteFishBluebladeWestDef => ThingDef.Named("Mote_FishBluebladeWest");

    public static ThingDef MoteFishTailteethEastDef => ThingDef.Named("Mote_FishTailteethEast");

    public static ThingDef MoteFishTailteethWestDef => ThingDef.Named("Mote_FishTailteethWest");

    public static ThingDef OysterDef => ThingDef.Named("Oyster");

    public static ThingDef PearlDef => ThingDef.Named("Pearl");

    public static ThingDef RawCornDef => ThingDef.Named("RawCorn");

    public static ThingDef RawRiceDef => ThingDef.Named("RawRice");

    public static JobDef FishAtFishingPierJobDef => DefDatabase<JobDef>.GetNamed("JobDef_FishAtFishingPier");

    public static JobDef FishAtFishingZoneJobDef => DefDatabase<JobDef>.GetNamed("JobDef_FishAtFishingZone");

    public static JobDef AquacultureBasinHarvestJobDef =>
        DefDatabase<JobDef>.GetNamed("JobDef_AquacultureBasinHarvest");

    public static JobDef AquacultureBasinChangeSpeciesJobDef =>
        DefDatabase<JobDef>.GetNamed("JobDef_AquacultureBasinChangeSpecies");

    public static JobDef AquacultureBasinMaintainJobDef =>
        DefDatabase<JobDef>.GetNamed("JobDef_AquacultureBasinMaintain");

    public static ThingDef MashgonDef => ThingDef.Named("Mashgon");

    public static ThingDef BluebladeDef => ThingDef.Named("Blueblade");

    public static ThingDef TailteethDef => ThingDef.Named("Tailteeth");

    public static PawnKindDef MashgonPawnKindDef => PawnKindDef.Named("PawnKindDefFishMashgon");

    public static PawnKindDef BluebladePawnKindDef => PawnKindDef.Named("PawnKindDefFishBlueblade");

    public static PawnKindDef TailteethPawnKindDef => PawnKindDef.Named("PawnKindDefFishTailteeth");

    public static string MashgonTexturePath =>
        PawnKindDef.Named("PawnKindDefFishMashgon").lifeStages.First().bodyGraphicData.texPath;

    public static string MashgonTexturePathWithChangeIcon => "Ui/Gizmos/MashgonWithChangeIcon";

    public static string BluebladeTexturePath =>
        PawnKindDef.Named("PawnKindDefFishBlueblade").lifeStages.First().bodyGraphicData.texPath;

    public static string BluebladeTexturePathWithChangeIcon => "Ui/Gizmos/BluebladeWithChangeIcon";

    public static string TailteethTexturePath =>
        PawnKindDef.Named("PawnKindDefFishTailteeth").lifeStages.First().bodyGraphicData.texPath;

    public static string TailteethTexturePathWithChangeIcon => "Ui/Gizmos/TailteethWithChangeIcon";

    public static List<PawnKindDef_FishSpecies> GetFishSpeciesList(BiomeDef biome)
    {
        if (!fishSpeciesListDico.ContainsKey(biome))
        {
            fishSpeciesListDico.Add(biome, BuildFishSpeciesListForMap(biome));
        }

        return fishSpeciesListDico.TryGetValue(biome);
    }

    public static List<PawnKindDef_FishSpecies> BuildFishSpeciesListForMap(BiomeDef biome)
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