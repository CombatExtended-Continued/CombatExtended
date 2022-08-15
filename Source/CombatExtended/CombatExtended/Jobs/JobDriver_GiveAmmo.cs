﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class JobDriver_GiveAmmo : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return GetActor().CanReserveAndReach(TargetA, PathEndMode.ClosestTouch, Danger.Deadly);
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_General.Do(
                delegate
                {
                    var targetPawn = (Pawn)TargetA.Thing;

                    var ammoGiverComp = targetPawn.TryGetComp<CompAmmoGiver>();

                    var newThing = ThingMaker.MakeThing(TargetB.Thing.def);

                    newThing.HitPoints = TargetB.Thing.HitPoints;

                    newThing.stackCount = ammoGiverComp.ammoAmountToGive;

                    TargetB.Thing.stackCount -= ammoGiverComp.ammoAmountToGive;

                    if (TargetB.Thing.stackCount <= 0)
                    {
                        TargetB.Thing.Destroy();
                    }

                    targetPawn.inventory.TryAddItemNotForSale(newThing);
                });
        }
    }
}
