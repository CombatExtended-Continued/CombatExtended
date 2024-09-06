using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using RimWorld.Planet;
using CombatExtended.Loader;

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

        private bool showExtraTooltips = false;

        private bool showExtraStats = false;

        private bool fragmentsFromWalls = false;

        private bool fasterRepeatShots = false;

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

        public bool PartialStat => partialstats;
        public bool EnableExtraEffects => enableExtraEffects;
        public bool ShowExtraTooltips => showExtraTooltips;

        public bool ShowExtraStats => showExtraStats;

        public bool ShowTutorialPopup = true;

        // Ammo settings
        private bool enableAmmoSystem = true;
        private bool rightClickAmmoSelect = true;
        private bool autoReloadOnChangeAmmo = true;
        private bool autoTakeAmmo = true;
        private bool showCaliberOnGuns = true;
        private bool reuseNeolithicProjectiles = true;
        private bool realisticCookOff = true;

        public bool EnableAmmoSystem => enableAmmoSystem;
        public bool RightClickAmmoSelect => rightClickAmmoSelect;
        public bool AutoReloadOnChangeAmmo => autoReloadOnChangeAmmo;
        public bool AutoTakeAmmo => autoTakeAmmo;
        public bool ShowCaliberOnGuns => showCaliberOnGuns;
        public bool ReuseNeolithicProjectiles => reuseNeolithicProjectiles;
        public bool RealisticCookOff => realisticCookOff;

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

        public bool CreateCasingsFilth => createCasingsFilth;

        public bool RecoilAnim => recoilAnim;

        #endregion

        private bool lastAmmoSystemStatus;

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
            Scribe_Values.Look(ref genericammo, "genericAmmo", false);

            Scribe_Values.Look(ref ShowTutorialPopup, "ShowTutorialPopup", true);

            //Bipod settings
            Scribe_Values.Look(ref bipodMechanics, "bipodMechs", true);
            Scribe_Values.Look(ref autosetup, "autosetup", true);

            Scribe_Values.Look(ref fragmentsFromWalls, "fragmentsFromWalls", false);
            Scribe_Values.Look(ref fasterRepeatShots, "fasterRepeatShots", false);

            lastAmmoSystemStatus = enableAmmoSystem;    // Store this now so we can monitor for changes
        }

        public void DoWindowContents(Listing_Standard list)
        {
            // Do general settings
            Text.Font = GameFont.Medium;
            list.Label("CE_Settings_HeaderGeneral".Translate());
            Text.Font = GameFont.Small;
            list.Gap();
            list.CheckboxLabeled("CE_Settings_PartialStats_Title".Translate(), ref partialstats, "CE_Settings_PartialStats_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_ShowCasings_Title".Translate(), ref showCasings, "CE_Settings_ShowCasings_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_СreateCasingsFilth_Title".Translate(), ref createCasingsFilth, "CE_Settings_СreateCasingsFilth_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_RecoilAnim_Title".Translate(), ref recoilAnim, "CE_Settings_RecoilAnim_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_ShowTaunts_Title".Translate(), ref showTaunts, "CE_Settings_ShowTaunts_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_AllowMeleeHunting_Title".Translate(), ref allowMeleeHunting, "CE_Settings_AllowMeleeHunting_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_SmokeEffects_Title".Translate(), ref smokeEffects, "CE_Settings_SmokeEffects_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_MergeExplosions_Title".Translate(), ref mergeExplosions, "CE_Settings_MergeExplosions_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_TurretsBreakShields_Title".Translate(), ref turretsBreakShields, "CE_Settings_TurretsBreakShields_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_ShowExtraTooltips_Title".Translate(), ref showExtraTooltips, "CE_Settings_ShowExtraTooltips_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_ShowExtraStats_Title".Translate(), ref showExtraStats, "CE_Settings_ShowExtraStats_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_FasterRepeatShots_Title".Translate(), ref fasterRepeatShots, "CE_Settings_FasterRepeatShots_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_EnableExtraEffects_Title".Translate(), ref enableExtraEffects, "CE_Settings_EnableExtraEffects_Desc".Translate());
            // Only Allow these settings to be changed in the main menu since doing while a
            // map is loaded will result in rendering issues.
            if (Current.Game == null)
            {
                list.CheckboxLabeled("CE_Settings_ShowBackpacks_Title".Translate(), ref showBackpacks, "CE_Settings_ShowBackpacks_Desc".Translate());
                list.CheckboxLabeled("CE_Settings_ShowWebbing_Title".Translate(), ref showTacticalVests, "CE_Settings_ShowWebbing_Desc".Translate());
                list.CheckboxLabeled("CE_Settings_FragmentsFromWalls_Title".Translate(), ref fragmentsFromWalls, "CE_Settings_FragmentsFromWalls_Desc".Translate());
            }
            else
            {
                // tell the user that he can only change these settings from main menu
                list.GapLine();
                Text.Font = GameFont.Medium;
                list.Label("CE_Settings_MainMenuOnly_Title".Translate(), tooltip: "CE_Settings_MainMenuOnly_Desc".Translate());
                Text.Font = GameFont.Small;

                list.Gap();
                list.Label("CE_Settings_ShowBackpacks_Title".Translate(), tooltip: "CE_Settings_ShowBackpacks_Desc".Translate());
                list.Label("CE_Settings_ShowWebbing_Title".Translate(), tooltip: "CE_Settings_ShowWebbing_Desc".Translate());
                list.Gap();
            }

            list.GapLine();
            Text.Font = GameFont.Medium;
            list.Label("CE_Settings_HeaderAutopatcher".Translate());
            Text.Font = GameFont.Small;
            list.Gap();

            list.CheckboxLabeled("CE_Settings_VerboseAutopatcher_Title".Translate(), ref debugAutopatcherLogger, "CE_Settings_VerboseAutopatcher_Desc".Translate());

            list.CheckboxLabeled("CE_Settings_ApparelAutopatcher_Title".Translate(), ref enableApparelAutopatcher, "CE_Settings_ApparelAutopatcher_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_RaceAutopatcher_Title".Translate(), ref enableRaceAutopatcher, "CE_Settings_RaceAutopatcher_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_WeaponAutopatcher_Title".Translate(), ref enableWeaponAutopatcher, "CE_Settings_WeaponAutopatcher_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_ToughnessAutopatcher_Title".Translate(), ref enableWeaponToughnessAutopatcher, "CE_Settings_ToughnessAutopatcher_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_PawnkindAutopatcher_Title".Translate(), ref enablePawnKindAutopatcher, "CE_Settings_PawnkindAutopatcher_Desc".Translate());

            // Do ammo settings
            list.NewColumn();

            Text.Font = GameFont.Medium;
            list.Label("CE_Settings_HeaderAmmo".Translate());
            Text.Font = GameFont.Small;
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
                list.CheckboxLabeled("CE_Settings_GenericAmmo".Translate(), ref genericammo, "CE_Settings_GenericAmmo_Desc".Translate()); ;
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

            Text.Font = GameFont.Medium;
            list.Label("CE_Settings_BipodSettings".Translate());
            Text.Font = GameFont.Small;
            list.CheckboxLabeled("CE_Settings_BipodMechanics_Title".Translate(), ref bipodMechanics, "CE_Settings_BipodMechanics_Desc".Translate());
            list.CheckboxLabeled("CE_Settings_BipodAutoSetUp_Title".Translate(), ref autosetup, "CE_Settings_BipodAutoSetUp_Desc".Translate());
#if DEBUG
            // Do Debug settings
            list.GapLine();
            Text.Font = GameFont.Medium;
            list.Label("Debug");
            list.Gap();
            Text.Font = GameFont.Small;
            list.CheckboxLabeled("Enable debugging", ref debuggingMode, "This will enable all debugging features.");
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
            //else
            //{
            //    list.Gap();
            //}
#endif

            // Update ammo if setting changes
            if (lastAmmoSystemStatus != enableAmmoSystem)
            {
                AmmoInjector.Inject();
                AmmoInjector.AddRemoveCaliberFromGunRecipes();  //Ensure the labels are _removed_ when the ammo system gets disabled
                lastAmmoSystemStatus = enableAmmoSystem;
                TutorUtility.DoModalDialogIfNotKnown(CE_ConceptDefOf.CE_AmmoSettings);
            }
            else if (AmmoInjector.gunRecipesShowCaliber != showCaliberOnGuns)
            {
                AmmoInjector.AddRemoveCaliberFromGunRecipes();
            }
        }

        #endregion
    }
}
