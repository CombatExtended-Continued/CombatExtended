using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;


namespace CombatExtended.WorldObjects
{
    public class ShellingComp : WorldObjectComp
    {
        private int _tickerId = Math.Abs((_tickerIdCounter += Rand.Int % UInt16.MaxValue + 1) & Rand.Int);
        private static int _tickerIdCounter = 0;        
        
        public struct ShellingRecord : IExposable
        {
            public float dmgMap;
            public float dmgTile;
            public float dmgMultipler;       

            public float DamageToTile => dmgTile * dmgMultipler;
            public float DamageToMap => dmgMap * dmgMultipler;

            public void ExposeData()
            {
                Scribe_Values.Look(ref dmgMap, "dmgMap");
                Scribe_Values.Look(ref dmgTile, "dmgTile");
                Scribe_Values.Look(ref dmgMultipler, "dmgMultipler");
            }
        }

        private int reconstructionCounter = 0; 
        private int reconstructionTicks = 0;
        private float chock = 0f;
        private float _health = 1.0f;
        private List<ShellingRecord> records = new List<ShellingRecord>();

        public float Health
        {
            get
            {
                return _health;
            }
            private set
            {
                _health = Mathf.Clamp01(value);
            }
        }

        public float TechLevelDamageMultiplier
        {
            get
            {
                int techLevel = FactionTechLevel;            
                if(techLevel <= 3)
                {
                    return 1.0f;
                }
                return 3f / techLevel;
            }
        }
        
        public int FactionTechLevel => (int) (parent.Faction?.def.techLevel ?? 0);
        public bool OutOfService => reconstructionTicks > 0;        
        public bool Damaged => Health != 1.0f;
        public bool FullHealth => Health == 1.0f;        

        public ShellingComp()
        {
            _tickerId = _tickerIdCounter;
            _tickerIdCounter += Rand.Int % UInt16.MaxValue + 1;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref _health, "health", 1.0f);
            Scribe_Values.Look(ref reconstructionTicks, "reconstructionTicks");
            Scribe_Values.Look(ref reconstructionCounter, "reconstructionCounter");
            Scribe_Collections.Look(ref records, "records", LookMode.Deep);
            records ??= new List<ShellingRecord>();            
        }

        public override void CompTick()
        {
            base.CompTick();
            if((GenTicks.TicksGame + _tickerId) % GenTicks.TickRareInterval == 0)
            {
                TickRare();
            }
            if(reconstructionTicks > 0)
            {
                reconstructionTicks--;                
            }
            if (FactionTechLevel <= 4 || !OutOfService)
            {
                return;
            }
            if ((GenTicks.TicksGame + _tickerId) % GenTicks.TickLongInterval == 0)
            {
                TickLong();
            }
        }

        public void ApplyDamage(TravelingShellProperties shellProperties, Faction enemyFaction, int sourceTile = -1)
        {            
            // we use this to create a chock to extreem shelling
            chock += 1 + shellProperties.damage_Tile * 100 * TechLevelDamageMultiplier;

            ShellingRecord record = new ShellingRecord();            
            record.dmgMultipler = TechLevelDamageMultiplier;
            record.dmgMap = shellProperties.damage_Map;
            record.dmgTile = shellProperties.damage_Tile;            
            this.records.Add(record);            
            this.Health = Health - record.DamageToTile;            
            // try to perform a repsonse
            this.Notify_ShelledBy(enemyFaction, sourceTile);

            // check if the health is too low then put the object out of service
            if (this.Health == 0)
            {
                if (parent.questTags.NullOrEmpty())
                {
                    if (parent is Site site)
                    {
                        // TODO
                        // figure out what to do with this
                    }
                    else if (FactionTechLevel > 6 || Rand.Chance(1 / Mathf.Clamp(FactionTechLevel - reconstructionCounter, 1, 8))) // a high tech faction can sort repair multiple times.
                    {
                        reconstructionTicks = 60000 * Rand.Range(4, 24);
                        reconstructionCounter++;
                    }else
                    {
                        parent.Destroy();
                        Notify_DestoyedBy(enemyFaction, sourceTile);
                    }
                }
                // now go through all quests and see if we finished something
                else if (!Find.QuestManager.quests.Where(q => q.parts.Any(p => p is QuestPart_DestroyWorldObject part && part.worldObject == parent)).EnumerableNullOrEmpty())
                {
                    parent.Destroy();
                }                               
            }
        }

        public void Notify_DestoyedBy(Faction enemyFaction, int sourceTile = -1)
        {
            // TODO
            // send a letter
        }

        public void Notify_ShelledBy(Faction enemyFaction, int sourceTile = -1)
        {
            Faction faction = parent.Faction;
            if (faction != null && enemyFaction != faction && !faction.IsPlayerSafe())
            {
                FactionRelation relation = enemyFaction.RelationWith(faction, true);
                if (relation == null)
                {
                    faction.TryMakeInitialRelationsWith(enemyFaction);
                    relation = faction.RelationWith(enemyFaction, false);
                }
                faction.TryAffectGoodwillWith(enemyFaction, -70, canSendMessage: true, canSendHostilityLetter: true, HistoryEventDefOf.AttackedSettlement);
            }
            if (ParentHasMap) // if the tile map is active, don't try any funny stuff
            {
                return;
            }
            // here we can responed to the attack
            float techMultipler = (FactionTechLevel - 4f) / 4f;

        }

        private void TickRare()
        {
            // reduce the chock value
            chock = Mathf.Max(chock - 50f / 60000f * GenTicks.TickRareInterval, 0f);
            // heal the current object
            if (Damaged) {
                // high tech factions should repair at a very high rate
                // for industrial and lower the value is 1x
                // the equation is (0.5 * FactionTechLevel - 1)^2 / 1f                    
                float techMultipler = FactionTechLevel > 4 ? Mathf.Max(Mathf.Pow(FactionTechLevel / 2f - 1, 2), 1.0f) / 2f : 1.0f;                
                if (!records.NullOrEmpty())
                {                    
                    float dH = GenTicks.TickRareInterval * 0.1f / 60000f * records.Count * techMultipler;
                    for (int i = 0; i < records.Count; i++)
                    {
                        ShellingRecord record = records[i];
                        record.dmgTile -= GenTicks.TickRareInterval * 0.05f / 60000f * techMultipler;
                        record.dmgMap -= GenTicks.TickRareInterval * 1500 / 60000f; // for beauty reasons we don't heal this as much
                        records[i] = record;
                    }
                    records.RemoveAll(r => r.DamageToTile <= 0);
                    // now limit how fast repairs can happen
                    Health += Mathf.Min(dH, GenTicks.TickRareInterval * 0.05f / 60000f * techMultipler);
                }
                else
                {
                    // if all records are already removed speed up the recovery
                    Health += GenTicks.TickRareInterval * 0.1f / 60000f * techMultipler;
                }                                              
            }
        }

        private void TickLong()
        {
            // TODO add MTB for random attacks in case of a recent attack
        }
    }
}

