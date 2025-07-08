using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended;
public class CompCIWSImpactHandler_Projectile : CompCIWSImpactHandler
{
    public override void OnImpact(ProjectileCE projectile, DamageInfo dinfo)
    {
        projectile.ExactPosition = parent.DrawPos;
        base.OnImpact(projectile, dinfo);
    }
}
