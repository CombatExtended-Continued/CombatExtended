using CombatExtended.Compatibility.VGECompat;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VanillaGravshipExpanded2;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CombatExtended.Compatibility.VGE2Compat;

public class Verb_BombardmentTargeterCE : Verb_CastBase
{
    public override float EffectiveRange => 55.9f;
    public override bool MultiSelect => false;

    public override bool TryCastShot()
    {
        var targeter = (Apparel_GravshipBombardmentTargeterCE)EquipmentSource;
        if (targeter.linkedTurretCE == null || targeter.linkedTurretCE.Destroyed)
        {
            return false;
        }
        // We use our own logic there
        // var comp = targeter.linkedTurret.TryGetComp<CompWorldArtillery>();
        ProjectilePropertiesCE projectilePropertiesCE = (ProjectilePropertiesCE)targeter.linkedTurretCE.Projectile?.projectile;
        TravelingShellProperties shellingProps = projectilePropertiesCE?.shellingProps;

        if (targeter.linkedTurretCE.Map == caster.Map)
        {
            targeter.linkedTurretCE.OrderAttack(currentTarget);
        }
        else if (shellingProps != null)
        {
            var globalTarget = new GlobalTargetInfo(caster.Map.Parent);
            //comp.StartAttack(globalTarget, currentTarget, targeter.linkedTurret);
            // Call our function
            targeter.linkedTurretCE.TryAttackWorldTarget(globalTarget, currentTarget);
        }

        targeter.lastFireTick = Find.TickManager.TicksGame;
        InternalDefOf.OrbitalTargeter_Fire.PlayOneShot(targeter.Wearer);
        return true;
    }

    public override void DrawHighlight(LocalTargetInfo target)
    {
        if (caster == null || !caster.Spawned) { return; }

        base.DrawHighlight(target);
    }

    public override float HighlightFieldRadiusAroundTarget(out bool needLOSToCenter)
    {
        needLOSToCenter = true;

        if (EquipmentSource is Apparel_GravshipBombardmentTargeterCE targeter && targeter.linkedTurretCE != null)
        {
            var turret = targeter.linkedTurretCE;
            // var proj = turret.AttackVerb?.verbProps?.defaultProjectile;
            // use our own code
            var proj = turret.Projectile;

            if (proj != null && proj.projectile.explosionRadius > 0f)
            {
                float radius = proj.projectile.explosionRadius + proj.projectile.explosionRadiusDisplayPadding;

                float forcedMiss = turret.AttackVerb.verbProps.ForcedMissRadius;
                if (forcedMiss > 0f && turret.AttackVerb.BurstShotCount > 1)
                {
                    radius += forcedMiss;
                }

                return radius;
            }
        }
        return 0f;
    }

    public override Texture2D UIIcon
    {
        get
        {
            if (EquipmentSource is Apparel_GravshipBombardmentTargeterCE targeter && targeter.linkedTurretCE != null)
            {
                return targeter.linkedTurretCE.def.uiIcon;
            }
            return base.UIIcon;
        }
    }

    public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
    {
        if (!base.ValidateTarget(target, showMessages))
        {
            return false;
        }

        var targeter = (Apparel_GravshipBombardmentTargeterCE)EquipmentSource;
        if (targeter?.linkedTurretCE != null)
        {
            var turret = targeter.linkedTurretCE;
            if (turret.Map == caster.Map)
            {
                float dist = (target.Cell - turret.Position).LengthHorizontal;
                if (dist < turret.AttackVerb.verbProps.minRange)
                {
                    if (showMessages)
                    {
                        Messages.Message("MessageTargetBelowMinimumRange".Translate(), MessageTypeDefOf.RejectInput, false);
                    }
                    return false;
                }
                if (dist > turret.AttackVerb.EffectiveRange)
                {
                    if (showMessages)
                    {
                        Messages.Message("MessageTargetBeyondMaximumRange".Translate(), MessageTypeDefOf.RejectInput, false);
                    }
                    return false;
                }
            }
        }
        return true;
    }
}

