using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.Sound;
using System.Reflection.Emit;
using System;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Rimatomics;
using CombatExtended.HarmonyCE;
using CombatExtended.WorldObjects;
using System.Runtime.CompilerServices;


namespace CombatExtended.Compatibility;
class Rimatomics : IPatch
{
    public static SoundDef HitSoundDef = null;


    public static List<ThingComp> shields;
    public static bool found = false;

    public static int lastCacheTick = 0;
    public static Map lastCacheMap = null;

    public bool CanInstall()
    {
        return ModLister.HasActiveModWithName("Dubs Rimatomics");
    }
    public void Install()
    {
        BlockerRegistry.RegisterCheckForCollisionBetweenCallback(Rimatomics.CheckForCollisionBetweenCallback);
        BlockerRegistry.RegisterImpactSomethingCallback(Rimatomics.ImpactSomethingCallback);
        BlockerRegistry.RegisterShieldZonesCallback(Rimatomics.ShieldZonesCallback);
        PatchRailgunWorldStrike();
    }

    public static bool CheckForCollisionBetweenCallback(ProjectileCE projectile, Vector3 from, Vector3 to)
    {
        Map map = projectile.Map;
        getShields(map);
        if (!found)
        {
            return false;
        }
        Vector3 exactPosition = projectile.ExactPosition;
        IntVec3 origin = projectile.OriginIV3;
        Quaternion targetAngle = projectile.ExactRotation;

        if (projectile.launcher == null)
        {
            return false;
        }

        foreach (ThingComp thingComp in shields)
        {
            var shield = thingComp as CompRimatomicsShield;
            if (!shield.Active || shield.ShieldState != ShieldState.Active)
            {
                continue;
            }
            if (!GenHostility.HostileTo(projectile.launcher, shield.parent) && !shield.debugInterceptNonHostileProjectiles && !shield.Props.interceptNonHostileProjectiles)
            {
                continue;
            }
            bool interceptOutgoing = shield.Props.interceptOutgoingProjectiles;

            int fieldRadius = (int)shield.Radius;
            Vector3 shieldPosition2d = new Vector3(shield.parent.Position.x, 0, shield.parent.Position.z);

            if (!CE_Utility.IntersectionPoint(from, to, shieldPosition2d, fieldRadius, out Vector3[] sect, map: map, spherical: false, catchOutbound: interceptOutgoing))
            {
                continue;
            }
            Vector3 nep = sect.OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
            Quaternion shieldProjAng = Quaternion.LookRotation(new Vector3(from.x - shieldPosition2d.x, 0, from.z - shieldPosition2d.z));
            var angle = Quaternion.Angle(targetAngle, shieldProjAng);
            if (angle > 90 || interceptOutgoing)
            {
                int damage = (projectile.def.projectile.GetDamageAmount(projectile.launcher));

                exactPosition = BlockerRegistry.GetExactPosition(origin.ToVector3(), projectile.ExactPosition, shield.parent.Position.ToVector3(), (fieldRadius - 1) * (fieldRadius - 1));
                FleckMakerCE.ThrowLightningGlow(exactPosition, map, 0.5f);
                projectile.ExactPosition = exactPosition;

                Effecter effecter = new Effecter(shield.Props.interceptEffect ?? EffecterDefOf.Interceptor_BlockedProjectile);
                effecter.Trigger(new TargetInfo(IntVec3Utility.ToIntVec3(exactPosition), shield.parent.Map, false), TargetInfo.Invalid);
                effecter.Cleanup();
                shield.energy -= damage * shield.EnergyLossPerDamage;
                if (shield.energy < 0f)
                {
                    DamageInfo dinfo = new DamageInfo(projectile.def.projectile.damageDef, (float)damage, 0f, -1f, null, null, null, 0, null, true, true);
                    shield.BreakShield(dinfo);
                }


                projectile.InterceptProjectile(shield, exactPosition, true);

                return true;
            }
        }
        return false;
    }
    public static bool ImpactSomethingCallback(ProjectileCE projectile, Thing launcher)
    {
        bool flyOverhead = projectile.def.projectile.flyOverhead;
        if (!flyOverhead)
        {
            return false;
        }
        Map map = projectile.Map;
        getShields(map);
        Vector3 destination = projectile.ExactPosition;
        foreach (CompRimatomicsShield shield in shields)
        {
            if (!shield.Active || shield.ShieldState != ShieldState.Active)
            {
                continue;
            }
            if (!GenHostility.HostileTo(projectile.launcher, shield.parent) && !shield.debugInterceptNonHostileProjectiles && !shield.Props.interceptNonHostileProjectiles)
            {
                continue;
            }
            int fieldRadius = (int)shield.Radius;
            int fieldRadiusSq = fieldRadius * fieldRadius;
            float DistanceSq = projectile.Position.DistanceToSquared(shield.parent.Position) - fieldRadiusSq;
            if (DistanceSq > 0)
            {
                continue;
            }

            int damage = (projectile.def.projectile.GetDamageAmount(launcher));
            Effecter effecter = new Effecter(shield.Props.interceptEffect ?? EffecterDefOf.Interceptor_BlockedProjectile);
            effecter.Trigger(new TargetInfo(projectile.Position, shield.parent.Map, false), TargetInfo.Invalid);
            effecter.Cleanup();
            shield.energy -= damage * shield.EnergyLossPerDamage;
            if (shield.energy < 0f)
            {
                DamageInfo dinfo = new DamageInfo(projectile.def.projectile.damageDef, (float)damage, 0f, -1f, null, null, null, 0, null, true, true);
                shield.BreakShield(dinfo);
            }
            projectile.InterceptProjectile(shield, projectile.ExactPosition, true);
            return true;
        }
        return false;
    }

    private static IEnumerable<IEnumerable<IntVec3>> ShieldZonesCallback(Thing pawnToSuppress)
    {
        Map map = pawnToSuppress.Map;
        getShields(map);
        List<IEnumerable<IntVec3>> result = new List<IEnumerable<IntVec3>>();
        foreach (CompRimatomicsShield shield in shields)
        {
            if (!shield.Active || shield.ShieldState != ShieldState.Active)
            {
                continue;
            }
            if (GenHostility.HostileTo(pawnToSuppress, shield.parent) && !shield.debugInterceptNonHostileProjectiles && !shield.Props.interceptNonHostileProjectiles)
            {
                // Avoid hostile shields because they aren't intercepting friendly projectiles
                continue;
            }

            int fieldRadius = (int)shield.Radius;
            result.Add(GenRadial.RadialCellsAround(shield.parent.Position, fieldRadius, true));
        }
        return result;
    }

    public static void getShields(Map map)
    {
        int thisTick = Find.TickManager.TicksAbs;
        if (lastCacheTick != thisTick || lastCacheMap != map)
        {
            found = false;
            IEnumerable<Building> buildings = map.listerBuildings.allBuildingsColonist.Where(b => b is Building_ShieldArray);
            shields = new List<ThingComp>();
            foreach (Building b in buildings)
            {
                found = true;
                shields.Add((b as Building_ShieldArray).CompShield);
            }

            lastCacheTick = thisTick;
            lastCacheMap = map;
        }
    }

    #region Railgun world strike
    // Lets Punisher Fire Mission bombard settlements and sites with no active map, like CE artillery's "Attack World Tile".
    // Rimatomics already handles projectile flight on the world map (WorldObject_Sabot), however Fire Mission only accepts
    // loaded tiles, and a sabot arriving at an unloaded tile is discarded.
    // One hit counts as one 155mm HE shell (shellingProps.damage 0.33).
    //
    // Member signatures avoid Rimatomics types, since CE reflects over its own types even when Rimatomics is absent.
    // Rimatomics private members are reached by name and through ___field injection.

    private static ThingDef worldStrikeShellDef;

    private static void PatchRailgunWorldStrike()
    {
        MethodInfo choseWorldTarget = AccessTools.Method(typeof(Building_Railgun), "ChoseWorldTarget", new[] { typeof(GlobalTargetInfo) });
        MethodInfo longTargetGetter = AccessTools.PropertyGetter(typeof(Building_EnergyWeapon), nameof(Building_EnergyWeapon.longTarget));
        MethodInfo sabotArrived = AccessTools.Method(typeof(WorldObject_Sabot), "Arrived");
        worldStrikeShellDef = DefDatabase<ThingDef>.GetNamedSilentFail("Bullet_155mmHowitzerShell_HE");
        if (choseWorldTarget == null || longTargetGetter == null || sabotArrived == null || worldStrikeShellDef == null)
        {
            Log.Warning("Combat Extended :: Rimatomics railgun world strike disabled; a Rimatomics method or the 155mm shell def was not found.");
            return;
        }
        // Prevent failure from holding up other patches as Patches.Install() has no per-patch guard
        // ChoseWorldTarget goes last: in case of failure no world strike can be ordered & the others do nothing
        try
        {
            Harmony harmony = HarmonyBase.instance;
            harmony.Patch(longTargetGetter, postfix: new HarmonyMethod(typeof(Rimatomics), nameof(LongTarget_Postfix)));
            harmony.Patch(sabotArrived, prefix: new HarmonyMethod(typeof(Rimatomics), nameof(SabotArrived_Prefix)));
            harmony.Patch(choseWorldTarget, prefix: new HarmonyMethod(typeof(Rimatomics), nameof(ChoseWorldTarget_Prefix)));
        }
        catch (Exception e)
        {
            Log.Error($"Combat Extended :: Failed to patch Rimatomics railgun world strike: {e}");
        }
    }

    /// <summary>A world object CE can shell. Can't be loaded (have a map) or owned by player. Has CE health and hostility comps.</summary>
    private static bool CanWorldStrike(WorldObject worldObject)
    {
        return worldObject != null
               && worldObject.Spawned
               && !worldObject.Destroyed
               && worldObject.Faction != Faction.OfPlayer
               && !(worldObject is MapParent mapParent && mapParent.HasMap)
               && worldObject.GetComponent<HealthComp>() != null
               && worldObject.GetComponent<HostilityComp>() != null;
    }

    /// <summary>
    /// Fire Mission's world tile targeter callback. Tiles with a map, out-of-range targets and anything CE cannot shell
    /// default to Rimatomics logic. Shellable unloaded targets become a world-object fire mission.
    /// </summary>
    private static bool ChoseWorldTarget_Prefix(Building_Turret __instance, GlobalTargetInfo target, object ___selectedRailguns, ref bool __result)
    {
        WorldObject worldObject = target.WorldObject;
        if (!CanWorldStrike(worldObject))
        {
            return true;
        }
        Building_Railgun origin = (Building_Railgun)__instance;
        int distance = Find.WorldGrid.TraversalDistanceBetween(origin.Map.Tile, worldObject.Tile, true, int.MaxValue, false);
        if (distance > origin.WorldRange)
        {
            return true;
        }
        // A loop rather than a lambda, so no compiler-generated member signature names a Rimatomics type.
        List<Thing> railguns = new List<Thing>();
        foreach (Building_Railgun railgun in (___selectedRailguns as List<Building_Railgun>) ?? new List<Building_Railgun> { origin })
        {
            if (railgun.Spawned && railgun.WorldRange >= distance)
            {
                railguns.Add(railgun);
            }
            else if (railgun.Spawned)
            {
                // The feedback Rimatomics' FireMission gives each selected railgun that cannot reach.
                Messages.Message("MessageTargetBeyondMaximumRange".Translate(), railgun, MessageTypeDefOf.RejectInput, true);
            }
        }
        Faction faction = worldObject.Faction;
        if (faction != null && !faction.Hidden)
        {
            if (faction.RelationWith(origin.Faction, true) == null)
            {
                faction.TryMakeInitialRelationsWith(origin.Faction);
            }
            if (!faction.HostileTo(origin.Faction))
            {
                // Same confirm as Command_ArtilleryTarget.
                Find.WindowStack.Add(new Dialog_MessageBox(
                                         "CE_ArtilleryTarget_AttackingAllies".Translate().Formatted(worldObject.Label, faction.Name),
                                         "CE_Yes".Translate(),
                                         delegate
                                         {
                                             OrderWorldStrikes(railguns, worldObject);
                                             Find.WorldTargeter.StopTargeting();
                                         },
                                         "CE_No".Translate(),
                                         delegate
                                         {
                                             Find.WorldTargeter.StopTargeting();
                                         }, buttonADestructive: true));
                __result = false;
                return false;
            }
        }
        OrderWorldStrikes(railguns, worldObject);
        __result = true;
        return false;
    }

    private static void OrderWorldStrikes(List<Thing> railguns, WorldObject target)
    {
        foreach (Thing railgun in railguns)
        {
            OrderWorldStrike(railgun, target);
        }
    }

    /// <summary>
    /// Synced for Multiplayer. CE registers every SyncMethod whether or not Rimatomics is installed, and Multiplayer
    /// patches it w/ Harmony, which loads every type its body names. So this body must not name any Rimatomics type;
    /// ApplyWorldStrike does the work instead.
    /// </summary>
    [Multiplayer.SyncMethod]
    private static void OrderWorldStrike(Thing thing, WorldObject target)
    {
        ApplyWorldStrike(thing, target);
    }

    /// <summary>
    /// Rimatomics' Building_Railgun.FireMission with a world object instead of a map cell. It also clears the local forced
    /// target, because TickGuns cancels the whole order, long-range target included, when that target dies. Never inlined.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ApplyWorldStrike(Thing thing, WorldObject target)
    {
        if (!(thing is Building_Railgun railgun) || !railgun.Spawned || !CanWorldStrike(target))
        {
            return;
        }
        railgun.forcedTarget = LocalTargetInfo.Invalid;
        GlobalTargetInfo strike = new GlobalTargetInfo(target);
        if (railgun.longTargetInt != strike)
        {
            railgun.longTargetInt = strike;
            if (railgun.burstCooldownTicksLeft <= 0)
            {
                railgun.TryStartShootSomething();
            }
            SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(railgun.Position, railgun.Map));
        }
        if (railgun.holdFire)
        {
            Messages.Message("MessageTurretWontFireBecauseHoldFire".Translate(railgun.def.label), railgun, MessageTypeDefOf.RejectInput, true);
        }
    }

    /// <summary>
    /// Building_EnergyWeapon.TickGuns() drops long range targets with null Maps, always the case for world targets.
    /// So while the strike is valid, the value this getter returns is given the railgun's own map. The stored
    /// longTargetInt is not modified (it saves as a plain world target), the tile still resolves to the world object
    /// and Cell stays invalid.
    /// Once the target is destroyed, captured or gains a map, the unmodified value lets Rimatomics drop the order itself.
    /// </summary>
    private static void LongTarget_Postfix(Building_Turret __instance, ref GlobalTargetInfo __result)
    {
        if (__result.worldObjectInt == null || __result.mapInt != null || __result.thingInt != null || !__instance.Spawned)
        {
            return;
        }
        if (CanWorldStrike(__result.worldObjectInt))
        {
            __result.mapInt = __instance.Map;
        }
    }

    /// <summary>
    /// Runs before WorldObject_Sabot.Arrived() and never skips it. Only world-strike sabots are touched: they carry an
    /// invalid destination cell, which a Fire Mission at a map never does. With no map at the destination, apply CE
    /// world-object damage in the same order as TravelingShell.TryShell; Arrived() then removes it like normal.
    /// </summary>
    private static void SabotArrived_Prefix(bool ___arrived, int ___destinationTile, ref IntVec3 ___destinationCell, Thing ___railgun, int ___initialTile)
    {
        if (___arrived || ___destinationCell.IsValid)
        {
            return;
        }
        // Runs inside WorldObject_Sabot.Tick. RimWorld doesn't catch per world object, so an exception here would abort the
        // whole world tick, and again every tick after, since the sabot would never be removed. On any failure, log once and
        // let Arrived() discard the round the way Rimatomics does without CE.
        try
        {
            Map map = Current.Game.FindMap(___destinationTile);
            if (map != null)
            {
                // The target gained a map midflight: pick random impact cell, as CE shells do.
                ___destinationCell = ShellingUtility.FindRandomImpactCell(map, worldStrikeShellDef);
                return;
            }
            Faction attacker = ___railgun?.Faction ?? Faction.OfPlayer;
            GlobalTargetInfo source = ___railgun != null && ___railgun.Spawned
                                      ? new GlobalTargetInfo(___railgun.Position, ___railgun.Map)
                                      : new GlobalTargetInfo((PlanetTile)___initialTile);
            foreach (WorldObject worldObject in Find.WorldObjects.ObjectsAt(___destinationTile).ToList())
            {
                HostilityComp hostility = worldObject.GetComponent<HostilityComp>();
                HealthComp healthComp = worldObject.GetComponent<HealthComp>();
                if (worldObject.Faction == Faction.OfPlayer || hostility == null || healthComp == null)
                {
                    continue;
                }
                if (worldObject.Faction != null)
                {
                    hostility.TryHostilityResponse(attacker, source);
                }
                healthComp.ApplyDamage(worldStrikeShellDef, attacker, source);
                break;
            }
        }
        catch (Exception e)
        {
            Log.ErrorOnce($"Combat Extended :: Rimatomics railgun world strike impact failed: {e}", 0x52574931);
        }
    }
    #endregion
}



