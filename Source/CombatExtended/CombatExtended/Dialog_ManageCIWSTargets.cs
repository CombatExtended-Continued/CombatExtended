using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended.CombatExtended
{
    [StaticConstructorOnStartup]
    public class Dialog_ManageCIWSTargets : Window
    {
        static IList<ThingDef> copied = new List<ThingDef>();

        private readonly IList<ThingDef> availableTargets;
        private readonly IList<ThingDef> ignored;

        private const float BufferArea = 16f, TextOffset = 6f, RowHeight = 30f, ElementsHeight = 26f;
        private IList<ThingDef> filteredAvailableTargets;
        private Vector2 scrollPosition;
        private Vector2 scrollPosition2;
        private static Texture2D _darkBackground = SolidColorMaterials.NewSolidColorTexture(0f, 0f, 0f, .2f);
        private string searchString;

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(1000, 700);
            }
        }
        private string SearchString
        {
            get => searchString;
            set
            {
                if (searchString != value)
                {
                    searchString = value;
                    ResetFilterCollection();
                }
            }
        }
        public Dialog_ManageCIWSTargets(IList<ThingDef> availableTargets, IList<ThingDef> ignored) : base()
        {
            this.availableTargets = availableTargets;
            this.ignored = ignored;
        }

        void ResetFilterCollection()
        {
            var collection = availableTargets.Except(ignored);
            if (!string.IsNullOrWhiteSpace(SearchString))
            {
                collection = collection.Where(x => x.label.IndexOf(SearchString, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            filteredAvailableTargets = collection.ToList();
        }

        public override void DoWindowContents(Rect inRect)
        {

            inRect = new Rect(inRect.x, inRect.y + BufferArea, inRect.width, inRect.height - BufferArea);
            var y = inRect.y;
            Rect labelRect = new Rect(inRect.width / 2, y, inRect.width / 2, ElementsHeight);
            Rect label2Rect = new Rect(inRect.x + BufferArea, y, inRect.width / 2, ElementsHeight);



            Widgets.Label(labelRect, "CIWSSettings_AvailableTargets".Translate());
            Widgets.Label(label2Rect, "CIWSSettings_IgnoredTargets".Translate());

            y += ElementsHeight + BufferArea;

            Rect searchBarRect = new Rect(inRect.width / 2 + TextOffset, y, inRect.width / 2 - BufferArea * 3, ElementsHeight);
            Rect copyRect = new Rect(inRect.x + BufferArea, y, 46f, ElementsHeight);
            Rect pasteRect = new Rect(copyRect.xMax + BufferArea, y, 46f, ElementsHeight);


            SearchString = Widgets.TextArea(searchBarRect, SearchString);
            if (Widgets.ButtonText(copyRect, "CIWSSettings_Copy".Translate()))
            {
                copied = ignored.ToList();
            }
            if (Widgets.ButtonText(pasteRect, "CIWSSettings_Paste".Translate()))
            {
                ignored.Clear();
                foreach (var item in copied)
                {
                    ignored.Add(item);
                }
                ResetFilterCollection();
            }

            y += ElementsHeight + BufferArea;

            Rect outRect = new Rect(inRect.width / 2, y, inRect.width / 2 - BufferArea, inRect.height - y - BufferArea);
            Rect viewRect = new Rect(outRect.x, outRect.y, outRect.width - 32f, filteredAvailableTargets.Count * RowHeight);
            GUI.DrawTexture(outRect, _darkBackground);
            Widgets.BeginScrollView(outRect, ref scrollPosition2, viewRect, true);
            for (int i = 0; i < filteredAvailableTargets.Count; i++)
            {
                ThingDef target = filteredAvailableTargets[i];
                Rect allowedDefRect = new Rect(viewRect.x, viewRect.y + (RowHeight * i), viewRect.width, RowHeight);
                if (i % 2 == 0)
                {
                    GUI.DrawTexture(allowedDefRect, _darkBackground);
                }
                allowedDefRect = new Rect(allowedDefRect.x + TextOffset, allowedDefRect.y, allowedDefRect.width - TextOffset, allowedDefRect.height);
                if (Widgets.ButtonText(allowedDefRect, target.label, false, false, true))
                {
                    ignored.Add(target);
                    ResetFilterCollection();
                }
            }
            Widgets.EndScrollView();
            outRect = new Rect(inRect.x + BufferArea, y, inRect.width / 2 - BufferArea * 2, inRect.height - y - BufferArea);
            viewRect = new Rect(outRect.x, outRect.y, outRect.width - 32f, ignored.Count * RowHeight);
            GUI.DrawTexture(outRect, _darkBackground);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);
            for (int i = 0; i < ignored.Count; i++)
            {
                var s = ignored[i];
                Rect ignoredDefRect = new Rect(viewRect.x, viewRect.y + (RowHeight * i), viewRect.width, RowHeight);
                if (i % 2 == 0)
                {
                    GUI.DrawTexture(ignoredDefRect, _darkBackground);
                }
                ignoredDefRect = new Rect(ignoredDefRect.x + TextOffset, ignoredDefRect.y, ignoredDefRect.width - TextOffset, ignoredDefRect.height);
                if (Widgets.ButtonText(ignoredDefRect, s.label, false, false, true))
                {
                    ignored.Remove(s);
                    ResetFilterCollection();
                }
            }
            Widgets.EndScrollView();
        }
    }
}
