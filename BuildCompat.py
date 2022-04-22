#!/usr/bin/python3
import os
from twisted.python.filepath import FilePath
from subprocess import Popen
import re

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

with open("Source/CombatExtended.sln") as f:
    for line in f.readlines():
        line = line.strip()
        match = PROJECT_PATTERN.match(line)
        if match:
            name, csproj = match.groups()
            csproj = csproj.replace('\\', '/').split('/')
            csproj = FilePath("Source").descendant(csproj)
            print(f"Building {name}")
            output = FilePath("AssembliesCompat").child(name+".dll")
            system("python3", "Make.py", "--csproj", csproj.path, "--output", output.path, "--download-libs", "--all-libs", "--publicizer", "./AssemblyPublicizer", "--", "-r:Assemblies/CombatExtended.dll")

