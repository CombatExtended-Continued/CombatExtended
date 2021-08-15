using System;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public struct EquipmentStatKey : IEquatable<EquipmentStatKey>
    {
        public ThingDef equipmentDef;
        public QualityCategory quality;

        public EquipmentStatKey(ThingWithComps equipment)
        {
            equipmentDef = equipment.def;
            if (!equipment.TryGetQuality(out quality))
                quality = QualityCategory.Normal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            unchecked
            {
                return (((equipmentDef.shortHash * 397) ^ quality.GetHashCode()) * 397) ^ equipmentDef.index;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other)
        {
            return other is EquipmentStatKey key ? Equals(key) : false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(EquipmentStatKey other)
        {
            return other.equipmentDef.index == equipmentDef.index && other.quality == quality;
        }

        public static bool operator ==(EquipmentStatKey left, EquipmentStatKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EquipmentStatKey left, EquipmentStatKey right)
        {
            return !left.Equals(right);
        }
    }
}
