using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;
using Harmony;

namespace CombatExtended.Harmony
{
    [HarmonyPatch(typeof(Verb_MeleeAttack))]
    [HarmonyPatch("TryCastShot")]
    [HarmonyPriority(Priority.High)]
    static class Harmony_Verb_MeleeAttack_TryCastShot_Patch
    {
        private const float ShieldBlockChance = 0.75f;   // If we have a shield equipped, this is the chance a parry will be a shield block

        public static bool Prefix(Verb_MeleeAttack __instance, ref bool __result)
        {
            var verb_MeleeAttack = Traverse.Create(__instance);

            Pawn casterPawn = __instance.CasterPawn;
            if (casterPawn.stances.FullBodyBusy)
            {
                __result = false;
                return false;
            }
            LocalTargetInfo currentTarget = verb_MeleeAttack.Field("currentTarget").GetValue<LocalTargetInfo>();
            Thing targThing = currentTarget.Thing;
            if (!__instance.CanHitTarget(targThing))
            {
                Log.Warning(string.Concat(new object[]
                {
            casterPawn,
            " meleed ",
            targThing,
            " from out of melee position."
                }));
            }
            casterPawn.Drawer.rotator.Face(targThing.DrawPos);

            // Award XP
            bool targetImmobile = verb_MeleeAttack.Method("IsTargetImmobile", currentTarget).GetValue<bool>();
            if (!targetImmobile && casterPawn.skills != null)
            {
                casterPawn.skills.Learn(SkillDefOf.Melee, 250f, false);
            }

            // Hit calculations
            SoundDef soundDef;
            var hitRoll = Rand.Value;
            string moteText = "";
            Log.Message("CE :: hitRoll for " + casterPawn.ToString() + " attacking " + targThing.ToString() + ": " + hitRoll.ToString());
            if (hitRoll < verb_MeleeAttack.Method("GetHitChance", (LocalTargetInfo)targThing).GetValue<float>())
            {
                // Check for dodge
                bool surpriseAttack = verb_MeleeAttack.Field("surpriseAttack").GetValue<bool>();
                if (!targetImmobile && !surpriseAttack && hitRoll < targThing.GetStatValue(CE_StatDefOf.MeleeDodgeChance))
                {
                    // Attack is evaded
                }
                else
                {
                    // Attack connects, calculate resolution
                    var resultRoll = Rand.Value;
                    var parryChance = targThing.GetStatValue(CE_StatDefOf.MeleeParryChance);
                    Pawn defender = targThing as Pawn;
                    if (!surpriseAttack && defender != null && resultRoll < parryChance)
                    {
                        // Attack is parried
                        Apparel shield = defender.apparel.WornApparel.FirstOrDefault(x => x is Apparel_Shield);
                        bool isShieldBlock = shield != null && Rand.Chance(ShieldBlockChance);
                        Thing parryThing = isShieldBlock ? shield
                            : defender.equipment?.Primary != null ? defender.equipment.Primary : defender;

                        if (resultRoll < parryChance * targThing.GetStatValue(CE_StatDefOf.MeleeCritChance))
                        {
                            // TODO Do a riposte
                            moteText = "Riposted";
                        }
                        else
                        {
                            // TODO Do a parry
                            moteText = "Parried";
                        }

                        // TODO Set hit sound to something more appropriate
                        soundDef = targThing.def.category == ThingCategory.Building ?
                            verb_MeleeAttack.Method("SoundHitBuilding").GetValue<SoundDef>()
                            : verb_MeleeAttack.Method("SoundHitPawn").GetValue<SoundDef>();
                    }
                    else
                    {
                        // Attack connects
                        if (!surpriseAttack && resultRoll < (1 - casterPawn.GetStatValue(CE_StatDefOf.MeleeCritChance)))
                        {
                            // Do a regular hit as per vanilla
                            __result = true;
                            verb_MeleeAttack.Method("ApplyMeleeDamageToTarget", currentTarget).GetValue();
                        }
                        else
                        {
                            // TODO Do a critical hit
                            moteText = "Critical hit";
                        }

                        // Set hit sound
                        soundDef = targThing.def.category == ThingCategory.Building ? 
                            verb_MeleeAttack.Method("SoundHitBuilding").GetValue<SoundDef>() 
                            : verb_MeleeAttack.Method("SoundHitPawn").GetValue<SoundDef>();
                    }
                }
            }
            else
            {
                // Attack missed
                __result = false;
                soundDef = verb_MeleeAttack.Method("SoundMiss").GetValue<SoundDef>();
            }
            if (!moteText.NullOrEmpty())
                MoteMaker.ThrowText(targThing.DrawPos, casterPawn.Map, moteText);

            /*
            if (Rand.Value < verb_MeleeAttack.Method("GetHitChance", (LocalTargetInfo)thing).GetValue<float>())
            {
                __result = true;
                verb_MeleeAttack.Method("ApplyMeleeDamageToTarget", currentTarget).GetValue();
                if (thing.def.category == ThingCategory.Building)
                {
                    soundDef = verb_MeleeAttack.Method("SoundHitBuilding").GetValue<SoundDef>();
                }
                else
                {
                    soundDef = verb_MeleeAttack.Method("SoundHitPawn").GetValue<SoundDef>();
                }
            }
            else
            {
                __result = false;
                soundDef = verb_MeleeAttack.Method("SoundMiss").GetValue<SoundDef>();
            }
            */

            // Play sound and do animations
            soundDef.PlayOneShot(new TargetInfo(targThing.Position, casterPawn.Map, false));
            casterPawn.Drawer.Notify_MeleeAttackOn(targThing);
            Pawn pawn = targThing as Pawn;
            if (pawn != null && !pawn.Dead)
            {
                pawn.stances.StaggerFor(95);
                if (casterPawn.MentalStateDef != MentalStateDefOf.SocialFighting || pawn.MentalStateDef != MentalStateDefOf.SocialFighting)
                {
                    pawn.mindState.meleeThreat = casterPawn;
                    pawn.mindState.lastMeleeThreatHarmTick = Find.TickManager.TicksGame;
                }
            }
            casterPawn.Drawer.rotator.FaceCell(targThing.Position);
            if (casterPawn.caller != null)
            {
                casterPawn.caller.Notify_DidMeleeAttack();
            }
            return false;
        }

        private static void DoRiposte(Pawn attacker, Pawn defender, Thing parryThing, bool riposte = false)
        {
            // TODO
        }
    }

    /*
    [HarmonyPatch(typeof(Verb_MeleeAttack))]
    [HarmonyPatch("GetHitChance")]
    [HarmonyPriority(Priority.High)]
    static class Harmony_Verb_MeleeAttack_GetHitChance_Patch
    {
        public static bool Prefix(Verb_MeleeAttack __instance, ref float __result, ref LocalTargetInfo target)
        {
            var verb = Traverse.Create(__instance);

            if (verb.Field("surpriseAttack").GetValue<bool>() || verb.Method("IsTargetImmobile", target).GetValue<bool>())
            {
                __result = 1f;
                return false;
            }
            if (__instance.CasterPawn.skills != null)
            {
                __result = __instance.CasterPawn.GetStatValue(StatDefOf.MeleeHitChance, true);
            }
            else
            {
                __result = 0.6f;
            }
            // Factor in target's dodge
            Pawn targetPawn = target.Thing as Pawn;
            if (targetPawn != null)
            {
                
            }
        }
    }
    */
}
