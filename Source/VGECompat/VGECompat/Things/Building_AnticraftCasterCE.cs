#region License
// Any VGE Code used for compatibility has been taken from the following source
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_AnticraftCaster.cs
#endregion

namespace CombatExtended.Compatibility.VGECompat;

public class Building_AnticraftCasterCE : Building_AnticraftEmitterCE
{
    public override bool CanAutoAttack => true;
}
