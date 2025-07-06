using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended.AI;
public class SquadHelper
{
    public static float StudyWeaponSights(Region startRegion, IEnumerable<Pawn> pawns, Map map, Faction fac)
    {
        var TEST_CELL_CONST = 0;

        //avg value of cost..
        var sum = 0f;

        List<Pawn> RemoverList =
            new List<Pawn>();

        foreach (Pawn pawn in pawns)
        {
            try
            {
                if (pawn.Faction == null)
                {
                    continue;
                }

                if (!pawn.Faction.HostileTo(fac))
                {
                    continue;
                }

                if (pawn.equipment == null)
                {
                    continue;
                }

                if (!pawn.equipment.HasAnything())
                {
                    continue;
                }

                if (pawn.equipment.PrimaryEq == null)
                {
                    continue;
                }

                if (pawn.equipment.PrimaryEq.PrimaryVerb == null)
                {
                    continue;
                }

                if (pawn.equipment.PrimaryEq.PrimaryVerb.verbProps == null)
                {
                    continue;
                }

            }
            catch (Exception er)
            {
                RemoverList.Add(pawn);

                continue;
            }

            Region targetRegion =
                pawn.Position.GetRegion(pawn.Map);

            if (Math.Sqrt(
                        Math.Pow(targetRegion.Cells.First().x - startRegion.Cells.First().x, 2)
                        +
                        Math.Pow(targetRegion.Cells.First().y - startRegion.Cells.First().y, 2) - 10f
                    ) <= pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.range)

            {
                for (int i = 0, j = 0; i < TEST_CELL_CONST + 1 && j < 4; i++)
                {
                    IntVec3 targetPoint =
                        startRegion.RandomCell;

                    IntVec3 startPoint =
                        targetRegion.RandomCell;

                    //TODO Implment Range of weapons

                    if (GenSight.LineOfSight(startPoint
                                             , targetPoint
                                             , map
                                             , true))
                    {
                        sum += 100f;

                        j++;
                    }
                }
            }

            if (pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.range * 2f
                    > Math.Sqrt(
                        Math.Pow(targetRegion.Cells.First().x - startRegion.Cells.First().x, 2)
                        +
                        Math.Pow(targetRegion.Cells.First().y - startRegion.Cells.First().y, 2)
                    )
               )
            {
                var temp = (
                               pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.range * 2f -
                               (float)Math.Sqrt(
                                   Math.Pow(targetRegion.Cells.First().x - startRegion.Cells.First().x, 2)
                                   +
                                   Math.Pow(targetRegion.Cells.First().y - startRegion.Cells.First().y, 2)
                               )
                           );

                for (int i = 0, j = 0; i < TEST_CELL_CONST + 1 && j < 4; i++)
                {
                    IntVec3 targetPoint =
                        startRegion.RandomCell;

                    IntVec3 startPoint =
                        targetRegion.RandomCell;

                    //TODO Implment Range of weapons

                    if (GenSight.LineOfSight(startPoint
                                             , targetPoint
                                             , map
                                             , true))
                    {
                        sum += (float)temp;

                        j++;
                    }
                }
            }
        }

        return sum;
    }
}
