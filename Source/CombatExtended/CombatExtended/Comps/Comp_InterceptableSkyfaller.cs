using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{
    public class Comp_InterceptableSkyfaller : ThingComp
    {
        #region Static

        public static Dictionary<Map, List<Thing>> interceptableSkyfallers = new Dictionary<Map, List<Thing>>();
        public static List<Thing> InterceptableSkyfallersPerMap(Map map) => interceptableSkyfallers.TryGetValue(map, out var result) ? result : new List<Thing>();

        #endregion

        public float currentHP;

        public CompProperties_InterceptableSkyfaller Props => props as CompProperties_InterceptableSkyfaller;
        public virtual float MaxHP => Props.HP;

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (interceptableSkyfallers.TryGetValue(map, out List<Thing> skyfallers))
            {
                skyfallers.Remove(parent);
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            var map = this.parent.Map;
            if (!interceptableSkyfallers.TryGetValue(map, out List<Thing> skyfallers))
            {
                skyfallers = new List<Thing>();
                interceptableSkyfallers.Add(map, skyfallers);
            }
            skyfallers.Add(parent);
            if (!respawningAfterLoad)
            {
                currentHP = MaxHP;
            }
        }
        public virtual void Impact(ProjectileCE projectile)
        {
            if ((projectile.def.projectile as ProjectilePropertiesCE).armorPenetrationSharp < Props.sharpArmor)
            {
                // Defclect logic and sfx
                return;
            }
            if (Rand.Chance(Props.instantDestroyChance))
            {
                currentHP = 0.0f;
            }
            else
            {
                var damage = projectile.DamageAmount;
                currentHP -= damage;
                if (parent is IThingHolder thingHolder && Rand.Chance(Props.chanceToHitContainingThings))
                {
                    var things = thingHolder.GetDirectlyHeldThings().OfType<ActiveDropPod>().SelectMany(x => x.contents.innerContainer);
                    var thingToImpact = things.Where(x =>!(x is Corpse)).RandomElement();
                    projectile.Impact(thingToImpact);
                }
                else
                {
                    projectile.Impact(null);
                }
            }
            if (currentHP <= 0.0f)
            {
                OnDestoying(projectile);
                parent.Destroy(DestroyMode.KillFinalize);
            }
        }
        public virtual void OnDestoying(ProjectileCE projectile)
        {
            FleckMakerCE.ThrowLightningGlow(parent.DrawPos, parent.Map, 20f);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentHP, "currentHP");
        }
    }
}
