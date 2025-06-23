using RimWorld;
using Verse;
using UnityEngine;
using CombatExtended.Loader;
using System.Collections.Generic;

namespace CombatExtended
{
    public class Settings : ModSettings, ISettingsCE
    {
        #region Settings

        // General settings
        private bool bipodMechanics = true;
        private bool autosetup = true;
        private bool showCasings = true;
        private bool createCasingsFilth = true;
        private bool recoilAnim = true;
        private bool showTaunts = true;
        private bool allowMeleeHunting = false;
        private bool smokeEffects = true;
        private bool mergeExplosions = true;
        private bool turretsBreakShields = true;
        private bool showBackpacks = true;
        private bool showTacticalVests = true;
        private bool genericammo = false;
        private bool partialstats = true;
        private bool enableExtraEffects = true;

        private bool enableArcOfFire = false;

        private bool enableCIWS = false;

        private bool showExtraTooltips = false;

        private bool showExtraStats = false;

        private bool fragmentsFromWalls = false;

        private bool midBurstRetarget = true;
        private bool fasterRepeatShots = true;

        private float explosionPenMultiplier = 1.0f;
        private float explosionFalloffFactor = 1.0f;

        private float medicineSearchRadius = 5f;

        public bool ShowCasings => showCasings;

        public bool BipodMechanics => bipodMechanics;

        public bool GenericAmmo => genericammo;
        public bool AutoSetUp => autosetup;
        public bool ShowTaunts => showTaunts;
        public bool AllowMeleeHunting => allowMeleeHunting;
        public bool SmokeEffects => smokeEffects;
        public bool MergeExplosions => mergeExplosions;
        public bool TurretsBreakShields => turretsBreakShields;
        public bool ShowBackpacks => showBackpacks;
        public bool ShowTacticalVests => showTacticalVests;

        public bool EnableArcOfFire => enableArcOfFire;

        public bool PartialStat => partialstats;
        public bool EnableExtraEffects => enableExtraEffects;
        public bool ShowExtraTooltips => showExtraTooltips;

        public bool ShowExtraStats => showExtraStats;
        public bool EnableCIWS => enableCIWS;

        public float MedicineSearchRadiusSquared => medicineSearchRadius * medicineSearchRadius;

        public bool ShowTutorialPopup = true;

        // Ammo settings
        private bool enableAmmoSystem = true;
        private bool rightClickAmmoSelect = true;
        private bool autoReloadOnChangeAmmo = true;
        private bool autoTakeAmmo = true;
        private bool showCaliberOnGuns = true;
        private bool reuseNeolithicProjectiles = true;
        private bool realisticCookOff = true;
        private bool stuckArrowsAsFlecks = true;

        public bool EnableAmmoSystem => enableAmmoSystem;
        public bool RightClickAmmoSelect => rightClickAmmoSelect;
        public bool AutoReloadOnChangeAmmo => autoReloadOnChangeAmmo;
        public bool AutoTakeAmmo => autoTakeAmmo;
        public bool ShowCaliberOnGuns => showCaliberOnGuns;
        public bool ReuseNeolithicProjectiles => reuseNeolithicProjectiles;
        public bool RealisticCookOff => realisticCookOff;
        public bool StuckArrowsAsFlecks => stuckArrowsAsFlecks;

        // Debug settings - make sure all of these default to false for the release build
        private bool debuggingMode = false;
        private bool debugVerbose = false;
        private bool debugDrawPartialLoSChecks = false;
        private bool debugEnableInventoryValidation = false;
        private bool debugDrawTargetCoverChecks = false;
        private bool debugGenClosetPawn = false;
        private bool debugMuzzleFlash = false;
        private bool debugShowTreeCollisionChance = false;
        private bool debugShowSuppressionBuildup = false;
        private bool debugDrawInterceptChecks = false;
        private bool debugDisplayDangerBuildup = false;
        private bool debugDisplayCellCoverRating = false;
        private bool debugDisplayAttritionInfo = false;

        public bool DebuggingMode => debuggingMode;
        public bool DebugVerbose => debugVerbose;
        public bool DebugDrawInterceptChecks => debugDrawInterceptChecks && debuggingMode;
        public bool DebugDrawPartialLoSChecks => debugDrawPartialLoSChecks && debuggingMode;
        public bool DebugEnableInventoryValidation => debugEnableInventoryValidation && debuggingMode;
        public bool DebugDrawTargetCoverChecks => debugDrawTargetCoverChecks && debuggingMode;
        public bool DebugMuzzleFlash => debugMuzzleFlash && debuggingMode;
        public bool DebugShowTreeCollisionChance => debugShowTreeCollisionChance && debuggingMode;
        public bool DebugShowSuppressionBuildup => debugShowSuppressionBuildup && debuggingMode;
        public bool DebugGenClosetPawn => debugGenClosetPawn && debuggingMode;
        public bool DebugDisplayDangerBuildup => debugDisplayDangerBuildup && debuggingMode;
        public bool DebugDisplayCellCoverRating => debugDisplayCellCoverRating && debuggingMode;
        public bool DebugDisplayAttritionInfo => debugDisplayAttritionInfo && debuggingMode;
        #endregion

        #region Autopatcher
        private bool debugAutopatcherLogger = false;

        public bool DebugAutopatcherLogger => debugAutopatcherLogger;

        private bool enableApparelAutopatcher = false;
        private bool enableWeaponAutopatcher = false;
        private bool enableWeaponToughnessAutopatcher = true;
        private bool enableRaceAutopatcher = true;
        private bool enablePawnKindAutopatcher = true;

        public bool EnableApparelAutopatcher => enableApparelAutopatcher;
        public bool EnableWeaponAutopatcher => enableWeaponAutopatcher;
        public bool EnableWeaponToughnessAutopatcher => enableWeaponToughnessAutopatcher;
        public bool EnableRaceAutopatcher => enableRaceAutopatcher;
        public bool EnablePawnKindAutopatcher => enablePawnKindAutopatcher;

        public bool FragmentsFromWalls => fragmentsFromWalls;

        public bool FasterRepeatShots => fasterRepeatShots;
        public bool MidBurstRetarget => midBurstRetarget;

        public float ExplosionPenMultiplier => explosionPenMultiplier;
        public float ExplosionFalloffFactor => explosionFalloffFactor;

        public bool CreateCasingsFilth => createCasingsFilth;

        public bool RecoilAnim => recoilAnim;

        #endregion

        private bool lastAmmoSystemStatus;
        private readonly Dictionary<int, float> _cachedTabHeights = [];
        private readonly HashSet<int> _dirtyTabs = [];

        #region Compatibility Modsettings
        public bool patchArmorDamage = true;

        #endregion



        #region Methods

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref showCasings, "showCasings", true);
            Scribe_Values.Look(ref createCasingsFilth, "createCasingsFilth", true);
            Scribe_Values.Look(ref recoilAnim, "recoilAnim", true);
            Scribe_Values.Look(ref showTaunts, "showTaunts", true);
            Scribe_Values.Look(ref allowMeleeHunting, "allowMeleeHunting", false);
            Scribe_Values.Look(ref smokeEffects, "smokeEffects", true);
            Scribe_Values.Look(ref mergeExplosions, "mergeExplosions", true);
            Scribe_Values.Look(ref turretsBreakShields, "turretsBreakShields", true);
            Scribe_Values.Look(ref showBackpacks, "showBackpacks", true);
            Scribe_Values.Look(ref showTacticalVests, "showTacticalVests", true);
            Scribe_Values.Look(ref partialstats, "PartialArmor", true);
            Scribe_Values.Look(ref enableExtraEffects, "enableExtraEffects", true);
            Scribe_Values.Look(ref showExtraTooltips, "showExtraTooltips", false);
            Scribe_Values.Look(ref enableArcOfFire, "enableArcOfFire", false);

            Scribe_Values.Look(ref showExtraStats, "showExtraStats", false);


#if DEBUG
            // Debug settings
            Scribe_Values.Look(ref debuggingMode, "debuggingMode", false);
            Scribe_Values.Look(ref debugDrawInterceptChecks, "drawInterceptChecks", false);
            Scribe_Values.Look(ref debugDrawPartialLoSChecks, "drawPartialLoSChecks", false);
            Scribe_Values.Look(ref debugEnableInventoryValidation, "enableInventoryValidation", false);
            Scribe_Values.Look(ref debugDrawTargetCoverChecks, "debugDrawTargetCoverChecks", false);
            Scribe_Values.Look(ref debugShowTreeCollisionChance, "debugShowTreeCollisionChance", false);
            Scribe_Values.Look(ref debugShowSuppressionBuildup, "debugShowSuppressionBuildup", false);
            Scribe_Values.Look(ref debugDisplayDangerBuildup, "debugDisplayDangerBuildup", false);
            Scribe_Values.Look(ref debugDisplayCellCoverRating, "debugDisplayCellCoverRating", false);
            Scribe_Values.Look(ref debugDisplayAttritionInfo, "debugDisplayAttritionInfo", false);
#endif
            Scribe_Values.Look(ref debugAutopatcherLogger, "debugAutopatcherLogger", false);

            Scribe_Values.Look(ref enableWeaponAutopatcher, "enableWeaponAutopatcher", false);
            Scribe_Values.Look(ref enableWeaponToughnessAutopatcher, "enableWeaponToughnessAutopatcher", true);
            Scribe_Values.Look(ref enableApparelAutopatcher, "enableApparelAutopatcher", false);
            Scribe_Values.Look(ref enableRaceAutopatcher, "enableRaceAutopatcher", true);
            Scribe_Values.Look(ref enablePawnKindAutopatcher, "enablePawnKindAutopatcher", true);

            // Ammo settings
            Scribe_Values.Look(ref enableAmmoSystem, "enableAmmoSystem", true);
            Scribe_Values.Look(ref rightClickAmmoSelect, "rightClickAmmoSelect", true);
            Scribe_Values.Look(ref autoReloadOnChangeAmmo, "autoReloadOnChangeAmmo", true);
            Scribe_Values.Look(ref autoTakeAmmo, "autoTakeAmmo", true);
            Scribe_Values.Look(ref showCaliberOnGuns, "showCaliberOnGuns", true);
            Scribe_Values.Look(ref reuseNeolithicProjectiles, "reuseNeolithicProjectiles", true);
            Scribe_Values.Look(ref realisticCookOff, "realisticCookOff", true);
            Scribe_Values.Look(ref stuckArrowsAsFlecks, "stuckArrowsAsFlecks", true);
            Scribe_Values.Look(ref genericammo, "genericAmmo", false);

            Scribe_Values.Look(ref ShowTutorialPopup, "ShowTutorialPopup", true);

            //Bipod settings
            Scribe_Values.Look(ref bipodMechanics, "bipodMechs", true);
            Scribe_Values.Look(ref autosetup, "autosetup", true);

            Scribe_Values.Look(ref fragmentsFromWalls, "fragmentsFromWalls", false);
            Scribe_Values.Look(ref fasterRepeatShots, "fasterRepeatShots", false);
            Scribe_Values.Look(ref midBurstRetarget, "midBurstRetarget", true);
            Scribe_Values.Look(ref explosionPenMultiplier, "explosionPenMultiplier", 1.0f);
            Scribe_Values.Look(ref explosionFalloffFactor, "explosionFalloffFactor", 1.0f);

            //CIWS
            Scribe_Values.Look(ref enableCIWS, nameof(enableCIWS), true);
            lastAmmoSystemStatus = enableAmmoSystem;    // Store this now so we can monitor for changes

            Scribe_Values.Look(ref medicineSearchRadius, "medicineSearchRadius", 5f);
        }
        public void DoWindowContents(Listing_Standard list)
        {
            switch (Controller.SelectedTab)
            {
                case 0:
                    DoSettingsWindowContents_Mechanics(list);
                    break;
                case 1:
                    DoSettingsWindowContents_Ammo(list);
                    break;
                case 2:
                    DoSettingsWindowContents_Graphic(list);
                    break;
                case 3:
                    DoSettingsWindowContents_Misc(list);
                    break;

            }
        }

        private void DoSettingsWindowContents_Mechanics(Listing_Standard list)
        {

            Rect fullRect = list.GetRect(500f);
            const float columnPadding = 16f;
            float columnWidth = (fullRect.width - columnPadding) / 2f;

            Rect leftRect = new Rect(fullRect.x, fullRect.y, columnWidth, fullRect.height);
            Rect rightRect = new Rect(fullRect.x + columnWidth + columnPadding, fullRect.y, columnWidth, fullRect.height);

            // LEFT COLUMN
            Listing_Standard left = new Listing_Standard();
            left.Begin(leftRect);
            left.Label("CE_Settings_HeaderGeneral".Translate());
            left.Gap();
            left.CheckboxLabeled("CE_Settings_PartialStats_Title".Translate(), ref partialstats, "CE_Settings_PartialStats_Desc".Translate());
            left.CheckboxLabeled("CE_Settings_AllowMeleeHunting_Title".Translate(), ref allowMeleeHunting, "CE_Settings_AllowMeleeHunting_Desc".Translate());
            left.CheckboxLabeled("CE_Settings_SmokeEffects_Title".Translate(), ref smokeEffects, "CE_Settings_SmokeEffects_Desc".Translate());
            left.CheckboxLabeled("CE_Settings_TurretsBreakShields_Title".Translate(), ref turretsBreakShields, "CE_Settings_TurretsBreakShields_Desc".Translate());
            left.CheckboxLabeled("CE_Settings_FasterRepeatShots_Title".Translate(), ref fasterRepeatShots, "CE_Settings_FasterRepeatShots_Desc".Translate());
            left.CheckboxLabeled("CE_Settings_MidBurstRetarget_Title".Translate(), ref midBurstRetarget, "CE_Settings_MidBurstRetarget_Desc".Translate());
            left.CheckboxLabeled("CE_Settings_EnableArcOfFire_Title".Translate(), ref enableArcOfFire, "CE_Settings_EnableArcOfFire_Desc".Translate());
            left.CheckboxLabeled("CE_Settings_EnableCIWS".Translate(), ref enableCIWS, "CE_Settings_EnableCIWS_Desc".Translate());
            left.CheckboxLabeled("CE_Settings_FragmentsFromWalls_Title".Translate(), ref fragmentsFromWalls, "CE_Settings_FragmentsFromWalls_Desc".Translate());
            left.GapLine();
            left.Label("CE_Settings_BipodSettings".Translate());
            left.Gap();
            left.CheckboxLabeled("CE_Settings_BipodMechanics_Title".Translate(), ref bipodMechanics, "CE_Settings_BipodMechanics_Desc".Translate());
            left.CheckboxLabeled("CE_Settings_BipodAutoSetUp_Title".Translate(), ref autosetup, "CE_Settings_BipodAutoSetUp_Desc".Translate());
            left.End();

            // RIGHT COLUMN
            Listing_Standard right = new Listing_Standard();
            right.Begin(rightRect);
            right.Label("CE_Settings_ExplosionSettings".Translate());
            right.Gap();
            right.CheckboxLabeled("CE_Settings_MergeExplosions_Title".Translate(), ref mergeExplosions, "CE_Settings_MergeExplosions_Desc".Translate());
            explosionPenMultiplier = Mathf.Round(right.SliderLabeled("CE_Settings_ExplosionPenMultiplier_Title".Translate() + ": " + explosionPenMultiplier.ToString("F1"), explosionPenMultiplier, 0.1f, 10f, tooltip: "CE_Settings_ExplosionPenMultiplier_Desc".Translate(), labelPct: 0.6f) * 10f) * 0.1f;
            explosionFalloffFactor = Mathf.Round(right.SliderLabeled("CE_Settings_ExplosionDamageFalloffFactor_Title".Translate() + ": " + explosionFalloffFactor.ToString("F1"), explosionFalloffFactor, 0.1f, 2f, tooltip: "CE_Settings_ExplosionDamageFalloffFactor_Desc".Translate(), labelPct: 0.6f) * 10f) * 0.1f;
            right.GapLine();
            right.Label("CE_Settings_StabilizationSettings".Translate());
            right.Gap();
            medicineSearchRadius = right.SliderLabeled("CE_Settings_MedicineSearchRadius_Title".Translate() + ": " + medicineSearchRadius.ToString("F0"), medicineSearchRadius, 1f, 100f, tooltip: "CE_Settings_MedicineSearchRadius_Desc".Translate(), labelPct: 0.6f);
            right.End();
        }

        private void DoSettingsWindowContents_Ammo(Listing_Standard list)
        {
            list.Label("CE_Settings_HeaderAmmo".Translate());
            list.Gap();
            list.CheckboxLabeled("CE_Settings_EnableAmmoSystem_Title".Translate(), ref enableAmmoSystem, "CE_Settings_EnableAmmoSystem_Desc".Translate());
            list.GapLine();
            if (enableAmmoSystem)
            {
                list.CheckboxLabeled("CE_Settings_RightClickAmmoSelect_Title".Translate(), ref rightClickAmmoSelect, "CE_Settings_RightClickAmmoSelect_Desc".Translate());
                list.CheckboxLabeled("CE_Settings_AutoReloadOnChangeAmmo_Title".Translate(), ref autoReloadOnChangeAmmo, "CE_Settings_AutoReloadOnChangeAmmo_Desc".Translate());
                list.CheckboxLabeled("CE_Settings_AutoTakeAmmo_Title".Translate(), ref autoTakeAmmo, "CE_Settings_AutoTakeAmmo_Desc".Translate());
                list.CheckboxLabeled("CE_Settings_ShowCaliberOnGuns_Title".Translate(), ref showCaliberOnGuns, "CE_Settings_ShowCaliberOnGuns_Desc".Translate());
                list.CheckboxLabeled("CE_Settings_ReuseNeolithicProjectiles_Title".Translate(), ref reuseNeolithicProjectiles, "CE_Settings_ReuseNeolithicProjectiles_Desc".Translate());
                list.CheckboxLabeled("CE_Settings_RealisticCookOff_Title".Translate(), ref realisticCookOff, "CE_Settings_RealisticCookOff_Desc".Translate());
                list.CheckboxLabeled("CE_Settings_GenericAmmo".Translate(), ref genericammo, "CE_Settings_GenericAmmo_Desc".Translate());
            }
            else
            {
                GUI.contentColor = Color.gray;
                list.Label("CE_Settings_RightClickAmmoSelect_Title".Translate());
                list.Label("CE_Settings_AutoReloadOnChangeAmmo_Title".Translate());
                list.Label("CE_Settings_AutoTakeAmmo_Title".Translate());
                list.Label("CE_Settings_ShowCaliberOnGuns_Title".Translate());
                list.Label("CE_Settings_ReuseNeolithicProjectiles_Title".Translate());
                list.Label("CE_Settings_RealisticCookOff_Title".Translate());

                GUI.contentColor = Color.white;
            }
            LastAmmoSystemStatusChanged();
        }

        private void DoSettingsWindowContents_Graphic(Listing_Standard list)
        {
            list.Label("CE_Settings_HeaderGraphic".Translate());
            list.Gap();

            list.CheckboxLabeled("CE_Settings_ShowCasings_Title".Translate(), ref showCasings, "CE_Settings_ShowCasings_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_CreateCasingsFilth_Title".Translate(), ref createCasingsFilth, "CE_Settings_CreateCasingsFilth_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_StuckArrowsAsFlecks_Title".Translate(), ref stuckArrowsAsFlecks, "CE_Settings_StuckArrowsAsFlecks_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_RecoilAnim_Title".Translate(), ref recoilAnim, "CE_Settings_RecoilAnim_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_ShowTaunts_Title".Translate(), ref showTaunts, "CE_Settings_ShowTaunts_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_EnableExtraEffects_Title".Translate(), ref enableExtraEffects, "CE_Settings_EnableExtraEffects_Desc".Translate());

            list.CheckboxLabeled("CE_Settings_ShowBackpacks_Title".Translate(), ref showBackpacks, "CE_Settings_ShowBackpacks_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_ShowWebbing_Title".Translate(), ref showTacticalVests, "CE_Settings_ShowWebbing_Desc".Translate());
        }

        private void DoSettingsWindowContents_Misc(Listing_Standard list)
        {
            list.Label("CE_Settings_HeaderMisc".Translate());
            list.Gap();
            list.CheckboxLabeled("CE_Settings_ShowExtraTooltips_Title".Translate(), ref showExtraTooltips, "CE_Settings_ShowExtraTooltips_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_ShowExtraStats_Title".Translate(), ref showExtraStats, "CE_Settings_ShowExtraStats_Desc".Translate());
            list.Gap();
            list.GapLine();
            list.Gap();
            list.Label("CE_Settings_HeaderAutopatcher".Translate());
            list.Gap();
            list.CheckboxLabeled("CE_Settings_VerboseAutopatcher_Title".Translate(), ref debugAutopatcherLogger, "CE_Settings_VerboseAutopatcher_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_ApparelAutopatcher_Title".Translate(), ref enableApparelAutopatcher, "CE_Settings_ApparelAutopatcher_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_RaceAutopatcher_Title".Translate(), ref enableRaceAutopatcher, "CE_Settings_RaceAutopatcher_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_WeaponAutopatcher_Title".Translate(), ref enableWeaponAutopatcher, "CE_Settings_WeaponAutopatcher_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_ToughnessAutopatcher_Title".Translate(), ref enableWeaponToughnessAutopatcher, "CE_Settings_ToughnessAutopatcher_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_PawnkindAutopatcher_Title".Translate(), ref enablePawnKindAutopatcher, "CE_Settings_PawnkindAutopatcher_Desc".Translate());
#if DEBUG
            list.Gap();
            list.GapLine();
            list.Label("Debug");
            list.Gap();
            Text.Font = GameFont.Small;
            bool prevDebug = debuggingMode;
            list.CheckboxLabeled("Enable debugging", ref debuggingMode, "This will enable all debugging features.");
            if (prevDebug != debuggingMode)
            {
                _dirtyTabs.Add(Controller.SelectedTab);
            }
            if (debuggingMode)
            {
                list.GapLine();
                list.CheckboxLabeled("Verbose", ref debugVerbose, "Enable logging for internel states and many other things.");
                list.CheckboxLabeled("Display result of GenStep_Attrition", ref debugDisplayAttritionInfo);
                list.CheckboxLabeled("Draw intercept checks", ref debugDrawInterceptChecks, "Displays projectile checks for intercept.");
                list.CheckboxLabeled("Draw partial LoS checks", ref debugDrawPartialLoSChecks, "Displays line of sight checks against partial cover.");
                list.CheckboxLabeled("Draw debug things in range", ref debugGenClosetPawn);
                list.CheckboxLabeled("Draw target cover checks", ref debugDrawTargetCoverChecks, "Displays highest cover of target as it is selected.");
                list.CheckboxLabeled("Enable inventory validation", ref debugEnableInventoryValidation, "Inventory will refresh its cache every tick and log any discrepancies.");
                list.CheckboxLabeled("Display tree collision chances", ref debugShowTreeCollisionChance, "Projectiles will display chances of coliding with trees as they pass by.");
                list.CheckboxLabeled("Display suppression buildup", ref debugShowSuppressionBuildup, "Pawns will display buildup numbers when taking suppression.");
                list.CheckboxLabeled("Display light intensity affected by muzzle flash", ref debugMuzzleFlash);
                list.CheckboxLabeled("Display danger buildup within cells", ref debugDisplayDangerBuildup);
                list.CheckboxLabeled("Display cover rating of cells of suppressed pawns", ref debugDisplayCellCoverRating);
            }
#endif
            list.Gap();
            list.GapLine();
            list.Gap();
            for (int i = 0; i < Controller.otherSettingList.Count; i++)
            {
                var otherSetting = Controller.otherSettingList[i];
                if (otherSetting != Controller.settings)
                {
                    otherSetting.DoWindowContents(list);
                    if (i < Controller.settingList.Count - 1)
                    {
                        list.GapLine();
                        list.Gap();
                    }
                }
            }
        }

        private void LastAmmoSystemStatusChanged()
        {
            if (lastAmmoSystemStatus != enableAmmoSystem)
            {
                AmmoInjector.Inject();
                AmmoInjector.AddRemoveCaliberFromGunRecipes();  //Ensure the labels are _removed_ when the ammo system gets disabled
                lastAmmoSystemStatus = enableAmmoSystem;
                TutorUtility.DoModalDialogIfNotKnown(CE_ConceptDefOf.CE_AmmoSettings);
                _dirtyTabs.Add(Controller.SelectedTab);
            }
            else if (AmmoInjector.gunRecipesShowCaliber != showCaliberOnGuns)
            {
                AmmoInjector.AddRemoveCaliberFromGunRecipes();
            }
        }


        #region Reset To Defaults
        public void ResetToDefaults()
        {
            switch (Controller.SelectedTab)
            {
                case 0:
                    ResetToDefault_Mechanics();
                    break;
                case 1:
                    ResetToDefault_Ammo();
                    break;
                case 2:
                    ResetToDefault_Graphics();
                    break;
                case 3:
                    ResetToDefault_Misc();
                    break;
            }
            _dirtyTabs.Add(Controller.SelectedTab);
        }
        private void ResetToDefault_Mechanics()
        {
            partialstats = true;
            allowMeleeHunting = false;
            smokeEffects = true;
            turretsBreakShields = true;
            fasterRepeatShots = true;
            midBurstRetarget = true;
            enableCIWS = true;
            fragmentsFromWalls = false;
            enableArcOfFire = false;
            mergeExplosions = true;
            explosionPenMultiplier = 1.0f;
            explosionFalloffFactor = 1.0f;
            bipodMechanics = true;
            autosetup = true;
            medicineSearchRadius = 5f;
        }
        private void ResetToDefault_Ammo()
        {
            enableAmmoSystem = true;
            rightClickAmmoSelect = true;
            autoReloadOnChangeAmmo = true;
            autoTakeAmmo = true;
            showCaliberOnGuns = true;
            reuseNeolithicProjectiles = true;
            realisticCookOff = true;
            genericammo = false;
            LastAmmoSystemStatusChanged();

        }
        private void ResetToDefault_Graphics()
        {
            showCasings = true;
            createCasingsFilth = true;
            recoilAnim = true;
            showTaunts = true;
            enableExtraEffects = true;
            showBackpacks = true;
            showTacticalVests = true;
        }
        private void ResetToDefault_Misc()
        {
            // Extra Tooltips
            showExtraTooltips = false;
            showExtraStats = false;

            // AutoPatcher Settings
            debugAutopatcherLogger = false;
            enableApparelAutopatcher = false;
            enableWeaponAutopatcher = false;
            enableWeaponToughnessAutopatcher = true;
            enableRaceAutopatcher = true;
            enablePawnKindAutopatcher = true;

#if DEBUG
            debuggingMode = false;
            debugDisplayAttritionInfo = false;
            debugDrawInterceptChecks = false;
            debugDrawPartialLoSChecks = false;
            debugGenClosetPawn = false;
            debugDrawTargetCoverChecks = false;
            debugEnableInventoryValidation = false;
            debugShowTreeCollisionChance = false;
            debugShowSuppressionBuildup = false;
            debugMuzzleFlash = false;
            debugDisplayDangerBuildup = false;
            debugDisplayCellCoverRating = false;
#endif
        }
        #endregion

        public float GetOrCalculateHeightForTab(int tabIndex, float width)
        {
            if (!_cachedTabHeights.TryGetValue(tabIndex, out float height) || _dirtyTabs.Contains(tabIndex))
            {
                Listing_Standard measuringList = new Listing_Standard();
                Rect dummyRect = new Rect(0f, 0f, width, 99999f);
                measuringList.Begin(dummyRect);
                switch (tabIndex)
                {
                    case 0:
                        measuringList.maxOneColumn = false;
                        measuringList.hasCustomColumnWidth = true;
                        measuringList.columnWidthInt = (int)(width / 2f - 10f);
                        DoSettingsWindowContents_Mechanics(measuringList);
                        break;
                    case 1: DoSettingsWindowContents_Ammo(measuringList); break;
                    case 2: DoSettingsWindowContents_Graphic(measuringList); break;
                    case 3: DoSettingsWindowContents_Misc(measuringList); break;
                }
                measuringList.End();
                height = measuringList.CurHeight + 10f;
                if (tabIndex == 0)
                {
                    height = measuringList.curY + 10f;
                }
                _cachedTabHeights[tabIndex] = height;
                _dirtyTabs.Remove(tabIndex);
            }
            return height;
        }

        #endregion
    }

}
