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
    public class SRTS : IPatch
    {
        public bool CanInstall()
        {
            Log.Message("Combat Extended :: Checking SRTS Expanded");
            if (!ModLister.HasActiveModWithName("SRTS Expanded"))
            {
                return false;
            }
            return true;
        }
        public void Install()
        {
            Log.Message("Combat Extended :: Installing SRTS Expanded");
        }
    }
}
