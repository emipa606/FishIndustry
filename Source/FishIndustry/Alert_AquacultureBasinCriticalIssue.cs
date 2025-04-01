using RimWorld;
using Verse;

namespace FishIndustry;

public class Alert_AquacultureBasinCriticalIssue : Alert
{
    public Alert_AquacultureBasinCriticalIssue()
    {
        defaultLabel = "FishIndustry.CriticalIssueLabel".Translate();
        defaultExplanation = "FishIndustry.CriticalIssueExplanation".Translate();
    }

    public override AlertReport GetReport()
    {
        var maps = Find.Maps;
        foreach (var map in maps)
        {
            foreach (var item in map.listerBuildings.AllBuildingsColonistOfClass<Building_AquacultureBasin>())
            {
                if (!item.IsForbidden(Faction.OfPlayer) && item.speciesDef != null &&
                    (item.waterQuality < 0.1f || !item.fishesAreFed))
                {
                    return AlertReport.CulpritIs(item);
                }
            }
        }

        return AlertReport.Inactive;
    }
}