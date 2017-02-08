using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;
using Verse;

namespace CombatExtended
{
    public class CompTurretInit : ThingComp
    {
        public CompProperties_TurretInit Props
        {
            get
            {
                return (CompProperties_TurretInit)props;
            }
        }
        public Thing gun;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            LongEventHandler.ExecuteWhenFinished(InitTurret);
        }

        private void InitTurret()
        {
            Building_TurretGunCE turret = parent as Building_TurretGunCE;
            if (turret != null && turret.gun == null)
            {
                gun = (Thing)ThingMaker.MakeThing(parent.def.building.turretGunDef);
                turret.gun = gun;
            }
        }

        /*public override void PostSpawnSetup()
        {
            base.PostSpawnSetup(map);
            //It just needed first time.
            if (gun != null)
            {
                gun.Destroy();
                gun = null;
            }
        }*/
    }
}
