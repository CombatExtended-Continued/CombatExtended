# AoC 1.7 compatibility assembly

This project builds `AOCCompat.dll` into `ModPatches/AOC The Cleanup Devil 1.7/Assemblies`.
`BuildCompat.py` discovers it through `Source/CombatExtended.sln`; no compiled compatibility DLL is committed.
The assembly is only loaded when `Millap.AOCNew` selects the corresponding load folder.

The five `AOC*-reference.dll` files in `Source/packages` retain the AoC assembly identities and metadata required for compilation. Method implementations and resources have been removed, and each assembly is marked with `ReferenceAssemblyAttribute`. They must not be copied to the game output. The project sets `Private=False` on every external reference. `AOC-References.json` records the source mod and hashes.

The original mod and its implementations are by Millap: <https://steamcommunity.com/sharedfiles/filedetails/?id=3820707817>.
Character Editor is optional and is accessed through reflection, so it is not a compile-time dependency.

Build using the repository's normal .NET 10 toolchain:

```text
dotnet build Source/AOCCompat/AOCCompat.csproj -c Release
```

An installed AoC 1.7 assembly can replace its corresponding reference DLL when updating the API metadata; keep `Private=False` and do not distribute original mod implementations with CE.
