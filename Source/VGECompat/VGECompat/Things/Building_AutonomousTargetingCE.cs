using RimWorld;
using VanillaGravshipExpanded;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Things/Building_AutonomousTargeting.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat;

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

