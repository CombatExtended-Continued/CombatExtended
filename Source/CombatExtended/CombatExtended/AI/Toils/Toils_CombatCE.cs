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
        public static Toil ReloadEquipedWeapon(IJobDriver_Tactical driver, TargetIndex progressIndex, Thing ammo = null)
        {
            // fields            
            CompAmmoUser compAmmo = null;
            int reloadingTime = 0;
            int startTick = 0;
            // reloading toil
            Toil waitToil = new Toil() { actor = driver.pawn };
            waitToil.defaultCompleteMode = ToilCompleteMode.Delay;
            waitToil.defaultDuration = 300;
            waitToil.AddPreInitAction(() =>
            {
                if (driver.pawn.equipment?.Primary == null)
                {
                    driver.EndJobWith(JobCondition.Incompletable);
                    return;
                }
                compAmmo = driver.pawn.equipment?.Primary.TryGetComp<CompAmmoUser>();
                if (compAmmo == null || !compAmmo.HasAmmoOrMagazine)
                {
                    driver.EndJobWith(JobCondition.Incompletable);
                    return;
                }
                startTick = GenTicks.TicksGame;                
                reloadingTime = Mathf.CeilToInt(compAmmo.parent.GetStatValue(CE_StatDefOf.ReloadTime).SecondsToTicks() / driver.pawn.GetStatValue(CE_StatDefOf.ReloadSpeed));
            });
            waitToil.tickAction = () =>
            {
                if (GenTicks.TicksGame - startTick >= reloadingTime)
                {
                    if (compAmmo == null)
                    {
                        driver.EndJobWith(JobCondition.Incompletable);
                        return;
                    }
                    if (ammo == null && !compAmmo.TryFindAmmoInInventory(out ammo))
                    {
                        driver.EndJobWith(JobCondition.Incompletable);
                        return;
                    }
                    if (!compAmmo.EmptyMagazine && !compAmmo.TryUnload())
                    {
                        driver.EndJobWith(JobCondition.Incompletable);
                        return;
                    }
                    compAmmo.LoadAmmo(ammo);
                    driver.ReadyForNextToil();
                }
            };
            return waitToil.WithProgressBarToilDelay(progressIndex);
        }

        public static IEnumerable<Toil> AttackStatic(IJobDriver_Tactical driver, TargetIndex targetIndex)
        {
            Toil init = new Toil();
            bool startedIncapacitated = false;
            init.initAction = delegate
            {
                Pawn pawn2 = driver.TargetThingB as Pawn;
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
                    driver.EndJobWith(JobCondition.Succeeded);
                }
                else
                {
                    if (target.HasThing)
                    {
                        Pawn pawn = target.Thing as Pawn;
                        if (target.Thing.Destroyed || (pawn != null && !startedIncapacitated && pawn.Downed) || (pawn != null && pawn.IsInvisible()))
                        {
                            driver.EndJobWith(JobCondition.Succeeded);
                            return;
                        }
                    }
                    if (driver.numAttacksMade >= driver.job.maxNumStaticAttacks && !driver.pawn.stances.FullBodyBusy)
                    {
                        driver.EndJobWith(JobCondition.Succeeded);
                    }
                    else if (driver.pawn.TryStartAttack(target))
                    {
                        driver.numAttacksMade++;
                    }
                    else if (!driver.pawn.stances.FullBodyBusy)
                    {
                        driver.EndJobWith(JobCondition.Incompletable);
                    }
                }
            };
            init.defaultCompleteMode = ToilCompleteMode.Never;
            init.activeSkill = () => Toils_Combat.GetActiveSkillForToil(init);
            yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
            yield return init;
        }
    }
}
