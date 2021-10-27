using System;
using System.Linq;
using Verse;

namespace CombatExtended
{
    public static class CE_Scriber
    {        
        public static void Reference<T>(T scribedThing, string name, ref ThingDef def, ref int id, ref string idString) where T : Thing
        {
            def = null;
            id = -1;
            idString = "";            
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                def = scribedThing.def;
                id = scribedThing.thingIDNumber;
                idString = scribedThing.ThingID;

                Scribe_Defs.Look(ref def, "CE_" + name + "_Def");
                Scribe_Values.Look(ref idString, "CE_" + name + "_IdString");
                Scribe_Values.Look(ref id, "CE_" + name + "_Id");
            }
            else
            {                
                Scribe_Defs.Look(ref def, "CE_" + name + "_Def");
                Scribe_Values.Look(ref idString, "CE_" + name + "_IdString", "");
                Scribe_Values.Look(ref id, "CE_" + name + "_Id", -1);
                if(id == -1 || idString == "")
                {                    
                    return;
                }          
            }
        }

        public static T FindReference<T>(ThingDef def, int id, string idString) where T : Thing
        {
            foreach(Map map in Find.Maps)
            {
                var things = map.listerThings.ThingsOfDef(def);
                if (things.NullOrEmpty())
                {
                    continue;
                }
                var match = things.FirstOrDefault(t => t.thingIDNumber == id || t.ThingID == idString);
                if(match != null)
                {
                    return (T)match;
                }
            }
            return default(T);
        }
    }
}

