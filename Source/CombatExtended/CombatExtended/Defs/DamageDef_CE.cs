using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class DamageDef_CE : DamageDef
    {
        public bool noDamageOnDeflect = false;
        public bool harmOnlyOutsideLayers = false;
    }
}
