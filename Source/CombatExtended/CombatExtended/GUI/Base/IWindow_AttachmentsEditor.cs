using System;
using CombatExtended.RocketGUI;
using UnityEngine;
using Verse;
using GUIUtility = CombatExtended.RocketGUI.GUIUtility;
namespace CombatExtended
{
    public abstract class IWindow_AttachmentsEditor : Window
    {       
        public readonly Map map;
        public readonly WeaponPlatformDef weaponDef;
        public readonly WeaponPlatform weapon;

        public Pawn HolderPawn
        {
            get
            {
                if (weapon.ParentHolder is Pawn pawn)
                    return pawn;
                return null;
            }
        }

        public override Vector2 InitialSize => new Vector2(800, 600);

        public IWindow_AttachmentsEditor(WeaponPlatform weapon, Map map)
        {
            this.map = map;
            this.resizer = new WindowResizer();
            this.forcePause = true;
            this.layer = WindowLayer.Dialog;
            this.doCloseButton = false;
            this.doCloseX = false;
            this.weapon = weapon;
            this.weaponDef = (WeaponPlatformDef)weapon.def;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Exception error = null;
            try
            {
                GUIUtility.StashGUIState();
                DoContent(inRect);
            }
            catch (Exception er)
            {
                error = er;
            }
            finally
            {
                GUIUtility.RestoreGUIState();
                if (error != null)
                    throw error;
            }
        }

        protected abstract void DoContent(Rect inRect);
    }
}
