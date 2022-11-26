﻿using System;
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

        public static void MergeStatLists(this ThingDef gun, List<StatModifier> mods)
        {
            foreach (StatModifier mod in mods)
            {
                gun.AddOrChangeStat(mod);
            }
        }

        public static void PatchGunsFromPreset(this IEnumerable<ThingDef> unpatchedGuns, GunPatcherPresetDef preset)
        {

            foreach (var gun in unpatchedGuns)
            {
                try
                {
                    bool shouldPatch = false;
                    if (gun.label.ToLower().Replace("-", "").Split(' ').Any(y => preset.names?.Contains(y) ?? false))
                    {
                        shouldPatch = true;
                    }
                    else if (preset.names?.Contains(gun.label.ToLower()) ?? false)
                    {
                        shouldPatch = true;
                    }
                    else if (gun.DiscardDesignationsMatch(preset))
                    {
                        shouldPatch = true;
                    }
                    else if (gun.MatchesVerbProps(preset))
                    {
                        shouldPatch = true;
                    }
                    else if (preset.tags != null && gun.weaponTags != null && preset.tags.Intersect(gun.weaponTags).Any())
                    {
                        shouldPatch = true;
                    }
                    else if (preset.specialGuns.Any
                             (
                                 y =>
                                 y.names.Contains(gun.label)
                                 ||
                                 y.names.Intersect(gun.label.ToLower().Replace("-", "").Split(' ')).Any()
                             )
                            )
                    {
                        shouldPatch = true;
                    }

                    if (shouldPatch)
                    {
                        gun.PatchGunFromPreset(preset);
                    }
                }
                catch (Exception e)
                {
                    Log.messageQueue.Enqueue(new LogMessage(LogMessageType.Error, "" + e, StackTraceUtility.ExtractStringFromException(e)));
                    Log.Error($"Unhandled exception patching gun {gun} from preset {preset}");
                }
            }
        }

        public static void PatchGunFromPreset(this ThingDef gun, GunPatcherPresetDef preset)
        {
            if (Controller.settings.DebugAutopatcherLogger)
            {
                Log.Message($"Auto-patching {gun} ({gun.label})");
            }

            var OldProj = gun.Verbs[0].defaultProjectile;

            var OldVerb = gun.Verbs[0];

            var OldBaseList = gun.statBases.ListFullCopy();

            var oldVerbs = gun.verbs;

            var oldComps = gun.comps;

            var oldTools = gun.tools;

            var oldTags = gun.weaponTags;
            gun.weaponTags = new List<string>(oldTags);

            gun.verbs = new List<VerbProperties>();

            gun.verbs.Add(preset.gunStats.MemberwiseClone());

            #region VerbProps curves
            if (preset.rangeCurve != null)
            {
                gun.verbs[0].range = preset.rangeCurve.Evaluate(OldVerb.range);
            }

            if (preset.warmupCurve != null)
            {
                gun.verbs[0].warmupTime = preset.warmupCurve.Evaluate(OldVerb.warmupTime);
            }
            #endregion

            if (gun.comps == null)
            {
                gun.comps = new List<CompProperties>();
            }
            else
            {
                gun.comps = new List<CompProperties>(gun.comps);
            }

            float finalMass = preset.Mass;

            float finalBulk = preset.Bulk;
            float FinalCooldown = preset.CooldownTime;


            LabelGun specialGun = null;
            try
            {
                #region stat curves

                if (preset.MassCurve != null)
                {
                    finalMass = preset.MassCurve.Evaluate(gun.GetStatValueAbstract(StatDefOf.Mass));
                }

                #endregion

                if (preset.specialGuns.Any(x => x.names.Intersect(gun.label.ToLower().Replace("-", "").Split(' ').ToList<string>()).Any()))
                {
                    specialGun = preset.specialGuns.Find(x => x.names.Intersect(gun.label.ToLower().Replace("-", "").Split(' ').ToList<string>()).Any());
                    gun.comps.Add(new CompProperties_AmmoUser
                    {
                        ammoSet = specialGun.caliber,
                        reloadTime = specialGun.reloadTime,
                        magazineSize = specialGun.magCap,
                        reloadOneAtATime = preset.reloadOneAtATime

                    });



                    finalMass = specialGun.mass;

                    finalBulk = specialGun.bulk;

                    Log.Message(specialGun.names.First().Colorize(Color.yellow));
                }
                else
                {
                    gun.comps.Add(new CompProperties_AmmoUser
                    {
                        ammoSet = preset.setCaliber,
                        reloadTime = preset.ReloadTime,
                        magazineSize = preset.AmmoCapacity,
                        reloadOneAtATime = preset.reloadOneAtATime

                    });
                }

                if (preset.DetermineCaliber)
                {
                    var comp = gun.comps.Find(x => x is CompProperties_AmmoUser) as CompProperties_AmmoUser;

                    comp.ammoSet = preset.CaliberRanges.Find
                                   (
                                       x =>
                                       x.DamageRange.Includes(OldProj.projectile.GetDamageAmount(1f))
                                       &&
                                       x.SpeedRange.Includes(OldProj.projectile.speed)
                                   )?.AmmoSet ?? comp.ammoSet;
                }

                if (preset.cooldownCurve != null)
                {
                    FinalCooldown = preset.cooldownCurve.Evaluate(gun.GetStatValueAbstract(StatDefOf.RangedWeapon_Cooldown));
                }

                if (preset.addTags != null)
                {
                    foreach (string tag in preset.addTags)
                    {
                        gun.weaponTags.Add(tag);
                    }
                }
                #region patching tools

                if (gun.tools != null)
                {
                    var newtools = new List<Tool>();

                    foreach (Tool tool in gun.tools)
                    {
                        if (tool is ToolCE)
                        {
                            newtools.Add(tool);
                        }
                        else
                        {
                            Tool newTool;
                            try
                            {
                                newTool = tool.ConvertTool();
                            }
                            catch
                            {
                                newTool = tool;
                                Log.Warning($"Failed to convert {tool} to ToolCE");
                            }
                            newtools.Add(newTool);
                        }
                    }
                    gun.tools = newtools;
                }

                #endregion

            }
            catch (Exception e)
            {
                try
                {
                    Log.messageQueue.Enqueue(new LogMessage(LogMessageType.Error, "" + e, StackTraceUtility.ExtractStringFromException(e)));
                }
                catch (Exception e1)
                {
                    Log.Error($"Cannot extract stack trace from {e}: Failed with exception {e1}");
                }
                Log.Error($"Failed auto patching {gun}.  Rolling back");
                gun.comps = oldComps;
                gun.tools = oldTools;
                gun.verbs = oldVerbs;
                gun.weaponTags = oldTags;
            }
            #region addings stats
            gun.comps.Add(preset.fireModes);
            gun.AddOrChangeStat(new StatModifier { stat = StatDefOf.Mass, value = finalMass });

            gun.AddOrChangeStat(new StatModifier { stat = CE_StatDefOf.Bulk, value = finalBulk });

            gun.AddOrChangeStat(new StatModifier { stat = StatDefOf.RangedWeapon_Cooldown, value = FinalCooldown });

            gun.statBases.RemoveAll(x => x.stat.label.ToLower().Contains("accuracy"));

            gun.statBases.Add(new StatModifier { stat = CE_StatDefOf.ShotSpread, value = preset.Spread });

            gun.statBases.Add(new StatModifier { stat = CE_StatDefOf.SwayFactor, value = preset.Sway });
            #endregion

            #region misc stats
            if (preset.MiscOtherStats != null)
            {
                gun.statBases.AddRange(preset.MiscOtherStats);
            }

            if (specialGun != null)
            {
                if (specialGun.stats != null)
                {
                    gun.MergeStatLists(specialGun.stats);
                }
            }
            #endregion

            #region bipods
            if (preset.addBipods)
            {
                var bipodDef = DefDatabase<BipodCategoryDef>.AllDefsListForReading.Find(x => x.bipod_id == preset.bipodTag);

                if (bipodDef != null)
                {
                    gun.comps.Add(new CompProperties_BipodComp
                    {
                        catDef = bipodDef,
                        warmupPenalty = bipodDef.warmup_mult_NOT_setup,
                        warmupMult = bipodDef.warmup_mult_setup,
                        ticksToSetUp = bipodDef.setuptime,
                        recoilMultoff = bipodDef.recoil_mult_NOT_setup,
                        recoilMulton = bipodDef.recoil_mult_setup,
                        additionalrange = bipodDef.ad_Range,
                        swayMult = bipodDef.swayMult,
                        swayPenalty = bipodDef.swayPenalty
                    });
                    gun.statBases.Add(new StatModifier { value = 0f, stat = BipodDefsOfs.BipodStats });
                }
            }
            #endregion
        }
    }
}
