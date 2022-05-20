all: Assemblies/0CombatExtendedLoader.dll Assemblies/CombatExtended.dll CompatAssemblies

.PHONY: all CompatAssemblies

PUBLICIZER := /tmp/AssemblyPublicizer

Assemblies/0CombatExtendedLoader.dll: Source/Loader/Loader.csproj $(wildcard Source/Loader/Loader/*.cs)
	python Make.py --csproj Source/Loader/Loader.csproj --output Assemblies/0CombatExtendedLoader.dll --reference /tmp/rwreference --all-libs $(DOWNLOAD_LIBS)

Assemblies/CombatExtended.dll: Assemblies/0CombatExtendedLoader.dll Source/CombatExtended/CombatExtended.csproj $(wildcard Source/CombatExtended/*/*.cs) $(wildcard Source/CombatExtended/*/*/*.cs)
	python Make.py --csproj Source/CombatExtended/CombatExtended.csproj --output Assemblies/CombatExtended.dll --reference /tmp/rwreference --all-libs $(DOWNLOAD_LIBS) --publicizer $(PUBLICIZER)


CompatAssemblies:
	DOWNLOAD_LIBS="--reference=/tmp/rwreference" PUBLICIZER=$(PUBLICIZER) python BuildCompat.py
