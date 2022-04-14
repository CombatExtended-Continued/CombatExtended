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

	private List<Assembly> extraAssemblies;
	private List<ISettingsCE> settingList;
	private ModContentPack content;
	private static Loader instance;

	public static bool LoadCompatAssembly(string name) {
	    Log.Message("Combat Extended :: Loading "+name);

	    return instance._LoadCompatAssembly(name);
	}

	public bool _LoadCompatAssembly(string name) {
	    DirectoryInfo locationInfo = new DirectoryInfo(content.RootDir).GetDirectories("AssembliesCompat").FirstOrFallback(null);
	    if (locationInfo==null || !locationInfo.Exists) {
		LongEventHandler.QueueLongEvent(ShowUncompiledBuildWarning, "CE_LongEvent_ShowUncompiledBuildWarning", false, null);
		return false;

	    }

	    FileInfo file = locationInfo.GetFiles(name+".dll").FirstOrFallback(null);
	    if (file == null || !file.Exists) {
		return false;
	    }
	    return _loadFile(file);

	}

	private bool _loadFile(FileInfo file) {
	    Log.Message("Combat Extended :: Loading "+file.FullName);
	    byte[] rawAssembly = File.ReadAllBytes(file.FullName);

		    
		    
	    Assembly assembly;

	    FileInfo pdbFile = new FileInfo(Path.Combine(file.DirectoryName, Path.GetFileNameWithoutExtension(file.FullName)) + ".pdb");
	    if (pdbFile.Exists) {
		
		assembly = AppDomain.CurrentDomain.Load(rawAssembly, File.ReadAllBytes(pdbFile.FullName));
	    }
	    else {
		assembly = AppDomain.CurrentDomain.Load(rawAssembly);
	    }
	    if (assembly != null) {
		content.assemblies.loadedAssemblies.Add(assembly);
		extraAssemblies.Add(assembly);
		foreach (Type t in assembly.GetTypes().Where(x => typeof(IModPart).IsAssignableFrom(x) && !x.IsAbstract)) {
		    
		    IModPart imp;
		    if (file.Name.EndsWith("CombatExtendedCore.dll")) {
			imp = ((IModPart)t.GetConstructor(new Type[]{typeof(ModContentPack)}).Invoke(new object[]{content}));
		    }
		    else {
			imp = ((IModPart)t.GetConstructor(new Type[]{}).Invoke(new object[]{}));
		    }
		    Type settingType = imp.GetSettingsType();
		    ISettingsCE settings = null;
		    if (settingType != null) {
			settings = (ISettingsCE) typeof(Loader).GetMethod(nameof(Loader.GetSettings)).MakeGenericMethod(settingType).Invoke(this, null);
			settingList.Add(settings);
		    }
		    
		    imp.PostLoad(content, settings);
		    
		    break;
		}
		return true;
	    }
	    return false;
	}
	
        public Loader(ModContentPack content) : base(content)
        {
	    settingList = new List<ISettingsCE>();
	    extraAssemblies = new List<Assembly>();
	    Loader.instance = this;
	    this.content = content;

	    DirectoryInfo locationInfo = new DirectoryInfo(content.RootDir).GetDirectories("AssembliesCompat").FirstOrFallback(null);
	    if (locationInfo==null || !locationInfo.Exists) {
		LongEventHandler.QueueLongEvent(ShowUncompiledBuildWarning, "CE_LongEvent_ShowUncompiledBuildWarning", false, null);
		return;

	    }

	    FileInfo[] files = locationInfo.GetFiles();
	    foreach (FileInfo file in files) {
		if (file.Name.EndsWith(".dll")) {
		    _loadFile(file);
		}
	    }
	    bool found = false;
	    var assembliesInfo = new DirectoryInfo(content.RootDir).GetDirectories("Assemblies").FirstOrFallback(null);
	    if (assembliesInfo!=null) {
		if (assembliesInfo.GetFiles("CombatExtended.dll").FirstOrFallback(null) !=null) {
		    found = true;
		}
	    }
	    if (!found) {
		LongEventHandler.QueueLongEvent(ShowUncompiledBuildWarning, "CE_LongEvent_ShowUncompiledBuildWarning", false, null);
	    }
	    
        }

	public override string SettingsCategory()
        {
            return "Combat Extended Loader";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
	    int offset = 0;
	    foreach (ISettingsCE settings in settingList) {
		settings.DoWindowContents(inRect, ref offset);
	    }
        }



        //Unused method is only here for reference, the repository assembly uses it to warn users to get a compiled build.
        private void ShowUncompiledBuildWarning()
        {
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
