using System.Collections.Generic;
using System.Linq;
using CombatExtended.AI;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class JobGiver_ReactToSuppression : ThinkNode_JobGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            var comp = pawn.TryGetComp<CompSuppressable>();

            if (comp == null) return null;

            Job reactJob = SuppressionUtility.GetRunForCoverJob(pawn);

            // Create smoke screen
            //if (TrySmokeScreening(pawn, out Job screen))
            //{
            //    if (reactJob != null)
            //    {
            //        pawn.jobs.jobQueue.EnqueueFirst(screen);
            //        return reactJob;
            //    }
            //    else
            //    {
            //        return screen;
            //    }
            //}
            // Hunker down
            if (reactJob == null && comp.IsHunkering)
            {
                LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_Hunkering, pawn, OpportunityType.Critical);
                reactJob = JobMaker.MakeJob(CE_JobDefOf.HunkerDown, pawn);
                reactJob.checkOverrideOnExpire = true;
                return reactJob;
            }
            else
            {
                // Run for cover
                LessonAutoActivator.TeachOpportunity(CE_ConceptDefOf.CE_SuppressionReaction, pawn, OpportunityType.Critical);
            }
            return reactJob;
        }

        // TODO fix this
        //
        //private bool TrySmokeScreening(Pawn pawn, out Job screen)
        //{
        //    ThingWithComps weapon = pawn.equipment.Primary;
        //    screen = null;
        //    CompInventory inventory = pawn.TryGetComp<CompInventory>();
        //    foreach (ThingWithComps w in inventory.rangedWeaponList)
        //    {
        //        if (!w.def.weaponTags.Contains("GrenadeSmoke"))
        //            continue;
        //        weapon = w;
        //        break;
        //    }
        //    if (weapon == null || !weapon.def.weaponTags.Contains("GrenadeSmoke"))
        //        return false;
        //    IntVec3 castTarget = IntVec3.Invalid;
        //    foreach (Pawn enemy in pawn.GetTacticalManager().TargetedByEnemy)
        //    {
        //        IEnumerable<IntVec3> cells = GenSightCE.PartialLineOfSights(pawn, enemy.positionInt);
        //        if (cells.Count() > 0)
        //            castTarget = cells.RandomElement();
        //    }
        //    if (castTarget.IsValid)
        //        return false;
        //    if (pawn.equipment?.Primary != null)
        //        pawn.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(CE_JobDefOf.EquipFromInventory, pawn.equipment.Primary));
        //    inventory.TrySwitchToWeapon(weapon);
        //    if (pawn.equipment.Primary != weapon)
        //        return false;
        //    screen = JobMaker.MakeJob(JobDefOf.AttackStatic, castTarget);
        //    return true;
        //}
    }
}