using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;

namespace CombatExtended;
public class ProjectileCE_SpawnsThing : ProjectileCE
{

    public override bool AnimalsFleeImpact => false;

    public override void Impact(Thing hitThing)
    {
        Map map = Map;
        base.Impact(hitThing);
        Thing thing = GenSpawn.Spawn(ThingMaker.MakeThing(this.def.projectile.spawnsThingDef, null), Position, map, WipeMode.Vanish);
        if (thing.def.CanHaveFaction && launcher != null)
        {
            thing.SetFaction(this.launcher.Faction, null);
        }
    }
}
