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
                    |
                    (preset.tags != null && x.weaponTags != null && preset.tags.Intersect(x.weaponTags).Any() )
                    |
                    preset.specialGuns.
                    Any
                    (
                        y =>
                        y.names.Contains(x.label)
                        |
                        y.names.Intersect(x.label.ToLower().Replace("-", "").Split(' ')).Any()
                        )
                    );

                foreach (var gun in matchingGuns)
                {
                    Log.Message(gun.label);

                    var OldProj = gun.Verbs[0].defaultProjectile;

                    gun.Verbs.Clear();

                    gun.verbs.Clear();

                    gun.verbs.Add(preset.gunStats.MemberwiseClone());

                    if (gun.comps == null)
                    {
                        gun.comps = new List<CompProperties>();
                    }

                    float finalMass = preset.Mass;

                    float finalBulk = preset.Bulk;

                    if (preset.specialGuns.Any(x => x.names.Intersect(gun.label.ToLower().Replace("-", "").Split(' ').ToList<string>()).Any()))
                    {
                        var specialGun = preset.specialGuns.Find(x => x.names.Intersect(gun.label.ToLower().Replace("-", "").Split(' ').ToList<string>()).Any());
                        gun.comps.Add(new CompProperties_AmmoUser
                        {
                            ammoSet = specialGun.caliber,
                            reloadTime = specialGun.reloadTime,
                            magazineSize = specialGun.magCap,
                            reloadOneAtATime = preset.reloadOneAtATime

                        });

                        finalMass = specialGun.mass;

                        finalMass = specialGun.bulk;

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


                    gun.comps.Add(preset.fireModes);

                    gun.AddOrChangeStat(new StatModifier { stat = StatDefOf.Mass, value = finalMass });

                    gun.AddOrChangeStat(new StatModifier { stat = CE_StatDefOf.Bulk, value = finalBulk });

                    gun.AddOrChangeStat(new StatModifier { stat = StatDefOf.RangedWeapon_Cooldown, value = preset.CooldownTime });

                    gun.statBases.RemoveAll(x => x.stat.label.Contains("accuracy"));

                    gun.statBases.Add(new StatModifier { stat = CE_StatDefOf.ShotSpread, value = preset.Spread });

                    gun.statBases.Add(new StatModifier { stat = CE_StatDefOf.SwayFactor, value = preset.Sway});

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
                }
            }
        }
    }
}
