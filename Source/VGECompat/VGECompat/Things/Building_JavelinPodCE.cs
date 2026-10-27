using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_JavelinPod.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended-Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat;

[StaticConstructorOnStartup]
public class Building_JavelinPodCE : Building_JavelinLauncherCE
{
    public override bool CanAutoAttack => true;
}
