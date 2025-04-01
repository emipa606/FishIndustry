using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace FishIndustry;

internal class Building_FishingPier : Building_WorkTable
{
    public const float baseFishSpawnMtbPier = 240000f;

    public const float aquaticAreaRadius = 10f;

    public readonly List<IntVec3> aquaticCells = [];

    public bool allowFishing = true;

    public bool allowUsingGrain = true;

    public IntVec3 bankCell = new IntVec3(0, 0, 0);
    public string bankCellTerrainDefAsString;

    public float cachedFishSpawnMtb;

    public string cachedSpeciesInZone = "";

    public IntVec3 fishingSpotCell = new IntVec3(0, 0, 0);

    public int fishStock = 1;

    public bool isAffectedByBadTemperature;

    public bool isAffectedByBiome;

    public bool isAffectedByToxicFallout;

    public int marshCellsCount;

    public int maxFishStock = -1;

    public IntVec3 middleCell = new IntVec3(0, 0, 0);

    public string middleCellTerrainDefAsString;

    public int oceanCellsCount;

    public IntVec3 riverCell = new IntVec3(0, 0, 0);

    public int riverCellsCount;

    public string riverCellTerrainDefAsString;

    public int viableCellsCount => oceanCellsCount + riverCellsCount + marshCellsCount;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, true);
        bankCell = Position + new IntVec3(0, 0, -1).RotatedBy(Rotation);
        middleCell = Position + new IntVec3(0, 0, 0).RotatedBy(Rotation);
        riverCell = Position + new IntVec3(0, 0, 1).RotatedBy(Rotation);
        fishingSpotCell = Position + new IntVec3(0, 0, 2).RotatedBy(Rotation);
        if (respawningAfterLoad)
        {
            return;
        }

        var terrainDef = map.terrainGrid.TerrainAt(bankCell);
        bankCellTerrainDefAsString = terrainDef.ToString();
        map.terrainGrid.SetTerrain(bankCell,
            terrainDef == TerrainDef.Named("Marsh")
                ? Util_FishIndustry.FishingPierFloorMarshDef
                : Util_FishIndustry.FishingPierFloorShallowWaterDef);

        var terrainDef2 = map.terrainGrid.TerrainAt(middleCell);
        middleCellTerrainDefAsString = terrainDef2.ToString();
        map.terrainGrid.SetTerrain(middleCell,
            terrainDef2 == TerrainDef.Named("Marsh")
                ? Util_FishIndustry.FishingPierFloorMarshDef
                : Util_FishIndustry.FishingPierFloorShallowWaterDef);

        var terrainDef3 = map.terrainGrid.TerrainAt(riverCell);
        riverCellTerrainDefAsString = terrainDef2.ToString();
        map.terrainGrid.SetTerrain(riverCell,
            terrainDef3 == TerrainDef.Named("Marsh")
                ? Util_FishIndustry.FishingPierFloorMarshDef
                : Util_FishIndustry.FishingPierFloorShallowWaterDef);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref bankCellTerrainDefAsString, "bankTerrainCellDefAsString");
        Scribe_Values.Look(ref middleCellTerrainDefAsString, "middleTerrainCellDefAsString");
        Scribe_Values.Look(ref riverCellTerrainDefAsString, "riverTerrainCellDefAsString");
        Scribe_Values.Look(ref fishStock, "fishStock");
        Scribe_Values.Look(ref allowFishing, "allowFishing");
        Scribe_Values.Look(ref allowUsingGrain, "allowUsingGrain");
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        Map.terrainGrid.SetTerrain(bankCell, TerrainDef.Named(bankCellTerrainDefAsString));
        Map.terrainGrid.SetTerrain(middleCell, TerrainDef.Named(middleCellTerrainDefAsString));
        Map.terrainGrid.SetTerrain(riverCell, TerrainDef.Named(riverCellTerrainDefAsString));
        base.Destroy(mode);
    }

    public override void TickRare()
    {
        base.TickRare();
        UpdateCells();
        Util_Zone_Fishing.UpdateZoneProperties(Map, aquaticCells, ref oceanCellsCount, ref riverCellsCount,
            ref marshCellsCount, ref isAffectedByBiome, ref isAffectedByToxicFallout, ref isAffectedByBadTemperature,
            ref maxFishStock);
        cachedSpeciesInZone =
            Util_Zone_Fishing.GetSpeciesInZoneText(Map.Biome, oceanCellsCount, riverCellsCount, marshCellsCount);
        if (fishStock < maxFishStock && viableCellsCount > 0)
        {
            Util_Zone_Fishing.ComputeFishSpawnRateFactor(Map, oceanCellsCount, riverCellsCount, marshCellsCount,
                isAffectedByToxicFallout, isAffectedByBadTemperature, out var spawnRateFactor);
            cachedFishSpawnMtb = baseFishSpawnMtbPier * spawnRateFactor;
            var num = maxFishStock - fishStock;
            for (var i = 0; i < num; i++)
            {
                if (Rand.MTBEventOccurs(cachedFishSpawnMtb, 1f, 250f))
                {
                    fishStock++;
                }
            }
        }
        else if (fishStock > maxFishStock)
        {
            var num2 = fishStock - maxFishStock;
            fishStock -= num2;
        }
    }

    public void UpdateCells()
    {
        aquaticCells.Clear();
        foreach (var item in GenRadial.RadialCellsAround(fishingSpotCell, aquaticAreaRadius, false))
        {
            if (item.InBounds(Map) && Util_Zone_Fishing.IsAquaticTerrain(Map, item))
            {
                aquaticCells.Add(item);
            }
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        var num = 700000115;
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        var command_Toggle = new Command_Toggle
        {
            icon = ContentFinder<Texture2D>.Get(Util_FishIndustry.BluebladeTexturePath),
            defaultLabel = "FishIndustry.AllowFishingLabel".Translate(),
            defaultDesc = "FishIndustry.AllowFishingDesc".Translate(),
            isActive = () => allowFishing,
            toggleAction = delegate { allowFishing = !allowFishing; },
            groupKey = num + 1
        };
        yield return command_Toggle;
        var command_Toggle2 = new Command_Toggle
        {
            icon = ContentFinder<Texture2D>.Get(ThingDef.Named("RawCorn").graphicData.texPath),
            defaultLabel = "FishIndustry.AllowUsingGrainLabel".Translate(),
            defaultDesc = "FishIndustry.AllowUsingGrainDesc".Translate(),
            isActive = () => allowUsingGrain,
            toggleAction = delegate { allowUsingGrain = !allowUsingGrain; },
            groupKey = num + 2
        };
        yield return command_Toggle2;
    }

    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder();
        if (Util_FishIndustry.GetFishSpeciesList(Map.Biome).NullOrEmpty())
        {
            stringBuilder.Append("FishIndustry.FishingPier_InvalidBiome".Translate());
            return stringBuilder.ToString();
        }

        if (maxFishStock < 0)
        {
            UpdateCells();
            Util_Zone_Fishing.UpdateZoneProperties(Map, aquaticCells, ref oceanCellsCount, ref riverCellsCount,
                ref marshCellsCount, ref isAffectedByBiome, ref isAffectedByToxicFallout,
                ref isAffectedByBadTemperature, ref maxFishStock);
            cachedSpeciesInZone =
                Util_Zone_Fishing.GetSpeciesInZoneText(Map.Biome, oceanCellsCount, riverCellsCount, marshCellsCount);
        }

        stringBuilder.Append("FishIndustry.FishStock".Translate(fishStock));
        if (Prefs.DevMode)
        {
            stringBuilder.Append($"/{maxFishStock}");
        }

        stringBuilder.AppendLine();
        if (viableCellsCount < 50)
        {
            stringBuilder.Append("FishIndustry.NotEnoughViableSpace".Translate());
        }
        else
        {
            stringBuilder.Append("FishIndustry.SpeciesInZone".Translate() + cachedSpeciesInZone);
        }

        if (isAffectedByBiome || isAffectedByToxicFallout || isAffectedByBadTemperature)
        {
            stringBuilder.AppendLine();
            stringBuilder.Append("FishIndustry.AffectedBy".Translate());
            var stringBuilder2 = new StringBuilder();
            if (isAffectedByBiome)
            {
                stringBuilder2.AppendWithComma("FishIndustry.AffectedByBiome".Translate());
            }

            if (isAffectedByToxicFallout)
            {
                stringBuilder2.AppendWithComma("FishIndustry.AffectedByToxicFallout".Translate());
            }

            if (isAffectedByBadTemperature)
            {
                stringBuilder2.AppendWithComma("FishIndustry.AffectedByBadTemperature".Translate());
            }

            stringBuilder.Append(stringBuilder2);
        }

        if (!Prefs.DevMode)
        {
            return stringBuilder.ToString();
        }

        stringBuilder.AppendLine();
        stringBuilder.Append($"Spawn mean time: {((int)cachedFishSpawnMtb).ToStringTicksToPeriod()}");

        return stringBuilder.ToString();
    }
}