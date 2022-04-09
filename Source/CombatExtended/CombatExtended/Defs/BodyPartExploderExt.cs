using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public class BodyPartExploderExt : DefModExtension
    {
        public float triggerChance;

        public List<DamageDef> allowedDamageDefs;
    }
}
