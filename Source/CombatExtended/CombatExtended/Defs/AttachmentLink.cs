using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class AttachmentLink
    {
        public AttachmentDef attachment;

        public List<StatModifier> statOffsets;

        public List<StatModifier> statMultipliers;

        public List<StatModifier> statReplacers;
    }
}
