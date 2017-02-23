using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class Apparel_TacVest : Apparel_VisibleAccessory
    {
        protected override float GetAltitudeOffset(Rot4 rotation)
        {
        	return rotation == Rot4.North ? 0.032f : 0.0269999985f;
        }
    }
}
