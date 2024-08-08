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
    /*
      A compiled version of this class resides in our source tree.  Anyone who downloads the
      source tree and installs it uncompiled as a mod will get a warning that they are running
      uncompiled.  During the packaging process this file is removed.  If CombatExtended.dll
      exists in the Assemblies directory, we can assume we are running a local build and omit
      the warning.
      
    */
    public class UncompiledWarning : Mod
    {
        public UncompiledWarning(ModContentPack content) : base(content)
        {
            foreach (Assembly assembly in content.assemblies.loadedAssemblies)
            {
                string name = assembly.GetName().Name;
                if (name == "CombatExtended")
                {
                    return;
                }
            }
            Log.Error("Combat Extended :: Running uncompiled");
            LongEventHandler.QueueLongEvent(ShowUncompiledBuildWarning, "CE_LongEvent_ShowUncompiledBuildWarning", false, null);
        }

        public override string SettingsCategory()
        {
            return "Combat Extended";
        }

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
