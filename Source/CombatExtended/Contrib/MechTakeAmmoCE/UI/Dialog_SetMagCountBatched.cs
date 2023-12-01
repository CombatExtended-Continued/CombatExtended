using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;
using UnityEngine;
using CombatExtended;
using CombatExtended.Compatibility;


namespace CombatExtended
{
    public class Dialog_SetMagCountBatched : Window
    {
        private readonly List<CompMechAmmo> _mechAmmoList;
        private readonly Dictionary<AmmoDef, int> _tmpLoadouts;
        private readonly List<AmmoLink> _mechAmmoTypes;
        private readonly int _mechMagazineSize;

        private const float BotAreaWidth = 30f;
        private const float BotAreaHeight = 30f;
        private new const float Margin = 10f;

        float headerHeight;
        private string Maglabel;

        public Dialog_SetMagCountBatched(List<CompMechAmmo> mechAmmoList)
        {
            if (mechAmmoList == null || mechAmmoList.Count == 0)
            {
                Log.Error("null or empty CompMechAmmo list for Dialog_SetMagCount");
                _mechAmmoList = new List<CompMechAmmo>(); // Prevent potential issues later on, just in case
                return;
            }
            _mechAmmoList = mechAmmoList;
            _mechAmmoTypes = mechAmmoList[0].AmmoUser.Props.ammoSet.ammoTypes;
            _mechMagazineSize = _mechAmmoList[0].AmmoUser.Props.magazineSize;
            // copy the loadouts from the _mechAmmoShown
            _tmpLoadouts = new Dictionary<AmmoDef, int>(mechAmmoList[0].Loadouts);

        }

        public override void PreOpen()
        {
            if (_mechAmmoList.Count == 0)
            {
                return;
            }

            Vector2 initialSize = this.InitialSize;
            Maglabel = "MTA_MagazinePrefix".Translate(_mechMagazineSize);
            Text.Font = GameFont.Small;
            headerHeight = Text.CalcHeight(Maglabel, initialSize.x);
            initialSize.y = (_mechAmmoTypes.Count + 3) * (BotAreaHeight) + headerHeight;
            this.windowRect = new Rect(((float)UI.screenWidth - initialSize.x) / 2f, ((float)UI.screenHeight - initialSize.y) / 2f, initialSize.x, initialSize.y);
            this.windowRect = this.windowRect.Rounded();
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(300f, 130f);
            }
        }
        public override void DoWindowContents(Rect inRect)
        {
            _mechAmmoList.RemoveAll(m => !m.parent.Spawned || m.ParentPawn.Dead);
            if (_mechAmmoList.Count == 0)
            {
                Close();
                return;
            }

            Text.Font = GameFont.Small;
            float curY = 0;

            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(inRect, Maglabel);
            curY += headerHeight + Margin;
            foreach (var ammoType in _mechAmmoTypes)
            {
                int value = 0;
                _tmpLoadouts.TryGetValue(ammoType.ammo, out value);
                string label = ammoType.ammo.ammoClass.labelShort != null ? ammoType.ammo.ammoClass.labelShort : ammoType.ammo.ammoClass.label;
                DrawThingRow(inRect, ref curY, ref value, ammoType.ammo, label);
                _tmpLoadouts.SetOrAdd(ammoType.ammo, value);
            }


            curY += Margin;
            if (Widgets.ButtonText(new Rect(inRect.x, curY, inRect.width, BotAreaHeight), "OK".Translate(), true, true, true, null))
            {
                SetMagCount(_mechAmmoList, _tmpLoadouts);
                Close(true);

            }
        }

        [Compatibility.Multiplayer.SyncMethod]
        private static void SetMagCount(List<CompMechAmmo> mechAmmoList, Dictionary<AmmoDef, int> tmpLoadouts)
        {
            //set the loadouts for all the comps
            foreach (var compMechAmmo in mechAmmoList)
            {
                //copy the loadouts from the _tmpLoadouts
                foreach ((AmmoDef ammoDef, int amount) in tmpLoadouts)
                {
                    compMechAmmo.Loadouts.SetOrAdd(ammoDef, amount);
                }
                compMechAmmo.TakeAmmoNow();
            }
        }

        public void DrawThingRow(Rect rect, ref float curY, ref int count, Def defForIcon, string label)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.DefIcon(new Rect(rect.x, curY, BotAreaWidth, BotAreaHeight), defForIcon);
            Widgets.Label(new Rect(rect.x + BotAreaWidth + Margin, curY, rect.width - BotAreaWidth * 4, BotAreaHeight), label);
            if (Widgets.ButtonText(new Rect(rect.x + rect.width - BotAreaWidth * 4, curY, BotAreaWidth, BotAreaHeight), "-", true, true, true, null))
            {
                count -= GenUI.CurrentAdjustmentMultiplier();
            }
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x + rect.width - BotAreaWidth * 3, curY, BotAreaWidth * 2, BotAreaHeight), count.ToString() + " (" + (count * _mechMagazineSize).ToString() + ")");
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonText(new Rect(rect.x + rect.width - BotAreaWidth, curY, BotAreaWidth, BotAreaHeight), "+", true, true, true, null))
            {
                count += GenUI.CurrentAdjustmentMultiplier();
            }

            count = count < 0 ? 0 : count;

            curY += BotAreaHeight;
        }
    }
}


