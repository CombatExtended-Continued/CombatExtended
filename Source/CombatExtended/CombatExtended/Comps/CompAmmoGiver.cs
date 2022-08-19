using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
    public class CompAmmoGiver : ThingComp
    {
        public int ammoAmountToGive;
        public Pawn dad => this.parent as Pawn;
        public CompAmmoUser user => dad.equipment.Primary?.TryGetComp<CompAmmoUser>() ?? null;

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (user != null && selPawn != dad)
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
                        yield return new FloatMenuOption("CE_GiveAmmoToThing".Translate() + (dad.Name?.ToStringShort ?? dad.def.label),
                          delegate
                          {
                              List<FloatMenuOption> options = new List<FloatMenuOption>();

                              foreach (AmmoThing ammo in selPawn.TryGetComp<CompInventory>().ammoList)
                              {
                                  if (ammo.AmmoDef.AmmoSetDefs.Contains(user.Props.ammoSet))
                                  {
                                      int outAmmoCount = 0;
                                      if (dad.TryGetComp<CompInventory>()?.CanFitInInventory(ammo, out outAmmoCount) ?? false && outAmmoCount >= ammo.stackCount)
                                      {
                                          options.Add(new FloatMenuOption("CE_Give".Translate() + " " + ammo.Label + " (" + "All".Translate() + ")", delegate
                                          {
                                              ammoAmountToGive = ammo.stackCount;

                                              var jobdef = CE_JobDefOf.GiveAmmo;

                                              var job = new Job { def = jobdef, targetA = dad, targetB = ammo };

                                              selPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                                          }));
                                      }
                                      
                                      if (outAmmoCount > 0)
                                      {
                                          options.Add(new FloatMenuOption("CE_Give".Translate() + " " + ammo.def.label + "...", delegate
                                          {
                                              Find.WindowStack.Add(new Window_GiveAmmoAmountSlider() { dad = dad, sourceAmmo = ammo, selPawn = selPawn, sourceComp = this, maxAmmoCount = outAmmoCount });
                                          }));
                                      }
                                      else
                                      {
                                          options.Add(new FloatMenuOption("CE_TargetInventoryFull".Translate(), null));
                                      }
                                      
                                  }
                              }

                              if (!options.Any())
                              {
                                  options.Add(new FloatMenuOption("CE_NoAmmoToGive".Translate(), null));
                              }

                              Find.WindowStack.Add(new FloatMenu(options));
                          });
                    }
                }
            }

        }
    }
}
