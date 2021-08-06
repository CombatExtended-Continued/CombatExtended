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
        private FireMode lastFireMode = FireMode.AutoFire;
        private AimMode lastAimMode = AimMode.Snapshot;

        private LocalTargetInfo? _castTarg = null;
        private LocalTargetInfo? _destTarg = null;

        public override int Priority => 0;

        private int _NVEfficiencyAge = -1;
        private float _NVEfficiency = -1;
        public float NightVisionEfficiency
        {
            get
            {
                if (_NVEfficiency == -1 || GenTicks.TicksGame - _NVEfficiencyAge > GenTicks.TickRareInterval)
                {
                    _NVEfficiency = SelPawn.GetStatValue(CE_StatDefOf.NightVisionEfficiency);
                    _NVEfficiencyAge = GenTicks.TicksGame;
                }
                return _NVEfficiency;
            }
        }

        public virtual bool ShouldRun
        {
            get
            {
                return !(SelPawn.Faction?.IsPlayer ?? false);
            }
        }

        public CompFireSelection()
        {
        }

        public override bool StartCastChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            this._castTarg = castTarg;
            this._destTarg = destTarg;

            return true;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref lastFireMode, "lastFireMode", FireMode.AutoFire);
            Scribe_Values.Look(ref lastAimMode, "lastAimMode", AimMode.Snapshot);
        }

        public override void OnStartCastSuccess(Verb verb)
        {
            base.OnStartCastSuccess(verb);
            if (!ShouldRun) return;

            CompFireModes fireModes = verb.EquipmentSource?.TryGetComp<CompFireModes>() ?? null;

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
                if (verbShoot.CompAmmo == null)
                {
                    fireModes.TrySetAimMode(AimMode.Snapshot);
                    fireModes.TrySetFireMode(FireMode.AutoFire);
                    return;
                }
                float shotDist = castTarg.Cell.DistanceTo(SelPawn.Position);

                if (projProps.pelletCount > 1 && shotDist < 20)
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
                float bullets = verbShoot.CompAmmo.CurMagCount + verbShoot.CompAmmo.MagsLeft;

                if (castTarg.Thing is Pawn target)
                {
                    if (SelPawn.EdgingCloser(target))
                    {
                        if (shotDist <= 20)
                        {
                            fireModes.TrySetAimMode(AimMode.SuppressFire);
                            fireModes.TrySetFireMode(FireMode.AutoFire);
                            return;
                        }
                        if (shotDist <= 40)
                        {
                            fireModes.TrySetAimMode(AimMode.Snapshot);
                            fireModes.TrySetFireMode(FireMode.AutoFire);
                            return;
                        }
                    }
                    float range = Mathf.Max(verb.EffectiveRange, 1);
                    float recoilFactor = verbProps.recoilAmount * (0.6f + shotDist / range);

                    if (shotDist / range > 0.5f && !Map.VisibilityGoodAt(SelPawn, castTarg.Cell, nightVisionEfficiency: NightVisionEfficiency))
                    {
                        fireModes.TrySetAimMode(AimMode.AimedShot);
                        fireModes.TrySetFireMode(FireMode.BurstFire);
                        return;
                    }
                    if (recoilFactor <= 0.60f)
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
                }
                if (castTarg.Thing is Building_Turret)
                {
                    if (shotDist > 40)
                    {
                        fireModes.TrySetAimMode(AimMode.AimedShot);
                        fireModes.TrySetFireMode(FireMode.BurstFire);
                        return;
                    }
                    fireModes.TrySetAimMode(AimMode.Snapshot);
                    fireModes.TrySetFireMode(FireMode.AutoFire);
                    return;
                }
                if (bullets < verbShoot.CompAmmo.MagSize && shotDist > 50)
                {
                    fireModes.TrySetAimMode(AimMode.AimedShot);
                    fireModes.TrySetFireMode(FireMode.SingleFire);
                    return;
                }
                if (bullets < verbShoot.CompAmmo.MagSize * 1.5f && shotDist > 35)
                {
                    fireModes.TrySetAimMode(AimMode.AimedShot);
                    fireModes.TrySetFireMode(FireMode.BurstFire);
                    return;
                }
                fireModes.TrySetAimMode(AimMode.Snapshot);
                fireModes.TrySetFireMode(FireMode.AutoFire);
            }
        }
    }
}
