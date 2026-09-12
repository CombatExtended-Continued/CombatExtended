#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Buildings/Building_TargetingConsole.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

using CombatExtended.Compatibility.VGECompat;
using Verse;

namespace CombatExtended.Compatibility.VGECompat2;

public class Building_TargetingConsoleCE : Building_TargetingTerminalCE
{
    public override int MaxLinkedTurrets => 3;
    public override float LinkRange => 55f;
    public override string OnlyArtilleryErrorKey => "VGE_TargetingConsoleOnlyGravshipArtillery";
    public override string LinkGizmoDesc => "VGE_LinkWithTurretConsoleDesc".Translate();
    public override string UnlinkGizmoDesc => "VGE_UnlinkWithTurretConsoleDesc".Translate();
    public override string SelectGizmoDesc => "VGE_SelectLinkedTurretConsoleDesc".Translate();
}
