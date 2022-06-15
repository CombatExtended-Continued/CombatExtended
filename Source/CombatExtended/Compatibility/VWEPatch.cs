using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using CombatExtended;
using UnityEngine;
using Verse.Sound;

namespace CombatExtended.Compatibility
{
    public class Verb_ShootCEKEK : Verb_ShootCE
    {
	public override int ShotsPerBurstFor(FireMode mode)
	{
	    if (base.CompFireModes != null)
	    {
		if (mode == FireMode.SingleFire)
		{

		    int lint = Rand.Range(0, 2);
		    if (lint != 0)
		    {
			return lint;
		    }
			
		    if (Rand.Range(0, 1000) == 1000)
		    {
			GenExplosionCE.DoExplosion(caster.Position, caster.Map, 2f, DamageDefOf.Bomb, null, 5, 0f, SoundDefOf.Psycast_Skip_Exit, null, null, null, null, 0, 0, true, null, 0f, 0, 0f, false);
		    }
		    return 0;
			

		}
		if (mode == FireMode.BurstFire)
		{
		    return Rand.Range(0, base.CompFireModes.Props.aimedBurstShotCount + 1);
		}
	    }
	    return Rand.Range(0, base.VerbPropsCE.burstShotCount + 1);
	    
	}
    }
    public class RandomDamage : ThingComp
    {
	public int max_damage  => Props.max_damage;
	public int Pellets => Props.pelletsMax;
	public int pletts => Props.pelletsMin;
		
	public RandDamageProps Props => (RandDamageProps)this.props;
	public override void Initialize(CompProperties props)
	{
	    AmmoDef me = parent.def as AmmoDef;
	    ProjectilePropertiesCE nomo = me.projectile as ProjectilePropertiesCE;
	    nomo.speed = Rand.Range(50, 200);
			
	    //Log.Error(nomo.pelletCount.ToString());
	    //nomo.armorPenetrationSharp
			
			
			
	    base.Initialize(props);
	}
	public override void CompTick()
	{
	    AmmoDef me = parent.def as AmmoDef;
	    ProjectilePropertiesCE nomo = me.projectile as ProjectilePropertiesCE;
	    nomo.pelletCount = Rand.Range(pletts, Pellets);
	    //Log.Error(nomo.pelletCount.ToString());
	}
		

    }
    public class RandDamageProps : CompProperties
    {
	public int pelletsMax;
	public int max_damage;
	public int pelletsMin;
	public RandDamageProps()
	{
	    this.compClass = typeof(RandomDamage);
	}

	public RandDamageProps(Type compClass) : base(compClass)
	{
	    this.compClass = compClass;
	}
    }
}

