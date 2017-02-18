using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CombatExtended.Detours
{
    internal class Detours_FloatMenuMakerMap
    {
        internal static bool CanTakeOrder(Pawn pawn)
        {
            return pawn.IsColonistPlayerControlled && pawn.jobs.CanTakeOrderedJob();
        }

        internal static List<FloatMenuOption> ChoicesAtFor(Vector3 clickPos, Pawn pawn)
        {
            IntVec3 intVec = IntVec3.FromVector3(clickPos);
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            if (!intVec.InBounds(pawn.Map) || !CanTakeOrder(pawn))
            {
                return list;
            }
            if (pawn.Map != Find.VisibleMap)
            {
                return list;
            }
            DangerUtility.NotifyDirectOrderingThisFrame(pawn);

            // ***** Beginning of drafted options *****
            FloatMenuMakerMap.making = true;
            try
            {
                if (pawn.Drafted)
                {
                    AddDraftedOrders(clickPos, pawn, list);
                }
                if (pawn.RaceProps.Humanlike)
                {
                    AddHumanlikeOrders(clickPos, pawn, list);
                }
                if (!pawn.Drafted)
                {
                    AddUndraftedOrders(clickPos, pawn, list);
                }
                foreach (FloatMenuOption current in pawn.GetExtraFloatMenuOptionsFor(intVec))
                {
                    list.Add(current);
                }
            }
            finally
            {
                DangerUtility.DoneDirectOrdering();
                FloatMenuMakerMap.making = false;
            }
            return list;
        }

        internal static void AddDraftedOrders(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            IntVec3 b = IntVec3.FromVector3(clickPos);
            foreach (LocalTargetInfo current in GenUI.TargetsAt(clickPos, TargetingParameters.ForAttackHostile(), true))
            {
                LocalTargetInfo attackTarg = current;
                if (pawn.equipment.Primary != null && !pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.MeleeRange)
                {
                    string str;
                    Action rangedAct = FloatMenuUtility.GetRangedAttackAction(pawn, attackTarg, out str);
                    string text = "FireAt".Translate(new object[]
                    {
                        attackTarg.Thing.LabelCap
                    });
                    FloatMenuOption floatMenuOption = new FloatMenuOption(MenuOptionPriority.High);
                    if (rangedAct == null)
                    {
                        text = text + " (" + str + ")";
                    }
					else
					{
						floatMenuOption.autoTakeable = true;
						floatMenuOption.action = delegate
						{
							MoteMaker.MakeStaticMote(attackTarg.Thing.DrawPos, attackTarg.Thing.Map, ThingDefOf.Mote_FeedbackAttack, 1f);
							rangedAct();
						};
					}
                    floatMenuOption.Label = text;
                    opts.Add(floatMenuOption);
                }
				string str2;
				Action meleeAct = FloatMenuUtility.GetMeleeAttackAction(pawn, attackTarg, out str2);
				Pawn pawn2 = attackTarg.Thing as Pawn;
				string text2;
				if (pawn2 != null && pawn2.Downed)
				{
					text2 = "MeleeAttackToDeath".Translate(new object[]
					{
						attackTarg.Thing.LabelCap
					});
				}
				else
				{
					text2 = "MeleeAttack".Translate(new object[]
					{
						attackTarg.Thing.LabelCap
					});
				}
                MenuOptionPriority priority = (!attackTarg.HasThing || !pawn.HostileTo(attackTarg.Thing)) ? MenuOptionPriority.VeryLow : MenuOptionPriority.AttackEnemy;
                Thing thing = attackTarg.Thing;
                FloatMenuOption floatMenuOption2 = new FloatMenuOption(string.Empty, null, priority, null, thing, 0f, null, null);
                if (meleeAct == null)
                {
                    text2 = text2 + " (" + str2 + ")";
                }
                else
                {
                    floatMenuOption2.action = delegate
                    {
                        MoteMaker.MakeStaticMote(attackTarg.Thing.DrawPos, attackTarg.Thing.Map, ThingDefOf.Mote_FeedbackAttack, 1f);
                        meleeAct();
                    };
                }
                floatMenuOption2.Label = text2;
                opts.Add(floatMenuOption2);
            }
            if (pawn.RaceProps.Humanlike && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                foreach (LocalTargetInfo current2 in GenUI.TargetsAt(clickPos, TargetingParameters.ForArrest(pawn), true))
                {
                    LocalTargetInfo dest = current2;
                    if (!((Pawn)dest.Thing).Downed)
                    {
                        if (!pawn.CanReach(dest, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                        {
                            opts.Add(new FloatMenuOption("CannotArrest".Translate() + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                        }
                        else if (!pawn.CanReserve(dest.Thing, 1))
                        {
                            opts.Add(new FloatMenuOption("CannotArrest".Translate() + ": " + "Reserved".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null));
                        }
                        else
                        {
                            Pawn pTarg = (Pawn)dest.Thing;
                            Action action = delegate
                            {
                                Building_Bed building_Bed = RestUtility.FindBedFor(pTarg, pawn, true, false, false);
                                if (building_Bed == null)
                                {
                                    Messages.Message("CannotArrest".Translate() + ": " + "NoPrisonerBed".Translate(), pTarg, MessageSound.RejectInput);
                                    return;
                                }
                                Job job = new Job(JobDefOf.Arrest, pTarg, building_Bed);
                                job.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job);
                                TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.ArrestingCreatesEnemies);
                            };
                            Thing thing = dest.Thing;
                            opts.Add(new FloatMenuOption("TryToArrest".Translate(new object[]
                            {
                                dest.Thing.LabelCap
                            }), action, MenuOptionPriority.High, null, thing, 0f, null, null));
                        }
                    }
                }
            }
            int num = GenRadial.NumCellsInRadius(2.9f);
            IntVec3 curLoc;
            for (int i = 0; i < num; i++)
            {
                curLoc = GenRadial.RadialPattern[i] + b;
                if (curLoc.Standable(pawn.Map))
                {
                    if (curLoc != pawn.Position)
                    {
                        if (!pawn.CanReach(curLoc, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                        {
                            FloatMenuOption item = new FloatMenuOption("CannotGoNoPath".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
                            opts.Add(item);
                        }
                        else
                        {
                            Action action2 = delegate
                            {
                                IntVec3 intVec = RCellFinder.BestOrderedGotoDestNear(curLoc, pawn);
                                Job job = new Job(JobDefOf.Goto, intVec);
                                if (pawn.Map.exitMapGrid.IsExitCell(UI.MouseCell()))
                                {
                                    job.exitMapOnArrival = true;
                                }
                                if (pawn.jobs.TryTakeOrderedJob(job))
                                {
                                    MoteMaker.MakeStaticMote(intVec, pawn.Map, ThingDefOf.Mote_FeedbackGoto, 1f);
                                }
                            };
                            opts.Add(new FloatMenuOption("GoHere".Translate(), action2, MenuOptionPriority.GoHere, null, null, 0f, null, null)
                            {
                                autoTakeable = true
                            });
                        }
                    }
                    break;
                }
            }
        }

        internal static void AddHumanlikeOrders(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);
            foreach (Thing current in c.GetThingList(pawn.Map))
            {
                Thing t = current;
                if (t.def.ingestible != null && pawn.RaceProps.CanEverEat(t) && t.IngestibleNow)
                {
                    string text;
                    if (t.def.ingestible.ingestCommandString.NullOrEmpty())
                    {
                        text = "ConsumeThing".Translate(new object[]
                        {
                            t.LabelShort
                        });
                    }
                    else
                    {
                        text = string.Format(t.def.ingestible.ingestCommandString, t.LabelShort);
                    }
                    FloatMenuOption item5;
                    if (t.def.IsPleasureDrug && pawn.IsTeetotaler())
                    {
                        item5 = new FloatMenuOption(text + " (" + TraitDefOf.DrugDesire.DataAtDegree(-1).label + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                    }
                    else if (!pawn.CanReach(t, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                    {
                        item5 = new FloatMenuOption(text + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                    }
                    else if (!pawn.CanReserve(t, 1))
                    {
                        item5 = new FloatMenuOption(text + " (" + "ReservedBy".Translate(new object[]
                        {
                            pawn.Map.reservationManager.FirstReserverOf(t, pawn.Faction, true).LabelShort
                        }) + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                    }
                    else
                    {
                        MenuOptionPriority priority = (!(t is Corpse)) ? MenuOptionPriority.Default : MenuOptionPriority.Low;
                        item5 = new FloatMenuOption(text, delegate
                        {
                            t.SetForbidden(false, true);
                            Job job = new Job(JobDefOf.Ingest, t);
                            job.count = FoodUtility.WillIngestStackCountOf(pawn, t.def);
                            pawn.jobs.TryTakeOrderedJob(job);
                        }, priority, null, null, 0f, null, null);
                    }
                    opts.Add(item5);
                }
            }
            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                foreach (LocalTargetInfo current2 in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), true))
                {
                    Pawn victim = (Pawn)current2.Thing;
                    if (!victim.InBed() && pawn.CanReserveAndReach(victim, PathEndMode.OnCell, Danger.Deadly, 1))
                    {
                        if ((victim.Faction == Faction.OfPlayer && victim.MentalStateDef == null) || (victim.Faction != Faction.OfPlayer && victim.MentalStateDef == null && !victim.IsPrisonerOfColony && (victim.Faction == null || !victim.Faction.HostileTo(Faction.OfPlayer))))
                        {
                            Pawn victim2 = victim;
                            opts.Add(new FloatMenuOption("Rescue".Translate(new object[]
                            {
                                victim.LabelCap
                            }), delegate
                            {
                                Building_Bed building_Bed = RestUtility.FindBedFor(victim, pawn, false, false, false);
                                if (building_Bed == null)
                                {
                                    string str2;
                                    if (victim.RaceProps.Animal)
                                    {
                                        str2 = "NoAnimalBed".Translate();
                                    }
                                    else
                                    {
                                        str2 = "NoNonPrisonerBed".Translate();
                                    }
                                    Messages.Message("CannotRescue".Translate() + ": " + str2, victim, MessageSound.RejectInput);
                                    return;
                                }
                                Job job = new Job(JobDefOf.Rescue, victim, building_Bed);
                                job.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job);
                                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Rescuing, KnowledgeAmount.Total);
                            }, MenuOptionPriority.RescueOrCapture, null, victim2, 0f, null, null));
                        }
                        if (victim.RaceProps.Humanlike && (victim.MentalStateDef != null || victim.Faction != Faction.OfPlayer || (victim.Downed && victim.guilt.IsGuilty)))
                        {
                            Pawn victim2 = victim;
                            opts.Add(new FloatMenuOption("Capture".Translate(new object[]
                            {
                                victim.LabelCap
                            }), delegate
                            {
                                Building_Bed building_Bed = RestUtility.FindBedFor(victim, pawn, true, false, false);
                                if (building_Bed == null)
                                {
                                    Messages.Message("CannotCapture".Translate() + ": " + "NoPrisonerBed".Translate(), victim, MessageSound.RejectInput);
                                    return;
                                }
                                Job job = new Job(JobDefOf.Capture, victim, building_Bed);
                                job.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job);
                                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.Capturing, KnowledgeAmount.Total);
                            }, MenuOptionPriority.RescueOrCapture, null, victim2, 0f, null, null));
                        }
                    }
                }
                foreach (LocalTargetInfo current3 in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), true))
                {
                    LocalTargetInfo LocalTargetInfo = current3;
                    Pawn victim = (Pawn)LocalTargetInfo.Thing;
                    if (victim.Downed && pawn.CanReserveAndReach(victim, PathEndMode.OnCell, Danger.Deadly, 1) && Building_CryptosleepCasket.FindCryptosleepCasketFor(victim, pawn) != null)
                    {
                        string label = "CarryToCryptosleepCasket".Translate(new object[]
                        {
                            LocalTargetInfo.Thing.LabelCap
                        });
                        JobDef jDef = JobDefOf.CarryToCryptosleepCasket;
                        Action action = delegate
                        {
                            Building_CryptosleepCasket building_CryptosleepCasket = Building_CryptosleepCasket.FindCryptosleepCasketFor(victim, pawn);
                            if (building_CryptosleepCasket == null)
                            {
                                Messages.Message("CannotCarryToCryptosleepCasket".Translate() + ": " + "NoCryptosleepCasket".Translate(), victim, MessageSound.RejectInput);
                                return;
                            }
                            Job job = new Job(jDef, victim, building_CryptosleepCasket);
                            job.count = 1;
                            pawn.jobs.TryTakeOrderedJob(job);
                        };
                        Pawn victim2 = victim;
                        opts.Add(new FloatMenuOption(label, action, MenuOptionPriority.Default, null, victim2, 0f, null, null));
                    }
                }
            }
            foreach (LocalTargetInfo current4 in GenUI.TargetsAt(clickPos, TargetingParameters.ForStrip(pawn), true))
            {
                LocalTargetInfo stripTarg = current4;
                FloatMenuOption item2;
                if (!pawn.CanReach(stripTarg, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                {
                    item2 = new FloatMenuOption("CannotStrip".Translate(new object[]
                    {
                        stripTarg.Thing.LabelCap
                    }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                }
                else if (!pawn.CanReserveAndReach(stripTarg, PathEndMode.ClosestTouch, Danger.Deadly, 1))
                {
                    item2 = new FloatMenuOption("CannotStrip".Translate(new object[]
                    {
                        stripTarg.Thing.LabelCap
                    }) + " (" + "ReservedBy".Translate(new object[]
                    {
                        pawn.Map.reservationManager.FirstReserverOf(stripTarg, pawn.Faction, true).LabelShort
                    }) + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                }
                else
                {
                    item2 = new FloatMenuOption("Strip".Translate(new object[]
                    {
                        stripTarg.Thing.LabelCap
                    }), delegate
                    {
                        stripTarg.Thing.SetForbidden(false, false);
                        pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.Strip, stripTarg));
                    }, MenuOptionPriority.Default, null, null, 0f, null, null);
                }
                opts.Add(item2);
            }


            CompInventory compInventory = pawn.TryGetComp<CompInventory>();      // Need compInventory here for equip and wear options
            if (pawn.equipment != null)
            {
				ThingWithComps equipment = null;
				List<Thing> thingList = c.GetThingList(pawn.Map);
				for (int i = 0; i < thingList.Count; i++)
				{
					if (thingList[i].TryGetComp<CompEquippable>() != null)
					{
						equipment = (ThingWithComps)thingList[i];
						break;
					}
				}
                if (equipment != null)
                {
					string labelShort = equipment.LabelShort;
					FloatMenuOption item3;
                    if (equipment.def.IsWeapon && pawn.story.WorkTagIsDisabled(WorkTags.Violent))
                    {
                        item3 = new FloatMenuOption("CannotEquip".Translate(new object[]
                        {
                            labelShort
                        }) + " (" + "IsIncapableOfViolenceLower".Translate(new object[]
                        {
                            pawn.LabelShort
                        }) + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                    }
                    else if (!pawn.CanReach(equipment, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                    {
                        item3 = new FloatMenuOption("CannotEquip".Translate(new object[]
                        {
                            labelShort
                        }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                    }
                    else if (!pawn.CanReserve(equipment, 1))
                    {
                        item3 = new FloatMenuOption("CannotEquip".Translate(new object[]
                        {
                            labelShort
                        }) + " (" + "ReservedBy".Translate(new object[]
                        {
                            pawn.Map.reservationManager.FirstReserverOf(equipment, pawn.Faction, true).LabelShort
                        }) + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                    }
                    else if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                    {
                        item3 = new FloatMenuOption("CannotEquip".Translate(new object[]
                        {
                            labelShort
                        }) + " (" + "Incapable".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                    }
                    else
                    {
                        string text2 = "Equip".Translate(new object[]
                        {
                            labelShort
                        });
                        if (equipment.def.IsRangedWeapon && pawn.story != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler))
                        {
                            text2 = text2 + " " + "EquipWarningBrawler".Translate();
                        }
                        item3 = new FloatMenuOption(text2, delegate
                        {
                            equipment.SetForbidden(false, true);
                            pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.Equip, equipment));
                            MoteMaker.MakeStaticMote(equipment.DrawPos, equipment.Map, ThingDefOf.Mote_FeedbackEquip, 1f);
                            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
                        }, MenuOptionPriority.High, null, null, 0f, null, null);
                    }
                    opts.Add(item3);
                }
            }
            if (pawn.apparel != null)
            {
                Apparel apparel = pawn.Map.thingGrid.ThingAt<Apparel>(c);
                if (apparel != null)
                {
                    FloatMenuOption item4;
                    if (!pawn.CanReach(apparel, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                    {
                        item4 = new FloatMenuOption("CannotWear".Translate(new object[]
                        {
                            apparel.Label
                        }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                    }
                    else if (!pawn.CanReserve(apparel, 1))
                    {
                        Pawn pawn2 = pawn.Map.reservationManager.FirstReserverOf(apparel, pawn.Faction, true);
                        item4 = new FloatMenuOption("CannotWear".Translate(new object[]
                        {
                            apparel.Label
                        }) + " (" + "ReservedBy".Translate(new object[]
                        {
                            pawn2.LabelShort
                        }) + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                    }
                    else if (!ApparelUtility.HasPartsToWear(pawn, apparel.def))
                    {
                        item4 = new FloatMenuOption("CannotWear".Translate(new object[]
                        {
                            apparel.Label
                        }) + " (" + "CannotWearBecauseOfMissingBodyParts".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
                    }
                    else
                    {
                        // CE PART!!!
                        // Added check for inventory capacity
                        int count;
                        if (compInventory != null && !compInventory.CanFitInInventory(apparel, out count, false, true))
                        {
                            item4 = new FloatMenuOption("CannotWear".Translate(new object[] { apparel.Label }) + " (" + "CE_InventoryFull".Translate() + ")", null);
                        }
                        else
                        {
                            // CE END: PART!!!

                            item4 = new FloatMenuOption("ForceWear".Translate(new object[]
                            {
                            apparel.LabelShort
                            }), delegate
                            {
                                apparel.SetForbidden(false, true);
                                Job job = new Job(JobDefOf.Wear, apparel);
                                pawn.jobs.TryTakeOrderedJob(job);
                            }, MenuOptionPriority.High, null, null, 0f, null, null);
                        }
                    }
                    opts.Add(item4);
                }
            }

            // CE PART
            // *** NEW: Pick up option ***
            if (compInventory != null)
            {
                List<Thing> thingList = c.GetThingList(pawn.Map);
                if (!thingList.NullOrEmpty<Thing>())
                {
                    Thing item = thingList.FirstOrDefault(thing => thing.def.alwaysHaulable && !(thing is Corpse));
                    if (item != null)
                    {
                        FloatMenuOption pickUpOption;
                        int count = 0;
                        if (!pawn.CanReach(item, PathEndMode.Touch, Danger.Deadly))
                        {
                            pickUpOption = new FloatMenuOption("CE_CannotPickUp".Translate() + " " + item.LabelShort + " (" + "NoPath".Translate() + ")", null);
                        }
                        else if (!pawn.CanReserve(item))
                        {
                            pickUpOption = new FloatMenuOption("CE_CannotPickUp".Translate() + " " + item.LabelShort + " (" + "ReservedBy".Translate(new object[] { pawn.Map.reservationManager.FirstReserverOf(item, pawn.Faction) }), null);
                        }
                        else if (!compInventory.CanFitInInventory(item, out count))
                        {
                            pickUpOption = new FloatMenuOption("CE_CannotPickUp".Translate() + " " + item.LabelShort + " (" + "CE_InventoryFull".Translate() + ")", null);
                        }
                        else
                        {
                            // Pick up one
                            pickUpOption = new FloatMenuOption("CE_PickUp".Translate() + " " + item.LabelShort,
                                new Action(delegate
                                {
                                    item.SetForbidden(false, false);
                                    Job job = new Job(JobDefOf.TakeInventory, item);
                                    job.count = 1;
                                    job.playerForced = true;
                                    pawn.jobs.TryTakeOrderedJob(job);
                                }));
                        }
                        opts.Add(pickUpOption);
                        if (count > 1 && item.stackCount > 1)
                        {
                            // Pick up half
                            int countHalf = count / 2;
                            if (count > 3)
                            {
                                FloatMenuOption pickUpHalfStackOption = new FloatMenuOption("CE_PickUpHalf".Translate() + " " + item.LabelShort + " x" + countHalf.ToString(),
                                   new Action(delegate
                                   {
                                       item.SetForbidden(false, false);
                                       Job job = new Job(JobDefOf.TakeInventory, item);
                                       job.count = countHalf;
                                       job.playerForced = true;
                                       pawn.jobs.TryTakeOrderedJob(job);
                                   }));
                                opts.Add(pickUpHalfStackOption);
                            }

                            // Pick up all
                            FloatMenuOption pickUpStackOption = new FloatMenuOption("CE_PickUp".Translate() + " " + item.LabelShort + " x" + count.ToString(),
                                new Action(delegate
                                {
                                    item.SetForbidden(false, false);
                                    Job job = new Job(JobDefOf.TakeInventory, item);
                                    job.count = count;
                                    job.playerForced = true;
                                    pawn.jobs.TryTakeOrderedJob(job);
                                }));
                            opts.Add(pickUpStackOption);
                        }
                    }
                }
            }
            // CE PART END

            if (!pawn.Map.IsPlayerHome)
            {
                Thing item = c.GetFirstItem(pawn.Map);
                if (item != null && item.def.EverHaulable)
                {
                    if (!pawn.CanReach(item, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                    {
                        opts.Add(new FloatMenuOption("CannotPickUp".Translate(new object[]
                        {
                            item.Label
                        }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                    }
                    else if (!pawn.CanReserve(item, 1))
                    {
                        Pawn pawn3 = pawn.Map.reservationManager.FirstReserverOf(item, pawn.Faction, true);
                        opts.Add(new FloatMenuOption("CannotPickUp".Translate(new object[]
                        {
                            item.Label
                        }) + " (" + "ReservedBy".Translate(new object[]
                        {
                            pawn3.LabelShort
                        }) + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                    }
                    else if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, item, 1))
                    {
                        opts.Add(new FloatMenuOption("CannotPickUp".Translate(new object[]
                        {
                            item.Label
                        }) + " (" + "TooHeavy".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                    }
                    else if (item.stackCount == 1)
                    {
                        opts.Add(new FloatMenuOption("PickUp".Translate(new object[]
                        {
                            item.Label
                        }), delegate
                        {
                            item.SetForbidden(false, false);
                            Job job = new Job(JobDefOf.TakeInventory, item);
                            job.count = 1;
                            pawn.jobs.TryTakeOrderedJob(job);
                        }, MenuOptionPriority.High, null, null, 0f, null, null));
                    }
                    else
                    {
                        if (MassUtility.WillBeOverEncumberedAfterPickingUp(pawn, item, item.stackCount))
                        {
                            opts.Add(new FloatMenuOption("CannotPickUpAll".Translate(new object[]
                            {
                                item.Label
                            }) + " (" + "TooHeavy".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                        }
                        else
                        {
                            opts.Add(new FloatMenuOption("PickUpAll".Translate(new object[]
                            {
                                item.Label
                            }), delegate
                            {
                                item.SetForbidden(false, false);
                                Job job = new Job(JobDefOf.TakeInventory, item);
                                job.count = item.stackCount;
                                pawn.jobs.TryTakeOrderedJob(job);
                            }, MenuOptionPriority.High, null, null, 0f, null, null));
                        }
                        opts.Add(new FloatMenuOption("PickUpSome".Translate(new object[]
                        {
                            item.Label
                        }), delegate
                        {
                            int to = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(pawn, item), item.stackCount);
                            Dialog_Slider window = new Dialog_Slider("PickUpCount".Translate(new object[]
                            {
                                item.LabelShort
                            }), 1, to, delegate (int count)
                            {
                                item.SetForbidden(false, false);
                                Job job = new Job(JobDefOf.TakeInventory, item);
                                job.count = count;
                                pawn.jobs.TryTakeOrderedJob(job);
                            }, -2147483648);
                            Find.WindowStack.Add(window);
                        }, MenuOptionPriority.High, null, null, 0f, null, null));
                    }
                }
            }
            if (!pawn.Map.IsPlayerHome)
            {
                Thing item = c.GetFirstItem(pawn.Map);
                if (item != null && item.def.EverHaulable)
                {
                    Pawn bestPackAnimal = GiveToPackAnimalUtility.PackAnimalWithTheMostFreeSpace(pawn.Map, pawn.Faction);
                    if (bestPackAnimal != null)
                    {
                        if (!pawn.CanReach(item, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                        {
                            opts.Add(new FloatMenuOption("CannotGiveToPackAnimal".Translate(new object[]
                            {
                                item.Label
                            }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                        }
                        else if (!pawn.CanReserve(item, 1))
                        {
                            Pawn pawn4 = pawn.Map.reservationManager.FirstReserverOf(item, pawn.Faction, true);
                            opts.Add(new FloatMenuOption("CannotGiveToPackAnimal".Translate(new object[]
                            {
                                item.Label
                            }) + " (" + "ReservedBy".Translate(new object[]
                            {
                                pawn4.LabelShort
                            }) + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                        }
                        else if (MassUtility.WillBeOverEncumberedAfterPickingUp(bestPackAnimal, item, 1))
                        {
                            opts.Add(new FloatMenuOption("CannotGiveToPackAnimal".Translate(new object[]
                            {
                                item.Label
                            }) + " (" + "TooHeavy".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                        }
                        else if (item.stackCount == 1)
                        {
                            opts.Add(new FloatMenuOption("GiveToPackAnimal".Translate(new object[]
                            {
                                item.Label
                            }), delegate
                            {
                                item.SetForbidden(false, false);
                                Job job = new Job(JobDefOf.GiveToPackAnimal, item);
                                job.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job);
                            }, MenuOptionPriority.High, null, null, 0f, null, null));
                        }
                        else
                        {
                            if (MassUtility.WillBeOverEncumberedAfterPickingUp(bestPackAnimal, item, item.stackCount))
                            {
                                opts.Add(new FloatMenuOption("CannotGiveToPackAnimalAll".Translate(new object[]
                                {
                                    item.Label
                                }) + " (" + "TooHeavy".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else
                            {
                                opts.Add(new FloatMenuOption("GiveToPackAnimalAll".Translate(new object[]
                                {
                                    item.Label
                                }), delegate
                                {
                                    item.SetForbidden(false, false);
                                    Job job = new Job(JobDefOf.GiveToPackAnimal, item);
                                    job.count = item.stackCount;
                                    pawn.jobs.TryTakeOrderedJob(job);
                                }, MenuOptionPriority.High, null, null, 0f, null, null));
                            }
                            opts.Add(new FloatMenuOption("GiveToPackAnimalSome".Translate(new object[]
                            {
                                item.Label
                            }), delegate
                            {
                                int to = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(bestPackAnimal, item), item.stackCount);
                                Dialog_Slider window = new Dialog_Slider("GiveToPackAnimalCount".Translate(new object[]
                                {
                                    item.LabelShort
                                }), 1, to, delegate (int count)
                                {
                                    item.SetForbidden(false, false);
                                    Job job = new Job(JobDefOf.GiveToPackAnimal, item);
                                    job.count = count;
                                    pawn.jobs.TryTakeOrderedJob(job);
                                }, -2147483648);
                                Find.WindowStack.Add(window);
                            }, MenuOptionPriority.High, null, null, 0f, null, null));
                        }
                    }
                }
            }
            if (!pawn.Map.IsPlayerHome)
            {
                foreach (LocalTargetInfo current5 in GenUI.TargetsAt(clickPos, TargetingParameters.ForRescue(pawn), true))
                {
                    Pawn p = (Pawn)current5.Thing;
                    if (p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer)
                    {
                        if (!pawn.CanReach(p, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn))
                        {
                            opts.Add(new FloatMenuOption("CannotCarryToExit".Translate(new object[]
                            {
                                p.Label
                            }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                        }
                        else if (!pawn.CanReserve(p, 1))
                        {
                            Pawn pawn5 = pawn.Map.reservationManager.FirstReserverOf(p, pawn.Faction, true);
                            opts.Add(new FloatMenuOption("CannotCarryToExit".Translate(new object[]
                            {
                                p.Label
                            }) + " (" + "ReservedBy".Translate(new object[]
                            {
                                pawn5.LabelShort
                            }) + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                        }
                        else
                        {
                            IntVec3 exitSpot;
                            if (!RCellFinder.TryFindBestExitSpot(pawn, out exitSpot, TraverseMode.ByPawn))
                            {
                                opts.Add(new FloatMenuOption("CannotCarryToExit".Translate(new object[]
                                {
                                    p.Label
                                }) + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                            }
                            else
                            {
                                opts.Add(new FloatMenuOption("CarryToExit".Translate(new object[]
                                {
                                    p.Label
                                }), delegate
                                {
                                    Job job = new Job(JobDefOf.CarryDownedPawnToExit, p, exitSpot);
                                    job.count = 1;
                                    pawn.jobs.TryTakeOrderedJob(job);
                                }, MenuOptionPriority.High, null, null, 0f, null, null));
                            }
                        }
                    }
                }
            }
            if (pawn.equipment != null && pawn.equipment.Primary != null && GenUI.TargetsAt(clickPos, TargetingParameters.ForSelf(pawn), true).Any<LocalTargetInfo>())
            {
                Action action2 = delegate
                {
                    pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.DropEquipment, pawn.equipment.Primary));
                };
                opts.Add(new FloatMenuOption("Drop".Translate(new object[]
                {
                    pawn.equipment.Primary.Label
                }), action2, MenuOptionPriority.Default, null, null, 0f, null, null));
            }
            foreach (LocalTargetInfo current6 in GenUI.TargetsAt(clickPos, TargetingParameters.ForTrade(), true))
            {
                LocalTargetInfo dest = current6;
                if (!pawn.CanReach(dest, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                {
                    opts.Add(new FloatMenuOption("CannotTrade".Translate() + " (" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                else if (!pawn.CanReserve(dest.Thing, 1))
                {
                    opts.Add(new FloatMenuOption("CannotTrade".Translate() + " (" + "Reserved".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                else
                {
                    Pawn pTarg = (Pawn)dest.Thing;
                    Action action3 = delegate
                    {
                        Job job = new Job(JobDefOf.TradeWithPawn, pTarg);
                        job.playerForced = true;
                        pawn.jobs.TryTakeOrderedJob(job);
                        PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.InteractingWithTraders, KnowledgeAmount.Total);
                    };
                    string str = string.Empty;
                    if (pTarg.Faction != null)
                    {
                        str = " (" + pTarg.Faction.Name + ")";
                    }
                    Thing thing = dest.Thing;
                    opts.Add(new FloatMenuOption("TradeWith".Translate(new object[]
                    {
                        pTarg.LabelShort + ", " + pTarg.TraderKind.label
                    }) + str, action3, MenuOptionPriority.InitiateSocial, null, thing, 0f, null, null));
                }
            }
            foreach (Thing current7 in pawn.Map.thingGrid.ThingsAt(c))
            {
                foreach (FloatMenuOption current8 in current7.GetFloatMenuOptions(pawn))
                {
                    opts.Add(current8);
                }
            }
        }


        internal static void AddUndraftedOrders(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            IntVec3 clickCell = IntVec3.FromVector3(clickPos);
            bool flag = false;
            bool flag2 = false;
            foreach (Thing current in pawn.Map.thingGrid.ThingsAt(clickCell))
            {
                flag2 = true;
                if (pawn.CanReach(current, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
                {
                    flag = true;
                    break;
                }
            }
            if (flag2 && !flag)
            {
                opts.Add(new FloatMenuOption("(" + "NoPath".Translate() + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null));
                return;
            }
            JobGiver_Work jobGiver_Work = pawn.thinker.TryGetMainTreeThinkNode<JobGiver_Work>();
            if (jobGiver_Work != null)
            {
                foreach (Thing current2 in pawn.Map.thingGrid.ThingsAt(clickCell))
                {
                    Pawn pawn2 = pawn.Map.reservationManager.FirstReserverOf(current2, pawn.Faction, true);
                    if (pawn2 != null && pawn2 != pawn)
                    {
                        opts.Add(new FloatMenuOption("IsReservedBy".Translate(new object[]
                        {
                            current2.LabelShort.CapitalizeFirst(),
                            pawn2.LabelShort
                        }), null, MenuOptionPriority.Default, null, null, 0f, null, null));
                    }
                    else
                    {
                        foreach (WorkTypeDef current3 in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                        {
                            for (int i = 0; i < current3.workGiversByPriority.Count; i++)
                            {
                                WorkGiver_Scanner workGiver_Scanner = current3.workGiversByPriority[i].Worker as WorkGiver_Scanner;
                                if (workGiver_Scanner != null && workGiver_Scanner.def.directOrderable && !workGiver_Scanner.ShouldSkip(pawn))
                                {
                                    JobFailReason.Clear();
                                    if (workGiver_Scanner.PotentialWorkThingRequest.Accepts(current2) || (workGiver_Scanner.PotentialWorkThingsGlobal(pawn) != null && workGiver_Scanner.PotentialWorkThingsGlobal(pawn).Contains(current2)))
                                    {
                                        Job job;
                                        if (!workGiver_Scanner.HasJobOnThingForced(pawn, current2))
                                        {
                                            job = null;
                                        }
                                        else
                                        {
                                            job = workGiver_Scanner.JobOnThingForced(pawn, current2);
                                        }
                                        if (job == null)
                                        {
                                            if (JobFailReason.HaveReason)
                                            {
                                                string label3 = "CannotGenericWork".Translate(new object[]
                                                {
                                                    workGiver_Scanner.def.verb,
                                                    current2.LabelShort
                                                }) + " (" + JobFailReason.Reason + ")";
                                                opts.Add(new FloatMenuOption(label3, null, MenuOptionPriority.Default, null, null, 0f, null, null));
                                            }
                                        }
                                        else
                                        {
                                            WorkTypeDef workType = workGiver_Scanner.def.workType;
                                            Action action = null;
                                            PawnCapacityDef pawnCapacityDef = workGiver_Scanner.MissingRequiredCapacity(pawn);
                                            string label;
                                            if (pawnCapacityDef != null)
                                            {
                                                label = "CannotMissingHealthActivities".Translate(new object[]
                                                {
                                                    pawnCapacityDef.label
                                                });
                                            }
                                            else if (pawn.jobs.curJob != null && pawn.jobs.curJob.JobIsSameAs(job))
                                            {
                                                label = "CannotGenericAlreadyAm".Translate(new object[]
                                                {
                                                    workType.gerundLabel,
                                                    current2.LabelShort
                                                });
                                            }
                                            else if (pawn.workSettings.GetPriority(workType) == 0)
                                            {
                                                if (pawn.story.WorkTypeIsDisabled(workType))
                                                {
                                                    label = "CannotPrioritizeWorkTypeDisabled".Translate(new object[]
                                                    {
                                                        workType.gerundLabel
                                                    });
                                                }
                                                else if ("CannotPrioritizeNotAssignedToWorkType".CanTranslate())
                                                {
                                                    label = "CannotPrioritizeNotAssignedToWorkType".Translate(new object[]
                                                    {
                                                        workType.gerundLabel
                                                    });
                                                }
                                                else
                                                {
                                                    label = "CannotPrioritizeIsNotA".Translate(new object[]
                                                    {
                                                        pawn.NameStringShort,
                                                        workType.pawnLabel
                                                    });
                                                }
                                            }
                                            else if (job.def == JobDefOf.Research && current2 is Building_ResearchBench)
                                            {
                                                label = "CannotPrioritizeResearch".Translate();
                                            }
                                            else if (current2.IsForbidden(pawn))
                                            {
                                                if (!current2.Position.InAllowedArea(pawn))
                                                {
                                                    label = "CannotPrioritizeForbiddenOutsideAllowedArea".Translate(new object[]
                                                    {
                                                        current2.Label
                                                    });
                                                }
                                                else
                                                {
                                                    label = "CannotPrioritizeForbidden".Translate(new object[]
                                                    {
                                                        current2.Label
                                                    });
                                                }
                                            }
                                            else if (!pawn.CanReach(current2, workGiver_Scanner.PathEndMode, Danger.Deadly, false, TraverseMode.ByPawn))
                                            {
                                                label = current2.Label + ": " + "NoPath".Translate();
                                            }
                                            else
                                            {
                                                label = "PrioritizeGeneric".Translate(new object[]
                                                {
                                                    workGiver_Scanner.def.gerund,
                                                    current2.Label
                                                });
                                                Job localJob = job;
                                                WorkGiver_Scanner localScanner = workGiver_Scanner;
                                                action = delegate
                                                {
                                                    pawn.jobs.TryTakeOrderedJobPrioritizedWork(localJob, localScanner, clickCell);
                                                };
                                            }
                                            if (!opts.Any((FloatMenuOption op) => op.Label == label.TrimEnd(new char[0])))
                                            {
                                                opts.Add(new FloatMenuOption(label, action, MenuOptionPriority.Default, null, null, 0f, null, null));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                Pawn pawn3 = pawn.Map.reservationManager.FirstReserverOf(clickCell, pawn.Faction, true);
                if (pawn3 != null && pawn3 != pawn)
                {
                    opts.Add(new FloatMenuOption("IsReservedBy".Translate(new object[]
                    {
                        "AreaLower".Translate(),
                        pawn3.LabelShort
                    }).CapitalizeFirst(), null, MenuOptionPriority.Default, null, null, 0f, null, null));
                }
                else
                {
                    foreach (WorkTypeDef current4 in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                    {
                        for (int j = 0; j < current4.workGiversByPriority.Count; j++)
                        {
                            WorkGiver_Scanner workGiver_Scanner2 = current4.workGiversByPriority[j].Worker as WorkGiver_Scanner;
                            if (workGiver_Scanner2 != null && workGiver_Scanner2.def.directOrderable && !workGiver_Scanner2.ShouldSkip(pawn))
                            {
                                JobFailReason.Clear();
                                if (workGiver_Scanner2.PotentialWorkCellsGlobal(pawn).Contains(clickCell))
                                {
                                    Job job2;
                                    if (!workGiver_Scanner2.HasJobOnCell(pawn, clickCell))
                                    {
                                        job2 = null;
                                    }
                                    else
                                    {
                                        job2 = workGiver_Scanner2.JobOnCell(pawn, clickCell);
                                    }
                                    if (job2 == null)
                                    {
                                        if (JobFailReason.HaveReason)
                                        {
                                            string label2 = "CannotGenericWork".Translate(new object[]
                                            {
                                                workGiver_Scanner2.def.verb,
                                                "AreaLower".Translate()
                                            }) + " (" + JobFailReason.Reason + ")";
                                            opts.Add(new FloatMenuOption(label2, null, MenuOptionPriority.Default, null, null, 0f, null, null));
                                        }
                                    }
                                    else
                                    {
                                        WorkTypeDef workType2 = workGiver_Scanner2.def.workType;
                                        Action action2 = null;
                                        PawnCapacityDef pawnCapacityDef2 = workGiver_Scanner2.MissingRequiredCapacity(pawn);
                                        string label;
                                        if (pawnCapacityDef2 != null)
                                        {
                                            label = "CannotMissingHealthActivities".Translate(new object[]
                                            {
                                                pawnCapacityDef2.label
                                            });
                                        }
                                        else if (pawn.jobs.curJob != null && pawn.jobs.curJob.JobIsSameAs(job2))
                                        {
                                            label = "CannotGenericAlreadyAm".Translate(new object[]
                                            {
                                                workType2.gerundLabel,
                                                "AreaLower".Translate()
                                            });
                                        }
                                        else if (pawn.workSettings.GetPriority(workType2) == 0)
                                        {
                                            if (pawn.story.WorkTypeIsDisabled(workType2))
                                            {
                                                label = "CannotPrioritizeWorkTypeDisabled".Translate(new object[]
                                                {
                                                    workType2.gerundLabel
                                                });
                                            }
                                            else if ("CannotPrioritizeNotAssignedToWorkType".CanTranslate())
                                            {
                                                label = "CannotPrioritizeNotAssignedToWorkType".Translate(new object[]
                                                {
                                                    workType2.gerundLabel
                                                });
                                            }
                                            else
                                            {
                                                label = "CannotPrioritizeIsNotA".Translate(new object[]
                                                {
                                                    pawn.NameStringShort,
                                                    workType2.pawnLabel
                                                });
                                            }
                                        }
                                        else if (!pawn.CanReach(clickCell, PathEndMode.Touch, Danger.Deadly, false, TraverseMode.ByPawn))
                                        {
                                            label = "AreaLower".Translate().CapitalizeFirst() + ": " + "NoPath".Translate();
                                        }
                                        else
                                        {
                                            label = "PrioritizeGeneric".Translate(new object[]
                                            {
                                                workGiver_Scanner2.def.gerund,
                                                "AreaLower".Translate()
                                            });
                                            Job localJob = job2;
                                            WorkGiver_Scanner localScanner = workGiver_Scanner2;
                                            action2 = delegate
                                            {
                                                pawn.jobs.TryTakeOrderedJobPrioritizedWork(localJob, localScanner, clickCell);
                                            };
                                        }
                                        if (!opts.Any((FloatMenuOption op) => op.Label == label.TrimEnd(new char[0])))
                                        {
                                            opts.Add(new FloatMenuOption(label, action2, MenuOptionPriority.Default, null, null, 0f, null, null));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}