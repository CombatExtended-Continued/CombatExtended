using System;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class StatPart_StatMinima : StatPart_StatSelect
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override float Select(float first, float second)
        {
            return Mathf.Min(first, second);
        }

        public override string ExplanationPart(StatRequest req)
        {
            return "CE_StatPart_MinimaExplanation".Translate() + ": \n\n" + base.ExplanationPart(req);
        }
    }
}
