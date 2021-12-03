using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class CombatReservationManager : MapComponent
    {
        private struct PawnReservationRecord : IExposable, IEquatable<PawnReservationRecord>
        {            
            public Pawn attacker;
            public Thing target;

            public Pawn TargetPawn
            {
                get => target as Pawn;
            }            

            private bool _tragetIsPawn;
            public  bool  TargetIsPawn
            {
                get => _tragetIsPawn;
            }            

            public bool IsValid
            {
                get => (attacker?.Spawned ?? false)                    
                    && (target?.Spawned ?? false)
                    && !attacker.Downed                    
                    && attacker.mindState?.enemyTarget != null
                    && attacker.mindState?.enemyTarget == target;
            }

            public PawnReservationRecord(Pawn attacker, Thing target)
            {
                this.attacker = attacker;
                this.target = target;
                this._tragetIsPawn = target is Pawn;
            }
         
            public void ExposeData()
            {
                Scribe_References.Look(ref attacker, "attacker");
                Scribe_References.Look(ref target, "target");
                Scribe_Values.Look(ref _tragetIsPawn, "_tragetIsPawn");
            }

            public bool Equals(PawnReservationRecord other) => other.attacker == attacker && other.target == target;
            public override bool Equals(object obj) => obj is PawnReservationRecord other ? other.Equals(this) : false;
            public override int GetHashCode()
            {                
                int hash = 17;
                unchecked
                {
                    hash = (hash * 7919) ^ (target?.thingIDNumber ?? 0);
                    hash = (hash * 7919) ^ (attacker?.thingIDNumber ?? 0);
                    hash = (hash * 7919) ^ (_tragetIsPawn ? 13 : 0);
                }
                return hash;                
            }
        }
        
        private class ReservedCombatTarget : IExposable
        {
            private int _reservationsDirtyAt = -1;
            private int _reservationsNum = 0;            

            private Thing target;
            private List<PawnReservationRecord> reservations = new List<PawnReservationRecord>(4);

            public Thing Target
            {
                get => target;
            }

            public int RecordCount
            {
                get
                {
                    if (_reservationsDirtyAt < GenTicks.TicksGame) Dirty();
                    return _reservationsNum;
                }
            }
            
            public List<PawnReservationRecord> Records
            {
                get
                {
                    if (_reservationsDirtyAt < GenTicks.TicksGame) Dirty();
                    return reservations;
                }
            }

            public bool IsValid
            {
                get => target != null && target.Spawned;
            }

            public ReservedCombatTarget()
            {
            }

            public ReservedCombatTarget(Thing thing)
            {
                this.target = thing;
            }

            public void Add(Pawn attacker)
            {
                if (reservations.All(r => r.attacker != attacker))
                {                                                            
                    reservations.Add(new PawnReservationRecord(attacker, target));
                    Dirty();
                }
            }
            public void ExposeData()
            {
                if(Scribe.mode == LoadSaveMode.Saving)
                    reservations.RemoveAll(r => !r.IsValid);
                Scribe_References.Look(ref target, "target");
                Scribe_Collections.Look(ref reservations, "reservations", LookMode.Deep);
                reservations ??= new List<PawnReservationRecord>();
            }
            public void Dirty()
            {
                reservations.RemoveAll(r => !r.IsValid);

                _reservationsDirtyAt = GenTicks.TicksGame + 30;                               
                _reservationsNum = reservations.Count;
            }
        }
                        
        private Dictionary<Thing, ReservedCombatTarget> reservations = new Dictionary<Thing, ReservedCombatTarget>(); 

        public CombatReservationManager(Map map) : base(map)
        {
        }

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();
            if (Controller.settings.DebugDrawTargetedBy && !Find.Selector.SelectedPawns.NullOrEmpty())
            {
                Pawn pawn = Find.Selector.SelectedPawns.First();
                Vector2 center = UI.MapToUIPosition(pawn.Position.ToVector3());
                if(Reserved(pawn, out List<Pawn> attackers))
                {
                    foreach(Pawn attacker in attackers)
                    {
                        Vector2 pos = UI.MapToUIPosition(attacker.Position.ToVector3());
                        Widgets.DrawLine(center, pos, Color.red, 1);
                    }
                }
            }
        }       

        public override void MapComponentTick()
        {
            base.MapComponentTick();            
            if((GenTicks.TicksGame + 31) % 15000 == 0)
            {
                reservations.RemoveAll(r => !r.Value.IsValid);
                foreach(KeyValuePair<Thing, ReservedCombatTarget> pair in reservations)                
                    pair.Value.Dirty();                
            }
        }

        public void Reserve(Pawn attacker, Thing target)
        {
            if (attacker != null && TryGetReservedCombatTarget(target, out ReservedCombatTarget store))                         
                store.Add(attacker);            
        }

        public bool Reserved(Thing target) => Reserved(target, out int _);
        public bool Reserved(Thing target, out int num)
        {
            if (!TryGetReservedCombatTarget(target, out ReservedCombatTarget store))
            {
                num = 0;
                return false;
            }            
            return (num = store.RecordCount) > 0;
        }
        public bool Reserved(Thing target, out List<Pawn> attackers)
        {
            if (!TryGetReservedCombatTarget(target, out ReservedCombatTarget store))
            {
                attackers = null;
                return false;
            }
            store.Dirty();
            attackers = new List<Pawn>();
            attackers.AddRange(store.Records.Select(r => r.attacker));            
            return attackers.Count > 0;
        }

        private bool TryGetReservedCombatTarget(Thing target, out ReservedCombatTarget store)
        {
            if (target == null || !target.Spawned)
            {
                store = null;
                return false;
            }
            if (!reservations.TryGetValue(target, out store))
            {
                store = new ReservedCombatTarget(target);
                reservations[target] = store;
            }
            return true;
        }
    }
}

