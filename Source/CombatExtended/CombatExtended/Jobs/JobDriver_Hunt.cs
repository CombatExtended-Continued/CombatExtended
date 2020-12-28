using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
    /// <summary>
    /// Description of JobDriver_Hunt.
    /// </summary>
    public class JobDriver_Hunt : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Victim, job, 1, -1, null);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(delegate
            {
                if (!job.ignoreDesignations)
                {
                    Pawn victim = Victim;
                    if (victim != null && !victim.Dead && Map.designationManager.DesignationOn(victim, DesignationDefOf.Hunt) == null)
                    {
                        return true;
                    }
                }
                return false;
            });

            yield return Toils_Reserve.Reserve(VictimInd, 1);

            var init = new Toil();
            init.initAction = delegate
            {
                jobStartTick = Find.TickManager.TicksGame;
            };
            yield return init;
            
            yield return Toils_Combat.TrySetJobToUseAttackVerb(TargetIndex.A);

            var comp = (pawn.equipment != null && pawn.equipment.Primary != null) ? pawn.equipment.Primary.TryGetComp<CompAmmoUser>() : null;
            var startCollectCorpse = StartCollectCorpseToil();
            var gotoCastPos = GotoCastPosition(VictimInd, true).JumpIfDespawnedOrNull(VictimInd, startCollectCorpse).FailOn(() => Find.TickManager.TicksGame > jobStartTick + MaxHuntTicks);
            
            yield return gotoCastPos;

            //var moveIfCannotHit = Toils_Jump.JumpIfTargetNotHittable(VictimInd, gotoCastPos);
            var moveIfCannotHit = Toils_Jump.JumpIf(gotoCastPos, delegate
            {
                var verb = pawn.CurJob.verbToUse;
                var optimalRange = GetOptimalHuntRange(pawn, Victim);
                if (pawn.Position.DistanceTo(Victim.Position) > optimalRange)
                {
                    return true;
                }
                return !verb.CanHitTarget(Victim);
            });
            
            yield return moveIfCannotHit;
            
            yield return Toils_Jump.JumpIfTargetDespawnedOrNull(VictimInd, startCollectCorpse);

            var startExecuteDowned = Toils_Goto.GotoThing(VictimInd, PathEndMode.Touch).JumpIfDespawnedOrNull(VictimInd, startCollectCorpse);
            
            yield return Toils_Jump.JumpIf(startExecuteDowned, () => Victim.Downed && Victim.RaceProps.executionRange <= 2);

            if (pawn?.equipment?.PrimaryEq?.PrimaryVerb?.IsMeleeAttack ?? false)
            {
                yield return Toils_Combat.FollowAndMeleeAttack(TargetIndex.A, delegate
                {
                    pawn.meleeVerbs.TryMeleeAttack(Victim);
                }).JumpIfDespawnedOrNull(VictimInd, startCollectCorpse);
            }
            else
            {
                yield return Toils_Combat.CastVerb(VictimInd, false).JumpIfDespawnedOrNull(VictimInd, startCollectCorpse)
                    .FailOn(() =>
                    {
                        if (Find.TickManager.TicksGame <= jobStartTick + MaxHuntTicks)
                        {
                            if (comp == null || comp.HasAndUsesAmmoOrMagazine)
                            {
                                return false;
                            }
                        }
                        return true;
                    });

                yield return Toils_Jump.Jump(moveIfCannotHit);
            }

            yield return Toils_Jump.Jump(moveIfCannotHit);

            // Execute downed animal - adapted from JobDriver_Slaughter
            yield return startExecuteDowned;
            
            yield return Toils_General.WaitWith(VictimInd, 180, true).JumpIfDespawnedOrNull(VictimInd, startCollectCorpse);
            
            yield return new Toil
            {
                initAction = delegate
                {
                    ExecutionUtility.DoExecutionByCut(pawn, Victim);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            // Haul corpse to stockpile
            yield return startCollectCorpse;

            yield return Toils_Goto.GotoCell(VictimInd, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(VictimInd).FailOnSomeonePhysicallyInteracting(VictimInd);

            yield return Toils_Haul.StartCarryThing(VictimInd);

            var carryToCell = Toils_Haul.CarryHauledThingToCell(StoreCellInd);

            yield return carryToCell;

            yield return Toils_Haul.PlaceHauledThingInCell(StoreCellInd, carryToCell, true);
        }

        const TargetIndex VictimInd = TargetIndex.A;

        const TargetIndex StoreCellInd = TargetIndex.B;

        const int MaxHuntTicks = 5000;

        int jobStartTick = -1;

        public Pawn Victim
        {
            get
            {
                Corpse corpse = Corpse;
                return corpse != null ? corpse.InnerPawn : (Pawn)job.GetTarget(TargetIndex.A).Thing;
            }
        }

        Corpse Corpse
        {
            get
            {
                return job.GetTarget(TargetIndex.A).Thing as Corpse;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref jobStartTick, "jobStartTick", 0);
        }

        public override string GetReport()
        {
            return pawn.CurJob.def.reportString.Replace("TargetA", Victim.LabelShort);
        }

        //Copy of Verse.AI.Toils_CombatGotoCastPosition
        Toil GotoCastPosition(TargetIndex targetInd, bool closeIfDowned = false)
        {
            var toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.CurJob;
                Thing thing = curJob.GetTarget(targetInd).Thing;
                var pawnVictim = thing as Pawn;

                if (!CastPositionFinder.TryFindCastPosition(new CastPositionRequest
                {
                    caster = toil.actor,
                    target = thing,
                    verb = curJob.verbToUse,
                    maxRangeFromTarget = GetOptimalHuntRange(actor, pawnVictim),
                    wantCoverFromTarget = false
                }, out var intVec))
                {
                    toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                    return;
                }
                toil.actor.pather.StartPath(intVec, PathEndMode.OnCell);
                actor.Map.pawnDestinationReservationManager.Reserve(actor, job, intVec);
            };
            toil.FailOnDespawnedOrNull(targetInd);
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
        }

        public static float GetOptimalHuntRange(Pawn hunter, Pawn victim)
        {
            var curJob = hunter.CurJob;
            var victimProps = victim.RaceProps;
            
            if (victim.Downed)
            {
                // TODO: How to detect boombats?
                return Mathf.Min(curJob.verbToUse.verbProps.range, victimProps.executionRange);
            }
            
            var normalRange = HuntRangePerBodysize(victimProps.baseBodySize, victimProps.executionRange, curJob.verbToUse.verbProps.range);

            if (victimProps.manhunterOnDamageChance > 0)
            {
                // NOTE: I assumed 2 is a good number to consider for aim time of hunting weapons (since hunting weapons are likely to be sniper rifles)
                // We can also get aim time from the actual weapon that the hunter is using
                // but I didn't see the need.
                const int aimTime = 2;
                
                // Determine how much target can move away from the hunter while the hunter is aiming
                float moveOffset = CE_Utility.GetMoveSpeed(victim) * aimTime;

                // Get a little bit closer when target is small. (Smaller targets don't attack that hard so it's ok from safety standpoint) 
                // For reference, Megasloth is 4, and squirrel is 0.2
                float bodySizeFactor = Mathf.Clamp01(victimProps.baseBodySize);
                
                // If the hunter's shooting skill is below skillThreshold, hunter gets closer to not miss their shots
                // I assumed level 10 is a good enough shooter to stay at maximum distance and don't miss 
                const float skillThreshold = 10f;
                float skillFactor = Mathf.Clamp01(hunter.skills.GetSkill(SkillDefOf.Shooting).Level / skillThreshold);
                
                float weaponRange = curJob.verbToUse.verbProps.range;

                // Add additional offset to create a safe margin just in case something weird happens 
                const int additionalOffset = 5;
                float optimal = weaponRange * bodySizeFactor * skillFactor - moveOffset - additionalOffset;
                
                // In some cases optimal range gets lower than the normal range that we calculated for non-manhunting animals.
                // For example when the hunter has 0 shooting skill, optimal range will be negative.
                // In that case we just go back to normal calculated range.
                return Mathf.Max(optimal, normalRange);
            }

            return normalRange;
            
            //Fit for an attack range per body size curve.
            float HuntRangePerBodysize(float x, float executionRange, float gunRange)
            {
                return Mathf.Min(Mathf.Clamp(1 + 20 * (1 - Mathf.Exp(-0.65f * x)), executionRange, 20), gunRange);
            }
        }

        Toil StartCollectCorpseToil()
        {
            var toil = new Toil();
            toil.initAction = delegate
            {
                if (Victim == null)
                {
                    toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.Hunted, new object[]
                {
                    pawn,
                    Victim
                });
                Corpse corpse = Victim.Corpse;
                if (corpse == null || !this.pawn.CanReserveAndReach(corpse, PathEndMode.ClosestTouch, Danger.Deadly, 1))
                {
                    pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }
                corpse.SetForbidden(false);
                IntVec3 c;
                if (StoreUtility.TryFindBestBetterStoreCellFor(corpse, pawn, Map, StoragePriority.Unstored, pawn.Faction, out c))
                {
                    pawn.Reserve(corpse, job, 1);
                    pawn.Reserve(c, job, 1);
                    job.SetTarget(TargetIndex.B, c);
                    job.SetTarget(TargetIndex.A, corpse);
                    job.count = 1;
                    job.haulMode = HaulMode.ToCellStorage;
                    return;
                }
                pawn.jobs.EndCurrentJob(JobCondition.Succeeded, true);
            };
            return toil;
        }
    }
}
