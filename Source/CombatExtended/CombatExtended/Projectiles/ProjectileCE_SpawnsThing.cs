using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;

namespace CombatExtended
{
    public class ProjectileCE_SpawnsThing : ProjectileCE
    {
        public override void Impact(Thing hitThing)
        {
            base.Impact(hitThing);
            Thing thing = GenSpawn.Spawn(ThingMaker.MakeThing(this.def.projectile.spawnsThingDef, null), Position, Map, WipeMode.Vanish);
            if (thing.def.CanHaveFaction && launcher != null)
            {
                thing.SetFaction(this.launcher.Faction, null);
            }
        }
    }
}
