using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using HarmonyLib;
using CombatExtended.AI;
using Verse;
using Verse.AI;

/*
 * Initial notes:
 * Trying to find a general solution to the reload after warmup problem (want reload before warmup).
 * Followed several paths and ended up with Verb.TryStartCastOn.
 * That logic has the bit where the pawn gets put into the warmup stance.  Now need to figure out how to insert a path change so that
 *  if a pawn is out of ammo they try to reload and if that job fails they should fail from the TryStartCastOn...
 */

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Verb), nameof(Verb.TryStartCastOn), new Type[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(bool), typeof(bool), typeof(bool) })]
    static class Harmony_Verb_TryStartCastOn
    {
        private static MethodBase mCausesTimeSlowdown = AccessTools.Method(typeof(Verb), "CausesTimeSlowdown");

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> codes = instructions.ToList();
            bool finished = false; ;
            Label l1 = generator.DefineLabel();

            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                if (!finished && codes[i].OperandIs(mCausesTimeSlowdown))
                {
                    finished = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(code);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Verb_TryStartCastOn), nameof(Harmony_Verb_TryStartCastOn.CheckReload)));
                    yield return new CodeInstruction(OpCodes.Brtrue_S, l1);

                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Ret);
                    code.labels.Add(l1);
                }
                yield return code;
            }
        }

        // Functions like a prefix.  If this has something to do return false. if nothing to do return true.
        static bool CheckReload(Verb __instance, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            // no work to do as the verb isn't the right kind.
            if (!(__instance is Verb_ShootCE || __instance is Verb_ShootCEOneUse))
                return true;
            // If this verb is owned by a pawn that has the tactical manager
            if (__instance.CasterIsPawn)
            {
                var manager = __instance.CasterPawn.GetTacticalManager();
                if(manager != null)
                    return manager.TryStartCastChecks(__instance, castTarg, destTarg);                
            }

            // Legacy setup
            //
            // TODO update this to the modern stander
            CompAmmoUser gun = __instance.EquipmentSource.TryGetComp<CompAmmoUser>();
            if (gun == null || !gun.HasMagazine || gun.CurMagCount > 0)
                return true; // gun isn't an ammo user that stores ammo internally or isn't out of bullets.

            // we got work to do at this point.
            // Try starting the reload job.
            gun.TryStartReload();
            return false;
        }
    }
}