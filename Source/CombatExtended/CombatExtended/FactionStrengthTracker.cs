using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class FactionStrengthTracker : IExposable
    {
        #region Cache
        private string _explanation = null;
        private float _strengthMul = 1.0f;
        private int _explanationExpireAt = -1;
        private int _strengthMulExpireAt = -1;
        #endregion

        private enum FactionRecordType
        {
            None = 0,
            SettlementDestroyed = 1,
            SiteDestroyed = 2,
            EnemyWeakened = 3,
            LeaderKilled = 4
        }

        private struct FactionStrengthRecord : IExposable
        {
            public int createdAt;
            public int duration;
            public float value;
            public FactionRecordType type;
                     
            public int TicksLeft
            {
                get => Mathf.Max(duration - (GenTicks.TicksGame - createdAt), 0);
            }            
            public bool IsExpired
            {
                get => GenTicks.TicksGame - createdAt > duration + 1;
            }            

            public FactionStrengthRecord(FactionRecordType type, float value, int duration)
            {
                this.type = type;                
                this.createdAt = GenTicks.TicksGame;
                this.value = value;
                this.duration = duration;
            }

            public void ExposeData()
            {
                Scribe_Values.Look(ref type, "type");
                Scribe_Values.Look(ref createdAt, "createdAt");
                Scribe_Values.Look(ref value, "value");
                Scribe_Values.Look(ref duration, "duration");
            }
        }        
        private Faction faction;
        private List<FactionStrengthRecord> records = new List<FactionStrengthRecord>();        

        public Faction Faction
        {
            get => faction;
        }
        public float StrengthPointsMultiplier
        {
            get => GetStrengthMultiplier();
        }
        public bool CanRaid
        {
            get => StrengthPointsMultiplier >= 1e-3f;
        }
        public string Explanation
        {
            get => GetExplanation();
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
            Scribe_References.Look(ref faction, "faction");
            Scribe_Collections.Look(ref records, "records", LookMode.Deep);

            records ??= new List<FactionStrengthRecord>();
        }

        public void TickLonger() => records.RemoveAll(r => r.IsExpired);

        public void Notify_LeaderKilled()
        {
            if (Rand.Chance(0.95f))
            {
                Register(FactionRecordType.LeaderKilled, Rand.Range(0.5f, 1.0f), Rand.Range(8, 30) * 60000);
            }
            if (Rand.Chance(0.95f))
            {
                Register(FactionRecordType.LeaderKilled, 0f, Rand.Range(8, 20) * 60000);
            }
        }

        public void Notify_SettlementDestroyed()
        {
            if (Rand.Chance(0.75f))
            {
                Register(FactionRecordType.SettlementDestroyed, Rand.Range(0.6f, 0.8f), Rand.Range(8, 25) * 60000);
            }
            if (Rand.Chance(0.75f))
            {
                Register(FactionRecordType.SettlementDestroyed, 0f, Rand.Range(8, 20) * 60000);
            }
        }

        public void Notify_SiteDestroyed()
        {
            if (Rand.Chance(0.75f))
            {
                Register(FactionRecordType.SiteDestroyed, Rand.Range(0.8f, 1.0f), Rand.Range(4, 30) * 60000);
            }
            if (Rand.Chance(0.75f))
            {
                Register(FactionRecordType.SiteDestroyed, 0f, Rand.Range(4, 15) * 60000);
            }
        }        

        public void Notify_EnemyWeakened()
        {
            if (Rand.Chance(0.25f))
            {
                Register(FactionRecordType.EnemyWeakened, Rand.Range(1.0f, 1.2f), Rand.Range(4, 30) * 60000);
            }            
        }        

        public string GetExplanation()
        {
            if(_explanationExpireAt > GenTicks.TicksGame && !Controller.settings.DebugVerbose)
            {
                return _explanation;
            }
            int minTicksLeft = int.MaxValue;
            string label = faction.HasName ? faction.Name : faction.def.label;
            StringBuilder builder = new StringBuilder();            
            builder.AppendFormat("<color=orange>{0}:</color>", label);
            if (DebugSettings.godMode)
            {
                builder.AppendFormat(" <color=grey>DEBUG: x{0}</color>", StrengthPointsMultiplier);
            }
            builder.AppendInNewLine(" <color=grey>");
            builder.Append("CE_FactionRecord_Explanation_Effects".Translate());
            builder.Append("</color>");
            foreach (var record in records)
            {
                if (record.IsExpired)
                {
                    continue;
                }
                minTicksLeft = Math.Min(record.TicksLeft, minTicksLeft);
                builder.AppendLine();
                builder.Append(" ");
                builder.AppendFormat(GetRecordTypeMessage(record.type), label);
                float daysLeft = (float) Math.Round(record.TicksLeft / 60000f, 1);
                if (record.value == 0f)
                {
                    builder.AppendInNewLine("  - ");
                    builder.AppendFormat("CE_FactionRecord_Explanation_RaidEmbargo".Translate(), daysLeft);
                }
                else
                {
                    builder.AppendInNewLine("  - ");
                    builder.AppendFormat("CE_FactionRecord_Explanation_Strength".Translate(), (float)Math.Round(record.value, 1), daysLeft);
                }
            }
            _explanationExpireAt = GenTicks.TicksGame + minTicksLeft - 1;
            _explanation = builder.ToString();
            return _explanation;
        }

        private void Register(FactionRecordType recordType, float value, int duration)
        {            
            FactionStrengthRecord record = new FactionStrengthRecord(recordType, value, duration);
            records.Add(record);
            _explanationExpireAt = -1;
            _strengthMulExpireAt = -1;
            _ = GetStrengthMultiplier();
            _ = GetExplanation();
            Alert_FactionInfluenced.instance?.Dirty();
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
            if(GenTicks.TicksGame < _strengthMulExpireAt)
            {
                return _strengthMul;
            }
            float multiplier = 1.0f;
            int minTicksLeft = int.MaxValue;            
            for (int i = 0; i < records.Count; i++)
            {
                FactionStrengthRecord record = records[i];
                if (!record.IsExpired)
                {
                    multiplier *= record.value;
                    minTicksLeft = Math.Min(minTicksLeft, record.TicksLeft);
                }
            }
            _strengthMulExpireAt = GenTicks.TicksGame + minTicksLeft - 1;
            _strengthMul = multiplier;            
            return multiplier;
        }
    }
}

