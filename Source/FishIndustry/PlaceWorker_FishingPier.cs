using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace FishIndustry;

public class PlaceWorker_FishingPier : PlaceWorker
{
    public static int lastMoteUpdateSecond;

    public static int lastMoteUpdateTick;

    public static IntVec3 lastMotePosition = IntVec3.Invalid;

    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
        Thing thingToIgnore = null, Thing thing = null)
    {
        if (lastMotePosition.IsValid && loc != lastMotePosition)
        {
            RemoveLastRespawnRateMote(map);
        }

        if (Util_FishIndustry.GetFishSpeciesList(map.Biome).NullOrEmpty())
        {
            return new AcceptanceReport("FishIndustry.FishingPier_InvalidBiome".Translate());
        }

        if (Util_PlaceWorker.IsNearFishingPier(map, loc, 15f))
        {
            return new AcceptanceReport("FishIndustry.TooCloseFishingPier".Translate());
        }

        if (Util_PlaceWorker.IsNearFishingZone(map, loc, 15f))
        {
            return new AcceptanceReport("FishIndustry.TooCloseFishingZone".Translate());
        }

        if (!Util_Zone_Fishing.IsAquaticTerrain(map, loc + new IntVec3(0, 0, -1).RotatedBy(rot)) ||
            !Util_Zone_Fishing.IsAquaticTerrain(map, loc + new IntVec3(0, 0, 0).RotatedBy(rot)) ||
            !Util_Zone_Fishing.IsAquaticTerrain(map, loc + new IntVec3(0, 0, 1).RotatedBy(rot)))
        {
            return new AcceptanceReport("FishIndustry.FishingPier_PierMustBeOnWater".Translate());
        }

        for (var i = -1; i <= 1; i++)
        {
            for (var j = 2; j <= 4; j++)
            {
                if (!Util_Zone_Fishing.IsAquaticTerrain(map, loc + new IntVec3(i, 0, j).RotatedBy(rot)))
                {
                    return new AcceptanceReport("FishIndustry.FishingPier_ZoneMustBeOnWater".Translate());
                }
            }
        }

        if (lastMotePosition.IsValid && (!Find.TickManager.Paused || DateTime.Now.Second == lastMoteUpdateSecond) &&
            (Find.TickManager.Paused || Find.TickManager.TicksGame < lastMoteUpdateTick + 60))
        {
            return true;
        }

        lastMoteUpdateSecond = DateTime.Now.Second;
        lastMoteUpdateTick = Find.TickManager.TicksGame;
        RemoveLastRespawnRateMote(map);
        DisplayMaxFishStockMoteAt(map, loc, rot);

        return true;
    }

    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
    {
        base.DrawGhost(def, center, rot, ghostCol, thing);
        var color = SimpleColor.Red;
        if (ghostCol == Designator_Place.CanPlaceColor)
        {
            color = SimpleColor.Green;
        }

        GenDraw.DrawCircleOutline(
            center.ToVector3Shifted() +
            new Vector3(0f, AltitudeLayer.Silhouettes.AltitudeFor(), 3f).RotatedBy(rot.AsAngle), 1.5f, color);
    }

    public void RemoveLastRespawnRateMote(Map map)
    {
        if (lastMotePosition.IsValid && lastMotePosition.InBounds(map))
        {
            foreach (var thing in lastMotePosition.GetThingList(map))
            {
                if (thing is not MoteText)
                {
                    continue;
                }

                thing.Destroy();
                break;
            }
        }

        lastMotePosition = IntVec3.Invalid;
    }

    public void DisplayMaxFishStockMoteAt(Map map, IntVec3 position, Rot4 rotation)
    {
        if (!position.InBounds(map))
        {
            return;
        }

        var num = Util_PlaceWorker.GetAquaticCellsInRadius(map, position + new IntVec3(0, 0, 2).RotatedBy(rotation),
            10f) - 3;
        var num2 = Mathf.RoundToInt(num / (float)(GenRadial.NumCellsInRadius(10f) - 3) * 100f);
        var textColor = Color.red;
        string text;
        if (num < 50)
        {
            text = "FishIndustry.FishingPier_TooFewWater".Translate();
        }
        else
        {
            text = $"{num2}%";
            switch (num2)
            {
                case >= 75:
                    textColor = Color.green;
                    break;
                case >= 50:
                    textColor = Color.yellow;
                    break;
                case >= 25:
                    textColor = new Color(255f, 165f, 0f);
                    break;
            }
        }

        var moteText = (MoteText)ThingMaker.MakeThing(ThingDefOf.Mote_Text);
        moteText.exactPosition = position.ToVector3Shifted();
        moteText.text = text;
        moteText.textColor = textColor;
        moteText.overrideTimeBeforeStartFadeout = 1f;
        GenSpawn.Spawn(moteText, position, map);
        lastMotePosition = position;
    }
}