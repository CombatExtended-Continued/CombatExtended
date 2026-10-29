using System.Collections.Generic;
using VanillaGravshipExpanded;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/ITurretLinker.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended-Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat;

public interface ITurretLinkerCE : ITurretLinker
{
    IEnumerable<Building_GravshipTurretCE> LinkedTurretsCE { get; }
    void LinkTo(Building_GravshipTurretCE turret);
    void Unlink(Building_GravshipTurretCE turret);
}

