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
        private const float SHELLING_FACTOR = 3.5f;

        public const int SHELLER_MINCOOLDOWNTICKS = 300;
        public const int SHELLER_MAXCOOLDOWNTICKS = 1200;
        public const int SHELLER_MAXCOOLDOWNTICKS_TECHMULMAX = 3;
        public const int SHELLER_MESSAGE_TICKSCOOLDOWN = 5000;
        public const int SHELLER_EXPIRYTICKS = 15000;
        public const int SHELLER_MIN_TICKSBETWEENSHOTS = 30;
        public const int SHELLER_MAX_TICKSBETWEENSHOTS = 240;
        public const int SHELLER_MIN_PROJECTILEPOINTS = 100;

        private int lastMessageSentAt = -1;
        private int startedAt;
        private int ticksToNextShot = 0;
        private int shotsFired = 0;
        private int budget = 0;
        private Pawn shooter;
        private Faction targetFaction;
        private GlobalTargetInfo target;

        // TODO fix cooldown
        private int cooldownTicks = -1;

        public HostilityComp comp;
        public virtual bool AbleToShellResponse
        {
            get
            {
                if (comp.AvailableProjectiles.NullOrEmpty())
                {
                    return false;
                }
                bool? res = null;
                if (comp.parent is Site site)
                {
                    foreach (var sitePart in site.parts)
                    {
                        res = sitePart.def.GetModExtension<WorldObjectHostilityExtension>()?.AbleToShellingResponse;
                        if (res.HasValue)
                        {
                            return res.Value;
                        }
                    }
                }
                res = comp.parent.Faction?.def.GetModExtension<WorldObjectHostilityExtension>()?.AbleToShellingResponse;
                if (res.HasValue)
                {
                    return res.Value;
                }
                res = (comp.props as WorldObjectCompProperties_Hostility).AbleToShellingResponse;
                if (res.HasValue)
                {
                    return res.Value;
                }
                return true;
            }
        }

        public virtual List<ShellingResponseDef.ShellingResponsePart_Projectile> AvailableProjectiles
        {
            get => comp.AvailableProjectiles;
        }
        public virtual bool Shooting
        {
            get => budget > 0 && GenTicks.TicksGame - startedAt < SHELLER_EXPIRYTICKS;
        }

        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref budget, "budget", 0);
            Scribe_Values.Look(ref startedAt, "startedAt", -1);
            Scribe_Values.Look(ref shotsFired, "shotsFired", 0);
            Scribe_Values.Look(ref ticksToNextShot, "ticksToNextShot", 0);
            Scribe_References.Look(ref targetFaction, "targetFaction");
            //TODO fix cooldown
            Scribe_Values.Look(ref cooldownTicks, "cooldownTicks", 0);
            Scribe_TargetInfo.Look(ref target, "target");
        }

        public virtual void ThrottledTick()
        {
            if (cooldownTicks > 0)
            {
                cooldownTicks -= WorldObjectTrackerCE.THROTTLED_TICK_INTERVAL;
                return;
            }
            if (!Shooting)
            {
                return;
            }
            if (ticksToNextShot > 0)
            {
                ticksToNextShot -= WorldObjectTrackerCE.THROTTLED_TICK_INTERVAL;
                return;
            }
            if (comp.parent is MapParent mapParent && mapParent.HasMap)
            {
                return;
            }
            CastShot();
        }

        public bool TryStartShelling(GlobalTargetInfo targetInfo, float points, Faction targetFaction = null)
        {
            if (Shooting || targetInfo.Tile < 0 || points <= 0 || !AbleToShellResponse)
            {
                return false;
            }
            budget = (int)(Mathf.CeilToInt(points) * SHELLING_FACTOR);
            FactionStrengthTracker tracker = comp.parent.Faction.GetStrengthTracker();
            if (tracker != null)
            {
                budget = (int)(budget * Mathf.Max(tracker.StrengthPointsMultiplier, 0.5f));
            }
            if (RandomAvailableShell(targetInfo) == null)
            {
                Stop();
                return false;
            }
            if (targetFaction != null && targetFaction.IsPlayer)
            {
                TrySendWarning();
            }
            this.targetFaction = targetFaction;
            if (comp.parent.Faction.def.humanlikeFaction)
            {
                shooter = comp.parent.Faction.GetRandomWorldPawn();
                if (shooter == null)
                {
                    Log.Error($"CE: shooter for HostilitySheller from {comp.parent} is null");
                }
            }
            target = targetInfo;
            ticksToNextShot = GetTicksToCooldown();
            startedAt = GenTicks.TicksGame;
            shotsFired = 0;
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

        protected virtual void CastShot()
        {
            ShellingResponseDef.ShellingResponsePart_Projectile responseProjectile = RandomAvailableShell(target);

            shotsFired++;
            if (responseProjectile == null)
            {
                Stop();
                cooldownTicks = GetTicksToCooldown();
                return;
            }
            budget -= (int)responseProjectile.points;

            LaunchProjectile(responseProjectile.projectile);
            if (budget <= AvailableProjectiles.Min(p => p.points))
            {
                Stop();
                cooldownTicks = GetTicksToCooldown();
                return;
            }
            ticksToNextShot = GetTicksToShot();
            if (targetFaction != null && targetFaction.IsPlayer)
            {
                TrySendWarning();
            }
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
            if (shooter == null && comp.parent.Faction.def.humanlikeFaction)
            {
                shooter = comp.parent.Faction.GetRandomWorldPawn();
            }
            shell.launcher = shooter;
            shell.equipmentDef = null;
            shell.globalSource = new GlobalTargetInfo(comp.parent);
            shell.globalSource.worldObjectInt = comp.parent;
            shell.shellDef = projectileDef;
            shell.globalTarget = target;
            if (shell.launcher == null)
            {
                Log.Warning($"CE: Launcher of shell {projectileDef} is null, this may cause targeting issues");
            }
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
            if (avgProjectiles - minProjectiles + SHELLER_MIN_PROJECTILEPOINTS < 0)
            {
                avgProjectiles = minProjectiles;
                minProjectiles = minProjectiles / 2 + 1;
            }
            return Rand.Range((int)minProjectiles, (int)avgProjectiles);
        }


        protected virtual void TrySendWarning()
        {
            if (GenTicks.TicksGame - lastMessageSentAt > SHELLER_MESSAGE_TICKSCOOLDOWN || lastMessageSentAt == -1)
            {
                lastMessageSentAt = GenTicks.TicksGame;
                var letter = LetterMaker.MakeLetter("CE_CounterShellingLabel".Translate(), "CE_Message_CounterShelling".Translate(comp.parent.Label, comp.parent.Faction.Name), CE_LetterDefOf.CE_ThreatBig, comp.parent, comp.parent.Faction);
                Find.LetterStack.ReceiveLetter(letter);
            }
        }
        private ShellingResponseDef.ShellingResponsePart_Projectile RandomAvailableShell(GlobalTargetInfo target) =>
            AvailableProjectiles
                .Where(p => (budget - p.points) > 0 && p.projectile.projectile is ProjectilePropertiesCE propEC && propEC.shellingProps.range >= Find.WorldGrid.TraversalDistanceBetween(target.Tile, comp.parent.Tile, true) * 0.5f)
                .RandomElementByWeightWithFallback(p => p.weight, null);

        private int GetTicksToCooldown() => Rand.Range(SHELLER_MINCOOLDOWNTICKS,
            Mathf.Clamp(7 - (int)comp.parent.Faction.def.techLevel, 1, SHELLER_MAXCOOLDOWNTICKS_TECHMULMAX) *
            SHELLER_MAXCOOLDOWNTICKS) * HealthMultiplier();

        private int GetTicksToShot() => Rand.Range(SHELLER_MIN_TICKSBETWEENSHOTS, SHELLER_MAX_TICKSBETWEENSHOTS) * HealthMultiplier();

        /// <summary>
        /// Compute the multiplier to be applied to retaliation fire rate based on the current health of this world object.
        /// </summary>
        /// <returns>The computed multiplier.</returns>
        private int HealthMultiplier()
        {
            var retaliationShellingCooldownMultiplier =
                comp.parent.Faction.GetShellingResponseDef().retaliationShellingCooldownImpact;

            var curHealth = comp.parent.GetComponent<HealthComp>()?.Health ?? 1f;

            return Mathf.FloorToInt(retaliationShellingCooldownMultiplier.LerpThroughRange(1f - curHealth));
        }
    }
}

