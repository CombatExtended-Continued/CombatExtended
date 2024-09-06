using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using System.Xml;
using CombatExtended.Compatibility;
using UnityEngine;
using Verse.AI;

namespace CombatExtended
{
    /// The file has a few classes but they are all extremely closely related to splitting them to files would only hinder working on it.

    public class ERAComponent : IExposable
    {
        public BodyPartDef part;

        public float armor;

        public float damageTreshold;

        public float APTreshold;

        public bool triggered = false;

        public CompProperties_Fragments frags;

        public List<DamageDef> ignoredDmgDefs;

        public CompFragments fragComp => new CompFragments()
        {
            props = frags
        };

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            foreach (XmlNode node in xmlRoot.ChildNodes)
            {
                switch (node.Name.ToLower())
                {
                    case "armor":
                        armor = ParseHelper.ParseFloat(node.InnerText);
                        break;
                    case "damagetreshold":
                        damageTreshold = ParseHelper.ParseFloat(node.InnerText);
                        break;
                    case "aptreshold":
                        damageTreshold = ParseHelper.ParseFloat(node.InnerText);
                        break;
                    case "part":
                        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "part", node.InnerText, null, null);
                        break;
                    case "triggered":
                        triggered = ParseHelper.ParseBool(node.InnerText);
                        break;
                    case "frags":
                        frags = new CompProperties_Fragments()
                        {
                            fragments = new List<ThingDefCountClass>()
                        };

                        foreach (XmlNode node2 in node.ChildNodes)
                        {
                            if (node2.Name == "fragments")
                            {
                                foreach (XmlNode node3 in node2.ChildNodes)
                                {
                                    ThingDefCountClass count = new ThingDefCountClass();

                                    count.LoadDataFromXmlCustom(node3);

                                    frags.fragments.Add(count);
                                }
                            }
                            if (node2.Name == "fragSpeedFactor")
                            {
                                frags.fragSpeedFactor = ParseHelper.ParseFloat(node2.InnerText);
                            }
                        }
                        break;
                    case "ignoreddmgdefs":
                        ignoredDmgDefs = new List<DamageDef>();

                        foreach (XmlNode node2 in node.ChildNodes)
                        {
                            DirectXmlCrossRefLoader.RegisterListWantsCrossRef(ignoredDmgDefs, node2.InnerText);
                        }
                        break;
                }
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref triggered, "triggered");
        }
    }

    public class CompArmorDurability : ThingComp
    {
        public CompProperties_ArmorDurability durabilityProps => props as CompProperties_ArmorDurability;

        public float maxDurability => durabilityProps.Durability;

        public float curDurability;
        public float curDurabilityPercent => (float)Math.Round(curDurability / maxDurability, 2);

        public bool regens = false;

        public int timer;

        public override void CompTick()
        {
            if (durabilityProps.Regenerates)
            {
                if (timer >= durabilityProps.RegenInterval)
                {
                    if (curDurability < maxDurability)
                    {
                        curDurability += Math.Min(durabilityProps.RegenValue, maxDurability - curDurability);
                    }
                    timer = 0;
                }
                timer++;
            }
            base.CompTick();
        }

        public override void PostPostMake()
        {
            curDurability = maxDurability;
            regens = durabilityProps.Regenerates;
            base.PostPostMake();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            regens = durabilityProps.Regenerates;
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref curDurability, "curDurability");
            Scribe_Values.Look(ref timer, "timer");
            base.PostExposeData();
        }

        public override string CompInspectStringExtra()
        {
            if (maxDurability != 500)
            {
                return "CE_ArmorDurability".Translate() + curDurability.ToString() + "/" + maxDurability.ToString() + " (" + curDurabilityPercent.ToStringPercent() + ")";
            }
            else
            {
                return null;
            }
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PostPreApplyDamage(ref dinfo, out absorbed);
            if (curDurability > 0 && dinfo.Def.harmsHealth && dinfo.Def.ExternalViolenceFor(parent))
            {
                curDurability -= dinfo.Amount;
                if (curDurability < 0)
                {
                    curDurability = 0;
                }
            }
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (durabilityProps.Repairable && !this.parent.HostileTo(selPawn))
            {
                var firstIngredientProvidedOrNotNeeded = true;
                var secondIngredientProvidedOrNotNeeded = true;

                List<Thing> ingredientsA = null;
                List<Thing> ingredientsB = null;

                // Only check for ingredients if there's any required
                if (!durabilityProps.RepairIngredients.NullOrEmpty())
                {
                    ingredientsA = Find.CurrentMap.listerThings.AllThings.FindAll(x => !x.IsForbidden(selPawn) && x.def == durabilityProps.RepairIngredients.First().thingDef && x.stackCount >= durabilityProps.RepairIngredients.First().count);
                    firstIngredientProvidedOrNotNeeded = ingredientsA.Any();

                    //The system supports only 2 ingredients at max
                    if (firstIngredientProvidedOrNotNeeded && durabilityProps.RepairIngredients.Count > 1)
                    {
                        ingredientsB = Find.CurrentMap.listerThings.AllThings.FindAll(x => !x.IsForbidden(selPawn) && x.def == durabilityProps.RepairIngredients.Last().thingDef && x.stackCount >= durabilityProps.RepairIngredients.Last().count);
                        secondIngredientProvidedOrNotNeeded = ingredientsB.Any();
                    }
                }

                if (curDurability < maxDurability + durabilityProps.MaxOverHeal && firstIngredientProvidedOrNotNeeded && secondIngredientProvidedOrNotNeeded)
                {
                    yield return new FloatMenuOption("CE_RepairArmorDurability".Translate(), delegate
                    {
                        Thing firstIngredient = null;
                        Thing secondIngredient = null;

                        // If ingredients are required, pick them. Otherwise just leave the variables as null.
                        if (!ingredientsA.NullOrEmpty())
                        {
                            firstIngredient = ingredientsA.MinBy(x => x.Position.DistanceTo(selPawn.Position));
                        }
                        if (!ingredientsB.NullOrEmpty())
                        {
                            secondIngredient = ingredientsB.MinBy(x => x.Position.DistanceTo(selPawn.Position));
                        }

                        StartJob(selPawn, firstIngredient, secondIngredient);
                    });
                }
                else if (this.curDurability >= maxDurability + durabilityProps.MaxOverHeal)
                {
                    yield return new FloatMenuOption("CE_ArmorDurability_CannotRepairUndamaged".Translate(), null);
                }
                else
                {
                    yield return new FloatMenuOption("CE_ArmorDurability_CannonRepairNoResource".Translate(), null);
                }
            }
        }

        [Compatibility.Multiplayer.SyncMethod]
        private void StartJob(Pawn selPawn, Thing firstIngredient = null, Thing secondIngredient = null)
        {
            var job = JobMaker.MakeJob(CE_JobDefOf.RepairNaturalArmor);
            job.targetA = this.parent;

            // Set the ingredients as targets if they were given
            if (firstIngredient != null)
            {
                job.targetB = firstIngredient;
            }

            if (secondIngredient != null)
            {
                job.targetC = secondIngredient;
            }

            selPawn.jobs.StartJob(job, JobCondition.InterruptForced);
        }
    }

    public class CompProperties_ArmorDurability : CompProperties
    {
        public CompProperties_ArmorDurability()
        {
            compClass = typeof(CompArmorDurability);
        }
        public float Durability;

        public bool Regenerates;

        public float RegenInterval;

        public float RegenValue;

        public bool Repairable;

        public List<ThingDefCountClass> RepairIngredients;

        public int RepairTime;

        public float RepairValue;

        public bool CanOverHeal;

        public float MaxOverHeal;

        public float MinArmorValueSharp = -1;

        public float MinArmorValueBlunt = -1;

        public float MinArmorValueHeat = -1;

        public float MinArmorValueElectric = -1;

        public float MinArmorPct = 0.25f;
    }

    public class JobDriver_RepairNaturalArmor : JobDriver
    {
        Pawn actor => GetActor();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            bool canReachTargetC = TargetC.Thing == null;
            var natArmor = (TargetA.Thing as ThingWithComps).TryGetComp<CompArmorDurability>();

            if (!canReachTargetC)
            {
                canReachTargetC = actor.CanReserveAndReach(TargetC, PathEndMode.ClosestTouch, Danger.Some, 1, natArmor.durabilityProps.RepairIngredients.Last().count);
            }

            bool canReachTargetsAB = actor.CanReserveAndReach(TargetA, PathEndMode.ClosestTouch, Danger.Some, 1, 1)
                                     && actor.CanReserveAndReach(TargetB, PathEndMode.ClosestTouch, Danger.Some, 1, natArmor.durabilityProps.RepairIngredients.First().count);
            return canReachTargetsAB
                   && (canReachTargetC)
                   ;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            var natArmor = (TargetA.Thing as ThingWithComps).TryGetComp<CompArmorDurability>();
            var targetB = TargetThingB;
            var targetC = TargetThingC;
            var countB = -1;
            var countC = -1;

            // Reserve ingredients if needed
            if (targetB != null)
            {
                countB = natArmor.durabilityProps.RepairIngredients.First().count;
                if (countB > 0)
                {
                    pawn.Reserve(TargetB, this.job, 1, countB);
                }
            }
            if (targetC != null)
            {
                countC = natArmor.durabilityProps.RepairIngredients.Last().count;
                if (countC > 0)
                {
                    pawn.Reserve(TargetC, this.job, 1, countC);
                }
            }

            if (targetB != null && countB > 0)
            {
                yield return Toils_Goto.Goto(TargetIndex.B, PathEndMode.ClosestTouch);
                yield return Toils_General.Do(
                                 delegate
                {
                    //left in code for explanation as to why it was replaced. This caused an error to apear every time it was called
                    //yield return Toils_Haul.TakeToInventory(TargetIndex.B, natArmor.durabilityProps.RepairIngredients.First().count);
                    if (countB <= targetB.stackCount)
                    {
                        var newthing = targetB.SplitOff(countB);
                        newthing.stackCount = countB;
                        pawn.inventory.TryAddItemNotForSale(newthing);
                    }
                    else
                    {
                        Log.Error("Ingredient stack count lower than needed. This shouldn't be possible to happen. Returning.");
                    }

                });
            }
            if (targetC != null && countC > 0)
            {
                yield return Toils_Goto.GotoCell(TargetC.Cell, PathEndMode.ClosestTouch);

                yield return Toils_General.Do(
                                 delegate
                {
                    //yield return Toils_Haul.TakeToInventory(TargetIndex.C, natArmor.durabilityProps.RepairIngredients.Last().count);
                    if (countC <= targetC.stackCount)
                    {
                        var newthing2 = targetC.SplitOff(countC);
                        newthing2.stackCount = countC;
                        pawn.inventory.TryAddItemNotForSale(newthing2);
                    }
                    else
                    {
                        Log.Error("Ingredient stack count lower than needed. This shouldn't be possible to happen. Returning.");
                    }
                });
            }
            var toil = Toils_Goto.Goto(TargetIndex.A, PathEndMode.ClosestTouch);

            //toil.AddPreInitAction(delegate { (TargetA.Thing as Pawn).jobs.StopAll(); });

            yield return toil;

            var toilWait = Toils_General.WaitWith(TargetIndex.A, natArmor.durabilityProps.RepairTime, true, true, true, face: TargetIndex.A);

            toilWait.AddFinishAction(
                delegate
            {
                var failed = false;
                Thing first = null;
                Thing second = null;

                // If ingredients are required, try to get them from inventory
                if (targetB != null && countB > 0)
                {
                    first = pawn.inventory.innerContainer.FirstOrDefault(x => x.def == targetB.def && x.stackCount >= countB);
                    if (first == null)
                    {
                        failed = true;
                    }
                }
                if (!failed && targetC != null && countC > 0)
                {
                    second = pawn.inventory.innerContainer.FirstOrDefault(x => x.def == targetC.def && x.stackCount >= countC);
                    if (second == null)
                    {
                        failed = true;
                    }
                }

                // Only finish the job if the pawn had the ingredients
                if (!failed)
                {
                    // Split off from the original stack (to avoid destroying the whole stack), and destroy what we took from it.
                    first?.SplitOff(countB).Destroy();
                    second?.SplitOff(countC).Destroy();

                    natArmor.curDurability += natArmor.durabilityProps.RepairValue;
                    if (natArmor.durabilityProps.CanOverHeal)
                    {
                        if (natArmor.curDurability > natArmor.durabilityProps.MaxOverHeal + natArmor.maxDurability)
                        {
                            natArmor.curDurability = natArmor.maxDurability + natArmor.durabilityProps.MaxOverHeal;
                        }
                        else
                        {
                            natArmor.curDurability += natArmor.durabilityProps.RepairValue;
                        }

                    }
                    else
                    {
                        if (natArmor.curDurability > natArmor.maxDurability)
                        {
                            natArmor.curDurability = natArmor.maxDurability;
                        }
                        else
                        {
                            natArmor.curDurability += natArmor.durabilityProps.RepairValue;
                        }
                    }
                }

            });

            yield return toilWait;

        }
    }
}
