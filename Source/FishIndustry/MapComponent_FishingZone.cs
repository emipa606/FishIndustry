using Verse;

namespace FishIndustry;

internal class MapComponent_FishingZone(Map map) : MapComponent(map)
{
    public const int updatePeriodInTicks = 250;

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        foreach (var allZone in map.zoneManager.AllZones)
        {
            if (allZone is not Zone_Fishing zoneFishing)
            {
                continue;
            }

            if (Find.TickManager.TicksGame < zoneFishing.nextUpdateTick)
            {
                continue;
            }

            zoneFishing.nextUpdateTick = Find.TickManager.TicksGame + updatePeriodInTicks;
            zoneFishing.UpdateZone();
        }
    }
}