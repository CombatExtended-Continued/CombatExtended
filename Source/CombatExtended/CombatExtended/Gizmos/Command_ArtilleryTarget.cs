using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CombatExtended;

public class Command_ArtilleryTarget : Command
{
    #region Fields

    public Building_TurretGunCE turret;

    public List<Command_ArtilleryTarget> others = null;

    #endregion

    #region Properties

    bool CanShootOtherLayers => turret.CompOrbitalTurret != null;
    ///// <summary>
    ///// When firing on orbital targets, it can be tricky to use binoculars ...
    ///// This disables the need of target mark.
    ///// </summary>
    bool MandatoryMarkToFireOutBounds => turret.CompOrbitalTurret?.Props.isMarkMandatory ?? true;

    public IEnumerable<Building_TurretGunCE> SelectedTurrets => others?.Select(o => o.turret) ?? new List<Building_TurretGunCE>() { turret };

    public override bool GroupsWith(Gizmo other) => other is Command_ArtilleryTarget;

    #endregion

    #region Methods

    public override void MergeWith(Gizmo other)
    {
        var order = other as Command_ArtilleryTarget;
        if (others == null)
        {
            others = new List<Command_ArtilleryTarget>();
            others.Add(this);
        }
        others.Add(order);
    }

    protected void CommandProcessInput(Event ev)
    {
        base.ProcessInput(ev);
    }

    public override void ProcessInput(Event ev)
    {
        CameraJumper.TryJump(CameraJumper.GetWorldTarget(turret));
        Find.WorldSelector.ClearSelection();

        if (turret == null)
        {
            Log.Error("Command_ArtilleryTarget without turret");
            return;
        }
        if (!turret.Active || (SelectedTurrets?.Any(t => t.Destroyed || !t.Active || !t.def.building.IsMortar) ?? false))
        {
            Log.Error("Command_ArtilleryTarget selected turrets collection is invalid");
            return;
        }
        PlanetTile turretTile = turret.Map.Tile;
        int radius = Mathf.FloorToInt(turret.MaxWorldRange);

        ShellingUtility.ClearRadiusCache();

        Find.WorldTargeter.BeginTargeting(
            action: (GlobalTargetInfo targetInfo) =>
            {
                // Check additionnal condition
                if (!AdditionnalTargettingCondition(targetInfo))
                {
                    return false;
                }

                IEnumerable<Building_TurretGunCE> turrets = SelectedTurrets;
                Map map = Find.World.worldObjects.MapParentAt(targetInfo.Tile)?.Map ?? null;

                if (!CanShootOtherLayers && targetInfo.Tile.Layer != turretTile.Layer)
                {
                    Messages.Message("CE_Message_ArtilleryBadLayer".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                // We only want player to target world object when there's no colonist in the map
                // Only if mark is needed
                if (map != null && (!MandatoryMarkToFireOutBounds || map.mapPawns.AnyPawnBlockingMapRemoval))
                {
                    return AttackWorldTile(turrets, targetInfo, map);
                }

                return AttackWorldObject(turrets, targetInfo);
            },
            canTargetTiles: true,
            closeWorldTabWhenFinished: true,
            onUpdate: () =>
            {
                if (others != null)
                {
                    foreach (var t in SelectedTurrets)
                    {
                        int radius2 = Mathf.FloorToInt(t.MaxWorldRange);
                        if (radius2 != radius)
                        {
                            ShellingUtility.CachedDrawTurretRadiusRing(t.Tile, radius2, CanShootOtherLayers);
                        }
                    }
                }
                ShellingUtility.CachedDrawTurretRadiusRing(turretTile, radius, CanShootOtherLayers);
            },
            extraLabelGetter: (targetInfo) =>
            {
                // Remove label when bad layer
                if (PlanetLayer.Selected != targetInfo.Tile.Layer)
                {
                    return "";
                }
                int distanceToTarget = ShellingUtility.GetDistancePlanetTiles(turretTile, targetInfo.Tile);
                float maxWorldRange = turret.MaxWorldRange;
                string distanceMessage = null;
                if (others != null)
                {
                    int inRangeCount = 0;
                    int count = 0;
                    foreach (var t in SelectedTurrets)
                    {
                        count++;
                        if (t.MaxWorldRange >= distanceToTarget)
                        {
                            inRangeCount++;
                        }
                    }
                    distanceMessage = "CE_ArtilleryTarget_Distance_Selections".Translate().Formatted(distanceToTarget, inRangeCount, count);
                }
                else
                {
                    distanceMessage = "CE_ArtilleryTarget_Distance".Translate().Formatted(distanceToTarget, maxWorldRange);
                }
                if (maxWorldRange > 0 && distanceToTarget > maxWorldRange)
                {
                    GUI.color = ColorLibrary.RedReadable;
                    return distanceMessage + "\n" + "CE_ArtilleryTarget_DestinationBeyondMaximumRange".Translate();
                }
                if (!targetInfo.HasWorldObject || targetInfo.WorldObject is Caravan)
                {
                    GUI.color = ColorLibrary.RedReadable;
                    return distanceMessage + "\n" + "CE_ArtilleryTarget_InvalidTarget".Translate();
                }
                string extra = "";
                if (targetInfo.WorldObject is Settlement settlement)
                {
                    extra = $" {settlement.Name}";
                    if (settlement.Faction != null && !settlement.Faction.name.NullOrEmpty())
                    {
                        extra += $" ({settlement.Faction.name})";
                    }
                }
                return distanceMessage + "\n" + "CE_ArtilleryTarget_ClickToOrderAttack".Translate() + extra;
            },
            canSelectTarget: (targetInfo) =>
            {
                if (
                // Is unvalid
                !targetInfo.HasWorldObject
                // Fire on its own tile
                || targetInfo.Tile == turretTile
                // World object has neither a Map nor a HealthComp (ennemy faction)
                || (targetInfo.WorldObject as MapParent)?.Map == null &&
                        targetInfo.WorldObject.GetComponent<WorldObjects.HealthComp>() == null)
                {
                    return false;
                }
                return true;
            });
        CommandProcessInput(ev);
    }

    /// <summary>
    /// Return false to stop the targeting process, true to continue.
    /// </summary>
    protected virtual bool AdditionnalTargettingCondition(GlobalTargetInfo targetInfo)
    {
        return true;
    }

    protected bool TryAttack(IEnumerable<Building_TurretGunCE> turrets, GlobalTargetInfo targetInfo, LocalTargetInfo localTargetInfo)
    {
        bool attackStarted = false;
        foreach (var t in turrets)
        {
            if (t.Active && t.TryAttackWorldTarget(targetInfo, localTargetInfo))
            {
                attackStarted = attackStarted || true;
            }
        }
        return attackStarted;
    }

    private bool AttackWorldTile(IEnumerable<Building_TurretGunCE> turrets, GlobalTargetInfo targetInfo, Map map)
    {
        IntVec3 selectedCell = IntVec3.Invalid;
        Find.WorldTargeter.StopTargeting();
        CameraJumper.TryJumpInternal(new IntVec3((int)map.Size.x / 2, 0, (int)map.Size.z / 2), map, CameraJumper.MovementMode.Pan);
        Find.Targeter.BeginTargeting(new TargetingParameters()
        {
            canTargetLocations = true,
            canTargetBuildings = true,
            canTargetHumans = true
        }, (target) =>
        {
            targetInfo.mapInt = map;
            targetInfo.tileInt = map.Tile;
            targetInfo.cellInt = target.cellInt;
            //targetInfo.thingInt = target.thingInt;
            TryAttack(turrets, targetInfo, target);
        }, highlightAction: (target) =>
        {
            GenDraw.DrawTargetHighlight(target);
        }, targetValidator: (target) =>
        {
            // Cannot fire through mountain roof
            RoofDef roof = map.roofGrid.RoofAt(target.Cell);
            if (roof != null && roof != RoofDefOf.RoofConstructed)
            {
                Messages.Message("CE_ArtilleryTarget_NoThickRoof".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }

            // Marker condition
            if (MandatoryMarkToFireOutBounds && target.Cell.GetFirstThing<ArtilleryMarker>(map) == null)
            {
                Messages.Message("CE_ArtilleryTarget_MustTargetMark".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        });
        return false;
    }

    private bool AttackWorldObject(IEnumerable<Building_TurretGunCE> turrets, GlobalTargetInfo targetInfo)
    {
        if (targetInfo.WorldObject.Destroyed || targetInfo.WorldObject is DestroyedSettlement || targetInfo.WorldObject.def == WorldObjectDefOf.DestroyedSettlement)
        {
            Messages.Message("CE_ArtilleryTarget_AlreadyDestroyed".Translate(), MessageTypeDefOf.CautionInput);
            return false;
        }

        if (targetInfo.WorldObject.Faction != null)
        {
            if (targetInfo.WorldObject.Faction == Faction.OfPlayer)
            {
                // We should not be able to target our own faction
                return false;
            }

            Faction targetFaction = targetInfo.WorldObject.Faction;
            FactionRelation relation = targetFaction.RelationWith(turret.Faction, true);
            if (relation == null)
            {
                targetFaction.TryMakeInitialRelationsWith(turret.Faction);
            }
            if (!targetFaction.HostileTo(turret.Faction) && !targetFaction.Hidden)
            {
                Find.WindowStack.Add(
                    new Dialog_MessageBox(
                        "CE_ArtilleryTarget_AttackingAllies".Translate().Formatted(targetInfo.WorldObject.Label, targetFaction.Name),
                        "CE_Yes".Translate(),
                        delegate
                        {
                            TryAttack(turrets, targetInfo, LocalTargetInfo.Invalid);
                            Find.WorldTargeter.StopTargeting();
                        },
                        "CE_No".Translate(),
                        delegate
                        {
                            Find.WorldTargeter.StopTargeting();
                        }, buttonADestructive: true));
                return false;
            }
        }
        return TryAttack(turrets, targetInfo, LocalTargetInfo.Invalid);
    }
    #endregion
}
