using System;
using System.Collections.Generic;
using CombatExtended.Detours;
using RimWorld;
using Verse;

namespace CombatExtended;
public class TabInjector : SpecialInjector
{
    #region Methods

    public override bool Inject()
    {
        // get reference to lists of itabs
        List<Type> itabs = ThingDefOf.Human.inspectorTabs;
        List<InspectTabBase> itabsResolved = ThingDefOf.Human.inspectorTabsResolved;

        /*

        #if DEBUG
        Log.Message( "Inspector tab types on humans:" );
        foreach ( var tab in itabs )
        {
            Log.Message( "\t" + tab.Name );
        }
        Log.Message( "Resolved tab instances on humans:" );
        foreach ( var tab in itabsResolved )
        {
            Log.Message( "\t" + tab.labelKey.Translate() );
        }
        #endif
        */

        // replace ITab in the unresolved list
        int index = itabs.IndexOf(typeof(ITab_Pawn_Gear));
        if (index != -1)
        {
            itabs.Remove(typeof(ITab_Pawn_Gear));
            itabs.Insert(index, typeof(ITab_Inventory));
        }

        // replace resolved ITab, if needed.
        InspectTabBase oldGearTab = InspectTabManager.GetSharedInstance(typeof(ITab_Pawn_Gear));
        InspectTabBase newGearTab = InspectTabManager.GetSharedInstance(typeof(ITab_Inventory));
        if (!itabsResolved.NullOrEmpty() && itabsResolved.Contains(oldGearTab))
        {
            int resolvedIndex = itabsResolved.IndexOf(oldGearTab);
            itabsResolved.Insert(resolvedIndex, newGearTab);
            itabsResolved.Remove(oldGearTab);
        }

        return true;
    }

    #endregion Methods
}