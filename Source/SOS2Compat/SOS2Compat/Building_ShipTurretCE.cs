using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CombatExtended.CombatExtended.Jobs.Utils;
using CombatExtended.CombatExtended.LoggerUtils;
using CombatExtended.WorldObjects;
using RimWorld;
using RimWorld.Planet;
using SaveOurShip2;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using Verse.Sound;

namespace CombatExtended.Compatibility
{
    public class Building_ShipTurretCE : Building_Turret
    {
        // TODO: Fix up the layout/formatting of this class
        // TODO: Fix Autocannons not consuming ammo
        #region License
        // Any SOS2 Code used for compatibility has been taken from the following source and is licensed under the "Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International Public License"
        // https://github.com/KentHaeger/SaveOurShip2/blob/cf179981d242764af20c41440d69649e6ecd6450/Source/1.5/Building/Building_ShipTurret.cs
        #endregion

        #region Shared

        public bool holdFire;
        public bool GroundDefenseMode;
        public int burstCooldownTicksLeft;
        public int burstWarmupTicksLeft;
        public TurretTop top;
        public override LocalTargetInfo CurrentTarget => currentTargetInt;
        public LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;

        
        public bool PlayerControlled => (Faction == Faction.OfPlayer || MannedByColonist) && !MannedByNonColonist; // Manned part added by CE
        private bool CanToggleHoldFire => PlayerControlled;
        private bool WarmingUp => burstWarmupTicksLeft > 0;
        public CompEquippable GunCompEq => Gun.TryGetComp<CompEquippable>();
        public CompPowerTrader powerComp;
        public CompInitiatable initiatableComp;

        public static implicit operator Building_ShipTurret(Building_ShipTurretCE turretCE)
        {
            return new ShipTurretWrapperCE(turretCE);
        }

        public Thing Gun
        {
            get
            {
                if (this.gunInt == null && Map != null)
                {
                    // I am leaving this here because god knows what uses it before postmake gets called.
                    CELogger.Warn($"Gun {this.ToString()} was referenced before PostMake. If you're seeing this, please report this to the Combat Extended team!", showOutOfDebugMode: true);
                    MakeGun();

                    if (!everSpawned && (!Map.IsPlayerHome || Faction != Faction.OfPlayer))
                    {
                        compAmmo?.ResetAmmoCount();
                        everSpawned = true;
                    }
                }
                return this.gunInt;
            }
        }

        private bool CanSetForcedTarget
        {
            get
            {
                return true;
            }
        }

        private void MakeGun()
        {
            this.gunInt = ThingMaker.MakeThing(this.def.building.turretGunDef, null);
            this.compAmmo = gunInt.TryGetComp<CompAmmoUser>();
            InitGun();
        }

        public float BurstCooldownTime()
        {
            if (def.building.turretBurstCooldownTime >= 0f)
            {
                return def.building.turretBurstCooldownTime;
            }
            return AttackVerb.verbProps.defaultCooldownTime;
        }

        public void BurstComplete() 
        {
            burstCooldownTicksLeft = BurstCooldownTime().SecondsToTicks();
            if (GroundDefenseMode)
            {
                if (CompAmmo != null && CompAmmo.CurMagCount <= 0)
                {
                    TryForceReload();
                }
            }

        }

        public override Verb AttackVerb
        {
            get
            {
                return Gun == null ? null : GunCompEq.verbTracker.PrimaryVerb;
            }
        }

        public void ResetCurrentTarget() // Core method
        {
            this.currentTargetInt = LocalTargetInfo.Invalid;
            this.burstWarmupTicksLeft = 0;
        }
        public Building_ShipTurretCE()
        {
            top = new TurretTop(this);
        }
        public void TryStartShootSomething(bool canBeginBurstImmediately)
        {
            if (GroundDefenseMode)
            {
                // CE Logic
                if (!Spawned || (holdFire && CanToggleHoldFire) || (Projectile.projectile.flyOverhead && Map.roofGrid.Roofed(Position)) || (CompAmmo != null && (isReloading || (mannableComp == null && CompAmmo.CurMagCount <= 0))))
                {
                    ResetCurrentTarget();
                    return;
                }
                //Copied and modified from Verb_LaunchProjectileCE
                if (!isReloading && (Projectile == null || (CompAmmo != null && !CompAmmo.CanBeFiredNow)))
                {
                    ResetCurrentTarget();
                    TryOrderReload();
                    return;
                }
                bool isValid = currentTargetInt.IsValid || globalTargetInfo.IsValid;
                currentTargetInt = forcedTarget.IsValid ? forcedTarget : TryFindNewTarget();
                if (!isValid && (currentTargetInt.IsValid || targetingWorldMap))
                {
                    SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(Position, Map, false));
                }
                if (!targetingWorldMap && !currentTargetInt.IsValid)
                {
                    ResetCurrentTarget();
                    return;
                }
                // Use verb warmup time instead of turret's
                if (AttackVerb.verbProps.warmupTime > 0f)
                {
                    burstWarmupTicksLeft = AttackVerb.verbProps.warmupTime.SecondsToTicks();
                    return;
                }
                if (targetingWorldMap && (!globalTargetInfo.IsValid || globalTargetInfo.WorldObject is DestroyedSettlement))
                {
                    ResetForcedTarget();
                    ResetCurrentTarget();
                    return;
                }
                if (canBeginBurstImmediately)
                {
                    BeginBurst();
                    return;
                }
                burstWarmupTicksLeft = 1;
            }
            else
            {
                // SOS2 Logic
                bool isValid = currentTargetInt.IsValid;
                if (!base.Spawned || (holdFire && CanToggleHoldFire) || !AttackVerb.Available() || PointDefenseMode || mapComp.ShipMapState != ShipMapState.inCombat || SpinalHasNoAmps)
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
                    SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
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

        public void ResetForcedTarget()
        {
            if (GroundDefenseMode)
            {
                // CE Logic
                this.targetingWorldMap = false;
                this.forcedTarget = LocalTargetInfo.Invalid;
                this.globalTargetInfo = GlobalTargetInfo.Invalid;
                this.burstWarmupTicksLeft = 0;
                if (this.burstCooldownTicksLeft <= 0)
                {
                    this.TryStartShootSomething(false);
                }
            } else
            {
                shipTarget = LocalTargetInfo.Invalid;
                burstWarmupTicksLeft = 0;
                if ((mapComp.ShipMapState == ShipMapState.inCombat || GroundDefenseMode) && burstCooldownTicksLeft <= 0)
                {
                    TryStartShootSomething(false);
                }
            }
        }

        public LocalTargetInfo TryFindNewTarget()
        {
            if (GroundDefenseMode)
            {
                IAttackTargetSearcher attackTargetSearcher = this.TargSearcher();
                Faction faction = attackTargetSearcher.Thing.Faction;
                float range = this.AttackVerb.verbProps.range;
                Building t;
                if (Rand.Value < 0.5f && this.AttackVerb.ProjectileFliesOverhead() && faction.HostileTo(Faction.OfPlayer) && base.Map.listerBuildings.allBuildingsColonist.Where(delegate (Building x)
                {
                    float num = this.AttackVerb.verbProps.EffectiveMinRange(x, this);
                    float num2 = (float)x.Position.DistanceToSquared(this.Position);
                    return num2 > num * num && num2 < range * range;
                }).TryRandomElement(out t))
                {
                    return t;
                }
                TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat;
                if (!Projectile.projectile.flyOverhead)
                {
                    targetScanFlags |= TargetScanFlags.NeedLOSToAll;
                    targetScanFlags |= TargetScanFlags.LOSBlockableByGas;
                }
                else
                {
                    targetScanFlags |= TargetScanFlags.NeedNotUnderThickRoof;
                }

                if (this.AttackVerb.IsIncendiary_Ranged())
                {
                    targetScanFlags |= TargetScanFlags.NeedNonBurning;
                }
                return (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(
                           attackTargetSearcher,
                           targetScanFlags,
                           this.IsValidTarget,
                           minDistance: 0f,
                           // Only consider targets within the maximum range of this turret
                           // to avoid iterating over potential targets that it can't reach.
                           maxDistance: range
                       );
            }
            else
            {
                return LocalTargetInfo.Invalid;
            }
            
        }

        private bool IsValidTarget(Thing t)
        {
            if (GroundDefenseMode)
            {
                Pawn pawn = t as Pawn;
                if (pawn != null)
                {
                    if (this.mannableComp == null)
                    {
                        return !GenAI.MachinesLike(base.Faction, pawn);
                    }
                    if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                if (t is Pawn p)
                {
                    if (p.Faction == Faction.OfPlayer)
                    {
                        return false;
                    }
                    foreach (Thing thing in t.Position.GetThingList(Map))
                    {
                        if (thing is Building b && b.def.building.shipPart)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        private IAttackTargetSearcher TargSearcher()    // CE Modified for mannable
        {
            if (GroundDefenseMode && mannableComp != null && mannableComp.MannedNow)
            {
                return mannableComp.ManningPawn;
            }
            return this;
        }

        public void BeginBurst()                     // Added handling for ticksUntilAutoReload
        {
            if (GroundDefenseMode) // Ground Logic
            {
                ticksUntilAutoReload = minTicksBeforeAutoReload;

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
                //draw the same percentage from each cap: needed*current/currenttotal
                foreach (CompPowerBattery bat in powerComp.PowerNet.batteryComps)
                {
                    bat.DrawPower(Mathf.Min(EnergyToFire * bat.StoredEnergy / powerComp.PowerNet.CurrentStoredEnergy(), bat.StoredEnergy));
                }
                AttackVerb.TryStartCastOn(CurrentTarget, false, true);

                OnAttackedTarget(CurrentTarget);
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
                if (GroundDefenseMode)
                {
                    AttackVerb.TryStartCastOn(CurrentTarget, false, true, false);
                    base.OnAttackedTarget(CurrentTarget);
                }
                else
                {
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
                    BurstComplete(); //Seems to prevent the "turbo railgun" bug. Don't ask me why.
                }
            }

        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (GroundDefenseMode)
            {
                // CE Gizmos
                // Ammo gizmos
                if (CompAmmo != null && (PlayerControlled || Prefs.DevMode))
                {
                    foreach (Command com in CompAmmo.CompGetGizmosExtra())
                    {
                        if (!PlayerControlled && Prefs.DevMode && com is GizmoAmmoStatus)
                        {
                            (com as GizmoAmmoStatus).prefix = "DEV: ";
                        }

                        yield return com;
                    }
                }
                // Don't show CONTROL gizmos on enemy turrets (even with dev mode enabled)
                if (PlayerControlled)
                {
                    // Fire mode gizmos
                    if (CompFireModes != null)
                    {
                        foreach (Command com in CompFireModes.GenerateGizmos())
                        {
                            yield return com;
                        }
                    }
                    // Set forced target gizmo
                    if (CanSetForcedTarget)
                    {
                        var vt = new Command_VerbTarget
                        {
                            defaultLabel = "CommandSetForceAttackTarget".Translate(),
                            defaultDesc = "CommandSetForceAttackTargetDesc".Translate(),
                            icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                            verb = AttackVerb, 
                            hotKey = KeyBindingDefOf.Misc4
                        };
                        if (Spawned && IsMortarOrProjectileFliesOverhead && Position.Roofed(Map))
                        {
                            vt.Disable("CannotFire".Translate() + ": " + "Roofed".Translate().CapitalizeFirst());
                        }
                        yield return vt;
                    }
                    // Stop forced attack gizmo
                    if (forcedTarget.IsValid)
                    {
                        Command_Action stop = new Command_Action();
                        stop.defaultLabel = "CommandStopForceAttack".Translate();
                        stop.defaultDesc = "CommandStopForceAttackDesc".Translate();
                        stop.icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true);
                        stop.action = delegate
                        {
                            SyncedResetForcedTarget();
                            SoundDefOf.Tick_Low.PlayOneShotOnCamera(null);
                        };
                        if (!this.forcedTarget.IsValid)
                        {
                            stop.Disable("CommandStopAttackFailNotForceAttacking".Translate());
                        }
                        stop.hotKey = KeyBindingDefOf.Misc5;
                        yield return stop;
                    }
                    // Toggle fire gizmo
                    if (CanToggleHoldFire)
                    {
                        yield return new Command_Toggle
                        {
                            defaultLabel = "CommandHoldFire".Translate(),
                            defaultDesc = "CommandHoldFireDesc".Translate(),
                            icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire", true),
                            hotKey = KeyBindingDefOf.Misc6,
                            toggleAction = ToggleHoldFire,
                            isActive = (() => holdFire)
                        };
                    }
                }
            }
            else
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

        public override string GetInspectString()       // Replaced vanilla loaded text with CE reloading
        {
            if (GroundDefenseMode)
            {
                StringBuilder stringBuilder = new StringBuilder();
                string inspectString = base.GetInspectString();
                if (!inspectString.NullOrEmpty())
                {
                    stringBuilder.AppendLine(inspectString);
                }
                stringBuilder.AppendLine("GunInstalled".Translate() + ": " + this.Gun.LabelCap);
                if (AttackVerb.verbProps.minRange > 0f)
                {
                    stringBuilder.AppendLine("MinimumRange".Translate() + ": " + AttackVerb.verbProps.minRange.ToString("F0"));
                }

                if (isReloading)
                {
                    stringBuilder.AppendLine("CE_TurretReloading".Translate());
                }
                else if (Spawned && IsMortarOrProjectileFliesOverhead && Position.Roofed(Map))
                {
                    stringBuilder.AppendLine("CannotFire".Translate() + ": " + "Roofed".Translate().CapitalizeFirst());
                }
                else if (Spawned && burstCooldownTicksLeft > 0)
                {
                    stringBuilder.AppendLine("CanFireIn".Translate() + ": " + this.burstCooldownTicksLeft.ToStringSecondsFromTicks());
                }
                return stringBuilder.ToString().TrimEndNewlines();
            } 
            else
            {
                StringBuilder stringBuilder = new StringBuilder();
                string inspectString = base.GetInspectString();
                if (!inspectString.NullOrEmpty())
                {
                    stringBuilder.AppendLine(inspectString);
                }
                if (AttackVerb.verbProps.minRange > 0f && GroundDefenseMode)
                {
                    stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("MinimumRange") + ": " + AttackVerb.verbProps.minRange.ToString("F0"));
                }
                if (base.Spawned && burstCooldownTicksLeft > 0 && BurstCooldownTime() > 5f)
                {
                    stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("CanFireIn") + ": " + burstCooldownTicksLeft.ToStringSecondsFromTicks());
                }
                if (spinalComp != null)
                {
                    // Do not show numer of amplifiers when room with capacitor and amplifiers is fogged
                    if (!(Position + GenAdj.CardinalDirectionsAround[Rotation.rotInt] * 2).Fogged(Map))
                    {
                        if (AmplifierCount != -1)
                        {
                            stringBuilder.AppendLine("SoS.AmplifierCount".Translate(AmplifierCount));
                        }
                        else
                        {
                            stringBuilder.AppendLine("SoS.SpinalCapNotFound".Translate());
                        }
                    }
                }
                if (torpComp != null)
                {
                    if (torpComp.Loaded)
                    {
                        int torp = 0;
                        int torpEMP = 0;
                        int torpAM = 0;
                        foreach (ThingDef t in torpComp.LoadedShells)
                        {
                            if (t == ResourceBank.ThingDefOf.ShipTorpedo_EMP)
                            {
                                torpEMP++;
                            }
                            else if (t == ResourceBank.ThingDefOf.ShipTorpedo_Antimatter)
                            {
                                torpAM++;
                            }
                            else
                            {
                                torp++;
                            }
                        }
                        if (torp > 0)
                        {
                            stringBuilder.AppendLine(torp + " HE torpedoes");
                        }
                        if (torpEMP > 0)
                        {
                            stringBuilder.AppendLine(torpEMP + " EMP torpedoes");
                        }
                        if (torpAM > 0)
                        {
                            stringBuilder.AppendLine(torpAM + " AM torpedoes");
                        }
                    }
                    else
                    {
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.TorpedoNotLoaded"));
                    }
                }
                return stringBuilder.ToString().TrimEndNewlines();
            }
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)      //Add mannableComp, ticksUntilAutoReload, register to GenClosestAmmo
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Map.GetComponent<TurretTracker>().Register(this);

            dormantComp = GetComp<CompCanBeDormant>();
            initiatableComp = GetComp<CompInitiatable>();
            mannableComp = GetComp<CompMannable>();
            mapComp = Map.GetComponent<ShipMapComp>();
            powerComp = this.TryGetComp<CompPowerTrader>();
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
            if (GroundDefenseMode)
            {
                if (!everSpawned && (!Map.IsPlayerHome || Faction != Faction.OfPlayer))
                {
                    compAmmo?.ResetAmmoCount();
                    everSpawned = true;
                }

                if (!respawningAfterLoad)
                {
                    CELogger.Message($"top is {top?.ToString() ?? "null"}");
                    top.SetRotationFromOrientation();
                    burstCooldownTicksLeft = def.building.turretInitialCooldownTime.SecondsToTicks();

                    //Delay auto-reload for a few seconds after spawn, so player can operate the turret right after placing it, before other colonists start reserving it for reload jobs
                    if (mannableComp != null)
                    {
                        ticksUntilAutoReload = minTicksBeforeAutoReload;
                    }
                }
            } else
            {
                if (!respawningAfterLoad)
                {
                    top.SetRotationFromOrientation();
                    burstCooldownTicksLeft = def.building.turretInitialCooldownTime.SecondsToTicks();
                    ResetForcedTarget();
                }
            }

            
        }
        public override void ExposeData()
        {
            base.ExposeData();
            // New variables                        
            Scribe_Deep.Look(ref gunInt, "gunInt");
            InitGun();
            Scribe_Values.Look(ref isReloading, "isReloading", false);
            Scribe_Values.Look(ref ticksUntilAutoReload, "ticksUntilAutoReload", 0);

            Scribe_Values.Look<int>(ref this.burstCooldownTicksLeft, "burstCooldownTicksLeft", 0, false);
            Scribe_Values.Look<int>(ref this.burstWarmupTicksLeft, "burstWarmupTicksLeft", 0, false);
            Scribe_TargetInfo.Look(ref this.currentTargetInt, "currentTarget");
            Scribe_Values.Look<bool>(ref this.holdFire, "holdFire", false, false);
            Scribe_Values.Look<bool>(ref this.everSpawned, "everSpawned", false, false);

            Scribe_TargetInfo.Look(ref globalTargetInfo, "globalSourceInfo");

            Scribe_Values.Look<IntVec3>(ref SynchronizedBurstLocation, "burstLocation");
            Scribe_Values.Look<bool>(ref PointDefenseMode, "pointDefenseMode");
            Scribe_Values.Look<bool>(ref useOptimalRange, "useOptimalRange");

            BackCompatibility.PostExposeData(this);
        }
        public override void PostMake()
        {
            base.PostMake();
            MakeGun();
        }
        public override void OrderAttack(LocalTargetInfo targ)
        {
            if (GroundDefenseMode)
            {
                // CE Logic
                if (globalTargetInfo.IsValid)
                {
                    this.ResetForcedTarget();
                }
                if (!targ.IsValid)
                {
                    if (this.forcedTarget.IsValid)
                    {
                        this.ResetForcedTarget();
                    }
                    return;
                }
                if ((targ.Cell - base.Position).LengthHorizontal < AttackVerb.verbProps.minRange)
                {
                    Messages.Message("MessageTargetBelowMinimumRange".Translate(), this, MessageTypeDefOf.RejectInput);
                    return;
                }
                if ((targ.Cell - base.Position).LengthHorizontal > AttackVerb.verbProps.range)
                {
                    Messages.Message("MessageTargetBeyondMaximumRange".Translate(), this, MessageTypeDefOf.RejectInput);
                    return;
                }
                if (this.forcedTarget != targ)
                {
                    this.forcedTarget = targ;
                    if (this.burstCooldownTicksLeft <= 0)
                    {
                        this.TryStartShootSomething(false);
                    }
                }
                if (this.holdFire)
                {
                    Messages.Message("MessageTurretWontFireBecauseHoldFire".Translate(this.def.label), this, MessageTypeDefOf.RejectInput, false);
                }
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

        public override void Tick()     //Autoreload code and IsReloading check
        {
            base.Tick();
            if (GroundDefenseMode)
            {
                if (ticksUntilAutoReload > 0)
                {
                    ticksUntilAutoReload--;    // Reduce time until we can auto-reload
                }

                if (!isReloading && this.IsHashIntervalTick(TicksBetweenAmmoChecks))
                {
                    if (MannableComp?.MannedNow ?? false)
                    {
                        TryOrderReload();
                    }
                    else
                    {
                        TryReloadViaAutoLoader();
                    }
                }

                //This code runs TryOrderReload for manning pawns or for non-humanlike intelligence such as mechs
                /*if (this.IsHashIntervalTick(TicksBetweenAmmoChecks) && !isReloading && (MannableComp?.MannedNow ?? false))
                      TryOrderReload(CompAmmo?.CurMagCount == 0);*/
                if (!CanSetForcedTarget && !isReloading && forcedTarget.IsValid && !globalTargetInfo.IsValid && burstCooldownTicksLeft <= 0)
                {
                    ResetForcedTarget();
                }
                if (!CanToggleHoldFire)
                {
                    holdFire = false;
                }
                if (forcedTarget.ThingDestroyed)
                {
                    ResetForcedTarget();
                }
                if (Active && (this.mannableComp == null || this.mannableComp.MannedNow) && base.Spawned && !(isReloading && WarmingUp))
                {
                    this.GunCompEq.verbTracker.VerbsTick();
                    if (!IsStunned && AttackVerb.state != VerbState.Bursting)
                    {
                        if (this.WarmingUp)
                        {
                            this.burstWarmupTicksLeft--;
                            if (this.burstWarmupTicksLeft == 0)
                            {
                                this.BeginBurst();
                            }
                        }
                        else
                        {
                            if (this.burstCooldownTicksLeft > 0)
                            {
                                this.burstCooldownTicksLeft--;
                            }
                            if (this.burstCooldownTicksLeft <= 0)
                            {
                                this.TryStartShootSomething(true);
                            }
                        }
                        this.top.TurretTopTick();
                        return;
                    }
                }
                else
                {
                    this.ResetCurrentTarget();
                }
            }
            else
            {
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

        public bool Active //needs power, heat and bridge on net
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
                float range = AttackVerb.verbProps.range;
                if (range < 90f)
                {
                    GenDraw.DrawRadiusRing(base.Position, range);
                }
                float minRange = AttackVerb.verbProps.minRange;     // Changed to minRange instead of EffectiveMinRange
                if (minRange < 90f && minRange > 0.1f)
                {
                    GenDraw.DrawRadiusRing(base.Position, minRange);
                }
                if (this.WarmingUp)
                {
                    int degreesWide = (int)((float)this.burstWarmupTicksLeft * 0.5f);
                    GenDraw.DrawAimPie(this, this.CurrentTarget, degreesWide, def.size.x * 0.5f);
                }
                if (this.forcedTarget.IsValid && (!this.forcedTarget.HasThing || this.forcedTarget.Thing.Spawned))
                {
                    Vector3 b;
                    if (this.forcedTarget.HasThing)
                    {
                        b = this.forcedTarget.Thing.TrueCenter();
                    }
                    else
                    {
                        b = this.forcedTarget.Cell.ToVector3Shifted();
                    }
                    Vector3 a = this.TrueCenter();
                    b.y = Altitudes.AltitudeFor(AltitudeLayer.MetaOverlays);
                    a.y = b.y;
                    GenDraw.DrawLineBetween(a, b, Building_TurretGun.ForcedTargetLineMat);
                }
            }
            else
            {

            }
        }
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish) // Same except for CE addition
        {
            Map.GetComponent<TurretTracker>().Unregister(this); // CE addition
            base.DeSpawn(mode);
            ResetCurrentTarget();
        }
        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (GroundDefenseMode)
            {
                Vector3 drawOffset = Vector3.zero;
                float angleOffset = 0f;
                if (Controller.settings.RecoilAnim)
                {
                    CE_Utility.Recoil(def.building.turretGunDef, AttackVerb, out drawOffset, out angleOffset, top.CurRotation, false); // TODO: Check
                }
                top.DrawTurret(drawLoc, drawOffset, angleOffset);
                base.DrawAt(drawLoc, flip);
            }
            else
            {
                top.DrawTurret(DrawPos, Vector3.zero, 0);
                base.DrawAt(drawLoc, flip);
            }

        }
        #endregion

        #region CE
        private const int minTicksBeforeAutoReload = 1800;              // This much time must pass before haulers will try to automatically reload an auto-turret
        private const int ticksBetweenAmmoChecks = 300;                 // Test nearby ammo every 5 seconds if there's many ammo changes
        private const int ticksBetweenSlowAmmoChecks = 3600;               // Test nearby ammo every minute if there's no ammo changes
        public bool isSlow = false;
        private int TicksBetweenAmmoChecks => isSlow ? ticksBetweenSlowAmmoChecks : ticksBetweenAmmoChecks;

        private Thing gunInt;
        private bool everSpawned = false;
        private int ticksUntilAutoReload = 0;
        public bool targetingWorldMap = false;
        public bool isReloading = false;
        public bool IsMannable => mannableComp != null;
        private bool MannedByColonist => mannableComp != null && mannableComp.ManningPawn != null && mannableComp.ManningPawn.Faction == Faction.OfPlayer;
        private bool MannedByNonColonist => mannableComp != null && mannableComp.ManningPawn != null && mannableComp.ManningPawn.Faction != Faction.OfPlayer;

        public bool IsMortar => def.building.IsMortar;
        public bool IsMortarOrProjectileFliesOverhead => Projectile.projectile.flyOverhead || IsMortar;

        private CompAmmoUser compAmmo = null;
        private CompFireModes compFireModes = null;
        public CompMannable mannableComp;
        public CompMannable MannableComp => mannableComp;
        public CompCanBeDormant dormantComp;
        private RimWorld.CompChangeableProjectile compChangeable = null;

        public GlobalTargetInfo globalTargetInfo = GlobalTargetInfo.Invalid;

        public bool Reloadable => CompAmmo?.HasMagazine ?? false;

        public CompAmmoUser CompAmmo
        {
            get
            {
                if (compAmmo == null && Gun != null)
                {
                    compAmmo = Gun.TryGetComp<CompAmmoUser>();
                }
                return compAmmo;
            }
        }
        public CompFireModes CompFireModes
        {
            get
            {
                if (compFireModes == null && Gun != null)
                {
                    compFireModes = Gun.TryGetComp<CompFireModes>();
                }
                return compFireModes;
            }
        }
        public ThingDef Projectile
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
        public RimWorld.CompChangeableProjectile CompChangeable
        {
            get
            {
                if (compChangeable == null && Gun != null)
                {
                    compChangeable = Gun.TryGetComp<RimWorld.CompChangeableProjectile>();
                }
                return compChangeable;
            }
        }
        [Compatibility.Multiplayer.SyncMethod]
        private void SyncedResetForcedTarget() => ResetForcedTarget();
        [Compatibility.Multiplayer.SyncMethod]
        private void ToggleHoldFire()
        {
            holdFire = !holdFire;
            if (holdFire)
            {
                ResetForcedTarget();
            }
        }
        private void InitGun()
        {
            // Callback for ammo comp
            if (CompAmmo != null)
            {
                CompAmmo.turret = this;
            }
            List<Verb> allVerbs = this.gunInt.TryGetComp<CompEquippable>().AllVerbs;
            for (int i = 0; i < allVerbs.Count; i++)
            {
                Verb verb = allVerbs[i];
                verb.caster = this;
                verb.castCompleteCallback = new Action(this.BurstComplete);
            }
        }

        public void TryForceReload()
        {
            TryOrderReload(true);
        }

        public void TryOrderReload(bool forced = false)
        {
            //No reload necessary at all --
            if (compAmmo == null || (CompAmmo.CurrentAmmo == CompAmmo.SelectedAmmo && (!CompAmmo.HasMagazine || CompAmmo.CurMagCount == CompAmmo.MagSize)))
            {
                return;
            }

            if (TryReloadViaAutoLoader())
            {
                return;
            }

            //Non-mannableComp interaction
            if (!mannableComp?.MannedNow ?? true)
            {
                return;
            }

            //Only have manningPawn reload after a long time of no firing
            if (!forced && Reloadable && (compAmmo.CurMagCount != 0 || ticksUntilAutoReload > 0))
            {
                return;
            }

            //Already reserved for manning
            Pawn manningPawn = mannableComp.ManningPawn;
            if (manningPawn != null)
            {
                if (!JobGiverUtils_Reload.CanReload(manningPawn, this))
                {
                    return;
                }
                var jobOnThing = JobGiverUtils_Reload.MakeReloadJob(manningPawn, this);

                if (jobOnThing != null)
                {
                    manningPawn.jobs.StartJob(jobOnThing, JobCondition.Ongoing, null, manningPawn.CurJob?.def != CE_JobDefOf.ReloadTurret);
                }
            }
        }
        public bool TryReloadViaAutoLoader()
        {
            if (TargetCurrentlyAimingAt != null)
            {
                return false;
            }

            List<Thing> adjThings = new List<Thing>();
            GenAdjFast.AdjacentThings8Way(this, adjThings);

            foreach (Thing building in adjThings)
            {
                if (building is Building_AutoloaderCE container && container.StartReload(compAmmo))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region SOS2
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
