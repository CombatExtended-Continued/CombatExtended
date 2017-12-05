using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public class ToolCE : Tool
    {
        public float armorPenetration = 0;
        public Gender restrictedGender = Gender.None;
    }
}
