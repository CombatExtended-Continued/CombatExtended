using Verse;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Text;

namespace CombatExtended.Compatibility
{
    class Patches
    {
        private List<IPatch> patches;
        public Patches()
        {
            patches = new List<IPatch>();
            Type iPatch = typeof(IPatch);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (iPatch.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                        {
                            IPatch patch = ((IPatch)type.GetConstructor(new Type[] { }).Invoke(new object[] { }));
                            patches.Add(patch);
                        }
                    }
                }
                catch (ReflectionTypeLoadException e)
                {
                    // Report any errors encountered while looking for compatibility patches in an assembly and prevent such errors
                    // from breaking the CE initialization process and cause exceptions at runtime.
                    // Since we're scanning all loaded assemblies, most of the time, type load errors here will be from some unrelated broken mod
                    // that vanilla itself has already excluded from loading via ModAssemblyHandler,
                    // so only log a message if development mode is enabled to reduce logspam.
                    if (Prefs.DevMode)
                    {
                        var errorStringBuilder = new StringBuilder();
                        errorStringBuilder.AppendLine($"[CE] ReflectionTypeLoadException while looking for compat patches in assembly {assembly.GetName().Name}: {e}");
                        errorStringBuilder.AppendLine();
                        errorStringBuilder.AppendLine("Loader exceptions:");

                        if (e.LoaderExceptions != null)
                        {
                            foreach (var loaderException in e.LoaderExceptions)
                            {
                                errorStringBuilder.AppendLine("   => " + loaderException.ToString());
                            }
                        }

                        Log.Warning(errorStringBuilder.ToString());
                    }
                }
            }
        }

        public void Install()
        {
            foreach (IPatch patch in patches)
            {
                if (patch.CanInstall())
                {
                    patch.Install();
                }
            }
        }

        public IEnumerable<string> GetCompatList()
        {
            foreach (IPatch patch in patches)
            {
                if (patch.CanInstall())
                {
                    foreach (string s in patch.GetCompatList())
                    {
                        yield return s;
                    }
                }
            }
        }
    }
}
