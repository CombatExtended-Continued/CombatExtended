using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.Detours
{
    internal static class Detours_Pawn_ApparelTracker
    {
        internal static bool TryDrop(this Pawn_ApparelTracker _this, Apparel ap, out Apparel resultingAp, IntVec3 pos, bool forbid = true)
        {
            if (!_this.WornApparel.Contains(ap))
            {
                Log.Warning(_this.pawn.LabelCap + " tried to drop apparel he didn't have: " + ap.LabelCap);
                resultingAp = null;
                return false;
            }
            if (_this.pawn.MapHeld == null)
            {
                Log.Warning(_this.pawn.LabelCap + " tried to drop apparel but his MapHeld is null.");
                resultingAp = null;
                return false;
            }
            ap.Notify_Stripped(_this.pawn);
            _this.Remove(ap);
            Thing thing = null;

            bool result = GenThing.TryDropAndSetForbidden(ap, pos, _this.pawn.MapHeld, ThingPlaceMode.Near, out thing, forbid);
            resultingAp = (thing as Apparel);
            CE_Utility.TryUpdateInventory(_this.pawn);     // Apparel was dropped, update inventory
            return result;
        }

        internal static void Wear(this Pawn_ApparelTracker _this, Apparel newApparel, bool dropReplacedApparel = true)
        {
            SlotGroupUtility.Notify_TakingThing(newApparel);
            if (newApparel.Spawned)
            {
                newApparel.DeSpawn();
            }
            if (!ApparelUtility.HasPartsToWear(_this.pawn, newApparel.def))
            {
                Log.Warning(string.Concat(new object[]
		        {
		            _this.pawn,
		            " tried to wear ",
		            newApparel,
		            " but he has no body parts required to wear it."
		        }));
                return;
            }
            for (int i = _this.WornApparel.Count - 1; i >= 0; i--)
            {
                Apparel apparel = _this.WornApparel[i];
                if (!ApparelUtility.CanWearTogether(newApparel.def, apparel.def))
                {
                    bool forbid = _this.pawn.Faction.HostileTo(Faction.OfPlayer);
                    if (dropReplacedApparel)
                    {
                        Apparel apparel2;
                        if (!_this.TryDrop(apparel, out apparel2, _this.pawn.Position, forbid))
                        {
                            Log.Error(_this.pawn + " could not drop " + apparel);
                            return;
                        }
                    }
                    else
                    {
                        _this.Remove(apparel);
                    }
                }
            }
            if (newApparel.wearer != null)
            {
                Log.Warning(string.Concat(new object[]
                {
                    _this.pawn,
                    " is trying to wear ",
                    newApparel,
                    " but this apparel already has a wearer (",
                    newApparel.wearer,
                    "). This may or may not cause bugs."
                }));
            }
            _this.WornApparel.Add(newApparel);
            newApparel.wearer = _this.pawn;
            _this.SortWornApparelIntoDrawOrder();
            _this.ApparelChanged();

            //CE PART
            CE_Utility.TryUpdateInventory(_this.pawn);     // Apparel was added, update inventory
            MethodInfo methodInfo = typeof(Pawn_ApparelTracker).GetMethod("SortWornApparelIntoDrawOrder", BindingFlags.Instance | BindingFlags.NonPublic);
            methodInfo.Invoke(_this, new object[] { });

            LongEventHandler.ExecuteWhenFinished(new Action(_this.ApparelChanged));
        }

        internal static void Notify_WornApparelDestroyed(this Pawn_ApparelTracker _this, Apparel apparel)
        {
            _this.Remove(apparel);
            CE_Utility.TryUpdateInventory(_this.pawn);     // Apparel was destroyed, update inventory
        }

        private static void SortWornApparelIntoDrawOrder(this Pawn_ApparelTracker _this)
        {
            _this.WornApparel.Sort((Apparel a, Apparel b) => a.def.apparel.LastLayer.CompareTo(b.def.apparel.LastLayer));
        }

        private static void ApparelChanged(this Pawn_ApparelTracker _this)
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                _this.pawn.Drawer.renderer.graphics.ResolveApparelGraphics();
                PortraitsCache.SetDirty(_this.pawn);
            });
        }
    }
}
