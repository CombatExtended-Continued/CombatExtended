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

        public SimpleCurve MassCurve;
        public float Mass;

        public SimpleCurve rangeCurve;

        public SimpleCurve warmupCurve;

        public SimpleCurve cooldownCurve;

        public List<StatModifier> MiscOtherStats;

        public float Bulk;

        public float Spread;

        public float Sway;

        public AmmoSetDef setCaliber;

        public bool reloadOneAtATime = false;

        public CompProperties_FireModes fireModes;

        public bool addBipods;

        public string bipodTag;
        #endregion

        #region Def matching

        public List<string> tags;

        public List<string> names;

        public FloatRange RangeRange;

        public FloatRange WarmupRange;

        public FloatRange DamageRange
        {
            get
            {
                var result = damageRange;

                if (CaliberRanges != null)
                {
                    if (CaliberRanges.Any(x => x.DamageRange.max > result.max))
                    {
                        result.max = CaliberRanges.MaxBy(z => z.DamageRange.max).DamageRange.max;
                    }
                    if (CaliberRanges.Any(x => x.DamageRange.min < result.min))
                    {
                        result.min = CaliberRanges.MinBy(z => z.DamageRange.min).DamageRange.min;
                    }
                }

                return result;
            }
        }

        public FloatRange damageRange;

        public FloatRange ProjSpeedRange
        {
            get
            {
                var result = projSpeedRange;

                if (CaliberRanges != null)
                {
                    if (CaliberRanges.Any(x => x.SpeedRange.max > result.max))
                    {
                        result.max = CaliberRanges.MaxBy(z => z.SpeedRange.max).SpeedRange.max;
                    }
                    if (CaliberRanges.Any(x => x.SpeedRange.min < result.min))
                    {
                        result.min = CaliberRanges.MinBy(z => z.SpeedRange.min).SpeedRange.min;
                    }
                }

                return result;
            }
        }

        public FloatRange projSpeedRange;

        public bool DiscardDesignations;

        public List<LabelGun> specialGuns = new List<LabelGun>();


        #endregion

        #region caliber fields

        public bool DetermineCaliber;

        public List<CaliberFloatRange> CaliberRanges = new List<CaliberFloatRange>();

        #endregion
    }
}
