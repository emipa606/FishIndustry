using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace FishIndustry;

public class JobDriver_FishAtFishingZone : JobDriver
{
    private readonly TargetIndex fishPositionIndex = TargetIndex.A;
    private readonly PathEndMode pathEndMode = PathEndMode.OnCell;

    private IntVec3 cardinalDir = IntVec3.Invalid;

    private Mote fishingRodMote;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(TargetLocA, job);
    }

    private bool FishingForbiddenOrInvalidSpot()
    {
        return Map.zoneManager.ZoneAt(TargetB.Cell) is not Zone_Fishing { allowFishing: not false } zone_Fishing ||
               !zone_Fishing.fishingSpots.Contains(TargetLocA);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref cardinalDir, "cardinalDir");
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        var fishingSkillLevel = pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Hunting);
        var fishingSkillDurationFactor = fishingSkillLevel / 20f;
        var fishingDuration = (int)(2400f * (1.5f - fishingSkillDurationFactor));
        yield return new Toil
        {
            initAction = delegate
            {
                foreach (var item in GenAdj.CardinalDirections.InRandomOrder())
                {
                    var intVec = TargetLocA + item;
                    if (intVec.GetZone(Map) is not Zone_Fishing zone_Fishing ||
                        !zone_Fishing.fishingSpots.Contains(TargetLocA))
                    {
                        continue;
                    }

                    cardinalDir = item;
                    pawn.CurJob.SetTarget(TargetIndex.B, intVec);
                    rotateToFace = TargetIndex.B;
                    break;
                }
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
        yield return Toils_Goto.GotoCell(TargetLocA, pathEndMode).FailOn(FishingForbiddenOrInvalidSpot);
        var fishToil = new Toil
        {
            tickAction = delegate
            {
                pawn.skills.Learn(SkillDefOf.Shooting, 0.15f);
                if (fishingRodMote.DestroyedOrNull())
                {
                    var cell = TargetB.Cell;
                    var thingDef = cardinalDir == IntVec3.North ? Util_FishIndustry.MoteFishingRodNorthDef :
                        cardinalDir == IntVec3.East ? Util_FishIndustry.MoteFishingRodEastDef :
                        !(cardinalDir == IntVec3.South) ? Util_FishIndustry.MoteFishingRodWestDef :
                        Util_FishIndustry.MoteFishingRodSouthDef;
                    fishingRodMote = (Mote)ThingMaker.MakeThing(thingDef);
                    fishingRodMote.exactPosition = cell.ToVector3Shifted();
                    fishingRodMote.Scale = 1f;
                    GenSpawn.Spawn(fishingRodMote, cell, Map);
                }
                else
                {
                    fishingRodMote.Maintain();
                }
            },
            defaultDuration = fishingDuration,
            defaultCompleteMode = ToilCompleteMode.Delay
        };
        yield return fishToil.WithProgressBarToilDelay(fishPositionIndex, true).FailOn(FishingForbiddenOrInvalidSpot);
        yield return new Toil
        {
            initAction = delegate
            {
                if (!(Rand.Value <= 0.7f))
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, Map, FleckDefOf.IncapIcon);
                    pawn.jobs.EndCurrentJob(JobCondition.Ongoing | JobCondition.Succeeded);
                }
                else
                {
                    var value = Rand.Value;
                    if (value < 0.02 && Util_FishIndustry.GetFishSpeciesList(Map.Biome)
                            .Contains(Util_FishIndustry.TailteethPawnKindDef as PawnKindDef_FishSpecies))
                    {
                        pawn.TakeDamage(new DamageInfo(DamageDefOf.Bite, Rand.Range(5, 12)));
                        Messages.Message(pawn.Name.ToStringShort + "FishIndustry.FisherBitten".Translate(), pawn,
                            MessageTypeDefOf.NegativeHealthEvent);
                        pawn.jobs.EndCurrentJob(JobCondition.Ongoing | JobCondition.Succeeded);
                    }
                    else
                    {
                        Thing thing;
                        if (value < 0.04)
                        {
                            thing = GenSpawn.Spawn(Util_FishIndustry.OysterDef, pawn.Position, Map);
                            thing.stackCount = Rand.RangeInclusive(5, 27);
                        }
                        else
                        {
                            PawnKindDef pawnKindDef =
                                Map.terrainGrid.TerrainAt(TargetLocA) == TerrainDefOf.WaterOceanShallow ||
                                Map.terrainGrid.TerrainAt(TargetLocA) == TerrainDefOf.WaterOceanDeep
                                    ? (from fishSpecies in Util_FishIndustry.GetFishSpeciesList(Map.Biome)
                                        where fishSpecies.livesInOcean
                                        select fishSpecies).RandomElementByWeight(def => def.commonality)
                                    : Map.terrainGrid.TerrainAt(TargetLocA) != TerrainDef.Named("Marsh")
                                        ? (from fishSpecies in Util_FishIndustry.GetFishSpeciesList(Map.Biome)
                                            where fishSpecies.livesInRiver
                                            select fishSpecies).RandomElementByWeight(def => def.commonality)
                                        : (from fishSpecies in Util_FishIndustry.GetFishSpeciesList(Map.Biome)
                                            where fishSpecies.livesInMarsh
                                            select fishSpecies).RandomElementByWeight(def => def.commonality);
                            var generatePawn = PawnGenerator.GeneratePawn(pawnKindDef);
                            ExecutionUtility.DoExecutionByCut(pawn, generatePawn);
                            var corpse = generatePawn.ParentHolder as Corpse;
                            GenSpawn.Spawn(corpse, pawn.Position, Map);
                            thing = corpse;
                            thing.SetForbidden(false);
                            if (generatePawn.BodySize >= 0.1f &&
                                TargetB.Cell.GetZone(Map) is Zone_Fishing zone_Fishing2 &&
                                zone_Fishing2.fishingSpots.Contains(TargetLocA))
                            {
                                zone_Fishing2.fishingSpots.Remove(TargetLocA);
                            }
                        }

                        if (StoreUtility.TryFindBestBetterStoreCellFor(thing, pawn, Map, StoragePriority.Unstored,
                                pawn.Faction, out var foundCell))
                        {
                            pawn.Reserve((LocalTargetInfo)thing, job);
                            pawn.Reserve(foundCell, job);
                            pawn.CurJob.SetTarget(TargetIndex.B, foundCell);
                            pawn.CurJob.SetTarget(TargetIndex.A, thing);
                            pawn.CurJob.count = 9999;
                            pawn.CurJob.haulMode = HaulMode.ToCellStorage;
                        }
                        else
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
                        }
                    }
                }
            }
        };
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);
        var carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
        yield return carryToCell;
        yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, carryToCell, true);
    }
}