using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class Apparel_Shield : Apparel
    {
        public const string OneHandedTag = "CE_OneHandedWeapon";

        public override bool AllowVerbCast(Verb verb)
        {
            ThingWithComps primary = Wearer.equipment?.Primary;
            return primary == null || (primary.def.weaponTags?.Contains(OneHandedTag) ?? false);
        }
    }
}
