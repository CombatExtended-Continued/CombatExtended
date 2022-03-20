using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using Verse.AI;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class LoaderAutoPatcher
    {
        static LoaderAutoPatcher()
        {
            foreach (var turret in DefDatabase<ThingDef>.AllDefs.Where(x => x.thingClass == typeof(Building_TurretGunCE)))
            {
                turret.AddComp(Comp_DropInMortar.Comp());
            }
        }
    }

    public class Comp_DropInMortar : ThingComp
    {
        public static CompProperties Comp()
        {
            var result = new CompProperties { compClass = typeof(Comp_DropInMortar) };

            return result;
        }

        public List<Pawn> loaders = new List<Pawn>();

        public override void PostExposeData()
        {
            Scribe_Collections.Look(ref loaders, "loaders", LookMode.Reference);
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (!loaders.Contains(selPawn))
            {
                yield return new FloatMenuOption("CE_ActAsLoader", delegate { loaders.Add(selPawn); });
            }
            else
            {
                yield return new FloatMenuOption("CE_StopActingAsLoader", delegate { loaders.Remove(selPawn); });
            }
        }

        public override IEnumerable<FloatMenuOption> CompMultiSelectFloatMenuOptions(List<Pawn> selPawns)
        {
            if (!loaders.All(x => selPawns.Contains(x)))
            {
                yield return new FloatMenuOption("CE_ActAsLoaders", delegate { loaders.AddRange(selPawns); });
            }
        }
    }
}
