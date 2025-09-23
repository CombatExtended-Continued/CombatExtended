using System;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CombatExtended;
// Variant of the vanilla Graphic_StackCount class, which changes the graphic displayed for an item
// based on the current stack count, but with customizable upper limit for each subgraphic taken
// from their respective filenames.
//
// This is good for displaying ammo items which should never exist as individual cartridges/rounds,
// such as a plasma power cell containing multiple shots
//
// EXAMPLE
// An plasma pistol power cell ammo item has three subgraphics
// - Pistol_15 : a plasma pistol power cell containing up to 15 rounds
// - Pistol_499: several plasma pistol power cells, not quite reaching a full stack
// - Pistol_500: an ammo box for plasma pistol power cells, representing a full stack of 500 rounds
// Thus if we currently have 7 "rounds" of plasma in any particular stack, we should display Pistol_15
// (and so forth)

public class Graphic_StackCountRanged : Graphic_Collection
{
    public override Material MatSingle
    {
        get
        {
            return this.subGraphics[this.subGraphics.Length - 1].MatSingle;
        }
    }

    public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
    {
        return GraphicDatabase.Get<Graphic_StackCount>(this.path, newShader, this.drawSize, newColor, newColorTwo, this.data);
    }

    public override Material MatAt(Rot4 rot, Thing thing = null)
    {
        if (thing == null)
        {
            return this.MatSingle;
        }
        return this.MatSingleFor(thing);
    }

    public override Material MatSingleFor(Thing thing)
    {
        if (thing == null)
        {
            return this.MatSingle;
        }
        return this.SubGraphicFor(thing).MatSingle;
    }

    public Graphic SubGraphicFor(Thing thing)
    {
        return this.SubGraphicForStackCount(thing.stackCount, thing.def);
    }

    public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
    {
        Graphic graphic;
        if (thing != null)
        {
            graphic = this.SubGraphicFor(thing);
        }
        else
        {
            graphic = this.subGraphics[0];
        }
        graphic.DrawWorker(loc, rot, thingDef, thing, extraRotation);
    }

    public Graphic SubGraphicForStackCount(int stackCount, ThingDef def)
    {
        switch (this.subGraphics.Length)
        {
            case 1:
                return this.subGraphics[0];
            case 2:
                if (stackCount == 1)
                {
                    return this.subGraphics[0];
                }
                return this.subGraphics[1];
            default:
                {
                    // Create a lookup table, pairing each subgraphic with their corresponding upper stack limit value
                    var graphicsAndUpperLimitsList = new List<KeyValuePair<int, Graphic>>();
                    foreach (Graphic currentGraphic in this.subGraphics)
                    {
                        // Extract the upper limit value from the last set of digits in the subgraphic filename
                        // This safely ignores any numbers elsewhere (e.g. in the folder name)
                        int currentLimit = int.Parse(Regex.Match(currentGraphic.path, @"\d+(?!\D*\d)").Value);
                        graphicsAndUpperLimitsList.Add(new KeyValuePair<int, Graphic>(currentLimit, currentGraphic));
                    }

                    // By default, the subgraphics filenames are ordered from lowest to highest
                    // We reverse it so that we can start comparing from highest to lowest
                    graphicsAndUpperLimitsList.Reverse();

                    // Start by pre-emptively using the subgraphic with the highest upper stack limit value
                    // This helps catch an edge case where a modder accidentally defines a max stack size greater than
                    // the subgraphic with the highest upper stack limit, which would otherwise cause our subsequent
                    // stack count comparison loop to fail
                    Graphic finalSubGraphic = graphicsAndUpperLimitsList[0].Value;

                    // Compare our current stack count with each upper stack limit value
                    foreach (KeyValuePair<int, Graphic> currentItem in graphicsAndUpperLimitsList)
                    {
                        if (stackCount <= currentItem.Key)
                        {
                            // Use the current subgraphic for now, and then keep checking
                            finalSubGraphic = currentItem.Value;
                        }
                        else
                        {
                            // We have found our final subgraphic, break out of the foreach loop
                            break;
                        }
                    }

                    return finalSubGraphic;
                }
        }
    }

    public override string ToString()
    {
        return string.Concat(new object[]
        {
            "StackCount(path=",
            this.path,
            ", count=",
            this.subGraphics.Length,
            ")"
        });
    }
}
