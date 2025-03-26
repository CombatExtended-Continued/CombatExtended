using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class ParryTracker : MapComponent
    {
        private struct ParryCounter
        {
            public int parries;
            private int timeoutTick;

            public ParryCounter(int timeoutTicks)
            {
                parries = 0;
                timeoutTick = Find.TickManager.TicksGame + timeoutTicks;
            }

            public bool ShouldTimeout()
            {
                return Find.TickManager.TicksGame >= timeoutTick;
            }
        }

        private const int SkillPerParry = 4;    // Award another parry per this many skill levels
        private const int TicksToTimeout = 120; // Reset parry counter after this many ticks

        private Dictionary<Pawn, ParryCounter> parryTracker = new Dictionary<Pawn, ParryCounter>();
        private List<Pawn> reusablePawnList = new List<Pawn>();

        public ParryTracker(Map map) : base(map)
        {
        }

        private int GetUsedParriesFor(Pawn pawn)
        {
            if (!parryTracker.TryGetValue(pawn, out ParryCounter counter))
            {
                return 0;
            }
            return counter.parries;
        }

        public bool CheckCanParry(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Error("CE tried checking CanParry with Null-Pawn");
                return false;
            }
            var skill = pawn.skills?.GetSkill(SkillDefOf.Melee);
            int parrySkill;
            if (skill != null)
            {
                parrySkill = Mathf.RoundToInt(skill.Level / SkillPerParry);
            }
            else
            {
                parrySkill = pawn.def.GetModExtension<RacePropertiesExtensionCE>()?.maxParry ?? 1;
            }
            int parriesLeft = parrySkill - GetUsedParriesFor(pawn);
            return parriesLeft > 0;
        }

        public void RegisterParryFor(Pawn pawn, int timeoutTicks)
        {
            if (!parryTracker.TryGetValue(pawn, out ParryCounter counter))
            {
                // Register new pawn in tracker
                counter = new ParryCounter(timeoutTicks);
                parryTracker.Add(pawn, counter);
            }
            counter.parries++;
        }

        public void ResetParriesFor(Pawn pawn)
        {
            parryTracker.Remove(pawn);
        }

        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % 10 == 0)
            {
                reusablePawnList.Clear();

                foreach (var kvp in parryTracker)
                {
                    if (kvp.Value.ShouldTimeout())
                    {
                        reusablePawnList.Add(kvp.Key);
                    }
                }

                for (int i = 0; i < reusablePawnList.Count; i++)
                {
                    parryTracker.Remove(reusablePawnList[i]);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            List<Pawn> pawns = null;
            List<ParryCounter> counters = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                pawns = new List<Pawn>();
                counters = new List<ParryCounter>();

                foreach (var kvp in parryTracker)
                {
                    pawns.Add(kvp.Key);
                    counters.Add(kvp.Value);
                }
            }
            Scribe_Collections.Look(ref pawns, "parryPawns", LookMode.Reference);
            Scribe_Collections.Look(ref counters, "parryCounters", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                parryTracker.Clear();

                if (pawns != null && counters != null && pawns.Count == counters.Count)
                {
                    for (int i = 0; i < pawns.Count; i++)
                    {
                        if (pawns[i] != null)
                        {
                            parryTracker[pawns[i]] = counters[i];
                        }

                    }
                }
            }
        }
    }
}
