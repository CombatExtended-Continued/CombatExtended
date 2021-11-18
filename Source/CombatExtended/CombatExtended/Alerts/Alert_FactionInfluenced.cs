using System;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Alert_FactionInfluenced : Alert
    {
        private int _ticks = -1;
        private string message = "";
        private AlertReport _report = false;

        public override Color BGColor => Color.clear;

        public static Alert_FactionInfluenced instance;

        public Alert_FactionInfluenced()
        {
            defaultLabel = "";
            defaultExplanation = "";
            instance = this;
        }

        public override string GetLabel()
        {
            return "CE_EnemyInfluenced".Translate();
        }

        public override TaggedString GetExplanation() => message;

        public override AlertReport GetReport()
        {
            if(GenTicks.TicksGame - _ticks < 15000)
            {
                return _report;
            }
            _ticks = GenTicks.TicksGame;
            _report = false;
            StringBuilder builder = new StringBuilder();
            WorldStrengthTracker worldTracker = Find.World.GetComponent<WorldStrengthTracker>();
            int i = 0;
            foreach (Faction faction in Find.World.factionManager.AllFactions)
            {
                FactionStrengthTracker tracker = faction.GetStrengthTracker();
                if(tracker != null && (tracker.StrengthPointsMultiplier != 1.0f || !tracker.CanRaid))
                {
                    if(i > 0)
                    {
                        builder.AppendInNewLine("\t");
                    }
                    _report = AlertReport.Active;
                    builder.AppendInNewLine(tracker.GetExplanation());
                    i++;
                }
            }
            message = builder.ToString();
            return _report;
        }

        public void Dirty() => _ticks = -1;
    }
}

