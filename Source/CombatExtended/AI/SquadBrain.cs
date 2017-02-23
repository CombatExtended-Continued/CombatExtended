using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.AI
{
    public class SquadBrain : IExposable
    {
        #region Structs

        private struct PawnAssignment
        {
            private int priority;
            private Pawn assignedPawn;

            private PawnAssignment(int priority, Pawn assignee)
            {
                this.priority = priority;
                this.assignedPawn = assignee;
            }
        }

        #endregion

        #region Fields

        public SquadBrainManager squadBrainManager;
        private bool initialized = false;
        private List<Pawn> ownedPawns = new List<Pawn>();                   // Pawns directly in this squad
        private float ownedForceInt;                                        // The overall combat power of this squad
        private float heavyFirepowerInt;                                    // Squad's ability to destroy fortifications
        private Faction faction;
        private List<SquadBrain> subordinates = new List<SquadBrain>();     // Subordinate squads created by this squad
        private SquadBrain commander;
        private RaidGoalFinder objective;
        private SquadObjective currentTarget;

        // Target trackers
        private Dictionary<TargetInfo, PawnAssignment> trackerSuppress = new Dictionary<TargetInfo, PawnAssignment>();
        private Dictionary<TargetInfo, PawnAssignment> trackerSnipe = new Dictionary<TargetInfo, PawnAssignment>();
        private Dictionary<TargetInfo, PawnAssignment> trackerFlank = new Dictionary<TargetInfo, PawnAssignment>();

        #endregion

        #region Properties

        public Map Map { get { return squadBrainManager.map; } }
        public float OwnedForce { get { return ownedForceInt; } }
        public float HeavyFirepower { get { return heavyFirepowerInt; } }

        #endregion

        #region Methods

        private static float GetForcePowerFor(List<Pawn> list)
        {
            float power = 0f;
            foreach (Pawn pawn in list)
            {
                power++;    // TODO
            }
            return power;
        }

        public virtual void ExposeData()
        {
            throw new NotImplementedException();
        }

        public void Init()
        {
            initialized = true;
            ownedForceInt = GetForcePowerFor(ownedPawns);
            foreach (SquadBrain subordinate in subordinates)
            {
                ownedForceInt += subordinate.OwnedForce;
            }
            TryGetNewOrders();
        }

        public void SquadBrainTick()
        {
            if (!initialized) Init();
        }

        private void TryGetNewOrders()
        {
            if (commander != null)
            {
                currentTarget = commander.GetOrdersForSubordinate(this);
                return;
            }
            if (objective != null)
            {
                currentTarget = objective.GetOrdersFor(this);
                return;
            }
            Log.Error("CE :: SquadBrain has neither commander nor objective, it can't obtain orders.");
            return;
        }

        private SquadObjective GetOrdersForSubordinate(SquadBrain subordinate)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
