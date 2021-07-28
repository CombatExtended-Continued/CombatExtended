using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatExtended.AI
{
    public class CompFireSelection : ICompTactics
    {
        private LocalTargetInfo? _castTarg = null;
        private LocalTargetInfo? _destTarg = null;

        public override int Priority => 0;

        public override bool StartCastChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            this._castTarg = castTarg;
            this._destTarg = destTarg;

            return true;
        }

        public override void OnStartCastSuccess(Verb verb)
        {
            base.OnStartCastSuccess(verb);

            var fireModes = verb.EquipmentSource.TryGetComp<CompFireModes>();
            if (verb.EquipmentSource != null && fireModes != null && _castTarg != null && _destTarg != null)
                OptimizeModes(fireModes, verb, _castTarg.Value, _destTarg.Value);

            this._castTarg = null;
            this._destTarg = null;
        }

        public void OptimizeModes(CompFireModes fireModes, Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            if ((verb.EquipmentSource?.def.IsIlluminationDevice() ?? false) || verb is Verb_ShootFlareCE)
            {
                fireModes.TrySetAimMode(AimMode.SuppressFire);
                fireModes.TrySetFireMode(FireMode.AutoFire);
                return;
            }
            if ((verb.EquipmentSource?.def.IsAOEWeapon() ?? false))
            {
                fireModes.TrySetAimMode(AimMode.Snapshot);
                fireModes.TrySetFireMode(FireMode.AutoFire);
                return;
            }
            if (verb is Verb_ShootCE verbShoot && verb.verbProps is VerbPropertiesCE verbProps && verbProps.defaultProjectile?.projectile is ProjectilePropertiesCE projProps)
            {
                if (projProps.pelletCount > 1)
                {
                    if (verbProps.warmupTime > 1.5f)
                    {
                        fireModes.TrySetAimMode(AimMode.SuppressFire);
                        fireModes.TrySetFireMode(FireMode.AutoFire);
                        return;
                    }
                    else
                    {
                        fireModes.TrySetAimMode(AimMode.Snapshot);
                        fireModes.TrySetFireMode(FireMode.AutoFire);
                        return;
                    }
                }
                if (castTarg.Thing is Pawn target)
                {
                    float shotDist = castTarg.Cell.DistanceTo(SelPawn.Position);
                    float range = Mathf.Max(verb.EffectiveRange, 1);
                    float recoilFactor = verbProps.recoilAmount * Mathf.Pow(0.5f + shotDist / range, 2);

                    if (recoilFactor <= 0.20f)
                    {
                        if (castTarg.HasThing && target.EdgingCloser(target))
                        {
                            fireModes.TrySetAimMode(AimMode.SuppressFire);
                            fireModes.TrySetFireMode(FireMode.AutoFire);
                            return;
                        }
                        fireModes.TrySetAimMode(AimMode.Snapshot);
                        fireModes.TrySetFireMode(FireMode.AutoFire);
                        return;
                    }
                    else if (recoilFactor > 2.5f)
                    {
                        fireModes.TrySetAimMode(AimMode.AimedShot);
                        fireModes.TrySetFireMode(FireMode.BurstFire);
                        return;
                    }

                    float pelletsAuto = verbShoot.ShotsPerBurstFor(FireMode.AutoFire);
                    float pelletsBurst = verbShoot.ShotsPerBurstFor(FireMode.BurstFire);

                    var allies = target.GetTacticalManager().TargetedByEnemy;
                    var suppressable = target.TryGetComp<CompSuppressable>();

                    if (allies.Count(p => p.equipment?.Primary?.def.IsMeleeWeapon ?? false) > 1 && !suppressable.isSuppressed)
                    {
                        if (pelletsBurst > 7.5f)
                            fireModes.TrySetAimMode(AimMode.SuppressFire);
                        else if (pelletsBurst > 5)
                            fireModes.TrySetAimMode(AimMode.Snapshot);
                        else if (recoilFactor > 0.5f)
                            fireModes.TrySetAimMode(AimMode.AimedShot);
                        fireModes.TrySetFireMode(FireMode.AutoFire);
                        return;
                    }
                    if (shotDist / range > 0.6f)
                    {
                        fireModes.TrySetFireMode(FireMode.BurstFire);
                        fireModes.TrySetAimMode(AimMode.AimedShot);
                        return;
                    }

                    fireModes.TrySetFireMode(FireMode.AutoFire);
                    fireModes.TrySetAimMode(AimMode.Snapshot);
                }
            }
        }
    }
}
