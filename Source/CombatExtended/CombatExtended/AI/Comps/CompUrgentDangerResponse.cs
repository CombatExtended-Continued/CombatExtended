using System;
using System.Collections.Generic;
using System.Linq;
using CombatExtended.Utilities;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
    public class CompUrgentDangerResponse : ICompTactics
    {
        private const int MINNORMALIZEDDIR = 25;
        private const int MINNORMALIZEDDIRSQR = MINNORMALIZEDDIR * MINNORMALIZEDDIR;

        private Job dangerJob = null;

        public override int Priority => 1200;

        public CompUrgentDangerResponse()
        {
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref dangerJob, "dangerJob");
        }

        public override Job TryGiveTacticalJob()
        {
            return null;
        }
    }
}