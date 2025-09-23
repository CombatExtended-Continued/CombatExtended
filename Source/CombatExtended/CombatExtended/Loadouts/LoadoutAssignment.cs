using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CombatExtended;
public class LoadoutAssignment : IExposable
{
    #region Fields

    internal Loadout loadout;
    internal Pawn pawn;

    #endregion Fields

    public bool Valid
    {
        get
        {
            return ((pawn != null) && (loadout != null));
        }
    }

    #region Methods

    public void ExposeData()
    {
        Scribe_References.Look( ref pawn, "pawn" );
        Scribe_References.Look( ref loadout, "loadout" );

#if DEBUG
        Log.Message( Scribe.mode + ", pawn: " + ( pawn == null ? "NULL" : pawn.NameStringShort ) + ", loadout: " + ( loadout == null ? "NULL" : loadout.label ) );
#endif
    }

    #endregion Methods
}