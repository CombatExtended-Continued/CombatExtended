using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class AmmoDef : ThingDef
    {
        public AmmoCategoryDef ammoClass;
        public int defaultAmmoCount = 1;
        public float cookOffSpeed = 1f;
        public float cookOffFlashScale = 1;
        public ThingDef cookOffProjectile = null;
        public SoundDef cookOffSound = null;
        public SoundDef cookOffTailSound = null;

        private List<ThingDef> users;
        public List<ThingDef> Users
        {
            get
            {
                if (users == null)
                {
                    users = CE_Utility.allWeaponDefs.FindAll(delegate(ThingDef def) 
                    {
                        CompProperties_AmmoUser props = def.GetCompProperties<CompProperties_AmmoUser>();
                        return props != null && props.ammoSet.ammoTypes.Any(x => x.ammo == this);
                    });
                }
                return users;
            }
        }
    }
}
