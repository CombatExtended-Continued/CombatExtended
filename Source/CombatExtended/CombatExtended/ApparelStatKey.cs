using System;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public partial class StatPart_ApparelStatMaxima
    {
        //private static Dictionary<CachedKey>        

        public struct ApparelStatKey : IEquatable<ApparelStatKey>
        {
            public ThingDef apparelDef;
            public QualityCategory quality;

            public ApparelStatKey(Apparel apparel)
            {
                apparelDef = apparel.def;
                if (!apparel.TryGetQuality(out quality))
                    quality = QualityCategory.Normal;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                unchecked
                {
                    return (((apparelDef.shortHash * 397) ^ quality.GetHashCode()) * 397) ^ apparelDef.index;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Equals(object other)
            {
                return other is ApparelStatKey key ? Equals(key) : false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(ApparelStatKey other)
            {
                return other.apparelDef.index == apparelDef.index && other.quality == quality;
            }

            public static bool operator ==(ApparelStatKey left, ApparelStatKey right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(ApparelStatKey left, ApparelStatKey right)
            {
                return !left.Equals(right);
            }
        }
    }
}
