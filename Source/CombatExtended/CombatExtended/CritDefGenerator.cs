using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public static class CritDefGenerator
    {
        public static IEnumerable<DamageDef> ImpliedCritDefs()
        {                                                                           //TODO: ExternalViolence for what?
            foreach(DamageDef current in from def in DefDatabase<DamageDef>.AllDefs where def.ExternalViolenceFor() select def)
            {
                var critDef = new DamageDef() {
                    defName = current.defName + "_Critical",
                    workerClass = current.workerClass,
                    //externalViolence = true,
                    impactSoundType = current.impactSoundType,
                    //spreadOut = current.spreadOut,
                    harmAllLayersUntilOutside = current.harmAllLayersUntilOutside,
                    //hasChanceToAdditionallyDamageInnerSolidParts = current.hasChanceToAdditionallyDamageInnerSolidParts,
                    hediff = current.hediff,
                    hediffSkin = current.hediffSkin,
                    hediffSolid = current.hediffSolid
                };
                yield return critDef;
            }
        }
    }
}
