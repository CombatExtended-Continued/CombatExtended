using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.Sound;
using System.Reflection.Emit;
using System;
using UnityEngine;
using RimWorld;


namespace CombatExtended.Compatibility
{
    public class Vehicles: IPatch
    {
        private static bool active = false;

        public static List<Func<Pawn, Tuple<bool, Vector2>>> CollisionBodyFactorCallbacks = new List<Func<Pawn, Tuple<bool, Vector2>>>();

        public bool CanInstall()
        {
            Log.Message("Combat Extended :: Checking Vehicle Framework");
            if (!ModLister.HasActiveModWithName("Vehicle Framework"))
            {
                return false;
            }
            return true;
        }
        public IEnumerable<string> GetCompatList()
        {
            yield return "VehiclesCompat";
        }


        public void Install()
        {
            Log.Message("Combat Extended :: Installing Vehicle Framework");
            active = true;
        }


        public static bool GetCollisionBodyFactors(Pawn pawn, out Vector2 ret)
        {
            ret = new Vector2();
            if (active)
            {
                foreach (Func<Pawn, Tuple<bool, Vector2>> f in CollisionBodyFactorCallbacks)
                {
                    var r = f(pawn);
                    if (r.Item1)
                    {
                        ret = r.Item2;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
