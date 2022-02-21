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
    public class CaliberFloatRange
    {
        public FloatRange DamageRange;

        public FloatRange SpeedRange;

        public AmmoSetDef AmmoSet;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DamageRange = ParseHelper.FromString<FloatRange>(xmlRoot.ChildNodes[0].InnerText);
            SpeedRange = ParseHelper.FromString<FloatRange>(xmlRoot.ChildNodes[1].InnerText);
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "AmmoSet", xmlRoot.LastChild.InnerText);
        }
    }
    public class LabelGun
    {
        public List<string> names;

        public int magCap;

        public float reloadTime;

        public float mass;

        public float bulk;

        public AmmoSetDef caliber;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            foreach (XmlNode child in xmlRoot.ChildNodes)
            {
                Log.Message("name " + child.Name + " innter text " + child.InnerText);
            }

            magCap = ParseHelper.FromString<int>(xmlRoot.FirstChild.InnerText);

            reloadTime = ParseHelper.FromString<float>(xmlRoot.ChildNodes[1].InnerText);

            mass = ParseHelper.FromString<float>(xmlRoot.ChildNodes[2].InnerText);

            bulk = ParseHelper.FromString<float>(xmlRoot.ChildNodes[3].InnerText);

            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "caliber", xmlRoot.ChildNodes[4].InnerText);

            if (names == null)
            {
                names = new List<string>();
            }
            foreach (XmlNode node in xmlRoot.LastChild.ChildNodes)
            {
                names.Add(node.InnerText);
            }
        }
    }
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
                }
            }
        }
    }

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
