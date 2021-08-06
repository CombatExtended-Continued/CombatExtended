using System;
using CombatExtended.RocketGUI;
using UnityEngine;
using Verse;
using GUIUtility = CombatExtended.RocketGUI.GUIUtility;
namespace CombatExtended
{
    public abstract class IWindow_AttachmentsEditor : Window
    {
        private const int PANEL_RIGHT_WIDTH = 250;
        private const int PANEL_ACTION_HEIGHT = 50;
        private const int PANEL_INNER_MARGINS = 4;

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
                if (weapon?.Destroyed ?? true)
                {
                    Close(doCloseSound: true);
                    return;
                }
                Rect leftRect = inRect;
                leftRect.xMax -= PANEL_RIGHT_WIDTH - 5;
                Rect rightRect = inRect.RightPartPixels(PANEL_RIGHT_WIDTH);
                Rect actionRect = rightRect.BottomPartPixels(PANEL_ACTION_HEIGHT);
                rightRect.yMax -= PANEL_ACTION_HEIGHT - 5;

                rightRect.xMin += PANEL_INNER_MARGINS;
                rightRect.yMin += PANEL_INNER_MARGINS;
                rightRect.xMax -= PANEL_INNER_MARGINS;
                rightRect.yMax -= PANEL_INNER_MARGINS;
                Widgets.DrawMenuSection(rightRect);
                GUIUtility.ExecuteSafeGUIAction(() =>
                {
                    this.DoRightPanel(rightRect.ContractedBy(PANEL_INNER_MARGINS));
                });

                leftRect.xMin += PANEL_INNER_MARGINS;
                leftRect.yMin += PANEL_INNER_MARGINS;
                leftRect.xMax -= PANEL_INNER_MARGINS;
                leftRect.yMax -= PANEL_INNER_MARGINS;
                Widgets.DrawMenuSection(leftRect);
                GUIUtility.ExecuteSafeGUIAction(() =>
                {
                    this.DoLeftPanel(leftRect);
                });

                actionRect.xMin += PANEL_INNER_MARGINS;
                actionRect.yMin += PANEL_INNER_MARGINS;
                actionRect.xMax -= PANEL_INNER_MARGINS;
                actionRect.yMax -= PANEL_INNER_MARGINS;
                GUIUtility.ExecuteSafeGUIAction(() =>
                {
                    this.DoActionPanel(actionRect);
                });
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

        protected abstract void DoRightPanel(Rect inRect);

        protected abstract void DoLeftPanel(Rect inRect);

        protected abstract void DoActionPanel(Rect inRect);
    }
}
