using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;

namespace CombatExtended
{
    public class Command_Reload : Command_Action
    {
        public CompAmmoUser compAmmo;

        public override void ProcessInput(Event ev)
        {
            if (compAmmo == null)
            {
                Log.Error("Command_Reload without ammo comp");
                return;
            }
            if (((ev.button == 1 || !ModSettings.rightClickAmmoSelect) 
                && compAmmo.useAmmo 
                && (compAmmo.compInventory != null || compAmmo.turret != null))
                || action == null)
            {
                Find.WindowStack.Add(MakeAmmoMenu());
            }
            else if (compAmmo.selectedAmmo != compAmmo.currentAmmo || compAmmo.curMagCount < compAmmo.Props.magazineSize)
            {
                base.ProcessInput(ev);
            }
            // Show we learned something by clicking this
            if (!tutorTag.NullOrEmpty()) PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDef.Named(tutorTag), KnowledgeAmount.Total);
        }

        private FloatMenu MakeAmmoMenu()
        {
            List<ThingDef> ammoList = new List<ThingDef>();      // List of all ammo types the gun can use and the pawn has in his inventory
            if (compAmmo.turret != null)
            {
                // If we have no inventory available (e.g. manned turret), add all possible ammo types to the selection
                foreach(AmmoLink link in compAmmo.Props.ammoSet.ammoTypes)
                {
                    ammoList.Add(link.ammo);
                }
            }
            else
            {
                // Iterate through all suitable ammo types and check if they're in our inventory
                foreach (AmmoLink curLink in compAmmo.Props.ammoSet.ammoTypes)
                {
                    if (compAmmo.compInventory.ammoList.Any(x => x.def == curLink.ammo))
                        ammoList.Add(curLink.ammo);
                }
            }

            // Append float menu options for every available ammo type
            List<FloatMenuOption> floatOptionList = new List<FloatMenuOption>();
            if (ammoList.NullOrEmpty())
            {
                floatOptionList.Add(new FloatMenuOption("CE_OutOfAmmo".Translate(), null));
            }
            else
            {
                // Append all available ammo types
                foreach (ThingDef curDef in ammoList)
                {
                    AmmoDef ammoDef = (AmmoDef)curDef;
                    floatOptionList.Add(new FloatMenuOption(ammoDef.ammoClass.LabelCap, new Action(delegate {
                        bool shouldReload = ModSettings.autoReloadOnChangeAmmo && (compAmmo.selectedAmmo != ammoDef || compAmmo.curMagCount < compAmmo.Props.magazineSize);
		               	compAmmo.selectedAmmo = ammoDef;
		               	if (shouldReload)
		               	{
			               	if (compAmmo.turret != null)
			               	{
			               		compAmmo.turret.OrderReload();
			               	}
			               	else
			               	{
			               		compAmmo.TryStartReload();
			               	}
		               	}
	               	})));
                }
            }
            // Append unload command
            if (compAmmo.useAmmo && compAmmo.wielder != null && compAmmo.hasMagazine && compAmmo.curMagCount > 0)
            {
                floatOptionList.Add(new FloatMenuOption("CE_UnloadLabel".Translate(), new Action(delegate { compAmmo.TryUnload(); })));
            }
            // Append reload command
            if (compAmmo.hasMagazine && !ModSettings.rightClickAmmoSelect)
            {
                floatOptionList.Add(new FloatMenuOption("CE_ReloadLabel".Translate(), new Action(action)));
            }
            return new FloatMenu(floatOptionList);
        }
    }
}
