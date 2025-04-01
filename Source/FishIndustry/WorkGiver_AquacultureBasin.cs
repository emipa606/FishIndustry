using RimWorld;
using Verse;
using Verse.AI;

namespace FishIndustry;

public class WorkGiver_AquacultureBasin : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest =>
        ThingRequest.ForDef(Util_FishIndustry.AquacultureBasinDef);

    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_AquacultureBasin buildingAquacultureBasin)
        {
            return false;
        }

        if (pawn.Dead || pawn.Downed || pawn.IsBurning())
        {
            return false;
        }

        if (!pawn.CanReserveAndReach(buildingAquacultureBasin, PathEndMode, Danger.Some))
        {
            return false;
        }

        return buildingAquacultureBasin.fishIsHarvestable || buildingAquacultureBasin.speciesShouldBeChanged ||
               buildingAquacultureBasin.shouldBeMaintained;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Job result = null;
        var building_AquacultureBasin = t as Building_AquacultureBasin;
        switch (building_AquacultureBasin)
        {
            case { fishIsHarvestable: true }:
                result = JobMaker.MakeJob(Util_FishIndustry.AquacultureBasinHarvestJobDef, building_AquacultureBasin);
                break;
            case { speciesShouldBeChanged: true }:
                result = JobMaker.MakeJob(Util_FishIndustry.AquacultureBasinChangeSpeciesJobDef,
                    building_AquacultureBasin);
                break;
            case { shouldBeMaintained: true }:
                result = JobMaker.MakeJob(Util_FishIndustry.AquacultureBasinMaintainJobDef, building_AquacultureBasin);
                break;
        }

        return result;
    }
}