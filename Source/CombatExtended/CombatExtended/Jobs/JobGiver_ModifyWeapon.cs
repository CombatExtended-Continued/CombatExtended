using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class JobGiver_ModifyWeapon : JobGiver_UpdateLoadout
    {
        public override float GetPriority(Pawn pawn)
        {
            if (!pawn.RaceProps.Humanlike)
                return -1;                        
            if (pawn.equipment?.Primary is WeaponPlatform platform && !platform.ConfigApplied)                           
                return 15;            
            return -1f;
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            if (false
                || pawn.equipment == null
                || pawn.equipment.Primary == null
                || !(pawn.equipment.Primary is WeaponPlatform))
                return null;
            WeaponPlatform platform = pawn.equipment?.Primary as WeaponPlatform;
            // abort if the weapon is already configured and doesn't require any changes.
            if (platform.ConfigApplied)
                return null;

            // get the crafting bench we are going to use for crafting
            Building bench = pawn.Map.listerBuildings
                                    .AllBuildingsColonistOfDef(CE_BuildingDefOf.GunsmithingBench)
                                    .FirstOrFallback(b => pawn.CanReach(b, PathEndMode.InteractionCell, Danger.Unspecified, false), null);
            if (bench == null)
                return null;
            List<ThingCount> chosenIngThings = new List<ThingCount>();
            AttachmentDef attachmentDef = null;
            IBillGiver billGiver = bench as IBillGiver;
            if (platform.AdditionList.Count >= 0)
            {
                foreach (AttachmentDef def in platform.AdditionList)
                {
                    bool failed = false;
                    foreach (ThingDefCountClass countClass in def.costList)
                    {
                        List<Thing> ing = pawn.Map.listerThings.ThingsOfDef(countClass.thingDef).Where(t => pawn.CanReach(t, PathEndMode.InteractionCell, Danger.Unspecified) && !t.IsForbidden(Faction.OfPlayer) && pawn.CanReserve(t)).OrderBy(t => t.Position.DistanceTo(bench.Position)).ToList();
                        int count = 0;
                        int i = 0;
                        while (i < ing.Count && count < countClass.count)
                        {
                            Thing t = ing[i++];
                            int c;
                            if (count + t.stackCount > countClass.count)
                            {
                                c = countClass.count - count;
                                count = countClass.count;
                            }
                            else
                            {
                                c = t.stackCount;
                                count += t.stackCount;
                            }
                            chosenIngThings.Add(new ThingCount(t, c));
                        }
                        if (count < countClass.count)
                        {
                            failed = true;
                            break;
                        }
                    }
                    if (failed)
                    {
                        chosenIngThings.Clear();
                        continue;
                    }
                    attachmentDef = def;
                    break;
                }
            }
            if (attachmentDef == null)
            {
                if(platform.RemovalList.Count == 0)
                    return null;
                chosenIngThings.Clear();
                attachmentDef = platform.RemovalList.RandomElement();
            }
            Job haulOffJob = null;
            Job modifyJob = TryCreateModifyJob(pawn, platform, attachmentDef, bench, billGiver, chosenIngThings, out haulOffJob);
            if(haulOffJob != null)
            {
                pawn.jobs.jobQueue.EnqueueFirst(modifyJob);
                return haulOffJob;
            }
            return modifyJob;
        }

        public static Job TryCreateModifyJob(Pawn pawn, WeaponPlatform weapon, AttachmentDef attachmentDef, Thing bench,  IBillGiver billGiver, List<ThingCount> chosenIngThings, out Job haulOffJob)
        {
            haulOffJob = WorkGiverUtility.HaulStuffOffBillGiverJob(pawn, billGiver, null);
            JobCE job = new JobCE();
            job.def = CE_JobDefOf.ModifyWeapon;
            job.targetA = bench;
            job.targetDefs.Add(attachmentDef);            
            job.targetQueueA = new List<LocalTargetInfo>() { weapon };
            job.targetQueueB = new List<LocalTargetInfo>(chosenIngThings.Count);
            job.countQueue = new List<int>(chosenIngThings.Count);
            for (int i = 0; i < chosenIngThings.Count; i++)
            {                
                job.targetQueueB.Add(chosenIngThings[i].Thing);
                job.countQueue.Add(chosenIngThings[i].Count);
            }
            job.haulMode = HaulMode.ToCellNonStorage;            
            return job;
        }     
    }
}
