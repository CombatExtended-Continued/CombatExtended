using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HugsLib.Source.Detour;

namespace CombatExtended.Detours
{
    internal static class Detours_Pawn_HealthTracker
    {

        public static readonly FieldInfo _pawn = typeof(Pawn_HealthTracker).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo _MakeDowned = typeof(Pawn_HealthTracker).GetMethod("MakeDowned", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo _MakeUndowned = typeof(Pawn_HealthTracker).GetMethod("MakeUndowned", BindingFlags.Instance | BindingFlags.NonPublic);

        [DetourMethod(typeof(Pawn_HealthTracker), "CheckForStateChange")]
        internal static void CheckForStateChange(this Pawn_HealthTracker _this, DamageInfo? _dinfo, Hediff _hediff)
        {
            Pawn pawn = (Pawn)_pawn.GetValue(_this);

            if (!_this.Dead)
            {
                if (ShouldBeDead(_this))
                {
                    if (!pawn.Destroyed)
                    {
                        _this.Kill(_dinfo, _hediff);
                    }
                    return;
                }
                if (!_this.Downed)
                {
                    if (ShouldBeDowned(_this))
                    {
                        float num = (!pawn.RaceProps.Animal) ? 0.67f : 0.47f;
                        /* Remove the code responsible for RNG death
                        if (!_this.forceIncap && (pawn.Faction == null || !pawn.Faction.IsPlayer) && !pawn.IsPrisonerOfColony && pawn.RaceProps.IsFlesh && Rand.Value < num)
                        {
                            _this.Kill(_dinfo, null);
                            return;
                        }
                        */
                        _this.forceIncap = false;
                        _MakeDowned.Invoke(_this, new object[] { _dinfo, _hediff });
                        return;
                    }
                    else if (!_this.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                    {
                        if (pawn.carryTracker != null && pawn.carryTracker.CarriedThing != null && pawn.jobs != null && pawn.CurJob != null)
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                        }
                        if (pawn.equipment != null && pawn.equipment.Primary != null)
                        {
                            if (pawn.InContainerEnclosed)
                            {
                                ThingWithComps thingWithComps;
                                pawn.equipment.TryTransferEquipmentToContainer(pawn.equipment.Primary, pawn.holdingContainer, out thingWithComps);
                            }
                            else if (pawn.Spawned)
                            {
                                ThingWithComps thingWithComps;
                                pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out thingWithComps, pawn.Position, true);
                            }
                            else
                            {
                                pawn.equipment.DestroyEquipment(pawn.equipment.Primary);
                            }
                        }
                    }
                }
                else if (!ShouldBeDowned(_this))
                {
                    _MakeUndowned.Invoke(_this, null);
                    return;
                }
            }
        }

        internal static bool ShouldBeDowned(this Pawn_HealthTracker _this)
        {
            return _this.InPainShock || !_this.capacities.CanBeAwake || !_this.capacities.CapableOf(PawnCapacityDefOf.Moving);
        }

        internal static bool ShouldBeDead(this Pawn_HealthTracker _this)
        {
            Pawn pawn = (Pawn)_pawn.GetValue(_this);

            if (_this.Dead)
            {
                return true;
            }
            for (int i = 0; i < _this.hediffSet.hediffs.Count; i++)
            {
                if (_this.hediffSet.hediffs[i].CauseDeathNow())
                {
                    return true;
                }
            }
            List<PawnCapacityDef> allDefsListForReading = DefDatabase<PawnCapacityDef>.AllDefsListForReading;
            for (int j = 0; j < allDefsListForReading.Count; j++)
            {
                PawnCapacityDef pawnCapacityDef = allDefsListForReading[j];
                bool flag = (!pawn.RaceProps.IsFlesh) ? pawnCapacityDef.lethalMechanoids : pawnCapacityDef.lethalFlesh;
                if (flag && !_this.capacities.CapableOf(pawnCapacityDef))
                {
                    return true;
                }
            }
            float num = PawnCapacityUtility.CalculatePartEfficiency(_this.hediffSet, pawn.RaceProps.body.corePart, false);
            return num <= 0.0001f;
        }
    }
}
