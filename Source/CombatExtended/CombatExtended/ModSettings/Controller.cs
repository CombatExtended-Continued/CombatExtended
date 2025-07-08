using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using System.Reflection;
using CombatExtended.HarmonyCE;
using CombatExtended.Compatibility;
using CombatExtended.Loader;
using Verse.Sound;

namespace CombatExtended;
/*
  This class handles initializing the different components of CombatExtended.  The LoadFolders
  system is responsible for loading the assemblies, this gives an opportunity to run any
  post-load sanity checking, and is responsible for rendering any sub-mod settings as required.
*/
public class Controller : Mod
{
    public static List<ISettingsCE> settingList = new List<ISettingsCE>();
    public static List<ISettingsCE> otherSettingList = new List<ISettingsCE>();
    public static Settings settings;
    public static Controller instance;
    public static ModContentPack content;
    private static bool genericState;
    private static Patches patches;
    private Vector2 scrollPosition;
    private const float ResetButtonWidth = 160f;
    private const float ResetButtonHeight = 40f;
    private static TaggedString[] s_tabNames;
    private static List<TabRecord> s_tabs;
    public static int SelectedTab;

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Rect windowRect = new Rect(inRect.x, inRect.y + 30f, inRect.width, inRect.height - 40f);
        Widgets.DrawMenuSection(windowRect);
        TabDrawer.DrawTabs(windowRect, s_tabs, 200f);
        Rect scrollRect = windowRect.ContractedBy(10f);
        float contentHeight = settings.GetOrCalculateHeightForTab(Controller.SelectedTab, scrollRect.width - 16f);
        Rect inner = new Rect(0f, 0f, scrollRect.width - 16f, contentHeight);
        Widgets.BeginScrollView(scrollRect, ref this.scrollPosition, inner, true);
        Listing_Standard list = new Listing_Standard();
        list.Begin(inner);
        settings.DoWindowContents(list);
        list.End();
        Widgets.EndScrollView();
        Rect resetButtonRect = new Rect(inRect.xMin + 200f, inRect.y + inRect.height + 5f, ResetButtonWidth, ResetButtonHeight);
        if (Widgets.ButtonText(resetButtonRect, "CE_ResetToDefault".Translate()))
        {
            settings.ResetToDefaults();
            SoundDefOf.Click.PlayOneShotOnCamera();
        }

    }

    public Controller(ModContentPack content) : base(content)
    {
        var author = content.ModMetaData.Authors.First();
        var repo = content.ModMetaData.Url;
        var version = content.ModMetaData.ModVersion;
        var commit = VersionInfo.COMMIT;
        Log.Message($"Loading Combat Extended {version} ({commit}):\nPlease report issues with CE to {author} at {repo}");
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
                otherSettingList.Add(settings);
            }

            modPart.PostLoad(content, settings);
        }
        LongEventHandler.QueueLongEvent(InitializeTabs, "CE_LongEvent_CE_InitTabs", true, null);

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

        genericState = settings.GenericAmmo;


    }


    private void InitializeTabs()
    {
        s_tabNames =
        [
            "CE_Settings_HeaderMechanics".Translate(),
            "CE_Settings_HeaderAmmo".Translate(),
            "CE_Settings_HeaderGraphic".Translate(),
            "CE_Settings_HeaderMisc".Translate()
        ];
        s_tabs = [];
        for (int i = 0; i < s_tabNames.Length; i++)
        {
            int capturedIndex = i;
            s_tabs.Add(new TabRecord(s_tabNames[i], delegate
            {
                Controller.SelectedTab = capturedIndex;
            }, () => Controller.SelectedTab == capturedIndex));
        }
    }

    public override string SettingsCategory()
    {
        return "Combat Extended";
    }

    public override void WriteSettings()
    {
        base.WriteSettings();
        if (settings.GenericAmmo != genericState)
        {
            GenericRestartPopup();
        }
    }

    private static void GenericRestartPopup()
    {
        var acceptAction = new Action(() =>
        {
            GenCommandLine.Restart();
        });
        var dialog = new Dialog_MessageBox("CE_Settings_GenericRestartPopup".Translate(), "CE_Settings_AcceptRestart".Translate(), acceptAction, "CE_Settings_DeclineRestart".Translate(), null, "CE_Settings_RestartTitle".Translate(), true);
        Find.WindowStack.Add(dialog);
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
