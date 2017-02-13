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
        private const float YOffsetInterval_Clothes = 0.005f; // copy-pasted from PawnRenderer

        protected override float GetAltitudeOffset(Rot4 rotation)
        {
        	/* Pawn offset reference:
        	 * Wounds offset: 0.02f
        	 * Shell offset: 0.0249999985f
        	 * Head offset: 0.03f
        	 * North flips order shell/head
        	 * Hair (if drawn gets important when north) offset: 0.035f
        	 */
        	return rotation == Rot4.North ? 0.032f : 0.0269999985f;
        }
    }
}
