using RimWorld;
using VanillaGravshipExpanded;
using Verse;

namespace CombatExtended.Compatibility.VGECompat;

#region License
// Any VGE Code used for compatibility has been taken from the following source
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_AutonomousTargeting.cs
#endregion

[StaticConstructorOnStartup]
public class Building_AutonomousTargetingCE: Building_TargetingTerminalCE
{
    private CompPowerTrader powerComp;
    private AutonomousTargetingExtension _extension;
    private AutonomousTargetingExtension Extension => _extension ??= def.GetModExtension<AutonomousTargetingExtension>();

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        powerComp = GetComp<CompPowerTrader>();
    }

    public bool IsPowered => powerComp?.PowerOn ?? false;
    public override bool MannedByPlayer => IsPowered;
    public override float GravshipTargeting => Extension.gravshipTargeting;
}

