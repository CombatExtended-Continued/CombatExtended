using System;
using RimWorld;
using Verse;
using CombatExtended.RocketGUI;
using UnityEngine;
using GUIUtility = CombatExtended.RocketGUI.GUIUtility;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;

namespace CombatExtended
{
    public class Window_AttachmentsEditor : Window
    {
        private readonly WeaponPlatform weapon;
        private readonly WeaponPlatformDef weaponDef;
        private readonly Fragment_AttachmentEditor editor;

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1000, 675);
            }
        }        

        public Window_AttachmentsEditor(WeaponPlatform weapon)
        {
            this.weapon = weapon;
            this.weaponDef = weapon.Platform;            
            this.editor = new Fragment_AttachmentEditor(weapon);
            this.layer = WindowLayer.Dialog;
            this.resizer = new WindowResizer();
            this.forcePause = true;
            this.doCloseButton = false;
            this.doCloseX = false;
            this.draggable = true;
        }
     
        public override void DoWindowContents(Rect inRect)
        {
            GUIUtility.ExecuteSafeGUIAction(() =>
            {                
                Rect titleRect = inRect;
                Text.Font = GameFont.Medium;
                titleRect.xMin += 5;
                titleRect.height = 35;
                Widgets.DefIcon(titleRect.LeftPartPixels(titleRect.height).ContractedBy(2), weaponDef, scale: 1.7f);
                titleRect.xMin += titleRect.height + 10;
                Widgets.Label(titleRect, "CE_EditAttachments".Translate() + $": {weaponDef.label.CapitalizeFirst()}");
            });
            inRect.yMin += 40;
            Rect rect = inRect;
            rect.height = 550;
            editor.DoContents(rect);
            inRect.yMin += 550 + 5;           
            GUIUtility.ExecuteSafeGUIAction(() =>
            {
                Text.Font = GameFont.Small;
                Rect actionRect = inRect;
                actionRect.yMin += 5;
                actionRect.width = 300;
                actionRect = actionRect.CenteredOnXIn(inRect);                
                GUI.color = Color.red;
                if (Widgets.ButtonText(actionRect.RightPartPixels(146), "CE_Close".Translate()))
                {
                    this.Close();
                }
                GUI.color = Color.white;
                if (Widgets.ButtonText(actionRect.LeftPartPixels(146), "CE_Apply".Translate()))
                {
                    this.Apply();
                    this.Close();
                }
            });
        }

        /// <summary>
        /// Used to apply the current selections
        /// </summary>
        private void Apply()
        {            
            List<AttachmentLink> selected = editor.CurConfig;
            // Set the weapons config
            weapon.TargetConfig = selected.Select(l => l.attachment).ToList();
            weapon.UpdateConfiguration();
            // if god mode is on insta apply everything
            if (Prefs.DevMode && DebugSettings.godMode)
            {
                weapon.attachments.ClearAndDestroyContents();
                foreach (AttachmentLink link in selected)
                {
                    Thing attachment = ThingMaker.MakeThing(link.attachment);
                    weapon.attachments.TryAdd(attachment);
                }
                weapon.UpdateConfiguration();
            }
            if (weapon.Wielder != null)
            {
                Job job = WorkGiver_ModifyWeapon.TryGetModifyWeaponJob(weapon.Wielder, weapon);
                if (job != null)
                    weapon.Wielder.jobs.StartJob(job, JobCondition.InterruptOptional);
            }
        }
    }
}
