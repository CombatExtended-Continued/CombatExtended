using RimWorld.Planet;
using VanillaGravshipExpanded;
using Verse;

#region License
// This file includes modified portions of code from:
// https://github.com/Vanilla-Expanded/VanillaGravshipExpanded/blob/main/Source/Comps/CompWorldArtillery.cs
//
// Original code © Oskar Potocki and the Vanilla Gravship Expanded Team.
// Incorporated with permission for Combat Extended–Vanilla Gravship Expended compatibility purposes only.
// All rights to the original code remain with the original authors.
#endregion

namespace CombatExtended.Compatibility.VGECompat;

public class CompProperties_WorldArtilleryCE : CompProperties_WorldArtillery
{
    public CompProperties_WorldArtilleryCE()
    {
        compClass = typeof(CompWorldArtilleryCE);
    }
}

// This logic could be reimplemented directly into CE ...
public class CompWorldArtilleryCE : ThingComp
{
    public CompProperties_WorldArtilleryCE Props => props as CompProperties_WorldArtilleryCE;
    public bool IsValidTargetForFiringMode(GlobalTargetInfo target, out string failReason)
    {
        var sourceMap = parent.Map;
        if (sourceMap == null)
        {
            failReason = "VGE_GravshipArtilleryNeedsVisibleMap".Translate();
            return false;
        }
        bool sourceIsOnGround = sourceMap.Tile.Valid && !sourceMap.Tile.LayerDef.isSpace;
        var mapParent = Find.WorldObjects.MapParentAt(target.Tile);
        if (mapParent == null || mapParent.Map == null)
        {
            failReason = "VGE_GravshipArtilleryNeedsVisibleMap".Translate();
            return false;
        }
        bool targetIsOnGround = mapParent.Map.Tile.Valid && !mapParent.Map.Tile.LayerDef.isSpace;

        ArtilleryFiringMode requiredMode;
        string invalidModeKey;
        if (sourceIsOnGround && targetIsOnGround)
        {
            requiredMode = ArtilleryFiringMode.GroundToGround;
            invalidModeKey = "VGE_GravshipArtilleryInvalidGroundToGround";
        }
        else if (sourceIsOnGround && !targetIsOnGround)
        {
            requiredMode = ArtilleryFiringMode.GroundToSpace;
            invalidModeKey = "VGE_GravshipArtilleryInvalidGroundToSpace";
        }
        else if (!sourceIsOnGround && !targetIsOnGround)
        {
            requiredMode = ArtilleryFiringMode.SpaceToSpace;
            invalidModeKey = "VGE_GravshipArtilleryInvalidSpaceToSpace";
        }
        else if (!sourceIsOnGround && targetIsOnGround)
        {
            requiredMode = ArtilleryFiringMode.SpaceToGround;
            invalidModeKey = "VGE_GravshipArtilleryInvalidSpaceToGround";
        }
        else
        {
            failReason = "VGE_GravshipArtilleryInvalidFiringMode".Translate();
            return false;
        }
        bool isValid = (Props.firingMode & requiredMode) != 0;
        failReason = isValid ? null : invalidModeKey.Translate();
        return isValid;
    }
}
