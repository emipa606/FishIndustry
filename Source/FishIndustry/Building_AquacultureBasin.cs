using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace FishIndustry;

[StaticConstructorOnStartup]
internal class Building_AquacultureBasin : Building_WorkTable
{
    private const int updatePeriodInTicks = 250;

    private const int foodDispensePeriodInTicks = 60000;

    private const float minWaterQuality = 0.1f;

    public const float waterCompleteDegradationPeriodInTicks = 30000f;

    public const float waterQualityVariation = 1f / 120f;

    public const int maintenanceCompleteDegradationPeriodInTicks = 60000;

    private const float maintenanceVariation = 0.004166667f;

    private const int breedingProgressPerMaintenanceInTicks = 5000;

    private const int microFungusReductionPerMaintenanceInTicks = 20000;

    private const int fishMotePeriodInTicks = 600;

    private static readonly Material mashgonTexture = MaterialPool.MatFrom(Util_FishIndustry.MashgonTexturePath);

    private static readonly Material bluebladeTexture = MaterialPool.MatFrom(Util_FishIndustry.BluebladeTexturePath);

    private static readonly Material tailTeethTexture = MaterialPool.MatFrom(Util_FishIndustry.TailteethTexturePath);

    private static readonly Material microFungusTexture =
        MaterialPool.MatFrom("Effects/MicroFungus", ShaderDatabase.Transparent);

    private readonly Vector3 breedingSpeciesScale = new(0.5f, 1f, 0.5f);

    private readonly Vector3 microFungusScale = new(3f, 1f, 3f);

    private int breedingProgressInTicks;

    private Matrix4x4 breedingSpeciesMatrix;

    private Material breedingSpeciesTexture;

    public PawnKindDef desiredSpeciesDef = Util_FishIndustry.BluebladePawnKindDef;

    public bool fishesAreFed;

    private float fishesHealth = 1f;

    private bool foodIsAvailable;

    private float maintenanceQuality = 1f;

    private float microFungusFadingFactor = 1f;

    private Matrix4x4 microFungusMatrix;

    public int microFungusRemainingDurationInTicks;

    private int nextFeedingTick;

    private int nextFishMoteTick;

    private int nextUpdateTick;

    public CompPowerTrader powerComp;

    public PawnKindDef speciesDef;

    public float waterQuality = 0.2f;

    private int breedingDurationInTicks
    {
        get
        {
            if (speciesDef != null)
            {
                return (int)(60000f * ((PawnKindDef_FishSpecies)speciesDef).breedingDurationInDays);
            }

            return 0;
        }
    }

    private bool isWellMaintained => maintenanceQuality >= 0.5f;

    private bool isBadlyMaintained => maintenanceQuality == 0f;

    public bool fishIsHarvestable => speciesDef != null && breedingProgressInTicks == breedingDurationInTicks;

    public bool speciesShouldBeChanged => powerComp.PowerOn && desiredSpeciesDef != null &&
                                          microFungusRemainingDurationInTicks == 0 && foodIsAvailable;

    public bool shouldBeMaintained => powerComp.PowerOn &&
                                      (microFungusRemainingDurationInTicks > 0 ||
                                       speciesDef != null && !isWellMaintained);

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        powerComp = GetComp<CompPowerTrader>();
        if (speciesDef != null)
        {
            if (speciesDef == Util_FishIndustry.MashgonPawnKindDef)
            {
                breedingSpeciesTexture = mashgonTexture;
            }
            else if (speciesDef == Util_FishIndustry.BluebladePawnKindDef)
            {
                breedingSpeciesTexture = bluebladeTexture;
            }
            else if (speciesDef == Util_FishIndustry.TailteethPawnKindDef)
            {
                breedingSpeciesTexture = tailTeethTexture;
            }
        }

        breedingSpeciesMatrix.SetTRS(
            Position.ToVector3ShiftedWithAltitude(AltitudeLayer.Silhouettes) +
            new Vector3(-1f, 0f, -0.7f).RotatedBy(Rotation.AsAngle), 0f.ToQuat(), breedingSpeciesScale);
        microFungusMatrix.SetTRS(Position.ToVector3ShiftedWithAltitude(AltitudeLayer.Silhouettes), 0f.ToQuat(),
            microFungusScale);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref nextUpdateTick, "nextUpdateTick");
        Scribe_Defs.Look(ref desiredSpeciesDef, "desiredSpeciesDef");
        Scribe_Defs.Look(ref speciesDef, "speciesDef");
        Scribe_Values.Look(ref breedingProgressInTicks, "breedingProgressInTicks");
        Scribe_Values.Look(ref nextFeedingTick, "nextFeedingTick");
        Scribe_Values.Look(ref foodIsAvailable, "foodIsAvailable");
        Scribe_Values.Look(ref fishesAreFed, "fishesAreFed");
        Scribe_Values.Look(ref waterQuality, "waterQuality");
        Scribe_Values.Look(ref microFungusRemainingDurationInTicks, "microFungusRemainingDurationInTicks");
        Scribe_Values.Look(ref fishesHealth, "fishesHealth");
        Scribe_Values.Look(ref maintenanceQuality, "maintenanceQuality");
        Scribe_Values.Look(ref nextFishMoteTick, "nextFishMoteTick");
    }

    protected override void Tick()
    {
        base.Tick();
        if (microFungusRemainingDurationInTicks > 0)
        {
            microFungusRemainingDurationInTicks--;
        }

        if (Find.TickManager.TicksGame >= nextUpdateTick)
        {
            nextUpdateTick = Find.TickManager.TicksGame + updatePeriodInTicks;
            UpdateFoodAvailability();
            TryFeedFishes();
            UpdateWaterQuality();
            UpdateFishesHealth();
            UpdateMaintenanceQuality();
            if (powerComp.PowerOn && speciesDef != null && fishesAreFed)
            {
                breedingProgressInTicks += (int)(250f * ((0.75f * waterQuality) + (0.25f * maintenanceQuality)));
                breedingProgressInTicks = Math.Min(breedingProgressInTicks, breedingDurationInTicks);
            }
        }

        ComputeDrawingParameters();
    }

    private void UpdateFoodAvailability()
    {
        var num = 0f;
        foreach (var item in GenAdj.CellsAdjacentCardinal(this))
        {
            Thing thing = null;
            Thing thing2 = null;
            var thingList = item.GetThingList(Map);
            foreach (var thing3 in thingList)
            {
                if (thing3.def == ThingDefOf.Hopper)
                {
                    thing2 = thing3;
                }

                if (IsAcceptableFeedstock(thing3.def))
                {
                    thing = thing3;
                }
            }

            if (thing2 == null || thing == null)
            {
                continue;
            }

            num += thing.stackCount * thing.def.ingestible.CachedNutrition;
            if (!(num >= def.building.nutritionCostPerDispense))
            {
                continue;
            }

            foodIsAvailable = true;
            return;
        }

        foodIsAvailable = false;
    }

    private void TryFeedFishes()
    {
        if (speciesDef == null || Find.TickManager.TicksGame < nextFeedingTick)
        {
            return;
        }

        if (powerComp.PowerOn && foodIsAvailable)
        {
            ConsumeFoodFromHoppers();
            fishesAreFed = true;
            nextFeedingTick = Find.TickManager.TicksGame + foodDispensePeriodInTicks;
        }
        else
        {
            fishesAreFed = false;
        }
    }

    private void ConsumeFoodFromHoppers()
    {
        var num = def.building.nutritionCostPerDispense;
        foreach (var item in GenAdj.CellsAdjacentCardinal(this))
        {
            Thing thing = null;
            Thing thing2 = null;
            var thingList = item.GetThingList(Map);
            foreach (var thing3 in thingList)
            {
                if (thing3.def == ThingDefOf.Hopper)
                {
                    thing2 = thing3;
                }

                if (IsAcceptableFeedstock(thing3.def))
                {
                    thing = thing3;
                }
            }

            if (thing2 == null || thing == null)
            {
                continue;
            }

            var num2 = Mathf.Min(thing.stackCount, Mathf.CeilToInt(num / thing.def.ingestible.CachedNutrition));
            thing.SplitOff(num2);
            num -= num2 * thing.def.ingestible.CachedNutrition;
            if (num <= 0f)
            {
                return;
            }
        }
    }

    private static bool IsAcceptableFeedstock(ThingDef def)
    {
        return def.IsNutritionGivingIngestible && def.ingestible.preferability != 0 &&
               (def.ingestible.foodType == FoodTypeFlags.Seed || def.ingestible.foodType == FoodTypeFlags.Meat ||
                def.ingestible.foodType == FoodTypeFlags.VegetableOrFruit ||
                def.ingestible.foodType == FoodTypeFlags.AnimalProduct ||
                def.ingestible.foodType == FoodTypeFlags.Fungus);
    }

    private void UpdateWaterQuality()
    {
        if (microFungusRemainingDurationInTicks > 0)
        {
            waterQuality -= 0.050000004f;
        }
        else if (powerComp.PowerOn)
        {
            waterQuality += 0.025000002f;
        }
        else
        {
            waterQuality -= 1f / 120f;
        }

        waterQuality = Mathf.Clamp01(waterQuality);
    }

    private void UpdateFishesHealth()
    {
        if (speciesDef == null)
        {
            return;
        }

        var num = 0.001f;
        if (!fishesAreFed)
        {
            num -= 0.01f;
        }

        if (waterQuality < minWaterQuality)
        {
            num -= 0.01f;
        }

        fishesHealth += num;
        if (fishesHealth <= 0f)
        {
            fishesHealth = 0f;
            StopBreeding();
            Messages.Message("FishIndustry.FishesDied".Translate(), this, MessageTypeDefOf.NegativeEvent);
        }

        fishesHealth = Mathf.Clamp01(fishesHealth);
    }

    private void UpdateMaintenanceQuality()
    {
        maintenanceQuality -= maintenanceVariation;
        maintenanceQuality = Mathf.Max(0f, maintenanceQuality);
    }

    private void StopBreeding()
    {
        desiredSpeciesDef = speciesDef;
        speciesDef = null;
        breedingProgressInTicks = 0;
    }

    public void StartNewBreedingCycle()
    {
        if (desiredSpeciesDef != null)
        {
            speciesDef = desiredSpeciesDef;
            desiredSpeciesDef = null;
        }

        breedingProgressInTicks = 0;
        maintenanceQuality = 1f;
        if (waterQuality < minWaterQuality)
        {
            waterQuality = minWaterQuality;
        }

        fishesHealth = 1f;
        breedingSpeciesTexture = MaterialPool.MatFrom(speciesDef.lifeStages.First().bodyGraphicData.texPath,
            ShaderDatabase.Transparent);
    }

    public Thing GetProduction()
    {
        var thing = ThingMaker.MakeThing(speciesDef.race.race.meatDef);
        thing.stackCount = Mathf.CeilToInt(((PawnKindDef_FishSpecies)speciesDef).breedQuantity * fishesHealth *
                                           Settings.fishBreedQuantityFactor);
        if (microFungusRemainingDurationInTicks == 0)
        {
            StartNewBreedingCycle();
        }
        else
        {
            StopBreeding();
        }

        return thing;
    }

    public void StartMicroFungusInfestation(int infestationDuration)
    {
        microFungusRemainingDurationInTicks = infestationDuration;
    }

    public void Notify_MaintenanceDone()
    {
        maintenanceQuality = 1f;
        if (microFungusRemainingDurationInTicks > 0)
        {
            microFungusRemainingDurationInTicks = Math.Max(0,
                microFungusRemainingDurationInTicks - microFungusReductionPerMaintenanceInTicks);
        }
        else if (speciesDef != null)
        {
            breedingProgressInTicks = Math.Min(breedingProgressInTicks + breedingProgressPerMaintenanceInTicks,
                breedingDurationInTicks);
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        var num = 700000113;
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        var pawnKindDef = desiredSpeciesDef ?? speciesDef;
        GetFishesNamePluralAndDescription(pawnKindDef, out var namePlural, out var description);
        GetButtonTexturePath(pawnKindDef, desiredSpeciesDef != null, out var buttonTexturePath);
        var command_Action = new Command_Action();
        if (desiredSpeciesDef != null)
        {
            command_Action.defaultLabel = "FishIndustry.ChangeSpecies".Translate();
            command_Action.defaultDesc = "FishIndustry.ChangeSpeciesDescription1".Translate() + namePlural + " (" +
                                         description + ") " + "FishIndustry.ChangeSpeciesDescription2".Translate();
        }
        else
        {
            command_Action.defaultLabel = "FishIndustry.Breeding".Translate();
            command_Action.defaultDesc =
                "FishIndustry.BasinIsBreeding".Translate() + namePlural + " (" + description + ").";
        }

        command_Action.icon = ContentFinder<Texture2D>.Get(buttonTexturePath);
        command_Action.action = SetDesiredSpecies;
        command_Action.activateSound = SoundDef.Named("Click");
        command_Action.groupKey = num + 1;
        yield return command_Action;

        var designator_Build = BuildCopyCommandUtility.FindAllowedDesignator(ThingDefOf.Hopper);
        if (designator_Build != null)
        {
            yield return designator_Build;
        }
    }

    private static void GetFishesNamePluralAndDescription(PawnKindDef speciesDef, out string namePlural,
        out string description)
    {
        namePlural = "";
        description = "";
        if (speciesDef == Util_FishIndustry.MashgonPawnKindDef)
        {
            namePlural = "FishIndustry.FishLabelPluralMashgon".Translate();
            description = "FishIndustry.FishDescriptionMashgon".Translate();
        }
        else if (speciesDef == Util_FishIndustry.BluebladePawnKindDef)
        {
            namePlural = "FishIndustry.FishLabelPluralBlueblade".Translate();
            description = "FishIndustry.FishDescriptionBlueblade".Translate();
        }
        else if (speciesDef == Util_FishIndustry.TailteethPawnKindDef)
        {
            namePlural = "FishIndustry.FishLabelPluralTailteeth".Translate();
            description = "FishIndustry.FishDescriptionTailteeth".Translate();
        }
        else
        {
            Log.Warning($"FishIndustry: unhandled PawnKindDef ({speciesDef}).");
        }
    }

    private static void GetButtonTexturePath(PawnKindDef species, bool changingBredSpecies,
        out string buttonTexturePath)
    {
        buttonTexturePath = "";
        if (changingBredSpecies)
        {
            if (species == Util_FishIndustry.MashgonPawnKindDef)
            {
                buttonTexturePath = Util_FishIndustry.MashgonTexturePathWithChangeIcon;
            }
            else if (species == Util_FishIndustry.BluebladePawnKindDef)
            {
                buttonTexturePath = Util_FishIndustry.BluebladeTexturePathWithChangeIcon;
            }
            else if (species == Util_FishIndustry.TailteethPawnKindDef)
            {
                buttonTexturePath = Util_FishIndustry.TailteethTexturePathWithChangeIcon;
            }
            else
            {
                Log.Warning($"FishIndustry: unhandled PawnKindDef ({species}).");
            }
        }
        else if (species == Util_FishIndustry.MashgonPawnKindDef)
        {
            buttonTexturePath = Util_FishIndustry.MashgonTexturePath;
        }
        else if (species == Util_FishIndustry.BluebladePawnKindDef)
        {
            buttonTexturePath = Util_FishIndustry.BluebladeTexturePath;
        }
        else if (species == Util_FishIndustry.TailteethPawnKindDef)
        {
            buttonTexturePath = Util_FishIndustry.TailteethTexturePath;
        }
        else
        {
            Log.Warning($"FishIndustry: unhandled PawnKindDef ({species}).");
        }
    }

    private void SetDesiredSpecies()
    {
        PawnKindDef pawnKindDef2 = null;
        var pawnKindDef = desiredSpeciesDef ?? speciesDef;
        if (pawnKindDef == Util_FishIndustry.MashgonPawnKindDef)
        {
            pawnKindDef2 = Util_FishIndustry.BluebladePawnKindDef;
        }
        else if (pawnKindDef == Util_FishIndustry.BluebladePawnKindDef)
        {
            pawnKindDef2 = Util_FishIndustry.TailteethPawnKindDef;
        }
        else if (pawnKindDef == Util_FishIndustry.TailteethPawnKindDef)
        {
            pawnKindDef2 = Util_FishIndustry.MashgonPawnKindDef;
        }

        desiredSpeciesDef = pawnKindDef2 == speciesDef ? null : pawnKindDef2;
    }

    public override string GetInspectString()
    {
        var list = new List<string>();
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(base.GetInspectString());
        stringBuilder.AppendLine();
        string text = speciesDef != null
            ? $"{"FishIndustry.ProgressLabel".Translate()}{Mathf.RoundToInt(breedingProgressInTicks / (float)breedingDurationInTicks * 100f)}%"
            : "FishIndustry.ProgressLabelNotBreeding".Translate();
        stringBuilder.AppendLine(text);
        if (speciesDef != null)
        {
            stringBuilder.Append("FishIndustry.WaterQualityFishesHealth".Translate(
                Mathf.RoundToInt(waterQuality * 100f), Mathf.RoundToInt(fishesHealth * 100f)));
        }
        else
        {
            stringBuilder.Append("FishIndustry.WaterQuality".Translate(Mathf.RoundToInt(waterQuality * 100f)));
        }

        if (shouldBeMaintained)
        {
            stringBuilder.AppendLine();
            stringBuilder.Append("FishIndustry.NeedsMaintenanceNow".Translate());
        }

        if (microFungusRemainingDurationInTicks > 0)
        {
            list.Add("FishIndustry.Issue_MicroFungus".Translate());
        }

        if (!foodIsAvailable)
        {
            list.Add("FishIndustry.Issue_NoFood".Translate());
        }

        if (waterQuality < minWaterQuality)
        {
            list.Add("FishIndustry.Issue_CriticalWaterQuality".Translate());
        }

        if (speciesDef != null && isBadlyMaintained)
        {
            list.Add("FishIndustry.Issue_BadlyMaintained".Translate());
        }

        if (list.Count <= 0)
        {
            return stringBuilder.ToString();
        }

        var text2 = list.Count != 1
            ? $"{"FishIndustry.Issues".Translate()}: {list.ToCommaList()}"
            : $"{"FishIndustry.Issue".Translate()}: {list.First()}";
        stringBuilder.AppendLine();
        stringBuilder.Append(text2);

        return stringBuilder.ToString();
    }

    private void ComputeDrawingParameters()
    {
        if (microFungusRemainingDurationInTicks > 0)
        {
            microFungusFadingFactor = Mathf.Clamp01(0.2f + (microFungusRemainingDurationInTicks /
                                                            (60000f * IncidentDef.Named("MicroFungus").durationDays
                                                                .max)));
        }
        else if (speciesDef != null && Find.TickManager.TicksGame >= nextFishMoteTick)
        {
            nextFishMoteTick = Find.TickManager.TicksGame + fishMotePeriodInTicks;
            ThrowFishMote();
        }
    }

    private void ThrowFishMote()
    {
        if (!Position.ShouldSpawnMotesAt(Map) || Map.moteCounter.Saturated)
        {
            return;
        }

        var chance = Rand.Chance(0.5f);
        ThingDef thingDef = null;
        if (speciesDef == Util_FishIndustry.MashgonPawnKindDef)
        {
            thingDef = !chance ? Util_FishIndustry.MoteFishMashgonWestDef : Util_FishIndustry.MoteFishMashgonEastDef;
        }
        else if (speciesDef == Util_FishIndustry.BluebladePawnKindDef)
        {
            thingDef = !chance
                ? Util_FishIndustry.MoteFishBluebladeWestDef
                : Util_FishIndustry.MoteFishBluebladeEastDef;
        }
        else if (speciesDef == Util_FishIndustry.TailteethPawnKindDef)
        {
            thingDef = !chance
                ? Util_FishIndustry.MoteFishTailteethWestDef
                : Util_FishIndustry.MoteFishTailteethEastDef;
        }

        var moteThrown = (MoteThrown)ThingMaker.MakeThing(thingDef);
        moteThrown.Scale = Mathf.Lerp(0.2f, 0.6f, breedingProgressInTicks / (float)breedingDurationInTicks);
        var exactPosition = Position.ToVector3Shifted();
        moteThrown.exactRotation = Rand.Range(0f, 45f);
        if (chance)
        {
            exactPosition += 0.8f * Vector3Utility.HorizontalVectorFromAngle(270f + moteThrown.exactRotation);
            moteThrown.SetVelocity(90f + moteThrown.exactRotation, 0.25f);
        }
        else
        {
            exactPosition += 0.8f * Vector3Utility.HorizontalVectorFromAngle(90f + moteThrown.exactRotation);
            moteThrown.SetVelocity(270f + moteThrown.exactRotation, 0.25f);
        }

        moteThrown.exactPosition = exactPosition;
        GenSpawn.Spawn(moteThrown, Position, Map);
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);
        if (speciesDef != null)
        {
            Graphics.DrawMesh(MeshPool.plane10, breedingSpeciesMatrix, breedingSpeciesTexture, 0);
        }

        if (microFungusRemainingDurationInTicks > 0)
        {
            Graphics.DrawMesh(MeshPool.plane10, microFungusMatrix,
                FadedMaterialPool.FadedVersionOf(microFungusTexture, microFungusFadingFactor), 0);
        }
    }
}