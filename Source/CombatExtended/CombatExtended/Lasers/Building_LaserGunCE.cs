using CombatExtended;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace CombatExtended.Lasers
{
    public class Building_LaserGunDef : ThingDef
    {
        public int beamPowerConsumption = 20;
        public bool supportsColors = false;
    }
    // AdeptusMechanicus.Building_LaserGunCE
    public class Building_LaserGunCE : Building_TurretGunCE, IBeamColorThing
    {
        public bool isCharged = false;
        public int previousBurstCooldownTicksLeft = 0;

        new public Building_LaserGunDef def => base.def as Building_LaserGunDef;

        public int BurstCooldownTicksLeft => burstCooldownTicksLeft;
        public int BurstWarmupTicksLeft => burstWarmupTicksLeft;

        public int BeamColor
        {
            get { return LaserColor.IndexBasedOnThingQuality(beamColorIndex, this); }
            set { beamColorIndex = value; }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<bool>(ref isCharged, "isCharged", false, false);
            Scribe_Values.Look<int>(ref previousBurstCooldownTicksLeft, "previousBurstCooldownTicksLeft", 0, false);
            Scribe_Values.Look<int>(ref beamColorIndex, "beamColorIndex", -1, false);
        }

        /*
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn pawn)
        {
            foreach (FloatMenuOption o in base.GetFloatMenuOptions(pawn))
            {
                if (o != null) yield return o;
            }

            if (!def.supportsColors) yield break;
            
            foreach (FloatMenuOption o in LaserColor.GetChangeBeamColorFloatMenuOptions(this, pawn))
            {
                if (o != null) yield return o;
            }
            
            yield break;
        }
    */

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override void Tick()
        {
            if (burstCooldownTicksLeft > previousBurstCooldownTicksLeft)
            {
                isCharged = false;
            }
            previousBurstCooldownTicksLeft = burstCooldownTicksLeft;

            if (!isCharged)
            {
                if (Drain(def.beamPowerConsumption))
                {
                    isCharged = true;
                }
            }

            if (!(isCharged || burstCooldownTicksLeft > 1)) return;

            int ticksLeft = burstWarmupTicksLeft;
            base.Tick();
            if (burstWarmupTicksLeft == def.building.turretBurstWarmupTime.SecondsToTicks() - 1 && ticksLeft == burstWarmupTicksLeft+1)
            {
                if (AttackVerb.verbProps.soundAiming != null)
                {
                    AttackVerb.verbProps.soundAiming.PlayOneShot(new TargetInfo(Position, Map, false));
                }
            }
        }

        public float AvailablePower()
        {
            if (powerComp.PowerNet == null) return 0;

            float availablePower = 0;
            foreach (var battery in powerComp.PowerNet.batteryComps)
            {
                availablePower += battery.StoredEnergy;
            }
            return availablePower;
        }
        public bool Drain(float amount)
        {
            if (amount <= 0) return true;
            if (AvailablePower() < amount) return false;

            foreach (var battery in powerComp.PowerNet.batteryComps)
            {
                var drain = battery.StoredEnergy > amount ? amount : battery.StoredEnergy;
                battery.DrawPower(drain);
                amount -= drain;

                if (amount <= 0) break;
            }

            return true;
        }

        public override string GetInspectString()
        {
            string result = base.GetInspectString();

            if (!isCharged)
            {
                result += "\n";
                result += "LaserTurretNotCharged".Translate();
            }

            return result;
        }

        private int beamColorIndex = -1;
    }


}
