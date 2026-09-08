using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VanillaGravshipExpanded;
using Verse;
using Verse.AI;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_EnemyMechTurret.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended-Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat;

[StaticConstructorOnStartup]
public class Building_EnemyMechTurretCE : Building_GravshipTurretCE
{
    private List<Map> cachedMapsInRange;
    public override bool CanFire => true;
    public override bool CanAutoAttack => true;
    public override float GravshipTargeting => 1f;
    protected override bool CanSetForcedTarget => true;
    // Not used in CE
    // public override bool HideForceTargetGizmo => true;

    protected override bool ShowNoLinkedTerminalOverlay => false;

    // Not used in CE
    // private CompWorldArtillery compWorldArtillery;

    public override float BurstCooldownTime()
    {
        float cooldown = base.BurstCooldownTime();
        float factor = 1f;
        float flat = 0f;
        int bufferLinks = 0;
        foreach (var b in Map.listerBuildings.allBuildingsNonColonist)
        {
            if (b.Faction == Faction)
            {
                if (b.TryGetComp<CompEnemyTerminal>() is CompEnemyTerminal terminal && terminal.IsManned)
                {
                    factor *= terminal.Props.cooldownFactor;
                    flat += terminal.Props.cooldownFlatOffset;
                }
                if (b.TryGetComp<CompEnemyTurretBuffer>() is CompEnemyTurretBuffer buffer && buffer.Active && buffer.Props.validTurrets.Contains(this.def) && b.Position.DistanceTo(this.Position) <= buffer.Props.radius)
                {
                    if (buffer.Props.cooldownReductionTicks > 0)
                    {
                        flat += buffer.Props.cooldownReductionTicks;
                        bufferLinks++;
                        if (bufferLinks >= buffer.Props.maxLinks)
                        {
                            break;
                        }
                    }
                }
            }
        }
        return Mathf.Max(6.33f, (cooldown * factor) - (flat / 60f));
    }

    private int GetTargetPriority(Thing t)
    {
        if (t is Building_GravshipTurret)
        {
            return 1;
        }
        if (t.def == VGEDefOf.VGE_GiantThruster)
        {
            return 2;
        }
        if (t.def == ThingDefOf.LargeThruster)
        {
            return 3;
        }
        if (t.def == ThingDefOf.SmallThruster)
        {
            return 4;
        }
        if (t.def == VGEDefOf.VGE_GiantAstrofuelTank)
        {
            return 5;
        }
        if (t.def == VGEDefOf.LargeChemfuelTank)
        {
            return 6;
        }
        if (t.def == ThingDefOf.ChemfuelTank)
        {
            return 7;
        }
        if (t is Building_Bed)
        {
            return 8;
        }
        if (t is Pawn pawn && pawn.IsColonist && pawn.Downed is false)
        {
            return 9;
        }
        return 10;
    }

    public override LocalTargetInfo TryFindNewTarget()
    {
        if (GravshipUtility.GetPlayerGravEngine_NewTemp(Map) != null)
        {
            return GetTargetForMap(Map);
        }
        // replace vge artillery comp with our logic
        // if (compWorldArtillery != null)
        if (IsMortar && Active && Faction.IsPlayerSafe() && ProjectileProps?.shellingProps != null)
        {
            if (cachedMapsInRange == null || this.IsHashIntervalTick(250))
            {
                var mapsWithDist = new List<(Map map, float distance)>();
                foreach (var map in Find.Maps)
                {
                    if (map == null || map.Disposed)
                    {
                        continue;
                    }
                    if (map.IsPocketMap is false)
                    {
                        float dist = GravshipHelper.GetDistance(Map.Tile, map.Tile);
                        if (dist <= MaxWorldRange) // Here, we use our MaxWorldRange
                        {
                            mapsWithDist.Add((map, dist));
                        }
                    }
                }
                mapsWithDist.Sort((a, b) =>
                {
                    int cmp = (GravshipUtility.GetPlayerGravEngine_NewTemp(b.map) != null ? 1 : 0)
                            - (GravshipUtility.GetPlayerGravEngine_NewTemp(a.map) != null ? 1 : 0);
                    if (cmp != 0) { return cmp; }
                    return a.distance.CompareTo(b.distance);
                });
                cachedMapsInRange = mapsWithDist.Select(x => x.map).ToList();
            }

            foreach (var map in cachedMapsInRange)
            {
                if (map == null || map.Disposed)
                {
                    continue;
                }
                var target = GetTargetForMap(map);
                if (target.IsValid)
                {
                    if (map != Map && Active)
                    {
                        return LocalTargetInfo.Invalid;
                    }
                    return target;
                }
            }
        }
        return GetTargetForMap(Map);
    }

    private LocalTargetInfo GetTargetForMap(Map map)
    {
        var searcher = this;
        var verb = AttackVerb;
        var searcherThing = searcher;
        var playerEngine = map == Map ? GravshipUtility.GetPlayerGravEngine_NewTemp(map) : null;
        TargetScanFlags flags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable;
        if (!AttackVerb.ProjectileFliesOverhead())
        {
            flags |= TargetScanFlags.NeedLOSToAll;
            flags |= TargetScanFlags.LOSBlockableByGas;
        }
        if (AttackVerb.IsIncendiary_Ranged())
        {
            flags |= TargetScanFlags.NeedNonBurning;
        }
        if (IsMortar)
        {
            flags |= TargetScanFlags.NeedNotUnderThickRoof;
        }

        Predicate<IAttackTarget> innerValidator = delegate (IAttackTarget t)
        {
            Thing thing = t.Thing;
            if (t == searcher)
            {
                return false;
            }
            if (thing.Map == Map)
            {
                if (playerEngine == null || !playerEngine.OnValidSubstructure(thing))
                {
                    return false;
                }
                float num3 = verb.verbProps.EffectiveMinRange(thing, searcherThing);
                if (num3 > 0f && (float)(searcherThing.Position - thing.Position).LengthHorizontalSquared < num3 * num3)
                {
                    return false;
                }
            }
            if (!searcherThing.HostileTo(thing))
            {
                return false;
            }
            if ((flags & TargetScanFlags.NeedNotUnderThickRoof) != 0)
            {
                RoofDef roof = thing.Position.GetRoof(thing.Map);
                if (roof != null && roof.isThickRoof)
                {
                    return false;
                }
            }
            if (((flags & TargetScanFlags.NeedThreat) != 0 || (flags & TargetScanFlags.NeedAutoTargetable) != 0) && t.ThreatDisabled(searcher))
            {
                return false;
            }
            if ((flags & TargetScanFlags.NeedAutoTargetable) != 0 && !AttackTargetFinder.IsAutoTargetable(t))
            {
                return false;
            }
            if ((flags & TargetScanFlags.NeedActiveThreat) != 0 && !GenHostility.IsActiveThreatTo(t, searcher.Faction))
            {
                return false;
            }
            return true;
        };

        var seenTargets = new Dictionary<Thing, int>();
        foreach (IAttackTarget target in map.attackTargetsCache.GetPotentialTargetsFor(this))
        {
            if (innerValidator(target))
            {
                seenTargets.TryAdd(target.Thing, GetTargetPriority(target.Thing));
            }
        }
        foreach (var building in map.listerBuildings.allBuildingsColonist)
        {
            if (building == searcherThing || !searcherThing.HostileTo(building))
            {
                continue;
            }
            if (map == Map && (playerEngine == null || !playerEngine.OnValidSubstructure(building)))
            {
                continue;
            }
            if (!seenTargets.ContainsKey(building))
            {
                int priority = GetTargetPriority(building);
                if (priority < 10)
                {
                    seenTargets[building] = priority;
                }
            }
        }
        var potentialTargets = seenTargets.OrderBy(x => x.Value).Select(x => x.Key).ToList();
        foreach (Thing target in potentialTargets)
        {
            if (map == Map)
            {
                if (verb.CanHitTarget(target))
                {
                    return target;
                }
            }
            else
            {
                // Replace with our own logic
                // compWorldArtillery.worldTarget = new GlobalTargetInfo(target);
                // compWorldArtillery.target = new LocalTargetInfo(target);
                // this.forcedTarget = compWorldArtillery.FindEdgeCell(Map, compWorldArtillery.worldTarget);
                TryAttackWorldTarget(new GlobalTargetInfo(target), new LocalTargetInfo(target));
                return this.forcedTarget; // this.forcedTarget is set in Building_TurretGunCE.TryOrderAttackWorldTile
            }
        }
        return LocalTargetInfo.Invalid;
    }
}
