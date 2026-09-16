using System;
using VanillaGravshipExpanded2;
using Verse;

namespace CombatExtended.Compatibility.VGE2Compat;
public class PawnBurnOnly : Pawn
{
    public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
    {
        if (dinfo.Def.hediff != InternalDefOf.Burn && dinfo.Def.hediff.defName != "BurnSecondary") // also handles incendiary weapon
        {
            // Maximum dammage for non fire damage is 5
            dinfo.SetAmount(Math.Min(dinfo.Amount, 5f));
        }
        base.PreApplyDamage(ref dinfo, out absorbed);
    }
}
