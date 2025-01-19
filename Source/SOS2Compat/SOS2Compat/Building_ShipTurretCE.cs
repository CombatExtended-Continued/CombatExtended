using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using SaveOurShip2;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended.Compatibility.SOS2Compat
{
    public class Building_ShipTurretCE : Building_TurretGunCE
    {
        #region License
        // Any SOS2 Code used for compatibility has been taken from the following source and is licensed under the "Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International Public License"
        // https://github.com/KentHaeger/SaveOurShip2/blob/cf179981d242764af20c41440d69649e6ecd6450/Source/1.5/Building/Building_ShipTurret.cs
        #endregion
        public bool GroundDefenseMode;

        // Allows conversion of Building_ShipTurretCE into Building_ShipTurret as the SaveOurShip2.ShipCombatProjectile class which is used in Verb_ShootShipCE requires a Building_ShipTurret to be passed into its constructor.
        public static implicit operator Building_ShipTurret(Building_ShipTurretCE turretCE)
        {
            return new ShipTurretWrapperCE(turretCE);
        }

        #region Shared
        // Changed to always return true to support ship battles
        protected override bool CanSetForcedTarget
        {
            get
            {
                return true;
            }
        }

        public Building_ShipTurretCE()
        {
            top = new TurretTop(this);
        }

        public override bool Active //Added sos2 heatnet logic
        {
            get
            {
                if (Spawned && heatComp != null && heatComp.myNet != null && !heatComp.myNet.venting && (powerComp == null || powerComp.PowerOn) && (heatComp.myNet.PilCons.Any() || heatComp.myNet.AICores.Any() || heatComp.myNet.TacCons.Any()))
                {
                    return true;
                }
                return false;
            }
        }

        public override ThingDef Projectile // changed defaultProjectile for ce to defaultProjectileGround.
        {
            get
            {
                if (GroundDefenseMode)
                {
                    if (CompAmmo != null && CompAmmo.CurrentAmmo != null)
                    {
                        return CompAmmo.CurAmmoProjectile;
                    }
                    if (CompChangeable != null && CompChangeable.Loaded)
                    {
                        return CompChangeable.Projectile;
                    }
                    return ((Verb_ShootShipCE)AttackVerb)?.VerbPropsShip.defaultProjectileGround ?? AttackVerb.verbProps.defaultProjectile; // Returns ground projectile but fallbacks to space projectile
                }
                return AttackVerb.verbProps.defaultProjectile;
            }
        }

        public override void TryStartShootSomething(bool canBeginBurstImmediately)
        {
            if (GroundDefenseMode) // CE Logic
            {
                base.TryStartShootSomething(canBeginBurstImmediately);
            }
            else // SOS2 Logic
            {
                bool isValid = currentTargetInt.IsValid;
                if (!Spawned || (holdFire && CanToggleHoldFire) || !AttackVerb.Available() || PointDefenseMode || mapComp.ShipMapState != ShipMapState.inCombat)
                {
                    ResetCurrentTarget();
                    return;
                }
                if (!PlayerControlled && mapComp.HasShipMapAI) //AI targeting
                {
                    // CE PATCH: Removed logic for spinal weapon here as we aren't patching them
                    if (mapComp.OriginMapComp.MapRootListAll.Any(b => !b.Destroyed))
                    {
                        shipTarget = mapComp.OriginMapComp.MapRootListAll.RandomElement();
                    }
                    else
                    {
                        shipTarget = mapComp.ShipCombatTargetMap.listerBuildings.allBuildingsColonist.RandomElement();
                    }
                }
                if (shipTarget.IsValid)
                {
                    currentTargetInt = MapEdgeCell(5);
                }
                else
                {
                    currentTargetInt = TryFindNewTarget();
                }
                if (!isValid && currentTargetInt.IsValid)
                {
                    SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(Position, Map, false));
                }
                if (!currentTargetInt.IsValid)
                {
                    ResetCurrentTarget();
                    return;
                }
                float randomInRange = def.building.turretBurstWarmupTime.RandomInRange;
                if (randomInRange > 0f)
                {
                    burstWarmupTicksLeft = randomInRange.SecondsToTicks();
                    return;
                }
                if (canBeginBurstImmediately)
                {
                    BeginBurst();
                    return;
                }
                burstWarmupTicksLeft = 1;
            }
        }

        public override void ResetForcedTarget()
        {
            if (GroundDefenseMode) // CE Logic
            {
                base.ResetForcedTarget();
            }
            else // SOS2 Logic
            {
                shipTarget = LocalTargetInfo.Invalid;
                burstWarmupTicksLeft = 0;
                if ((mapComp.ShipMapState == ShipMapState.inCombat || GroundDefenseMode) && burstCooldownTicksLeft <= 0)
                {
                    TryStartShootSomething(false);
                }
            }
        }

        public override LocalTargetInfo TryFindNewTarget()
        {
            if (GroundDefenseMode) // CE Logic
            {
                return base.TryFindNewTarget();
            }
            else // SOS2 Logic
            {
                return LocalTargetInfo.Invalid;
            }
        }

        public override void BeginBurst()
        {
            // Shared Power/Heat/Ammo checks
            if (powerComp != null && powerComp.PowerNet.CurrentStoredEnergy() < EnergyToFire)
            {
                if (PlayerControlled)
                {
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CannotFireDueToPower", Label), this, MessageTypeDefOf.CautionInput);
                }
                ResetCurrentTarget();
                return;
            }
            if (heatComp.Props.heatPerPulse > 0 && !heatComp.AddHeatToNetwork(HeatToFire))
            {
                if (PlayerControlled)
                {
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CannotFireDueToHeat", Label), this, MessageTypeDefOf.CautionInput);
                }
                ResetCurrentTarget();
                return;
            }
            //ammo
            if (fuelComp != null)
            {
                if (fuelComp.Fuel <= 0)
                {
                    if (!PointDefenseMode && PlayerControlled)
                    {
                        Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CannotFireDueToAmmo", Label), this, MessageTypeDefOf.CautionInput);
                    }
                    shipTarget = LocalTargetInfo.Invalid;
                    ResetCurrentTarget();
                    return;
                }
                fuelComp.ConsumeFuel(1);
            }
            //draw the same percentage from each cap: needed*current/currenttotal
            foreach (CompPowerBattery bat in powerComp.PowerNet.batteryComps)
            {
                bat.DrawPower(Mathf.Min(EnergyToFire * bat.StoredEnergy / powerComp.PowerNet.CurrentStoredEnergy(), bat.StoredEnergy));
            }
            if (GroundDefenseMode) // CE Logic
            {
                base.BeginBurst();
            }
            else // Space Logic
            {
                // CE PATCH: Removed Spinal Logic here as spinals dont need patching
                // CE PATCH: Also moved power/heat checks to earlier in this function to cover ground defense mode aswell
                //sfx
                heatComp.Props.singleFireSound?.PlayOneShot(this);
                //cast
                if (shipTarget == null)
                {
                    shipTarget = LocalTargetInfo.Invalid;
                }

                if (PointDefenseMode)
                {
                    currentTargetInt = MapEdgeCell(20);
                    mapComp.lastPDTick = Find.TickManager.TicksGame;
                }
                //sync
                ((Verb_ShootShipCE)AttackVerb).shipTarget = shipTarget;
                if (AttackVerb.verbProps.burstShotCount > 0 && mapComp.ShipCombatTargetMap != null)
                {
                    SynchronizedBurstLocation = mapComp.FindClosestEdgeCell(mapComp.ShipCombatTargetMap, shipTarget.Cell);
                }
                else
                {
                    SynchronizedBurstLocation = IntVec3.Invalid;
                }
                // CE PATCH: Removed Spinal Logic here as spinals dont need patching
                AttackVerb.TryStartCastOn(currentTargetInt);
                OnAttackedTarget(currentTargetInt);
                BurstComplete();
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                if (!GroundDefenseMode && (gizmo as Command_VerbTarget != null))
                {
                    continue; // Only show the CE ground attack gizmo when in ground defense mode (otherwise it conflicts with the space attack gizmo)
                }
                yield return gizmo;
            }
            if (!GroundDefenseMode)
            {
                // SOS2 Gizmos
                if (!selected)
                {
                    selected = true;
                }
                if (!PlayerControlled)
                {
                    yield break;
                }

                if (CanSetForcedTarget)
                {
                    Command_TargetShipCombatCE command_VerbTargetShip = new Command_TargetShipCombatCE
                    {
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandSetForceAttackTarget"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandSetForceAttackTargetDesc"),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack"),
                        verb = AttackVerb,
                        turrets = Find.Selector.SelectedObjects.OfType<Building_ShipTurretCE>().ToList(),
                        hotKey = KeyBindingDefOf.Misc4,
                        drawRadius = false
                    };
                    yield return command_VerbTargetShip;
                }
                if (shipTarget.IsValid)
                {
                    Command_Action command_Action2 = new Command_Action
                    {
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandStopForceAttack"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandStopForceAttackDesc"),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt"),
                        action = delegate
                        {
                            ResetForcedTarget();
                            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                        }
                    };
                    if (!shipTarget.IsValid)
                    {
                        command_Action2.Disable(TranslatorFormattedStringExtensions.Translate("CommandStopAttackFailNotForceAttacking"));
                    }
                    command_Action2.hotKey = KeyBindingDefOf.Misc5;
                    yield return command_Action2;
                }
                if (CanToggleHoldFire)
                {
                    Command_Toggle command_Toggle = new Command_Toggle
                    {
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandHoldFire"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandHoldFireDesc"),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire"),
                        hotKey = KeyBindingDefOf.Misc6,
                        toggleAction = delegate
                        {
                            holdFire = !holdFire;
                            if (holdFire)
                            {
                                ResetForcedTarget();
                            }
                        },
                        isActive = (() => holdFire)
                    };
                    yield return command_Toggle;
                }
                // CE PATCH: Removed Torpedo Logic here as torpedos dont need patching
                if (heatComp.Props.pointDefense)
                {
                    Command_Toggle command_Toggle = new Command_Toggle
                    {
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.TurretPointDefense"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.TurretPointDefenseDesc"),
                        icon = ContentFinder<Texture2D>.Get("UI/PointDefenseMode"),
                        toggleAction = delegate
                        {
                            PointDefenseMode = !PointDefenseMode;
                            if (PointDefenseMode)
                            {
                                holdFire = false;
                            }
                        },
                        isActive = (() => PointDefenseMode)
                    };
                    yield return command_Toggle;
                }
                if (heatComp.Props.maxRange > heatComp.Props.optRange)
                {
                    Command_Toggle command_Toggle = new Command_Toggle
                    {
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.TurretOptimalRange"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.TurretOptimalRangeDesc"),
                        icon = ContentFinder<Texture2D>.Get("UI/OptimalRangeMode"),
                        toggleAction = delegate
                        {
                            useOptimalRange = !useOptimalRange;
                        },
                        isActive = (() => useOptimalRange)
                    };
                    yield return command_Toggle;
                }
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad) // Add all the SOS2 fields and setup GroundDefenseMode
        {
            base.SpawnSetup(map, respawningAfterLoad);

            mapComp = map.GetComponent<ShipMapComp>();
            heatComp = this.TryGetComp<CompShipHeat>();
            fuelComp = this.TryGetComp<CompRefuelable>();
            // CE PATCH: Removed Spinal and Torpedo comps here as Spinal and Torpedo dont need patching

            if (!Map.IsSpace() && heatComp.Props.groundDefense) // Ground defense prop is used to disable large guns on ground
            {
                GroundDefenseMode = true;
            }
            else
            {
                GroundDefenseMode = false;
            }
            if (!GroundDefenseMode)
            {
                ResetForcedTarget();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // New variables                        
            Scribe_Values.Look<IntVec3>(ref SynchronizedBurstLocation, "burstLocation");
            Scribe_Values.Look<bool>(ref PointDefenseMode, "pointDefenseMode");
            Scribe_Values.Look<bool>(ref useOptimalRange, "useOptimalRange");

            BackCompatibility.PostExposeData(this);
        }

        [Compatibility.Multiplayer.SyncMethod]
        public override void OrderAttack(LocalTargetInfo targ)
        {
            if (GroundDefenseMode) // CE Logic
            {
                base.OrderAttack(targ);
            }
            else // SOS2 Logic
            {
                if (holdFire)
                {
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageTurretWontFireBecauseHoldFire", def.label), this, MessageTypeDefOf.RejectInput, historical: false);
                    return;
                }
                if (PointDefenseMode)
                {
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.TurretInPointDefense", def.label), this, MessageTypeDefOf.RejectInput, historical: false);
                    return;
                }
                if (forcedTarget != targ)
                {
                    forcedTarget = targ;
                    if (burstCooldownTicksLeft <= 0)
                    {
                        TryStartShootSomething(false);
                    }
                }
            }
        }

        public override void Tick()
        {
            if (GroundDefenseMode) // CE Logic
            {
                base.Tick();
            }
            else // SOS2 Logic
            {
                // CE PATCH: Can't call base.Tick() as we don't want to call the CE Logic here, so therefore we have to call the same logic as Building_Turret and ThingWithComps first
                // ThingWithComps Tick
                if (comps != null)
                {
                    int i = 0;
                    for (int count = comps.Count; i < count; i++)
                    {
                        comps[i].CompTick();
                    }
                }
                // Building_Turret Tick
                if (forcedTarget.HasThing && (!forcedTarget.Thing.Spawned || !base.Spawned || forcedTarget.Thing.Map != base.Map))
                {
                    forcedTarget = LocalTargetInfo.Invalid;
                }
                // SOS2 Tick
                if (selected && !Find.Selector.IsSelected(this))
                {
                    selected = false;
                }
                if (!CanToggleHoldFire)
                {
                    holdFire = false;
                }
                if (forcedTarget.ThingDestroyed)
                {
                    ResetForcedTarget();
                }
                if (mapComp.ShipMapState != ShipMapState.inCombat)
                {
                    ResetForcedTarget();
                }
                if (Active && !IsStunned)
                {
                    this.GunCompEq.verbTracker.VerbsTick();
                    if (AttackVerb.state != VerbState.Bursting)
                    {
                        if (burstCooldownTicksLeft > 0)
                        {
                            burstCooldownTicksLeft--;
                        }
                        if (mapComp.ShipMapState == ShipMapState.inCombat && !heatComp.Venting)
                        {
                            if (heatComp.Props.pointDefense) //PD mode
                            {
                                bool pdActive = false;
                                if (burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10))
                                {
                                    pdActive = IncomingPtDefTargetsInRange();
                                    if (!PlayerControlled)
                                    {
                                        if (pdActive)
                                        {
                                            PointDefenseMode = true;
                                        }
                                        else
                                        {
                                            PointDefenseMode = false;
                                        }
                                    }
                                }
                                if (pdActive && PointDefenseMode)
                                {
                                    if (Find.TickManager.TicksGame > mapComp.lastPDTick + 10 && !holdFire)
                                    {
                                        BeginBurst();
                                    }
                                }
                            }
                            if (InRangeSC(mapComp.OriginMapComp.Range))
                            {
                                if (WarmingUp)
                                {
                                    burstWarmupTicksLeft--;
                                    if (burstWarmupTicksLeft == 0)
                                    {
                                        BeginBurst();
                                    }
                                }
                                else if (burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10))
                                {
                                    TryStartShootSomething(true);
                                }
                            }
                        }
                    }
                    top.TurretTopTick();
                    return;
                }
                else
                {
                    ResetCurrentTarget();
                }
            }
        }

        public override void DrawExtraSelectionOverlays() // Don't draw in ship combat mode
        {
            if (GroundDefenseMode)
            {
                base.DrawExtraSelectionOverlays();
            }
        }
        #endregion

        #region SOS2 Unchanged
        public bool PointDefenseMode;
        public float AmplifierDamageBonus = 0;
        private bool selected = false;
        public bool useOptimalRange;
        public IntVec3 SynchronizedBurstLocation;
        LocalTargetInfo shipTarget = LocalTargetInfo.Invalid;

        public float EnergyToFire => heatComp.Props.energyToFire * (1 + AmplifierDamageBonus);
        public float HeatToFire => heatComp.Props.heatPerPulse * (1 + AmplifierDamageBonus) * 3;

        public ShipMapComp mapComp;
        public CompShipHeat heatComp;
        public CompRefuelable fuelComp;

        public bool InRangeSC(float range)
        {
            if ((!useOptimalRange && heatComp.Props.maxRange > range) || (useOptimalRange && heatComp.Props.optRange > range))
            {
                return true;
            }
            return false;
        }

        private LocalTargetInfo MapEdgeCell(int miss)
        {
            if (miss > 0)
            {
                miss = Rand.RangeInclusive(-miss, miss);
            }
            //fire same as engine direction or opposite if retreating
            IntVec3 v;
            if ((mapComp.EngineRot == 0 && mapComp.Heading != -1) || (mapComp.EngineRot == 2 && mapComp.Heading == -1)) //north
            {
                v = new IntVec3(Position.x + miss, 0, Map.Size.z - 1);
            }
            else if ((mapComp.EngineRot == 1 && mapComp.Heading != -1) || (mapComp.EngineRot == 3 && mapComp.Heading == -1)) //east
            {
                v = new IntVec3(Map.Size.x - 1, 0, Position.z + miss);
            }
            else if ((mapComp.EngineRot == 2 && mapComp.Heading != -1) || (mapComp.EngineRot == 0 && mapComp.Heading == -1)) //south
            {
                v = new IntVec3(Position.x + miss, 0, 0);
            }
            else //west
            {
                v = new IntVec3(0, 0, Position.z + miss);
            }
            if (v.x < 0)
            {
                v.x = 0;
            }
            else if (v.x >= Map.Size.x)
            {
                v.x = Map.Size.x - 1;
            }
            if (v.z < 0)
            {
                v.z = 0;
            }
            else if (v.z >= Map.Size.z)
            {
                v.z = Map.Size.z;
            }

            return new LocalTargetInfo(v);
        }

        public bool IncomingPtDefTargetsInRange()
        {
            if (mapComp.TargetMapComp.TorpsInRange.Any() || mapComp.TargetMapComp.ShuttlesInRange.Where(shuttle => shuttle.Faction != this.Faction).Any())
            {
                return true;
            }
            return false;
        }

        public bool InRange(LocalTargetInfo target) // TODO: Check if this is an obsolete method
        {
            float range = Position.DistanceTo(target.Cell);
            if (range > AttackVerb.verbProps.minRange && range < AttackVerb.verbProps.range)
            {
                return true;
            }
            return false;
        }

        public void SetTarget(LocalTargetInfo target)
        {
            shipTarget = target;
        }
        #endregion

    }
}
