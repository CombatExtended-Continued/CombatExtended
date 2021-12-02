using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using CombatExtended.AI;
using MonoMod.Utils;
using RimWorld;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class CompTacticalManager : ThingComp
    {               
        private Job curJob = null;
        private List<Verse.WeakReference<Pawn>> targetedBy = new List<Verse.WeakReference<Pawn>>();        

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

        private int _targetedByTick = -1;
        private List<Pawn> _targetedByCache = new List<Pawn>();
        public List<Pawn> TargetedBy
        {
            get
            {
                if (_targetedByTick != GenTicks.TicksGame || _targetedByTick == -1)
                {
                    _targetedByTick = GenTicks.TicksGame;
                    _targetedByCache.Clear();

                    Job job;
                    for (int i = 0; i < targetedBy.Count; i++)
                    {
                        try
                        {
                            Verse.WeakReference<Pawn> reference = targetedBy[i];
                            Pawn pawn;
                            if (reference.SafeGetIsAlive() && (job = (pawn = (Pawn)reference.SafeGetTarget())?.jobs.curJob) != null && (pawn?.Spawned ?? false))
                            {
                                if (job.AnyTargetIs(parent))
                                    _targetedByCache.Add(pawn);
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                return _targetedByCache;
            }
        }

        private int _targetedByEnemyTick = -1;
        private List<Pawn> _targetedByEnemyCache = new List<Pawn>();
        public List<Pawn> TargetedByEnemy
        {
            get
            {
                if (_targetedByEnemyTick != GenTicks.TicksGame || _targetedByEnemyTick == -1)
                {
                    _targetedByEnemyTick = GenTicks.TicksGame;
                    _targetedByEnemyCache.Clear();

                    List<Pawn> pawns = TargetedBy;
                    for (int i = 0; i < pawns.Count; i++)
                    {
                        Pawn pawn = pawns[i];
                        if (pawn.HostileTo(parent))
                            _targetedByEnemyCache.Add(pawn);
                    }
                }
                return _targetedByEnemyCache;
            }
        }

        public bool DraftedColonist
        {
            get
            {
                return (SelPawn.Faction?.IsPlayer ?? false) && SelPawn.Drafted;
            }
        }

        private readonly TargetIndex[] _targetIndices = new TargetIndex[]
        {
            TargetIndex.A,
            TargetIndex.B,
            TargetIndex.C,
        };

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(20)) TickShort();
            if (parent.IsHashIntervalTick(120))
            {                
                /*
                 * Clear the cache if it's very outdated to allow GC to take over
                 */
                if (_targetedByTick != -1 && GenTicks.TicksGame - _targetedByTick > 300)
                {
                    _targetedByCache.Clear();
                    _targetedByEnemyCache.Clear();
                    _targetedByTick = -1;
                    _targetedByEnemyTick = -1;
                }
                Job job;
                /*
                 * Start scaning for possilbe current targets
                 */
                if (parent.Spawned
                    && curJob != (job = SelPawn.jobs?.curJob)
                    && job != null && job.def.alwaysShowWeapon == false)
                {
                    if (SelPawn.mindState?.enemyTarget is Pawn target && target.Spawned)
                        target.GetTacticalManager()?.Notify_BeingTargetedBy(target);
                    /*
                     * Scan the current job to check for potential target pawns
                     */
                    HashSet<Pawn> targets = new HashSet<Pawn>();
                    for (int i = 0; i < _targetIndices.Length; i++)
                    {
                        LocalTargetInfo info = job.GetTarget(_targetIndices[i]);

                        if (info.HasThing && info.Thing is Pawn pawn && pawn.Spawned)
                            targets.Add(pawn);
                    }
                    if (job.targetQueueA != null)
                    {
                        for (int i = 0; i < job.targetQueueA.Count; i++)
                        {
                            LocalTargetInfo info = job.targetQueueA[i];

                            if (info.HasThing && info.Thing is Pawn pawn && pawn.Spawned)
                                targets.Add(pawn);
                        }
                    }
                    if (job.targetQueueB != null)
                    {
                        for (int i = 0; i < job.targetQueueB.Count; i++)
                        {
                            LocalTargetInfo info = job.targetQueueB[i];

                            if (info.HasThing && info.Thing is Pawn pawn && pawn.Spawned)
                                targets.Add(pawn);
                        }
                    }

                    // Notify others of this pawn targeting them
                    foreach (Pawn other in targets)
                    {
                        if (other.thingIDNumber != parent.thingIDNumber)
                            other.GetTacticalManager()?.Notify_BeingTargetedBy(other);
                    }
                    targets.Clear();
                }
            }
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
            for (int i = 0; i < targetedBy.Count; i++)
            {
                if (targetedBy[i].Target == pawn)
                    return;
            }
            targetedBy.Add(new Verse.WeakReference<Pawn>(pawn));
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
                Scribe_Collections.Look(ref _tacticalComps, "tacticalComps", LookMode.Deep);
            }
            catch (Exception er)
            {
                Log.Error($"CE: Error scribing {parent} {er}");
            }
            finally
            {
                this.ValidateComps();
            }
        }

        private void TickRarer()
        {
            List<ICompTactics> comps = TacticalComps;
            for (int i = 0; i < comps.Count; i++)
            {
                try
                {
                    comps[i].TickRarer();
                }
                catch (Exception er)
                {
                    Log.Error($"CE: Error ticking comp {comps[i].GetType()} with error {er}");
                }
            }
        }

        private void TickShort()
        {            
            List<ICompTactics> comps = TacticalComps;
            for(int i =0;i < comps.Count; i++)
            {
                try
                {
                    comps[i].TickShort();
                }
                catch (Exception er)
                {
                    Log.Error($"CE: Error ticking short comp {comps[i].GetType()} with error {er}");
                }
            }
        }

        private void TryGiveTacticalJobs()
        {
            if (CompSuppressable == null || CompSuppressable.IsHunkering || !SelPawn.Spawned || SelPawn.Downed)
            {
                return;
            }
            List<ICompTactics> comps = TacticalComps;
            for (int i = 0; i < comps.Count; i++)
            {                
                Job job = comps[i].TryGiveTacticalJob();
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
