using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Harmony;

namespace CombatExtended
{
    public static class CritDefGenerator
    {
        public static IEnumerable<DamageDef> ImpliedCritDefs()
        {
            foreach(DamageDef current in DefDatabase<DamageDef>.AllDefs.Where(d => d.harmsHealth))
            {
                var critDef = new DamageDef() {
                    defName = current.defName + "_Critical",
                    workerClass = current.workerClass,
                    impactSoundType = current.impactSoundType,
                    minDamageToFragment = current.minDamageToFragment,
                    harmAllLayersUntilOutside = current.harmAllLayersUntilOutside,
                    //hasChanceToAdditionallyDamageInnerSolidParts = current.hasChanceToAdditionallyDamageInnerSolidParts,
                    hediff = current.hediff,
                    hediffSkin = current.hediffSkin,
                    hediffSolid = current.hediffSolid
                };
                // Private fields now :V
                Traverse.Create(critDef).Field("externalViolence").SetValue(Traverse.Create(current).Field("externalViolence").GetValue());
                Traverse.Create(critDef).Field("externalViolenceForMechanoids").SetValue(Traverse.Create(current).Field("externalViolenceForMechanoids").GetValue());
                yield return critDef;
            }
        }
    }
}
