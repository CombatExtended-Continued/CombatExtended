using HarmonyLib;
using Verse;
using RimWorld;
using System.Linq;

namespace CombatExtended.HarmonyCE;

/// <summary>
/// Prevent using ranged verbs other than binoculars (artillery spotting) for shield belt users.
/// </summary>
[HarmonyPatch(typeof(CompShield), nameof(CompShield.CompAllowVerbCast))]
internal static class CompShield_PatchCompAllowVerbCast
{
    internal static bool Prefix(ref bool __result, Verb verb, CompShield __instance)
    {
        if (__instance.Props.blocksRangedWeapons)
        {
            __result = (__instance.ShieldState != ShieldState.Active) || verb is Verb_MarkForArtillery || !(verb is Verb_LaunchProjectileCE || verb is Verb_LaunchProjectile);
        }
        else
        {
            // Let pawns use ranged weapons with Biotech's ranged shield belts
            __result = true;
        }

        return false;
    }
}

[HarmonyPatch(typeof(CompShield), nameof(CompShield.PostPreApplyDamage))]
internal static class CompShield_PatchCheckPreAbsorbDamage
{
    internal static bool Prefix(out bool absorbed, DamageInfo dinfo, CompShield __instance)
    {
        absorbed = false;

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
            absorbed = true;
            return false;
        }
        if (dinfo.Def.ignoreShields)
        {
            return false;
        }
        if (dinfo.Def.isRanged || dinfo.Def.isExplosive)
        {
            absorbed = true;
            __instance.energy -= dinfo.Amount * __instance.Props.energyLossPerDamage * (isEMP ? (1 + bc) : 1);
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

[HarmonyPatch(typeof(CompShield), nameof(CompShield.CompTick))]
internal static class CompShield_DisableOnOperateTurret
{
    private const int SHORT_SHIELD_RECHARGE_TIME = 2 * GenTicks.TicksPerRealSecond;
    internal static void Postfix(CompShield __instance, ref int ___ticksToReset)
    {
        if (!Controller.settings.TurretsBreakShields)
        {
            return;
        }
        if (__instance.PawnOwner?.CurJobDef == JobDefOf.ManTurret && (__instance.PawnOwner?.jobs?.curDriver?.OnLastToil ?? false))
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
