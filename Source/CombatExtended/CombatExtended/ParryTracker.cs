using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public ParryTracker(Map map) : base(map)
        {
        }

        private int GetUsedParriesFor(Pawn pawn)
        {
            ParryCounter counter;
            if (!parryTracker.TryGetValue(pawn, out counter))
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
            ParryCounter counter;
            if (!parryTracker.TryGetValue(pawn, out counter))
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
                foreach (var entry in parryTracker.Where(kvp => kvp.Value.ShouldTimeout()).ToArray())
                {
                    parryTracker.Remove(entry.Key);
                }
            }
        }

        public override void ExposeData()
        {
            // TODO Save parryTracker
        }
    }
}
