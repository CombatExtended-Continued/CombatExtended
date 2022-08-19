using HarmonyLib;
using Verse;
using RimWorld;
using System.Linq;

namespace CombatExtended.HarmonyCE
{

    /// <summary>
    /// Prevent using ranged verbs other than binoculars (artillery spotting) for shield belt users.
    /// </summary>
    [HarmonyPatch(typeof(ShieldBelt), nameof(ShieldBelt.AllowVerbCast))]
    internal static class ShieldBelt_PatchAllowVerbCast
    {
        internal static bool Prefix(ref bool __result, Verb verb, ShieldBelt __instance)
        {
            __result = (__instance.ShieldState != ShieldState.Active) || verb is Verb_MarkForArtillery || !(verb is Verb_LaunchProjectileCE || verb is Verb_LaunchProjectile);
            return false;
        }
    }

    [HarmonyPatch(typeof(ShieldBelt), nameof(ShieldBelt.CheckPreAbsorbDamage))]
    internal static class ShieldBelt_PatchCheckPreAbsorbDamage
    {
        internal static bool Prefix(ref bool __result, DamageInfo dinfo, ShieldBelt __instance)
        {
	    __result = false;

	    if (__instance.ShieldState != ShieldState.Active)
	    {
		return false;
	    }
	    float bc = 1.0f;
	    bool isEMP = dinfo.Def == DamageDefOf.EMP;
	    if (dinfo.Weapon?.projectile is ProjectilePropertiesCE pce)
	    {
		bc = pce.empShieldBreakChance;
		isEMP = isEMP || pce.secondaryDamage?.FirstOrDefault(sd => sd.def == DamageDefOf.EMP) != null;
	    }
	    if (isEMP && Rand.Chance(bc))
	    {
		__instance.energy = 0f;
		__instance.Break();
		__result = true;
		return false;
	    }
	    if (dinfo.Def.isRanged || dinfo.Def.isExplosive)
	    {
		__result = true;
		__instance.energy -= dinfo.Amount * __instance.EnergyLossPerDamage * (isEMP ? (1+bc) : 1);
		if (__instance.energy < 0f)
		{
		    __instance.Break();
		}
		else
		{
		    __instance.AbsorbedDamage(dinfo);
		}
	    }
	    return false;
	}
    }

    [HarmonyPatch(typeof(ShieldBelt), "Tick")]
    internal static class ShieldBelt_DisableOnOperateTurret
    {
        private const int SHORT_SHIELD_RECHARGE_TIME = 2 * GenTicks.TicksPerRealSecond;
        internal static void Postfix(ShieldBelt __instance, ref int ___ticksToReset, int ___StartingTicksToReset)
        {
            if (!Controller.settings.TurretsBreakShields)
            {
                return;
            }
            if (__instance.Wearer?.CurJob?.def == JobDefOf.ManTurret && (__instance.Wearer?.jobs?.curDriver?.OnLastToil ?? false))
            {
                if (__instance.ShieldState == ShieldState.Active)
                {
                    __instance.Break();
                    ___ticksToReset = SHORT_SHIELD_RECHARGE_TIME;
                }
                if (___ticksToReset < SHORT_SHIELD_RECHARGE_TIME)
                {
                    ___ticksToReset = SHORT_SHIELD_RECHARGE_TIME;
                }
            }
        }
    }
}
