all: Assemblies/0CombatExtendedLoader.dll Assemblies/CombatExtended.dll AssembliesCompat/MultiplayerCompat.dll AssembliesCompat/MiscTurretsCompat.dll AssembliesCompat/BetterTurretsCompat.dll



Assemblies/0CombatExtendedLoader.dll: Source/Loader/Loader.csproj $(wildcard Source/Loader/Loader/*.cs)
	python Make.py --csproj Source/Loader/Loader.csproj --output Assemblies/0CombatExtendedLoader.dll --reference /tmp/rwreference --all-libs $(DOWNLOAD_LIBS)

Assemblies/CombatExtended.dll: Assemblies/0CombatExtendedLoader.dll Source/CombatExtended/CombatExtended.csproj $(wildcard Source/CombatExtended/*/*.cs)
	python Make.py --csproj Source/CombatExtended/CombatExtended.csproj --output Assemblies/CombatExtended.dll --reference /tmp/rwreference --all-libs $(DOWNLOAD_LIBS) --publicizer /tmp/AssemblyPublicizer/ -- -r:Assemblies/0CombatExtendedLoader.dll

AssembliesCompat/MultiplayerCompat.dll: Assemblies/CombatExtended.dll Assemblies/0CombatExtendedLoader.dll Source/MultiplayerCompat/MultiplayerCompat.csproj $(wildcard Source/MultiplayerCompat/MultiplayerCompat/*.cs)
	mkdir -p AssembliesCompat
	python Make.py --csproj Source/MultiplayerCompat/MultiplayerCompat.csproj --output AssembliesCompat/MultiplayerCompat.dll --reference /tmp/rwreference --all-libs $(DOWNLOAD_LIBS) -- -r:Assemblies/0CombatExtendedLoader.dll -r:Assemblies/CombatExtended.dll

AssembliesCompat/MiscTurretsCompat.dll: Assemblies/CombatExtended.dll Assemblies/0CombatExtendedLoader.dll Source/MiscTurretsCompat/MiscTurretsCompat.csproj $(wildcard Source/MiscTurretsCompat/MiscTurretsCompat/*.cs)
	mkdir -p AssembliesCompat
	python Make.py --csproj Source/MiscTurretsCompat/MiscTurretsCompat.csproj --output AssembliesCompat/MiscTurretsCompat.dll --reference /tmp/rwreference --all-libs $(DOWNLOAD_LIBS) -- -r:Assemblies/0CombatExtendedLoader.dll -r:Assemblies/CombatExtended.dll

AssembliesCompat/BetterTurretsCompat.dll: Assemblies/CombatExtended.dll Assemblies/0CombatExtendedLoader.dll Source/BetterTurretsCompat/BetterTurretsCompat.csproj $(wildcard Source/BetterTurretsCompat/BetterTurretsCompat/*.cs)
	mkdir -p AssembliesCompat
	python Make.py --csproj Source/BetterTurretsCompat/BetterTurretsCompat.csproj --output AssembliesCompat/BetterTurretsCompat.dll --reference /tmp/rwreference --all-libs $(DOWNLOAD_LIBS) -- -r:Assemblies/0CombatExtendedLoader.dll -r:Assemblies/CombatExtended.dll
