using CombatExtended.Compatibility.VGECompat;
using RimWorld;
using System.Collections.Generic;
using VanillaGravshipExpanded2;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Apparel/Apparel_GravshipBombardmentTargeter.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGE2Compat;

internal class Apparel_GravshipBombardmentTargeterCE : Apparel_GravshipBombardmentTargeter, ITurretLinkerCE
{
    public Building_GravshipTurretCE linkedTurretCE;

    public IEnumerable<Building_GravshipTurretCE> LinkedTurretsCE
    {
        get
        {
            if (linkedTurretCE != null)
            {
                yield return linkedTurretCE;
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref linkedTurretCE, "linkedTurretCE");
    }

    public override IEnumerable<Gizmo> GetWornGizmos()
    {
        foreach (var gizmo in base.GetWornGizmos())
        {
            // clean unwanted gizmo
            if (!TurretLinkerCEUtility.IsUnwantedGizmo(this, gizmo))
            {
                if (gizmo is Command_VerbTarget command && linkedTurretCE != null)
                {
                    command.defaultLabel = "VGE_FireBombardmentTargeter".Translate(linkedTurretCE.LabelNoParenthesis);
                    command.defaultDesc = "VGE_FireBombardmentTargeterDesc".Translate(linkedTurretCE.LabelNoParenthesis);
                    command.icon = linkedTurretCE.def.uiIcon;
                }
                yield return gizmo;
            }
            // else skip
        }
        foreach (var gizmo in TurretLinkerCEUtility.GetLinkerGizmos(this, LinkRange))
        {
            yield return gizmo;
        }
    }

    public void LinkTo(Building_GravshipTurretCE turret)
    {
        linkedTurretCE = turret;
        if (turret.linkedTerminal != this)
        {
            turret.LinkTo(this);
        }
    }

    public void Unlink(Building_GravshipTurretCE turret)
    {
        if (linkedTurretCE == turret)
        {
            linkedTurretCE = null;
            turret.unlinking = true;
            turret.Unlink();
            turret.unlinking = false;
        }
    }

    public override void Notify_Unequipped(Pawn pawn)
    {
        base.Notify_Unequipped(pawn);
        if (linkedTurretCE != null)
        {
            Unlink(linkedTurretCE);
        }
    }
}

