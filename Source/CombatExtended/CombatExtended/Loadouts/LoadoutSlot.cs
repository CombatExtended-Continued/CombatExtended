using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    // this has been reduced to a thingCount at this point, with the exception of the added default count bit
    // -- Fluffy
    public class LoadoutSlot : IExposable
    {
        #region Fields

        private const int _defaultCount = 1;
        private int _count;
        private ThingDef _def;

        #endregion Fields

        #region Constructors

        public LoadoutSlot( ThingDef def, int count = 1 )
        {
            Count = count;
            Def = def;

            // increase default ammo count
            if ( def is AmmoDef )
                Count = ( (AmmoDef)def ).defaultAmmoCount;
        }

        public LoadoutSlot()
        {
            // for scribe; if Count is set default will be overwritten. Def is always stored/loaded.
            Count = _defaultCount;
        }

        #endregion Constructors

        #region Properties

        public int Count { get { return _count; } set { _count = value; } }
        public ThingDef Def { get { return _def; } set { _def = value; } }

        #endregion Properties

        #region Methods

        public void ExposeData()
        {
            Scribe_Values.LookValue( ref _count, "count", _defaultCount );
            Scribe_Defs.LookDef( ref _def, "def" );
        }

        #endregion Methods
    }
}