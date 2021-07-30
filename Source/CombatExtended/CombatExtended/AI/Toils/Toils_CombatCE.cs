using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public static class Toils_CombatCE
    {
        public static IEnumerable<Toil> ReloadEquipedWeapon(Pawn pawn, CompAmmoUser compAmmo, TargetIndex weaponIndex)
        {
            Toil waitToil = new Toil() { actor = pawn };
            waitToil.defaultCompleteMode = ToilCompleteMode.Delay;
            waitToil.defaultDuration = Mathf.CeilToInt(compAmmo.Props.reloadTime.SecondsToTicks() / pawn.GetStatValue(CE_StatDefOf.ReloadSpeed));
            yield return waitToil.WithProgressBarToilDelay(weaponIndex);

            Toil reloadToil = new Toil() { actor = pawn };
            reloadToil.AddFinishAction(() =>
            {
                compAmmo.TryFindAmmoInInventory(out Thing ammo);
                compAmmo.LoadAmmo(ammo);
            });
            reloadToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return reloadToil;
        }

        public static Toil AttackStatic(JobDriver driver, TargetIndex targetIndex)
        {
            Toil init = new Toil();
            int numAttacksMade = 0;
            bool startedIncapacitated = false;
            init.initAction = delegate
            {
                Pawn pawn2 = driver.TargetThingA as Pawn;
                if (pawn2 != null)
                {
                    startedIncapacitated = pawn2.Downed;
                }
                driver.pawn.pather.StopDead();
            };
            init.tickAction = delegate
            {
                LocalTargetInfo target = driver.job.GetTarget(targetIndex);
                if (!driver.job.GetTarget(targetIndex).IsValid)
                {
                    driver.ReadyForNextToil();
                }
                else
                {
                    if (target.HasThing)
                    {
                        Pawn pawn = target.Thing as Pawn;
                        if (target.Thing.Destroyed || (pawn != null && !startedIncapacitated && pawn.Downed) || (pawn != null && pawn.IsInvisible()))
                        {
                            driver.EndJobWith(JobCondition.Incompletable);
                            return;
                        }
                    }
                    if (numAttacksMade >= driver.job.maxNumStaticAttacks && !driver.pawn.stances.FullBodyBusy)
                    {
                        driver.ReadyForNextToil();
                    }
                    else if (driver.pawn.TryStartAttack(target))
                    {
                        numAttacksMade++;
                    }
                    else if (!driver.pawn.stances.FullBodyBusy)
                    {
                        driver.ReadyForNextToil();
                    }
                }
            };
            init.defaultCompleteMode = ToilCompleteMode.Never;
            init.activeSkill = () => Toils_Combat.GetActiveSkillForToil(init);
            return init;
        }
    }
}
