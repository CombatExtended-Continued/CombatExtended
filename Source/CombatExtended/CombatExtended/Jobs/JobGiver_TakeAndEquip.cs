using RimWorld;
using System;
using System.Linq;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Collections.Generic;

namespace CombatExtended
{
    public class JobGiver_TakeAndEquip : ThinkNode_JobGiver
    {
        private const float ammoFractionOfNonAmmoInventory = 0.666f;

        private enum WorkPriority
        {
            None,
            Unloading,
            LowAmmo,
            Weapon,
            Ammo
            //Apparel
        }

        private WorkPriority GetPriorityWork(Pawn pawn)
        {
            #region Traders have no work priority
            if (pawn.kindDef.trader)
            {
                return WorkPriority.None;
            }
            #endregion

            //#region Pawns with non-idle jobs have no work priority
            //bool hasCurJob = pawn.CurJob != null;
            //JobDef jobDef = hasCurJob ? pawn.CurJob.def : null;

            //if (hasCurJob && !jobDef.isIdle)
            //{
            //    return WorkPriority.None;
            //}
            //#endregion

            bool hasPrimary = (pawn.equipment != null && pawn.equipment.Primary != null);
            CompAmmoUser primaryAmmoUser = hasPrimary ? pawn.equipment.Primary.TryGetComp<CompAmmoUser>() : hasWeaponInInventory(pawn) ? weaponInInventory(pawn) : null;

            #region Colonists with primary ammo-user and a loadout have no work priority
            if (pawn.Faction.IsPlayer
              && primaryAmmoUser != null)
            {
                Loadout loadout = pawn.GetLoadout();
                // if (loadout != null && !loadout.Slots.NullOrEmpty())
                if (loadout != null && loadout.SlotCount > 0)
                {
                    return WorkPriority.None;
                }
            }
            #endregion

            // Pawns without weapon..
            if (!hasPrimary)
            {
                // With inventory && non-colonist && not stealing && little space left
                if (Unload(pawn))
                {
                    return WorkPriority.Unloading;
                }
                // Without inventory || colonist || stealing || lots of space left
                if (!hasWeaponInInventory(pawn))
                {
                    return WorkPriority.Weapon;
                }
            }

            CompInventory compInventory = pawn.TryGetComp<CompInventory>();
            // Pawn with ammo-using weapon..
            if (primaryAmmoUser != null && primaryAmmoUser.UseAmmo)
            {
                // Magazine size
                FloatRange magazineSize = new FloatRange(1f, 2f);
                LoadoutPropertiesExtension loadoutPropertiesExtension = (LoadoutPropertiesExtension)(pawn.kindDef.modExtensions?.FirstOrDefault(x => x is LoadoutPropertiesExtension));
                bool hasWeaponTags = pawn.kindDef.weaponTags?.Any() ?? false;

                if (hasWeaponTags
                  && primaryAmmoUser.parent.def.weaponTags.Any(pawn.kindDef.weaponTags.Contains)
                  && loadoutPropertiesExtension != null
                  && loadoutPropertiesExtension.primaryMagazineCount != FloatRange.Zero)
                {
                    magazineSize.min = loadoutPropertiesExtension.primaryMagazineCount.min;
                    magazineSize.max = loadoutPropertiesExtension.primaryMagazineCount.max;
                }

                magazineSize.min *= primaryAmmoUser.Props.magazineSize;
                magazineSize.max *= primaryAmmoUser.Props.magazineSize;

                // Number of things in inventory that could be put in the weapon
                int viableAmmoCarried = 0;
                float viableAmmoBulk = 0;
                foreach (AmmoLink link in primaryAmmoUser.Props.ammoSet.ammoTypes)
                {
                    var count = compInventory.AmmoCountOfDef(link.ammo);
                    viableAmmoCarried += count;
                    viableAmmoBulk += count * link.ammo.GetStatValueAbstract(CE_StatDefOf.Bulk);
                }

                // ~2/3rds of the inventory bulk minus non-usable and non-ammo bulk could be filled with ammo
                float potentialAmmoBulk = ammoFractionOfNonAmmoInventory * (compInventory.capacityBulk - compInventory.currentBulk + viableAmmoBulk);
                // There's less ammo [bulk] than fits the potential ammo bulk [bulk]
                if (viableAmmoBulk < potentialAmmoBulk)
                {
                    // There's less ammo [nr] than fits a clip [nr]
                    if (primaryAmmoUser.Props.magazineSize == 0 || viableAmmoCarried < magazineSize.min)
                    {
                        return Unload(pawn) ? WorkPriority.Unloading : WorkPriority.LowAmmo;
                    }

                    // There's less ammo [nr] than fits two clips [nr] && no enemies are close
                    if (viableAmmoCarried < magazineSize.max
                     && !PawnUtility.EnemiesAreNearby(pawn, 20, true))
                    {
                        return Unload(pawn) ? WorkPriority.Unloading : WorkPriority.Ammo;
                    }
                }
            }
            return WorkPriority.None;
        }

        public override float GetPriority(Pawn pawn)
        {
            if ((!Controller.settings.AutoTakeAmmo && pawn.IsColonist) || !Controller.settings.EnableAmmoSystem) return 0f;

            if (pawn.Faction == null) return 0f;

            var priority = GetPriorityWork(pawn);

            if (priority == WorkPriority.Unloading) return 9.2f;
            else if (priority == WorkPriority.LowAmmo) return 9f;
            else if (priority == WorkPriority.Weapon) return 8f;
            else if (priority == WorkPriority.Ammo) return 6f;
            //else if (priority == WorkPriority.Apparel) return 5f;
            else if (priority == WorkPriority.None) return 0f;

            TimeAssignmentDef assignment = (pawn.timetable != null) ? pawn.timetable.CurrentAssignment : TimeAssignmentDefOf.Anything;
            if (assignment == TimeAssignmentDefOf.Sleep) return 0f;

            if (pawn.health == null || pawn.Downed || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return 0f;
            }
            else return 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!Controller.settings.EnableAmmoSystem || !Controller.settings.AutoTakeAmmo)
            {
                return null;
            }

            if (pawn.Faction == null) //Wild man (b19 incident added) Faction is null
            {
                return null;
            }

            if (!pawn.RaceProps.Humanlike || (pawn.story != null && pawn.WorkTagIsDisabled(WorkTags.Violent)))
            {
                return null;
            }
            if (pawn.Faction.IsPlayer && pawn.Drafted)
            {
                return null;
            }

            if (!Rand.MTBEventOccurs(60, 5, 30))
            {
                return null;
            }

            if (!pawn.Faction.IsPlayer && FindBattleWorthyEnemyPawnsCount(Find.CurrentMap, pawn) > 25)
            {
                return null;
            }
            if (pawn.IsPrisoner && (pawn.HostFaction != Faction.OfPlayer || pawn.guest.interactionMode == PrisonerInteractionModeDefOf.Release))
            {
                return null;
            }

            //Log.Message(pawn.ThingID +  " - priority:" + (GetPriorityWork(pawn)).ToString() + " capacityWeight: " + pawn.TryGetComp<CompInventory>().capacityWeight.ToString() + " currentWeight: " + pawn.TryGetComp<CompInventory>().currentWeight.ToString() + " capacityBulk: " + pawn.TryGetComp<CompInventory>().capacityBulk.ToString() + " currentBulk: " + pawn.TryGetComp<CompInventory>().currentBulk.ToString());

            var brawler = (pawn.story != null && pawn.story.traits != null && pawn.story.traits.HasTrait(TraitDefOf.Brawler));
            CompInventory inventory = pawn.TryGetComp<CompInventory>();
            bool hasPrimary = (pawn.equipment != null && pawn.equipment.Primary != null);
            CompAmmoUser primaryAmmoUser = hasPrimary ? pawn.equipment.Primary.TryGetComp<CompAmmoUser>() : null;
            CompAmmoUser primaryAmmoUserWithInventoryCheck = hasPrimary ? pawn.equipment.Primary.TryGetComp<CompAmmoUser>() : hasWeaponInInventory(pawn) ? weaponInInventory(pawn) : null;
            if (inventory != null)
            {
                // Prefer ranged weapon in inventory
                if (!pawn.Faction.IsPlayer && hasPrimary && pawn.equipment.Primary.def.IsMeleeWeapon && !brawler)
                {
                    if ((pawn.skills.GetSkill(SkillDefOf.Shooting).Level >= pawn.skills.GetSkill(SkillDefOf.Melee).Level
                         || pawn.skills.GetSkill(SkillDefOf.Shooting).Level >= 6))
                    {
                        ThingWithComps InvListGun3 = inventory.rangedWeaponList.Find(thing => thing.TryGetComp<CompAmmoUser>() != null && thing.TryGetComp<CompAmmoUser>().HasAmmoOrMagazine);
                        if (InvListGun3 != null)
                        {
                            inventory.TrySwitchToWeapon(InvListGun3);
                        }
                    }
                }

                // Equip weapon if no any weapon
                if (!pawn.Faction.IsPlayer && !hasPrimary)
                {
                    // For ranged weapon
                    if ((pawn.skills.GetSkill(SkillDefOf.Shooting).Level >= pawn.skills.GetSkill(SkillDefOf.Melee).Level
                         || pawn.skills.GetSkill(SkillDefOf.Shooting).Level >= 6) && !brawler)
                    {
                        ThingWithComps InvListGun3 = inventory.rangedWeaponList.Find(thing => thing.TryGetComp<CompAmmoUser>() != null && thing.TryGetComp<CompAmmoUser>().HasAmmoOrMagazine);
                        if (InvListGun3 != null)
                        {
                            inventory.TrySwitchToWeapon(InvListGun3);
                        }
                    }
                    else
                    {
                        // For melee weapon
                        ThingWithComps InvListMeleeWeapon = inventory.meleeWeaponList.Find(thing => thing.def.IsMeleeWeapon);
                        if (InvListMeleeWeapon != null)
                        {
                            inventory.TrySwitchToWeapon(InvListMeleeWeapon);
                        }
                    }
                }

                var priority = GetPriorityWork(pawn);

                // Drop excess ranged weapon
                if (!pawn.Faction.IsPlayer && primaryAmmoUser != null && priority == WorkPriority.Unloading && inventory.rangedWeaponList.Count >= 1)
                {
                    Thing ListGun = inventory.rangedWeaponList.Find(thing => thing.TryGetComp<CompAmmoUser>() != null && thing.def != pawn.equipment.Primary.def);
                    if (ListGun != null)
                    {
                        Thing ammoListGun = null;
                        if (!ListGun.TryGetComp<CompAmmoUser>().HasAmmoOrMagazine)
                            foreach (AmmoLink link in ListGun.TryGetComp<CompAmmoUser>().Props.ammoSet.ammoTypes)
                            {
                                if (inventory.ammoList.Find(thing => thing.def == link.ammo) == null)
                                {
                                    ammoListGun = ListGun;
                                    break;
                                }
                            }
                        if (ammoListGun != null)
                        {
                            Thing droppedWeapon;
                            if (inventory.container.TryDrop(ListGun, pawn.Position, pawn.Map, ThingPlaceMode.Near, ListGun.stackCount, out droppedWeapon))
                            {
                                pawn.jobs.EndCurrentJob(JobCondition.None, true);
                                pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.DropEquipment, droppedWeapon, 30, true));
                            }
                        }
                    }
                }

                // Find and drop not need ammo from inventory
                if (!pawn.Faction.IsPlayer && hasPrimary && inventory.ammoList.Count > 1 && priority == WorkPriority.Unloading)
                {
                    Thing WrongammoThing = null;
                    WrongammoThing = primaryAmmoUser != null
                        ? inventory.ammoList.Find(thing => !primaryAmmoUser.Props.ammoSet.ammoTypes.Any(a => a.ammo == thing.def))
                        : inventory.ammoList.RandomElement<Thing>();

                    if (WrongammoThing != null)
                    {
                        Thing InvListGun = inventory.rangedWeaponList.Find(thing => hasPrimary && thing.TryGetComp<CompAmmoUser>() != null && thing.def != pawn.equipment.Primary.def);
                        if (InvListGun != null)
                        {
                            Thing ammoInvListGun = null;
                            foreach (AmmoLink link in InvListGun.TryGetComp<CompAmmoUser>().Props.ammoSet.ammoTypes)
                            {
                                ammoInvListGun = inventory.ammoList.Find(thing => thing.def == link.ammo);
                                break;
                            }
                            if (ammoInvListGun != null && ammoInvListGun != WrongammoThing)
                            {
                                Thing droppedThingAmmo;
                                if (inventory.container.TryDrop(ammoInvListGun, pawn.Position, pawn.Map, ThingPlaceMode.Near, ammoInvListGun.stackCount, out droppedThingAmmo))
                                {
                                    pawn.jobs.EndCurrentJob(JobCondition.None, true);
                                    pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.DropEquipment, 30, true));
                                }
                            }
                        }
                        else
                        {
                            Thing droppedThing;
                            if (inventory.container.TryDrop(WrongammoThing, pawn.Position, pawn.Map, ThingPlaceMode.Near, WrongammoThing.stackCount, out droppedThing))
                            {
                                pawn.jobs.EndCurrentJob(JobCondition.None, true);
                                pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.DropEquipment, 30, true));
                            }
                        }
                    }
                }


                // Find weapon in inventory and try to switch if any ammo in inventory.
                if (priority == WorkPriority.Weapon && !hasPrimary)
                {
                    ThingWithComps InvListGun2 = inventory.rangedWeaponList.Find(thing => thing.TryGetComp<CompAmmoUser>() != null);

                    if (InvListGun2 != null)
                    {
                        Thing ammoInvListGun2 = null;
                        foreach (AmmoLink link in InvListGun2.TryGetComp<CompAmmoUser>().Props.ammoSet.ammoTypes)
                        {
                            ammoInvListGun2 = inventory.ammoList.Find(thing => thing.def == link.ammo);
                            break;
                        }
                        if (ammoInvListGun2 != null)
                        {
                            inventory.TrySwitchToWeapon(InvListGun2);
                        }
                    }

                    // Find weapon with near ammo for ai.
                    if (!pawn.Faction.IsPlayer)
                    {
                        Predicate<Thing> validatorWS = (Thing w) => w.def.IsWeapon
                            && w.MarketValue > 500 && pawn.CanReserve(w, 1)
                            && pawn.Position.InHorDistOf(w.Position, 25f)
                            && pawn.CanReach(w, PathEndMode.Touch, Danger.Deadly, true)
                            && (pawn.Faction.HostileTo(Faction.OfPlayer) || pawn.Faction == Faction.OfPlayer || !pawn.Map.areaManager.Home[w.Position]);

                        // generate a list of all weapons (this includes melee weapons)
                        List<Thing> allWeapons = (
                            from w in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways)
                            where validatorWS(w)
                            orderby w.MarketValue - w.Position.DistanceToSquared(pawn.Position) * 2f descending
                            select w
                            ).ToList();

                        // now just get the ranged weapons out...
                        List<Thing> rangedWeapons = allWeapons.Where(w => w.def.IsRangedWeapon).ToList();

                        if (!rangedWeapons.NullOrEmpty())
                        {
                            foreach (Thing thing in rangedWeapons)
                            {
                                if (thing.TryGetComp<CompAmmoUser>() == null)
                                {
                                    // pickup a non-CE ranged weapon...
                                    int numToThing = 0;
                                    if (inventory.CanFitInInventory(thing, out numToThing))
                                    {
                                        return JobMaker.MakeJob(JobDefOf.Equip, thing);
                                    }
                                }
                                else
                                {
                                    // pickup a CE ranged weapon...
                                    List<ThingDef> thingDefAmmoList = thing.TryGetComp<CompAmmoUser>().Props.ammoSet.ammoTypes.Select(g => g.ammo as ThingDef).ToList();

                                    Predicate<Thing> validatorA = (Thing t) => t.def.category == ThingCategory.Item
                                        && t is AmmoThing && pawn.CanReserve(t, 1)
                                        && pawn.Position.InHorDistOf(t.Position, 25f)
                                        && pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly, true)
                                        && (pawn.Faction.HostileTo(Faction.OfPlayer) || pawn.Faction == Faction.OfPlayer || !pawn.Map.areaManager.Home[t.Position]);

                                    List<Thing> thingAmmoList = (
                                        from t in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways)
                                        where validatorA(t)
                                        select t
                                        ).ToList();

                                    if (thingAmmoList.Count > 0 && thingDefAmmoList.Count > 0)
                                    {
                                        int desiredStackSize = thing.TryGetComp<CompAmmoUser>().Props.magazineSize * 2;
                                        Thing th = thingAmmoList.FirstOrDefault(x => thingDefAmmoList.Contains(x.def) && x.stackCount > desiredStackSize);
                                        if (th != null)
                                        {
                                            int numToThing = 0;
                                            if (inventory.CanFitInInventory(thing, out numToThing))
                                            {
                                                return JobMaker.MakeJob(JobDefOf.Equip, thing);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // else if no ranged weapons with nearby ammo was found, lets consider a melee weapon.
                        if (allWeapons != null && allWeapons.Count > 0)
                        {
                            // since we don't need to worry about ammo, just pick one.
                            Thing meleeWeapon = allWeapons.FirstOrDefault(w => !w.def.IsRangedWeapon && w.def.IsMeleeWeapon);

                            if (meleeWeapon != null)
                            {
                                return JobMaker.MakeJob(JobDefOf.Equip, meleeWeapon);
                            }
                        }
                    }
                }

                // Find ammo
                if ((priority == WorkPriority.Ammo || priority == WorkPriority.LowAmmo)
                    && primaryAmmoUserWithInventoryCheck != null)
                {
                    List<ThingDef> curAmmoList = (from AmmoLink g in primaryAmmoUserWithInventoryCheck.Props.ammoSet.ammoTypes
                                                  select g.ammo as ThingDef).ToList();

                    if (curAmmoList.Count > 0)
                    {
                        Predicate<Thing> validator = (Thing t) => t is AmmoThing && pawn.CanReserve(t, 1)
                                        && pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly, true)
                                        && ((pawn.Faction.IsPlayer && !ForbidUtility.IsForbidden(t, pawn)) || (!pawn.Faction.IsPlayer && pawn.Position.InHorDistOf(t.Position, 35f)))
                                        && (pawn.Faction.HostileTo(Faction.OfPlayer) || pawn.Faction == Faction.OfPlayer || !pawn.Map.areaManager.Home[t.Position]);
                        List<Thing> curThingList = (
                            from t in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableAlways)
                            where validator(t)
                            select t
                            ).ToList();
                        foreach (Thing th in curThingList)
                        {
                            foreach (ThingDef thd in curAmmoList)
                            {
                                if (thd == th.def)
                                {
                                    //Defence from low count loot spam
                                    float thw = (th.GetStatValue(CE_StatDefOf.Bulk)) * th.stackCount;
                                    if (thw > 0.5f)
                                    {
                                        if (pawn.Faction.IsPlayer)
                                        {
                                            int SearchRadius = 0;
                                            if (priority == WorkPriority.LowAmmo) SearchRadius = 70;
                                            else SearchRadius = 30;

                                            Thing closestThing = GenClosest.ClosestThingReachable(
                                            pawn.Position,
                                            pawn.Map,
                                            ThingRequest.ForDef(th.def),
                                            PathEndMode.ClosestTouch,
                                            TraverseParms.For(pawn, Danger.None, TraverseMode.ByPawn),
                                            SearchRadius,
                                            x => !x.IsForbidden(pawn) && pawn.CanReserve(x));

                                            if (closestThing != null)
                                            {
                                                int numToCarry = 0;
                                                if (inventory.CanFitInInventory(th, out numToCarry))
                                                {
                                                    Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, th);
                                                    job.count = numToCarry;
                                                    return job;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            int numToCarry = 0;
                                            if (inventory.CanFitInInventory(th, out numToCarry))
                                            {
                                                Job job = JobMaker.MakeJob(JobDefOf.TakeInventory, th);
                                                job.count = Mathf.RoundToInt(numToCarry * 0.8f);
                                                return job;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                /*
                if (!pawn.Faction.IsPlayer && pawn.apparel != null && priority == WorkPriority.Apparel)
                {
                    if (!pawn.apparel.BodyPartGroupIsCovered(BodyPartGroupDefOf.Torso))
                    {
                        Apparel apparel = this.FindGarmentCoveringPart(pawn, BodyPartGroupDefOf.Torso);
                        if (apparel != null)
                        {
                            int numToapparel = 0;
                            if (inventory.CanFitInInventory(apparel, out numToapparel))
                            {
                                return JobMaker.MakeJob(JobDefOf.Wear, apparel)
                                {
                                    ignoreForbidden = true
                                };
                            }
                        }
                    }
                    if (!pawn.apparel.BodyPartGroupIsCovered(BodyPartGroupDefOf.Legs))
                    {
                        Apparel apparel2 = this.FindGarmentCoveringPart(pawn, BodyPartGroupDefOf.Legs);
                        if (apparel2 != null)
                        {
                            int numToapparel2 = 0;
                            if (inventory.CanFitInInventory(apparel2, out numToapparel2))
                            {
                                return JobMaker.MakeJob(JobDefOf.Wear, apparel2)
                                {
                                    ignoreForbidden = true
                                };
                            }
                        }
                    }
                    if (!pawn.apparel.BodyPartGroupIsCovered(BodyPartGroupDefOf.FullHead))
                    {
                        Apparel apparel3 = this.FindGarmentCoveringPart(pawn, BodyPartGroupDefOf.FullHead);
                        if (apparel3 != null)
                        {
                            int numToapparel3 = 0;
                            if (inventory.CanFitInInventory(apparel3, out numToapparel3))
                            {
                                return JobMaker.MakeJob(JobDefOf.Wear, apparel3)
                                {
                                    ignoreForbidden = true,
                                    locomotionUrgency = LocomotionUrgency.Sprint
                                };
                            }
                        }
                    }
                }
                */
                return null;
            }
            return null;
        }

        /*
        private static Job GotoForce(Pawn pawn, LocalTargetInfo target, PathEndMode pathEndMode)
        {
            using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, target, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings, false), pathEndMode))
            {
                IntVec3 cellBeforeBlocker;
                Thing thing = pawnPath.FirstBlockingBuilding(out cellBeforeBlocker, pawn);
                if (thing != null)
                {
                    Job job = DigUtility.PassBlockerJob(pawn, thing, cellBeforeBlocker, true);
                    if (job != null)
                    {
                        return job;
                    }
                }
                if (thing == null)
                {
                    return JobMaker.MakeJob(JobDefOf.Goto, target, 100, true);
                }
                if (pawn.equipment.Primary != null)
                {
                    Verb primaryVerb = pawn.equipment.PrimaryEq.PrimaryVerb;
                    if (primaryVerb.verbProps.ai_IsBuildingDestroyer && (!primaryVerb.verbProps.ai_IsIncendiary || thing.FlammableNow))
                    {
                        return JobMaker.MakeJob(JobDefOf.UseVerbOnThing)
                        {
                            targetA = thing,
                            verbToUse = primaryVerb,
                            expiryInterval = 100
                        };
                    }
                }
                return MeleeOrWaitJob(pawn, thing, cellBeforeBlocker);
            }
        }
        */

        private static bool hasWeaponInInventory(Pawn pawn)
        {
            Thing ListGun = pawn.TryGetComp<CompInventory>().rangedWeaponList.Find(thing => thing.TryGetComp<CompAmmoUser>() != null);
            if (ListGun != null)
            {
                //Log.Message("pawn: " + pawn.ThingID +  " gun: " + ListGun.ToString());
                return true;
            }
            return false;
        }

        private static CompAmmoUser weaponInInventory(Pawn pawn)
        {
            return pawn.TryGetComp<CompInventory>().rangedWeaponList.Find(thing => thing.TryGetComp<CompAmmoUser>() != null).TryGetComp<CompAmmoUser>();
        }

        public static int FindBattleWorthyEnemyPawnsCount(Map map, Pawn pawn)
        {
            if (pawn == null || pawn.Faction == null)
            {
                return 0;
            }
            IEnumerable<Pawn> pawns = map.mapPawns.FreeHumanlikesSpawnedOfFaction(pawn.Faction).Where(p => p.Faction != Faction.OfPlayer && !p.Downed);
            if (pawns == null)
                return 0;
            else
                return pawns.Count();
        }

        private static bool Unload(Pawn pawn)
        {
            var inv = pawn.TryGetComp<CompInventory>();
            if (inv != null
            && !pawn.Faction.IsPlayer
            && (pawn.CurJob != null && pawn.CurJob.def != JobDefOf.Steal)
            && ((inv.capacityWeight - inv.currentWeight < 3f)
            || (inv.capacityBulk - inv.currentBulk < 4f)))
            {
                return true;
            }
            else return false;
        }

        private static Job MeleeOrWaitJob(Pawn pawn, Thing blocker, IntVec3 cellBeforeBlocker)
        {
            if (!pawn.CanReserve(blocker, 1))
            {
                return JobMaker.MakeJob(JobDefOf.Goto, CellFinder.RandomClosewalkCellNear(cellBeforeBlocker, pawn.Map, 10), 100, true);
            }
            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, blocker);
            job.ignoreDesignations = true;
            job.expiryInterval = 100;
            job.checkOverrideOnExpire = true;
            return job;
        }

        /*
        private Apparel FindGarmentCoveringPart(Pawn pawn, BodyPartGroupDef bodyPartGroupDef)
        {
            Room room = pawn.GetRoom();
            Predicate<Thing> validator = (Thing t) => pawn.CanReserve(t, 1) 
            && pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly, true) 
            && (t.Position.DistanceToSquared(pawn.Position) < 12f || room == RegionAndRoomQuery.RoomAtFast(t.Position, t.Map));
            List<Thing> aList = (
                from t in pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel)
                orderby t.MarketValue - t.Position.DistanceToSquared(pawn.Position) * 2f descending
                where validator(t)
                select t
                ).ToList();
            foreach (Thing current in aList)
            {
                Apparel ap = current as Apparel;
                if (ap != null && ap.def.apparel.bodyPartGroups.Contains(bodyPartGroupDef) && pawn.CanReserve(ap, 1) && ApparelUtility.HasPartsToWear(pawn, ap.def))
                {
                    return ap;
                }
            }
            return null;
        }
        */
    }
}
