﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace CombatExtended
{
    [DefOf]
    public static class CE_StatDefOf
    {
        // *** Item stats ***
        public static readonly StatDef Bulk = StatDef.Named("Bulk"); // for items in inventory
        public static readonly StatDef WornBulk = StatDef.Named("WornBulk"); // worn apparel

        // *** Weapon stats ***
        public static StatDef StuffEffectMultiplierToughness;
        public static StatDef ToughnessRating;

        // *** Ranged weapon stats ***
        public static readonly StatDef ShotSpread = StatDef.Named("ShotSpread"); // pawn capacity
        public static readonly StatDef SwayFactor = StatDef.Named("SwayFactor"); // pawn capacity
        public static StatDef SightsEfficiency;
        public static readonly StatDef AimingAccuracy = StatDef.Named("AimingAccuracy"); // pawn capacity
        public static readonly StatDef ReloadSpeed = StatDef.Named("ReloadSpeed"); // pawn capacity
        public static readonly StatDef MuzzleFlash = StatDef.Named("MuzzleFlash");
        public static readonly StatDef MagazineCapacity = StatDef.Named("MagazineCapacity");
        public static readonly StatDef AmmoGenPerMagOverride = StatDef.Named("AmmoGenPerMagOverride");
        public static readonly StatDef NightVisionEfficiency_Weapon = StatDef.Named("NightVisionEfficiency_Weapon");
        public static StatDef TicksBetweenBurstShots;
        public static StatDef BurstShotCount;
        public static StatDef Recoil;
        public static StatDef ReloadTime;
        public static StatDef OneHandedness;

        // *** Melee weapon stats ***
        public static StatDef MeleePenetrationFactor;
        public static StatDef MeleeCounterParryBonus;

        // *** Pawn stats ***
        public static StatDef CarryBulk;    // Inventory max space
        public static StatDef CarryWeight;  // Inventory max weight
        public static StatDef MeleeCritChance;
        public static StatDef MeleeDodgeChance;
        public static StatDef MeleeParryChance;
        public static StatDef UnarmedDamage;
        public static StatDef BodyPartSharpArmor;
        public static StatDef BodyPartBluntArmor;
        public static StatDef AverageSharpArmor;
        public static StatDef NightVisionEfficiency;

        public static StatDef SmokeSensitivity;

        public static StatDef Suppressability;

        public static StatDef ArmorRating_Electric;
    }
}
