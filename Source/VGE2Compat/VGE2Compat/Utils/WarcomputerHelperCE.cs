using CombatExtended.Compatibility.VGECompat;
using System;
using VanillaGravshipExpanded2;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Utils/WarcomputerHelper.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGE2Compat;

// I adapted their code from different patches to centralize it there.
// I used this infographic as reference https://raw.githubusercontent.com/Vanilla-Expanded/VanillaGravshipExpanded2/refs/heads/main/About/InfographicsBattle_16.png
public static class WarcomputerHelperCE
{
    public static void ApplyForcedWarcomputerBuffs(this Building_GravshipTurretCE turret)
    {
        var hasBuff = WarcomputerHelper.IsWarcomputerPresent(turret.Map);
        var verb = turret.AttackVerb;

        if (turret.def.building.turretGunDef.Verbs[0] is not VerbPropertiesCE)
        {
            return;
        }

        var originalProps = (VerbPropertiesCE)turret.def.building.turretGunDef.Verbs[0];
        if (verb.verbProps == originalProps)
        {
            verb.verbProps = (VerbProperties)WarcomputerHelper.MemberwiseCloneMethod.Invoke(originalProps, null);
        }

        if (turret.def == InternalDefOf.VGE_GaussGun)
        {
            ((VerbPropertiesCE)verb.verbProps).circularError = hasBuff ? originalProps.circularError * 0.85f : originalProps.circularError;
        }

        if (turret.def == InternalDefOf.VGE_GaussHowitzer)
        {
            ((VerbPropertiesCE)verb.verbProps).circularError = hasBuff ? originalProps.circularError * 0.75f : originalProps.circularError;
        }

        // Equivalent Verb_TicksBetweenBurstShots_Patch
        if (turret.def == InternalDefOf.VGE_JavelinPod || turret.def == InternalDefOf.VGE_JavelinLauncher)
        {
            // equivalent to hardcoded -5 ...
            verb.verbProps.ticksBetweenBurstShots = hasBuff ? (int)Math.Floor(originalProps.ticksBetweenBurstShots * 0.5f) : originalProps.ticksBetweenBurstShots;
        }

        // Equivalent Verb_BurstShotCount_Patch
        if (turret.def == InternalDefOf.VGE_AnticraftCaster || turret.def == InternalDefOf.VGE_AnticraftEmitter)
        {
            verb.verbProps.burstShotCount = hasBuff ? (int)Math.Floor(originalProps.burstShotCount * 1.5f) : originalProps.burstShotCount;
        }
    }

    // Equivalent to CompPointDefence_InterceptionRadius_Patch
    // see https://github.com/Vanilla-Expanded/VanillaGravshipExpanded2/blob/main/1.6/Source/Harmony/CompPointDefence_InterceptionRadius_Patch.cs
    public static void ApplyForcedWarcomputerBuffsForCIWS(this Building_CIWS_CE turret)
    {
        var hasBuff = WarcomputerHelper.IsWarcomputerPresent(turret.Map);
        for (int i = 0; i < turret.GunCompEq.AllVerbs.Count; i++)
        {
            var verb = turret.GunCompEq.AllVerbs[i];
            var originalProps = turret.def.building.turretGunDef.Verbs[i];
            if (verb.verbProps == originalProps)
            {
                verb.verbProps = (VerbProperties)WarcomputerHelper.MemberwiseCloneMethod.Invoke(originalProps, null);
            }

            // I increase by 1/3 because that's what the VGE2 does. Even if they hardcoded 19.9f ...
            verb.verbProps.range = hasBuff ? originalProps.range * 1.33f : originalProps.range;
        }
    }
}

