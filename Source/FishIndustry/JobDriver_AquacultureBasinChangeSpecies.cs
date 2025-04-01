using System.Collections.Generic;
using Verse.AI;

namespace FishIndustry;

public class JobDriver_AquacultureBasinChangeSpecies : JobDriver
{
    public readonly TargetIndex aquacultureBasinIndex = TargetIndex.A;

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

        yield return Toils_Goto.GotoThing(aquacultureBasinIndex, PathEndMode.InteractionCell).FailOn(() =>
            !aquacultureBasin.powerComp.PowerOn || aquacultureBasin.desiredSpeciesDef == null);
        yield return Toils_General.Wait(600).WithProgressBarToilDelay(aquacultureBasinIndex).FailOn(() =>
            !aquacultureBasin.powerComp.PowerOn || aquacultureBasin.desiredSpeciesDef == null);
        yield return new Toil
        {
            initAction = delegate { aquacultureBasin.StartNewBreedingCycle(); },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
    }
}