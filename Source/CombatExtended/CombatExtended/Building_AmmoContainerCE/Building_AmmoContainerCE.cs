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
    public class Building_AmmoContainerCE : Building
    {
        public CompAmmoUser CompAmmoUser;

        public bool isActive => TargetTurret != null;

        public bool shouldReplaceAmmo;

        public int ticksToCompleteInitial;

        public int ticksToComplete;

        public CompAmmoUser TargetTurret;

        private static readonly Material UnfilledMat = SolidColorMaterials.NewSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f, 0.65f), ShaderDatabase.MetaOverlay);

        private static readonly Material FilledMat = SolidColorMaterials.NewSolidColorMaterial(new Color(0.9f, 0.85f, 0.2f, 0.65f), ShaderDatabase.MetaOverlay);

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref shouldReplaceAmmo, "shouldReplaceAmmo", false);
            Scribe_Values.Look(ref ticksToCompleteInitial, "ticksToCompleteInitial", 0);
            Scribe_Values.Look(ref ticksToComplete, "ticksToComplete", 0, false);
            Scribe_Values.Look(ref TargetTurret, "TargetTurret");
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Map.GetComponent<AmmoContainerTracker>().Register(this);
            CompAmmoUser = GetComp<CompAmmoUser>();
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
            Thing outThing;
            Thing ammoThing = ThingMaker.MakeThing(CompAmmoUser.CurrentAmmo);
            ammoThing.stackCount = CompAmmoUser.CurMagCount;
            CompAmmoUser.CurMagCount = 0;
            if (forcibly)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Bullet, Rand.Range(0, 10));
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
                    drop.defaultLabel = "CommandDropAllInAmmoContainer".Translate();
                    drop.defaultDesc = "CommandDropAllInAmmoContainerDesc".Translate();
                    drop.icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true);
                    drop.action = delegate
                    {
                        DropAmmo();
                    };
                    yield return drop;
                }

                Command_Action toggleShouldReplace = new Command_Action();
                toggleShouldReplace.defaultLabel = "CommandToggleShouldReplace".Translate();
                toggleShouldReplace.defaultDesc = "CommandToggleShouldReplace".Translate();
                toggleShouldReplace.icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true);
                toggleShouldReplace.action = delegate
                {
                    shouldReplaceAmmo = !shouldReplaceAmmo;
                };
                yield return toggleShouldReplace;

                //forced reload
                if (Prefs.DevMode)
                {
                    Command_Action reload = new Command_Action();
                    reload.defaultLabel = "CommandForcedReload".Translate();
                    reload.defaultDesc = "CommandForcedReload".Translate();
                    reload.icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true);
                    reload.action = delegate
                    {
                        TryActiveReload();
                    };
                    yield return reload;
                }
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (isActive)
            {
                ticksToComplete--;
                if (ticksToComplete == 0)
                {
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
                stringBuilder.AppendLine(ticksToComplete.TicksToSeconds().ToString());
            }
            stringBuilder.AppendLine(shouldReplaceAmmo.ToString());
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public bool TryActiveReload()
        {
            List<Thing> adjThings = new List<Thing>();
            GenAdjFast.AdjacentThings8Way(this, adjThings);

            foreach (Thing building in adjThings)
            {
                if (building is Building_TurretGunCE turret && StartReload(turret.GetAmmo()))
                {
                    return true;
                }
            }
            return false;
        }

        public bool StartReload(CompAmmoUser TurretMagazine)
        {
            if (isActive)
            {
                return false;
            }

            if (TurretMagazine.FullMagazine || CompAmmoUser.EmptyMagazine)
            {
                return false;
            }

            if (TurretMagazine.CurrentAmmo == CompAmmoUser.CurrentAmmo || (shouldReplaceAmmo && TurretMagazine.Props.ammoSet == CompAmmoUser.Props.ammoSet))
            {
                TargetTurret = TurretMagazine;
                ticksToComplete = Mathf.CeilToInt(TurretMagazine.Props.reloadTime.SecondsToTicks());
                ticksToCompleteInitial = ticksToComplete;
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
            int ammoCount = Mathf.Min(CompAmmoUser.CurMagCount, TargetTurret.MissingToFullMagazine);
            TargetTurret.CurMagCount += ammoCount;
            CompAmmoUser.CurMagCount -= ammoCount;
            ticksToCompleteInitial = 0;
            TargetTurret = null;
            TryActiveReload();
            return true;
        }
    }
}
