using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.AI;
public static class CombatRoleUtility
{
    #region Constants

    public const string AssaultTag = "CE_AI_AssaultWeapon";
    public const string PistolTag = "CE_AI_Pistol";
    public const string RifleTag = "CE_AI_Rifle";
    public const string SuppressTag = "CE_AI_Suppressive";
    public const string GrenadeTag = "CE_AI_Grenade";
    public const string NonlethalTag = "CE_AI_Nonlethal";
    public const string LauncherTag = "CE_AI_Launcher";

    #endregion

    #region Methods

    #region Role Selection

    public static bool CanDoRole(this Pawn pawn, CombatRole role)
    {
        CompInventory comp = pawn.TryGetComp<CompInventory>();
        if (comp == null)
        {
            return false;
        }
        switch (role)
        {
            default:
                return false;
            case CombatRole.Melee:
                return CanBeMelee(pawn, comp);
            case CombatRole.Assault:
                return CanBeAssault(pawn, comp);
            case CombatRole.Sniper:
                return CanBeSniper(pawn, comp);
            case CombatRole.Suppressor:
                return CanBeSuppressor(pawn, comp);
            case CombatRole.Grenadier:
                return CanBeGrenadier(pawn, comp);
            case CombatRole.Rocketeer:
                return CanBeRocketeer(pawn, comp);
        }
    }

    /// <summary>
    /// Determines whether a pawn is elligible to be used as a melee combatant by its squad
    /// </summary>
    /// <returns>True if the pawn has a melee weapon and no ranged weapons with ammo, false otherwise</returns>
    private static bool CanBeMelee(Pawn pawn, CompInventory comp)
    {
        bool hasMelee = !comp.meleeWeaponList.NullOrEmpty();
        bool hasRanged = comp.rangedWeaponList.Any(w => w.HasAmmo());
        return hasMelee && !hasRanged;
    }

    /// <summary>
    /// Determines whether a pawn is elligible to be used as a charging flanker unit by its squad
    /// </summary>
    /// <returns>True if the pawn has a gun tagged as assault weapon (e.g. SMG or shotgun) or their only ranged weapon is a pistol (with ammo), false otherwise</returns>
    private static bool CanBeAssault(Pawn pawn, CompInventory comp)
    {
        if (comp.rangedWeaponList.NullOrEmpty())
        {
            return false;
        }
        bool hasPistol = false;
        bool hasNonPistol = false;
        foreach (ThingWithComps gun in comp.rangedWeaponList)
        {
            if (gun.def.IsAssaultGun() && gun.HasAmmo())
            {
                return true;
            }
            if (gun.def.IsPistol())
            {
                hasPistol = hasPistol || gun.HasAmmo();
            }
            else
            {
                hasNonPistol = hasNonPistol || gun.HasAmmo();
            }
        }
        return hasPistol && !hasNonPistol;
    }

    /// <summary>
    /// Determines whether a pawn is elligible to be used as a sniper, going for kills on high value targets
    /// </summary>
    /// <returns>True if pawn has a weapon tagged as rifle with ammo available, false otherwise</returns>
    private static bool CanBeSniper(Pawn pawn, CompInventory comp)
    {
        return comp.rangedWeaponList.Any(w => w.def.IsRifle() && w.HasAmmo());
    }

    /// <summary>
    /// Determines whether a pawn can be used to suppress targets
    /// </summary>
    /// <returns>True if the pawn has a weapon tagged for suppression and ammo available, false otherwise</returns>
    private static bool CanBeSuppressor(Pawn pawn, CompInventory comp)
    {
        return comp.rangedWeaponList.Any(w => w.def.IsSuppressive() && w.HasAmmo());
    }

    /// <summary>
    /// Determines whether a pawn has grenades they can use against a target
    /// </summary>
    /// <returns>True if the pawn has at least one destructive grenade in their inventory, false otherwise</returns>
    private static bool CanBeGrenadier(Pawn pawn, CompInventory comp)
    {
        return comp.rangedWeaponList.Any(w => w.def.IsGrenade() && w.def.IsLethal());
    }

    /// <summary>
    /// Determines whether a pawn can use a rocket/grenade launcher against fortified targets
    /// </summary>
    /// <returns>True if the pawn has a rocket/grenade launcher with ammo in their inventory, false otherwise</returns>
    private static bool CanBeRocketeer(Pawn pawn, CompInventory comp)
    {
        return comp.rangedWeaponList.Any(w => w.def.IsLauncher() && w.HasAmmo());
    }

    #endregion

    #region Gun Extension Methods

    public static bool IsPistol(this ThingDef def)
    {
        return def.weaponTags.Contains(PistolTag);
    }

    public static bool IsAssaultGun(this ThingDef def)
    {
        return def.weaponTags.Contains(AssaultTag);
    }

    public static bool IsRifle(this ThingDef def)
    {
        return def.weaponTags.Contains(RifleTag);
    }

    public static bool IsSuppressive(this ThingDef def)
    {
        return def.weaponTags.Contains(SuppressTag);
    }

    public static bool IsGrenade(this ThingDef def)
    {
        return def.weaponTags.Contains(GrenadeTag);
    }

    public static bool IsLauncher(this ThingDef def)
    {
        return def.weaponTags.Contains(LauncherTag);
    }

    public static bool IsLethal(this ThingDef def)
    {
        return !def.weaponTags.Contains(NonlethalTag);
    }

    #endregion

    /// <summary>
    /// Finds the most damaging gun and grenade in a pawn's inventory for a given minimum range
    /// </summary>
    /// <param name="pawn">The Pawn whose inventory to search through</param>
    /// <param name="range">The minimum range the returned weapons must have, set to negative for unlimited</param>
    /// <returns>Pair where First is the highest DPS gun and Second is the highest damage/radius grenade in inventory, Pair with null values if no gun/grenade with the given range could be found or the Pawn has no CompInventory</returns>
    internal static Pair<ThingWithComps, ThingWithComps> GetBestGunAndGrenadeFor(Pawn pawn, int range = -1)
    {
        CompInventory inventory = pawn.TryGetComp<CompInventory>();
        if (inventory == null)
        {
            Log.Error("CE tried calculating best gun and grenade for " + pawn.ToString() + " without CompInventory");
            return new Pair<ThingWithComps, ThingWithComps>();
        }
        ThingWithComps bestGun = null;
        ThingWithComps bestGrenade = null;
        float maxGunScore = 0f;
        float maxGrenadeScore = 0f;
        foreach (ThingWithComps curWeapon in inventory.rangedWeaponList)
        {
            CompEquippable compEq = curWeapon.TryGetComp<CompEquippable>();
            if (compEq.PrimaryVerb.verbProps.range > range && curWeapon.def.IsLethal())
            {
                float score = 0f;
                bool isGrenade = curWeapon.def.IsGrenade();
                if (isGrenade)
                {
                    // Score the grenade based on damage and explosion radius and compare against stored max
                    ThingDef grenade = compEq.PrimaryVerb.verbProps.projectileDef;
                    score = grenade.projectile.damageAmountBase * grenade.projectile.explosionRadius * (grenade.HasComp(typeof(CompExplosiveCE)) ? 2 : 1);
                    if (score > maxGrenadeScore)
                    {
                        bestGrenade = curWeapon;
                    }
                }
                else
                {
                    // Score the gun based on DPS and compare against stored max
                    Verb verb = compEq.PrimaryVerb;
                    ThingDef bullet = verb as Verb_LaunchProjectileCE == null ? verb.verbProps.projectileDef : (verb as Verb_LaunchProjectileCE).ProjectileDef;
                    float secPerBurst = verb.verbProps.warmupTime + curWeapon.GetStatValue(StatDefOf.RangedWeapon_Cooldown) + (verb.verbProps.ticksBetweenBurstShots * verb.verbProps.burstShotCount);  // Overall time for one burst
                    score = bullet.projectile.damageAmountBase * verb.verbProps.burstShotCount / secPerBurst;
                    if (score > maxGunScore)
                    {
                        bestGun = curWeapon;
                    }
                }
            }
        }
        return new Pair<ThingWithComps, ThingWithComps>(bestGun, bestGrenade);
    }

    #endregion
}
