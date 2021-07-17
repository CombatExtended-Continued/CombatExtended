using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using Steamworks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public partial class StatPart_ApparelStatMaxima : StatPart_ApparelStatSelect
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override float Select(float first, float second)
        {
            return Mathf.Max(first, second);
        }
    }
}
