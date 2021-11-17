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
    public class HostilitySheller : IExposable
    {
        public const int SHELLER_EXPIRYTICKS = 7200;
        public const int SHELLER_MINCOOLDOWNTICKS = 15000;        
        public const int SHELLER_MIN_TICKSBETWEENSHOTS = 50;
        public const int SHELLER_MAX_TICKSBETWEENSHOTS = 240;
        public const int SHELLER_MIN_PROJECTILEPOINTS = 250;

        private int cooldownTicks = 0;
        private int startedAt;
        private int ticksToNextShot = 0;
        private int shotsFired = 0;
        private int totalShots = 0;
        private int budget = 0;
        private Pawn shooter;
        private GlobalTargetInfo target;       

        public HostilityComp comp;
       
        public List<ShellingResponseDef.ShellingResponsePart_Projectile> AvailableProjectiles
        {
            get => comp.AvailableProjectiles;
        }
        public bool Shooting
        {
            get => totalShots > 0 && totalShots > shotsFired && budget > 0 && GenTicks.TicksGame - startedAt < SHELLER_MINCOOLDOWNTICKS;
        }        

        public void ExposeData()
        {
            Scribe_Values.Look(ref budget, "budget", 0);
            Scribe_Values.Look(ref startedAt, "startedAt", -1);
            Scribe_Values.Look(ref shotsFired, "shotsFired", 0);
            Scribe_Values.Look(ref totalShots, "totalShots", 0);
            Scribe_Values.Look(ref ticksToNextShot, "ticksToNextShot", 0);
            Scribe_Values.Look(ref cooldownTicks, "cooldownTicks", 0);
            Scribe_TargetInfo.Look(ref target, "target");
        }

        public void Tick()
        {
            if(cooldownTicks > 0)
            {
                cooldownTicks--;
                return;
            }
            if(!Shooting)
            {
                return;
            }
            if (ticksToNextShot > 0)
            {
                ticksToNextShot--;
                return;
            }
            CastShot();
        }

        public bool TryStartShelling(GlobalTargetInfo targetInfo, float points)
        {
            if (Shooting || targetInfo.Tile < 0 || points <= 0 || comp.AvailableProjectiles.NullOrEmpty())
            {
                return false;
            }
            if (cooldownTicks > 0)
            {
                cooldownTicks -= GetTicksToShot() * 4; // so if you keep attacking them they finish the cooldown faster
                return false;
            }
            shooter = comp.parent.Faction.GetRandomWorldPawn();            
            target = targetInfo;
            ticksToNextShot = GetTicksToShot();
            totalShots = GetPointsTotalShots(Mathf.CeilToInt(points));
            startedAt = GenTicks.TicksGame;
            shotsFired = 0;
            budget = Mathf.CeilToInt(points);
            cooldownTicks = -1;            
            return true;
        }

        public void Stop()
        {
            shotsFired = 0;
            totalShots = 0;
            ticksToNextShot = 0;
            startedAt = -1;
            budget = 0;
            target = GlobalTargetInfo.Invalid;
            shooter = null;
        }        

        private void CastShot()
        {            
            shotsFired++;
            float shotsRemainingMinPoints = (totalShots - shotsFired) * SHELLER_MIN_PROJECTILEPOINTS;            
            ShellingResponseDef.ShellingResponsePart_Projectile responseProjectile = AvailableProjectiles
                .Where(p => (budget - p.points) > shotsRemainingMinPoints)
                .RandomElementByWeightWithFallback(p => p.weight, null);

            if (responseProjectile == null)
            {
                Stop();                
                cooldownTicks = GetTicksToCooldown();
                return;
            }
            budget -= (int)responseProjectile.points;

            LaunchProjectile(responseProjectile.projectile);
            if (shotsFired >= totalShots && budget <= SHELLER_MIN_PROJECTILEPOINTS / 2)
            {
                Stop();
                cooldownTicks = GetTicksToCooldown();
                return;
            }
            ticksToNextShot = GetTicksToShot();
        }

        private void LaunchProjectile(ThingDef projectileDef)
        {
            TravelingShell shell = (TravelingShell)WorldObjectMaker.MakeWorldObject(CE_WorldObjectDefOf.TravelingShell);
            if (comp.parent.Faction != null)
            {
                shell.SetFaction(comp.parent.Faction);
            }
            shell.tileInt = comp.parent.Tile;
            shell.SpawnSetup();
            Find.World.worldObjects.Add(shell);
            shell.launcher = shooter;
            shell.equipmentDef = null;
            shell.globalSource = new GlobalTargetInfo(comp.parent);
            shell.globalSource.worldObjectInt = comp.parent;            
            shell.shellDef = projectileDef;            
            shell.globalTarget = target;
            if (!shell.TryTravel(comp.parent.Tile, target.Tile))
            {
                Stop();
                Log.Error($"CE: Travling shell {projectileDef} failed to launch!");
                shell.Destroy();
            }
        }        

        private int GetPointsTotalShots(int points)
        {
            float weightSum = AvailableProjectiles.Sum(p => p.weight);
            return (int)(points / Rand.Range(SHELLER_MIN_PROJECTILEPOINTS, Mathf.Max(AvailableProjectiles.Sum(p => p.points * p.weight / weightSum), SHELLER_MIN_PROJECTILEPOINTS * 2f)));
        }

        private int GetTicksToCooldown() => Rand.Range(SHELLER_MINCOOLDOWNTICKS, Mathf.Max(7 - (int)comp.parent.Faction.def.techLevel, 4) * SHELLER_MINCOOLDOWNTICKS);

        private int GetTicksToShot() => Rand.Range(SHELLER_MIN_TICKSBETWEENSHOTS, SHELLER_MAX_TICKSBETWEENSHOTS);
    }

    //public class ShellingComp : WorldObjectComp
    //{
    //    private int _tickerId = Math.Abs((_tickerIdCounter += Rand.Int % UInt16.MaxValue + 1) & Rand.Int);
    //    private static int _tickerIdCounter = 0;        
    //
    //    public struct ShellingRecord : IExposable
    //    {
    //        public float dmgMap;
    //        public float dmgTile;
    //        public float dmgMultipler;       
    //
    //        public float DamageToTile => dmgTile * dmgMultipler;
    //        public float DamageToMap => dmgMap * dmgMultipler;
    //
    //        public void ExposeData()
    //        {
    //            Scribe_Values.Look(ref dmgMap, "dmgMap");
    //            Scribe_Values.Look(ref dmgTile, "dmgTile");
    //            Scribe_Values.Look(ref dmgMultipler, "dmgMultipler");
    //        }
    //    }
    //
    //    private int reconstructionCounter = 0; 
    //    private int reconstructionTicks = 0;
    //    private float chock = 0f;
    //    private float _health = 1.0f;
    //    private List<ShellingRecord> records = new List<ShellingRecord>();
    //
    //    public float Health
    //    {
    //        get
    //        {
    //            return _health;
    //        }
    //        private set
    //        {
    //            _health = Mathf.Clamp01(value);
    //        }
    //    }
    //
    //    public float TechLevelDamageMultiplier
    //    {
    //        get
    //        {
    //            int techLevel = FactionTechLevel;            
    //            if(techLevel <= 3)
    //            {
    //                return 1.0f;
    //            }
    //            return 3f / techLevel;
    //        }
    //    }
    //    
    //    public int FactionTechLevel => (int) (parent.Faction?.def.techLevel ?? 0);
    //    public bool OutOfService => reconstructionTicks > 0;        
    //    public bool Damaged => Health != 1.0f;
    //    public bool FullHealth => Health == 1.0f;        
    //
    //    public ShellingComp()
    //    {
    //        _tickerId = _tickerIdCounter;
    //        _tickerIdCounter += Rand.Int % UInt16.MaxValue + 1;
    //    }

    //    public override void PostExposeData()
    //    {
    //        base.PostExposeData();
    //        Scribe_Values.Look(ref _health, "health", 1.0f);
    //        Scribe_Values.Look(ref reconstructionTicks, "reconstructionTicks");
    //        Scribe_Values.Look(ref reconstructionCounter, "reconstructionCounter");
    //        Scribe_Collections.Look(ref records, "records", LookMode.Deep);
    //        records ??= new List<ShellingRecord>();            
    //    }

    //    public override void CompTick()
    //    {
    //        base.CompTick();
    //        if((GenTicks.TicksGame + _tickerId) % GenTicks.TickRareInterval == 0)
    //        {
    //            TickRare();
    //        }
    //        if(reconstructionTicks > 0)
    //        {
    //            reconstructionTicks--;                
    //        }
    //        if (FactionTechLevel <= 4 || !OutOfService)
    //        {
    //            return;
    //        }
    //        if ((GenTicks.TicksGame + _tickerId) % GenTicks.TickLongInterval == 0)
    //        {
    //            TickLong();
    //        }
    //    }

    //    public void ApplyDamage(TravelingShellProperties shellProperties, Faction enemyFaction, int sourceTile = -1)
    //    {            
    //        // we use this to create a chock to extreem shelling
    //        chock += 1 + shellProperties.damage_Tile * 100 * TechLevelDamageMultiplier;

    //        ShellingRecord record = new ShellingRecord();            
    //        record.dmgMultipler = TechLevelDamageMultiplier;
    //        record.dmgMap = shellProperties.damage_Map;
    //        record.dmgTile = shellProperties.damage_Tile;            
    //        this.records.Add(record);            
    //        this.Health = Health - record.DamageToTile;            
    //        // try to perform a repsonse
    //        this.Notify_ShelledBy(enemyFaction, sourceTile);

    //        // check if the health is too low then put the object out of service
    //        if (this.Health == 0)
    //        {
    //            if (parent.questTags.NullOrEmpty())
    //            {
    //                if (parent is Site site)
    //                {
    //                    // TODO
    //                    // figure out what to do with this
    //                }
    //                else if (FactionTechLevel > 6 || Rand.Chance(1 / Mathf.Clamp(FactionTechLevel - reconstructionCounter, 1, 8))) // a high tech faction can sort repair multiple times.
    //                {
    //                    reconstructionTicks = 60000 * Rand.Range(4, 24);
    //                    reconstructionCounter++;
    //                }else
    //                {
    //                    parent.Destroy();
    //                    Notify_DestoyedBy(enemyFaction, sourceTile);
    //                }
    //            }
    //            // now go through all quests and see if we finished something
    //            else if (!Find.QuestManager.quests.Where(q => q.parts.Any(p => p is QuestPart_DestroyWorldObject part && part.worldObject == parent)).EnumerableNullOrEmpty())
    //            {
    //                parent.Destroy();
    //            }                               
    //        }
    //    }

    //    public void Notify_DestoyedBy(Faction enemyFaction, int sourceTile = -1)
    //    {
    //        // TODO
    //        // send a letter
    //    }

    //    public void Notify_ShelledBy(Faction enemyFaction, int sourceTile = -1)
    //    {
    //        Faction faction = parent.Faction;
    //        if (faction != null && enemyFaction != faction && !faction.IsPlayerSafe())
    //        {
    //            FactionRelation relation = enemyFaction.RelationWith(faction, true);
    //            if (relation == null)
    //            {
    //                faction.TryMakeInitialRelationsWith(enemyFaction);
    //                relation = faction.RelationWith(enemyFaction, false);
    //            }
    //            faction.TryAffectGoodwillWith(enemyFaction, -70, canSendMessage: true, canSendHostilityLetter: true, HistoryEventDefOf.AttackedSettlement);
    //        }
    //        if (ParentHasMap) // if the tile map is active, don't try any funny stuff
    //        {
    //            return;
    //        }
    //        // here we can responed to the attack
    //        float techMultipler = (FactionTechLevel - 4f) / 4f;

    //    }

    //    private void TickRare()
    //    {
    //        // reduce the chock value
    //        chock = Mathf.Max(chock - 50f / 60000f * GenTicks.TickRareInterval, 0f);
    //        // heal the current object
    //        if (Damaged) {
    //            // high tech factions should repair at a very high rate
    //            // for industrial and lower the value is 1x
    //            // the equation is (0.5 * FactionTechLevel - 1)^2 / 1f                    
    //            float techMultipler = FactionTechLevel > 4 ? Mathf.Max(Mathf.Pow(FactionTechLevel / 2f - 1, 2), 1.0f) / 2f : 1.0f;                
    //            if (!records.NullOrEmpty())
    //            {                    
    //                float dH = GenTicks.TickRareInterval * 0.1f / 60000f * records.Count * techMultipler;
    //                for (int i = 0; i < records.Count; i++)
    //                {
    //                    ShellingRecord record = records[i];
    //                    record.dmgTile -= GenTicks.TickRareInterval * 0.05f / 60000f * techMultipler;
    //                    record.dmgMap -= GenTicks.TickRareInterval * 1500 / 60000f; // for beauty reasons we don't heal this as much
    //                    records[i] = record;
    //                }
    //                records.RemoveAll(r => r.DamageToTile <= 0);
    //                // now limit how fast repairs can happen
    //                Health += Mathf.Min(dH, GenTicks.TickRareInterval * 0.05f / 60000f * techMultipler);
    //            }
    //            else
    //            {
    //                // if all records are already removed speed up the recovery
    //                Health += GenTicks.TickRareInterval * 0.1f / 60000f * techMultipler;
    //            }                                              
    //        }
    //    }

    //    private void TickLong()
    //    {
    //        // TODO add MTB for random attacks in case of a recent attack
    //    }
    //}
}

