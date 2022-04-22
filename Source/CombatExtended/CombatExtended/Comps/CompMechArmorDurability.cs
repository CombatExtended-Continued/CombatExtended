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
    public class ERAComponent : IExposable
    {
        public BodyPartDef part;

        public float armor;

        public float damageTreshold;

        public float APTreshold;

        public bool triggered = false;

        public CompProperties_Fragments frags;

        public CompFragments fragComp => new CompFragments() { props = frags };

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            foreach (XmlNode node in xmlRoot.ChildNodes)
            {
                if (node.Name.ToLower() == "armor")
                {
                    armor = ParseHelper.ParseFloat(node.InnerText);
                }
                if (node.Name.ToLower() == "damagetreshold")
                {
                    damageTreshold = ParseHelper.ParseFloat(node.InnerText);
                }
                if (node.Name.ToLower() == "aptreshold")
                {
                    damageTreshold = ParseHelper.ParseFloat(node.InnerText);
                }
                if (node.Name.ToLower() == "part")
                {
                    DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "part", node.InnerText, null, null);
                }
                if (node.Name.ToLower() == "triggered")
                {
                    triggered = ParseHelper.ParseBool(node.InnerText);
                }
                if (node.Name.ToLower() == "frags")
                {
                    frags = new CompProperties_Fragments() { fragments = new List<ThingDefCountClass>() };

                    foreach (XmlNode node2 in node.ChildNodes)
                    {
                        if (node2.Name == "fragments")
                        {
                            foreach (XmlNode node3 in node2.ChildNodes)
                            {
                                ThingDefCountClass count = new ThingDefCountClass();

                                count.LoadDataFromXmlCustom(node3);

                                frags.fragments.Add(count);
                            }
                        }
                        if (node2.Name == "fragSpeedFactor")
                        {
                            frags.fragSpeedFactor = ParseHelper.ParseFloat(node2.InnerText);
                        }
                    }
                }

            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref triggered, "triggered");
        }
    }
    public class CompMechArmorDurability : ThingComp
    {
        public List<ERAComponent> ERA => this.parent.def.GetModExtension<ERAModExt>().ERA;

        public MechArmorDurabilityExt durabilityProps => this.parent.def.GetModExtension<MechArmorDurabilityExt>();

        public float maxDurability => durabilityProps.Durability;

        public float curDurability;

        public float curDurabilityPercent => (float)Math.Round(curDurability / maxDurability, 2);

        public override void PostPostMake()
        {
            curDurability = maxDurability;
            base.PostPostMake();
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            curDurability -= (dinfo.Amount / this.parent.GetStatValue(StatDefOf.IncomingDamageFactor));

            if (curDurability < 0)
            {
                curDurability = 0;
            }
        }
    }

    public class ERAModExt : DefModExtension
    {
        public List<ERAComponent> ERA;
    }

    public class MechArmorDurabilityExt : DefModExtension
    {
        public float Durability;
    }
}
