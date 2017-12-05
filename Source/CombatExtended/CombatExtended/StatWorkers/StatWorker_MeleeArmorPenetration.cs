using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class StatWorker_MeleeArmorPenetration : StatWorker
    {
        private float GetMeleePenetration(StatRequest req, DamageDef damageType = null)
        {
            Pawn pawn = req.Thing as Pawn;
            if (pawn == null)
            {
                return 0;
            }
            List<VerbEntry> updatedAvailableVerbsList = pawn.meleeVerbs.GetUpdatedAvailableVerbsList();
            if (updatedAvailableVerbsList.Count == 0)
            {
                return 0;
            }

            List<VerbEntry> verbsList;
            if (damageType != null) verbsList = updatedAvailableVerbsList.Where(x => x.verb.verbProps.meleeDamageDef == damageType).ToList();
            else verbsList = updatedAvailableVerbsList;


            float totalSelectionWeight = 0f;
            for (int i = 0; i < verbsList.Count; i++)
            {
                totalSelectionWeight += verbsList[i].SelectionWeight;
            }
            float totalAveragePen = 0f;
            foreach (VerbEntry current in verbsList)
            {
                var propsCE = current.verb.verbProps as VerbPropertiesCE;
                if (propsCE != null)
                {
                    ThingWithComps ownerEquipment = current.verb.ownerEquipment;
                    var weightFactor = current.SelectionWeight / totalSelectionWeight;
                    totalAveragePen += weightFactor * propsCE.meleeArmorPenetration;
                }
            }
            return totalAveragePen;
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            return GetMeleePenetration(req);
        }

        public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            var pawn = req.Thing as Pawn;
            if (pawn == null)
                return base.GetExplanationUnfinalized(req, numberSense);

            // Find all damage types of our verbs
            List<DamageDef> allDamageTypes = new List<DamageDef>();
            foreach(VerbEntry current in pawn.meleeVerbs.GetUpdatedAvailableVerbsList())
            {
                var dam = current.verb.verbProps.meleeDamageDef;
                if (dam != null) allDamageTypes.Add(dam);
            }

            // Generate entries
            StringBuilder stringBuilder = new StringBuilder();
            foreach(DamageDef current in allDamageTypes.Distinct())
            {
                var averageString = "";
                if (allDamageTypes.FindAll(x => x == current).Count > 1)
                {
                    averageString = " (" + "AverageOfAllAttacks".Translate() + ")";
                }
                stringBuilder.AppendLine(current.LabelCap + averageString);
                stringBuilder.AppendLine("  " + GetMeleePenetration(req, current).ToString("0.##"));
                stringBuilder.AppendLine();
            }
            stringBuilder.Length--; // Move back one position to remove last empty line

            return stringBuilder.ToString();
        }
    }
}
