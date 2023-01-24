using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;
using UnityEngine;

using CombatExtended;
using Verse.AI;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public class CompMechAmmo : ThingComp
    {
        private Pawn _parentPawn;
        private Pawn_InventoryTracker _pawnInventory;
        private CompAmmoUser _ammoUser;

        private ThingWithComps _cachedEquipment;

        private static Texture2D _gizmoIconSetMagCount;
        private static Texture2D _gizmoIconTakeAmmoNow;

        private readonly string _txtSetMagCount = "MTA_SetMagCount".Translate();
        private readonly string _txtTakeAmmoNow = "MTA_TakeAmmoNow".Translate();
        private readonly string _txtMagCount = "MTA_MagCount".Translate();
        private readonly string _txtShot = "MTA_Shot".Translate();
        private readonly string _txtNoNeedAmmo = "MTA_NoNeedAmmo".Translate();

        public static readonly int REFRESH_INTERVAL = 6000;

        public int magCount = 6;

        public Texture2D GizmoIcon_SetMagCount
        {
            get
            {
                if (_gizmoIconSetMagCount == null)
                {
                    _gizmoIconSetMagCount = ContentFinder<Texture2D>.Get(this.Props.gizmoIconSetMagCount, false);
                }

                return _gizmoIconSetMagCount;
            }
        }

        public Texture2D GizmoIcon_TakeAmmoNow
        {
            get
            {
                if (_gizmoIconTakeAmmoNow == null)
                {
                    _gizmoIconTakeAmmoNow = ContentFinder<Texture2D>.Get(this.Props.gizmoIconTakeAmmoNow, false);
                }

                return _gizmoIconTakeAmmoNow;
            }
        }

        public Pawn ParentPawn
        {
            get
            {
                if (_parentPawn == null)
                {
                    _parentPawn = this.parent as Pawn;
                }

                return _parentPawn;
            }
        }

        public Pawn_InventoryTracker PawnInventory
        {
            get
            {
                if (_pawnInventory == null)
                {
                    _pawnInventory = ParentPawn?.inventory;
                }

                return _pawnInventory;
            }
        }

        public CompAmmoUser AmmoUser
        {
            get
            {
                if (_cachedEquipment != ParentPawn.equipment.Primary)
                {
                    _cachedEquipment = ParentPawn.equipment.Primary;
                    _ammoUser = _cachedEquipment?.GetComp<CompAmmoUser>();
                }
                if (_ammoUser == null)
                {
                    _ammoUser = ParentPawn?.equipment.Primary?.GetComp<CompAmmoUser>();
                }

                return _ammoUser;
            }
        }

        public CompProperties_MechAmmo Props => (CompProperties_MechAmmo)this.props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!IsWorkable())
            {
                yield break;
            }

            yield return new Command_Action
            {
                action = SetMagCount,
                defaultLabel = _txtSetMagCount,
                icon = GizmoIcon_SetMagCount,
            };
            yield return new Command_Action
            {
                action = TakeAmmoNow,
                defaultLabel = _txtTakeAmmoNow,
                icon = GizmoIcon_TakeAmmoNow,
            };
            yield break;
        }

        public override void CompTick()
        {
            if (!parent.IsHashIntervalTick(REFRESH_INTERVAL))
            {
                return;
            }

            if (!IsWorkable())
            {
                return;
            }

            TryMakeAmmoJob();
        }

        public void SetMagCount()
        {
            Dialog_SetValue dialog = new Dialog_SetValue(GetMagCountText, OnMagCountSetted, magCount);
            Find.WindowStack.Add(dialog);
        }

        public void TakeAmmoNow()
        {
            this.TryMakeAmmoJob(true);
        }

        public void TryMakeAmmoJob(bool forced = false)
        {
            Thing ammoFound;

            if (ParentPawn == null)
            {
                return;
            }

            if (ParentPawn.Drafted && !forced)
            {
                return;
            }

            if (ParentPawn.CurJobDef == MTAJobDefOf.MTA_TakeAmmo)
            {
                return;
            }

            if (ParentPawn.GetComp<CompMechAmmo>() == null)
            {
                return;
            }

            if (AmmoUser == null)
            {
                return;
            }

            AmmoDef currentAmmo = AmmoUser.SelectedAmmo;

            int ammoNeed = AmmoUser.NeedAmmo(AmmoUser.MagSize * magCount);

            if (ammoNeed == 0)
            {
                return;
            }

            ammoFound = ParentPawn.FindBestAmmo(currentAmmo);
            if (ammoFound == null)
            {
                return;
            }

            Job jobTakeAmmo = JobMaker.MakeJob(MTAJobDefOf.MTA_TakeAmmo, ammoFound);
            Job jobReload = JobMaker.MakeJob(CE_JobDefOf.ReloadWeapon, ParentPawn, AmmoUser.parent);
            jobTakeAmmo.count = ammoNeed;
            ParentPawn.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
            ParentPawn.jobs.StartJob(jobTakeAmmo);
        }

        public bool IsWorkable()
        {
            if (ParentPawn == null)
            {
                return false;
            }

            if (ParentPawn.Faction != Faction.OfPlayer)
            {
                return false;
            }

            if (ParentPawn.equipment.Primary == null)
            {
                return false;
            }

            if (AmmoUser == null)
            {
                return false;
            }

            return true;
        }

        private string GetMagCountText(int value)
        {
            if (AmmoUser == null)
            {
                return _txtNoNeedAmmo;
            }
            return _txtMagCount + value + " = " + value * AmmoUser.MagSize + _txtShot;
        }

        private void OnMagCountSetted(int value)
        {
            magCount = value;
        }
    }
}
