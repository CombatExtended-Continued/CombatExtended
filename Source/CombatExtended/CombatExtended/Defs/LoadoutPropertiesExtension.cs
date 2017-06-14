using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.CombatExtended.Defs
{
    public class LoadoutPropertiesExtension : DefModExtension
    {
        public FloatRange primaryMagazineCount = FloatRange.Zero;
        public FloatRange shieldMoney = FloatRange.Zero;
        public List<string> shieldTags;
        public float shieldChance = 0;
        public bool allowAdvancedAmmo = false;
        public SidearmOption forcedSidearm;
        public List<SidearmOption> sidearms;
    }
}
