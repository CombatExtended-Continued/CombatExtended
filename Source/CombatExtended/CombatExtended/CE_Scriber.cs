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
        private static int idCounter = 13;        

        private struct ScribingAction
        {
            public readonly Object owner;
            public readonly Action<string> scribingAction;
            public readonly Action<string> postLoadAction;
            public readonly string id;

            public ScribingAction(Object owner, string id, Action<string> action, Action<string> postLoadAction)
            {
                this.scribingAction = action;
                this.postLoadAction = postLoadAction;
                this.owner = owner;
                this.id = id;
            }            
        }        

        public static void Late(Object owner, Action<string> scribingAction, Action<string> postLoadAction = null)
        {
            string loadingId = null;
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                loadingId = $"{idCounter++}_{Rand.Range(0, (int)1e5) << Rand.Range(0, 32)}";
            }
            Scribe_Values.Look(ref loadingId, "loadingId");
            if (Scribe.mode != LoadSaveMode.Saving)
            {                
                ScribingAction r = new ScribingAction(owner, loadingId, scribingAction, postLoadAction);
                queuedLate.Add(r);
            }
            else
            {                
                try
                {                    
                    scribingAction.Invoke(loadingId);
                }
                catch(Exception er)
                {
                    Log.Error($"CE: Error while scribing {owner} (Late) {er}");
                }
            }
        }

        public static void ExecuteLateScribe()
        {
            Scribe_Values.Look(ref idCounter, "lastscriber_IdCounter", 13);
            for (int i = 0; i < queuedLate.Count; i++)
            {
                if (queuedLate[i].scribingAction != null)
                {
                    try
                    {
                        queuedLate[i].scribingAction.Invoke(queuedLate[i].id);
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
                        queuedLate[i].postLoadAction.Invoke(queuedLate[i].id);
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

