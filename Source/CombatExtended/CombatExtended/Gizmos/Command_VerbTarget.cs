using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Sound;

namespace CombatExtended
{
    // Cloned from Verse.Command_VerbTarget which is internal for no goddamn reason
    public class Command_VerbTarget : Command
    {
        public Verb verb;

        public override Color IconDrawColor
        {
            get
            {
                if (verb.EquipmentSource != null)
                {
                    return verb.EquipmentSource.DrawColor;
                }
                return base.IconDrawColor;
            }
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            Targeter targeter = Find.Targeter;
            if (verb.CasterIsPawn && targeter.targetingVerb != null && targeter.targetingVerb.verbProps == verb.verbProps)
            {
                Pawn casterPawn = verb.CasterPawn;
                if (!targeter.IsPawnTargeting(casterPawn))
                {
                    targeter.targetingVerbAdditionalPawns.Add(casterPawn);
                }
            }
            else
            {
                Find.Targeter.BeginTargeting(verb);
            }
        }
    }
}

