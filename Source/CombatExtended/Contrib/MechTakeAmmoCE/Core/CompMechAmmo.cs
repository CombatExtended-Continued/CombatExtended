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
        //private readonly string _txtMagCount = "MTA_MagCount".Translate();
        //private readonly string _txtShot = "MTA_Shot".Translate();
        //private readonly string _txtNoNeedAmmo = "MTA_NoNeedAmmo".Translate();

        private Dictionary<AmmoDef, int> _loadouts = new Dictionary<AmmoDef, int>();

        public static readonly int REFRESH_INTERVAL = 6000;

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

        public Dictionary<AmmoDef, int> Loadouts
        {
            get
            {
                if (_loadouts == null)
                {
                    _loadouts = new Dictionary<AmmoDef, int>();
                }
                return _loadouts;
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

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look<AmmoDef, int>(ref _loadouts, "MTA_Loadouts");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && _loadouts == null)
            {
                _loadouts = new Dictionary<AmmoDef, int>();
            }
        }

        public void SetMagCount()
        {
            Current.Game.GetComponent<GameComponent_MechLoadoutDialogManger>()?.RegisterCompMechAmmo(this);
        }

        public void TakeAmmoNow()
        {
            this.TryMakeAmmoJob(true);
        }

        public void TryMakeAmmoJob(bool forced = false)
        {
            Thing ammoFound;

            if (!AmmoUser.UseAmmo)
            {
                return;
            }

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

            bool currentAmmoInUse = false;

            foreach (var ammoType in AmmoUser.Props.ammoSet.ammoTypes)
            {
                AmmoDef ammoDef = ammoType.ammo;

                int magCount = GetMagCount(ammoDef);
                int ammoNeed = AmmoUser.NeedAmmo(ammoDef, AmmoUser.MagSize * magCount);

                if (ammoDef == AmmoUser.CurrentAmmo && magCount > 0)
                {
                    currentAmmoInUse = true;
                }

                if (ammoNeed > 0)
                {
                    ammoFound = ParentPawn.FindBestAmmo(ammoDef);
                }
                else
                {
                    continue;
                }

                if (ammoFound == null)
                {
                    continue;
                }

                Job jobTakeAmmo = JobMaker.MakeJob(MTAJobDefOf.MTA_TakeAmmo, ammoFound);

                jobTakeAmmo.count = ammoNeed;
                Log.Message(" needs " + ammoNeed + " " + ammoDef.label + " ammo.");
                if (ParentPawn.jobs.curJob.def != MTAJobDefOf.MTA_TakeAmmo)
                {
                    ParentPawn.jobs.EndCurrentJob(JobCondition.InterruptForced, false);
                }

                ParentPawn.jobs.TryTakeOrderedJob(jobTakeAmmo, 0, true);
            }
            if (!currentAmmoInUse && AmmoUser.CurrentAmmo != null)
            {
                var ammoToDrop = AmmoUser.CurrentAmmo;
                AmmoUser.TryUnload(true);
            }


            if (ParentPawn.jobs.AllJobs().Any(x => x.def == MTAJobDefOf.MTA_TakeAmmo))
            {
                ParentPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(MTAJobDefOf.MTA_UnloadAmmo, ParentPawn), 0, true);

                if (!AmmoUser.FullMagazine)
                {
                    ParentPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(CE_JobDefOf.ReloadWeapon, ParentPawn, AmmoUser.parent), 0, true);
                }
            }

        }

        public int GetMagCount(AmmoDef ammo)
        {
            if (Loadouts.TryGetValue(ammo, out int result))
            {
                return result;
            }
            return 0;
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

            if (!AmmoUser.UseAmmo)
            {
                return false;
            }

            return true;
        }

        public void DropUnusedAmmo()
        {
            foreach (var ammoType in AmmoUser.Props.ammoSet.ammoTypes)
            {
                AmmoDef ammoDef = ammoType.ammo;

                int magCount = GetMagCount(ammoDef);
                int ammoNeed = AmmoUser.NeedAmmo(ammoDef, AmmoUser.MagSize * magCount);


                if (ammoNeed < 0)
                {
                    ParentPawn.inventory.DropCount(ammoDef, -ammoNeed);
                }
            }
        }
    }

    public class Command_SetLoadout : Command
    {
        public ThingDef equipmentDef;
        public override bool GroupsWith(Gizmo other)
        {
            return this.equipmentDef == (other as Command_SetLoadout)?.equipmentDef && base.GroupsWith(other);
        }
    }
}
