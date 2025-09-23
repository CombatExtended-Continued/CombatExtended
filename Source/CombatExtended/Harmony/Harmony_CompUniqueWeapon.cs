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
                foreach (WeaponTraitDef trait in __instance.traits)
                {
                    if (trait is CustomWeaponTraitDef { underBarrelProps: not null } customTrait)
                    {
                        CompUnderBarrel compUnderBarrel = __instance.parent.TryGetComp<CompUnderBarrel>();
                        compUnderBarrel.props = customTrait.underBarrelProps;
                    }
                }

            }
        }
    }
}
