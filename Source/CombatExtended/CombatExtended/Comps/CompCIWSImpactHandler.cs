using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended
{
    public class CompCIWSImpactHandler : ThingComp
    {
        public CompProperties_CIWSImpactHandler Props => props as CompProperties_CIWSImpactHandler;
     
        public virtual void OnImpact(ProjectileCE projectile, DamageInfo dinfo)
        {
            parent.Position = projectile.Position;
            if (!Props.impacted.NullOrUndefined())
            {
                Props.impacted.PlayOneShot(new TargetInfo(parent.DrawPos.ToIntVec3(), parent.Map));
            }
            parent.Destroy(DestroyMode.Vanish);
        }
        
    }
    public class CompProperties_CIWSImpactHandler : CompProperties
    {
        public CompProperties_CIWSImpactHandler()
        {
            compClass = typeof(CompCIWSImpactHandler);
        }
        public SoundDef impacted;
    }
}
