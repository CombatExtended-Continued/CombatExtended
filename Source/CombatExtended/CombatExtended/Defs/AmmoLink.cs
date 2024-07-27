using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class AmmoLink
    {
        public AmmoDef ammo;
        public ThingDef projectile;
        private ThingDef projectileCIWS;

        public AmmoLink() { }

        public AmmoLink(AmmoDef ammo, ThingDef projectile, ThingDef CIWSProjectile = null)
        {
            this.ammo = ammo;
            this.projectile = projectile;
            this.projectileCIWS = CIWSProjectile;
        }

        public ThingDef CIWSProjectile => projectileCIWS ?? projectile;
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1)
            {
                Log.Error("Misconfigured AmmoLink: " + xmlRoot.OuterXml);
                return;
            }
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "ammo", xmlRoot.Name);
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "projectile", (string)ParseHelper.FromString(xmlRoot.FirstChild.Value, typeof(string)));
        }

        public override string ToString()
        {
            return string.Concat("(", ammo == null ? "null" : ammo.defName, " -> ", projectile == null ? "null" : projectile.defName, ")");
        }

        public override int GetHashCode()
        {
            return ammo.shortHash + projectile.shortHash << 16;
        }
    }
}
