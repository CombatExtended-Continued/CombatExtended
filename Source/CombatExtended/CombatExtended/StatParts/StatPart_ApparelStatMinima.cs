using System;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;

namespace CombatExtended
{
    public partial class StatPart_ApparelStatMinima : StatPart_ApparelStatSelect
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override float Select(float first, float second)
        {
            return Mathf.Min(first, second);
        }
    }
}
