using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public abstract class IJobDriver_Tactical : JobDriver
    {
        public int numAttacksMade = 0;

        private CompSuppressable _compSuppressable = null;
        public virtual CompSuppressable CompSuppressable
        {
            get
            {
                if (_compSuppressable == null)
                    _compSuppressable = pawn.TryGetComp<CompSuppressable>();
                return _compSuppressable;
            }
        }

        private CompInventory _compInventory = null;
        public virtual CompInventory CompInventory
        {
            get
            {
                if (_compInventory == null) _compInventory = pawn.TryGetComp<CompInventory>();
                return _compInventory;
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref numAttacksMade, "numAttacksMade", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
    }
}
