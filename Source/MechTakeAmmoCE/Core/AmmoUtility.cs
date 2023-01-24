using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using CombatExtended;
using RimWorld;

namespace MTA
{
    public static class AmmoUtility
    {
        public static Thing FindBestAmmo(this Pawn pawn, ThingDef ammoDef)
        {
            if (pawn == null || ammoDef == null)
            {
                return null;
            }

            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(ammoDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false), 9999f, null, null, 0, -1, false, RegionType.Set_Passable, false);
        }

        public static List<FloatMenuOption> BuildAmmoOptions(this CompAmmoUser ammoUser, CompMechAmmo forMech = null)
        {
            List<FloatMenuOption> floatOptionList = new List<FloatMenuOption>();

            if (ammoUser == null)
            {
                return floatOptionList;
            }

            foreach (var ammoLink in ammoUser.Props.ammoSet.ammoTypes)
            {
                if (ammoLink.ammo == ammoUser.SelectedAmmo)
                {
                    continue;
                }
                FloatMenuOption option = new FloatMenuOption(ammoLink.ammo.ammoClass.label, () =>
                {
                    if (ammoUser.CurMagCount > 0)
                    {
                        ammoUser.TryUnload();
                    }

                    ammoUser.SelectedAmmo = ammoLink.ammo;

                    if (forMech != null)
                    {
                        forMech.TakeAmmoNow();
                    }
                });
                floatOptionList.Add(option);
            }

            return floatOptionList;
        }

        public static int NeedAmmo(this CompAmmoUser ammoUser, int amount)
        {
            int current = 0;
            if (ammoUser == null)
            {
                return 0;
            }

            if (ammoUser.CurrentAmmo == ammoUser.SelectedAmmo)
            {
                current = ammoUser.CurMagCount;
            }

            foreach (Thing thing in ammoUser.Holder.inventory.innerContainer)
            {
                if(thing.def == ammoUser.SelectedAmmo)
                {
                    current += thing.stackCount;
                }
            }
            return amount-current;
        }
    }
}
