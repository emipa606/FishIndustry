using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace FishIndustry;

public class WorkGiver_FishingPier : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(Util_FishIndustry.FishingPierDef);

    public override PathEndMode PathEndMode => PathEndMode.OnCell;

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_FishingPier buildingFishingPier)
        {
            return false;
        }

        if (buildingFishingPier.IsBurning() || !buildingFishingPier.allowFishing)
        {
            return false;
        }

        if (!Util_Zone_Fishing.IsAquaticTerrain(buildingFishingPier.Map, buildingFishingPier.fishingSpotCell))
        {
            return false;
        }

        if (!pawn.CanReserveAndReach(buildingFishingPier, PathEndMode, Danger.Some))
        {
            return false;
        }

        return buildingFishingPier.fishStock > 0;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        var building_FishingPier = t as Building_FishingPier;
        if (building_FishingPier != null && (!building_FishingPier.allowUsingGrain || HasFoodToAttractFishes(pawn)))
        {
            return JobMaker.MakeJob(Util_FishIndustry.FishAtFishingPierJobDef, building_FishingPier,
                building_FishingPier.fishingSpotCell);
        }

        Predicate<Thing> validator = grainStack =>
            !grainStack.IsForbidden(pawn.Faction) && grainStack.stackCount >= 20;
        var traverseParams = TraverseParms.For(pawn, Danger.Some);
        var thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
            ThingRequest.ForDef(Util_FishIndustry.RawCornDef), PathEndMode.ClosestTouch, traverseParams, 9999f,
            validator);
        Job job;
        if (thing != null)
        {
            job = JobMaker.MakeJob(JobDefOf.TakeInventory, thing);
            job.count = 20;
            return job;
        }

        var thing2 = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
            ThingRequest.ForDef(Util_FishIndustry.RawRiceDef), PathEndMode.ClosestTouch, traverseParams, 9999f,
            validator);
        if (thing2 == null && building_FishingPier != null)
        {
            return JobMaker.MakeJob(Util_FishIndustry.FishAtFishingPierJobDef, building_FishingPier,
                building_FishingPier.fishingSpotCell);
        }

        job = JobMaker.MakeJob(JobDefOf.TakeInventory, thing2);
        job.count = 20;
        return job;
    }

    public bool HasFoodToAttractFishes(Pawn fisher)
    {
        foreach (var item in fisher.inventory.innerContainer)
        {
            if ((item.def == Util_FishIndustry.RawCornDef || item.def == Util_FishIndustry.RawRiceDef) &&
                item.stackCount >= 5)
            {
                return true;
            }
        }

        return false;
    }
}