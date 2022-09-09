using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using System.Xml;
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

        public CompFragments fragComp => new CompFragments() { props = frags };

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
                        frags = new CompProperties_Fragments() { fragments = new List<ThingDefCountClass>() };

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
        public MechArmorDurabilityExt durabilityProps => this.parent.def.GetModExtension<MechArmorDurabilityExt>();

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
                        curDurability += Math.Min(durabilityProps.RegenValue, maxDurability- curDurability);
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

        public override void PostExposeData()
        {
            Scribe_Values.Look<bool>(ref regens, "regens", false);
            base.PostExposeData();
        }

        public override void PostPreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            base.PostPreApplyDamage(dinfo, out absorbed);
            if (curDurability < 0)
            {
                curDurability = 0;
            }
            else
            {
                curDurability -= dinfo.Amount;
            }
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (durabilityProps.Repairable)
            {
                var ingredientsA = Find.CurrentMap.listerThings.AllThings.FindAll(x => !x.IsForbidden(selPawn) && x.def == durabilityProps.RepairIngredients.First().thingDef && x.stackCount >= durabilityProps.RepairIngredients.First().count);
                var ingredientsB = new List<Thing>();
                bool ingredientsBBool = false;

                //The system supports only 2 ingredients at max
                if (durabilityProps.RepairIngredients.Count > 1)
                {
                    ingredientsB = Find.CurrentMap.listerThings.AllThings.FindAll(x => !x.IsForbidden(selPawn) && x.def == durabilityProps.RepairIngredients.Last().thingDef && x.stackCount >= durabilityProps.RepairIngredients.Last().count);
                    ingredientsBBool = ingredientsB.Any();
                }


                if (!ingredientsA.NullOrEmpty() && curDurability < maxDurability + durabilityProps.MaxOverHeal)
                {
                    if (ingredientsBBool)
                    {
                        yield return new FloatMenuOption("Fix natural armor", delegate
                        {
                            selPawn.jobs.StartJob(new Job
                                (
                                CE_JobDefOf.RepairNaturalArmor,
                                this.parent,
                                ingredientsA.MinBy(x => x.Position.DistanceTo(selPawn.Position))
                                )
                                {
                                targetC = ingredientsB.MinBy(x => x.Position.DistanceTo(selPawn.Position)
                                )}
                                ,
                                JobCondition.InterruptForced
                                )
                                ;
                        });

                    }
                    else
                    {
                        yield return new FloatMenuOption("Fix natural armor", delegate
                        {
                            selPawn.jobs.StartJob(new Job
                                (
                                CE_JobDefOf.RepairNaturalArmor,
                                this.parent,
                                ingredientsA.MinBy(x => x.Position.DistanceTo(selPawn.Position))
                                ),
                                JobCondition.InterruptForced
                                )
                                ;
                        });
                    }
                }
                else if (this.curDurability >= maxDurability + durabilityProps.MaxOverHeal)
                {
                    yield return new FloatMenuOption("Can't repair natural armor, armor is undamaged", null);
                }
                else
                {
                    yield return new FloatMenuOption("Can't repair natural armor, no resources", null);
                }
            }
        }
    }

    public class MechArmorDurabilityExt : DefModExtension
    {
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
    }

    public class JobDriver_RepairNaturalArmor : JobDriver
    {
        Pawn actor => GetActor();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            bool canReachTargetC = TargetC.Thing == null;

            if (!canReachTargetC)
                canReachTargetC = actor.CanReserveAndReach(TargetC, PathEndMode.ClosestTouch, Danger.Some, 1, 1);

            bool canReachTargetsAB = actor.CanReserveAndReach(TargetA, PathEndMode.ClosestTouch, Danger.Some, 1, 1)
                && actor.CanReserveAndReach(TargetB, PathEndMode.ClosestTouch, Danger.Some, 1, 1);
            return canReachTargetsAB
                && (canReachTargetC)
                ;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            var natArmor = (TargetA.Thing as ThingWithComps).TryGetComp<CompArmorDurability>();

            pawn.Reserve(TargetB, this.job, 1, 1);

            pawn.Reserve(TargetC, this.job, 1 , 1);

            var TargetThingC = TargetC.Thing;
            yield return Toils_Goto.Goto(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_General.Do(
            delegate
            {
                //left in code for explanation as to why it was replaced. This caused an error to apear every time it was called
                //yield return Toils_Haul.TakeToInventory(TargetIndex.B, natArmor.durabilityProps.RepairIngredients.First().count);
                int ingrCount = natArmor.durabilityProps.RepairIngredients.First().count;
                if (ingrCount < TargetThingB.stackCount)
                {
                    TargetThingB.stackCount -= ingrCount;
                }
                else if (ingrCount == TargetThingB.stackCount)
                {
                    TargetThingB.Destroy();
                }
                else
                {
                    Log.Error("Ingredient stack count lower than needed. This shouldn't be possible to happen. Returning.");
                }
                var newthing = ThingMaker.MakeThing(natArmor.durabilityProps.RepairIngredients.First().thingDef);
                newthing.stackCount = ingrCount;
                pawn.inventory.TryAddItemNotForSale(newthing);

            });
            if (TargetThingC != null)
            {
                yield return Toils_Goto.GotoCell(TargetC.Cell, PathEndMode.ClosestTouch);

                yield return Toils_General.Do(
                delegate
                {
                    //yield return Toils_Haul.TakeToInventory(TargetIndex.C, natArmor.durabilityProps.RepairIngredients.Last().count);
                    int ingrCount2 = natArmor.durabilityProps.RepairIngredients.Last().count;
                    if (ingrCount2 < TargetThingC.stackCount)
                    {
                        TargetThingC.stackCount -= ingrCount2;
                    }
                    else if (ingrCount2 == TargetThingC.stackCount)
                    {
                        TargetThingC.Destroy();
                    }
                    else
                    {
                        Log.Error("Ingredient stack count lower than needed. This shouldn't be possible to happen. Returning.");
                    }
                    var newthing2 = ThingMaker.MakeThing(natArmor.durabilityProps.RepairIngredients.Last().thingDef);
                    newthing2.stackCount = ingrCount2;
                    pawn.inventory.TryAddItemNotForSale(newthing2);
                });
            }
            var toil = Toils_Goto.Goto(TargetIndex.A, PathEndMode.ClosestTouch);

            //toil.AddPreInitAction(delegate { (TargetA.Thing as Pawn).jobs.StopAll(); });

            yield return toil;

            var toilWait = Toils_General.Wait(natArmor.durabilityProps.RepairTime, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);

            toilWait.AddFinishAction(
                delegate
                {
                    pawn.inventory.innerContainer.Where(x => x.def == TargetThingB.def).First().Destroy();
                    if (TargetThingC != null)
                    {
                        pawn.inventory.innerContainer.Where(x => x.def == TargetThingC.def).First().Destroy();
                    }
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

                });

            yield return toilWait;

        }
    }
}
