#!/usr/bin/python3
import sys
import os
from xml.dom.minidom import parse as XMLOpen
import subprocess

cwd = os.path.dirname(os.path.abspath(__file__))

usage = f"""
python Make.py [--reference <path/to/rimworld>] [--harmony <path/to/0Harmony.dll>] [-o <output/path.dll>] [--csproj <path/to/Source/CombatExtended.csproj>]

If unspecified: 
  reference defaults to $RWREFERENCE or {os.path.abspath(cwd+'/../..')}
  o defaults to Assemblies/CombatExtended.dll
  csproj defaults to Source/CombatExtended/CombatExtended.csproj

  reference can point either to the base RimWorld directory, or to a directory containing all assemblies needed to compile (including 0Harmony.dll)
    
  harmony points to the 0Harmony.dll to use.  If omitted, $reference, $reference/Mods, and $reference/../../workshop/294100/2009463077 and $PWD are searched to find 0Harmony.dll
"""

if "--help" in sys.argv or "-h" in sys.argv:
    print(usage)
    raise SystemExit(1)

VERBOSE = "--verbose" in sys.argv or "-v" in sys.argv

def findHarmony(ref):
    if "0Harmony.dll" in os.listdir(ref):
        return os.path.abspath(ref+"/0Harmony.dll")
    if "Mods" in os.listdir(ref):
        mref = ref + "/Mods"
        if "Harmony" in os.listdir(mref):
            return os.path.abspath(mref + "/Harmony/v1.1/Assemblies/0Harmony.dll")
    p = os.path.abspath(ref+"/../../../../")
    if "workshop" in os.listdir(p):
        p += '/workshop'
        if 'content' in os.listdir(p):
            p += "/content"
            if '294100' in os.listdir(p):
                p += '/294100'
                if '2009463077' in os.listdir(p):
                    return os.path.abspath(p + '/2009463077/v1.1/Assemblies/0Harmony.dll')
    return os.path.abspath(cwd+"/Assemblies/0Harmony.dll")
                
def findRimworld(ref):
    ref = os.path.abspath(ref)
    if ref.endswith("Managed"):
        return ref
    if "System.dll" in os.listdir(ref):
        return ref
    dirs = [d for d in os.listdir(ref) if d.startswith("RimWorld") and d.endswith("_Data")]
    if dirs:
        return ref + f"/{dirs[0]}/Managed"
    
        

def findArg(s):
    if s in sys.argv:
        return sys.argv.index(s)
    return -1

rindex =  findArg("--reference")
if rindex > -1:
    RIMWORLD = sys.argv[rindex+1]
else:
    RIMWORLD = os.environ.get("RWREFERENCE", cwd+'/../..')
RIMWORLD = findRimworld(RIMWORLD)
        
hindex = findArg("--harmony")
if hindex > -1:
    HARMONY = sys.argv[hindex + 1]
else:
    HARMONY = findHarmony(RIMWORLD)


    
oindex = findArg("-o")

if oindex > -1:
    OUTPUT = sys.argv[oindex+1]
else:
    OUTPUT=cwd + "/Assemblies/CombatExtended.dll"

cindex = findArg("--csproj")

if cindex > -1:
    CSPROJ = sys.argv[cindex + 1]
else:
    CSPROJ = "Source/CombatExtended/CombatExtended.csproj"

with XMLOpen(CSPROJ) as csproj:
    libraries = [HARMONY]
    sources = []
    if "--all-libs" in sys.argv:
        for reference in os.listdir(RIMWORLD):
            if reference.endswith(".dll"):
                libraries.append(RIMWORLD+"/"+reference)
        
    else:
        for reference in ['mscorlib.dll']:
            libraries.append(RIMWORLD+'/'+reference)
        
        for reference in csproj.getElementsByTagName("Reference"):
            hintPath = reference.getElementsByTagName("HintPath")
            if hintPath:
                hintPath = hintPath[0].firstChild.data.strip()
                hintPath = hintPath.replace("\\", "/")
                hintPath = hintPath.replace("../../../../RimWorldWin64_Data/Managed", RIMWORLD)
            else:
                hintPath = RIMWORLD+"/"+reference.attributes['Include'].value + '.dll'
            if "0Harmony" in hintPath:
                continue
            if not os.path.exists(hintPath):
                print(f"Library not found: {hintPath}")
                continue
            libraries.append(hintPath)
        
    for source in csproj.getElementsByTagName("Compile"):
        sources.append("Source/CombatExtended/"+source.attributes['Include'].value.replace("\\", "/"))

args = ["mcs", "-nostdlib", "-langversion:Experimental", "-target:library", f'-out:{OUTPUT}', *sources, *[f'-r:{r}' for r in libraries]]

if VERBOSE:
    print(HARMONY)
    print(RIMWORLD)
    print(CSPROJ)

t = subprocess.Popen(args)



t.wait()
raise SystemExit(t.poll())
