using System.Reflection;
using RimWorld;
using Verse;
using System;

namespace CombatExtended.Loader {
    public interface IModPart {
	public void PostLoad(ModContentPack content, ISettingsCE settings);
	public Type GetSettingsType();
    }
}
