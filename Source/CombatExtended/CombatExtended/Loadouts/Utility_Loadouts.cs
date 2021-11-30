using System.Text.RegularExpressions;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    [StaticConstructorOnStartup]
    public static class Utility_Loadouts
    {
        #region Fields


        private static float _labelSize = -1f;
        private static float _margin = 6f;
        private static Texture2D _overburdenedTex;

        // Base values for WeightCapacity and BulkCapacity are no longer meaningful,
        // instead the median values are calculated from the colonists themselves
        // via UpdateColonistCapacities
        public static float medianWeightCapacity = 0f;
        public static float medianBulkCapacity = 0f;

        #endregion Fields

        #region Properties

        public static float LabelSize
        {
            get
            {
                if (_labelSize < 0)
                {
                    // get size of label
                    _labelSize = (_margin + Math.Max(Text.CalcSize("CE_Weight".Translate()).x, Text.CalcSize("CE_Bulk".Translate()).x));
                }
                return _labelSize;
            }
        }

        public static Texture2D OverburdenedTex
        {
            get
            {
                if (_overburdenedTex == null)
                    _overburdenedTex = SolidColorMaterials.NewSolidColorTexture(Color.red);
                return _overburdenedTex;
            }
        }

        #endregion Properties

        #region Methods

        public static void DrawBar(Rect canvas, float current, float capacity, string label = "", string tooltip = "")
        {
            // rects
            Rect labelRect = new Rect(canvas);
            Rect barRect = new Rect(canvas);
            if (label != "")
                barRect.xMin += LabelSize;
            labelRect.width = LabelSize;

            // label
            if (label != "")
                Widgets.Label(labelRect, label);

            // bar
            bool overburdened = current > capacity;
            float fillPercentage = overburdened ? 1f : (float.IsNaN(current / capacity) ? 1f : current / capacity);
            if (overburdened)
            {
                Widgets.FillableBar(barRect, fillPercentage, OverburdenedTex);
                DrawBarThreshold(barRect, capacity / current, 1f);
            }
            else
                Widgets.FillableBar(barRect, fillPercentage);

            // tooltip
            if (tooltip != "")
                TooltipHandler.TipRegion(canvas, tooltip);
        }

        public static void DrawBarThreshold(Rect barRect, float pct, float curLevel = 1f)
        {
            float thresholdBarWidth = (float)((barRect.width <= 60f) ? 1 : 2);

            Rect position = new Rect(barRect.x + barRect.width * pct - (thresholdBarWidth - 1f), barRect.y + barRect.height / 2f, thresholdBarWidth, barRect.height / 2f);
            Texture2D image;
            if (pct < curLevel)
            {
                image = BaseContent.BlackTex;
                GUI.color = new Color(1f, 1f, 1f, 0.9f);
            }
            else
            {
                image = BaseContent.GreyTex;
                GUI.color = new Color(1f, 1f, 1f, 0.5f);
            }
            GUI.DrawTexture(position, image);
            GUI.color = Color.white;
        }

        public static string GetBulkTip(this Loadout loadout)
        {
            float workSpeedFactor = MassBulkUtility.WorkSpeedFactor(loadout.Bulk, medianBulkCapacity);

            return "CE_DetailedBaseBulkTip".Translate(
                CE_StatDefOf.CarryBulk.ValueToString(medianBulkCapacity, CE_StatDefOf.CarryBulk.toStringNumberSense),
                CE_StatDefOf.CarryBulk.ValueToString(loadout.Bulk, CE_StatDefOf.CarryBulk.toStringNumberSense),
                workSpeedFactor.ToStringPercent());
        }

        public static string GetBulkTip(this Pawn pawn)
        {
            CompInventory comp = pawn.TryGetComp<CompInventory>();
            if (comp != null)
                return "CE_DetailedBulkTip".Translate(CE_StatDefOf.CarryBulk.ValueToString(comp.capacityBulk, CE_StatDefOf.CarryBulk.toStringNumberSense), CE_StatDefOf.CarryBulk.ValueToString(comp.currentBulk, CE_StatDefOf.CarryBulk.toStringNumberSense),
                                                       comp.workSpeedFactor.ToStringPercent());
            else
                return String.Empty;
        }

        public static string GetBulkTip(this Thing thing, int count = 1)
        {
            return
                "CE_Bulk".Translate() + ": " + CE_StatDefOf.Bulk.ValueToString(thing.GetStatValue(CE_StatDefOf.Bulk) * count, CE_StatDefOf.Bulk.toStringNumberSense);
        }

        public static string GetBulkTip(this ThingDef def, int count = 1)
        {
            return
                "CE_Bulk".Translate() + ": " + CE_StatDefOf.Bulk.ValueToString(def.GetStatValueAbstract(CE_StatDefOf.Bulk) * count, CE_StatDefOf.Bulk.toStringNumberSense);
        }

        public static Loadout GetLoadout(this Pawn pawn)
        {
            if (pawn == null)
                throw new ArgumentNullException("pawn");

            Loadout loadout;
            if (!LoadoutManager.AssignedLoadouts.TryGetValue(pawn, out loadout))
            {
                LoadoutManager.AssignedLoadouts.Add(pawn, LoadoutManager.DefaultLoadout);
                loadout = LoadoutManager.DefaultLoadout;
            }
            return loadout;
        }

        public static int GetLoadoutId(this Pawn pawn)
        {
            return GetLoadout(pawn).uniqueID;
        }

        public static string GetWeightAndBulkTip(this Loadout loadout)
        {
            return loadout.GetWeightTip() + "\n\n" + loadout.GetBulkTip();
        }

        public static string GetWeightAndBulkTip(this Pawn pawn)
        {
            return pawn.GetWeightTip() + "\n\n" + pawn.GetBulkTip();
        }

        public static string GetWeightAndBulkTip(this Thing thing)
        {
            return thing.GetWeightTip(thing.stackCount) + "\n" + thing.GetBulkTip(thing.stackCount);
        }

        public static string GetWeightAndBulkTip(this ThingDef def, int count = 1)
        {
            return def.LabelCap +
                (count != 1 ? " x" + count : "") +
                "\n" + def.GetWeightTip(count) + "\n" + def.GetBulkTip(count);
        }

        public static string GetWeightAndBulkTip(this LoadoutGenericDef def, int count = 1)
        {
            return "CE_Weight".Translate() + ": " + StatDefOf.Mass.ValueToString(def.mass * count, StatDefOf.Mass.toStringNumberSense) + "\n" +
                "CE_Bulk".Translate() + ": " + CE_StatDefOf.Bulk.ValueToString(def.bulk * count, CE_StatDefOf.Bulk.toStringNumberSense);
        }

        public static string GetWeightTip(this ThingDef def, int count = 1)
        {
            return
                "CE_Weight".Translate() + ": " + StatDefOf.Mass.ValueToString(def.GetStatValueAbstract(StatDefOf.Mass) * count, StatDefOf.Mass.toStringNumberSense);
            //old "CE_Weight".Translate() + ": " + CE_StatDefOf.Weight.ValueToString(def.GetStatValueAbstract(CE_StatDefOf.Weight) * count, CE_StatDefOf.Weight.toStringNumberSense);
        }

        public static string GetWeightTip(this Thing thing, int count = 1)
        {
            return
                "CE_Weight".Translate() + ": " + StatDefOf.Mass.ValueToString(thing.GetStatValue(StatDefOf.Mass) * count, StatDefOf.Mass.toStringNumberSense);
            //old "CE_Weight".Translate() + ": " + CE_StatDefOf.Weight.ValueToString(thing.GetStatValue(CE_StatDefOf.Weight) * count, CE_StatDefOf.Weight.toStringNumberSense);
        }

        public static string GetWeightTip(this Loadout loadout)
        {
            float moveSpeedFactor = MassBulkUtility.MoveSpeedFactor(loadout.Weight, medianWeightCapacity);
            float encumberPenalty = MassBulkUtility.EncumberPenalty(loadout.Weight, medianWeightCapacity);

            return "CE_DetailedBaseWeightTip".Translate(CE_StatDefOf.CarryWeight.ValueToString(medianWeightCapacity, CE_StatDefOf.CarryWeight.toStringNumberSense), CE_StatDefOf.CarryWeight.ValueToString(loadout.Weight, CE_StatDefOf.CarryWeight.toStringNumberSense),
                                                 moveSpeedFactor.ToStringPercent(),
                                                 encumberPenalty.ToStringPercent());
        }

        public static string GetWeightTip(this Pawn pawn)
        {
            CompInventory comp = pawn.TryGetComp<CompInventory>();
            if (comp != null)
                return "CE_DetailedWeightTip".Translate(CE_StatDefOf.CarryWeight.ValueToString(comp.capacityWeight, CE_StatDefOf.CarryWeight.toStringNumberSense), CE_StatDefOf.CarryWeight.ValueToString(comp.currentWeight, CE_StatDefOf.CarryWeight.toStringNumberSense),
                                                     comp.moveSpeedFactor.ToStringPercent(),
                                                     comp.encumberPenalty.ToStringPercent());
            else
                return "";
        }

        public static void SetLoadout(this Pawn pawn, Loadout loadout)
        {
            if (pawn == null)
                throw new ArgumentNullException("pawn");

            if (LoadoutManager.AssignedLoadouts.ContainsKey(pawn))
                LoadoutManager.AssignedLoadouts[pawn] = loadout;
            else
                LoadoutManager.AssignedLoadouts.Add(pawn, loadout);
        }

        public static void SetLoadoutById(this Pawn pawn, int loadoutId)
        {
            Loadout loadout = LoadoutManager.GetLoadoutById(loadoutId);
            if (loadout == null)
                throw new ArgumentNullException("loadout");

            SetLoadout(pawn, loadout);
        }

        public static void UpdateColonistCapacities()
        {
            Pawn[] colonists = PawnsFinder.AllMaps_FreeColonists.ToArray();

            if (colonists.Length > 0)
            {
                // The median calculation is O(n log n) but it could be made O(n) with a little effort
                // Due to the small number of colonists, and because the values only need to be updated occasionally,
                // this shouldn't be a problem.
                medianWeightCapacity = Median(colonists.Select(c => c.GetStatValue(CE_StatDefOf.CarryWeight)).ToArray());
                medianBulkCapacity = Median(colonists.Select(c => c.GetStatValue(CE_StatDefOf.CarryBulk)).ToArray());
            }
            else
            {
                // Use the base values
                medianWeightCapacity = ThingDefOf.Human.GetStatValueAbstract(CE_StatDefOf.CarryWeight);
                medianBulkCapacity = ThingDefOf.Human.GetStatValueAbstract(CE_StatDefOf.CarryBulk);
            }
        }

        private static float Median(float[] xs)
        {
            var ys = xs.OrderBy(x => x).ToArray();
            if (ys.Length == 0)
            {
                Log.Error("Combat Extended :: Utility_Loadouts :: Median: Nonzero-length array");
                return 0;
            }
            else if (ys.Length % 2 == 0)
                return (ys[(int)(ys.Length / 2) - 1] + ys[(int)(ys.Length / 2)]) / 2;
            else
                return ys[Mathf.FloorToInt(ys.Length / 2f)];
        }

        /// <summary>
        /// Generates a loadout from a pawn's current equipment and inventory.  Attempts to put items which fit in Generics that are default/DropExcess into said Generic.
        /// </summary>
        /// <param name="pawn">Pawn to check equipment/inventory on and generate a Loadout from.</param>
        /// <returns>Loadout which was generated based on Pawn's inventory.</returns>
        public static Loadout GenerateLoadoutFromPawn(this Pawn pawn)
        {
            // generate the name for this new pawn based loadout.
            string newName = string.Concat(pawn.Name.ToStringShort, " ", "CE_DefaultLoadoutName".Translate());
            Regex reNum = new Regex(@"^(.*?)\d+$");
            if (reNum.IsMatch(newName))
                newName = reNum.Replace(newName, @"$1");
            newName = LoadoutManager.GetUniqueLabel(newName);

            // set basic loadout properties.
            Loadout loadout = new Loadout(newName);
            loadout.defaultLoadout = false;
            loadout.canBeDeleted = true;

            LoadoutSlot slot = null;
            
            // grab the pawn's current equipment as a loadoutslot.
            if (pawn.equipment?.Primary != null)
            {                
                slot = new LoadoutSlot(pawn.equipment.Primary.def);
                loadout.AddSlot(slot);
                if (pawn.equipment.Primary is WeaponPlatform platform && !platform.attachments.NullOrEmpty())
                    slot.attachments.AddRange(platform.attachments.Select(l => l.attachment));                                   
            }

            // get a list of generics which are drop only.  Assumes that anything that doesn't fit here is a Specific slot later.
            IEnumerable<LoadoutGenericDef> generics = DefDatabase<LoadoutGenericDef>.AllDefs.Where(gd => gd.defaultCountType == LoadoutCountType.dropExcess);

            // enumerate each item in the pawn's inventory and add appropriate slots.
            foreach (Thing thing in pawn.inventory.innerContainer)
            {
                LoadoutGenericDef foundGeneric = null;
                // first check if it's a generic-able item...
                foreach (LoadoutGenericDef generic in generics)
                {
                    if (generic.lambda(thing.def))
                    {
                        foundGeneric = generic;
                        break;
                    }
                }

                // assign a loadout slot that fits the thing.
                if (foundGeneric != null)
                {
                    slot = new LoadoutSlot(foundGeneric, thing.stackCount);
                }
                else
                {
                    slot = new LoadoutSlot(thing.def, thing.stackCount);
                }

                // add the slot (this also takes care of adding to existing slots)
                loadout.AddSlot(slot);
            }

            // finally check the loadout and make sure that it has sufficient generics like what happens with a new loadout in the management UI.
            foreach (LoadoutGenericDef generic in generics.Where(gd => gd.isBasic))
            {
                slot = loadout.Slots.FirstOrDefault(s => s.genericDef == generic);
                if (slot != null)
                {
                    if (slot.count < slot.genericDef.defaultCount)
                        slot.count = slot.genericDef.defaultCount;
                }
                else
                {
                    slot = new LoadoutSlot(generic);
                    loadout.AddSlot(slot);
                }
            }

            return loadout;
        }

        public static bool IsItemQuestLocked(this Pawn pawn, Thing thing)
        {
            if (pawn == null || thing == null)
            {
                return false;
            }
            return (thing is Apparel eqApparel && (pawn.apparel?.IsLocked(eqApparel) ?? false))
                || (thing.def.IsWeapon && pawn.IsQuestLodger() && !EquipmentUtility.QuestLodgerCanUnequip(thing, pawn));
        }

        #endregion Methods
    }
}
