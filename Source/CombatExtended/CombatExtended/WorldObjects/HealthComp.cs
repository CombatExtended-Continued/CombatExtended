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
    public class HealthComp : WorldObjectComp
    {
        public const float HEALTH_HEALRATE_DAY = 0.15f;

        private float health = 1.0f;
        public float Health
        {
            get => health;
            set => health = Mathf.Clamp01(value);
        }

        public float HealingRatePerTick
        {
            get => ((int)parent.Faction.def.techLevel) / 4f * HEALTH_HEALRATE_DAY / 60000f;
        }

        public float ArmorDamageMultiplier
        {
            get =>  4f / Mathf.Max((int)parent.Faction.def.techLevel, 1f);
        }

        public WorldObjectCompProperties_Health Props
        {
            get => props as WorldObjectCompProperties_Health;
        }

        public HealthComp()
        {
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref health, "health", 1.0f);
        }

        public void HealthUpdate(int deltaTicks)
        {
            health += HealingRatePerTick * deltaTicks;
        }

        public void ApplyDamage(float amount)
        {
            if (Props.destoyedInstantly)
            {
                parent.Destroy();
                return;
            }            
            Health -= ArmorDamageMultiplier * amount;
            Notify_DamageTaken();
        }

        public void Notify_DamageTaken()
        {
            if(health <= 1e-4)
            {
                Destroy();
                return;
            }
        }

        public void Destroy()
        {
            parent.Destroy();
        }
    }

    //public class HealthComp : WorldObjectComp
    //{
    //    private const int RECONSTRUCTION_DAYS = 11;
    //    private const int RECONSTRUCTION_LIMIT = 3;
    //    private const int RECORD_EXIPRY = 120000;

    //    private const float RECONSTRUCTION_STARTING_HEALTH = 0.5f;
    //    private const float HEALTH_GAIN_TICK = 0.1f / 60000f;
    //    private const float HEALTH_GAIN_TICKRARE = 240 * HEALTH_GAIN_TICK;

    //    public struct DamageRecord : IExposable
    //    {
    //        public float tileAmount;
    //        public float mapAmout;
    //        public int createdAt;

    //        public int Age => createdAt != 0 ? GenTicks.TicksGame - createdAt : 0;

    //        public void ExposeData()
    //        {
    //            Scribe_Values.Look(ref tileAmount, "tile");
    //            Scribe_Values.Look(ref mapAmout, "mapAmout");
    //            Scribe_Values.Look(ref createdAt, "createdAt");
    //        }
    //    }

    //    private float _health = 1.0f;

    //    private int lastDamagedTick = -1;
    //    private int stunTicksLeft = 0;
    //    private int reconstructionCounter = 0;
    //    private int reconstructionTicksLeft = 0;

    //    private List<DamageRecord> records = new List<DamageRecord>();

    //    public float Health
    //    {
    //        get => Mathf.Clamp01(_health);
    //        private set => _health = Mathf.Clamp01(value);
    //    }

    //    public bool FullHeath
    //    {
    //        get => Health == 1.0f;
    //    }

    //    public bool Damaged
    //    {
    //        get => !FullHeath;
    //    }

    //    public bool Leveled
    //    {
    //        get => reconstructionTicksLeft > 0;
    //    }

    //    public float MapDamage
    //    {
    //        get => records.Sum(m => m.mapAmout);
    //    }

    //    public HealthComp()
    //    {
    //    }

    //    public override void CompTick()
    //    {
    //        base.CompTick();
    //        //if (Leveled)
    //        //{
    //        //    reconstructionTicksLeft--;
    //        //    if(reconstructionTicksLeft <= 0)
    //        //    {
    //        //        Health = RECONSTRUCTION_STARTING_HEALTH;
    //        //        reconstructionCounter--;
    //        //        records.Clear();
    //        //    }
    //        //    return;
    //        //}
    //        //if (GenTicks.TicksGame % 240 == 0 && Damaged)
    //        //{
    //        //    Health += HEALTH_GAIN_TICKRARE;
    //        //}
    //        //if (GenTicks.TicksGame % 30000 == 0 && records.Count > 0)
    //        //{
    //        //    records.RemoveAll((r) =>
    //        //    {
    //        //        if(r.Age > RECORD_EXIPRY)
    //        //        {
    //        //            Health += r.tileAmount * 0.5f;
    //        //            return true;
    //        //        }
    //        //        return false;
    //        //    });                
    //        //}            
    //    }

    //    public IEnumerable<DamageRecord> GetHealthRecords() => records;

    //    public override void PostExposeData()
    //    {
    //        base.PostExposeData();
    //        //Scribe_Values.Look(ref this._health, "health", 1.0f);
    //        //Scribe_Values.Look(ref this.stunTicksLeft, "health");
    //        //Scribe_Values.Look(ref this.lastDamagedTick, "health");
    //        //Scribe_Values.Look(ref this.reconstructionTicksLeft, "reconstructionTicksLeft", 0);
    //        //Scribe_Values.Look(ref this.reconstructionCounter, "reconstructionCounter", 0);
    //        //Scribe_Collections.Look(ref this.records, "records", LookMode.Deep);
    //        //this.records ??= new List<DamageRecord>();
    //    }

    //    public override string GetDescriptionPart()
    //    {
    //        return base.GetDescriptionPart() + $"\n\tHealth: {Health * 100}%";
    //    }

    //    public void ApplyDamage(float tileAmount, float mapAmount, bool canStun = false)
    //    {
    //        //if(parent is DestroyedSettlement || parent.Faction.IsPlayerSafe())
    //        //{
    //        //    return; 
    //        //}
    //        //lastDamagedTick = GenTicks.TicksGame;
    //        //DamageRecord record = new DamageRecord();
    //        //record.createdAt = GenTicks.TicksGame;
    //        //record.mapAmout = mapAmount;            
    //        //if (Leveled)
    //        //{                                
    //        //    record.tileAmount = tileAmount * 0.5f;                
    //        //    records.Add(record);
    //        //    reconstructionTicksLeft += (int)(10000 * tileAmount);
    //        //}
    //        //else
    //        //{
    //        //    Health -= tileAmount;                
    //        //    record.tileAmount = tileAmount;                
    //        //    records.Add(record);
    //        //    if (Health <= 0.0f)
    //        //    {
    //        //        reconstructionTicksLeft = RECONSTRUCTION_DAYS * 60000;
    //        //        Level();
    //        //    }
    //        //}            
    //    }

    //    public void Level()
    //    {
    //        //if(parent is Settlement settlement)
    //        //{
    //        //    this.ReplaceParent(WorldObjectDefOf.DestroyedSettlement);

    //        //    foreach (Faction allFaction in Find.FactionManager.AllFactions)
    //        //    {
    //        //        if (!allFaction.Hidden && !allFaction.IsPlayer && allFaction != parent.Faction && allFaction.HostileTo(parent.Faction))
    //        //        {
    //        //            FactionRelationKind playerRelationKind = allFaction.PlayerRelationKind;
    //        //            Faction.OfPlayer.TryAffectGoodwillWith(allFaction, 20, canSendMessage: false, canSendHostilityLetter: false, HistoryEventDefOf.DestroyedEnemyBase);
    //        //        }
    //        //    }
    //        //}
    //        //else if(parent is Site site)
    //        //{

    //        //}
    //    }

    //    public void Restore()
    //    {
    //        if (parent is DestroyedSettlement destroyedSettlement)
    //        {
    //            this.ReplaceParent(WorldObjectDefOf.Settlement);
    //        }
    //    }

    //    private void ReplaceParent(WorldObjectDef worldObjectDef)
    //    {
    //        WorldObject worldObject = (WorldObject)WorldObjectMaker.MakeWorldObject(worldObjectDef);
    //        worldObject.Tile = parent.Tile;
    //        worldObject.SetFaction(parent.Faction);
    //        var other = worldObject.GetComponent<HealthComp>();

    //        Find.WorldObjects.Add(worldObject);
    //        other.records = records;
    //        other.reconstructionCounter = reconstructionCounter;
    //        other.reconstructionTicksLeft = reconstructionTicksLeft;
    //        other.stunTicksLeft = stunTicksLeft;
    //        other.lastDamagedTick = lastDamagedTick;
    //        other.Health = this.Health;

    //        TimedDetectionRaids component = parent.GetComponent<TimedDetectionRaids>();
    //        if (component != null)
    //        {
    //            component.CopyFrom(parent.GetComponent<TimedDetectionRaids>());
    //            component.SetNotifiedSilently();
    //        }

    //        parent.Destroy();
    //        if (parent is MapParent mapParent && mapParent.HasMap)
    //        {
    //            mapParent.Map.info.parent = worldObject as MapParent;
    //        }
    //    }
    //}
}

