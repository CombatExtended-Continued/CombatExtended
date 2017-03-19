using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class AmmoThing : ThingWithComps
    {
        AmmoDef ammoDef { get { return def as AmmoDef; } }

        public override string GetDescription()
        {
            if(ammoDef != null && ammoDef.ammoClass != null)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(base.GetDescription());

                // Append ammo class description
                stringBuilder.AppendLine("\n" + ammoDef.ammoClass.LabelCap + ":");
                stringBuilder.AppendLine(ammoDef.ammoClass.description);

                // Append guns that use this caliber
                stringBuilder.AppendLine("\n" + "CE_UsedBy".Translate() + ":");
                foreach(var user in ammoDef.Users)
                {
                    stringBuilder.AppendLine("   -" + user.LabelCap);
                }

                return stringBuilder.ToString();
            }
            return base.GetDescription();
        }
    }
}
