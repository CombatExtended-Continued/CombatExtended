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
        public DamageDef def;
        public int amount;

        public DamageInfo GetDinfo()
        {
            return new DamageInfo(def, amount);
        }

        public DamageInfo GetDinfo(DamageInfo primaryDinfo)
        {
            var dinfo = new DamageInfo(def,
                            amount,
                            primaryDinfo.ArmorPenetrationInt, //Armor Penetration TODO: Fix this after DamageWorker restructuring.
                            primaryDinfo.Angle,
                            primaryDinfo.Instigator,
                            primaryDinfo.HitPart,
                            primaryDinfo.Weapon);
            dinfo.SetBodyRegion(primaryDinfo.Height, primaryDinfo.Depth);
            return dinfo;
        }
    }
}