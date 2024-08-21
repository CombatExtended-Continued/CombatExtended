PUBLICIZER := /tmp/AssemblyPublicizer


all: Assemblies/CombatExtended.dll CompatAssemblies AssembliesCompat $(PUBLICIZER)

$(PUBLICIZER):
	git clone https://github.com/CombatExtended-Continued/AssemblyPublicizer --depth=1 $(PUBLICIZER)

.PHONY: all CompatAssemblies


AssembliesCompat: $(PUBLICIZER)
	mkdir -p AssembliesCompat

Assemblies/CombatExtendedLoader.dll: Source/Loader/Loader.csproj $(wildcard Source/Loader/Loader/*.cs) $(PUBLICIZER)
	python3 Make.py --csproj Source/Loader/Loader.csproj --output Assemblies/CombatExtendedLoader.dll --reference /tmp/rwreference --all-libs $(DOWNLOAD_LIBS)

Assemblies/CombatExtended.dll: Source/CombatExtended/CombatExtended.csproj $(wildcard Source/CombatExtended/*/*.cs) $(wildcard Source/CombatExtended/*/*/*.cs) $(PUBLICIZER)
	python3 Make.py --csproj Source/CombatExtended/CombatExtended.csproj --output Assemblies/CombatExtended.dll --reference /tmp/rwreference --all-libs $(DOWNLOAD_LIBS) --publicizer $(PUBLICIZER)


CompatAssemblies:
	DOWNLOAD_LIBS="--reference=/tmp/rwreference" PUBLICIZER=$(PUBLICIZER) python3 BuildCompat.py -j
