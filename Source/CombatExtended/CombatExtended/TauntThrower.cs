using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Multiplayer.API;
using RimWorld;
using Verse;
using Verse.Grammar;
using UnityEngine;

namespace CombatExtended
{
    public class TauntThrower : MapComponent
    {
        private const int minTicksBetweenTaunts = 180;

        private Dictionary<Pawn, int> tauntTickTracker = new Dictionary<Pawn, int>();

        public TauntThrower(Map map) : base(map)
        {
        }

        private bool AllowThrowTauntNow(Pawn pawn)
        {
            if(!Controller.settings.ShowTaunts || pawn == null || !pawn.def.race.Humanlike)
            {
                return false;
            }
            return TimedOut(pawn);
        }

        private bool TimedOut(Pawn pawn)
        {
            int lastTauntTick;
            tauntTickTracker.TryGetValue(pawn, out lastTauntTick);
            return Find.TickManager.TicksGame - lastTauntTick > minTicksBetweenTaunts;
        }

        public void TryThrowTaunt(RulePackDef rulePack, Pawn pawn)
        {
            if (!AllowThrowTauntNow(pawn))
                return;

            string taunt = GrammarResolver.Resolve(rulePack.RulesPlusIncludes[0].keyword, new GrammarRequest { Includes = { rulePack } });
            if (taunt.NullOrEmpty())
            {
                Log.Warning("CE tried throwing invalid taunt for " + pawn.ToString());
            }
            else
            {
                MoteMakerCE.ThrowText(pawn.Position.ToVector3Shifted(), pawn.Map, taunt);
            }
            var curTick = Find.TickManager.TicksGame;
            if (!tauntTickTracker.ContainsKey(pawn))
            {
                tauntTickTracker.Add(pawn, curTick);
            }
            else
            {
                tauntTickTracker[pawn] = curTick;
            }
        }

        public override void MapComponentTick()
        {
            // Use map.uniqueId as it'll be the same for each player, GetHashCode() is not guaranteed to be the same
            // for each player (unless it's a custom implementation)
            // and we want this to be called at the same time for everyone
            if ((Find.TickManager.TicksGame + map.uniqueID * 3) % 10 == 0)
            {
                foreach (var entry in tauntTickTracker.Where(kvp => TimedOut(kvp.Key)).ToArray())
                {
                    tauntTickTracker.Remove(entry.Key);
                }
            }
        }
    }
}

