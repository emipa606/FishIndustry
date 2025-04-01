using RimWorld;
using Verse;

namespace FishIndustry;

public class Alert_AquacultureBasinNeedsHopper : Alert
{
    public Alert_AquacultureBasinNeedsHopper()
    {
        defaultLabel = "FishIndustry.NeedHopperLabel".Translate();
        defaultExplanation = "FishIndustry.NeedHopperExplanation".Translate();
    }

    public override AlertReport GetReport()
    {
        var maps = Find.Maps;
        foreach (var map in maps)
        {
            foreach (var item in map.listerBuildings
                         .AllBuildingsColonistOfDef(Util_FishIndustry.AquacultureBasinDef))
            {
                var hasHopper = false;
                foreach (var item2 in GenAdj.CellsAdjacentCardinal(item))
                {
                    Thing edifice = item2.GetEdifice(map);
                    if (edifice == null || edifice.def != ThingDefOf.Hopper)
                    {
                        continue;
                    }

                    hasHopper = true;
                    break;
                }

                if (!hasHopper)
                {
                    return AlertReport.CulpritIs(item);
                }
            }
        }

        return AlertReport.Inactive;
    }
}