using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using SaveOurShip2;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended.Compatibility.SOS2Compat
{
    public class Command_TargetShipCombatCE : Command
    {
        // Pretty much a copy of Command_TargetShipCombat from SOS2 except with Building_ShipTurret changed to Building_ShipTurretCE
        #region License
        // Any SOS2 Code used for compatibility has been taken from the following source and is licensed under the "Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International Public License"
        // https://github.com/KentHaeger/SaveOurShip2/blob/cf179981d242764af20c41440d69649e6ecd6450/Source/1.5/Verb/Command_TargetShipCombat.cs
        #endregion

        public Verb verb;

        private List<Verb> groupedVerbs;
        public List<Building_ShipTurretCE> turrets; // We need to overwrite the turrets list to CE turrets
        public bool drawRadius = true;

        public override void GizmoUpdateOnMouseover()
        {
            if (drawRadius)
            {
                verb.verbProps.DrawRadiusRing(verb.caster.Position);
                if (!groupedVerbs.NullOrEmpty())
                {
                    foreach (Verb groupedVerb in groupedVerbs)
                    {
                        groupedVerb.verbProps.DrawRadiusRing(groupedVerb.caster.Position);
                    }
                }
            }
        }

        public override void MergeWith(Gizmo other)
        {
            base.MergeWith(other);
            Command_TargetShipCombatCE command_VerbTargetShip = other as Command_TargetShipCombatCE;
            if (command_VerbTargetShip == null)
            {
                Log.ErrorOnce("Tried to merge Command_VerbTarget with unexpected type", 73406263);
                return;
            }
            if (groupedVerbs == null)
            {
                groupedVerbs = new List<Verb>();
            }
            groupedVerbs.Add(command_VerbTargetShip.verb);
            if (command_VerbTargetShip.groupedVerbs != null)
            {
                groupedVerbs.AddRange(command_VerbTargetShip.groupedVerbs);
            }
        }

        public override void ProcessInput(Event ev)
        {
            var mapComp = turrets.FirstOrDefault().Map.GetComponent<ShipMapComp>();
            base.ProcessInput(ev);
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            if (mapComp.ShipMapState != ShipMapState.inCombat)
            {
                Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.TurretNotInShipCombat"), null, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            CameraJumper.TryJump(mapComp.ShipCombatTargetMap.Center, mapComp.ShipCombatTargetMap);
            Targeter targeter = Find.Targeter;
            TargetingParameters parms = new TargetingParameters();
            parms.canTargetPawns = true;
            parms.canTargetBuildings = true;
            parms.canTargetLocations = true;
            Find.Targeter.BeginTargeting(parms, (Action<LocalTargetInfo>)delegate (LocalTargetInfo x)
            {
                foreach (Building_ShipTurretCE turret in turrets)
                {
                    turret.SetTarget(x);
                }
            }, (Pawn)null, delegate { CameraJumper.TryJump(turrets[0].Position, mapComp.ShipCombatOriginMap); });
        }
    }
}
