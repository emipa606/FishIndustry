using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FishIndustry;

public class PawnKindDef_FishSpecies : PawnKindDef
{
    public readonly int breedQuantity = 0;
    public readonly float breedingDurationInDays = 1f;

    public readonly float commonality = 0.2f;

    public readonly bool livesInMarsh = false;
    public readonly bool livesInOcean = false;

    public readonly bool livesInRiver = false;

    public List<BiomeDef> naturalBiomes;
}