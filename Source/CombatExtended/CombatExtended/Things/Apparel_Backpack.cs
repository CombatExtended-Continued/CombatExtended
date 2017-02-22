using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class Apparel_Backpack : Apparel_VisibleAccessory
    {
        protected override float GetAltitudeOffset(Rot4 rotation)
        {
            return rotation == Rot4.North ? 0.033f : 0.0279999985f;
        }
    }
}
