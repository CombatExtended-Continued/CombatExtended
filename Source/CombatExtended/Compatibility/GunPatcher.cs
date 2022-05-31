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
            var unpatchedGuns = DefDatabase<ThingDef>.AllDefs.Where(x => x.IsRangedWeapon && (x.verbs?.Any(x => !(x is VerbPropertiesCE)) ?? false));

            var patcherDefs = DefDatabase<GunPatcherPresetDef>.AllDefs;

            var toolMissers = DefDatabase<ThingDef>.AllDefs.Where(x => x.tools != null && x.tools.Any(y => !(y is ToolCE)));

            foreach (var preset in patcherDefs)
            {
                unpatchedGuns.PatchGunsFromPreset(preset);
            }

            if (unpatchedGuns.Count() > 0)
            {
                foreach (var gun in unpatchedGuns)
                {
                    gun.PatchGunFromPreset(
                        patcherDefs.MaxBy
                        (
                            //random range is there to avoid elements with same values
                            x =>
                            (x.DamageRange.Average - (gun.Verbs[0]?.defaultProjectile?.projectile.GetDamageAmount(1f) ?? Rand.Range(-800f, -900f)))
                            +
                            (x.RangeRange.Average - (gun.Verbs[0]?.range ?? Rand.Range(-800f, -900f)))
                            +
                            (x.ProjSpeedRange.Average - (gun.Verbs[0]?.defaultProjectile?.projectile.speed ?? Rand.Range(-800f, -900f)))
                            +
                            (x.WarmupRange.Average - (gun.Verbs[0]?.warmupTime ?? Rand.Range(-800f, -900f)))

                            ));
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
