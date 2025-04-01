using RimWorld;
using UnityEngine;
using Verse;

namespace FishIndustry;

internal class Designator_ZoneAdd_Fishing : Designator_ZoneAdd
{
    public Designator_ZoneAdd_Fishing()
    {
        zoneTypeToPlace = typeof(Zone_Fishing);
        defaultLabel = "FishIndustry.FishingZone".Translate();
        defaultDesc = "FishIndustry.DesignatorFishingZoneDesc".Translate();
        icon = ContentFinder<Texture2D>.Get("Ui/Designators/ZoneCreate_Fishing");
    }

    protected override string NewZoneLabel => "FishIndustry.FishingZone".Translate();

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        if (!base.CanDesignateCell(c).Accepted)
        {
            return false;
        }

        if (Util_PlaceWorker.IsNearFishingPier(Map, c, 15f))
        {
            return false;
        }

        return Util_Zone_Fishing.IsAquaticTerrain(Map, c) && c.Walkable(Map);
    }

    protected override Zone MakeNewZone()
    {
        return new Zone_Fishing(Find.CurrentMap.zoneManager);
    }
}