using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace FishIndustry;

internal class Zone_Fishing : Zone
{
    private const float baseFishSpawnMtbZone = 420000f;

    private const int fishingZoneColorCount = 5;

    private static int nextFishingZoneColorIndex;

    public bool allowFishing = true;

    private float cachedFishSpawnMtb;

    private string cachedSpeciesInZone = "";

    public List<IntVec3> fishingSpots = [];

    private bool isAffectedByBadTemperature;

    private bool isAffectedByBiome;

    private bool isAffectedByMapCondition;

    private int marshCellsCount;

    private int maxFishStock = -1;

    public int nextUpdateTick;

    private int oceanCellsCount;

    private int riverCellsCount;

    public Zone_Fishing()
    {
    }

    public Zone_Fishing(ZoneManager zoneManager)
        : base("FishIndustry.FishingZone".Translate(), zoneManager)
    {
    }

    private int viableCellsCount => oceanCellsCount + riverCellsCount + marshCellsCount;

    public override bool IsMultiselectable => true;

    protected override Color NextZoneColor
    {
        get
        {
            var zoneColor = GetZoneColor(nextFishingZoneColorIndex);
            nextFishingZoneColorIndex++;
            if (nextFishingZoneColorIndex >= fishingZoneColorCount)
            {
                nextFishingZoneColorIndex = 0;
            }

            return zoneColor;
        }
    }

    private static Color GetZoneColor(int colorIndex)
    {
        Color black;
        switch (colorIndex)
        {
            case 0:
                black = Color.Lerp(new Color(0f, 0f, 1f), Color.gray, 0.5f);
                break;
            case 1:
                black = Color.Lerp(new Color(0f, 1f, 1f), Color.gray, 0.5f);
                break;
            case 2:
                black = Color.Lerp(new Color(0f, 0.5f, 1f), Color.gray, 0.5f);
                break;
            case 3:
                black = Color.Lerp(new Color(0.5f, 1f, 1f), Color.gray, 0.5f);
                break;
            default:
                black = Color.Lerp(new Color(0.5f, 0.5f, 1f), Color.gray, 0.5f);
                break;
        }

        black.a = 0.09f;

        return black;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref nextUpdateTick, "nextUpdateTick");
        Scribe_Collections.Look(ref fishingSpots, "fishingSpots", LookMode.Value);
        Scribe_Values.Look(ref allowFishing, "allowFishing");
    }

    public override void RemoveCell(IntVec3 c)
    {
        var cardinalDirections = GenAdj.CardinalDirections;
        foreach (var intVec in cardinalDirections)
        {
            var intVec2 = c + intVec;
            if (!fishingSpots.Contains(intVec2))
            {
                continue;
            }

            var keep = false;
            var cardinalDirections2 = GenAdj.CardinalDirections;
            foreach (var intVec3 in cardinalDirections2)
            {
                var intVec4 = intVec2 + intVec3;
                if (intVec4 == c || !cells.Contains(intVec4))
                {
                    continue;
                }

                keep = true;
                break;
            }

            if (!keep)
            {
                fishingSpots.Remove(intVec2);
            }
        }

        base.RemoveCell(c);
    }

    private bool IsNearFishingZone(IntVec3 cell)
    {
        var cardinalDirections = GenAdj.CardinalDirections;
        foreach (var intVec in cardinalDirections)
        {
            var item = cell + intVec;
            if (cells.Contains(item))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsBankCell(IntVec3 cell)
    {
        return cell.Standable(Map) && !Util_Zone_Fishing.IsAquaticTerrain(Map, cell);
    }

    private bool IsBridgeCell(IntVec3 cell)
    {
        return cell.Standable(Map) && cell.GetTerrain(Map) == TerrainDefOf.Bridge;
    }

    public void UpdateZone()
    {
        UpdateCellsAndFishingSpots();
        Util_Zone_Fishing.UpdateZoneProperties(Map, Cells, ref oceanCellsCount, ref riverCellsCount,
            ref marshCellsCount, ref isAffectedByBiome, ref isAffectedByMapCondition, ref isAffectedByBadTemperature,
            ref maxFishStock);
        cachedSpeciesInZone =
            Util_Zone_Fishing.GetSpeciesInZoneText(Map.Biome, oceanCellsCount, riverCellsCount, marshCellsCount);
        if (fishingSpots.Count < maxFishStock && viableCellsCount > 0)
        {
            Util_Zone_Fishing.ComputeFishSpawnRateFactor(Map, oceanCellsCount, riverCellsCount, marshCellsCount,
                isAffectedByMapCondition, isAffectedByBadTemperature, out var spawnRateFactor);
            cachedFishSpawnMtb = baseFishSpawnMtbZone * spawnRateFactor;
            var num = maxFishStock - fishingSpots.Count;
            var num2 = 0;
            for (var i = 0; i < num; i++)
            {
                if (Rand.MTBEventOccurs(cachedFishSpawnMtb, 1f, 250f))
                {
                    num2++;
                }
            }

            if (num2 <= 0)
            {
                return;
            }

            var bankCells = new List<IntVec3>();
            var bridgeCells = new List<IntVec3>();
            GetFreeBankAndBridgeCells(num, ref bankCells, ref bridgeCells);
            for (var j = 0; j < num2; j++)
            {
                if (bankCells.Count == 0 && bridgeCells.Count == 0)
                {
                    break;
                }

                TrySpawnNewFish(ref bankCells, ref bridgeCells);
            }
        }
        else if (fishingSpots.Count > maxFishStock)
        {
            var num3 = fishingSpots.Count - maxFishStock;
            for (var k = 0; k < num3; k++)
            {
                var item = fishingSpots.RandomElement();
                fishingSpots.Remove(item);
            }
        }
    }

    private void UpdateCellsAndFishingSpots()
    {
        var list = new List<IntVec3>();
        foreach (var cell in Cells)
        {
            if (!Util_Zone_Fishing.IsAquaticTerrain(Map, cell) || !cell.Walkable(Map))
            {
                list.Add(cell);
            }
        }

        foreach (var item in list)
        {
            RemoveCell(item);
        }

        list.Clear();
        foreach (var fishingSpot in fishingSpots)
        {
            if (!IsNearFishingZone(fishingSpot))
            {
                list.Add(fishingSpot);
            }
        }

        foreach (var item2 in list)
        {
            fishingSpots.Remove(item2);
        }
    }

    private void GetFreeBankAndBridgeCells(int requiredBankCellsCount, ref List<IntVec3> bankCells,
        ref List<IntVec3> bridgeCells)
    {
        var num = 0;
        bankCells.Clear();
        bridgeCells.Clear();
        foreach (var item in cells.InRandomOrder())
        {
            foreach (var item2 in GenAdj.CardinalDirections.InRandomOrder())
            {
                var intVec = item + item2;
                if (IsBankCell(intVec) && !fishingSpots.Contains(intVec) && !bankCells.Contains(intVec))
                {
                    bankCells.Add(intVec);
                }

                if (!IsBridgeCell(intVec) || fishingSpots.Contains(intVec) || bridgeCells.Contains(intVec))
                {
                    continue;
                }

                bridgeCells.Add(intVec);
                num++;
                if (num >= requiredBankCellsCount)
                {
                    return;
                }
            }
        }
    }

    private void TrySpawnNewFish(ref List<IntVec3> bankCells, ref List<IntVec3> bridgeCells)
    {
        foreach (var bridgeCell in bridgeCells)
        {
            if (fishingSpots.Contains(bridgeCell))
            {
                continue;
            }

            fishingSpots.Add(bridgeCell);
            bridgeCells.Remove(bridgeCell);
            return;
        }

        foreach (var bankCell in bankCells)
        {
            if (fishingSpots.Contains(bankCell))
            {
                continue;
            }

            fishingSpots.Add(bankCell);
            bankCells.Remove(bankCell);
            MoteMaker.PlaceTempRoof(bankCell, Map);
            break;
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        var num = 700000114;
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
            UpdateCellsAndFishingSpots();
            Util_Zone_Fishing.UpdateZoneProperties(Map, Cells, ref oceanCellsCount, ref riverCellsCount,
                ref marshCellsCount, ref isAffectedByBiome, ref isAffectedByMapCondition,
                ref isAffectedByBadTemperature, ref maxFishStock);
            cachedSpeciesInZone =
                Util_Zone_Fishing.GetSpeciesInZoneText(Map.Biome, oceanCellsCount, riverCellsCount, marshCellsCount);
        }

        stringBuilder.Append("FishIndustry.FishStock".Translate(fishingSpots.Count));
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

        if (isAffectedByBiome || isAffectedByMapCondition || isAffectedByBadTemperature)
        {
            stringBuilder.AppendLine();
            stringBuilder.Append("FishIndustry.AffectedBy".Translate());
            var stringBuilder2 = new StringBuilder();
            if (isAffectedByBiome)
            {
                stringBuilder2.AppendWithComma("FishIndustry.AffectedByBiome".Translate());
            }

            if (isAffectedByMapCondition)
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