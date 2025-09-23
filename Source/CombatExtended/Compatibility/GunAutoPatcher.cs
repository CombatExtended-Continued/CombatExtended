using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using CombatExtended.Compatibility;

namespace CombatExtended;
[StaticConstructorOnStartup]
public class GunAutoPatcher
{
    private static bool shouldPatch(ThingDef thingDef)
    {
        if (thingDef.weaponTags?.Contains("Patched") ?? false)
        {
            return false;
        }
        if (thingDef.thingClass != typeof(ThingWithComps) && thingDef.thingClass != typeof(Thing))
        {
            return false;
        }
        if (!thingDef.IsRangedWeapon)
        {
            return false;
        }
        if (thingDef.verbs is List<VerbProperties> verbs)
        {
            foreach (var verb in verbs)
            {
                if (verb is VerbPropertiesCE)
                {
                    return false;
                }
                if (verb.defaultProjectile?.thingClass is Type tc)
                {
                    if (tc != typeof(Bullet) && tc != typeof(Projectile_Explosive))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                var t = verb.verbClass;
                if (t != typeof(Verb_ShootOneUse) && t != typeof(Verb_Shoot) && t != typeof(Verb_LaunchProjectile) && t != typeof(Verb_LaunchProjectileStatic))
                {
                    return false;
                }

            }
            return true;
        }
        return false;
    }
    static GunAutoPatcher()
    {
        if (!Controller.settings.EnableWeaponAutopatcher)
        {
            return;
        }

        var unpatchedGuns = DefDatabase<ThingDef>.AllDefs.Where(shouldPatch);

        var patcherDefs = DefDatabase<GunPatcherPresetDef>.AllDefs;

        var toolMissers = DefDatabase<ThingDef>.AllDefs.Where(x => x.tools != null && x.tools.All(y => !(y is ToolCE)));

        foreach (var preset in patcherDefs)
        {
            try
            {
                unpatchedGuns.PatchGunsFromPreset(preset);
            }
            catch (Exception e)
            {
                Log.messageQueue.Enqueue(new LogMessage(LogMessageType.Error, "" + e, StackTraceUtility.ExtractStringFromException(e)));
                Log.Error($"Unhandled exception handling {preset}");

            }
        }

        if (unpatchedGuns.Count() > 0)
        {
            foreach (var gun in unpatchedGuns)
            {
                try
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
                catch (Exception e)
                {
                    Log.messageQueue.Enqueue(new LogMessage(LogMessageType.Error, "" + e, StackTraceUtility.ExtractStringFromException(e)));
                    Log.Error($"Unhandled exception patching gun {gun} from preset");
                }
            }
        }

        foreach (var toolMisser in toolMissers)
        {
            List<Tool> newTools = new List<Tool>();
            foreach (var tool in toolMisser.tools)
            {
                Tool newTool;
                try
                {
                    newTool = tool.ConvertTool();
                }
                catch
                {
                    Log.Error($"Failed to autoconvert tool {tool} in {toolMisser}.  Using original");
                    newTool = tool;
                }
                newTools.Add(newTool);
            }

            toolMisser.tools = newTools;
        }
    }
}
