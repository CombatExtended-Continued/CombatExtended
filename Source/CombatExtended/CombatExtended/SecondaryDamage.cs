using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class SecondaryDamage
    {
        private const float SecExplosionPenPerDmg = 3;

        public DamageDef def;
        public int amount;
        public float chance = 1f;

        public DamageInfo GetDinfo()
        {
            return new DamageInfo(def, amount);
        }

        public DamageInfo GetDinfo(DamageInfo primaryDinfo)
        {
            var penetration = 0f;
            if (def.isExplosive)
                penetration = amount * SecExplosionPenPerDmg;
            else if (def.armorCategory == DamageArmorCategoryDefOf.Sharp)
                penetration = primaryDinfo.ArmorPenetrationInt;

            var dinfo = new DamageInfo(def,
                            amount,
                            penetration,
                            primaryDinfo.Angle,
                            primaryDinfo.Instigator,
                            primaryDinfo.HitPart,
                            primaryDinfo.Weapon);
            dinfo.SetBodyRegion(primaryDinfo.Height, primaryDinfo.Depth);
            return dinfo;
        }
    }
}