using RimWorld;
using RimWorld.Planet;
using Verse;

namespace CombatExtended.Compatibility.VGECompat;

public class Command_VGEArtilleryTarget : Command_ArtilleryTarget
{
    public CompWorldArtilleryCE compWorldArtillery;

    protected override bool AdditionnalTargettingCondition(GlobalTargetInfo targetInfo) 
    {
        if (compWorldArtillery != null)
        {
            string failReason;
            if (!compWorldArtillery.IsValidTargetForFiringMode(targetInfo, out failReason))
            {
                Messages.Message(failReason, MessageTypeDefOf.RejectInput, false);
                return false;
            }
        }
        return true;
    }
}
