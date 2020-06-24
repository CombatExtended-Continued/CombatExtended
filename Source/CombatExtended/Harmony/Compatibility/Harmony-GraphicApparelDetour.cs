using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;


namespace CombatExtended.HarmonyCE.Compatibility
{

    [HarmonyPatch]
    class GraphicApparelDetour_Disable
    {
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
            ReportOffendingDetourMods();
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
                        yield return method_info;
                    }
                }
            }
        }

        static bool Prefix()
        {
            return false;
        }

        static void ReportOffendingDetourMods()
        {
            List<string> offending_mods = new List<string>();

            foreach (var mod in LoadedModManager.RunningMods)
            {
                foreach (var mod_ass in mod.assemblies.loadedAssemblies)
                {
                    if(target_asses.Contains(mod_ass))
                    {
                        offending_mods.Add(mod.Name);
                    }
                }
            }
            if (offending_mods.Any())
            {
                bool pl = offending_mods.Count > 1;
                Log.Error($"Combat Extended:: An incompatible and outdated detour has been detected and disabled in the following mod{(pl ? "s" : "")}:");
                foreach (var mod_name in offending_mods)
                {
                    Log.Error($"   {mod_name}");
                }
                Log.Error($"Please ask the developer{(pl ? "s" : "")} of {(pl ? "these mods" : "this mod")} to update {(pl ? "them" : "it")}  with a more compatible patching method, such as the Harmony library.");
            }
        }
    }
}
