using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Command_ArtilleryTarget : Command
    {     
        public Building_TurretGunCE turret;

        public List<Command_ArtilleryTarget> others = null;

        public IEnumerable<Building_TurretGunCE> SelectedTurrets => others?.Select(o => o.turret) ?? new List<Building_TurretGunCE>() { turret };

        public override bool GroupsWith(Gizmo other) => other is Command_ArtilleryTarget;

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

        public override void ProcessInput(Event ev)
        {            
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(turret));
            Find.WorldSelector.ClearSelection();            

            if (turret == null)
            {
                Log.Error("Command_ArtilleryTarget without turret");
                return;
            }
            if(!turret.Active || (SelectedTurrets?.Any(t => t.Destroyed || !t.Active || !t.def.building.IsMortar) ?? false))
            {
                Log.Error("Command_ArtilleryTarget selected turrets collection is invalid");
                return;
            }
            int tile = turret.Map.Tile;
            int radius = (int) turret.MaxWorldRange;            
            Find.WorldTargeter.BeginTargeting((targetInfo) =>
            {                
                if (!targetInfo.HasWorldObject || targetInfo.Tile == tile)
                {
                    return false;
                }                
                IEnumerable<Building_TurretGunCE> turrets = SelectedTurrets;                
                Map map = Find.World.worldObjects.MapParentAt(targetInfo.Tile)?.Map ?? null;                
                if (map != null)
                {
                    IntVec3 selectedCell = IntVec3.Invalid;
                    Find.WorldTargeter.StopTargeting();
                    CameraJumper.TryJumpInternal(new IntVec3((int)map.Size.x / 2, 0, (int)map.Size.z / 2), map);
                    Find.Targeter.BeginTargeting(new TargetingParameters()
                    {
                        canTargetLocations = true,
                        canTargetBuildings = true,
                        canTargetHumans = true
                    }, (target)=>
                    {
                        targetInfo.mapInt = map;
                        targetInfo.tileInt = map.Tile;
                        targetInfo.cellInt = target.cellInt;
                        targetInfo.thingInt = target.thingInt;                        
                        TryAttack(turrets, targetInfo, target);
                    }, highlightAction: (target)=>
                    {
                        GenDraw.DrawTargetHighlight(target);
                    }, targetValidator: (target) =>
                    {
                        RoofDef roof = map.roofGrid.RoofAt(target.Cell);
                        return roof == null || roof == RoofDefOf.RoofConstructed;
                    });                                                
                    return false;
                }
                if(targetInfo.WorldObject.Faction != null)
                {
                    Faction targetFaction = targetInfo.WorldObject.Faction;
                    FactionRelation relation = targetFaction.RelationWith(turret.Faction, true);
                    if(relation == null)
                    {
                        targetFaction.TryMakeInitialRelationsWith(turret.Faction);
                    }
                    if (!targetFaction.HostileTo(turret.Faction))
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
            }, true, closeWorldTabWhenFinished: true, onUpdate: ()=>
            {
                if (others != null)
                {
                    foreach (var t in SelectedTurrets)
                    {
                        if (t.MaxWorldRange != radius)
                        {
                            GenDraw.DrawWorldRadiusRing(tile, (int)t.MaxWorldRange);
                        }                        
                    }
                }
                GenDraw.DrawWorldRadiusRing(tile, radius);                
            }, extraLabelGetter: (targetInfo) =>
            {
                int distanceToTarget = Find.WorldGrid.TraversalDistanceBetween(tile, targetInfo.Tile, true);
                string distanceMessage = null;
                if (others != null)
                {
                    int inRangeCount = 0;
                    int count = 0;
                    foreach (var t in SelectedTurrets)
                    {
                        count++;
                        if(t.MaxWorldRange >= distanceToTarget)
                        {
                            inRangeCount++;
                        }
                    }
                    distanceMessage = "CE_ArtilleryTarget_Distance_Selections".Translate().Formatted(distanceToTarget, inRangeCount, count);
                }
                else
                {
                    distanceMessage = "CE_ArtilleryTarget_Distance".Translate().Formatted(distanceToTarget, radius);
                }
                if (turret.MaxWorldRange > 0 && distanceToTarget > turret.MaxWorldRange)
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
                if(targetInfo.WorldObject is Settlement settlement)
                {
                    extra = $" {settlement.Name}";
                    if(settlement.Faction != null && !settlement.Faction.name.NullOrEmpty())
                    {
                        extra += $" ({settlement.Faction.name})";
                    }
                }
                return distanceMessage + "\n" + "CE_ArtilleryTarget_ClickToOrderAttack".Translate() + extra;
            });
            base.ProcessInput(ev);
        }

        private bool TryAttack(IEnumerable<Building_TurretGunCE> turrets, GlobalTargetInfo targetInfo, LocalTargetInfo localTargetInfo)
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
    }
}
