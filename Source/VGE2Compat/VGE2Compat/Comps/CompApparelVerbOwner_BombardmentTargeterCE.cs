using RimWorld;
using Verse;
using VanillaGravshipExpanded;

namespace CombatExtended.Compatibility.VGE2Compat;

public class CompApparelVerbOwner_BombardmentTargeterCE : CompApparelVerbOwner
{
    public override void Notify_Equipped(Pawn pawn)
    {
        base.Notify_Equipped(pawn);
        foreach (var verb in VerbTracker.AllVerbs)
        {
            verb.caster = pawn;
        }
    }

    public override bool CanBeUsed(out string reason)
    {
        if (!base.CanBeUsed(out reason))
        {
            return false;
        }

        var targeter = (Apparel_GravshipBombardmentTargeterCE)parent;
        if (targeter.linkedTurretCE == null || targeter.linkedTurretCE.Destroyed)
        {
            reason = "VGE_NeedsLinkedTargetingTerminal".Translate();
            return false;
        }

        if (Find.TickManager.TicksGame < targeter.lastFireTick + Apparel_GravshipBombardmentTargeterCE.CooldownTicks)
        {
            reason = "VGE_TargeterOnCooldown".Translate((targeter.lastFireTick + Apparel_GravshipBombardmentTargeterCE.CooldownTicks - Find.TickManager.TicksGame).ToStringSecondsFromTicks());
            return false;
        }

        // Use compAmmo hear instead of CompFuel
        if (targeter.linkedTurretCE.CompAmmo != null && !targeter.linkedTurretCE.CompAmmo.HasAmmo)
        {
            reason = "VGE_TargeterNoAmmo".Translate();
            return false;
        }

        if (!targeter.linkedTurretCE.CanFire)
        {
            reason = "VGE_NeedsMannedTargetingTerminal".Translate();
            return false;
        }

        var wearer = Wearer;
        if (wearer?.Map == null)
        {
            reason = "VGE_NeedsLinkedTargetingTerminal".Translate();
            return false;
        }
        if (targeter.linkedTurretCE.Map == wearer.Map)
        {
            float dist = wearer.Position.DistanceTo(targeter.linkedTurretCE.Position);
            float maxRange = targeter.linkedTurretCE.AttackVerb.EffectiveRange;
            if (dist > maxRange)
            {
                reason = "VGE_TargeterOutOfRange".Translate();
                return false;
            }
        }
        else
        {
            // Use our own logic instead of CompWorldArtillery
            var projectileProps = (ProjectilePropertiesCE)targeter.linkedTurretCE.Projectile.projectile;
            var hasShellingProps = projectileProps.shellingProps != null;
            float dist = hasShellingProps ? GravshipHelper.GetDistance(targeter.linkedTurretCE.Map.Tile, wearer.Map.Tile) : 99999f;
            float maxRange = hasShellingProps ? projectileProps.shellingProps.range : 0f;
            if (hasShellingProps || dist > maxRange)
            {
                reason = "VGE_TargeterOutOfRange".Translate();
                return false;
            }
        }

        return true;
    }
}
