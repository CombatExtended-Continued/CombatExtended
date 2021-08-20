using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CombatExtended
{    
    public class GizmoEditAttachments : Command
    { 
        public List<GizmoEditAttachments> others;
        public WeaponPlatform weapon;
        private static Texture2D GearTex;                        

        /// <summary>
        /// Will return false if the user has selected weapons of different defs.
        /// </summary>
        public override bool Visible
        {
            get
            {
                return others?.All(w => w.weapon?.Platform == weapon?.Platform) ?? true;
            }
        }

        public override string Label
        {
            get
            {
                if(others?.Count > 1)
                    return "CE_EditWeapon_Group".Translate();
                return "CE_EditWeapon".Translate();
            }
        }

        public GizmoEditAttachments()
        {
            if (GearTex == null)
                GearTex = ContentFinder<Texture2D>.Get("UI/Icons/gear");
            this.icon = GearTex;            
        }

        public override bool GroupsWith(Gizmo other)
        {
            return other is GizmoEditAttachments;
        }

        public override void MergeWith(Gizmo other)
        {
            base.MergeWith(other);
            GizmoEditAttachments otherWeapon = other as GizmoEditAttachments;
            if (otherWeapon == null) return;            
            if (others == null)
            {
                others = new List<GizmoEditAttachments>();
                others.Add(this);
            }
            others.Add(otherWeapon);
        }     

        public override float GetWidth(float maxWidth)
        {
            return Mathf.Min(75f, maxWidth);
        }

        public override void ProcessInput(Event ev)
        {
            // skip if we have weapons of different defs.
            if (weapon == null || (others?.Any(w => w.weapon?.Platform != weapon?.Platform) ?? false))
                return;
            if (others != null && others.Count > 1) // edit everything from scratsh
            {
                if (Find.WindowStack.IsOpen<Window_AttachmentsEditor>())
                    Find.WindowStack.TryRemove(typeof(Window_AttachmentsEditor));
                Find.WindowStack.Add(new Window_AttachmentsEditor(weapon.Platform, new List<AttachmentLink>(), (selected) =>
                {
                    List<AttachmentDef> config = selected.Select(l => l.attachment).ToList();
                    foreach (GizmoEditAttachments command in others)
                    {
                        command.weapon.TargetConfig = config.ToList();
                        command.weapon.UpdateConfiguration();
                    }
                }));
            }
            else // edit the current weapon only
            {
                if (Find.WindowStack.IsOpen<Window_AttachmentsEditor>())
                    Find.WindowStack.TryRemove(typeof(Window_AttachmentsEditor));
                Find.WindowStack.Add(new Window_AttachmentsEditor(weapon));
            }
        }        
    }
}
