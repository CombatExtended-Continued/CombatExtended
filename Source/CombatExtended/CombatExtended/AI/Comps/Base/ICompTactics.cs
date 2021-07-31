using System;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public abstract class ICompTactics : IExposable
    {
        private Pawn pawnInt;

        public virtual Pawn SelPawn
        {
            get
            {
                return pawnInt;
            }
        }

        public abstract int Priority
        {
            get;
        }

        public virtual ThingWithComps CurrentWeapon
        {
            get
            {
                return SelPawn.equipment.Primary;
            }
        }

        private CompSuppressable _compSuppressable = null;
        public virtual CompSuppressable CompSuppressable
        {
            get
            {
                if (_compSuppressable == null)
                    _compSuppressable = SelPawn.TryGetComp<CompSuppressable>();
                return _compSuppressable;
            }
        }

        private CompInventory _compInventory = null;
        public virtual CompInventory CompInventory
        {
            get
            {
                if (_compInventory == null) _compInventory = SelPawn.TryGetComp<CompInventory>();
                return _compInventory;
            }
        }

        private CompAmmoUser _AmmoUser_CompAmmoUser = null;
        private ThingWithComps _AmmoUser_ThingWithComps = null;

        public virtual CompAmmoUser CurrentWeaponCompAmmo
        {
            get
            {
                if (_AmmoUser_ThingWithComps == CurrentWeapon)
                    return _AmmoUser_CompAmmoUser;

                _AmmoUser_ThingWithComps = CurrentWeapon;

                if (_AmmoUser_ThingWithComps == null)
                    return _AmmoUser_CompAmmoUser = null;

                return _AmmoUser_CompAmmoUser = _AmmoUser_ThingWithComps.TryGetComp<CompAmmoUser>();
            }
        }

        public Map Map
        {
            get
            {
                return pawnInt.Map;
            }
        }

        public ICompTactics()
        {
        }

        public virtual void Initialize(Pawn pawn)
        {
            this.pawnInt = pawn;
        }

        public virtual Job TryGiveTacticalJob()
        {
            return null;
        }

        public virtual bool StartCastChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            return true;
        }

        public virtual void OnStartCastFailed()
        {
        }

        public virtual void OnStartCastSuccess(Verb verb)
        {
        }

        public virtual void PostExposeData()
        {
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref pawnInt, "pawnInt");
            this.PostExposeData();
        }

        public void Notify_StartCastChecksFailed(ICompTactics failedComp)
        {
            if (failedComp != this)
                OnStartCastFailed();
        }

        public void Notify_StartCastChecksSuccess(Verb verb)
        {
            OnStartCastSuccess(verb);
        }
    }
}
