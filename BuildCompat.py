#!/usr/bin/python3
import os
from twisted.python.filepath import FilePath
from subprocess import Popen
import re
import sys
import argparse
from xml.dom.minidom import parse as XMLOpen

tdir = os.environ.get('TEMP', '/tmp')

PROJECT_PATTERN = re.compile(r'''Project.".[0-9A-Za-z]{8}-[0-9A-Za-z]{4}-[0-9A-Za-z]{4}-[0-9A-Za-z]{4}-[0-9A-Za-z]{12}.". = '''
                             r'''"([0-9a-zA-Z]+Compat)", '''
                             r'''"([0-9A-zA-Z\/]+.csproj)", '''
                             r'''".[0-9A-Za-z]{8}-[0-9A-Za-z]{4}-[0-9A-Za-z]{4}-[0-9A-Za-z]{4}-[0-9A-Za-z]{12}."''')

def system(*cmd):
  sp = Popen(cmd)
  sp.wait()
  ec = sp.poll()
  if ec:
    raise SystemExit(ec)


def parseArgs(argv):
  argParser = argparse.ArgumentParser(description="Automate compiling C# projects on Linux, using Mono")
  argParser.add_argument("--reference", type=str, action="append", metavar="PATH", help="Location of Rimworld Reference libraries")
  argParser.add_argument("--sdk-reference", type=str, metavar="PATH", help="Location to search for latest SDK reference")
  argParser.add_argument("--all-libs", action="store_true", default=False, help="Reference all dlls in $reference, even those not specified by the .csproj file")
  argParser.add_argument("--verbose", "-v", action="count", default=0, help="Increase verbosity, specify more times for more verbosity")
  argParser.add_argument("--download-libs", action="store_true", default=False, help=f"Automatically download referenced packages to {tdir}/downloads and unpack them to $reference")
  argParser.add_argument("--solution", type=str, default="Source/CombatExtended.sln", help="C# solution to build.")
  argParser.add_argument("--publicizer", type=str, metavar="PATH", help="AoT compiled AssemblyPublicizer executable")
  argParser.add_argument("--debug", action="store_true", default=False, help="Define `DEBUG` when calling csc")
  argParser.add_argument("--csc", metavar="CSC", default=None, help="Specify alternative csc executable")
  argParser.add_argument("--build-props", metavar="PROPS", default=None, help="Specify common build properties file")
  argParser.add_argument("--subproject", metavar="PROJ", default=None, type=str, action="append", help="Build specific subproject (default builds all)")
  argParser.add_argument("--list-projects", default=False, action="store_true", help="List the compat projects found in the .sln and exit")
  options = argParser.parse_args(argv[1:])
  if not options.download_libs and options.reference is None and options.list_projects:
    print("You must either set reference to where the rimworld reference assemblies are (including Harmony), or set the download-libs option")
    exit(1)
  if options.solution is None or not os.path.exists(options.solution):
    print("You must specify the path to the .sln file you wish to build")
    exit(1)

  if options.reference is None:
    options.reference = [f"{tdir}/rwreference"]

  return options

def parseSln(path, subprojects):
  with open(path) as f:
    for line in f.readlines():
        line = line.strip()
        match = PROJECT_PATTERN.match(line)
        if match:
            name, csproj = match.groups()
            if subprojects and name not in subprojects:
              continue
            yield name, csproj.replace('\\', '/').split('/')

def parse_packages(csproj):
    packages = []
    for idx, package in enumerate(csproj.getElementsByTagName("PackageReference")):
        name = package.attributes['Include'].value
        version = package.attributes['Version'].value
        dest = f"{tdir}/downloads/rwref-{idx}.zip"
        packages.append((name, version, dest))
    return packages


def parseCSProj(sln, name, path, options):
  csproj = FilePath(sln).parent().descendant(path)
  output = FilePath("AssembliesCompat").child(name+".dll")
  project_refs = []
  with XMLOpen(csproj.path) as cpath:
    packages = parse_packages(cpath)
    for ref in cpath.getElementsByTagName("ProjectReference"):
      if 'Include' in ref.attributes:
        project_refs.append(os.path.normpath(os.path.join(
          os.path.dirname(csproj.path),
          ref.attributes['Include'].value.replace('\\', '/'))))

      op = cpath.getElementsByTagName("OutputPath")
      if op:
        op = op[0].firstChild.data
        if 'ModPatches' in op:
          op = op.rsplit("..\\ModPatches", 1)[-1].replace('\\', '/').split('/')
          if op:
            od = FilePath("ModPatches").descendant(op)
            if not od.exists():
              od.makedirs()
            output = od.child(name+".dll")
  return csproj, output, project_refs, packages


def buildCSProj(name, csproj, output, project_refs, built_outputs, config, rest):
  print(f"Building {name}")
  extra_refs = [f"-r:{built_outputs[ref]}" for ref in project_refs if ref in built_outputs]
  args = []
  for arg in ("debug",
              "all-libs",
              "csc",
              "build-props",
              "publicizer",
              "sdk-reference",
              "verbose",
              "reference"):
    val = getattr(config, arg.replace('-', '_'))
    if val:
      if isinstance(val, str):
        args.append(f"--{arg}")
        args.append(val)
      if isinstance(val, bool):
        args.append(f"--{arg}")
        continue
      if isinstance(val, list):
        for c in val:
          args.append(f"--{arg}")
          args.append(c)
      if isinstance(val, int):
        args.append("-" + ('v' * val))

  if system("python3", "Make.py", "--csproj", csproj.path, "--output", output.path, *args, "--", "-r:Assemblies/CombatExtended.dll", *extra_refs, *rest):
    raise RuntimeError("Failed building " + name)
  built_outputs[os.path.normpath(csproj.path)] = output.path

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

            
def main(argv=sys.argv):
    rest = []
    if '--' in argv:
        _idx = argv.index('--')
        argv, rest = argv[:_idx], argv[_idx+1:]

    options = parseArgs(argv)
  
    built_outputs = {}
    projects = parseSln(options.solution, options.subproject)
    packages = set()
    to_build = []

    for project, csproj in projects:
      if options.list_projects:
        print(project)
        continue
      
      csproj, output, project_refs, pkgs = parseCSProj(options.solution, project, csproj, options)
      for pkg in pkgs:
        packages.add(pkg)
      to_build.append((project, csproj, output, project_refs, built_outputs, options, rest))

    if options.download_libs:
      for name, version, dest in packages:
        downloadLib(name, version, dest, options.verbose)
        unpackLib(dest, f"{tdir}/downloads/unpack", verbose=options.verbose)
      system(f"chmod", "-R", "+r", f"{tdir}/downloads/unpack")
      os.system(f"cp -r {tdir}/downloads/unpack/ref/net10.0/* {options.reference[0]}")
      os.system(f"cp -r {tdir}/downloads/unpack/lib/net10.0/* {options.reference[0]}")


    for (project, csproj, output, project_refs, built_outputs, options, rest) in to_build:
      buildCSProj(project, csproj, output, project_refs, built_outputs, options, rest)
                                                 

if __name__ == '__main__':
  raise SystemExit(main())
