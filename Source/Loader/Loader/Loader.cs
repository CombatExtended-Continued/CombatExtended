using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using System.Reflection;
using System.IO;

namespace CombatExtended.Loader
{
    public class Loader : Mod
    {
        /*
          This class handles loading and initializing the different components of CombatExtended.
          If the main CombatExtended.dll is unavailable, it will load an outdated one which is
          sufficient to allow the game to reach the main menu.  It will then display a warning to
          the user that they are running the uncompiled git sources rather than a real CombatExtend
          release.

          DLLs in the main CombatExtended/Assemblies directory are automatically loaded before any
          code here is run.

          DLLs in CombatExtended/AssembliesCore are loaded by Loader's constructor.

          After all core CombatExtended assemblies are loaded, all classes implementing
          `CombatExtended.Loader.IModPart` are instantiated, and their list of required Compat modules
          is queried.  These compat modules are loaded (and *their* Compat list is queried) until all
          required modules are loaded.

          Then each IModPart's PostLoad method is called, with an ISettingsCE if desired (or null).
        */
        public static List<ISettingsCE> settingList = new List<ISettingsCE>();
        private static Loader instance = null;
        private Vector2 scrollPosition;

        private ModContentPack content;

        private Assembly _loadCompatAssembly(string name)
        {
            DirectoryInfo locationInfo = new DirectoryInfo(content.RootDir).GetDirectories("AssembliesCompat").FirstOrFallback(null);
            if (locationInfo == null)
            {
                Log.Error("Combat Extended :: Cannot find compat assembly directory");
            }
            FileInfo file = locationInfo.GetFiles(name + ".dll").FirstOrFallback(null);
            if (file == null)
            {
                Log.Error("Combat Extended :: Cannot find compat assembly for " + name);
            }
            return _loadAssembly(file);
        }

        private Assembly _loadAssembly(FileInfo file)
        {
            Log.Message("Combat Extended :: Loading " + file.FullName);
            byte[] rawAssembly = File.ReadAllBytes(file.FullName);

            Assembly assembly;

            FileInfo pdbFile = new FileInfo(Path.Combine(file.DirectoryName, Path.GetFileNameWithoutExtension(file.FullName)) + ".pdb");
            if (pdbFile.Exists)
            {
                assembly = AppDomain.CurrentDomain.Load(rawAssembly, File.ReadAllBytes(pdbFile.FullName));
            }
            else
            {
                assembly = AppDomain.CurrentDomain.Load(rawAssembly);
            }
            if (assembly != null)
            {
                content.assemblies.loadedAssemblies.Add(assembly);
            }

            return assembly;
        }

        public Loader(ModContentPack content) : base(content)
        {
            Loader.instance = this;
            this.content = content;
            bool found = false;
            Dictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>();

            foreach (Assembly assembly in content.assemblies.loadedAssemblies)
            {
                string name = assembly.GetName().Name;
                assemblies[name] = assembly;
                if (name == "CombatExtended")
                {
                    found = true;
                }
            }


            DirectoryInfo locationInfo = new DirectoryInfo(content.RootDir).GetDirectories("AssembliesCore").FirstOrFallback(null);
            if (locationInfo != null)
            {
                foreach (FileInfo fileInfo in locationInfo.GetFiles())
                {
                    if (fileInfo.Name.EndsWith(".dll"))
                    {
                        Assembly assembly = _loadAssembly(fileInfo);
                        assemblies[assembly.GetName().Name] = assembly;
                    }
                }
            }

            Queue<Assembly> toProcess = new Queue<Assembly>(content.assemblies.loadedAssemblies);
            List<IModPart> modParts = new List<IModPart>();
            HashSet<string> compatParts = new HashSet<string>();

            while (toProcess.Any())
            {
                Assembly assembly = toProcess.Dequeue();

                foreach (Type t in assembly.GetTypes().Where(x => typeof(IModPart).IsAssignableFrom(x) && !x.IsAbstract))
                {
                    IModPart imp = ((IModPart)t.GetConstructor(new Type[] { }).Invoke(new object[] { }));
                    modParts.Add(imp);
                    foreach (string compatPart in imp.GetCompatList())
                    {
                        if (!compatParts.Contains(compatPart))
                        {
                            compatParts.Add(compatPart);
                            Assembly compatAssembly = _loadCompatAssembly(compatPart);
                            toProcess.Enqueue(compatAssembly);
                        }
                    }
                }

            }


            foreach (IModPart modPart in modParts)
            {
                Type settingsType = modPart.GetSettingsType();
                ISettingsCE settings = null;
                if (settingsType != null)
                {
                    if (typeof(ModSettings).IsAssignableFrom(settingsType))
                    {
                        settings = (ISettingsCE)typeof(Loader).GetMethod(nameof(Loader.GetSettings)).MakeGenericMethod(settingsType).Invoke(instance, null);
                    }
                    else
                    {
                        settings = (ISettingsCE)settingsType.GetConstructor(new Type[] { }).Invoke(new object[] { });
                    }
                    settingList.Add(settings);
                }

                modPart.PostLoad(content, settings);

            }


            if (!found)
            {
                Log.Error("Combat Extended :: Running uncompiled");
                LongEventHandler.QueueLongEvent(ShowUncompiledBuildWarning, "CE_LongEvent_ShowUncompiledBuildWarning", false, null);
                _loadAssembly(new DirectoryInfo(content.RootDir)
                              .GetDirectories("Source")
                              .FirstOrFallback(null)?
                              .GetDirectories("packages")
                              .FirstOrFallback(null)?.GetFiles("CombatExtended.dll")
                              .FirstOrFallback(null));
            }

        }

        public override string SettingsCategory()
        {
            return "Combat Extended";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect inner = inRect.ContractedBy(20f);
            inner.height = 800f;
            inner.x += 10f;
            Widgets.BeginScrollView(inRect, ref this.scrollPosition, inner, true);
            Listing_Standard list = new Listing_Standard();
            list.ColumnWidth = (inner.width - 17) / 2; // Subtract 17 for gap between columns
            list.Begin(inner);

            foreach (ISettingsCE settings in settingList)
            {
                settings.DoWindowContents(list);
            }
            list.End();
            Widgets.EndScrollView();
        }

        //Unused method is only here for reference, the repository assembly uses it to warn users to get a compiled build.
        private static void ShowUncompiledBuildWarning()
        {
            Log.Error("You are running CE Uncompiled.  See https://github.com/CombatExtended-Continued/CombatExtended#development-version for details");
            var continueAnywayAction = new Action(() =>
            {

            });
            var getDevBuildAction = new Action(() =>
            {
                Application.OpenURL("https://github.com/CombatExtended-Continued/CombatExtended#development-version");
            });

            var dialog = new Dialog_MessageBox("CE_UncompiledDevBuild".Translate(), "CE_ContinueAnyway".Translate(), continueAnywayAction, "CE_GetCompiledDevBuild".Translate(), getDevBuildAction, null, true);
            Find.WindowStack.Add(dialog);
        }
    }
}
