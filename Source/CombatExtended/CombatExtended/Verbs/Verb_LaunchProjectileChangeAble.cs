using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using System.Xml;

namespace CombatExtended
{
    public class AdditionalWeapon
    {
        public ThingDef projectile;

        public float chanceToUse;

        public int burstCount;

        public int uses;

        public int shotTime;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            foreach (XmlNode node in xmlRoot.ChildNodes)
            {
                if (node.Name.ToLower() == "uses")
                {
                    uses = ParseHelper.ParseIntPermissive(node.InnerText);
                }
                if (node.Name.ToLower() == "burstcount")
                {
                    burstCount = ParseHelper.ParseIntPermissive(node.InnerText);
                }
                if (node.Name.ToLower() == "chancetouse")
                {
                    chanceToUse = ParseHelper.ParseFloat(node.InnerText);
                }
                if (node.Name.ToLower() == "shottime")
                {
                    shotTime = ParseHelper.ParseIntPermissive(node.InnerText);
                }
                if (node.Name.ToLower() == "projectile")
                {
                    DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "projectile", node.InnerText, null, null);
                }

            }
        }
    }
    public class ProjectileChangeExt : DefModExtension
    {
        public List<AdditionalWeapon> additionalEquipment;
    }

    public class Verb_LaunchProjectileChangeAble : Verb_ShootCE
    {
        public Dictionary<AdditionalWeapon, int> UseManager = null;

        public ProjectileChangeExt ChangerExt => this.CasterPawn?.def.GetModExtension<ProjectileChangeExt>() ?? null;

        public ThingDef ProjectileInt;

        public bool fireswitch;

        public Pair<AdditionalWeapon, int> burstSwitcherPair;

        public override ThingDef Projectile
        {
            get
            {
                if (fireswitch)
                {
                    if (burstSwitcherPair != null)
                    {
                        if (burstSwitcherPair.second > 0)
                        {
                            return burstSwitcherPair.first.projectile;
                        }
                    }

                    return ProjectileInt;
                }
                return base.Projectile;
            }
        }

        public override bool TryCastShot()
        {
            fireswitch = false;

            if (ChangerExt != null)
            {
                if (UseManager.NullOrEmpty())
                {
                    if (UseManager == null)
                    {
                        UseManager = new Dictionary<AdditionalWeapon, int>();
                    }

                    foreach (var weapon in ChangerExt.additionalEquipment)
                    {
                        UseManager.Add(weapon, weapon.uses);
                    }
                }

                if (this.Bursting)
                {
                    if (burstSwitcherPair == null || burstSwitcherPair.second < 1)
                    {
                        foreach (var weapon in ChangerExt.additionalEquipment)
                        {
                            if (Rand.Chance(weapon.chanceToUse))
                            {
                                if (UseManager.TryGetValue(weapon) > 0)
                                {
                                    UseManager.SetOrAdd(weapon, (UseManager.TryGetValue(weapon) - 1));
                                    ProjectileInt = weapon.projectile;
                                    fireswitch = true;

                                    if (burstSwitcherPair == null)
                                    {
                                        burstSwitcherPair = new Pair<AdditionalWeapon, int>();
                                    }

                                    burstSwitcherPair.first = weapon;
                                    burstSwitcherPair.second = (weapon.burstCount - 1);
                                }

                            }
                        }
                    }
                    else
                    {
                        burstSwitcherPair.second--;
                    }
                    
                }
            }
            return base.TryCastShot();
        }
    }
}
