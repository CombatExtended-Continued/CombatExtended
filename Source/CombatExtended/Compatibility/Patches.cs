using Verse;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CombatExtended.Compatibility
{
    public class Patches
    {
        private List<IPatch> patches;

        public static List<Func<IEnumerable<ThingDef>>> UsedAmmoCallbacks = new List<Func<IEnumerable<ThingDef>>>();
        public static List<Func<Pawn, Tuple<bool, Vector2>>> CollisionBodyFactorCallbacks = new List<Func<Pawn, Tuple<bool, Vector2>>>();

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

        public static IEnumerable<ThingDef> GetUsedAmmo()
        {
            foreach (var cb in UsedAmmoCallbacks)
            {
                foreach (ThingDef td in cb())
                {
                    yield return td;
                }
            }
        }

        private static bool _gcbfactive = false;

        public static void RegisterCollisionBodyFactorCallback(Func<Pawn, Tuple<bool, Vector2>> f)
        {
            CollisionBodyFactorCallbacks.Add(f);
            _gcbfactive = true;
        }

        public static bool GetCollisionBodyFactors(Pawn pawn, out Vector2 ret)
        {
            ret = new Vector2();
            if (_gcbfactive)
            {
                foreach (Func<Pawn, Tuple<bool, Vector2>> f in CollisionBodyFactorCallbacks)
                {
                    var r = f(pawn);
                    if (r.Item1)
                    {
                        ret = r.Item2;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
