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

namespace IssacZhuangMTA
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
            CompAmmoUser ammoUser = pawn?.equipment?.Primary?.GetComp<CompAmmoUser>();
            if (ammoUser == null)
            {
                yield break;
            }

            yield return Toils_Ammo.TryUnloadAmmo(ammoUser);
        }
    }
}
