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
        private const float SHELLING_FACTOR = 4f;

        public const int SHELLER_EXPIRYTICKS = 15000;            
        public const int SHELLER_MIN_TICKSBETWEENSHOTS = 30;
        public const int SHELLER_MAX_TICKSBETWEENSHOTS = 80;
        public const int SHELLER_MIN_PROJECTILEPOINTS = 100;
        
        private int startedAt;        
        private int ticksToNextShot = 0;        
        private int shotsFired = 0;
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
            get => budget > 0 && GenTicks.TicksGame - startedAt < SHELLER_EXPIRYTICKS;
        }        

        public void ExposeData()
        {            
            Scribe_Values.Look(ref budget, "budget", 0);
            Scribe_Values.Look(ref startedAt, "startedAt", -1);
            Scribe_Values.Look(ref shotsFired, "shotsFired", 0);            
            Scribe_Values.Look(ref ticksToNextShot, "ticksToNextShot", 0);
            // TODO fix cooldown
            // Scribe_Values.Look(ref cooldownTicks, "cooldownTicks", 0);
            Scribe_TargetInfo.Look(ref target, "target");
        }

        public void ThrottledTick()
        {            
            if(!Shooting)
            {
                return;
            }
            if (ticksToNextShot > 0)
            {
                ticksToNextShot -= WorldObjectTrackerCE.THROTTLED_TICK_INTERVAL;
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
            shooter = comp.parent.Faction.GetRandomWorldPawn();            
            target = targetInfo;
            ticksToNextShot = GetTicksToShot();
            budget = (int)(Mathf.CeilToInt(points) * SHELLING_FACTOR);
            FactionStrengthTracker tracker = comp.parent.Faction.GetStrengthTracker();
            if (tracker != null)
            {
                budget = (int)(budget * Mathf.Max(tracker.StrengthPointsMultiplier, 0.5f));
            }            
            startedAt = GenTicks.TicksGame;
            shotsFired = 0;
            // TODO fix cooldown
            // cooldownTicks = -1;            
            return true;
        }

        public void Stop()
        {
            shotsFired = 0;            
            ticksToNextShot = 0;
            startedAt = -1;
            budget = -1;
            target = GlobalTargetInfo.Invalid;
            shooter = null;
        }        

        private void CastShot()
        {
            float distance = Find.WorldGrid.TraversalDistanceBetween(target.Tile, comp.parent.Tile, true);
            ShellingResponseDef.ShellingResponsePart_Projectile responseProjectile = AvailableProjectiles
                .Where(p => (budget - p.points) > 0 && p.projectile.projectile is ProjectilePropertiesCE propEC && propEC.shellingProps.range >= distance * 0.5f)
                .RandomElementByWeightWithFallback(p => p.weight, null);

            shotsFired++;
            if (responseProjectile == null)
            {
                Stop();
                // TODO fix cooldown
                //cooldownTicks = GetTicksToCooldown();
                return;
            }
            budget -= (int)responseProjectile.points;

            LaunchProjectile(responseProjectile.projectile);
            if (budget <= AvailableProjectiles.Min(p => p.points))
            {
                Stop();
                // TODO fix cooldown
                //cooldownTicks = GetTicksToCooldown();
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
            float minProjectiles = points / AvailableProjectiles.Min(p => p.points);
            float avgProjectiles = AvailableProjectiles.Sum(p => points / p.points) / AvailableProjectiles.Count;
            if(avgProjectiles - minProjectiles + SHELLER_MIN_PROJECTILEPOINTS < 0)
            {
                avgProjectiles = minProjectiles;
                minProjectiles = minProjectiles / 2 + 1;
            }
            return Rand.Range((int)minProjectiles, (int)avgProjectiles);
        }

        // TODO fix cooldown
        // private int GetTicksToCooldown() => Rand.Range(SHELLER_MINCOOLDOWNTICKS, Mathf.Max(7 - (int)comp.parent.Faction.def.techLevel, 4) * SHELLER_MINCOOLDOWNTICKS);

        private int GetTicksToShot() => Rand.Range(SHELLER_MIN_TICKSBETWEENSHOTS, SHELLER_MAX_TICKSBETWEENSHOTS);
    }
}

