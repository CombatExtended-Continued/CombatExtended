using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using CombatExtended.Harmony;

namespace CombatExtended
{
    public class Controller : Mod
    {
        public static Settings settings;

        public Controller(ModContentPack content) : base(content)
        {
            settings = GetSettings<Settings>();

            // Apply Harmony patches
            HarmonyBase.InitPatches();

            // Inject ammo
            LongEventHandler.QueueLongEvent(AmmoInjector.Inject, "LibraryStartup", false, null);

            Log.Message("Combat Extended :: initialized");
        }

        public override string SettingsCategory()
        {
            return "Combat Extended";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoWindowContents(inRect);
        }
    }
}
