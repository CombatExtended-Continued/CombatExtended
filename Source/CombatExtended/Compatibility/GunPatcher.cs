using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using CombatExtended.Compatibility;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class GunPatcher
    {
        static GunPatcher()
        {
            var unpatchedGuns = DefDatabase<ThingDef>.AllDefs.Where(x => !(x.weaponTags?.Contains("Patched") ?? true) && (x.thingClass == typeof(ThingWithComps) || x.thingClass == typeof(Thing)) && x.IsRangedWeapon &&
                                                                    (
                                                                        (x.verbs?.Any(x => !(x is VerbPropertiesCE)) ?? false)
                                                                        &&
                                                                        (
                                                                            ((x.verbs?.FirstOrFallback()?.defaultProjectile.thingClass ?? null) == typeof(Bullet))
                                                                            ||
                                                                            ((x.verbs?.FirstOrFallback()?.defaultProjectile.thingClass ?? null) == typeof(Projectile_Explosive))
                                                                    )));

            var patcherDefs = DefDatabase<GunPatcherPresetDef>.AllDefs;

            var toolMissers = DefDatabase<ThingDef>.AllDefs.Where(x => x.tools != null && x.tools.All(y => !(y is ToolCE)));

            foreach (var preset in patcherDefs)
            {
                unpatchedGuns.PatchGunsFromPreset(preset);
            }

            if (unpatchedGuns.Count() > 0)
            {
                foreach (var gun in unpatchedGuns)
                {
                        gun.PatchGunFromPreset
                            (
                             patcherDefs.MaxBy
                             (
                              //random range is there to avoid elements with same values
                              x =>
                              (
                               x.DamageRange.Average
                               +
                               x.RangeRange.Average
                               +
                               x.ProjSpeedRange.Average
                               +
                               x.WarmupRange.Average
                               )));
                }
            }

            foreach (var toolMisser in toolMissers)
            {
                List<Tool> newTools = new List<Tool>();
                foreach(var tool in toolMisser.tools)
                {
                    newTools.Add(tool.ConvertTool());
                }

                toolMisser.tools = newTools;
            }
        }
    }
}
