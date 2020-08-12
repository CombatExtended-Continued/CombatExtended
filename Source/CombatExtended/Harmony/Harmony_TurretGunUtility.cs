using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

// ReSharper disable InconsistentNaming
// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable UsePatternMatching

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch]
    public static class Harmony_TurretGunUtility
    {
        const string className = "DisplayClass";
        const string methodName = "<TryFindRandomShellDef>";

        public static void Postfix(object __instance, ThingDef x, ref bool __result)
        {
            // Ignore already true results.
            if (__result)
            {
                return;
            }

            var ammoDef = x as AmmoDef;

            // Ignore all non-shell defs.
            if (ammoDef == null || !AmmoUtility.IsShell(ammoDef))
            {
                return;
            }

            #region Get properties of parent of TryFindRandomShellDef
            var type = __instance.GetType();

            bool allowEMP;
            bool.TryParse(AccessTools.Field(type, "allowEMP").GetValue(__instance)?.ToString(), out allowEMP);

            float maxMarketValue;
            float.TryParse(AccessTools.Field(type, "maxMarketValue").GetValue(__instance)?.ToString(), out maxMarketValue);

            bool mustHarmHealth;
            bool.TryParse(AccessTools.Field(type, "mustHarmHealth").GetValue(__instance)?.ToString(), out mustHarmHealth);
            #endregion

            // Check if market value is within range.
            if (maxMarketValue >= 0.0f && ammoDef.BaseMarketValue > maxMarketValue)
            {
                return;
            }

            // Get the explosive damage def.
            var explosiveDamageDef = ammoDef.GetCompProperties<CompProperties_ExplosiveCE>()?.explosiveDamageType ??
                ammoDef.GetCompProperties<CompProperties_Explosive>()?.explosiveDamageType;

            // Get the projectile damage def via the mortar ammo set.
            var mortarAmmoSet = DefDatabase<AmmoSetDef>.GetNamed("AmmoSet_81mmMortarShell");
            var projectileDamageDef = ammoDef.projectile?.damageDef ?? mortarAmmoSet.ammoTypes.FirstOrDefault(t => t.ammo == ammoDef)?.projectile?.projectile?.damageDef;

            // Ignore shells that don't have damage defs.
            if (explosiveDamageDef == null && projectileDamageDef == null)
            {
                return;
            }

            // Ignore EMP if not allowed.
            if (!allowEMP && (explosiveDamageDef == DamageDefOf.EMP || projectileDamageDef == DamageDefOf.EMP))
            {
                return;
            }

            // Check if shell harms health.
            if (mustHarmHealth && explosiveDamageDef != null && !explosiveDamageDef.harmsHealth && projectileDamageDef != null && !projectileDamageDef.harmsHealth)
            {
                return;
            }

            __result = true;
        }

        public static MethodBase TargetMethod()
        {
            var classTargets = typeof(TurretGunUtility).GetNestedTypes(AccessTools.all)
                .Where(x => x.Name.Contains(className));

            if (!classTargets.Any())
                Log.Error("CombatExtended :: Harmony_TurretGunUtility couldn't find subclass with part `"+ className + "`");
            
            var methodTarget = classTargets.SelectMany(x => x.GetMethods(AccessTools.all))
                .FirstOrDefault(x => x.Name.Contains(methodName));

            if (methodTarget == null)
                Log.Error("CombatExtended :: Harmony_TurretGunUtility couldn't find method with part `"+ methodName + "` in subclasses with part `"+ className + "`");
            
            return methodTarget;
        }
    }
}