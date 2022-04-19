all: Assemblies/CombatExtendedLoader.dll Assemblies/CombatExtended.dll AssembliesCompat/MultiplayerCompat.dll AssembliesCompat/MiscTurretsCompat.dll



Assemblies/CombatExtendedLoader.dll: Source/Loader/Loader.csproj $(wildcard Source/Loader/Loader/*.cs)
	python Make.py --csproj Source/Loader/Loader.csproj --output Assemblies/CombatExtendedLoader.dll --reference /tmp/rwreference --all-libs $(DOWNLOAD_LIBS)

Assemblies/CombatExtended.dll: Assemblies/CombatExtendedLoader.dll Source/CombatExtended/CombatExtended.csproj $(wildcard Source/CombatExtended/*/*.cs)
	python Make.py --csproj Source/CombatExtended/CombatExtended.csproj --output AssembliesCore/CombatExtended.dll --reference /tmp/rwreference --all-libs $(DOWNLOAD_LIBS) --publicizer /tmp/AssemblyPublicizer/

AssembliesCompat/MultiplayerCompat.dll: Assemblies/CombatExtended.dll Assemblies/CombatExtendedLoader.dll Source/MultiplayerCompat/MultiplayerCompat.csproj $(wildcard Source/MultiplayerCompat/MultiplayerCompat/*.cs)
	mkdir -p AssembliesCompat
	python Make.py --csproj Source/MultiplayerCompat/MultiplayerCompat.csproj --output AssembliesCompat/MultiplayerCompat.dll --reference /tmp/rwreference --all-libs $(DOWNLOAD_LIBS)

AssembliesCompat/MiscTurretsCompat.dll: Assemblies/CombatExtended.dll Assemblies/CombatExtendedLoader.dll Source/MiscTurretsCompat/MiscTurretsCompat.csproj $(wildcard Source/MiscTurretsCompat/MiscTurretsCompat/*.cs)
	mkdir -p AssembliesCompat
	python Make.py --csproj Source/MiscTurretsCompat/MiscTurretsCompat.csproj --output AssembliesCompat/MiscTurretsCompat.dll --reference /tmp/rwreference --all-libs $(DOWNLOAD_LIBS)
