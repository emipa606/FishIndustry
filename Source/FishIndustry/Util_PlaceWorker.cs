using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FishIndustry;

public static class Util_PlaceWorker
{
    public const int minDistanceBetweenTwoFishingSpots = 15;

    public static bool IsNearFishingPier(Map map, IntVec3 position, float distance)
    {
        var list = new List<Thing>();
        list.AddRange(map.listerThings.ThingsOfDef(Util_FishIndustry.FishingPierDef));
        list.AddRange(map.listerThings.ThingsOfDef(Util_FishIndustry.FishingPierDef.blueprintDef));
        list.AddRange(map.listerThings.ThingsOfDef(Util_FishIndustry.FishingPierDef.frameDef));
        return list.Any(item => item.Position.InHorDistOf(position, distance));
    }

    public static bool IsNearFishingZone(Map map, IntVec3 position, float distance)
    {
        foreach (var item in GenRadial.RadialCellsAround(position, distance, true))
        {
            if (!item.InBounds(map))
            {
                continue;
            }

            var zone = map.zoneManager.ZoneAt(item);
            if (zone is Zone_Fishing)
            {
                return true;
            }
        }

        return false;
    }

    public static int GetAquaticCellsInRadius(Map map, IntVec3 position, float radius)
    {
        if (radius <= 0f)
        {
            return 0;
        }

        return GenRadial.RadialCellsAround(position, radius, true)
            .Count(item => item.InBounds(map) && Util_Zone_Fishing.IsAquaticTerrain(map, item));
    }
}