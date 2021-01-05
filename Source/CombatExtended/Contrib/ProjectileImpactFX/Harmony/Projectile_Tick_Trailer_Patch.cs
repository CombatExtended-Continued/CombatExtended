using Verse;
using HarmonyLib;
using RimWorld;

namespace ProjectileImpactFX.HarmonyInstance
{
    [HarmonyPatch(typeof(Projectile), "Tick")]
    public static class Projectile_Tick_Trailer_Patch
    {
        public static void Postfix(Projectile __instance, int ___ticksToImpact)
        {
            if (__instance != null)
            {
                if (__instance.def.HasModExtension<TrailerProjectileExtension>())
                {
                    TrailerProjectileExtension trailer = __instance.def.GetModExtension<TrailerProjectileExtension>();
                    if (trailer != null)
                    {
                        if (___ticksToImpact % trailer.trailerMoteInterval == 0)
                        {
                            for (int i = 0; i < trailer.motesThrown; i++)
                            {
                                //    TrailThrower.ThrowSmokeTrail(__instance.Position.ToVector3Shifted(), trailer.trailMoteSize, __instance.Map, trailer.trailMoteDef);

                                TrailThrower.ThrowSmoke(__instance.DrawPos, trailer.trailMoteSize, __instance.Map, trailer.trailMoteDef);
                            }
                        }
                    }
                }
            }

        }
    }
}
