using RimWorld;
using Verse;

namespace FishIndustry;

public class Alert_AquacultureBasinNotBreeding : Alert
{
    public Alert_AquacultureBasinNotBreeding()
    {
        defaultLabel = "FishIndustry.IdleAquacultureBasinLabel".Translate();
        defaultExplanation = "FishIndustry.IdleAquacultureBasinExplanation".Translate();
    }

    public override AlertReport GetReport()
    {
        if (ModLister.OdysseyInstalled)
        {
            return AlertReport.Inactive;
        }

        var maps = Find.Maps;
        foreach (var map in maps)
        {
            foreach (var item in map.listerBuildings.AllBuildingsColonistOfClass<Building_AquacultureBasin>())
            {
                if (item.powerComp.PowerOn && !item.IsForbidden(Faction.OfPlayer) && item.speciesDef == null &&
                    item.microFungusRemainingDurationInTicks == 0)
                {
                    return AlertReport.CulpritIs(item);
                }
            }
        }

        return AlertReport.Inactive;
    }
}