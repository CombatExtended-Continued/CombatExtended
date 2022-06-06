using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using CombatExtended;
using VFESecurity;

namespace CombatExtended.Compatibility.Artillery
{
    [StaticConstructorOnStartup]
    public class CompLongRangeArtilleryCE : CompLongRangeArtillery
    {
        private static new IEnumerable<CompLongRangeArtilleryCE> SelectedComps
        {
            get
            {
                var selectedObjects = Find.Selector.SelectedObjectsListForReading;
                for (int i = 0; i < selectedObjects.Count; i++)
                {
                    var obj = selectedObjects[i];
                    if (obj is Thing thing && thing.TryGetComp<CompLongRangeArtilleryCE>() is CompLongRangeArtilleryCE artilleryComp)
                        yield return artilleryComp;
                }
            }
        }

        public new Building_TurretGunCE Turret => (Building_TurretGunCE)parent;
        private new CompChangeableProjectile ChangeableProjectile => Turret.Gun.TryGetComp<CompChangeableProjectile>();
        private new CompRefuelable RefuelableComp => parent.GetComp<CompRefuelable>();
        private new CompPowerTrader PowerComp => parent.GetComp<CompPowerTrader>();
        private new CompMannable MannableComp => parent.GetComp<CompMannable>();

        public new bool CanLaunch => (PowerComp == null || PowerComp.PowerOn) && (RefuelableComp == null || RefuelableComp.HasFuel) && (Turret.CompAmmo == null || Turret.CompAmmo.HasAmmoOrMagazine)
            && (MannableComp == null || MannableComp.MannedNow) && Turret.burstCooldownTicksLeft <= 0 && !parent.OccupiedRect().Cells.Any(c => c.Roofed(parent.Map));


        public override void CompTick()
        {
            // Automatically attack if there is a forced target
            if (targetedTile != GlobalTargetInfo.Invalid)
            {
                var turretTop = Turret.top;
                NonPublicProperties.TurretTop_set_CurRotation(turretTop, CurAngle);
                NonPublicFields.TurretTop_ticksUntilIdleTurn.SetValue(turretTop, Rand.RangeInclusive(150, 350));
                if (CanLaunch)
                {
                    if (warmupTicksLeft == 0)
                    {
                        TryLaunch(targetedTile.Tile);
                        ResetWarmupTicks();
                    }
                    else
                        warmupTicksLeft--;
                }
            }

            // Set warmup ticks if the turret is unmanned
            if (MannableComp != null && !MannableComp.MannedNow)
                ResetWarmupTicks();
        }

        private new void ResetWarmupTicks()
        {
            warmupTicksLeft = Mathf.Max(1, Turret.def.building.turretBurstWarmupTime.SecondsToTicks());
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
	    // Don't want to do this to enemy artillery :P
            if (Turret.Faction == Faction.OfPlayer)
            {
                // Target other map tiles
                yield return new Command_Action()
                {
                    defaultLabel = "VFESecurity.TargetWorldTile".Translate(),
                    defaultDesc = "VFESecurity.TargetWorldTile_Description".Translate(parent.def.label),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/ArtilleryTargetTile"),
                    action = () => StartChoosingTarget()
                };

                // Cancel targeting
                if (targetedTile != GlobalTargetInfo.Invalid)
                {
                    yield return new Command_Action()
                    {
                        defaultLabel = "CommandStopForceAttack".Translate(),
                        defaultDesc = "CommandStopForceAttackDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true),
                        action = () => ResetForcedTarget()
                    };
                }
            }
        }

        public new void StartChoosingTarget()
        {
            // Adapted from transport pod code
            CameraJumper.TryJump(CameraJumper.GetWorldTarget(parent));
            Find.WorldSelector.ClearSelection();
            int tile = parent.Map.Tile;
            Find.WorldTargeter.BeginTargeting(ChooseWorldTarget, true, (Texture2D)Turret.def.building.turretTopMat.mainTexture, true, () => GenDraw.DrawWorldRadiusRing(tile, ShortestSelectedCompRange), TargetChooserLabel);
        }

        public new bool ChooseWorldTarget(GlobalTargetInfo t)
        {
            // Invalid tile
            if (!t.IsValid)
            {
                Messages.Message("VFESecurity.MessageTargetWorldTileInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // Out of range
            int parentMap = parent.Map.Tile;
            if (Find.WorldGrid.TraversalDistanceBetween(parentMap, t.Tile) > ShortestSelectedCompRange)
            {
                Messages.Message("VFESecurity.MessageTargetWorldTileOutOfRange".Translate(parent.def.label), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // Transport pods or artillery strike
            if (t.WorldObject is TravelingTransportPods || t.WorldObject is TravellingArtilleryStrike)
            {
                Messages.Message("VFESecurity.TargetWorldFlyingObject".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            // Same as map tile
            if (parentMap == t.Tile)
            {
                Messages.Message("VFESecurity.TargetWorldTileSameTile".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            var floatMenuOptions = FloatMenuOptionsFor(t.Tile);

            // No options
            if (!floatMenuOptions.Any())
            {
                SetTargetedTile(t);
                return true;
            }
            else
            {
                // One option
                if (floatMenuOptions.Count() == 1)
                {
                    if (!floatMenuOptions.First().Disabled)
                        floatMenuOptions.First().action();
                    return false;
                }

                // Multiple options
                Find.WindowStack.Add(new FloatMenu(floatMenuOptions.ToList()));
                return false;
            }
        }

        public new IEnumerable<FloatMenuOption> FloatMenuOptionsFor(int tile)
        {
            bool anything = false;
            var worldObjects = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < worldObjects.Count; i++)
            {
                var worldObject = worldObjects[i];
                if (worldObject.Tile == tile)
                {
                    if (worldObject != null)
                    {
                        if (worldObject is MapParent mapParent && mapParent.HasMap)
                        {
                            yield return new FloatMenuOption("VFESecurity.TargetMap".Translate(), () => SetTargetedTile(worldObject));
                            anything = true;
                        }

                        // Peace talks - cause badwill and potentially cause a raid
                        if (worldObject is PeaceTalks talks)
                        {
                            yield return new FloatMenuOption("VFESecurity.TargetPeaceTalks".Translate(), () => SetTargetedTile(worldObject));
                            anything = true;
                        }

                        // Settlement - cause badwill, potentially cause an artillery retaliation and potentially destroy
                        if (worldObject is Settlement settlement && settlement.Faction != Faction.OfPlayer)
                        {
                            yield return new FloatMenuOption("VFESecurity.TargetSettlement".Translate(), () => {SetTargetedTile(worldObject);});
                            anything = true;
                        }

                        if (worldObject is Site site)
                        {
                            // Bandit camp - potentially destroy
                            if (site.parts.Any(p => p.def == VFESecurity.SitePartDefOf.BanditCamp))
                            {
                                yield return new FloatMenuOption("VFESecurity.TargetOutpost".Translate(), () => SetTargetedTile(worldObject));
                                anything = true;
                            }
                        }
                    }
                }
            }

            if (!anything)
            {
                yield return new FloatMenuOption("VFESecurity.TargetWorldTileEmpty".Translate(), () => SetTargetedTile(new GlobalTargetInfo(tile)));
            }
        }


        public new void SetTargetedTile(GlobalTargetInfo t)
        {
            CameraJumper.TryHideWorld();
            var compList = SelectedComps.ToList();
            for (int i = 0; i < compList.Count; i++)
            {
                var comp = compList[i];

                comp.Turret.ResetForcedTarget();
                comp.Turret.ResetCurrentTarget();
                comp.targetedTile = t;
                SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(comp.parent.Position, comp.parent.Map, false));
                comp.ResetWarmupTicks();
            }
        }

        public new void ResetForcedTarget()
        {
            targetedTile = GlobalTargetInfo.Invalid;
            ResetWarmupTicks();
        }

        private new ArtilleryStrikeArrivalAction CurrentArrivalAction
        {
            get
            {
                var worldObject = targetedTile.WorldObject;
                if (worldObject != null)
                {
                    if (worldObject is MapParent mapParent && mapParent.HasMap)
                        return new ArtilleryStrikeArrivalAction_Map(mapParent);

                    // Peace talks - cause badwill and potentially cause a raid
                    if (worldObject is PeaceTalks talks)
                        return new ArtilleryStrikeArrivalAction_PeaceTalksCE(parent.Map);

                    // Settlement - cause badwill, potentially cause an artillery retaliation and potentially destroy
                    if (worldObject is Settlement settlement && settlement.Faction != Faction.OfPlayer)
                    {
                        // Special case: Insectoids from Vanilla Factions Expanded
                        if (ModCompatibilityCheck.VanillaFactionsExpandedInsectoids && settlement.Faction.def == FactionDefNamed.VFEI_Insect)
                            return new ArtilleryStrikeArrivalAction_InsectoidCE(settlement, parent.Map);

                        // Standard
                        return new ArtilleryStrikeArrivalAction_SettlementCE(settlement);
                    }

                    if (worldObject is Site site)
                    {
                        // Bandit camp - potentially destroy
                        if (site.parts.Any(p => p.def == VFESecurity.SitePartDefOf.BanditCamp))
                            return new ArtilleryStrikeArrivalAction_OutpostCE(site);
                    }
                }

                return null;
            }
        }

        public new void TryLaunch(int destinationTile)
        {
            var arrivalAction = CurrentArrivalAction;

            if (arrivalAction != null)
                arrivalAction.source = parent;

            // Play sounds
            var verb = Turret.CurrentEffectiveVerb;
            if (verb.verbProps.soundCast != null)
                verb.verbProps.soundCast.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
            if (verb.verbProps.soundCastTail != null)
                verb.verbProps.soundCastTail.PlayOneShotOnCamera(parent.Map);

            // Make active artillery strike thing
            var activeArtilleryStrike = (ActiveArtilleryStrike)ThingMaker.MakeThing(VFESecurity.ThingDefOf.VFES_ActiveArtilleryStrike);
            activeArtilleryStrike.missRadius = ArtilleryStrikeUtility.FinalisedMissRadius(Turret.CurrentEffectiveVerb.verbProps.ForcedMissRadius, Props.maxForcedMissRadiusFactor, parent.Tile, destinationTile, Props.worldTileRange);

            // Simulate an attack
            if (Turret.CompAmmo != null)
            {

		var compCharges = Turret.TryGetComp<CompCharges>();
		if (compCharges != null)
		{
		    if (compCharges.GetChargeBracket(1000, 1, ((ProjectilePropertiesCE)Turret.CompAmmo.CurrentAmmo.GetProjectileProperties()).Gravity, out var bracket))
                    {
                        activeArtilleryStrike.missRadius = bracket.x;
                    }

		}
		else {
		    activeArtilleryStrike.missRadius = 150;
		}
                activeArtilleryStrike.shellDef = Turret.CompAmmo.CurrentAmmo;
                activeArtilleryStrike.shellCount = 1;
                Turret.CompAmmo.Notify_ShotFired();
                Turret.CompAmmo.TryReduceAmmoCount();
            }
            else if (ChangeableProjectile != null)
            {
                activeArtilleryStrike.shellDef = ChangeableProjectile.Projectile;
                activeArtilleryStrike.shellCount = 1;
                ChangeableProjectile.Notify_ProjectileLaunched();
            }
            else
            {
                activeArtilleryStrike.shellDef = verb.GetProjectile();
                for (int j = 0; j < verb.verbProps.burstShotCount; j++)
                {
                    activeArtilleryStrike.shellCount++;
                    if (verb.verbProps.consumeFuelPerShot > 0 && RefuelableComp != null)
                        RefuelableComp.ConsumeFuel(verb.verbProps.consumeFuelPerShot);
                }
            }
            Turret.BurstComplete();
            VFESecurity.ThingDefOf.VFES_ArtilleryStrikeLeaving.thingClass = typeof(ArtilleryStrikeLeavingCE);
            var artilleryStrikeLeaving = (ArtilleryStrikeLeavingCE)SkyfallerMaker.MakeSkyfaller(VFESecurity.ThingDefOf.VFES_ArtilleryStrikeLeaving, activeArtilleryStrike);
            artilleryStrikeLeaving.startCell = parent.Position;
            artilleryStrikeLeaving.edgeCell = FacingEdgeCell;
            artilleryStrikeLeaving.rotation = CurAngle;
            artilleryStrikeLeaving.destinationTile = destinationTile;
            artilleryStrikeLeaving.arrivalAction = arrivalAction;
            artilleryStrikeLeaving.groupID = Find.TickManager.TicksGame;

            var impliedGraphicData = new GraphicData();
            impliedGraphicData.CopyFrom(Turret.CompAmmo.CurrentAmmo.graphicData);
            impliedGraphicData.shaderType = VFESecurity.ShaderTypeDefOf.CutoutFlying;
            artilleryStrikeLeaving.cachedShellGraphic = impliedGraphicData.Graphic;

            GenSpawn.Spawn(artilleryStrikeLeaving, parent.Position, parent.Map);
        }
    }
}
