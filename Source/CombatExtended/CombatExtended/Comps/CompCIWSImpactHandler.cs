using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended;
public class CompCIWSImpactHandler : ThingComp
{
    private float hp;

    public float HP
    {
        get => hp;
        set => hp = value;
    }
    public CompProperties_CIWSImpactHandler Props => props as CompProperties_CIWSImpactHandler;
    public virtual void OnImpact(ProjectileCE projectile, DamageInfo dinfo)
    {
        if (!Props.impacted.NullOrUndefined())
        {
            Props.impacted.PlayOneShot(new TargetInfo(parent.DrawPos.ToIntVec3(), parent.Map));
        }
        if (Props.impactEffecter != null)
        {
            Props.impactEffecter.Spawn(parent.DrawPos.ToIntVec3(), parent.Map);
        }
        ApplyDamage(dinfo);
    }
    public virtual void ApplyDamage(DamageInfo dinfo)
    {
        HP -= dinfo.Amount;
        if (HP <= 0.00001f && !parent.Destroyed)
        {
            OnDestroying(dinfo);
        }
    }
    protected virtual void OnDestroying(DamageInfo dinfo)
    {
        parent.Destroy(DestroyMode.Vanish);
    }
    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref hp, nameof(HP));
    }
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (!respawningAfterLoad)
        {
            HP = Props.HP;
        }
    }

}
public class CompProperties_CIWSImpactHandler : CompProperties
{
    public CompProperties_CIWSImpactHandler()
    {
        compClass = typeof(CompCIWSImpactHandler);
    }
    public float HP = 0.0001f;
    public SoundDef impacted;
    public EffecterDef impactEffecter;
}
