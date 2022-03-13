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
    public class AmmoGiverComp : ThingComp
    {
        public Pawn dad => this.parent as Pawn;
        public CompAmmoUser user => dad.equipment.Primary?.TryGetComp<CompAmmoUser>() ?? null;

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (!selPawn.Faction.HostileTo(dad.Faction))
            {
                if (selPawn.CanReach(dad, PathEndMode.ClosestTouch, Danger.Deadly)
                    &&
                    !selPawn.Downed
                    &&
                    selPawn.inventory.innerContainer.Where(x => x is AmmoThing).Any(x => ((AmmoDef)x.def).AmmoSetDefs.Contains(user.Props.ammoSet))
                    )
                {
                    if (user != null)
                    {
                        yield return new FloatMenuOption("CE_GiveAmmoToThing".Translate() + dad.Name.ToStringShort,
                        delegate
                        {
                            List<FloatMenuOption> options = new List<FloatMenuOption>();

                            foreach (AmmoThing ammo in selPawn.TryGetComp<CompInventory>().ammoList)
                            {
                                if (ammo.AmmoDef.AmmoSetDefs.Contains(user.Props.ammoSet))
                                {
                                    options.Add(new FloatMenuOption("CE_Give".Translate() + " " + ammo.Label, delegate
                                    {
                                        var jobdef = CE_JobDefOf.GiveAmmo;

                                        var job = new Job { def = jobdef, targetA = dad, targetB = ammo };

                                        selPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                                    }));
                                }
                            }

                            if (!options.Any())
                            {
                                options.Add(new FloatMenuOption("CE_NoAmmoToGive", null));
                            }

                            Find.WindowStack.Add(new FloatMenu(options));
                        });
                    }
                }
            }

        }
    }

}
