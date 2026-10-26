#!/usr/bin/python3
import os
import sys
import argparse
from xml.dom.minidom import parse as XMLOpen
import tempfile
import time
from dataclasses import dataclass
import subprocess

tdir = tempfile.gettempdir()

SDK_PATH = '/usr/share/dotnet/sdk'

csc_candidates = sorted(os.listdir(SDK_PATH),
                        reverse=True,
                        key=lambda v: tuple(map(int, v.split('.'))))


if csc_candidates:
    CSC = os.path.join(SDK_PATH, csc_candidates[0], "Roslyn/bincore/csc")
else:
    import shutil
    CSC = shutil.which("csc")
    if not csc:
        CSC = shutil.which("mono-csc")

args = [CSC, "-unsafe", "-warnaserror", "-nowarn:CS1701", "-target:library", "-nostdlib"]


def system(*cmd):
    sp = subprocess.Popen(cmd)
    sp.wait()
    return sp.poll()


@dataclass
class PackageReference(object):
    name: str
    version: str
    destination: str


@dataclass
class LibraryReference(object):
    name: str
    hintPath: str


@dataclass
class PublicizeTarget(object):
    inputName: str
    outputName: str


def parseArgs(argv):
    argParser = argparse.ArgumentParser(description="Automate compiling C# projects on Linux, using Mono")
    argParser.add_argument("--reference", type=str, action="append", metavar="PATH", help="Location of Rimworld Reference libraries")
    argParser.add_argument("--sdk-reference", type=str, metavar="PATH", help="Location to search for latest SDK reference")
    argParser.add_argument("--output", "-o", type=str, default="Assemblies/CombatExtended.dll", help="Output filename")
    argParser.add_argument("--csproj", type=str, default="Source/CombatExtended/CombatExtended.csproj", help="C# project to build.")
    argParser.add_argument("--all-libs", action="store_true", default=False, help="Reference all dlls in $reference, even those not specified by the .csproj file")
    argParser.add_argument("--verbose", "-v", action="count", default=0, help="Increase verbosity, specify more times for more verbosity")
    argParser.add_argument("--download-libs", action="store_true", default=False, help=f"Automatically download referenced packages to {tdir}/downloads and unpack them to $reference")
    argParser.add_argument("--publicizer", type=str, metavar="PATH", help="AoT compiled AssemblyPublicizer executable")
    argParser.add_argument("--debug", action="store_true", default=False, help="Define `DEBUG` when calling csc")
    argParser.add_argument("--refout", metavar="PATH", default=None, help="Specify where to save a reference library")
    argParser.add_argument("--csc", metavar="CSC", default=None, help="Specify alternative csc executable")
    argParser.add_argument("--build-props", metavar="PROPS", default=None, help="Specify common build properties file")

    options = argParser.parse_args(argv[1:])
    if not options.download_libs and options.reference is None:
        print("You must either set reference to where the rimworld reference assemblies are (including Harmony), or set the download-libs option")
        exit(1)
    if options.csproj is None or not os.path.exists(options.csproj):
        print("You must specify the path to the .csproj file you wish to build")
        exit(1)

    if options.reference is None:
        options.reference = [f"{tdir}/rwreference"]


    auto_sdk = bool(options.sdk_reference)
    if options.sdk_reference is None:
        options.sdk_reference = "/usr/share/dotnet/packs/Microsoft.NETCore.App.Ref"

    sdk_candidates = sorted(os.listdir(options.sdk_reference),
                            reverse=True,
                            key=lambda v: tuple(map(int, v.split('.'))))
    if sdk_candidates:
        if options.verbose:
            print("using sdk version", sdk_candidates[0])
        options.reference.append(f"{options.sdk_reference}/{sdk_candidates[0]}/ref/net10.0/")
    elif auto_sdk:
        print(f"automatic sdk selection requested, but {options.sdk_reference} contains no candidates")
        exit(1)

    if options.csc is not None:
        global CSC
        args[0] = options.csc
        CSC = args[0]
        
    return options


def downloadLib(name, version, dest, verbose):
    if verbose:
        print(f"Downloading {version} of {name} from nuget")
    quiet = ["-q"] if verbose < 5 else []
    args = ["wget", *quiet, f"https://www.nuget.org/api/v2/package/{name}/{version}", "-O", dest]
    if system(*args):
        raise Exception(f"Can't find version {version} of {name} on nuget")


def unpackLib(path, dest, verbose):
    cwd = os.getcwd()
    quiet = ["-q"] if verbose < 6 else []
    system("unzip", *quiet, "-o", path, "-d", dest)
    

def resolveLibrary(library, reference, csproj, verbose):
    if library.hintPath:
        if verbose > 1:
            print(f"Adding library via hintPath: {library.hintPath}")
        base_dir = os.path.split(csproj)[0]
        t_path = os.path.join(base_dir, library.hintPath)
        if os.path.isabs(library.hintPath):
            return os.path.abspath(t_path)
        else:
            return os.path.relpath(t_path)
    if verbose > 1:
        print(f"Searching for library named {library.name}.dll")
    for r in reference:
        p = os.path.join(reference, library.name + '.dll')
        if os.path.exists(p):
            return p
    raise RuntimeError(f"Required library {librar.name}.dll not found")
    

def publicize(p, publicizer, verbose, reference):
    if verbose:
        print(f"Publicizing Assembly: {p}")
    cwd = os.getcwd()
    os.chdir(p.rsplit('/',1)[0])
    args = [publicizer, "--exit", "-i", p, "-o", f"{p[:-4]}_publicized.dll"]
    for r in reference:
        args.append('-p')
        args.append(r)
    if system(*args):
        raise RuntimeError("Failed to publicize assembly")
    os.chdir(cwd)
    return p[:-4]+'_publicized.dll'


def parse_packages(csproj):
    packages = []
    for idx, package in enumerate(csproj.getElementsByTagName("PackageReference")):
        name = package.attributes['Include'].value
        version = package.attributes['Version'].value
        dest = f"{tdir}/downloads/rwref-{idx}.zip"
        packages.append(PackageReference(name, version, dest))
    return packages


def parse_libraries(csproj):
    libraries = []
    removed_libraries = []
    for reference in csproj.getElementsByTagName("Reference"):
        hintPath = reference.getElementsByTagName("HintPath")
        if 'Remove' in reference.attributes:
            assemblyName = reference.attributes['Remove'].value.rsplit('\\',1)[1]
            removed_libraries.append(assemblyName)

            continue
        if hintPath:
            hintPath = hintPath[0].firstChild.data.strip()
            hintPath = hintPath.replace("\\", "/")
        libraries.append(LibraryReference(reference.attributes['Include'].value, hintPath))
    return libraries, removed_libraries


def parse_sources(csproj, base_dir, verbose):
    sources = []
    for d, subds, files in os.walk(base_dir):
        for f in files:
            if f.endswith('.cs'):
                p = os.path.join(d,f)
                p = p.split(base_dir+'/', 1)[1]
                sources.append(p)

    for source in csproj.getElementsByTagName("Compile"):
        if 'Include' in source.attributes:
            sources.append(source.attributes['Include'].value.replace("\\", "/"))
        if 'Remove' in source.attributes:
            v = source.attributes['Remove'].value.replace('\\', '/')
            if v in sources:
                sources.remove(v)
            else:
                if v.endswith('**'):
                    if verbose:
                        print("Removing wildcard:",v)
                    sl = len(sources)
                    sources = [i for i in sources if not i.startswith(v[:-2])]
                    if verbose:
                        print(f"Removed {sl-len(sources)}")
                else:
                    print("Directive to exclude non-existent file:", v)
    return [os.path.join(base_dir, i) for i in sources]


def parse_publicize(csproj):
    publicized_libraries = []
    removed_libraries = []
    publicize_task = [i for i in csproj.getElementsByTagName("Target")
                          if 'Name' in i.attributes and i.attributes['Name'].value.startswith('Publi')]
    variables = {}

    for pt in publicize_task:

        for pg in pt.getElementsByTagName("PropertyGroup"):
            for var in pg.childNodes:
                if var.nodeName != '#text':
                    val = var.firstChild.data.strip()
                    if '\\' in val:
                        variables[var.nodeName] = val.rsplit('\\', 1)[1]
                    elif ')' in val:
                        variables[var.nodeName] = val.rsplit(')', 1)[1]
                    else:
                        variables[var.nodeName] = val
        for pub in pt.getElementsByTagName("Publicise"):
            target = pub.attributes['TargetAssemblyPath'].value

            if '$' in target:
                target = target.replace('$', '%')
                target = target.replace(')', ')s')
                target = target % variables

            output = target.rsplit('.',1)[0] + '_publicized.dll'

            publicized_libraries.append(PublicizeTarget(target, output))
    if not publicized_libraries:
        publicize_task = [i.attributes['Include'].value for i in csproj.getElementsByTagName("Publicize")
                          if 'Include' in i.attributes]
        for pub in publicize_task:
            publicized_libraries.append(PublicizeTarget(pub+'.dll', pub+'_publicized.dll'))
            removed_libraries.append(pub+'.dll')
    
    return publicized_libraries, removed_libraries


def parse_csproj(csproj_path, verbose):
    
    base_dir = os.path.split(csproj_path)[0]
    
    if verbose:
        print(f"Parsing .csproj: {csproj_path}")
    
    with XMLOpen(csproj_path) as csproj:
        packages = parse_packages(csproj)

        libraries, removed_libraries = parse_libraries(csproj)

        sources = parse_sources(csproj, base_dir, verbose)

        publicized_libraries, removed_libraries_2 = parse_publicize(csproj)
        removed_libraries.extend(removed_libraries_2)
    
    return packages, libraries, removed_libraries, publicized_libraries, sources


def main(argv=sys.argv):
    rest = []
    if '--' in argv:
        _idx = argv.index('--')
        argv, rest = argv[:_idx], argv[_idx+1:]
    options = parseArgs(argv)
    verbose = options.verbose

    system(f"mkdir", "-p", f"{tdir}/downloads/unpack")
    system(f"mkdir", "-p", options.reference[0])
        
    packages, libraries, removed_libraries, publicized_libraries, sources = parse_csproj(options.csproj, options.verbose)

    if options.build_props:
        if verbose:
            print(f"Parsing common build properties: {options.build_props}")
        with XMLOpen(options.build_props) as bprops:
            shared_packages = parse_packages(bprops)
            packages.extend(shared_packages)
    
    if publicized_libraries:
        if not options.publicizer:
            print("This .csproj requires publicizing assemblies, but you have not provided a path to AssemblyPublicizer")
            exit(1)

        publicizer = options.publicizer
              
    if options.download_libs:
        for package in packages:
            downloadLib(package.name, package.version, package.destination, verbose=verbose)
            unpackLib(package.destination, f"{tdir}/downloads/unpack", verbose=verbose)
            time.sleep(1)
        system(f"chmod", "-R", "+r", f"{tdir}/downloads/unpack")
        os.system(f"cp -r {tdir}/downloads/unpack/ref/net10.0/* {options.reference[0]}")
        os.system(f"cp -r {tdir}/downloads/unpack/lib/net10.0/* {options.reference[0]}")

    libraries = [resolveLibrary(l, options.reference, options.csproj, verbose) for l in libraries if '$' not in l.name]

    if options.all_libs:
        for search_path in options.reference:
            for ref in os.listdir(search_path):
                if ref.endswith(".dll") and not ref.endswith("_publicized.dll"):
                    libraries.append(os.path.join(search_path, ref))

    for p in publicized_libraries:
        for l in libraries:
            if os.path.basename(l) == p.inputName:
                libraries.append(publicize(l, publicizer, verbose, options.reference))
                break
                
    libraries = [l for l in set(libraries) if not os.path.basename(l) in removed_libraries]
    args.extend([f'-out:{options.output}', *sources, *[f'-r:{r}' for r in libraries]])
    args.extend(rest)
    if options.refout:
        args.extend([f"-refout:{options.refout}"])
    if options.debug:
        args.append('-define:DEBUG')
        args.append('-debug:embedded')
    if verbose > 2:
        print(libraries)
    if verbose > 6:
        print(args)
    os.execvp(args[0], args)
    
if __name__=='__main__':
    main(sys.argv)
