using System;
using System.Collections.Generic;
using System.Linq;

using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class GameComponent_MechLoadoutDialogManger : GameComponent
    {

        //queue of compMechAmmo
        private List<CompMechAmmo> _compMechAmmoQueue = new List<CompMechAmmo>();

        public GameComponent_MechLoadoutDialogManger(Game game)
        {
        }

        public override void GameComponentUpdate()
        {
            //open dialog if queue is not empty
            //copy the queue to a new list to avoid concurrent modification
            if (_compMechAmmoQueue.Count > 0)
            {
                List<CompMechAmmo> compMechAmmoList = new List<CompMechAmmo>(_compMechAmmoQueue);
                HashSet<AmmoLink> ammoTypes = new HashSet<AmmoLink>(compMechAmmoList[0].AmmoUser.Props.ammoSet.ammoTypes);
                foreach (var compMechAmmo in compMechAmmoList)
                {
                    if (!ammoTypes.SetEquals(compMechAmmo.AmmoUser.Props.ammoSet.ammoTypes))
                    {
                        Messages.Message("MTA_CannotSetLoadoutForMultipleEquipment".Translate(), MessageTypeDefOf.RejectInput);
                        _compMechAmmoQueue.Clear();
                        return;
                    }
                }
                _compMechAmmoQueue.Clear();
                Find.WindowStack.Add(new Dialog_SetMagCountBatched(compMechAmmoList));
            }
        }

        //add compMechAmmo to queue
        public void RegisterCompMechAmmo(CompMechAmmo compMechAmmo)
        {
            if (!_compMechAmmoQueue.Contains(compMechAmmo))
            {
                _compMechAmmoQueue.Add(compMechAmmo);
            }
        }


    }
}


