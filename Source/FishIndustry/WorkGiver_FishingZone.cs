using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace FishIndustry;

public class WorkGiver_FishingZone : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.OnCell;

    public override IEnumerable<IntVec3> PotentialWorkCellsGlobal(Pawn pawn)
    {
        var list = new List<IntVec3>();
        foreach (var allZone in pawn.Map.zoneManager.AllZones)
        {
            if (allZone is not Zone_Fishing { allowFishing: not false } zone_Fishing)
            {
                continue;
            }

            foreach (var fishingSpot in zone_Fishing.fishingSpots)
            {
                if (!fishingSpot.IsForbidden(pawn) && pawn.CanReserveAndReach(fishingSpot, PathEndMode, Danger.Some))
                {
                    list.Add(fishingSpot);
                }
            }
        }

        return list;
    }

    public override Job JobOnCell(Pawn pawn, IntVec3 cell, bool forced = false)
    {
        return JobMaker.MakeJob(Util_FishIndustry.FishAtFishingZoneJobDef, cell);
    }
}