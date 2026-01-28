using RimWorld;
using VanillaGravshipExpanded;
using Verse;

#region License
// Any VGE Code used for compatibility has been taken from the following source
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_AnticraftEmitterCE.cs
#endregion

namespace CombatExtended.Compatibility.VGECompat;

public class Building_AnticraftEmitterCE: Building_GravshipTurretCE
{
    // serialization fields
    private bool isFiringBurst = false;

    public override Building_GravshipTurret GetBuilding_GravshipTurret(Building_TurretGunCEWithVGEAdapter instance)
    {
        return AdapterUtils<Building_AnticraftEmitterCE, Building_AnticraftEmitter>.DelegateValuesToTargetType((Building_AnticraftEmitterCE) instance);
    }

    public Building_AnticraftEmitter ToBuilding_AnticraftEmitter => (Building_AnticraftEmitter)ToBuilding_GravshipTurret;

    public override void PostSwapMap()
    {
        ToBuilding_AnticraftEmitter.PostSwapMap();
        UpdatePowerOutput();
    }

    public override void PostMapInit()
    {
        ToBuilding_AnticraftEmitter.PostSwapMap();
        UpdatePowerOutput();
    }

    public override void Tick()
    {
        base.Tick();
        ToBuilding_AnticraftEmitter.Tick();
        isFiringBurst = ToBuilding_AnticraftEmitter.IsFiringBurst;
        UpdatePowerOutput();
    }

    private void UpdatePowerOutput()
    {
        // Update power output to match the original emitter
        if (PowerComp is CompPowerTrader powerTrader && ToBuilding_AnticraftEmitter.PowerComp is CompPowerTrader powerTrader2)
        {
            powerTrader.PowerOutput = powerTrader2.PowerOutput;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref isFiringBurst, "isFiringBurst");
    }
}
