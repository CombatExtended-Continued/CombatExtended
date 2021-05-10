using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CombatExtended
{
    public static class Extensions
    {
        public static bool IsNonLethalWeapon(this ThingDef thingDef)
        {
            return IsNonLethalMeleeWeapon(thingDef) || IsNonLethalRangedWeapon(thingDef);
        }

        public static bool IsNonLethalMeleeWeapon(this ThingDef thingDef)
        {
            return thingDef?.weaponTags?.Contains("CE_Nonlethal_Melee") ?? false;
        }

        public static bool IsNonLethalRangedWeapon(this ThingDef thingDef)
        {
            return thingDef?.weaponTags?.Contains("CE_Nonlethal_Ranged") ?? false;
        }
    }
}
