using RimWorld;
using Verse;

namespace CombatExtended.Compatibility.VGECompat2;
public class PawnBurnOnly : Pawn
{
    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        Log.Message("SpawnSetup");

        base.SpawnSetup(map, respawningAfterLoad);
    }
    
    public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
    {
        Log.Message("PreApplyDamage");

        Log.Message("Dinfos" + dinfo);
        if (dinfo.Def != DamageDefOf.Burn)
        {
            dinfo.SetAmount(0);
        }
        base.PreApplyDamage(ref dinfo, out absorbed);
    }

    public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
    {
        Log.Message("PostApplyDamage");

        base.PostApplyDamage(dinfo, totalDamageDealt);
    }
}
