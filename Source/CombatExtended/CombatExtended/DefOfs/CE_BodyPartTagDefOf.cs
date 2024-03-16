using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    [DefOf]
    public class CE_BodyPartTagDefOf
    {
        static CE_BodyPartTagDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CE_BodyPartTagDefOf));
        }
        public static BodyPartTagDef OutsideSquishy;
    }
}

