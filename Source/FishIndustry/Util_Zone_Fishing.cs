using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace FishIndustry;

public static class Util_Zone_Fishing
{
    public const int minCellsToSpawnFish = 50;

    public static bool IsAquaticTerrain(Map map, IntVec3 position)
    {
        var terrainDef = map.terrainGrid.TerrainAt(position);
        return terrainDef == TerrainDefOf.WaterShallow || terrainDef == TerrainDefOf.WaterOceanShallow ||
               terrainDef == TerrainDefOf.WaterMovingShallow || terrainDef == TerrainDefOf.WaterDeep ||
               terrainDef == TerrainDefOf.WaterOceanDeep || terrainDef == TerrainDefOf.WaterMovingChestDeep ||
               terrainDef == TerrainDef.Named("Marsh");
    }

    public static void UpdateZoneProperties(Map map, List<IntVec3> aquaticCells, ref int oceanCellsCount,
        ref int riverCellsCount, ref int marshCellsCount, ref bool isAffectedByBiome, ref bool isAffectedByToxicFallout,
        ref bool isAffectedByBadTemperature, ref int maxFishStock)
    {
        var zoneIsOutdoor = false;
        var zoneIsUnroofed = false;
        oceanCellsCount = 0;
        riverCellsCount = 0;
        marshCellsCount = 0;
        isAffectedByBiome = false;
        isAffectedByToxicFallout = false;
        isAffectedByBadTemperature = false;
        UpdateViableCells(map, aquaticCells, ref oceanCellsCount, ref riverCellsCount, ref marshCellsCount,
            ref zoneIsOutdoor, ref zoneIsUnroofed);
        UpdateAffectionIndicators(map, zoneIsOutdoor, zoneIsUnroofed, ref isAffectedByBiome,
            ref isAffectedByToxicFallout, ref isAffectedByBadTemperature);
        UpdateMaxFishStock(map.Biome, oceanCellsCount + riverCellsCount + marshCellsCount, isAffectedByBiome,
            isAffectedByToxicFallout, isAffectedByBadTemperature, ref maxFishStock);
    }

    public static void UpdateViableCells(Map map, List<IntVec3> aquaticCells, ref int oceanCellsCount,
        ref int riverCellsCount, ref int marshCellsCount, ref bool zoneIsOutdoor, ref bool zoneIsUnroofed)
    {
        var toxicFallout = map.GameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout);
        foreach (var aquaticCell in aquaticCells)
        {
            if (aquaticCell.UsesOutdoorTemperature(map))
            {
                zoneIsOutdoor = true;
            }

            if (!aquaticCell.Roofed(map))
            {
                zoneIsUnroofed = true;
                if (toxicFallout)
                {
                    continue;
                }
            }

            var terrainDef = map.terrainGrid.TerrainAt(aquaticCell);
            if (terrainDef == TerrainDefOf.WaterOceanShallow || terrainDef == TerrainDefOf.WaterOceanDeep)
            {
                oceanCellsCount++;
            }
            else if (terrainDef == TerrainDefOf.WaterMovingShallow || terrainDef == TerrainDefOf.WaterShallow ||
                     terrainDef == TerrainDefOf.WaterDeep)
            {
                riverCellsCount++;
            }
            else if (terrainDef == TerrainDef.Named("Marsh"))
            {
                marshCellsCount++;
            }
        }
    }

    public static void UpdateAffectionIndicators(Map map, bool zoneIsOutdoor, bool zoneIsUnroofed,
        ref bool isAffectedByBiome, ref bool isAffectedByToxicFallout, ref bool isAffectedByBadTemperature)
    {
        if (GetBiomeMaxFishStockFactor(map.Biome) < 1f)
        {
            isAffectedByBiome = true;
        }

        if (zoneIsUnroofed && map.GameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout))
        {
            isAffectedByToxicFallout = true;
        }

        if (zoneIsOutdoor && (map.gameConditionManager.ConditionIsActive(GameConditionDefOf.VolcanicWinter) ||
                              !map.weatherManager.growthSeasonMemory.GrowthSeasonOutdoorsNow))
        {
            isAffectedByBadTemperature = true;
        }
    }

    public static void UpdateMaxFishStock(BiomeDef biome, int viableCellsCount, bool isAffectedByBiome,
        bool isAffectedByToxicFallout, bool isAffectedByBadTemperature, ref int maxFishStock)
    {
        var num = 1f;
        if (viableCellsCount < 50f)
        {
            maxFishStock = 0;
            return;
        }

        var num2 = GetBiomeMaxFishStockFactor(biome);
        if (isAffectedByToxicFallout)
        {
            num -= 0.25f;
        }

        if (isAffectedByBadTemperature)
        {
            num -= 0.25f;
        }

        var f = viableCellsCount / 50f * num2 * num;
        maxFishStock = Math.Max(1, Mathf.RoundToInt(f));
    }

    public static void ComputeFishSpawnRateFactor(Map map, int oceanCellsCount, int riverCellsCount,
        int marshCellsCount, bool isAffectedByToxicFallout, bool isAffectedByBadTemperature, out float spawnRateFactor)
    {
        spawnRateFactor = 0f;
        var num = oceanCellsCount + riverCellsCount + marshCellsCount;
        if (num == 0)
        {
            return;
        }

        var biomeFishSpawnRateFactor = GetBiomeFishSpawnRateFactor(map.Biome);
        var num2 = num / ((oceanCellsCount * 1f) + (riverCellsCount * 0.75f) + (marshCellsCount * 0.5f));
        var num3 = 1f;
        if (isAffectedByBadTemperature &&
            map.gameConditionManager.ConditionIsActive(GameConditionDefOf.VolcanicWinter))
        {
            num3 += 0.5f;
        }

        if (isAffectedByBadTemperature && !map.weatherManager.growthSeasonMemory.GrowthSeasonOutdoorsNow)
        {
            num3 += 0.5f;
        }

        spawnRateFactor = biomeFishSpawnRateFactor * num2 * num3;
        spawnRateFactor /= Settings.fishRespawnRateFactor;
    }

    public static float GetBiomeMaxFishStockFactor(BiomeDef biome)
    {
        var result = 1f;
        if (biome == BiomeDefOf.BorealForest)
        {
            result = 0.8f;
        }
        else if (biome == BiomeDefOf.Tundra || biome == BiomeDef.Named("AridShrubland"))

        {
            result = 0.6f;
        }
        else if (biome == BiomeDefOf.SeaIce || biome == BiomeDefOf.Desert)
        {
            result = 0.4f;
        }
        else if (biome == BiomeDefOf.IceSheet || biome == BiomeDef.Named("ExtremeDesert"))
        {
            result = 0.2f;
        }

        return result;
    }

    public static float GetBiomeFishSpawnRateFactor(BiomeDef biome)
    {
        var result = 1f;
        if (biome == BiomeDefOf.BorealForest)
        {
            result = 1.2f;
        }
        else if (biome == BiomeDefOf.Tundra || biome == BiomeDef.Named("AridShrubland"))

        {
            result = 1.4f;
        }
        else if (biome == BiomeDefOf.SeaIce || biome == BiomeDefOf.Desert)
        {
            result = 1.6f;
        }
        else if (biome == BiomeDefOf.IceSheet || biome == BiomeDef.Named("ExtremeDesert"))
        {
            result = 1.8f;
        }

        return result;
    }

    public static string GetSpeciesInZoneText(BiomeDef biome, int oceanCellsCount, int riverCellsCount,
        int marshCellsCount)
    {
        var stringBuilder = new StringBuilder();
        foreach (var fishSpecies in Util_FishIndustry.GetFishSpeciesList(biome))
        {
            if (oceanCellsCount > 0 && fishSpecies.livesInOcean)
            {
                stringBuilder.AppendWithComma(fishSpecies.label);
            }
            else if (riverCellsCount > 0 && fishSpecies.livesInRiver)
            {
                stringBuilder.AppendWithComma(fishSpecies.label);
            }
            else if (marshCellsCount > 0 && fishSpecies.livesInMarsh)
            {
                stringBuilder.AppendWithComma(fishSpecies.label);
            }
        }

        if (stringBuilder.Length == 0)
        {
            stringBuilder.Append("none");
        }

        return stringBuilder.ToString();
    }
}