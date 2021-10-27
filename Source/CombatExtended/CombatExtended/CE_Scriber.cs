using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Verse;

namespace CombatExtended
{
    public static class CE_Scriber 
    {
        private static List<ScribingAction> queuedLate = new List<ScribingAction>();

        private struct ScribingAction
        {
            public readonly Object owner;
            public readonly Action scribingAction;
            public readonly Action postLoadAction;

            public ScribingAction(Object owner, Action action, Action postLoadAction)
            {
                this.scribingAction = action;
                this.postLoadAction = postLoadAction;
                this.owner = owner;
            }            
        }        

        public static void Late(Object owner, Action scribingAction, Action postLoadAction = null)
        {
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                ScribingAction r = new ScribingAction(owner, scribingAction, postLoadAction);
                queuedLate.Add(r);
            }
            else
            {
                try
                {
                    scribingAction.Invoke();
                }
                catch(Exception er)
                {
                    Log.Error($"CE: Error while scribing {owner} (Late) {er}");
                }
            }
        }

        public static void ExecuteLateScribe()
        {            
            for (int i = 0; i < queuedLate.Count; i++)
            {
                if (queuedLate[i].scribingAction != null)
                {
                    try
                    {
                        queuedLate[i].scribingAction.Invoke();
                    }
                    catch(Exception er)
                    {
                        Log.Error($"CE: Error while scribing {queuedLate[i].owner} (ExecuteLateScribe) {er}");
                    }
                }
            }
        }

        public static void Reset()
        {           
            for (int i = 0; i < queuedLate.Count; i++)
            {
                if (queuedLate[i].postLoadAction != null)
                {
                    try
                    {
                        queuedLate[i].postLoadAction.Invoke();
                    }
                    catch(Exception er)
                    {
                        Log.Error($"CE: Error while scribing {queuedLate[i].owner} (reset) {er}");
                    }
                }
            }
            queuedLate.Clear();
        }
    }
}

