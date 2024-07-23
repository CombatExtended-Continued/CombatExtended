using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CombatExtended;
using CombatExtended.AI;
using CombatExtended.Compatibility;
using Mono.Unix.Native;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;
using Verse.Sound;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class Building_AutoloaderCE : Building
    {
        public CompAmmoUser CompAmmoUser;

        public bool isActive => TargetAmmoUser != null;

        public bool shouldReplaceAmmo;

        public int ticksToCompleteInitial;

        public int ticksToComplete;

        public bool isReloading;

        public CompAmmoUser TargetAmmoUser => TargetTurret.GetAmmo();

        public Building_Turret TargetTurret;

        private Sustainer reloadingSustainer;

        private static readonly Material UnfilledMat = SolidColorMaterials.NewSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f, 0.65f), ShaderDatabase.MetaOverlay);

        private static readonly Material FilledMat = SolidColorMaterials.NewSolidColorMaterial(new Color(0.9f, 0.85f, 0.2f, 0.65f), ShaderDatabase.MetaOverlay);

        public CompPowerTrader powerComp;
        public CompCanBeDormant dormantComp;
        public CompInitiatable initiatableComp;
        public CompMannable mannableComp;

        public ModExtension_AutoLoaderGraphics graphicsExt;

        public bool shouldBeOn => ShouldBeOn();

        public bool ShouldBeOn(bool failureNotify = false)
        {
            if (manningRequiredButUnmanned)
            {
                if (failureNotify) { Messages.Message(string.Format("CE_AutoLoader_Unmanned".Translate(), Label), this, MessageTypeDefOf.RejectInput, historical: false); }
                return false;
            }
            if (powerRequiredButUnpowered)
            {
                if (failureNotify) { Messages.Message(string.Format("CE_AutoLoader_Unpowered".Translate(), Label), this, MessageTypeDefOf.RejectInput, historical: false); }
                return false;
            }
            return (dormantComp == null || dormantComp.Awake) && (initiatableComp == null || initiatableComp.Initiated);
        }

        public bool manningRequiredButUnmanned => mannableComp != null && !mannableComp.MannedNow;

        public bool powerRequiredButUnpowered => powerComp != null && !powerComp.PowerOn;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref shouldReplaceAmmo, "shouldReplaceAmmo", false);
            Scribe_Values.Look(ref ticksToCompleteInitial, "ticksToCompleteInitial", 0);
            Scribe_Values.Look(ref ticksToComplete, "ticksToComplete", 0, false);
            Scribe_Values.Look(ref isReloading, "isReloading");

            Scribe_References.Look(ref TargetTurret, "Turret", false);
        }

        public override Graphic Graphic
        {
            get
            {
                Graphic graphic = null;
                if (graphicsExt != null)
                {
                    if (CompAmmoUser.CurMagCount > CompAmmoUser.MagSize * 0.75f)
                    {
                        graphic = graphicsExt.fullGraphic?.GraphicColoredFor(this);
                    }
                    else if (CompAmmoUser.EmptyMagazine)
                    {
                        graphic = graphicsExt.emptyGraphic?.GraphicColoredFor(this);
                    }
                    else
                    {
                        graphic = graphicsExt.halfFullGraphic?.GraphicColoredFor(this);
                    }
                }
                return graphic ?? base.Graphic;
            }
        }

        public bool CanReplaceAmmo(CompAmmoUser ammoUser)
        {
            return shouldReplaceAmmo && ammoUser.Props.ammoSet == CompAmmoUser.Props.ammoSet && ammoUser.CurrentAmmo != CompAmmoUser.CurrentAmmo;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Map.GetComponent<AutoLoaderTracker>().Register(this);
            CompAmmoUser = GetComp<CompAmmoUser>();

            dormantComp = GetComp<CompCanBeDormant>();
            initiatableComp = GetComp<CompInitiatable>();
            powerComp = GetComp<CompPowerTrader>();
            mannableComp = GetComp<CompMannable>();

            graphicsExt = def.GetModExtension<ModExtension_AutoLoaderGraphics>();
            if (CompAmmoUser == null)
            {
                Log.Error(this.GetCustomLabelNoCount() + " Requires CompAmmoUser to function properly.");
            }
            if (this.def.tickerType != TickerType.Normal)
            {
                Log.Error(this.GetCustomLabelNoCount() + " Requires normal ticker type to function properly.");
            }
            if (this.def.drawerType != DrawerType.MapMeshAndRealTime)
            {
                Log.Error(this.GetCustomLabelNoCount() + " Requires MapMeshAndRealTime drawer type to display progress bar.");
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            TargetTurret?.SetReloading(false);
            Map.GetComponent<AutoLoaderTracker>().Unregister(this);
            DropAmmo(mode == DestroyMode.KillFinalize);
            base.DeSpawn(mode);
        }

        public void DropAmmo(bool forcibly = false)
        {
            if (CompAmmoUser.EmptyMagazine)
            {
                return;
            }
            Thing outThing;
            Thing ammoThing = ThingMaker.MakeThing(CompAmmoUser.CurrentAmmo);
            ammoThing.stackCount = CompAmmoUser.CurMagCount;
            CompAmmoUser.CurMagCount = 0;
            if (!GenThing.TryDropAndSetForbidden(ammoThing, Position, Map, ThingPlaceMode.Near, out outThing, Faction != Faction.OfPlayer))
            {
                Log.Warning(String.Concat(GetType().Assembly.GetName().Name + " :: " + GetType().Name + " :: ", "Unable to drop ", ammoThing.LabelCap, " on the ground, thing was destroyed."));
            }
            if (forcibly)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Bullet, Rand.Range(0, 100));
                ammoThing.TakeDamage(dinfo);
            }
            Notify_ColorChanged();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            // Ammo gizmos
            if (CompAmmoUser != null && (Faction == Faction.OfPlayer || Prefs.DevMode))
            {
                GizmoAmmoStatus ammoStatusGizmo = new GizmoAmmoStatus { compAmmo = CompAmmoUser };
                yield return ammoStatusGizmo;

                Command_Reload reloadCommandGizmo = new Command_Reload
                {
                    compAmmo = CompAmmoUser,
                    action = null,
                    defaultLabel = CompAmmoUser.HasMagazine ? (string)"CE_ReloadLabel".Translate() : "",
                    defaultDesc = "CE_ReloadDesc".Translate(),
                    icon = CompAmmoUser.CurrentAmmo == null ? ContentFinder<Texture2D>.Get("UI/Buttons/Reload", true) : CompAmmoUser.SelectedAmmo.IconTexture(),
                    tutorTag = "CE_AutoLoader"
                };
                yield return reloadCommandGizmo;

                // God mode gizmos for emptying and filling the magazine
                if (DebugSettings.godMode)
                {
                    Command_Action devSetAmmoToMinCommandGizmo = new Command_Action
                    {
                        action = delegate { CompAmmoUser.CurMagCount = 0; Notify_ColorChanged(); },
                        defaultLabel = "DEV: Set ammo to 0"
                    };
                    yield return devSetAmmoToMinCommandGizmo;

                    Command_Action devSetAmmoToMaxCommandGizmo = new Command_Action
                    {
                        action = delegate { CompAmmoUser.CurrentAmmo = CompAmmoUser.SelectedAmmo; CompAmmoUser.CurMagCount = CompAmmoUser.MagSize; Notify_ColorChanged(); },
                        defaultLabel = "DEV: Set ammo to max"
                    };
                    yield return devSetAmmoToMaxCommandGizmo;

                    Command_Action devSetAmmoPlusOneCommandGizmo = new Command_Action
                    {
                        action = delegate { CompAmmoUser.CurrentAmmo = CompAmmoUser.SelectedAmmo; CompAmmoUser.CurMagCount += 1; Notify_ColorChanged(); },
                        defaultLabel = "DEV: Ammo +1"
                    };
                    yield return devSetAmmoPlusOneCommandGizmo;
                }

                //drop all
                if (!CompAmmoUser.EmptyMagazine)
                {
                    Command_Action drop = new Command_Action();
                    drop.defaultLabel = "CE_AutoLoader_DropAmmo".Translate();
                    drop.defaultDesc = "CE_AutoLoader_DropAmmoDesc".Translate();
                    drop.icon = ContentFinder<Texture2D>.Get("UI/Buttons/CE_AutoLoader_Drop", true);
                    drop.action = delegate
                    {
                        DropAmmo();
                    };
                    yield return drop;

                    //forced reload
                    Command_Action reload = new Command_Action();
                    reload.defaultLabel = "CE_AutoLoader_ForceReload".Translate();
                    reload.defaultDesc = "CE_AutoLoader_ForceReloadDesc".Translate();
                    reload.icon = ContentFinder<Texture2D>.Get("UI/Buttons/CE_AutoLoader_Reload", true);
                    reload.action = delegate
                    {
                        List<Thing> adjThings = new List<Thing>();
                        GenAdjFast.AdjacentThings8Way(this, adjThings);
                        bool success = false;
                        if (ShouldBeOn(true))
                        {
                            foreach (Thing building in adjThings)
                            {
                                if (building is Building_TurretGunCE turret && StartReload(turret.GetAmmo()))
                                {
                                    success = true;
                                    break;
                                }
                            }
                            if (!success)
                            {
                                Messages.Message(string.Format("CE_AutoLoader_NoTurretToReload".Translate(), Label, CompAmmoUser.Props.ammoSet.label), this, MessageTypeDefOf.RejectInput, historical: false);
                            }
                        }
                    };
                    yield return reload;
                }

                Command_Action toggleShouldReplace = new Command_Action();
                toggleShouldReplace.defaultLabel = shouldReplaceAmmo ? "CE_AutoLoader_ToggleReplaceOn".Translate() : "CE_AutoLoader_ToggleReplaceOff".Translate();
                toggleShouldReplace.defaultDesc = shouldReplaceAmmo ? "CE_AutoLoader_ToggleReplaceDescOn".Translate() : "CE_AutoLoader_ToggleReplaceDescOff".Translate();
                toggleShouldReplace.icon = shouldReplaceAmmo ? ContentFinder<Texture2D>.Get("UI/Buttons/CE_AutoLoader_ReplaceOn", true) : ContentFinder<Texture2D>.Get("UI/Buttons/CE_AutoLoader_ReplaceOff", true);
                toggleShouldReplace.action = delegate
                {
                    shouldReplaceAmmo = !shouldReplaceAmmo;
                };
                yield return toggleShouldReplace;
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (isActive && shouldBeOn)
            {
                if (!TargetTurret.Spawned || TargetTurret.IsForbidden(Faction) || CompAmmoUser.EmptyMagazine)
                {
                    TargetTurret?.SetReloading(false);
                    TargetTurret = null;
                    ticksToCompleteInitial = 0;
                }

                ticksToComplete--;

                if (reloadingSustainer == null)
                {
                    reloadingSustainer = (graphicsExt?.reloadingSustainer ?? CE_SoundDefOf.CE_AutoLoaderAmbient).TrySpawnSustainer(SoundInfo.InMap(this));
                }
                reloadingSustainer.Maintain();

                if (ticksToComplete == 0)
                {
                    ticksToCompleteInitial = 0;
                    (graphicsExt?.reloadCompleteSound ?? TargetAmmoUser.parent.def.soundInteract).PlayOneShot(this);
                    ReloadFinalize();
                    Notify_ColorChanged();
                }
            }
            else if (reloadingSustainer != null)
            {
                reloadingSustainer.End();
                reloadingSustainer = null;
            }

        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            if (isActive)
            {
                GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
                r.center = DrawPos + Vector3.up * 0.1f;
                r.size = new Vector2(1f, 0.14f);
                r.fillPercent = 1 - (ticksToComplete / (float)ticksToCompleteInitial);
                r.filledMat = FilledMat;
                r.unfilledMat = UnfilledMat;
                r.margin = 0.12f;
                GenDraw.DrawFillableBar(r);
            }
        }


        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!inspectString.NullOrEmpty())
            {
                stringBuilder.AppendLine(inspectString);
            }
            if (manningRequiredButUnmanned)
            {
                stringBuilder.AppendLine("CE_AutoLoader_ManningRequired".Translate());
            }
            if (isActive)
            {
                stringBuilder.AppendLine("CE_AutoLoader_ReloadTime".Translate(ticksToComplete.TicksToSeconds().ToString("F0")));
            }
            stringBuilder.AppendLine("CE_AutoLoader_ShouldReplace".Translate(shouldReplaceAmmo.ToString()));
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public bool TryActiveReload()
        {
            List<Thing> adjThings = new List<Thing>();
            GenAdjFast.AdjacentThings8Way(this, adjThings);

            foreach (Thing building in adjThings)
            {
                if (building is Building_TurretGunCE turret && (turret.GetAmmo().EmptyMagazine || turret.currentTargetInt == LocalTargetInfo.Invalid) && StartReload(turret.GetAmmo()))
                {
                    return true;
                }
            }
            return false;
        }

        public bool StartReload(CompAmmoUser TurretMagazine, bool continued = false)
        {
            Building_Turret turret = TurretMagazine.turret;

            //If turret is unable to be reloaded
            if (!turret.Spawned || turret.IsForbidden(Faction) || turret.GetReloading())
            {
                return false;
            }

            //If the ammo container is unable to reload turret
            if ((isActive && !continued) || CompAmmoUser.EmptyMagazine || !shouldBeOn)
            {
                return false;
            }

            //if this is the right turret to reload
            if (graphicsExt != null)
            {
                //if def exists and match
                bool tagMatch = graphicsExt.allowedTurrets.Any() && graphicsExt.allowedTurrets.Contains(turret.def.defName);

                //if tag exists and match
                if (!tagMatch && graphicsExt.allowedTurretTags.Any())
                {
                    foreach (string loadertag in graphicsExt.allowedTurretTags)
                    {
                        if (turret.def.building.buildingTags.NotNullAndContains(loadertag))
                        {
                            tagMatch = true;
                            break;
                        }
                    }
                }

                if (!tagMatch)
                {
                    return false;
                }
            }

            bool canReplaceAmmo = CanReplaceAmmo(TurretMagazine);

            if ((TurretMagazine.FullMagazine && !canReplaceAmmo))
            {
                return false;
            }

            if (TurretMagazine.CurrentAmmo == CompAmmoUser.CurrentAmmo || canReplaceAmmo)
            {
                TargetTurret = TurretMagazine.turret;
                ticksToComplete = Mathf.CeilToInt(TurretMagazine.Props.reloadTime.SecondsToTicks() / this.GetStatValue(CE_StatDefOf.ReloadSpeed));
                ticksToCompleteInitial = ticksToComplete;
                turret.SetReloading(true);
                return true;
            }

            return false;
        }

        public bool ReloadFinalize()
        {
            if (TargetAmmoUser.CurrentAmmo != CompAmmoUser.CurrentAmmo)
            {
                TargetAmmoUser.TryUnload();
                TargetAmmoUser.SelectedAmmo = CompAmmoUser.CurrentAmmo;
                TargetAmmoUser.CurrentAmmo = CompAmmoUser.CurrentAmmo;
            }
            if (TargetAmmoUser.Props.reloadOneAtATime)
            {
                TargetAmmoUser.CurMagCount++;
                CompAmmoUser.CurMagCount--;
                if (StartReload(TargetAmmoUser, true))
                {
                    return true;
                }
            }
            else
            {
                int ammoCount = Mathf.Min(CompAmmoUser.CurMagCount, TargetAmmoUser.MissingToFullMagazine);
                TargetAmmoUser.CurMagCount += ammoCount;
                CompAmmoUser.CurMagCount -= ammoCount;
            };
            TargetTurret.SetReloading(false);
            TargetTurret = null;
            TryActiveReload();
            return true;
        }
    }
}
