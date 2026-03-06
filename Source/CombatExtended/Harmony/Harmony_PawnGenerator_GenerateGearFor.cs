using HarmonyLib;
using Verse;

namespace CombatExtended.HarmonyCE;
[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GenerateGearFor))]
public static class Harmony_PawnGenerator_GenerateGearFor
{
    public static void Postfix(Pawn pawn)
    {
        if (pawn?.equipment?.Primary is ThingWithComps weapon && weapon.IsTwoHandedWeapon())
        {
            if (pawn.apparel != null)
            {
                var list = pawn.apparel.wornApparel;
                // Reverse loop to prevent removal issues
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i] is Apparel_Shield shield)
                    {
                        Log.Warning($"Combat Extended :: Removing shield from {pawn.kindDef.defName} as they generated with shield and two-handed weapon.");
                        if (!shield.Destroyed)
                        {
                            shield.Destroy();
                        }
                    }
                }
            }
        }
    }
}

