using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class FactionStrengthTracker : IExposable
    {
        private struct FactionStrengthRecord : IExposable
        {            
            private int createdAt;

            public float strengthMultiplier;
            public int raidEmbargoDuration;
            public int strengthMultiplierDuration;
            public FactionRecordType type;
            
            public int Age => GenTicks.TicksGame - createdAt;

            public int StrengthMultiplierTicksLeft => Math.Max(strengthMultiplierDuration - Age, 0);

            public int RaidEmbargoTicksLeft => Math.Max(raidEmbargoDuration - Age, 0);

            public bool IsExpired => StrengthMultiplierTicksLeft == 0 && RaidEmbargoTicksLeft == 0;

            public FactionStrengthRecord(float strengthMultiplier, int raidEmbargoDuration, int strengthMultiplierDuration, FactionRecordType type)
            {
                this.type = type;
                this.strengthMultiplier = strengthMultiplier;
                this.createdAt = GenTicks.TicksGame;
                this.raidEmbargoDuration = raidEmbargoDuration;
                this.strengthMultiplierDuration = strengthMultiplierDuration;                
            }

            public void ExposeData()
            {
                Scribe_Values.Look(ref type, "type");
                Scribe_Values.Look(ref createdAt, "createdAt");
                Scribe_Values.Look(ref strengthMultiplier, "strengthMultiplier");
                Scribe_Values.Look(ref strengthMultiplierDuration, "strengthMultiplierDuration");
                Scribe_Values.Look(ref raidEmbargoDuration, "raidEmbargoDuration");
            }
        }

        private int raidEmbargoEndsAt = -1;
        private Faction faction;
        private List<FactionStrengthRecord> records = new List<FactionStrengthRecord>();

        public Faction Faction
        {
            get => faction;
        }

        public float RaidPointsMultiplier
        {
            get => CanRaid ? GetStrengthMultiplier() : 0;
        }

        public bool CanRaid
        {
            get => raidEmbargoEndsAt - 120 < GenTicks.TicksGame;
        }

        public FactionStrengthTracker()
        {
        }        

        public FactionStrengthTracker(Faction faction)
        {
            this.faction = faction;
        }        

        public void ExposeData()
        {
            Scribe_Values.Look(ref raidEmbargoEndsAt, "raidEmbargoEndsAt");
            Scribe_References.Look(ref faction, "faction");
            Scribe_Collections.Look(ref records, "records", LookMode.Deep);

            records ??= new List<FactionStrengthRecord>();
        }

        public void TickLonger() => records.RemoveAll(r => r.IsExpired);

        public void Notify_LeaderKilled()
        {
            Register(FactionRecordType.LeaderKilled, Rand.Range(0.5f, 1.0f), Rand.Range(4, 25) * 60000, Rand.Chance(0.5f) ? Rand.Range(0, 20) * 60000 : 0);
        }

        public void Notify_SettlementDestroyed()
        {
            Register(FactionRecordType.SettlementDestroyed, Rand.Range(0.6f, 0.8f), Rand.Range(4, 25) * 60000, Rand.Range(7, 20) * 60000);
        }

        public void Notify_SiteDestroyed()
        {
            Register(FactionRecordType.SiteDestroyed, Rand.Range(0.8f, 1.0f), Rand.Range(4, 25) * 60000, Rand.Range(3, 9) * 60000);
        }        

        public void Notify_EnemyWeakened()
        {
            Register(FactionRecordType.EnemyWeakened, Rand.Range(1.0f, 1.5f), Rand.Range(4, 25) * 60000, 0);
        }

        public void Register(FactionRecordType recordType, float multiplier, int strengthMultiplierDuration) => Register(recordType, multiplier, strengthMultiplierDuration, 0);

        public void Register(FactionRecordType recordType, float multiplier, int strengthMultiplierDuration, int raidEmbargoDuration)
        {
            int ticks = GenTicks.TicksGame;
            if (raidEmbargoEndsAt < ticks)
            {
                raidEmbargoEndsAt = ticks; 
            }
            raidEmbargoEndsAt += raidEmbargoDuration;
            FactionStrengthRecord record = new FactionStrengthRecord(multiplier, raidEmbargoDuration, strengthMultiplierDuration, recordType);
            records.Add(record);
            // update the notification
            Alert_FactionInfluenced.instance?.Dirty();
        }

        public string GetExplanation()
        {
            string label = faction.HasName ? faction.Name : faction.def.label;
            StringBuilder builder = new StringBuilder();            
            builder.AppendFormat("<color=orange>{0}:</color>", label);
            builder.AppendInNewLine(" <color=grey>");
            builder.Append("CE_FactionRecord_Explanation_Effects".Translate());
            builder.Append("</color>");
            foreach (var record in records)
            {
                if (record.IsExpired || (record.strengthMultiplier == 1.0f && record.raidEmbargoDuration == 0))
                {
                    continue;
                }                
                builder.AppendLine();
                builder.Append(" ");
                builder.AppendFormat(GetRecordTypeMessage(record.type), label);
                if (record.raidEmbargoDuration > 0)
                {
                    builder.AppendInNewLine("  -");
                    builder.AppendFormat("CE_FactionRecord_Explanation_RaidEmbargo".Translate(), (int)(record.RaidEmbargoTicksLeft / 60000));
                }
                if (record.strengthMultiplier != 1.0f)
                {
                    builder.AppendInNewLine("  -");
                    builder.AppendFormat("CE_FactionRecord_Explanation_Strength".Translate(), (float)Math.Round(record.strengthMultiplier, 1), record.StrengthMultiplierTicksLeft / 60000);
                }
            }
            return builder.ToString();
        }

        private string GetRecordTypeMessage(FactionRecordType type)
        {
            switch (type)
            {
                case FactionRecordType.None:
                    return "CE_FactionRecord_Explanation_None".Translate();
                case FactionRecordType.SettlementDestroyed:
                    return "CE_FactionRecord_Explanation_SettlementDestroyed".Translate();
                case FactionRecordType.SiteDestroyed:
                    return "CE_FactionRecord_Explanation_SiteDestroyed".Translate();                
                case FactionRecordType.EnemyWeakened:
                    return "CE_FactionRecord_Explanation_EnemyWeakened".Translate();
                case FactionRecordType.LeaderKilled:
                    return "CE_FactionRecord_Explanation_LeaderKilled".Translate();
            }
            throw new NotImplementedException();
        }

        private float GetStrengthMultiplier()
        {
            float multiplier = 1.0f;
            for (int i = 0; i < records.Count; i++)
            {
                if (!records[i].IsExpired)
                {
                    multiplier *= records[i].strengthMultiplier;
                }
            }
            return multiplier;
        }
    }
}

