using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;


namespace CombatExtended.Harmony
{

    [HarmonyPatch]
    class GraphicApparelDetour_Disable
    {
        static readonly string logPrefix = Assembly.GetExecutingAssembly().GetName().Name + " :: " + typeof(GraphicApparelDetour_Disable).Name + " :: ";
        static List<Assembly> target_asses = new List<Assembly>();

        static bool Prepare()
        {
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (ass.FullName.Contains("GraphicApparelDetour"))
                {
                    target_asses.Add(ass);
                }
            }
            if (target_asses.Any())
            {
                return true;
            }
            return false;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var ass in target_asses)
            {
                foreach (var type in ass.GetTypes())
                {
                    MethodBase method_info = null;
                    if (type.Name.Contains("InjectorThingy"))
                    {
                        method_info = AccessTools.Method(type, "InjectStuff");
                    }
                    if (method_info != null)
                    {
                        Log.Message($"{logPrefix}Disabling TryGetGraphicApparel detour injection in: {ass.FullName}");
                        yield return method_info;
                    }
                }
            }
        }

        static bool Prefix()
        {
            return false;
        }
    }
}
