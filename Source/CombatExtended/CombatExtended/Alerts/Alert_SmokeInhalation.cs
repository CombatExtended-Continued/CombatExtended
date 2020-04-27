using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace CombatExtended.CombatExtended.Alerts
{
    class Alert_SmokeInhalation : Alert_Critical
    {
        private List<Pawn> SmokePoisonedPawns
        {
            get
            {
                List<Pawn> smokePoisonedPawns = new List<Pawn>();
                List<Pawn> colonistsPrisonersAnimals = new List<Pawn>();
                colonistsPrisonersAnimals.AddRange(PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction_NoCryptosleep);
                colonistsPrisonersAnimals.AddRange(PawnsFinder.AllMaps_PrisonersOfColonySpawned);
                foreach (Pawn p in colonistsPrisonersAnimals)
                {
                    if (p.GetStatValue(CE_StatDefOf.SmokeSensitivity) > 0f)
                    {
                        float smokeInhalationSeverity = p.health?.hediffSet?.GetFirstHediffOfDef(CE_HediffDefOf.SmokeInhalation, true)?.Severity ?? 0f;
                        if (smokeInhalationSeverity > 0.4f)
                        {
                            smokePoisonedPawns.Add(p);
                        }
                    }
                }
                return smokePoisonedPawns;
            }
        }

        public override string GetLabel()
        {
            return "CE_SmokeInhalation".Translate();
        }

        public override TaggedString GetExplanation()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (Pawn pawn in this.SmokePoisonedPawns)
            {
                stringBuilder.AppendLine("  - " + pawn.NameShortColored.Resolve());
            }
            return "CE_SmokeInhalationDesc".Translate(stringBuilder.ToString());
        }

        public override AlertReport GetReport()
        {
            return AlertReport.CulpritsAre(this.SmokePoisonedPawns);
        }

        protected override Color BGColor
        {
            get
            {
                float num = Pulser.PulseBrightness(0.5f, Pulser.PulseBrightness(0.5f, 0.6f));
                return new Color(num, num, num) * new Color(1.0f, 0.25f, 0.0f);
            }
        }
    }
}
