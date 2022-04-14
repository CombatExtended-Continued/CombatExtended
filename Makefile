all: Assemblies/CombatExtendedLoader.dll Assemblies/CombatExtended.dll AssembliesCompat/MultiplayerCompat.dll



Assemblies/CombatExtendedLoader.dll: Source/Loader/Loader.csproj $(wildcard Source/Loader/Loader/*.cs)
	python Make.py --csproj Source/Loader/Loader.csproj --output Assemblies/CombatExtendedLoader.dll --reference /tmp/rwreference --all-libs --download-libs

Assemblies/CombatExtended.dll: Assemblies/CombatExtendedLoader.dll Source/CombatExtended/CombatExtended.csproj $(wildcard Source/CombatExtended/*/*.cs)
	python Make.py --csproj Source/CombatExtended/CombatExtended.csproj --output Assemblies/CombatExtended.dll --reference /tmp/rwreference --all-libs --download-libs --publicizer /tmp/AssemblyPublicizer/

AssembliesCompat/MultiplayerCompat.dll: Assemblies/CombatExtended.dll Assemblies/CombatExtendedLoader.dll Source/MultiplayerCompat/MultiplayerCompat.csproj $(wildcard Source/MultiplayerCompat/MultiplayerCompat/*.cs)
	mkdir -p AssembliesCompat
	python Make.py --csproj Source/MultiplayerCompat/MultiplayerCompat.csproj --output AssembliesCompat/MultiplayerCompat.dll --reference /tmp/rwreference --all-libs --download-libs
