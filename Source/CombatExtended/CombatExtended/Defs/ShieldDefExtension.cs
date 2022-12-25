using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class ShieldDefExtension : DefModExtension
    {
        public List<BodyPartGroupDef> shieldCoverage = new List<BodyPartGroupDef>();
        public List<BodyPartGroupDef> crouchCoverage = new List<BodyPartGroupDef>();
        public bool drawAsTall = false;

        public bool PartIsCoveredByShield(BodyPartRecord part, Pawn pawn)
        {
            if (!shieldCoverage.NullOrEmpty())
            {
                foreach (BodyPartGroupDef group in shieldCoverage)
                {
                    if (part.IsInGroup(group))
                    {
                        return true;
                    }
                }
            }
            if (!crouchCoverage.NullOrEmpty() && pawn.IsCrouching())
            {
                foreach (BodyPartGroupDef group in crouchCoverage)
                {
                    if (part.IsInGroup(group))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
