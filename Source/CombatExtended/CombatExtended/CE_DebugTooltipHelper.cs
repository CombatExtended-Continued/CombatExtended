using System;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using RimWorld.Planet;
using System.Linq;
using HarmonyLib;
using System.Reflection;
using System.Text;
using RimWorld;

namespace CombatExtended
{    
    public class CE_DebugTooltipHelper : GameComponent
    {
        private StringBuilder builder = new StringBuilder();

        private static List<Pair<Func<Map, IntVec3, string>, KeyCode>> mapCallbacks = new List<Pair<Func<Map, IntVec3, string>, KeyCode>>();
        private static List<Pair<Func<World, int, string>, KeyCode>> worldCallbacks = new List<Pair<Func<World, int, string>, KeyCode>>();
        
        private static readonly Rect MouseRect = new Rect(0, 0, 50, 50);

        static CE_DebugTooltipHelper()
        {            
            var functions = typeof(CE_DebugTooltipHelper).Assembly
                .GetTypes()
                .SelectMany(t => t.GetMethods(AccessTools.all))
                .Where(m => m.HasAttribute<CE_DebugTooltip>() && m.IsStatic);
            foreach (MethodBase m in functions)
            {
                CE_DebugTooltip attribute = m.TryGetAttribute<CE_DebugTooltip>();
                if (attribute.tooltipType == CE_DebugTooltipType.World)
                {
                    ParameterInfo[] param = m.GetParameters();
                    if (param[0].ParameterType != typeof(World) || param[1].ParameterType != typeof(int))
                    {
                        Log.Error($"CE: Error processing debug tooltip {m.GetType().Name}:{m.Name} {m.FullDescription()} need to have (World, int) as parameters, skipped");
                        continue;
                    }
                    worldCallbacks.Add(new Pair<Func<World, int, string>, KeyCode>((world, tile) => (string)m.Invoke(null, new object[] { world, tile }), attribute.altKey));                                        
                }
                else if (attribute.tooltipType == CE_DebugTooltipType.Map)
                {
                    ParameterInfo[] param = m.GetParameters();
                    if (param[0].ParameterType != typeof(Map) || param[1].ParameterType != typeof(IntVec3))
                    {
                        Log.Error($"CE: Error processing debug tooltip {m.GetType().Name}:{m.Name} {m.FullDescription()} need to have (Map, IntVec3) as parameters, skipped");
                        continue;
                    }
                    mapCallbacks.Add(new Pair<Func<Map, IntVec3, string>, KeyCode>((map, cell) => (string)m.Invoke(null, new object[] { map, cell }), attribute.altKey));                    
                }                
            }
        }

        public CE_DebugTooltipHelper(Game game)
        {          
        }
               
        public override void GameComponentUpdate()
        {
            base.GameComponentUpdate();
            if (!Controller.settings.ShowExtraTooltips || !Input.anyKey || Find.CurrentMap == null || Current.ProgramState != ProgramState.Playing)
            {
                return;
            }
            Rect mouseRect;

            mouseRect = MouseRect;
            mouseRect.center = Event.current.mousePosition;
            Camera worldCamera = Find.WorldCamera;
            
            if (!worldCamera.gameObject.activeInHierarchy)
            {
                IntVec3 mouseCell = UI.MouseCell();

                if (mouseCell.InBounds(Find.CurrentMap))
                {                    
                    TryMapTooltips(mouseRect, mouseCell);                                    
                }
            }
            else
            {
                int tile = GenWorld.MouseTile();

                if (tile != -1)
                {
                    TryWorldTooltips(mouseRect, tile);
                }
            }           
        }

        private void TryMapTooltips(Rect mouseRect, IntVec3 mouseCell)
        {
            bool bracketShown = false;
            for(int i = 0;i < mapCallbacks.Count; i++)
            {
                Pair<Func<Map, IntVec3, string>, KeyCode> callback = mapCallbacks[i];                
                if (Input.GetKey(callback.Second == KeyCode.None ? KeyCode.LeftShift : callback.Second))
                {
		    string message;
		    try
		    {
			message = callback.First(Find.CurrentMap, mouseCell);
		    }
		    catch (Exception e)
		    {
			Log.Error(e.ToString());
			message = "Debug Callback failed (see log for details)";
		    }
                    if (!message.NullOrEmpty())
                    {
                        DoTipSignal(mouseRect, message);
                    }
                    if (!bracketShown)
                    {
                        GenUI.RenderMouseoverBracket();
                        bracketShown = true;
                    }
                }                                
            }                      
        }

        private void TryWorldTooltips(Rect mouseRect, int tile)
        {                                
            for (int i = 0; i < worldCallbacks.Count; i++)
            {
                Pair<Func<World, int, string>, KeyCode> callback = worldCallbacks[i];
                if (Input.GetKey(callback.Second == KeyCode.None ? KeyCode.LeftShift : callback.Second))
                {
                    string message = callback.First(Find.World, tile);
                    if (!message.NullOrEmpty())
                    {
                        DoTipSignal(mouseRect, message);
                    }
                }                          
            }            
        }

        private TipSignal DoTipSignal(Rect rect, string message)
        {
            builder.Clear();
            builder.Append("<color=orange>CE_DEBUG_TIP:</color>. ");
            builder.Append(message);
            TipSignal tip = new TipSignal();
            tip.text = message;            
            tip.priority = (TooltipPriority)3;
            TooltipHandler.TipRegion(rect, tip);
            return tip;
        }
    }
}
