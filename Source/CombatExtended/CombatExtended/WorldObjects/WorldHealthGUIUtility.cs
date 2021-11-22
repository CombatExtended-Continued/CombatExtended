using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld.Planet;
using Verse;

namespace CombatExtended.WorldObjects
{
    public static class WorldHealthGUIUtility
    {
        private static List<WorldObject> visibleObjects = new List<WorldObject>(1000);

        public static void OnGUIWorldObjectHealth()
        {
            WorldObjectTrackerCE tracker = Find.World.GetComponent<WorldObjectTrackerCE>();
            visibleObjects.Clear();
            visibleObjects.AddRange(tracker.TrackedObjects.Where(w => Visible(w)));
            for(int i = 0;i < visibleObjects.Count; i++)
            {                
                DrawHealthBar(visibleObjects[i]);
            }
        }

        private static bool Visible(WorldObject worldObject) => !worldObject.HiddenBehindTerrainNow();

        private static void DrawHealthBar(WorldObject worldObject)
        {
            if (worldObject is MapParent mapParent && mapParent.HasMap && mapParent.Map != null && Find.Maps.Contains(mapParent.Map)) 
            {
                return;
            }
            HealthComp comp = worldObject.GetComponent<HealthComp>();
            if (comp == null)
            {
                return;
            }
            Vector2 position = worldObject.ScreenPos();
            Rect rect = new Rect(new Vector2(position.x - 15, position.y + 18), new Vector2(30, 5));
            Color color;           
            color = Color.red;
            RocketGUI.GUIUtility.ExecuteSafeGUIAction(() =>
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Tiny;
                Widgets.DrawBoxSolid(rect, Color.black);
                Widgets.DrawBoxSolid(rect.ContractedBy(1).LeftPart(comp.Health), GetHealthBarColor(comp.Health));
            });
        }

        private static Color GetHealthBarColor(float health)
        {
            if (health > 0.5f)
            {
                 return Color.green;
            }
            else if (health > 0.20f)
            {
                return Color.yellow;
            }
            return Color.red;
        }
    }
}

