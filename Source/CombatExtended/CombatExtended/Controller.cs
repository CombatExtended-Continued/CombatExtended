using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using System.Reflection;
using System.IO;
using CombatExtended.HarmonyCE;
using CombatExtended.Compatibility;
using CombatExtended.Loader;

namespace CombatExtended
{
    /*
      This class handles initializing the different components of CombatExtended.  The LoadFolders
      system is responsible for loading the assemblies, this gives an opportunity to run any
      post-load sanity checking, and is responsible for rendering any sub-mod settings as required.
    */
    public class Controller : Mod
    {
        public static List<ISettingsCE> settingList = new List<ISettingsCE>();
        public static Settings settings;
        public static Controller instance;
        public static ModContentPack content;
        private static Patches patches;
        private Vector2 scrollPosition;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect inner = inRect.ContractedBy(20f);
            inner.height = 800f;
            inner.x += 10f;
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, inner, true);
            Listing_Standard list = new Listing_Standard();
            list.ColumnWidth = (inner.width - 17) / 2; // Subtract 17 for gap between columns
            list.Begin(inner);

            foreach (ISettingsCE settings in settingList)
            {
                settings.DoWindowContents(list);
            }
            list.End();
            Widgets.EndScrollView();
        }

        public Controller(ModContentPack content) : base(content)
        {
            patches = new Patches();
            Controller.instance = this;
            Controller.content = content;
            Controller.settings = GetSettings<Settings>();
            settingList.Add(Controller.settings);
            PostLoad();
        }

        public void PostLoad()
        {
            // Apply Harmony patches
            HarmonyBase.InitPatches();

            Queue<Assembly> toProcess = new Queue<Assembly>(content.assemblies.loadedAssemblies);
            List<IModPart> modParts = new List<IModPart>();
            while (toProcess.Any())
            {
                Assembly assembly = toProcess.Dequeue();

                foreach (Type t in assembly.GetTypes().Where(x => typeof(IModPart).IsAssignableFrom(x) && !x.IsAbstract))
                {
                    IModPart imp = ((IModPart)t.GetConstructor(new Type[] { }).Invoke(new object[] { }));
                    modParts.Add(imp);
                }
            }
            foreach (IModPart modPart in modParts)
            {
                Log.Message("CE: Loading Mod Part");
                Type settingsType = modPart.GetSettingsType();
                ISettingsCE settings = null;
                if (settingsType != null)
                {
                    if (typeof(ModSettings).IsAssignableFrom(settingsType))
                    {
                        settings = (ISettingsCE)typeof(Controller).GetMethod(nameof(Controller.GetSettings)).MakeGenericMethod(settingsType).Invoke(instance, null);
                    }
                    else
                    {
                        settings = (ISettingsCE)settingsType.GetConstructor(new Type[] { }).Invoke(new object[] { });
                    }
                    settingList.Add(settings);
                }

                modPart.PostLoad(content, settings);

            }

            // Initialize loadout generator
            LongEventHandler.QueueLongEvent(LoadoutPropertiesExtension.Reset, "CE_LongEvent_LoadoutProperties", false, null);

            // Inject ammo
            LongEventHandler.QueueLongEvent(AmmoInjector.Inject, "CE_LongEvent_AmmoInjector", false, null);

            // Inject pawn and plant bounds
            LongEventHandler.QueueLongEvent(BoundsInjector.Inject, "CE_LongEvent_BoundingBoxes", false, null);

            // Initialize the DefUtility (a caching system for common checks on defs)
            LongEventHandler.QueueLongEvent(DefUtility.Initialize, "CE_LongEvent_BoundingBoxes", false, null);

            Log.Message("Combat Extended :: initialized");

            // Tutorial popup
            if (Controller.settings.ShowTutorialPopup && !Prefs.AdaptiveTrainingEnabled)
            {
                LongEventHandler.QueueLongEvent(DoTutorialPopup, "CE_LongEvent_TutorialPopup", false, null);
            }

            LongEventHandler.QueueLongEvent(patches.Install, "CE_LongEvent_CompatibilityPatches", false, null);

        }

        public override string SettingsCategory()
        {
            return "Combat Extended";
        }

        private static void DoTutorialPopup()
        {
            var enableAction = new Action(() =>
            {
                Prefs.AdaptiveTrainingEnabled = true;
                settings.ShowTutorialPopup = false;
                settings.Write();
            });
            var disableAction = new Action(() =>
            {
                settings.ShowTutorialPopup = false;
                settings.Write();
            });

            var dialog = new Dialog_MessageBox("CE_EnableTutorText".Translate(), "CE_EnableTutorDisable".Translate(), disableAction, "CE_EnableTutorEnable".Translate(),
                                               enableAction, null, true);
            Find.WindowStack.Add(dialog);
        }

    }
}
