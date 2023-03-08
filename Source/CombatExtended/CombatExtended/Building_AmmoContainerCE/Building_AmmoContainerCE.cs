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
    public class Building_AmmoContainerCE : Building
    {
        public CompAmmoUser CompAmmoUser;

        public bool isActive => TargetTurret != null;

        public bool shouldReplaceAmmo;

        public int ticksToCompleteInitial;

        public int ticksToComplete;

        public bool isReloading;

        public CompAmmoUser TargetTurret;

        private static readonly Material UnfilledMat = SolidColorMaterials.NewSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f, 0.65f), ShaderDatabase.MetaOverlay);

        private static readonly Material FilledMat = SolidColorMaterials.NewSolidColorMaterial(new Color(0.9f, 0.85f, 0.2f, 0.65f), ShaderDatabase.MetaOverlay);

        public CompPowerTrader powerComp;
        public CompCanBeDormant dormantComp;
        public CompInitiatable initiatableComp;
        public CompMannable mannableComp;

        public bool shouldBeOn => (powerComp == null || powerComp.PowerOn) && (dormantComp == null || dormantComp.Awake) && (initiatableComp == null || initiatableComp.Initiated) && (mannableComp == null || mannableComp.MannedNow);

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref shouldReplaceAmmo, "shouldReplaceAmmo", false);
            Scribe_Values.Look(ref ticksToCompleteInitial, "ticksToCompleteInitial", 0);
            Scribe_Values.Look(ref ticksToComplete, "ticksToComplete", 0, false);
            Scribe_Values.Look(ref TargetTurret, "TargetTurret");
            Scribe_Values.Look(ref isReloading, "isReloading");
        }

        public bool CanReplaceAmmo(CompAmmoUser ammoUser)
        {
            return shouldReplaceAmmo && ammoUser.Props.ammoSet == CompAmmoUser.Props.ammoSet && ammoUser.CurrentAmmo != CompAmmoUser.CurrentAmmo;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Map.GetComponent<AmmoContainerTracker>().Register(this);
            CompAmmoUser = GetComp<CompAmmoUser>();

            dormantComp = GetComp<CompCanBeDormant>();
            initiatableComp = GetComp<CompInitiatable>();
            powerComp = GetComp<CompPowerTrader>();
            mannableComp = GetComp<CompMannable>();

            if (CompAmmoUser == null)
            {
                Log.Error(this.GetCustomLabelNoCount() + " Requires CompAmmoUser to funtion properly.");
            }
            if (this.def.tickerType != TickerType.Normal)
            {
                Log.Error(this.GetCustomLabelNoCount() + " Requires normal ticker type to funtion properly.");
            }
            if (this.def.drawerType != DrawerType.MapMeshAndRealTime)
            {
                Log.Error(this.GetCustomLabelNoCount() + " Requires MapMeshAndRealTime drawer type to display progress bar.");
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map.GetComponent<AmmoContainerTracker>().Unregister(this);
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
            if (forcibly)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Bullet, Rand.Range(0, 100));
                ammoThing.TakeDamage(dinfo);
            }
            if (!GenThing.TryDropAndSetForbidden(ammoThing, Position, Map, ThingPlaceMode.Near, out outThing, Faction != Faction.OfPlayer))
            {
                Log.Warning(String.Concat(GetType().Assembly.GetName().Name + " :: " + GetType().Name + " :: ", "Unable to drop ", ammoThing.LabelCap, " on the ground, thing was destroyed."));
            }
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
                    tutorTag = "CE_AmmoContainer"
                };
                yield return reloadCommandGizmo;

                // God mode gizmos for emptying and filling the magazine
                if (DebugSettings.godMode)
                {
                    Command_Action devSetAmmoToMinCommandGizmo = new Command_Action
                    {
                        action = delegate { CompAmmoUser.CurMagCount = 0; },
                        defaultLabel = "DEV: Set ammo to 0"
                    };
                    yield return devSetAmmoToMinCommandGizmo;

                    Command_Action devSetAmmoToMaxCommandGizmo = new Command_Action
                    {
                        action = delegate { CompAmmoUser.CurrentAmmo = CompAmmoUser.SelectedAmmo; CompAmmoUser.CurMagCount = CompAmmoUser.MagSize; },
                        defaultLabel = "DEV: Set ammo to max"
                    };
                    yield return devSetAmmoToMaxCommandGizmo;

                    Command_Action devSetAmmoPlusOneCommandGizmo = new Command_Action
                    {
                        action = delegate { CompAmmoUser.CurrentAmmo = CompAmmoUser.SelectedAmmo; CompAmmoUser.CurMagCount += 1; },
                        defaultLabel = "DEV: Ammo +1"
                    };
                    yield return devSetAmmoPlusOneCommandGizmo;
                }

                //drop all
                if (!CompAmmoUser.EmptyMagazine)
                {
                    Command_Action drop = new Command_Action();
                    drop.defaultLabel = "CE_AmmoContainer_DropAmmo".Translate();
                    drop.defaultDesc = "CE_AmmoContainer_DropAmmoDesc".Translate();
                    drop.icon = ContentFinder<Texture2D>.Get("UI/Buttons/CE_AmmoContainer_Drop", true);
                    drop.action = delegate
                    {
                        DropAmmo();
                    };
                    yield return drop;

                    //forced reload
                    Command_Action reload = new Command_Action();
                    reload.defaultLabel = "CE_AmmoContainer_ForceReload".Translate();
                    reload.defaultDesc = "CE_AmmoContainer_ForceReloadDesc".Translate();
                    reload.icon = ContentFinder<Texture2D>.Get("UI/Buttons/CE_AmmoContainer_Reload", true);
                    reload.action = delegate
                    {
                        List<Thing> adjThings = new List<Thing>();
                        GenAdjFast.AdjacentThings8Way(this, adjThings);
                        bool success = false;
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
                            Messages.Message(string.Format("CE_AmmoContainer_NoTurretToReload".Translate(), Label, CompAmmoUser.Props.ammoSet.label), this, MessageTypeDefOf.RejectInput, historical: false);
                        }
                    };
                    yield return reload;
                }

                Command_Action toggleShouldReplace = new Command_Action();
                toggleShouldReplace.defaultLabel = shouldReplaceAmmo ? "CE_AmmoContainer_ToggleReplaceOn".Translate() : "CE_AmmoContainer_ToggleReplaceOff".Translate();
                toggleShouldReplace.defaultDesc = shouldReplaceAmmo ? "CE_AmmoContainer_ToggleReplaceDescOn".Translate() : "CE_AmmoContainer_ToggleReplaceDescOff".Translate();
                toggleShouldReplace.icon = shouldReplaceAmmo ? ContentFinder<Texture2D>.Get("UI/Buttons/CE_AmmoContainer_ReplaceOn", true) : ContentFinder<Texture2D>.Get("UI/Buttons/CE_AmmoContainer_ReplaceOff", true);
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
                ticksToComplete--;
                if (ticksToComplete == 0)
                {
                    ticksToCompleteInitial = 0;
                    ReloadFinalize();
                }
            }
        }

        public override void Draw()
        {
            base.Draw();
            if (isActive)
            {
                GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
                r.center = DrawPos + Vector3.up * 0.1f;
                r.size = new Vector2(1f, 0.14f);
                r.fillPercent = ticksToComplete / (float)ticksToCompleteInitial;
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
            if (isActive)
            {
                stringBuilder.AppendLine("CE_AmmoContainer_ReloadTime".Translate(ticksToComplete.TicksToSeconds().ToString("F0")));
            }
            stringBuilder.AppendLine("CE_AmmoContainer_ShouldReplace".Translate(shouldReplaceAmmo.ToString()));
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

            bool canReplaceAmmo = CanReplaceAmmo(TurretMagazine);

            if ((TurretMagazine.FullMagazine && !canReplaceAmmo) || CompAmmoUser.EmptyMagazine || !shouldBeOn)
            {
                return false;
            }

            if (TurretMagazine.CurrentAmmo == CompAmmoUser.CurrentAmmo || canReplaceAmmo)
            {
                TargetTurret = TurretMagazine;
                ticksToComplete = Mathf.CeilToInt(TurretMagazine.Props.reloadTime.SecondsToTicks() / this.GetStatValue(CE_StatDefOf.ReloadSpeed));
                ticksToCompleteInitial = ticksToComplete;
                turret.SetReloading(true);
                return true;
            }

            return false;
        }

        public bool ReloadFinalize()
        {
            if (TargetTurret.CurrentAmmo != CompAmmoUser.CurrentAmmo)
            {
                TargetTurret.TryUnload();
                TargetTurret.SelectedAmmo = CompAmmoUser.CurrentAmmo;
                TargetTurret.CurrentAmmo = CompAmmoUser.CurrentAmmo;
            }
            if (TargetTurret.Props.reloadOneAtATime)
            {
                TargetTurret.CurMagCount++;
                CompAmmoUser.CurMagCount--;
                if (StartReload(TargetTurret, true))
                {
                    return true;
                }
            }
            else
            {
                int ammoCount = Mathf.Min(CompAmmoUser.CurMagCount, TargetTurret.MissingToFullMagazine);
                TargetTurret.CurMagCount += ammoCount;
                CompAmmoUser.CurMagCount -= ammoCount;
            }
            TargetTurret.turret.SetReloading(false);
            TargetTurret = null;
            TryActiveReload();
            return true;
        }
    }
}
