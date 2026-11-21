#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Buildings/Building_Siegeworm.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

using CombatExtended.Compatibility.VGECompat;
using System.Collections.Generic;
using Verse;

namespace CombatExtended.Compatibility.VGE2Compat;

public class Building_SiegewormCE : Building_GravshipTurretCE
{
    public override bool CanFire => permanentlyDisabled is false;
    protected override bool CanSetForcedTarget => permanentlyDisabled is false;
    public override float GravshipTargeting => 1f;
    protected override bool ShowNoLinkedTerminalOverlay => false;

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            if (gizmo is Command_Action action && (action.defaultLabel == "VGE_LinkWithTerminal".Translate() || action.defaultLabel == "VGE_UnlinkWithTerminal".Translate() || action.defaultLabel == "VGE_SelectLinkedTerminal".Translate()))
            {
                continue;
            }
            yield return gizmo;
        }
    }
}
