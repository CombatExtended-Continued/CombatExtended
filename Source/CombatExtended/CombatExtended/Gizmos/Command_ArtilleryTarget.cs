using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Command_ArtilleryTarget : Command
    {        
        public Building_TurretGunCE turret;        
        public List<Command_ArtilleryTarget> others = null;

        public IEnumerable<Building_TurretGunCE> SelectedTurrets
        {
            get
            {
                return others?.Select(o => o.turret) ?? null;
            }
        }

        public override bool GroupsWith(Gizmo other)
        {
            var order = other as Command_ArtilleryTarget;
            return order != null && (order.turret?.Active ?? false) && order.turret.def.building.IsMortar;
        }

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
            int radius = turret.MaxWorldRange;
            Find.WorldTargeter.BeginTargeting((targetInfo) =>
            {
                if(others != null)
                {
                    bool started = false;
                    foreach(var t in SelectedTurrets)
                    {
                        if (t.Active)
                        {
                            started = true;
                            t.AttackGlobalTarget(targetInfo);
                        }
                    }
                    return started;
                }
                else if(turret.Active)
                {
                    turret.AttackGlobalTarget(targetInfo);
                    return true;
                }
                return false;
            }, true, closeWorldTabWhenFinished: false, onUpdate: ()=>
            {
                if (others != null)
                {
                    foreach (var t in SelectedTurrets)
                    {
                        if (t.MaxWorldRange != radius)                        
                            GenDraw.DrawWorldRadiusRing(tile, t.MaxWorldRange);                        
                    }
                }
                GenDraw.DrawWorldRadiusRing(tile, radius);                
            }, extraLabelGetter: (targetInfo) =>
            {
                int distanceToTarget = Find.WorldGrid.TraversalDistanceBetween(tile, targetInfo.Tile, true, maxDist: (int) (turret.MaxWorldRange * 1.5f));
                if (turret.MaxWorldRange > 0 && distanceToTarget > turret.MaxWorldRange)
                {
                    GUI.color = ColorLibrary.RedReadable;
                    return "ArtilleryTarget_DestinationBeyondMaximumRange".Translate();
                }
                return "ClickToSeeAvailableOrders_Empty".Translate();
            });
            base.ProcessInput(ev);
        }
    }
}
