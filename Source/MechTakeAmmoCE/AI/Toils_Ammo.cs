using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using Verse.AI;
using RimWorld;

namespace MTA
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
    }
}
