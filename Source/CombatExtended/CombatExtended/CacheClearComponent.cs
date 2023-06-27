using System;
using System.Collections.Generic;
using Verse;

namespace CombatExtended
{
    public class CacheClearComponent : GameComponent
    {
        #region Fields

        private static HashSet<Action> clearCacheActions = new HashSet<Action>();

        #endregion

        #region Constructors

        // Required constructor for game components
        public CacheClearComponent(Game _)
        {
        }

        #endregion

        #region Methods

        public override void FinalizeInit()
        {
            foreach (Action clearAction in clearCacheActions)
            {
                clearAction();
            }
        }

        public static void AddClearCacheAction(Action action)
        {
            clearCacheActions.Add(action);
        }

        #endregion
    }
}