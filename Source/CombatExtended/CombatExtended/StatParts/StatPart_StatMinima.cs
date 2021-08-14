using System;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
    public class StatPart_StatMinima : StatPart_StatSelect
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override float Select(float first, float second)
        {
            return Mathf.Min(first, second);
        }
    }
}
