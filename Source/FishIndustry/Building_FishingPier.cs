using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace FishIndustry;

internal class Building_FishingPier : Building_WorkTable
{
    private const float baseFishSpawnMtbPier = 240000f;

    private const float aquaticAreaRadius = 10f;

    public readonly List<IntVec3> aquaticCells = [];

    public bool allowFishing = true;

    public bool allowUsingGrain = true;

    public IntVec3 bankCell = new(0, 0, 0);
    private string bankCellTerrainDefAsString;

    private float cachedFishSpawnMtb;

    private string cachedSpeciesInZone = "";

    public int fishStock = 1;

    public IntVec3 fishingSpotCell = new(0, 0, 0);

    private bool isAffectedByBadTemperature;

    private bool isAffectedByBiome;

    private bool isAffectedByToxicFallout;

    private int marshCellsCount;

    private int maxFishStock = -1;

    public IntVec3 middleCell = new(0, 0, 0);

    private string middleCellTerrainDefAsString;

    private int oceanCellsCount;

    public IntVec3 riverCell = new(0, 0, 0);

    private string riverCellTerrainDefAsString;

    private int riverCellsCount;

    private int viableCellsCount => oceanCellsCount + riverCellsCount + marshCellsCount;

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
        if (ModLister.OdysseyInstalled)
        {
            return;
        }

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

        if (ModLister.OdysseyInstalled)
        {
            return;
        }

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

    private void UpdateCells()
    {
        aquaticCells.Clear();
        foreach (var item in GenRadial.RadialCellsAround(fishingSpotCell, aquaticAreaRadius, false)
                     .Where(c => c.InBounds(Map) && Util_Zone_Fishing.IsAquaticTerrain(Map, c)))
        {
            aquaticCells.Add(item);
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        var num = 700000115;
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (ModLister.OdysseyInstalled)
        {
            yield break;
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
        if (ModLister.OdysseyInstalled)
        {
            return base.GetInspectString();
        }

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