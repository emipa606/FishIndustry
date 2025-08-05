using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace FishIndustry;

public class JobDriver_AquacultureBasinHarvest : JobDriver
{
    private readonly TargetIndex aquacultureBasinIndex = TargetIndex.A;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(TargetA, job);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        if (TargetThingA is not Building_AquacultureBasin aquacultureBasin)
        {
            yield break;
        }

        yield return Toils_Goto.GotoThing(aquacultureBasinIndex, PathEndMode.InteractionCell);
        yield return Toils_General.Wait(120).WithProgressBarToilDelay(aquacultureBasinIndex);
        yield return new Toil
        {
            initAction = delegate
            {
                var production = aquacultureBasin.GetProduction();
                if (production == null)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
                else
                {
                    while (production.stackCount > production.def.stackLimit)
                    {
                        var thing = ThingMaker.MakeThing(production.def);
                        thing.stackCount = production.def.stackLimit;
                        GenPlace.TryPlaceThing(thing, GetActor().Position, Map, ThingPlaceMode.Near);
                        production.stackCount -= production.def.stackLimit;
                    }

                    GenSpawn.Spawn(production, aquacultureBasin.InteractionCell, Map);
                    if (StoreUtility.TryFindBestBetterStoreCellFor(production, pawn, Map, StoragePriority.Unstored,
                            pawn.Faction, out var foundCell))
                    {
                        pawn.Reserve((LocalTargetInfo)production, job);
                        pawn.Reserve(foundCell, job);
                        pawn.CurJob.SetTarget(TargetIndex.B, foundCell);
                        pawn.CurJob.SetTarget(TargetIndex.A, production);
                        pawn.CurJob.count = 99999;
                        pawn.CurJob.haulMode = HaulMode.ToCellStorage;
                    }
                    else
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
                    }
                }
            }
        };
        yield return Toils_Reserve.Release(aquacultureBasinIndex);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);
        var carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
        yield return carryToCell;
        yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, carryToCell, true);
    }
}