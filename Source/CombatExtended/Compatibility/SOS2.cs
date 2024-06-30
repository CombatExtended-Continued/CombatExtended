﻿using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SaveOurShip2;
using UnityEngine;
using RimWorld;
using Vehicles;
using Verse.Sound;

namespace CombatExtended.Compatibility
{
    public class SOS2 : IPatch
    {
        

        const string ModName = "Save Our Ship 2";
        bool IPatch.CanInstall()
        {
            Log.Message("Combat Extended :: Checking SOS2");
            if (!ModLister.HasActiveModWithName(ModName))
            {
                return false;
            }
            return true;
        }

        public void Install()
        {
            Log.Message("Combat Extended :: Installing SOS2");
        }
        public IEnumerable<string> GetCompatList()
        {
            yield return "SOS2Compat";
        }

        
    }
}
