#!/usr/bin/python3
import os
import sys
import argparse
from xml.dom.minidom import parse as XMLOpen
import tempfile
import time
from dataclasses import dataclass

tdir = tempfile.gettempdir()

args = ["csc", "-unsafe", "-warnaserror", "-nostdlib", "-target:library"]


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
    argParser = argparse.ArgumentParser()
    argParser.add_argument("--reference", type=str)
    argParser.add_argument("--output", "-o", type=str, default="Assemblies/CombatExtended.dll")
    argParser.add_argument("--csproj", type=str, default="Source/CombatExtended/CombatExtended.csproj")
    argParser.add_argument("--all-libs", action="store_true", default=False)
    argParser.add_argument("--verbose", "-v", action="count", default=0)
    argParser.add_argument("--download-libs", action="store_true", default=False)
    argParser.add_argument("--publicizer", type=str)

    options = argParser.parse_args(argv[1:])
    if not options.download_libs and options.reference is None:
        print("You must either set reference to where the rimworld reference assemblies are (including Harmony), or set the download-libs option")
        exit(1)
    if options.csproj is None or not os.path.exists(options.csproj):
        print("You must specify the path to the .csproj file you wish to build")
        exit(1)

    if options.reference is None:
        options.reference = f"{tdir}/rwreference"

    return options


def downloadLib(name, version, dest, verbose):
    if verbose:
        print(f"Downloading {version} of {name} from nuget")
    quiet = "-q" if verbose < 5 else ""
    if os.system(f"wget {quiet} https://www.nuget.org/api/v2/package/{name}/{version} -O {dest}"):
        raise Exception(f"Can't find version {version} of {name} on nuget")

def unpackLib(path, dest, verbose):
    cwd = os.getcwd()
    quiet = "-q" if verbose < 6 else ""
    os.system(f"unzip {quiet} -o {path} -d {dest}")
    

def resolveLibrary(library, reference, csproj, verbose):
    if library.hintPath:
        if verbose > 1:
            print("Adding library via hintPath: {hintPath}")
        base_dir = os.path.split(csproj)[0]
        t_path = os.path.join(base_dir, library.hintPath)
        if os.path.isabs(library.hintPath):
            return os.path.abspath(t_path)
        else:
            return os.path.relpath(t_path)
    if verbose > 1:
        print("Searching for library named {library.name}.dll")
    return os.path.join(reference, library.name+'.dll')
    
def publicize(p, publicizer, verbose):
    if verbose:
        print(f"Publicizing Assembly: {p}")
    cwd = os.getcwd()
    os.chdir(p.rsplit('/',1)[0])
    os.system(f"mono {publicizer} --exit -i {p} -o {p[:-4]}_publicized.dll")
    os.chdir(cwd)
    return p[:-4]+'_publicized.dll'

def makePublicizer(p, verbose):
    ap_path = os.path.join(p, "AssemblyPublicizer.exe")
    
    if os.path.exists(ap_path):
        if verbose:
            print("Reusing AssemblyPublicizer")
    else:
        if verbose:
            print("Making AssemblyPublicizer")
        quiet = "-q" if verbose < 5 else ""
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
    return ap_path


def parse_csproj(csproj_path, verbose):
    packages = []
    sources = []
    libraries = []
    removed_libraries = []
    base_dir = os.path.split(csproj_path)[0]
    publicized_libraries = []
    if verbose:
        print("Parsing .csproj: {csproj_path}")
    
    with XMLOpen(csproj_path) as csproj:
        for idx, package in enumerate(csproj.getElementsByTagName("PackageReference")):
            name = package.attributes['Include'].value
            version = package.attributes['Version'].value
            dest = f"{tdir}/downloads/rwref-{idx}.zip"
            packages.append(PackageReference(name, version, dest))

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
        sources = [(base_dir+'/'+i) for i in sources]

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

    return packages, libraries, removed_libraries, publicized_libraries, sources

def main(argv=sys.argv):
    
    options = parseArgs(argv)
    verbose = options.verbose

    os.system(f"mkdir -p {tdir}/downloads/unpack")
    os.system(f"mkdir -p {options.reference}")
    
    
    packages, libraries, removed_libraries, publicized_libraries, sources = parse_csproj(options.csproj, options.verbose)
    if publicized_libraries:
        if not options.publicizer:
            print("This .csproj requires publicizing assemblies, but you have not provided a path to AssemblyPublicizer")
            exit(1)

        publicizer = makePublicizer(options.publicizer, verbose)
              

    if options.download_libs:
        for package in packages:
            downloadLib(package.name, package.version, package.destination, verbose=verbose)
            unpackLib(package.destination, f"{tdir}/downloads/unpack", verbose=verbose)
            time.sleep(1)
        os.system(f"cp -r {tdir}/downloads/unpack/ref/net472/* {options.reference}")
        os.system(f"cp -r {tdir}/downloads/unpack/lib/net472/* {options.reference}")

    print(libraries)

    libraries = [resolveLibrary(l, options.reference, options.csproj, verbose) for l in libraries if '$' not in l.name]

    if options.all_libs:
        for ref in os.listdir(options.reference):
            if ref.endswith(".dll"):
                libraries.append(os.path.join(options.reference, ref))

    for p in publicized_libraries:
        for l in libraries:
            if os.path.basename(l) == p.inputName:
                libraries.append(publicize(l, publicizer, verbose))
                break
                

    libraries = [l for l in set(libraries) if not os.path.basename(l) in removed_libraries]
    args.extend([f'-out:{options.output}', *sources, *[f'-r:{r}' for r in libraries]])
    os.execvp(args[0], args)
    
if __name__=='__main__':
    main(sys.argv)
