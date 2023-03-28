﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class OneHandedAutopatcher
    {
        static OneHandedAutopatcher()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(x => (x.weaponTags?.Contains(Apparel_Shield.OneHandedTag) ?? false)))
            {
                if (def.statBases == null)
                {
                    def.statBases = new List<StatModifier>();
                }
                def.statBases.Add(new StatModifier { stat = CE_StatDefOf.OneHandedness, value = 1 });
            }
        }
    }
    public class StatWorker_OneHandedness : StatWorker
    {

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            return IsOneHanded(req) ? 1 : 0;
        }
        public bool IsOneHanded(StatRequest req)
        {
            if (req.Thing != null)
            {
                return (req.Thing.def.weaponTags?.Contains(Apparel_Shield.OneHandedTag) ?? false);
            }
            else if (req.Def is ThingDef def)
            {
                return (def.weaponTags?.Contains(Apparel_Shield.OneHandedTag) ?? false);
            }
            return false;
        }

        public override string ValueToString(float val, bool finalized, ToStringNumberSense numberSense = ToStringNumberSense.Absolute)
        {
            return (val > 0 ? "CE_Yes" : "CE_No").Translate();
        }
    }
}
