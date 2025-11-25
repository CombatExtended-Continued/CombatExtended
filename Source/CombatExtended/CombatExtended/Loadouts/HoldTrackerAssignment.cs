using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace CombatExtended;
public class HoldTrackerAssignment : IExposable
{
    #region Fields

    internal Pawn pawn;
    internal List<HoldRecord> recs;

    #endregion Fields

    public bool Valid
    {
        get
        {
            return ((pawn != null) && (recs != null));
        }
    }

    #region Methods

    public void ExposeData()
    {
        Scribe_References.Look( ref pawn, "pawn" );
        Scribe_Collections.Look( ref recs, "holdRecords" );

#if DEBUG
        Log.Message( Scribe.mode + ", pawn: " + ( pawn == null ? "NULL" : pawn.NameStringShort ) + ", holdRecords: " + ( recs == null ? "NULL" : string.Join(", ", recs.Select(hr => hr.thingDef.ToString()).ToArray()) ) );
#endif
    }

    #endregion Methods
}