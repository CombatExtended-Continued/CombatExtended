using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CombatExtended.HarmonyCE
{
    public class Harmony_CompUniqueWeapon_Setup
    {
        [HarmonyPatch(typeof(CompUniqueWeapon), nameof(CompUniqueWeapon.Setup))]
        internal static class CompUniqueWeapon_Setup
        {
            internal static void Postfix(CompUniqueWeapon __instance, bool fromSave)
            {
                CompUnderBarrel compUnderBarrel = __instance.parent.TryGetComp<CompUnderBarrel>();
                bool underBarrelApplied = false;
                bool verbOverrideApplied = false;

                foreach (WeaponTraitDef trait in __instance.traits)
                {
                    if (trait is CustomWeaponTraitDef customTrait)
                    {
                        if (!underBarrelApplied && customTrait.underBarrelProps != null)
                        {
                            if (compUnderBarrel != null)
                            {
                                compUnderBarrel.props = customTrait.underBarrelProps;
                            }
                            else
                            {
                                Log.Warning($"[CE] Trait {customTrait.defName} has underBarrelProps but {__instance.parent.def.defName} lacks CompUnderBarrel");
                            }
                            underBarrelApplied = true;
                        }

                        if (!verbOverrideApplied && ApplyVerbOverride(__instance, customTrait))
                        {
                            verbOverrideApplied = true;
                        }

                        if (underBarrelApplied && verbOverrideApplied)
                        {
                            break;
                        }
                    }
                }
            }

            private static bool ApplyVerbOverride(CompUniqueWeapon instance, CustomWeaponTraitDef customTrait)
            {
                CompEquippable compEq = instance.parent.TryGetComp<CompEquippable>();
                if (compEq?.PrimaryVerb == null)
                {
                    return false;
                }

                VerbPropertiesCE overrideVerb = null;

                // Per-weapon override (higher priority)
                if (customTrait.verbsOverridesCE != null &&
                    customTrait.verbsOverridesCE.TryGetValue(instance.parent.def, out List<VerbPropertiesCE> perWeapon) &&
                    perWeapon is { Count: > 0 })
                {
                    overrideVerb = perWeapon[0];
                }
                // Generic fallback
                else if (customTrait.verbsOverrideCE is { Count: > 0 })
                {
                    overrideVerb = customTrait.verbsOverrideCE[0];
                }

                if (overrideVerb != null)
                {
                    compEq.PrimaryVerb.verbProps = overrideVerb;
                    return true;
                }

                return false;
            }
        }
    }
}
