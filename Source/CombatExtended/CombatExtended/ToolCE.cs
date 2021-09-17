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
        public float armorPenetrationSharp;
        public float armorPenetrationBlunt;
        public Gender restrictedGender = Gender.None;
        public AttachmentDef requiredAttachment = null;
    }
}
