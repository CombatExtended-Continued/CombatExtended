#!/usr/bin/python3
import sys
import os
from xml.dom.minidom import parse as XMLOpen
import subprocess

import tempfile

tdir = tempfile.gettempdir()

cwd = os.path.dirname(os.path.abspath(__file__))

usage = f"""
python Make.py [--reference <path/to/rimworld>] [--harmony <path/to/0Harmony.dll>] [-o <output/path.dll>] [--csproj <path/to/Source/CombatExtended.csproj>] [--all-libs] [--verbose] [--download-libs] [--publicize <somelibrary.dll>,...] [--publicizer <path/to/AssemblyPublicizer_Source>]

If unspecified: 
  reference defaults to $RWREFERENCE or {os.path.abspath(cwd+'/../..')}
  o defaults to Assemblies/CombatExtended.dll
  csproj defaults to Source/CombatExtended/CombatExtended.csproj

  reference can point either to the base RimWorld directory, or to a directory containing all assemblies needed to compile (including 0Harmony.dll)
    
  harmony points to the 0Harmony.dll to use.  If omitted, $reference, $reference/Mods, and $reference/../../workshop/294100/2009463077 and $PWD are searched to find 0Harmony.dll

  all-libs adds all libraries in $reference (or $reference/RimWorld*_Data/Managed) will be added to the library list
  verbose outputs the values of $reference, $csproj, and $harmony after resolving them.

  download-libs fetches reference libraries from nuget.org to $TMP/rwreference, then sets reference to $TMP/rwreference

--publicize takes a comma-separated list of dlls and publicizes them, then uses the public outputs in place of the original.
--publicizer takes the path to the AssemblyPublicizer source code.  If needed, it will compile AssemblyPublicizer
"""

if "--help" in sys.argv or "-h" in sys.argv:
    print(usage)
    raise SystemExit(1)

VERBOSE = "--verbose" in sys.argv or "-v" in sys.argv
quiet = "" if VERBOSE else "-q"

DOWNLOAD_LIBS = '--download-libs' in sys.argv

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
    
def publicize(p):
    cwd = os.getcwd()
    os.chdir(p.rsplit('/',1)[0])
    os.system(f"mono {PUBLICIZER_EXE} --exit -i {p} -o {p[:-4]}_publicized.dll")
    os.chdir(cwd)

def downloadLib(name, version, dest):
    print(f"Downloading {version} of {name} from nuget")
    if os.system(f"wget {quiet} https://www.nuget.org/api/v2/package/{name}/{version} -O {dest}"):
        raise Exception(f"Can't find version {version} of {name} on nuget")

    
def makePublicizer(p):
    cwd = os.getcwd()
    os.chdir(p)
    
    downloadLib("Mono.Options", "6.6.0.161",f"{tdir}/downloads/mono.options.zip")
    downloadLib("Mono.Cecil", "0.11.4",f"{tdir}/downloads/mono.cecil.zip")
    os.system(f"unzip {quiet} -o {tdir}/downloads/mono.cecil.zip -d .")
    os.system(f"unzip {quiet} -o {tdir}/downloads/mono.options.zip -d .")
    os.system("cp lib/net40/*dll .")
    os.system("csc -out:AssemblyPublicizer.exe ./AssemblyPublicizer/AssemblyPublicizer.cs ./AssemblyPublicizer/Properties/AssemblyInfo.cs -r:Mono.Options.dll -r:Mono.Cecil.dll")
    os.system("mono --aot AssemblyPublicizer.exe")
    os.chdir(cwd)

def findArg(s):
    if s in sys.argv:
        return sys.argv.index(s)
    return -1

rindex =  findArg("--reference")
if DOWNLOAD_LIBS:
    os.system(f'mkdir -p {tdir}/rwreference')
    RIMWORLD = f"{tdir}/rwreference"
    HARMONY = f"{tdir}/rwreference/0Harmony.dll"
else:
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

pindex = findArg("--publicize")
if pindex > -1:
    PUBLICIZE=sys.argv[pindex+1].split(',')
    pindex = findArg("--publicizer")
    if pindex > -1:
        PUBLICIZER_SOURCE = sys.argv[pindex+1]
        PUBLICIZER_EXE = os.path.join(PUBLICIZER_SOURCE,"AssemblyPublicizer.exe")
        if not os.path.exists(PUBLICIZER_EXE):
            makePublicizer(PUBLICIZER_SOURCE)
else:
    PUBLICIZE=None

    
    
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

base_dir = CSPROJ.rsplit('/',1)[0]


    
    
with XMLOpen(CSPROJ) as csproj:
    
    if DOWNLOAD_LIBS:
        os.system(f"mkdir -p {tdir}/downloads/unpack")
        for idx, package in enumerate(csproj.getElementsByTagName("PackageReference")):
            name = package.attributes['Include'].value
            version = package.attributes['Version'].value
            dest = f"{tdir}/downloads/rwref-{idx}.zip"
            downloadLib(name, version, dest)
            os.system(f"unzip {quiet} -o {tdir}/downloads/rwref-{idx}.zip -d {tdir}/downloads/unpack")
        if VERBOSE:
            print(os.listdir(tdir+'/downloads'))
        os.system(f"cp -r {tdir}/downloads/unpack/ref/net472/* {tdir}/rwreference")
        os.system(f"cp -r {tdir}/downloads/unpack/lib/net472/* {tdir}/rwreference")
    
    libraries = [HARMONY]
    removed_libraries = []
    sources = []
    if "--all-libs" in sys.argv:
        for reference in os.listdir(RIMWORLD):
            if reference.endswith(".dll"):
                libraries.append(RIMWORLD+"/"+reference)

    for reference in ['mscorlib.dll']:
        libraries.append(RIMWORLD+'/'+reference)

    for reference in csproj.getElementsByTagName("Reference"):
        hintPath = reference.getElementsByTagName("HintPath")
        if 'Remove' in reference.attributes:
            assemblyName = reference.attributes['Remove'].value.rsplit('\\',1)[1]
            removed_libraries.append(assemblyName)
            
            continue
        if hintPath:
            hintPath = hintPath[0].firstChild.data.strip()
            hintPath = hintPath.replace("\\", "/")
            hintPath = hintPath.replace("../../../../RimWorldWin64_Data/Managed", RIMWORLD)
            hintPath = base_dir+'/'+hintPath
        else:
            hintPath = RIMWORLD+"/"+reference.attributes['Include'].value + '.dll'
        if "0Harmony" in hintPath:
            continue
        if not os.path.exists(hintPath):
            l = hintPath.rsplit('/', 1)[1]
            for f in libraries:
                if f.endswith(l):
                    found = True
                    break
            else:
                print(f"Library not found: {hintPath}")

            continue
        libraries.append(hintPath)

    for d, subds, files in os.walk(base_dir):
        for f in files:
            if f.endswith('.cs'):
                p = os.path.join(d,f)
                p = p.split(base_dir+'/', 1)[1]
                sources.append(p)
                
    for source in csproj.getElementsByTagName("Compile"):
        if 'Include' in source.attributes:
            sources.append("Source/CombatExtended/"+source.attributes['Include'].value.replace("\\", "/"))
        if 'Remove' in source.attributes:
            v = source.attributes['Remove'].value.replace('\\', '/')
            if v in sources:
                sources.remove(v)
            else:
                if v.endswith('**'):
                    print("Removing wildcard:",v)
                    sl = len(sources)
                    sources = [i for i in sources if not i.startswith(v[:-2])]
                    print(f"Removed {sl-len(sources)}")
                else:
                    print("Directive to exclude non-existent file:", v)
    sources = [(base_dir+'/'+i) for i in sources]

if PUBLICIZE:
    for idx,l in enumerate(libraries):
        if l.rsplit('/',1)[1] in PUBLICIZE:
            libraries[idx] = l[:-4]+"_publicized.dll"
            publicize(l)

print(removed_libraries)
#libraries = [l for l in libraries if not l.rsplit('/',1)[1] in removed_libraries]
    
args = ["csc", "-unsafe", "-warnaserror", "-nostdlib", "-target:library", f'-out:{OUTPUT}', *sources, *[f'-r:{r}' for r in libraries]]

if VERBOSE:
    print(HARMONY)
    print(RIMWORLD)
    print(CSPROJ)

os.execvp(args[0], args)
