using System;
using RimWorld;
using CombatExtended.RocketGUI;
using UnityEngine;
using GUIUtility = CombatExtended.RocketGUI.GUIUtility;
using Verse;

namespace CombatExtended
{
    public class ITab_AttachmentView : ITab
    {
        public ITab_AttachmentView()
        {
            size = new Vector2(460f, 450f);
            labelKey = "TabAttachments";
            tutorTag = "Attachments";
        }

        public override void FillTab()
        {
            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 20f, size.x, size.y - 20f).ContractedBy(10f);
        }
    }
}
