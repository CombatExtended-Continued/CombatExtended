using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

using CombatExtended;

namespace CombatExtended
{
    public class JobDriver_UnloadAmmo : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            ThingWithComps equipment = pawn?.equipment?.Primary;
            if (equipment == null)
            {
                yield break;
            }
            CompMechAmmo mechAmmo = pawn?.GetComp<CompMechAmmo>();
            if (mechAmmo != null)
            {
                yield return Toils_Ammo.DropUnusedAmmo(mechAmmo);
            }

        }
    }
}
