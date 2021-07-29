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
        private Verb lastVerb = null;
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

        private int _lastOptimized = -1;
        public bool ShouldOptimze
        {
            get
            {
                if (_lastOptimized == -1)
                    _lastOptimized = GenTicks.TicksGame; ;
                return GenTicks.TicksGame - _lastOptimized > 600;
            }
            set
            {
                if (!value) _lastOptimized = GenTicks.TicksGame;
            }
        }

        public override bool StartCastChecks(Verb verb, LocalTargetInfo castTarg, LocalTargetInfo destTarg)
        {
            if (!ShouldRun) return true;
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

            var fireModes = verb.EquipmentSource.TryGetComp<CompFireModes>();
            if (verb.EquipmentSource != null && fireModes != null && _castTarg != null && _destTarg != null)
            {
                if (ShouldOptimze || lastVerb != verb)
                {
                    OptimizeModes(fireModes, verb, _castTarg.Value, _destTarg.Value);
                    this.lastVerb = verb;
                    this.ShouldOptimze = false;
                    this.lastAimMode = fireModes.CurrentAimMode;
                    this.lastFireMode = fireModes.CurrentFireMode;
                }
                else
                {
                    fireModes.TrySetAimMode(this.lastAimMode);
                    fireModes.TrySetFireMode(this.lastFireMode);
                }
            }
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
                float shotDist = castTarg.Cell.DistanceTo(SelPawn.Position);
                float bullets = verbShoot.compAmmo.CurMagCount + verbShoot.compAmmo.MagsLeft;

                if (bullets < Mathf.Max(verbShoot.compAmmo.Props.magazineSize / 3f, 30) && shotDist > 35)
                {
                    if (shotDist < 25)
                    {
                        fireModes.TrySetAimMode(AimMode.Snapshot);
                        fireModes.TrySetFireMode(FireMode.BurstFire);
                    }
                    else
                    {
                        fireModes.TrySetAimMode(AimMode.AimedShot);
                        fireModes.TrySetFireMode(FireMode.SingleFire);
                    }
                    return;
                }
                if (!Map.VisibilityGoodAt(SelPawn, castTarg.Cell, nightVisionEfficiency: NightVisionEfficiency))
                {
                    fireModes.TrySetAimMode(AimMode.AimedShot);
                    fireModes.TrySetFireMode(FireMode.AutoFire);
                    return;
                }
                if (castTarg.Thing is Pawn target)
                {
                    float range = Mathf.Max(verb.EffectiveRange, 1);
                    float recoilFactor = verbProps.recoilAmount * (0.6f + shotDist / range);
                    if (recoilFactor <= 0.40f)
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

                    FireMode smartAuto = bullets < verbShoot.compAmmo.Props.magazineSize * 1.5f ? FireMode.BurstFire : FireMode.AutoFire;

                    if (fireModes.AvailableFireModes.Count == 1)
                    {
                        fireModes.TrySetAimMode(AimMode.AimedShot);
                        fireModes.TrySetFireMode(smartAuto);
                        return;
                    }
                    float pelletsAuto = verbShoot.ShotsPerBurstFor(FireMode.AutoFire);
                    float pelletsBurst = verbShoot.ShotsPerBurstFor(FireMode.BurstFire);

                    var allies = target.GetTacticalManager().TargetedByEnemy;
                    var suppressable = target.TryGetComp<CompSuppressable>();

                    if (allies.Count(p => p.equipment?.Primary?.def.IsMeleeWeapon ?? false) > 1 && !suppressable.isSuppressed)
                    {
                        fireModes.TrySetAimMode(AimMode.SuppressFire);
                        fireModes.TrySetFireMode(smartAuto);
                        return;
                    }
                    if (shotDist / range > 0.6f)
                    {
                        fireModes.TrySetAimMode(AimMode.AimedShot);
                        fireModes.TrySetFireMode(smartAuto);
                        return;
                    }
                    fireModes.TrySetAimMode(AimMode.Snapshot);
                    fireModes.TrySetFireMode(smartAuto);
                }
            }
        }
    }
}
