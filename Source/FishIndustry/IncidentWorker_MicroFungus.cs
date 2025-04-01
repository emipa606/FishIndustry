using RimWorld;
using Verse;

namespace FishIndustry;

public class IncidentWorker_MicroFungus : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        var map = (Map)parms.target;
        return map.listerBuildings.AllBuildingsColonistOfDef(Util_FishIndustry.AquacultureBasinDef).Count > 0;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        var map = (Map)parms.target;
        Building_AquacultureBasin building_AquacultureBasin = null;
        foreach (var item in map.listerBuildings.AllBuildingsColonistOfDef(Util_FishIndustry.AquacultureBasinDef))
        {
            building_AquacultureBasin = item as Building_AquacultureBasin;
            var num = (int)(60000f * def.durationDays.min * Rand.Range(0.8f, 1.2f));
            var room = building_AquacultureBasin?.InteractionCell.GetRoom(map);
            if (room == null || room.PsychologicallyOutdoors)
            {
                num = (int)(60000f * def.durationDays.max * Rand.Range(0.8f, 1.2f));
            }
            else
            {
                var num2 = 0f - room.GetStat(RoomStatDefOf.Cleanliness);
                if (num2 > 0f)
                {
                    num += (int)(60000f * def.durationDays.min * num2);
                }
            }

            building_AquacultureBasin?.StartMicroFungusInfestation(num);
        }

        if (building_AquacultureBasin != null)
        {
            Find.LetterStack.ReceiveLetter("FishIndustry.LetterLabelMicroFungus".Translate(),
                "FishIndustry.MicroFungus".Translate(), LetterDefOf.NegativeEvent, building_AquacultureBasin);
        }

        return true;
    }
}