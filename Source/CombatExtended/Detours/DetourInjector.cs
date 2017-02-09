using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.Detours
{
    [StaticConstructorOnStartup]
    internal static class DetourInjector
    {
        static DetourInjector()
        {
            LongEventHandler.QueueLongEvent(Inject, "LibraryStartup", false, null);
        }

        public static void Inject()
        {
            if (InjectDetours()) Log.Message("Combat Realism :: Detours successfully injected");
            else Log.Error("Combat Realism :: Failed to inject detours");
        }

        public static bool InjectDetours()
        {
            // Detour VerbsTick
            if (!Detours.TryDetourFromTo(typeof(VerbTracker).GetMethod("VerbsTick", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours_VerbTracker).GetMethod("VerbsTick", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            // Detour TooltipUtility
            if (!Detours.TryDetourFromTo(typeof(TooltipUtility).GetMethod("ShotCalculationTipString", BindingFlags.Static | BindingFlags.Public),
                typeof(Detours_TooltipUtility).GetMethod("ShotCalculationTipString", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            // Detour FloatMenuMakerMap
            if (!Detours.TryDetourFromTo(typeof(FloatMenuMakerMap).GetMethod("ChoicesAtFor", BindingFlags.Static | BindingFlags.Public),
                typeof(Detours_FloatMenuMakerMap).GetMethod("ChoicesAtFor", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            // *************************************
            // *** Detour Inventory methods ***
            // *************************************

            // ThingContainer

            MethodInfo tryAddSource = typeof(ThingContainer).GetMethod("TryAdd", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(Thing), typeof(bool) }, null);
            if (!Detours.TryDetourFromTo(tryAddSource, typeof(Detours_ThingContainer).GetMethod("TryAdd", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            MethodInfo tryDrop2Source = typeof(ThingContainer).GetMethod("TryDrop",
                 BindingFlags.Instance | BindingFlags.Public,
                 null,
                 new Type[] { typeof(Thing), typeof(IntVec3), typeof(Map), typeof(ThingPlaceMode), typeof(int), typeof(Thing).MakeByRefType(), typeof(Action<Thing, int>) },
                 null);

            MethodInfo tryDrop2Dest = typeof(Detours_ThingContainer).GetMethod("TryDrop",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(ThingContainer), typeof(Thing), typeof(IntVec3), typeof(Map), typeof(ThingPlaceMode), typeof(int), typeof(Thing).MakeByRefType(), typeof(Action<Thing, int>) },
                null);

            if (!Detours.TryDetourFromTo(tryDrop2Source, tryDrop2Dest))
                return false;

            if (!Detours.TryDetourFromTo(typeof(ThingContainer).GetMethod("Get", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours_ThingContainer).GetMethod("Get", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            if (!Detours.TryDetourFromTo(typeof(ThingContainer).GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours_ThingContainer).GetMethod("Remove", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            // Pawn_ApparelTracker

            MethodInfo tryDrop3Source = typeof(Pawn_ApparelTracker).GetMethod("TryDrop",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new Type[] { typeof(Apparel), typeof(Apparel).MakeByRefType(), typeof(IntVec3), typeof(bool) },
                null);

            MethodInfo tryDrop3Dest = typeof(Detours_Pawn_ApparelTracker).GetMethod("TryDrop",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(Pawn_ApparelTracker), typeof(Apparel), typeof(Apparel).MakeByRefType(), typeof(IntVec3), typeof(bool) },
                null);

            if (!Detours.TryDetourFromTo(tryDrop3Source, tryDrop3Dest))
                return false;

            if (!Detours.TryDetourFromTo(typeof(Pawn_ApparelTracker).GetMethod("Wear", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours_Pawn_ApparelTracker).GetMethod("Wear", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            if (!Detours.TryDetourFromTo(typeof(Pawn_ApparelTracker).GetMethod("Notify_WornApparelDestroyed", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(Detours_Pawn_ApparelTracker).GetMethod("Notify_WornApparelDestroyed", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            // Pawn_EquipmentTracker

            if (!Detours.TryDetourFromTo(typeof(Pawn_EquipmentTracker).GetMethod("AddEquipment", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours_Pawn_EquipmentTracker).GetMethod("AddEquipment", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            if (!Detours.TryDetourFromTo(typeof(Pawn_EquipmentTracker).GetMethod("Notify_PrimaryDestroyed", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(Detours_Pawn_EquipmentTracker).GetMethod("Notify_PrimaryDestroyed", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            if (!Detours.TryDetourFromTo(typeof(Pawn_EquipmentTracker).GetMethod("TryDropEquipment", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours_Pawn_EquipmentTracker).GetMethod("TryDropEquipment", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            if (!Detours.TryDetourFromTo(typeof(Pawn_EquipmentTracker).GetMethod("TryTransferEquipmentToContainer", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours_Pawn_EquipmentTracker).GetMethod("TryTransferEquipmentToContainer", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            if (!Detours.TryDetourFromTo(typeof(Pawn_EquipmentTracker).GetMethod("TryStartAttack", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours_Pawn_EquipmentTracker).GetMethod("TryStartAttack", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            // MassUtility
            if (!Detours.TryDetourFromTo(typeof(MassUtility).GetMethod("Capacity", BindingFlags.Static | BindingFlags.Public),
                typeof(Detours_MassUtility).GetMethod("Capacity", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            // *************************************
            // *** Detour inventory-related methods ***
            // *************************************

            // WorkGiver_InteractAnimal
            if (!Detours.TryDetourFromTo(typeof(WorkGiver_InteractAnimal).GetMethod("TakeFoodForAnimalInteractJob", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(Detours_WorkGiver_InteractAnimal).GetMethod("TakeFoodForAnimalInteractJob", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            // WorkGiver_HunterHunt
            if (!Detours.TryDetourFromTo(typeof(WorkGiver_HunterHunt).GetMethod("HasHuntingWeapon", BindingFlags.Static | BindingFlags.Public),
                typeof(Detours_WorkGiver_HunterHunt).GetMethod("HasHuntingWeapon", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;

            // TradeDeal
            if (!Detours.TryDetourFromTo(typeof(TradeDeal).GetMethod("UpdateCurrencyCount", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours_TradeDeal).GetMethod("UpdateCurrencyCount", BindingFlags.Static | BindingFlags.NonPublic)))
                return false;


            // *************************************
            // *** Detour additional methods ***
            // *************************************

            // RightTools
            if (!Detours.TryDetourFromTo(typeof(ThinkNode_JobGiver).GetMethod("TryIssueJobPackage", BindingFlags.Instance | BindingFlags.Public),
                typeof(Detours_ThinkNode_JobGiver).GetMethod("TryIssueJobPackage", BindingFlags.Instance | BindingFlags.Public)))
                return false;

            return true;
        }
    }
}
