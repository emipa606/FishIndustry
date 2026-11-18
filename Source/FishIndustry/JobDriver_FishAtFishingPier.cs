using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FishIndustry;

public class JobDriver_FishAtFishingPier : JobDriver
{
    public const int grainCountToAttractFishes = 5;

    private readonly TargetIndex fishingPierIndex = TargetIndex.A;

    private Mote fishingRodMote;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(TargetA, job);
    }

    private bool FishingForbiddenOrPierDestroyedOrNoFish()
    {
        var building_FishingPier = TargetThingA as Building_FishingPier;
        return building_FishingPier != null && (building_FishingPier.DestroyedOrNull() ||
                                                building_FishingPier.IsBurning() ||
                                                building_FishingPier.IsForbidden(building_FishingPier.Faction) ||
                                                !building_FishingPier.allowFishing ||
                                                building_FishingPier.fishStock == 0);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        var fishingPier = TargetThingA as Building_FishingPier;
        var thrownGrainCount = 0;
        var nextGrainThrowTick = 0;
        this.FailOnBurningImmobile(fishingPierIndex);
        rotateToFace = TargetIndex.B;
        if (fishingPier != null)
        {
            pawn.CurJob.SetTarget(TargetIndex.C, fishingPier.riverCell);
            var riverCellIndex = TargetIndex.C;
            var fishingSkillLevel = pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Hunting);
            var fishingSkillDurationFactor = fishingSkillLevel / 20f;
            var fishingDuration = (int)(1600f * (1.5f - fishingSkillDurationFactor));
            yield return Toils_Goto.GotoThing(fishingPierIndex, fishingPier.riverCell)
                .FailOn(FishingForbiddenOrPierDestroyedOrNoFish);
            var attractfishesToil = new Toil
            {
                tickAction = delegate
                {
                    if (!fishingPier.allowUsingGrain)
                    {
                        ReadyForNextToil();
                    }
                    else if (Find.TickManager.TicksGame >= nextGrainThrowTick)
                    {
                        nextGrainThrowTick = Find.TickManager.TicksGame + 180;
                        Thing thing = null;
                        foreach (var item in pawn.inventory.innerContainer)
                        {
                            if (item.def != Util_FishIndustry.RawCornDef && item.def != Util_FishIndustry.RawRiceDef)
                            {
                                continue;
                            }

                            thing = item;
                            break;
                        }

                        if (thing == null || thrownGrainCount >= 5)
                        {
                            fishingDuration -= Mathf.CeilToInt(fishingDuration * 0.75f * (thrownGrainCount / 5f));
                            ReadyForNextToil();
                        }
                        else
                        {
                            thrownGrainCount++;
                            pawn.inventory.innerContainer.Take(thing, 1);
                            for (var i = 0; i < 5; i++)
                            {
                                if (!pawn.Position.ShouldSpawnMotesAt(pawn.Map))
                                {
                                    break;
                                }

                                if (pawn.Map.moteCounter.Saturated)
                                {
                                    break;
                                }

                                var num = Rand.Range(1.5f, 3f);
                                var vector = pawn.Position.ToVector3Shifted() +
                                             new Vector3(0f, 0f, 2f).RotatedBy(fishingPier.Rotation.AsAngle) +
                                             Vector3Utility.RandomHorizontalOffset(1.5f);
                                vector.y = pawn.DrawPos.y;
                                var fleckData = default(FleckCreationData);
                                fleckData.def = FleckDefOf.Stone;
                                fleckData.spawnPosition = pawn.DrawPos;
                                fleckData.scale = 0.2f;
                                fleckData.rotationRate = Rand.Range(-300, 300);
                                fleckData.velocityAngle = (vector - fleckData.spawnPosition).AngleFlat();
                                fleckData.velocitySpeed = num;
                                fleckData.airTimeLeft =
                                    Mathf.RoundToInt((vector - fleckData.spawnPosition).MagnitudeHorizontal() / num);
                                pawn.Map.flecks.CreateFleck(fleckData);
                                FleckMaker.WaterSplash(vector, pawn.Map, 1.8f, 0.5f);
                            }
                        }
                    }
                },
                defaultDuration = fishingDuration,
                defaultCompleteMode = ToilCompleteMode.Delay
            };
            yield return attractfishesToil.FailOn(FishingForbiddenOrPierDestroyedOrNoFish);
            var fishToil = new Toil
            {
                initAction = delegate
                {
                    CurToil.defaultDuration = fishingDuration;
                    ticksLeftThisToil = fishingDuration;
                    ThingDef thingDef;
                    if (fishingPier.Rotation == Rot4.North)
                    {
                        thingDef = Util_FishIndustry.MoteFishingRodNorthDef;
                    }
                    else
                    {
                        if (fishingPier.Rotation == Rot4.East)
                        {
                            thingDef = Util_FishIndustry.MoteFishingRodEastDef;
                        }
                        else
                        {
                            thingDef = fishingPier.Rotation != Rot4.South
                                ? Util_FishIndustry.MoteFishingRodWestDef
                                : Util_FishIndustry.MoteFishingRodSouthDef;
                        }
                    }

                    fishingRodMote = (Mote)ThingMaker.MakeThing(thingDef);
                    fishingRodMote.exactPosition = fishingPier.fishingSpotCell.ToVector3Shifted();
                    fishingRodMote.Scale = 1f;
                    GenSpawn.Spawn(fishingRodMote, fishingPier.fishingSpotCell, Map);
                },
                tickAction = delegate
                {
                    pawn.skills.Learn(SkillDefOf.Shooting, 0.15f);
                    fishingRodMote?.Maintain();
                },
                defaultDuration = fishingDuration,
                defaultCompleteMode = ToilCompleteMode.Delay
            };
            yield return fishToil.WithProgressBarToilDelay(riverCellIndex)
                .FailOn(FishingForbiddenOrPierDestroyedOrNoFish);
            yield return new Toil
            {
                initAction = delegate
                {
                    if (!(Rand.Value <= 0.9f))
                    {
                        FleckMaker.ThrowMetaIcon(pawn.Position, Map, FleckDefOf.IncapIcon);
                        pawn.jobs.EndCurrentJob(JobCondition.Ongoing | JobCondition.Succeeded);
                    }
                    else
                    {
                        var value = Rand.Value;
                        Thing thing2;
                        if (value < 0.02)
                        {
                            var value2 = Rand.Value;
                            switch (value2)
                            {
                                case < 0.01f:
                                {
                                    thing2 = GenSpawn.Spawn(ThingDefOf.Gold, pawn.Position, Map);
                                    thing2.stackCount = Rand.RangeInclusive(58, 289);
                                    var thing3 = GenSpawn.Spawn(ThingDefOf.Silver, fishingPier.middleCell, Map);
                                    thing3.stackCount = Rand.RangeInclusive(237, 2154);
                                    Find.LetterStack.ReceiveLetter("FishIndustry.LetterLabelSunkenTreasure".Translate(),
                                        "FishIndustry.SunkenTreasure".Translate(
                                            pawn.Name.ToStringShort.CapitalizeFirst()),
                                        LetterDefOf.PositiveEvent, pawn);
                                    break;
                                }
                                case < 0.02f:
                                {
                                    var thing4 = GenSpawn.Spawn(ThingDef.Named("Apparel_PowerArmor"), pawn.Position,
                                        Map);
                                    thing2 = thing4;
                                    var thing5 = GenSpawn.Spawn(ThingDef.Named("Apparel_PowerArmorHelmet"),
                                        pawn.Position,
                                        Map);
                                    var thing6 = !(Rand.Value < 0.5f)
                                        ? GenSpawn.Spawn(ThingDef.Named("Gun_SniperRifle"), pawn.Position, Map)
                                        : GenSpawn.Spawn(ThingDef.Named("Gun_ChargeRifle"), pawn.Position, Map);
                                    thing4.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Masterwork,
                                        ArtGenerationContext.Outsider);
                                    thing5.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Masterwork,
                                        ArtGenerationContext.Outsider);
                                    thing6.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Masterwork,
                                        ArtGenerationContext.Outsider);
                                    var faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.AncientsHostile);
                                    var generatePawn =
                                        PawnGenerator.GeneratePawn(PawnKindDefOf.AncientSoldier, faction);
                                    HealthUtility.DamageUntilDead(generatePawn);
                                    var corpse = generatePawn.ParentHolder as Corpse;
                                    var compRottable = corpse.TryGetComp<CompRottable>();
                                    compRottable?.RotProgress = 1200000f;

                                    GenSpawn.Spawn(corpse, fishingPier.bankCell, Map);
                                    string text =
                                        "FishIndustry.LetterDescDeadMarine".Translate(pawn.Name.ToStringShort
                                            .CapitalizeFirst());
                                    Find.LetterStack.ReceiveLetter("FishIndustry.LetterLabelDeadMarine".Translate(),
                                        (TaggedString)text, LetterDefOf.PositiveEvent, pawn);
                                    break;
                                }
                                default:
                                    thing2 = GenSpawn.Spawn(ThingDefOf.Gold, pawn.Position, Map);
                                    thing2.stackCount = Rand.RangeInclusive(1, 7);
                                    break;
                            }
                        }
                        else if (value < 0.04)
                        {
                            thing2 = GenSpawn.Spawn(Util_FishIndustry.OysterDef, pawn.Position, Map);
                            thing2.stackCount = Rand.RangeInclusive(2, 9);
                        }
                        else
                        {
                            var c = fishingPier.aquaticCells.RandomElement();
                            PawnKindDef pawnKindDef;
                            if (Map.terrainGrid.TerrainAt(c) == TerrainDefOf.WaterOceanShallow ||
                                Map.terrainGrid.TerrainAt(c) == TerrainDefOf.WaterOceanDeep)
                            {
                                pawnKindDef = (from fishSpecies in Util_FishIndustry.GetFishSpeciesList(Map.Biome)
                                    where fishSpecies.livesInOcean
                                    select fishSpecies).RandomElementByWeight(def => def.commonality);
                            }
                            else
                            {
                                if (Map.terrainGrid.TerrainAt(c) == TerrainDef.Named("Marsh"))
                                {
                                    pawnKindDef = (from fishSpecies in Util_FishIndustry.GetFishSpeciesList(Map.Biome)
                                        where fishSpecies.livesInMarsh
                                        select fishSpecies).RandomElementByWeight(def => def.commonality);
                                }
                                else
                                {
                                    pawnKindDef = (from fishSpecies in Util_FishIndustry.GetFishSpeciesList(Map.Biome)
                                        where fishSpecies.livesInRiver
                                        select fishSpecies).RandomElementByWeight(def => def.commonality);
                                }
                            }

                            var pawn2 = PawnGenerator.GeneratePawn(pawnKindDef);
                            ExecutionUtility.DoExecutionByCut(pawn, pawn2);
                            var corpse2 = pawn2.ParentHolder as Corpse;
                            GenSpawn.Spawn(corpse2, pawn.Position, Map);
                            thing2 = corpse2;
                            thing2.SetForbidden(false);
                            if (pawn2.BodySize >= 0.1f)
                            {
                                fishingPier.fishStock--;
                            }
                        }

                        if (StoreUtility.TryFindBestBetterStoreCellFor(thing2, pawn, Map, StoragePriority.Unstored,
                                pawn.Faction, out var foundCell))
                        {
                            pawn.Reserve((LocalTargetInfo)thing2, job);
                            pawn.Reserve(foundCell, job);
                            pawn.CurJob.SetTarget(TargetIndex.B, foundCell);
                            pawn.CurJob.SetTarget(TargetIndex.A, thing2);
                            pawn.CurJob.count = 9999;
                            pawn.CurJob.haulMode = HaulMode.ToCellStorage;
                        }
                        else
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
                        }
                    }
                }
            };
        }

        yield return Toils_Haul.StartCarryThing(TargetIndex.A);
        var carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
        yield return carryToCell;
        yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, carryToCell, true);
    }
}