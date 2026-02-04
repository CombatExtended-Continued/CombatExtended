namespace CombatExtended.Compatibility.VGECompat;

#region License
// Any VGE Code used for compatibility has been taken from the following source
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_JavelinPod.cs
#endregion

public class Building_JavelinPodCE : Building_JavelinLauncherCE
{
    public override bool CanAutoAttack => true;
}

