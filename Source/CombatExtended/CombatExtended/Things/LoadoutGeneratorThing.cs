using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CombatExtended
{
    public class LoadoutGeneratorThing : Thing
    {
        public int priority
        {
            get
            {
                LoadoutGeneratorDef lDef = def as LoadoutGeneratorDef;
                if (lDef != null)
                    return lDef.priority;
                return 0;
            }
        }
        public LoadoutGenerator loadoutGenerator
        {
            get
            {
                LoadoutGeneratorDef lDef = def as LoadoutGeneratorDef;
                if (lDef != null)
                    return lDef.loadoutGenerator;
                return null;
            }
        }
    }
}
