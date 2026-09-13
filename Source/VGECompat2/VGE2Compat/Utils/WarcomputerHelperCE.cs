using CombatExtended;
using Verse;

namespace VanillaGravshipExpanded2
{
    public static class WarcomputerHelperCE 
    {
        public static void ApplyForcedMissRadiusBuffs(this Building_TurretGunCE turret)
        {
            var hasBuff = WarcomputerHelper.IsWarcomputerPresent(turret.Map);
            var verb = turret.AttackVerb;

            var originalProps = turret.def.building.turretGunDef.Verbs[0];
            if (verb.verbProps == originalProps)
            {
                verb.verbProps = (VerbProperties)WarcomputerHelper.MemberwiseCloneMethod.Invoke(originalProps, null);
            }

            if (turret.def == InternalDefOf.VGE_GaussGun)
            {
                verb.verbProps.forcedMissRadius = hasBuff ? System.Math.Max(0f, originalProps.forcedMissRadius - 1f) : originalProps.forcedMissRadius;
            }

            if (turret.def == InternalDefOf.VGE_GaussHowitzer)
            {
                verb.verbProps.forcedMissRadius = hasBuff ? System.Math.Max(0f, originalProps.forcedMissRadius - 2f) : originalProps.forcedMissRadius;
            }

            // Equivalent Verb_TicksBetweenBurstShots_Patch
            if (turret.def == InternalDefOf.VGE_JavelinPod || turret.def == InternalDefOf.VGE_JavelinLauncher)
            {
                verb.verbProps.ticksBetweenBurstShots += hasBuff ? -verb.verbProps.ticksBetweenBurstShots/2 : 0; // I use -5 instead of a hardcoded value ...
            }

            // Equivalent Verb_BurstShotCount_Patch
            if (turret.def == InternalDefOf.VGE_AnticraftCaster)
            {
                verb.verbProps.burstShotCount += hasBuff ? 10 : 0;
            }

            if (turret.def == InternalDefOf.VGE_AnticraftEmitter)
            {
                verb.verbProps.burstShotCount += hasBuff ? 15 : 0;
            }
        }
    }
}
