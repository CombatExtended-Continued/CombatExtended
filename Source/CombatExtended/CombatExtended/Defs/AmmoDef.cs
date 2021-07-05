using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class AmmoDef : ThingDef
    {
        public AmmoCategoryDef ammoClass;
        public int defaultAmmoCount = 1;
        public float cookOffSpeed = 1f;
        public float cookOffFlashScale = 1;
        public ThingDef cookOffProjectile = null;
        public SoundDef cookOffSound = null;
        public SoundDef cookOffTailSound = null;
        public ThingDef detonateProjectile = null;
        // mortar ammo should still availabe when the ammo system is off
        public bool isMortarAmmo = false;
        public bool menuHidden = false;

        private List<DefHyperlink> originalHyperlinks;

        private List<ThingDef> users;
        public List<ThingDef> Users
        {
            get
            {
                if (users == null)
                {
                    users = CE_Utility.allWeaponDefs.FindAll(delegate (ThingDef def)
                    {
                        CompProperties_AmmoUser props = def.GetCompProperties<CompProperties_AmmoUser>();
                        if (props?.ammoSet?.ammoTypes != null)
                        {
                            return props.ammoSet.ammoTypes.Any(x => x.ammo == this);
                        }
                        return false;
                    });

                    if (users != null && !users.Any())
                        return users;

                    if (descriptionHyperlinks.NullOrEmpty())
                        descriptionHyperlinks = new List<DefHyperlink>();
                    else
                    {
                        if (originalHyperlinks.NullOrEmpty())
                        {
                            originalHyperlinks = new List<DefHyperlink>();

                            foreach (var i in descriptionHyperlinks)
                                originalHyperlinks.Add(i);
                        }
                        else
                        {
                            var exceptList = descriptionHyperlinks.Except(originalHyperlinks).ToList();
                            foreach (var i in exceptList)
                            {
                                descriptionHyperlinks.Remove(i);
                                i.def.descriptionHyperlinks.Remove(this);
                            }
                        }
                    }

                    foreach (var user in users)
                    {
                        descriptionHyperlinks.Add(user);

                        if (user.descriptionHyperlinks.NullOrEmpty())
                            user.descriptionHyperlinks = new List<DefHyperlink>();

                        user.descriptionHyperlinks.Add(this);
                    }

                }
                return users;
            }
        }

        private List<AmmoSetDef> ammoSetDefs;
        public List<AmmoSetDef> AmmoSetDefs
        {
            get
            {
                if (ammoSetDefs == null)
                    ammoSetDefs = Users.Select(x => x.GetCompProperties<CompProperties_AmmoUser>().ammoSet).Distinct().ToList();

                return ammoSetDefs;
            }
        }

        private string oldDescription;
        public void AddDescriptionParts()
        {
            if (ammoClass != null)
            {
                if (oldDescription.NullOrEmpty())
                    oldDescription = description;

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(oldDescription);

                // Append ammo class description
                stringBuilder.AppendLine("\n" + ammoClass.LabelCap + ":");
                stringBuilder.AppendLine(ammoClass.description);

                // Append guns that use this caliber
                if (!Users.NullOrEmpty())
                    stringBuilder.AppendLine("\n" + "CE_UsedBy".Translate() + ":");

                description = stringBuilder.ToString().TrimEndNewlines();
            }
        }

        public override void ResolveReferences()
        {
            base.ResolveReferences();

            if (detonateProjectile != null)
            {
                foreach (var comp in detonateProjectile.comps)
                {
                    if (!comps.Any(x => x.compClass == comp.compClass)
                        && (comp.compClass == typeof(CompFragments)
                            || comp.compClass == typeof(CompExplosive)
                            || comp.compClass == typeof(CompExplosiveCE)))
                        comps.Add(comp);
                }
            }
        }
    }
}
