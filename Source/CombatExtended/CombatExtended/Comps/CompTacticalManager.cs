using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using CombatExtended.AI;
using MonoMod.Utils;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended
{    
    public class CompTacticalManager : ThingComp
    {        
        private const int LASTATTACKTARGET_MAXTICKS = 240;

        [StructLayout(LayoutKind.Sequential)]
        private struct TargetingRecord : IExposable
        {
            private WeakReference refPawn;
            private int createdAt;
            private bool isHostile;

            public bool IsValid
            {
                get => refPawn.SafeGetIsAlive()
                    && refPawn.SafeGetTarget() is Pawn pawn
                    && pawn.Spawned
                    && GenTicks.TicksGame - createdAt < LASTATTACKTARGET_MAXTICKS                    
                    && !pawn.Destroyed
                    && !pawn.Downed
                    && !pawn.Dead;
            }

            public bool IsHostile
            {
                get => isHostile;
            }

            public Pawn Pawn
            {
                get => (Pawn) refPawn.SafeGetTarget();
            }

            public TargetingRecord(Pawn targeter, Pawn target)
            {
                this.refPawn = new WeakReference(targeter);
                this.createdAt = GenTicks.TicksGame;
                this.isHostile = targeter.Faction == null || target.Faction == null || target.Faction.HostileTo(targeter.Faction); 
            }

            public void Renew()
            {
                this.createdAt = GenTicks.TicksGame;
            }

            public void ExposeData()
            {
                Pawn pawn = (Pawn)(refPawn?.SafeGetTarget() ?? null);
                Scribe_References.Look(ref pawn, "pawn");
                if(Scribe.mode != LoadSaveMode.Saving)
                    this.refPawn = new WeakReference(pawn);
                Scribe_Values.Look(ref createdAt, "createdAt", -1);
                Scribe_Values.Look(ref isHostile, "isHostile", true);
            }
        }
        
        private List<TargetingRecord> targetedBy = new List<TargetingRecord>();        

        private Pawn _pawn = null;
        public Pawn SelPawn
        {
            get
            {
                return _pawn ?? (_pawn = parent as Pawn);
            }
        }

        private List<ICompTactics> _tacticalComps = new List<ICompTactics>();
        public List<ICompTactics> TacticalComps
        {
            get
            {
                if ((_tacticalComps?.Count ?? 0) == 0)
                    ValidateComps();
                return _tacticalComps;
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
        
        public IEnumerable<Pawn> TargetedBy
        {
            get
            {
                return targetedBy.Where(t => t.IsValid).Select(t => t.Pawn);
            }
        }
       
        public IEnumerable<Pawn> TargetedByEnemy
        {
            get
            {                
                return targetedBy.Where(t => t.IsValid && (t.Pawn.Faction == null || SelPawn.Faction == null || SelPawn.Faction.HostileTo(t.Pawn.Faction))).Select(t => t.Pawn);
            }
        }

        public bool DraftedColonist
        {
            get
            {
                return (SelPawn.Faction?.IsPlayer ?? false) && SelPawn.Drafted;
            }
        }
        
        public override void CompTick()
        {
            if (parent.IsHashIntervalTick(30))
            {
                if (GenTicks.TicksGame - SelPawn.LastAttackTargetTick < LASTATTACKTARGET_MAXTICKS && SelPawn.LastAttackedTarget.Pawn is Pawn other)
                    other.GetTacticalManager()?.Notify_BeingTargetedBy(SelPawn);
            }            
            base.CompTick();
            if (parent.IsHashIntervalTick(60)) targetedBy.RemoveAll(r => !r.IsValid);
        }

        private int _counter = 0;

        public override void CompTickRare()
        {
            base.CompTickRare();
            TryGiveTacticalJobs();
            if (_counter++ % 2 == 0) TickRarer();
        }

        public void Notify_BeingTargetedBy(Pawn pawn)
        {
            bool renewed = false;
            for (int i = 0; i < targetedBy.Count; i++)
            {
                TargetingRecord record = targetedBy[i];
                if (record.Pawn == pawn)
                {
                    record.Renew();
                    renewed = true;
                    break;
                }
            }
            if (!renewed) targetedBy.Add(new TargetingRecord(pawn, SelPawn));            
        }

        public bool TryStartCastChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            if (CompSuppressable == null || SelPawn.MentalState != null || CompSuppressable.IsHunkering)
                return true;

            bool AllChecksPassed(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg, out ICompTactics failedComp)
            {
                foreach (ICompTactics comp in TacticalComps)
                {
                    if (!comp.StartCastChecks(verb, castTarg, destTarg))
                    {
                        failedComp = comp;
                        return false;
                    }
                }
                failedComp = null;
                return true;
            }

            ICompTactics failedComp = null;

            if (!CompSuppressable.IsHunkering && (SelPawn.jobs.curDriver is IJobDriver_Tactical || AllChecksPassed(verb, castTarg, destTarg, out failedComp)))
            {
                foreach (ICompTactics comp in TacticalComps)
                    comp.Notify_StartCastChecksSuccess(verb);
                return true;
            }
            else
            {
                foreach (ICompTactics comp in TacticalComps)
                    comp.Notify_StartCastChecksFailed(failedComp);
                return false;
            }
        }

        public void Notify_BulletImpactNearby()
        {
            foreach (ICompTactics comp in TacticalComps)
            {
                try
                {
                    comp.Notify_BulletImpactNearBy();
                }
                catch (Exception er)
                {
                    Log.Error($"CE: Error running Notify_BulletImpactNearBy {comp.GetType()} with error {er}");
                }
            }
        }

        public T GetTacticalComp<T>() where T : ICompTactics
        {
            return (T)TacticalComps.FirstOrFallback(c => c is T, null);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            try
            {
                // scribe targeting records
                targetedBy.RemoveAll(t => !t.IsValid);
                Scribe_Collections.Look(ref targetedBy, "targetedBy", LookMode.Deep);
                // scribe AI comps
                Scribe_Collections.Look(ref _tacticalComps, "tacticalComps", LookMode.Deep);
            }
            catch (Exception er)
            {
                Log.Error($"CE: Error scribing {parent} {er}");
            }
            finally
            {
                this.targetedBy ??= new List<TargetingRecord>();
                this.ValidateComps();
            }
        }

        private void TickRarer()
        {
            foreach (ICompTactics comp in TacticalComps)
            {
                try
                {
                    comp.TickRarer();
                }
                catch (Exception er)
                {
                    Log.Error($"CE: Error ticking comp {comp.GetType()} with error {er}");
                }
            }
        }

        private void TryGiveTacticalJobs()
        {
            if (CompSuppressable == null || CompSuppressable.IsHunkering || !SelPawn.Spawned || SelPawn.Downed)
            {
                return;
            }
            foreach (ICompTactics comp in TacticalComps)
            {
                Job job = comp.TryGiveTacticalJob();
                if (job != null)
                {
                    SelPawn.jobs.StartJob(job, JobCondition.InterruptForced);
                    return;
                }
            }
        }

        private void ValidateComps()
        {
            if (_tacticalComps == null)
                _tacticalComps = new List<ICompTactics>();
            foreach (Type type in typeof(ICompTactics).AllSubclassesNonAbstract())
            {
                ICompTactics comp;
                if ((comp = _tacticalComps.FirstOrFallback(t => t.GetType() == type)) == null)
                    _tacticalComps.Add(comp = (ICompTactics)Activator.CreateInstance(type, new object[0]));
                comp.Initialize(SelPawn);
            }
            _tacticalComps.SortBy(t => -1f * t.Priority);
        }
    }
}
