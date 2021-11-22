using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace CombatExtended.HarmonyCE
{
    [HarmonyPatch(typeof(Command), nameof(Command.DrawIcon))]
    public  static class Harmony_Command
    {
        public static bool Prefix(Command __instance, Rect rect, GizmoRenderParms parms, Material buttonMat)
        {
			if (__instance is Command_VerbTarget command)
			{
				if (command.verb?.EquipmentSource is WeaponPlatform platform)
				{
					RocketGUI.GUIUtility.ExecuteSafeGUIAction(() =>
					{
						rect.position += new Vector2(command.iconOffset.x * rect.size.x, command.iconOffset.y * rect.size.y);
						Color color = GUI.color;
						if (!command.disabled || parms.lowLight)
						{
							color = command.IconDrawColor;
						}
						else
						{
							color = command.IconDrawColor.SaturationChanged(0f);
						}
						if (parms.lowLight)
						{
							color = GUI.color.ToTransparent(0.6f);
						}
						float dx = rect.width * 0.15f / 2f;
						rect.xMin += dx;
						rect.xMax -= dx;
						float dy = rect.height * 0.15f / 2f;
						rect.yMin += dy;
						rect.yMax -= dy;
						if (command.verb is Verb_ShootUseAttachment useAttachment)
						{
							GenUI.DrawTextureWithMaterial(rect, useAttachment.Attachment.attachmentVerb.iconTex, buttonMat);						
						}
						else
						{						
							RocketGUI.GUIUtility.DrawWeaponWithAttachments(rect, platform, null, color, buttonMat);						
						}
					});
					return false;
            	}
				if(command.verb is Verb_MarkForArtillery mark && !mark.MarkingConditionsMet())
                {
					command.disabled = true;
					command.disabledReason = "CE_MarkingUnavailableReason".Translate();
				}
			}
            return true;
        }
    }
}
