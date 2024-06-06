using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(LordToil_Siege), "LordToilTick")]
    internal static class Harmony_LordToil_Siege
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var methodCustomCondition = typeof(Harmony_LordToil_Siege).GetMethod(nameof(CustomCondition), BindingFlags.Static | BindingFlags.Public);
            var startIndex = -1;
            var endIndex = -1;
            MethodInfo targetMethod = AccessTools.Method(typeof(ThingDef), "get_IsShell"); // Start of the if condition
            FieldInfo targetField = AccessTools.Field(typeof(DamageDef), "harmsHealth"); // End of the if condition
            for (int i = 0; i < codes.Count; i++)
            {
                if (startIndex == -1)
                {
                    // Find the start of the if statement
                    if (codes[i].opcode == OpCodes.Callvirt && codes[i].Calls(targetMethod))
                    {
                        startIndex = i - 1; // We want to edit -1 instruction from the call of IsShell
                    }
                }
                else if (codes[i].opcode == OpCodes.Ldfld && codes[i].LoadsField(targetField))
                {
                    endIndex = i;
                }
            }
            if (startIndex == -1 || endIndex == -1)
            {
                Log.Error("CombatExtended :: Harmony_LordToil_Siege couldn't find code block for patching");
                return codes; // Don't modify as we couldn't find the original code block
            }
            else
            {
                codes[startIndex] = new CodeInstruction(OpCodes.Call, methodCustomCondition); // Call the new method for evaluation
                codes.RemoveRange(startIndex + 1, endIndex - startIndex); // Remove the default code
                return codes;
            }
        }

        public static bool CustomCondition(Thing thing)
        {
            // Ensure damage check is the same here as from CombatExtended.HarmonyCE.Harmony_TurretGunUtility
            var ammoDef = thing.def as AmmoDef;
            if (ammoDef == null)
            {
                return false;
            }
            // Ignore all non-shell defs.
            if (ammoDef == null || !AmmoUtility.IsShell(ammoDef))
            {
                return false;
            }

            // Get the explosive damage def.
            var explosiveDamageDef = ammoDef.GetCompProperties<CompProperties_ExplosiveCE>()?.explosiveDamageType ?? ammoDef.GetCompProperties<CompProperties_Explosive>()?.explosiveDamageType;

            // Get the projectile damage def via the mortar ammo set.
            var projectileDamageDef = ammoDef.projectile?.damageDef ?? CE_AmmoSetDefOf.AmmoSet_81mmMortarShell.ammoTypes.FirstOrDefault(t => t.ammo == ammoDef)?.projectile?.projectile?.damageDef;

            // Ignore shells that don't have damage defs.
            if (explosiveDamageDef == null && projectileDamageDef == null)
            {
                return false;
            }

            // Get the number of fragments
            var fragments = ammoDef.GetCompProperties<CompProperties_Fragments>()?.fragments.Count;

            // Get patched harmful damage for modded/toxic shells
            var harmful = projectileDamageDef?.GetModExtension<DamageDefExtensionCE>()?.isHarmful ?? false;

            // Check if shell harms health.
            if ((explosiveDamageDef == null || !explosiveDamageDef.harmsHealth) && (projectileDamageDef == null || !projectileDamageDef.harmsHealth) && (fragments == null || fragments < 1) && !harmful)
            {
                return false;
            }
            return true;
        }
    }
}
