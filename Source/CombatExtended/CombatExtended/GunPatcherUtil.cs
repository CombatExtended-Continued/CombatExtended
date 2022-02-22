using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;


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
            return
                preset.WarmupRange != null
                &&
                preset.DamageRange != null
                &&
                preset.RangeRange != null
                &&
                preset.ProjSpeedRange != null
                &&
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
}
