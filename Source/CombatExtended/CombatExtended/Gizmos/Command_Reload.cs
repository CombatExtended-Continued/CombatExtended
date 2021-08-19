using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using CombatExtended.Compatibility;

namespace CombatExtended
{
    public class Command_Reload : Command_Action
    {
        List<Command_Reload> others;
        public CompAmmoUser compAmmo;

        public override bool GroupsWith(Gizmo other)
        {
            var order = other as Command_Reload;
            return order != null;
        }

        public override void MergeWith(Gizmo other)
        {
            var order = other as Command_Reload;

            if (others == null)
            {
                others = new List<Command_Reload>();
                others.Add(this);
            }

            others.Add(order);
        }

        public override void ProcessInput(Event ev)
        {
            if (compAmmo == null)
            {
                Log.Error("Command_Reload without ammo comp");
                return;
            }

            if (compAmmo.UseAmmo && (compAmmo.CompInventory != null || compAmmo.turret != null) || action == null)
            {
                bool currentlyMannedTurret = compAmmo.turret?.GetMannable()?.MannedNow ?? false;
                if (Controller.settings.RightClickAmmoSelect && action != null && (compAmmo.turret == null || currentlyMannedTurret))
                {
                    base.ProcessInput(ev);
                }
                else
                {
                    Find.WindowStack.Add(MakeAmmoMenu());
                }
            }
            else if (compAmmo.SelectedAmmo != compAmmo.CurrentAmmo || compAmmo.CurMagCount < compAmmo.MagSize)
            {
                base.ProcessInput(ev);
            }

            // Show we learned something by clicking this
            if (!tutorTag.NullOrEmpty())
            {
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDef.Named(tutorTag), KnowledgeAmount.Total);
            }
        }

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                if (Controller.settings.RightClickAmmoSelect)
                {
                    foreach (var option in BuildAmmoOptions())
                    {
                        yield return option;
                    }
                }
            }
        }

        private FloatMenu MakeAmmoMenu()
        {
            return new FloatMenu(BuildAmmoOptions());
        }

        private List<FloatMenuOption> BuildAmmoOptions()
        {
            //Prepare list of others in case only a single gizmo is selected
            if (others == null)
                others = new List<Command_Reload>();
            if (!others.Contains(this))
                others.Add(this);

            // Append float menu options for every available ammo type
            List<FloatMenuOption> floatOptionList = new List<FloatMenuOption>();

            #region Ammo type switching
            if (compAmmo.UseAmmo)
            {
                //List of actions to be taken on choosing a new ammo type, listed by ammoCategoryDef (FMJ/AP/HP)
                Dictionary<AmmoCategoryDef, Action> ammoClassActions = new Dictionary<AmmoCategoryDef, Action>();

                //Index 0: amount which COULD have this ammoClass; Index 1: amount which CURRENTLY has this ammoClass
                Dictionary<AmmoCategoryDef, int[]> ammoClassAmounts = new Dictionary<AmmoCategoryDef, int[]>();

                //Whether ALL in OTHERS lack reloadable weapons
                var flag = false;

                foreach (var other in others)
                {
                    var user = other.compAmmo;

                    foreach (AmmoLink link in user.Props.ammoSet.ammoTypes)
                    {
                        var ammoDef = link.ammo;
                        var ammoClass = ammoDef.ammoClass;

                        // If we have no inventory available (e.g. manned turret), add all possible ammo types to the selection
                        // Otherwise, iterate through all suitable ammo types and check if they're in our inventory
                        if (user.CompInventory?.ammoList?.Any(x => x.def == ammoDef) ?? true)
                        {
                            if (!ammoClassAmounts.ContainsKey(ammoClass))
                                ammoClassAmounts.Add(ammoClass, new int[2]);

                            ammoClassAmounts[ammoClass][0]++;

                            Action del = null;

                            //Increase amount of current ammo of this type by 1
                            if (user.CurrentAmmo == ammoDef)
                                ammoClassAmounts[ammoClass][1]++;

                            if (user.SelectedAmmo == ammoDef)
                            {
                                if (Controller.settings.AutoReloadOnChangeAmmo && user.turret?.GetMannable() == null && user.CurMagCount < user.MagSize)
                                    del += other.action;
                            }
                            else
                            {
                                del += delegate { user.SelectedAmmo = ammoDef; };

                                if (Controller.settings.AutoReloadOnChangeAmmo && user.turret?.GetMannable() == null)
                                    del += other.action;
                            }

                            //Add to delegate or create delegate at ammoClass key
                            if (ammoClassActions.ContainsKey(ammoClass))
                                ammoClassActions[ammoClass] += del;
                            else
                                ammoClassActions.Add(ammoClass, del);

                            flag = true;
                        }
                    }
                }
                
                //At least one ammo type is available
                if (flag)
                {
                    foreach (var pair in ammoClassActions)
                    {
                        //Create entries of form "(a/b) c"
                        //a = number of guns currently using this ammo category
                        //b = number of guns that could use this ammo category
                        //c = name of category (FMJ/AP/HP/..)
                        floatOptionList.Add(new FloatMenuOption(
                                others.Except(this).Any() ?
                                "(" + ammoClassAmounts[pair.Key][1] + "/" + ammoClassAmounts[pair.Key][0] + ") " + pair.Key.LabelCap
                                : pair.Key.LabelCap
                            , pair.Value));
                    }
                }
                else    //Display when ALL OTHERS have no ammo
                    floatOptionList.Add(new FloatMenuOption("CE_OutOfAmmo".Translate(), null));
            }
            #endregion

            #region Unloading and reloading
            var unload = false;
            Action unloadDel = null;

            var reload = false;
            Action reloadDel = null;

            foreach (var other in others)
            {
                var user = other.compAmmo;
                if (user.HasMagazine && user.Wielder != null || (user.turret?.GetMannable()?.MannedNow ?? false))
                {
                    reload = true;
                    reloadDel += other.action;

                    if (user.UseAmmo && user.CurMagCount > 0)
                    {
                        unload = true;
                        unloadDel += delegate { user.TryUnload(true); };
                    }
                }
            }

            // Append unload delegates
            if (unload)
                floatOptionList.Add(new FloatMenuOption("CE_UnloadLabel".Translate(), unloadDel));

            // Append reload delegates
            if (reload)
                floatOptionList.Add(new FloatMenuOption("CE_ReloadLabel".Translate(), reloadDel));
            #endregion

            return floatOptionList;
        }

    }
}
