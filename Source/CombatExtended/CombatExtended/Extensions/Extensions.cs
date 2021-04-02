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
            return thingDef?.weaponTags?.Contains("CE_AI_Nonlethal") == true;
        }
    }
}
