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

        #region Shared Functionality

        public bool holdFire;
        public bool GroundDefenseMode;

        public override LocalTargetInfo CurrentTarget => currentTargetInt;
        private bool CanToggleHoldFire => PlayerControlled;
        private bool WarmingUp => burstWarmupTicksLeft > 0;

        public static implicit operator Building_ShipTurret(Building_ShipTurretCE turretCE)
        {
            return new ShipTurretWrapperCE(turretCE);
        }

        protected override bool CanSetForcedTarget
        {
            get
            {
                return true;
            }
        }

        public override void BurstComplete()
        {
            if (GroundDefenseMode)
            {
                base.BurstComplete();
            }
            else
            {
                burstCooldownTicksLeft = BurstCooldownTime().SecondsToTicks();
            }
        }

        public override Verb AttackVerb
        {
            get
            {
                return Gun == null ? null : GunCompEq.verbTracker.PrimaryVerb;
            }
        }
        public Building_ShipTurretCE()
        {
            top = new TurretTop(this);
        }
        public override void TryStartShootSomething(bool canBeginBurstImmediately)
        {
            if (GroundDefenseMode)
            {
                // CE Logic
                base.TryStartShootSomething(canBeginBurstImmediately);
            }
            else
            {
                // SOS2 Logic
                bool isValid = currentTargetInt.IsValid;
                if (!Spawned || (holdFire && CanToggleHoldFire) || !AttackVerb.Available() || PointDefenseMode || mapComp.ShipMapState != ShipMapState.inCombat || SpinalHasNoAmps)
                {
                    ResetCurrentTarget();
                    return;
                }
                if (!PlayerControlled && mapComp.HasShipMapAI) //AI targeting
                {
                    // Target pawns with the Psychic Flayer
                    if (spinalComp != null && !spinalComp.Props.destroysHull && mapComp.ShipCombatTargetMap.mapPawns.FreeColonistsAndPrisoners.Any())
                    {
                        shipTarget = mapComp.ShipCombatTargetMap.mapPawns.FreeColonistsAndPrisoners.RandomElement();
                    }
                    else //try bridges, else random
                    {
                        if (mapComp.OriginMapComp.MapRootListAll.Any(b => !b.Destroyed))
                        {
                            shipTarget = mapComp.OriginMapComp.MapRootListAll.RandomElement();
                        }
                        else
                        {
                            shipTarget = mapComp.ShipCombatTargetMap.listerBuildings.allBuildingsColonist.RandomElement();
                        }
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
            if (GroundDefenseMode)
            {
                // CE Logic
                base.ResetForcedTarget();
            }
            else
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
            if (GroundDefenseMode)
            {
                return base.TryFindNewTarget();
            }
            else
            {
                return LocalTargetInfo.Invalid;
            }

        }

        public override void BeginBurst()
        {
            if (GroundDefenseMode) // Ground Logic - Added power and heat consumption per shot
            {
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
                base.BeginBurst();
            }
            else // Space Logic
            {
                if (spinalComp != null)
                {
                    SpinalRecalc();
                    if (AmplifierCount == -1)
                    {
                        return;
                    }

                    if ((Rotation == new Rot4(mapComp.EngineRot) && mapComp.Heading == -1) || (Rotation == new Rot4(mapComp.EngineRot + 2) && mapComp.Heading == 1))
                    {
                        if (mapComp.HasShipMapAI)
                        {
                            if (!mapComp.Retreating)
                            {
                                mapComp.Heading *= -1;
                            }
                        }
                    }
                }
                //check if we have power to fire
                if (powerComp != null && powerComp.PowerNet.CurrentStoredEnergy() < EnergyToFire)
                {
                    if (!PointDefenseMode && PlayerControlled)
                    {
                        Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CannotFireDueToPower", Label), this, MessageTypeDefOf.CautionInput);
                    }

                    shipTarget = LocalTargetInfo.Invalid;
                    ResetCurrentTarget();
                    return;
                }
                //if we do not have enough heatcap, vent heat to room/fail to fire in vacuum
                if (heatComp.Props.heatPerPulse > 0 && !heatComp.AddHeatToNetwork(HeatToFire))
                {
                    if (!PointDefenseMode && PlayerControlled)
                    {
                        Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CannotFireDueToHeat", Label), this, MessageTypeDefOf.CautionInput);
                    }

                    shipTarget = LocalTargetInfo.Invalid;
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
                //spinal weapons fire straight and destroy things in the way
                if (spinalComp != null)
                {
                    if (Rotation.AsByte == 0)
                    {
                        currentTargetInt = new LocalTargetInfo(new IntVec3(Position.x, 0, Map.Size.z - 1));
                    }
                    else if (Rotation.AsByte == 1)
                    {
                        currentTargetInt = new LocalTargetInfo(new IntVec3(Map.Size.x - 1, 0, Position.z));
                    }
                    else if (Rotation.AsByte == 2)
                    {
                        currentTargetInt = new LocalTargetInfo(new IntVec3(Position.x, 0, 1));
                    }
                    else
                    {
                        currentTargetInt = new LocalTargetInfo(new IntVec3(1, 0, Position.z));
                    }

                    if (spinalComp.Props.destroysHull)
                    {
                        List<Thing> thingsToDestroy = new List<Thing>();

                        if (Rotation.AsByte == 0)
                        {
                            for (int x = Position.x - 1; x <= Position.x + 1; x++)
                            {
                                for (int z = Position.z + 3; z < Map.Size.z; z++)
                                {
                                    IntVec3 vec = new IntVec3(x, 0, z);
                                    foreach (Thing thing in vec.GetThingList(Map))
                                    {
                                        thingsToDestroy.Add(thing);
                                    }
                                }
                            }
                        }
                        else if (Rotation.AsByte == 1)
                        {
                            for (int x = Position.x + 3; x < Map.Size.x; x++)
                            {
                                for (int z = Position.z - 1; z <= Position.z + 1; z++)
                                {
                                    IntVec3 vec = new IntVec3(x, 0, z);
                                    foreach (Thing thing in vec.GetThingList(Map))
                                    {
                                        thingsToDestroy.Add(thing);
                                    }
                                }
                            }
                        }
                        else if (Rotation.AsByte == 2)
                        {
                            for (int x = Position.x - 1; x <= Position.x + 1; x++)
                            {
                                for (int z = Position.z - 3; z > 0; z--)
                                {
                                    IntVec3 vec = new IntVec3(x, 0, z);
                                    foreach (Thing thing in vec.GetThingList(Map))
                                    {
                                        thingsToDestroy.Add(thing);
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (int x = 1; x <= Position.x - 3; x++)
                            {
                                for (int z = Position.z - 1; z <= Position.z + 1; z++)
                                {
                                    IntVec3 vec = new IntVec3(x, 0, z);
                                    foreach (Thing thing in vec.GetThingList(Map))
                                    {
                                        thingsToDestroy.Add(thing);
                                    }
                                }
                            }
                        }

                        foreach (Thing thing in thingsToDestroy)
                        {
                            GenExplosion.DoExplosion(thing.Position, thing.Map, 0.5f, DamageDefOf.Bomb, null);
                            if (!thing.Destroyed)
                            {
                                thing.Kill();
                            }
                        }
                    }
                }
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
                    SpinalRecalc();
                    selected = true;
                }
                if (!PlayerControlled || SpinalHasNoAmps)
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
                if (torpComp != null)
                {
                    if (CanExtractTorpedo)
                    {
                        Command_Action command_Action = new Command_Action
                        {
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.TurretExtractTorpedo"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.TurretExtractTorpedoDesc"),
                            icon = torpComp.LoadedShells[0].uiIcon,
                            iconAngle = torpComp.LoadedShells[0].uiIconAngle,
                            iconOffset = torpComp.LoadedShells[0].uiIconOffset,
                            iconDrawScale = GenUI.IconDrawScale(torpComp.LoadedShells[0]),
                            action = delegate
                            {
                                ExtractShells();
                            }
                        };
                        yield return command_Action;
                        Command_Action command_ExtractOne = new Command_Action
                        {
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.TurretExtractOneTorpedo"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.TurretExtractOneTorpedoDesc"),
                            icon = torpComp.LoadedShells[0].uiIcon,
                            iconAngle = torpComp.LoadedShells[0].uiIconAngle,
                            iconOffset = torpComp.LoadedShells[0].uiIconOffset,
                            iconDrawScale = GenUI.IconDrawScale(torpComp.LoadedShells[0]),
                            action = delegate
                            {
                                ExtractOneShellMenu();
                            }
                        };
                        yield return command_ExtractOne;
                    }
                    List<ThingDef> torpTypes = new List<ThingDef>();
                    foreach (ThingDef torp in torpComp.LoadedShells)
                    {
                        if (!torpTypes.Contains(torp))
                        {
                            torpTypes.Add(torp);
                        }
                    }
                    torpTypes = torpTypes.OrderBy(t => t.defName).ToList();
                    foreach (ThingDef torp in torpTypes)
                    {
                        Command_Toggle command_Toggle = new Command_Toggle
                        {
                            defaultLabel = torp.label,
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.TorpedoAllowedDesc"),
                            icon = torp.uiIcon,
                            iconAngle = torp.uiIconAngle,
                            iconOffset = torp.uiIconOffset,
                            iconDrawScale = GenUI.IconDrawScale(torp),
                            toggleAction = delegate
                            {
                                if (torpComp.PreventShells.Contains(torp))
                                {
                                    torpComp.PreventShells.Remove(torp);
                                }
                                else
                                {
                                    torpComp.PreventShells.Add(torp);
                                }
                            },
                            isActive = () => !torpComp.PreventShells.Contains(torp)
                        };
                        yield return command_Toggle;
                    }
                    StorageSettings storeSettings = torpComp.GetStoreSettings();
                    foreach (Gizmo item in StorageSettingsClipboard.CopyPasteGizmosFor(storeSettings))
                    {
                        yield return item;
                    }
                }
                else if (heatComp.Props.pointDefense)
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

        public override void SpawnSetup(Map map, bool respawningAfterLoad) // Add all the SOS2 fields and setyup GroundDefenseMode
        {
            base.SpawnSetup(map, respawningAfterLoad);

            mapComp = map.GetComponent<ShipMapComp>();
            heatComp = this.TryGetComp<CompShipHeat>();
            fuelComp = this.TryGetComp<CompRefuelable>();
            spinalComp = this.TryGetComp<CompSpinalMount>();
            torpComp = Gun.TryGetComp<SaveOurShip2.CompChangeableProjectile>();

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
            if (GroundDefenseMode)
            {
                // CE Logic
                base.OrderAttack(targ);
            }
            else
            {
                // SOS2 Logic
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
            if (GroundDefenseMode)
            {
                base.Tick();
            }
            else
            {
                // Can't call base.Tick() as we don't want to call the CE Logic here, so therefore we have to call the same logic as Building_Turret and ThingWithComps first
                // ThingWithComps Tick
                if (comps != null)
                {
                    int i = 0;
                    for (int count = comps.Count; i < count; i++)
                    {
                        comps[i].CompTick();
                    }
                }
                // Building_Turret logic
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
                if (mapComp.ShipMapState != ShipMapState.inCombat || SpinalHasNoAmps)
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
        public override void DrawExtraSelectionOverlays() // SOS2 Only has an implementation for ground defense mode so its not included here.
        {
            if (GroundDefenseMode)
            {
                base.DrawExtraSelectionOverlays();
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
        #endregion

        #region SOS2 Functionality
        public bool PointDefenseMode;
        //public Thing gun;
        public float AmplifierDamageBonus = 0;
        private bool selected = false;
        public bool useOptimalRange;
        public IntVec3 SynchronizedBurstLocation;
        LocalTargetInfo shipTarget = LocalTargetInfo.Invalid;
        public int AmplifierCount = -1;
        List<Thing> amps = new List<Thing>();
        public float EnergyToFire => heatComp.Props.energyToFire * (1 + AmplifierDamageBonus);
        public float HeatToFire => heatComp.Props.heatPerPulse * (1 + AmplifierDamageBonus) * 3;

        public ShipMapComp mapComp;
        public CompSpinalMount spinalComp;
        public CompShipHeat heatComp;
        public CompRefuelable fuelComp;
        public SaveOurShip2.CompChangeableProjectile torpComp;
        public bool SpinalHasNoAmps => spinalComp != null && AmplifierCount == -1;

        private bool CanExtractTorpedo
        {
            get
            {
                if (!PlayerControlled)
                {
                    return false;
                }
                return torpComp?.Loaded ?? false;
            }
        }
        private void ExtractShells()
        {
            foreach (Thing t in torpComp.RemoveShells())
            {
                GenPlace.TryPlaceThing(t, base.Position, base.Map, ThingPlaceMode.Near);
            }
        }
        public bool InRangeSC(float range)
        {
            if ((!useOptimalRange && heatComp.Props.maxRange > range) || (useOptimalRange && heatComp.Props.optRange > range))
            {
                return true;
            }
            return false;
        }
        private void ExtractOneShellMenu()
        {
            List<ThingDef> loadedTypes = new List<ThingDef>();
            foreach (ThingDef torp in torpComp.LoadedShells)
            {
                if (!loadedTypes.Contains(torp))
                {
                    loadedTypes.Add(torp);
                }
            }
            List<FloatMenuOption> unloadOptions = new List<FloatMenuOption>();
            foreach (ThingDef loadedType in loadedTypes)
            {
                String commandText = TranslatorFormattedStringExtensions.Translate("SoS.TurretExtract") + " " + loadedType.label;
                unloadOptions.Add(new FloatMenuOption(commandText, delegate
                {
                    Thing torp = torpComp.RemoveOneShellOfType(loadedType);
                    GenPlace.TryPlaceThing(torp, base.Position, base.Map, ThingPlaceMode.Near);
                }, loadedType.uiIcon, Color.white));

            }
            // Having options in the UI in different order, depending on torp loading order is annoyng,
            // like may have 2 launchers with same torpedoes loaded, but options in diffrrent order.
            unloadOptions = unloadOptions.OrderBy(x => x.Label).ToList();
            Find.WindowStack.Add(new FloatMenu(unloadOptions));
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

        public void SpinalRecalc()
        {
            if (spinalComp == null)
            {
                return;
            }

            if (AmplifierCount != -1 && amps.All(a => a != null && !a.Destroyed)) //no changes if all amps intact
            {
                return;
            }

            amps.Clear();
            AmplifierCount = -1;
            float ampBoost = 0;
            bool foundNonAmp = false;
            Thing amp = this;
            IntVec3 previousThingPos;
            IntVec3 vec;
            if (Rotation.AsByte == 0)
            {
                vec = new IntVec3(0, 0, -1);
            }
            else if (Rotation.AsByte == 1)
            {
                vec = new IntVec3(-1, 0, 0);
            }
            else if (Rotation.AsByte == 2)
            {
                vec = new IntVec3(0, 0, 1);
            }
            else
            {
                vec = new IntVec3(1, 0, 0);
            }
            previousThingPos = amp.Position + vec;
            do
            {
                previousThingPos += vec;
                amp = previousThingPos.GetFirstThingWithComp<CompSpinalMount>(Map);
                CompSpinalMount ampComp = amp.TryGetComp<CompSpinalMount>();
                if (amp == null || amp.Rotation != Rotation)
                {
                    AmplifierCount = -1;
                    break;
                }
                //found amp
                if (amp.Position == previousThingPos)
                {
                    amps.Add(amp);
                    AmplifierCount += 1;
                    ampBoost += ampComp.Props.ampAmount;
                    ampComp.SetColor(spinalComp.Props.color);
                }
                //found emitter
                else if (amp.Position == previousThingPos + vec && ampComp.Props.stackEnd)
                {
                    amps.Add(amp);
                    AmplifierCount += 1;
                    foundNonAmp = true;
                }
                //found unaligned
                else
                {
                    AmplifierCount = -1;
                    foundNonAmp = true;
                }
            } while (!foundNonAmp);

            if (ampBoost > 0)
            {
                AmplifierDamageBonus = ampBoost;
            }
        }
        public bool IncomingPtDefTargetsInRange() //PD targets are in range if they are on target map and in PD range
        {
            if (mapComp.TargetMapComp.TorpsInRange.Any() || mapComp.TargetMapComp.ShuttlesInRange.Where(shuttle => shuttle.Faction != this.Faction).Any())
            {
                return true;
            }
            return false;
        }
        public bool InRange(LocalTargetInfo target)
        {
            float range = Position.DistanceTo(target.Cell);
            if (range > AttackVerb.verbProps.minRange && range < AttackVerb.verbProps.range)
            {
                return true;
            }
            return false;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish) // CE has no custom logic for destroy
        {
            Map map = Map;
            base.Destroy(mode);
            if (torpComp != null && !ShipInteriorMod2.MoveShipFlag)
            {
                foreach (ThingDef def in torpComp.LoadedShells)
                {
                    Thing thing = ThingMaker.MakeThing(def);
                    GenPlace.TryPlaceThing(thing, Position, map, ThingPlaceMode.Near, null, null, default);
                }
            }
        }
        public void SetTarget(LocalTargetInfo target)
        {
            shipTarget = target;
        }
        #endregion

    }
}
