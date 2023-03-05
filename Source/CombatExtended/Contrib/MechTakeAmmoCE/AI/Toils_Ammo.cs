using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using Verse.AI;
using RimWorld;
using CombatExtended;

namespace CombatExtended
{
    public static class Toils_Ammo
    {
        public static Toil Drop(ThingDef def, int count)
        {
            Toil toil = ToilMaker.MakeToil("DropAmmo");
            toil.initAction = () =>
            {
                Pawn actor = toil.actor;
                actor.inventory.DropCount(def, count);
            };
            return toil;
        }

        public static Toil TryUnloadAmmo(CompAmmoUser ammoUser)
        {
            Toil toil = ToilMaker.MakeToil("TryUnloadAmmo");
            toil.initAction = () =>
            {
                ammoUser?.TryUnload(true);
            };
            return toil;
        }

        public static Toil DropUnusedAmmo(CompMechAmmo mechAmmo)
        {
            Toil toil = ToilMaker.MakeToil("DropUnusedAmmo");
            toil.initAction = () =>
            {
                mechAmmo.DropUnusedAmmo();
            };
            return toil;
        }
    }
}
