using System;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public abstract class ICompTactics : ThingComp
    {

        public virtual Pawn SelPawn
        {
            get
            {
                return parent as Pawn;
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
                return parent.Map;
            }
        }

        public virtual bool ShouldRun
        {
            get
            {
                return !(SelPawn.Faction?.IsPlayer ?? false);
            }
        }

        public abstract bool StartCastChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg);

        public void Notify_StartCastChecksFailed(ICompTactics failedComp)
        {
            if (failedComp != this)
                OnStartCastFailed();
        }

        public void Notify_StartCastChecksSuccess(Verb verb)
        {
            OnStartCastSuccess(verb);
        }

        public virtual void OnStartCastFailed()
        {
        }

        public virtual void OnStartCastSuccess(Verb verb)
        {
        }
    }
}
