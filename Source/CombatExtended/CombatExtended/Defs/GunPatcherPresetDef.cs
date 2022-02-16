using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
    public static class GunPatcherUtil
    {
        public static bool DiscardDesignationsMatch(this ThingDef thingDef, GunPatcherPresetDef presetDef)
        {
            bool result = false;

            if (presetDef.DiscardDesignations)
            {
                string[] labels = thingDef.label.Replace("A", " ").ToLower().Replace("-", "").Split(' ');

                return labels.Any(x => presetDef.names.Contains(x));
            }

            return result;
        }

        public static bool MatchesVerbProps(this ThingDef gun, GunPatcherPresetDef preset)
        {
            //Log.Message((gun.Verbs.FirstOrFallback()?.warmupTime ?? 0f).ToString());
            //Log.Message((gun.Verbs.FirstOrFallback()?.range ?? 0f).ToString());
            return 
                preset.WarmupRange.Includes(gun.Verbs.FirstOrFallback()?.warmupTime ?? 0f)
                &&
                preset.RangeRange.Includes(gun.Verbs.FirstOrFallback()?.range ?? 0f)
                &&
                preset.DamageRange.Includes(gun.Verbs.FirstOrFallback()?.defaultProjectile.projectile.GetDamageAmount(1f) ?? 0f)
                &&
                preset.ProjSpeedRange.Includes(gun.Verbs.FirstOrFallback()?.defaultProjectile.projectile.speed ?? 0f)
                ;
        }

        public static void AddOrChangeStat(this ThingDef gun, StatModifier mod)
        {
            if (gun.statBases.Any(x => x.stat == mod.stat))
            {
                gun.statBases.Find(x => x.stat == mod.stat).value = mod.value;
            }
            else
            {
                gun.statBases.Add(mod);
            }
        }
    }

    [StaticConstructorOnStartup]
    public class GunPatcher
    {
        static GunPatcher()
        {
            var unpatchedGuns = DefDatabase<ThingDef>.AllDefs.Where(x => x.IsRangedWeapon && (x.verbs?.Any(x => !(x is VerbPropertiesCE)) ?? false));

            foreach (var preset in DefDatabase<GunPatcherPresetDef>.AllDefs)
            {
                var matchingGuns = unpatchedGuns.Where
                    (
                    x =>
                    // name checking
                    (x.label.ToLower().Replace("-", "").Split(' ').Any(y => preset.names?.Contains(y) ?? false))
                    |
                    (preset.names?.Contains(x.label.ToLower()) ?? false)
                    |
                    x.DiscardDesignationsMatch(preset)
                    |
                    x.MatchesVerbProps(preset)
                    );

                foreach (var gun in matchingGuns)
                {
                    Log.Message(gun.label);

                    gun.Verbs.Clear();

                    gun.verbs.Clear();

                    gun.verbs.Add(preset.gunStats.MemberwiseClone());

                    if (gun.comps == null)
                    {
                        gun.comps = new List<CompProperties>();
                    }

                    gun.comps.Add(new CompProperties_AmmoUser
                    {
                        ammoSet = preset.setCaliber,
                        reloadTime = preset.ReloadTime,
                        magazineSize = preset.AmmoCapacity,
                        reloadOneAtATime = preset.reloadOneAtATime
                        
                    });

                    gun.comps.Add(preset.fireModes);

                    gun.AddOrChangeStat(new StatModifier { stat = StatDefOf.Mass, value = preset.Mass });

                    gun.AddOrChangeStat(new StatModifier { stat = CE_StatDefOf.Bulk, value = preset.Bulk });

                    gun.AddOrChangeStat(new StatModifier { stat = StatDefOf.RangedWeapon_Cooldown, value = preset.CooldownTime });
                }
            }
        }
    }

    public class GunPatcherPresetDef : Def
    {
        public VerbPropertiesCE gunStats;

        public float ReloadTime;

        public int AmmoCapacity;

        public float CooldownTime;

        public float Bulk;

        public float Mass;

        public AmmoSetDef setCaliber;

        public bool reloadOneAtATime = false;

        public CompProperties_FireModes fireModes;

        #region Def matching

        public List<string> tags;

        public List<string> names;

        public FloatRange RangeRange;

        public FloatRange WarmupRange;

        public FloatRange DamageRange;

        public FloatRange ProjSpeedRange;

        public bool DiscardDesignations;
        #endregion
    }
}
