using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended;
public class StatWorker_LongRange : StatWorker
{
    public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
    {
        float result = ((req.Def as ThingDef)?.GetProjectile()?.projectile as ProjectilePropertiesCE)?.shellingProps?.range ?? 0f;
        return result;
    }
    public override bool ShouldShowFor(StatRequest req)
    {
        return this.GetValueUnfinalized(req) > 0f && base.ShouldShowFor(req);
    }
}
