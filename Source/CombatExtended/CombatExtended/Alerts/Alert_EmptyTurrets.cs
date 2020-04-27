using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CombatExtended.CombatExtended.Alerts
{
    class Alert_EmptyTurrets : Alert
    {
        private List<Thing> EmptyTurrets
        {
            get
            {
                List<Thing> emptyTurrets = new List<Thing>();
                if (Controller.settings.EnableAmmoSystem)
                {
                    foreach (Map map in Find.Maps)
                    {
                        if (map.IsPlayerHome)
                        {
                            foreach (Building_TurretGunCE turretBuilding in map.listerBuildings.AllBuildingsColonistOfClass<Building_TurretGunCE>())
                            {
                                if (!turretBuilding.IsMannable
                                    && turretBuilding.CompAmmo != null
                                    && turretBuilding.CompAmmo.EmptyMagazine)
                                {
                                    emptyTurrets.Add(turretBuilding);
                                }
                            }
                        }
                    }
                }
                return emptyTurrets;
            }
        }

        public override string GetLabel()
        {
            return "CE_EmptyTurrets".Translate();
        }

        public override TaggedString GetExplanation()
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (Thing thing in this.EmptyTurrets)
            {
                stringBuilder.AppendLine("  - " + thing.LabelShort);
            }
            return "CE_EmptyTurretsDesc".Translate(stringBuilder.ToString());
        }

        public override AlertReport GetReport()
        {
            return AlertReport.CulpritsAre(this.EmptyTurrets);
        }
    }
}
