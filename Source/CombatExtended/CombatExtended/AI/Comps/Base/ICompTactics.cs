using System;
using RimWorld;
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

        private Map _sightGridMap = null;
        private Faction _sightGridFaction = null;
        private SightGrid _sightGrid = null;
        public SightGrid MapSightGrid
        {
            get
            {
                if (!SelPawn.Spawned || SelPawn.Faction == null)
                {
                    _sightGridMap = null;
                    _sightGrid = null;
                    return null;
                }
                if (_sightGridMap != SelPawn.Map || _sightGridFaction != SelPawn.Faction)
                {
                    _sightGridFaction = SelPawn.Faction;
                    _sightGridMap = SelPawn.Map;
                    _sightGridMap.GetComponent<SightTracker>().TryGetGrid(SelPawn, out _sightGrid);
                }
                return _sightGrid;
            }
        }

        private Map _turretTrackerMap = null;
        private Faction _sturretTrackerFaction = null;
        private TurretTracker _turretTracker = null;
        public TurretTracker MapTurretTracker
        {
            get
            {
                if (!SelPawn.Spawned || SelPawn.Faction == null || SelPawn.Faction == SelPawn.Map.ParentFaction)
                {
                    _turretTracker = null;
                    _turretTrackerMap = null;
                    return null;
                }
                if (_turretTrackerMap != SelPawn.Map || _sturretTrackerFaction != SelPawn.Faction)
                {
                    _sturretTrackerFaction = SelPawn.Faction;
                    _turretTrackerMap = SelPawn.Map;
                    _turretTracker = _turretTrackerMap.GetComponent<TurretTracker>();
                }
                return _turretTracker;
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

        public virtual void TickRarer()
        {
        }

        public virtual void TickShort()
        {
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

        public virtual void Notify_BulletImpactNearBy()
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
