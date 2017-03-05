using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.AI
{
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
            if (comp == null) return false;
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
            if (comp.rangedWeaponList.NullOrEmpty()) return false;
            bool hasPistol = false;
            bool hasNonPistol = false;
            foreach (ThingWithComps gun in comp.rangedWeaponList)
            {
                if (gun.def.IsAssaultGun() && gun.HasAmmo()) return true;
                if (gun.def.IsPistol()) hasPistol = hasPistol || gun.HasAmmo();
                else hasNonPistol = hasNonPistol || gun.HasAmmo();
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

        #endregion
    }
}
