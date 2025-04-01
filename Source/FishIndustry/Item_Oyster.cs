using Verse;

namespace FishIndustry;

internal class Item_Oyster : ThingWithComps
{
    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (Rand.Value < 0.02f)
        {
            GenSpawn.Spawn(Util_FishIndustry.PearlDef, Position, MapHeld);
        }

        base.Destroy(mode);
    }
}