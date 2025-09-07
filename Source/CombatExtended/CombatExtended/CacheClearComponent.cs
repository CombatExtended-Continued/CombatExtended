using System;
using System.Collections.Generic;
using Verse;

namespace CombatExtended;
/// <summary>
/// Game component responsible for clearing game-specific caches - things that would become irrelevant when loading or starting a new game.
/// </summary>
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

    /// <summary>
    /// Used to register an action for clearing game-specific caches.
    /// </summary>
    /// <param name="action">An action that will clear cached data, it will be called from <see cref="FinalizeInit"/>.</param>
    public static void AddClearCacheAction(Action action)
    {
        clearCacheActions.Add(action);
    }

    #endregion
}
