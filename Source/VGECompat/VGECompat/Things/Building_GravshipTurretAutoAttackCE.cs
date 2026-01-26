namespace CombatExtended.Compatibility.VGECompat;

#region License
// Any VGE Code used for compatibility has been taken from the following source
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_GravshipTurretAutoAttack.cs
#endregion

public class Building_GravshipTurretAutoAttackCE : Building_GravshipTurretCE
{
    public override bool CanAutoAttack => true;
}

