using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using System.Xml;

namespace CombatExtended
{
    public class GunPatcherPresetDef : Def
    {
        #region stats
        public VerbPropertiesCE gunStats;

        public float ReloadTime;

        public int AmmoCapacity;

        public float CooldownTime;

        public float Bulk;

        public float Mass;

        public float Spread;

        public AmmoSetDef setCaliber;

        public bool reloadOneAtATime = false;

        public CompProperties_FireModes fireModes;
        #endregion

        #region Def matching

        public List<string> tags;

        public List<string> names;

        public FloatRange RangeRange;

        public FloatRange WarmupRange;

        public FloatRange DamageRange;

        public FloatRange ProjSpeedRange;

        public bool DiscardDesignations;

        public List<LabelGun> specialGuns = new List<LabelGun>();


        #endregion

        #region caliber fields

        public bool DetermineCaliber;

        public List<CaliberFloatRange> CaliberRanges = new List<CaliberFloatRange>();

        #endregion
    }
}
