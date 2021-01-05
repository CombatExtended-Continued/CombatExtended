using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using CombatExtended;

namespace ProjectileImpactFX.HarmonyInstance
{
    [StaticConstructorOnStartup]
    class Main
    {
        static Main()
        {
            var harmony = new Harmony("com.ogliss.rimworld.mod.ProjectileImpactFX");
            Type type = AccessTools.TypeByName("CombatExtended.ProjectileCE");
            if (type != null)
            {

                string s = "CE Patching: ";

                if (CombatExtendedPatch(harmony))
                {
                    s += "Complete";
                }
                else
                {
                    s += "Failed";
                }
                Log.Message(s);
            }
            else
            {
                if (!VanillaPatch(harmony))
                {
                    Log.Warning("Vanilla Patch Failed");
                }
            }

            MethodInfo target = typeof(Verb).GetMethod("TryCastNextBurstShot", BindingFlags.NonPublic | BindingFlags.Instance);
            if (target == null)
            {
                Log.Warning("Target: Verb.TryCastNextBurstShot Not found");
            }
            MethodInfo patch = typeof(Verb_TryCastNextBurstShot_MuzzlePosition_Transpiler).GetMethod("Transpiler");
            if (patch == null)
            {
                Log.Warning("Patch is null Verb_TryCastNextBurstShot_MuzzlePosition_Transpiler.Transpiler");
            }
            if (target != null && patch != null)
            {
                if (harmony.Patch(target, null, null, new HarmonyMethod(patch)) == null)
                {
                    Log.Warning("ProjectileFX: Muzzle Position Transpiler Failed to apply");
                }
            }

         //   harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static bool CombatExtendedPatch(Harmony harmony)
        {
            Type type = AccessTools.TypeByName("CombatExtended.ProjectileCE");
            MethodInfo target = type.GetMethod("Impact", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (target == null)
            {
                Log.Warning("Target: CombatExtended.ProjectileCE.Impact Not found");
                return false;
            }
            MethodInfo patch = typeof(Projectile_Impact_EffectProjectileExtension_Patch_CE).GetMethod("Prefix");
            if (patch == null)
            {
                Log.Warning("Patch is null Projectile_Impact_EffectProjectileExtension_Patch_CE.Prefix");
                return false;
            }
            MethodInfo target2 = type.GetMethod("Tick");
            if (target2 == null)
            {
                Log.Warning("Target2: CombatExtended.ProjectileCE.Tick Not found");
                return false;
            }
            MethodInfo patch2 = typeof(Projectile_Tick_Trailer_Patch_CE).GetMethod("Postfix");
            if (patch == null)
            {
                Log.Warning("Patch2 is null Projectile_Tick_Trailer_Patch_CE.Postfix");
                return false;
            }
            return harmony.Patch(target, new HarmonyMethod(patch)) != null && harmony.Patch(target2, new HarmonyMethod(patch2)) != null;

        }
        public static bool VanillaPatch(Harmony harmony)
        {
            MethodInfo target = typeof(Projectile).GetMethod("Impact", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (target == null)
            {
                Log.Warning("Target is null Projectile.Impact");
                return false;
            }
            MethodInfo patch = typeof(Projectile_Impact_EffectProjectileExtension_Patch).GetMethod("Prefix");
            if (patch == null)
            {
                Log.Warning("Patch is null Projectile_Impact_EffectProjectileExtension_Patch.Prefix");
                return false;
            }
            MethodInfo target2 = typeof(Projectile).GetMethod("Tick");
            if (target2 == null)
            {
                Log.Warning("Target2: Projectile.Tick Not found");
                return false;
            }
            MethodInfo patch2 = typeof(Projectile_Tick_Trailer_Patch).GetMethod("Postfix");
            if (patch == null)
            {
                Log.Warning("Patch2 is null Projectile_Tick_Trailer_Patch.Postfix");
                return false;
            }
            return harmony.Patch(target, new HarmonyMethod(patch)) != null && harmony.Patch(target2, new HarmonyMethod(patch2)) != null;
        }
    }
}