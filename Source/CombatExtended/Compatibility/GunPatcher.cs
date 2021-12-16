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

    [StaticConstructorOnStartup]
    public class GunPatcher
    {
        static GunPatcher()
        {
            IEnumerable<ThingDef> guns = DefDatabase<ThingDef>.AllDefs.Where(x =>
            x.weaponTags != null &&
            !(x.weaponTags.Contains("patched") | x.statBases.Any(o => o.stat == CE_StatDefOf.Bulk)) &&
            x.weaponTags.Any(P => DefDatabase<PresetPatcherDef>.AllDefsListForReading.Any(F => F.ID == P))  
            );

            foreach (ThingDef gun in guns)
            {
                var the_tag = gun.weaponTags.Find(P => DefDatabase<PresetPatcherDef>.AllDefsListForReading.Any(F => F.ID == P));

                PresetPatcherDef the_patcher_def = DefDatabase<PresetPatcherDef>.AllDefsListForReading.Find(F => F.ID == the_tag);

                Log.Message("Patching: " + gun.defName.Colorize(Color.blue) + " because of it's: " + the_tag.Colorize(Color.cyan) + " weapontag");

                var BaseVerb = gun.verbs.Find(x => x.range > 0).MemberwiseClone();

                gun.verbs.RemoveAll(M => (M.range > 0));

                gun.statBases.Add(new StatModifier { stat = CE_StatDefOf.Bulk, value = the_patcher_def.bulk });

                var newTools = new List<Tool>();

                #region Tool patching
                foreach (Tool tool in gun.tools)
                {
                    if (!(tool is ToolCE))
                    {
                        ToolCE newTool = new ToolCE();

                        newTool.capacities = tool.capacities;

                        newTool.armorPenetrationSharp = tool.armorPenetration;

                        newTool.armorPenetrationBlunt = tool.armorPenetration;

                        newTool.armorPenetration = tool.armorPenetration;

                        newTool.chanceFactor = tool.chanceFactor;

                        newTool.power = tool.power;

                        newTool.linkedBodyPartsGroup = tool.linkedBodyPartsGroup;

                        newTool.label = tool.label;

                        newTools.Add(newTool);
                    }
                }

                gun.tools = newTools;
                #endregion

                gun.verbs.Add(new VerbPropertiesCE
                {
                    verbClass = typeof(Verb_ShootCE),

                    recoilAmount = the_patcher_def.BaseRecoil,

                    hasStandardCommand = true,

                    burstShotCount = the_patcher_def.FullAutoShotCount,

                    range = the_patcher_def.Range,

                    warmupTime = the_patcher_def.WarmupTime,

                    ticksBetweenBurstShots = the_patcher_def.shotinterval,

                    defaultProjectile = the_patcher_def.DefaultProj,

                    muzzleFlashScale = 9f,

                    soundAiming = BaseVerb.soundAiming,

                    soundCast = BaseVerb.soundCast,

                    soundCastTail = BaseVerb.soundCastTail
                    


                    

                    



                });

                gun.comps.Add(new CompProperties_AmmoUser
                {
                    magazineSize = the_patcher_def.MagSize,

                    reloadTime = the_patcher_def.ReloadTime,

                    ammoSet = the_patcher_def.ammoset
                });

                gun.comps.Add(new CompProperties_FireModes
                {
                    aimedBurstShotCount = the_patcher_def.BurstCount
                });

                if (gun.statBases.Any(I => I.stat == StatDefOf.RangedWeapon_Cooldown))
                {
                    gun.statBases.RemoveAll(I => I.stat == StatDefOf.RangedWeapon_Cooldown);
                }


                gun.statBases.Add(new StatModifier
                {
                    stat = CE_StatDefOf.SightsEfficiency,

                    value = the_patcher_def.CooldownRanged
                });



                gun.statBases.Add(new StatModifier
                {
                    stat = CE_StatDefOf.SwayFactor,

                    value = the_patcher_def.SwayAm
                });

                gun.statBases.Add(new StatModifier
                {
                    stat = CE_StatDefOf.ShotSpread,

                    value = the_patcher_def.BaseSpread
                });

                var ac_1 = gun.statBases.Find(F => F.stat == StatDefOf.AccuracyLong);

                if(ac_1 != null)
                {
                    gun.statBases.Remove(ac_1);
                }

                var ac_2 = gun.statBases.Find(F => F.stat == StatDefOf.AccuracyMedium);

                if (ac_2 != null)
                {
                    gun.statBases.Remove(ac_2);
                }

                var ac_3 = gun.statBases.Find(F => F.stat == StatDefOf.AccuracyShort);

                if (ac_3 != null)
                {
                    gun.statBases.Remove(ac_3);
                }

                var ac_4 = gun.statBases.Find(F => F.stat == StatDefOf.AccuracyTouch);

                if (ac_4 != null)
                {
                    gun.statBases.Remove(ac_4);
                }

                gun.weaponTags.Add("patched");


            }
        }
    }
}
