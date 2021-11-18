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
        private const float SHELLING_FACTOR = 1f;

        public const int SHELLER_EXPIRYTICKS = 15000;
        public const int SHELLER_MINCOOLDOWNTICKS = 7500;        
        public const int SHELLER_MIN_TICKSBETWEENSHOTS = 30;
        public const int SHELLER_MAX_TICKSBETWEENSHOTS = 80;
        public const int SHELLER_MIN_PROJECTILEPOINTS = 100;

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
            get => totalShots > 0 && totalShots > shotsFired && budget > 0 && GenTicks.TicksGame - startedAt < SHELLER_EXPIRYTICKS;
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

        public void ThrottledTick()
        {
            if(cooldownTicks > 0)
            {
                cooldownTicks -= WorldObjectTrackerCE.THROTTLED_TICK_INTERVAL;
                return;
            }
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
            if (cooldownTicks > 0)
            {
                cooldownTicks = (int) (cooldownTicks *  Mathf.Min(5000 / points, 0.5f)); // so if you keep attacking them they finish the cooldown faster
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
            totalShots = GetPointsTotalShots(Mathf.CeilToInt(points));
            startedAt = GenTicks.TicksGame;
            shotsFired = 0;                                   
            cooldownTicks = -1;
            Log.Message($"budget: {budget}, totalshots: {totalShots}");
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
            float shotsRemainingMinPoints = (totalShots - shotsFired) * SHELLER_MIN_PROJECTILEPOINTS;
            float distance = Find.WorldGrid.TraversalDistanceBetween(target.Tile, comp.parent.Tile, true);
            ShellingResponseDef.ShellingResponsePart_Projectile responseProjectile = AvailableProjectiles
                .Where(p => (budget - p.points) > shotsRemainingMinPoints && p.projectile.projectile is ProjectilePropertiesCE propEC && propEC.shellingProps.range >= distance * 0.5f)
                .RandomElementByWeightWithFallback(p => p.weight, null);

            shotsFired++;
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
}

