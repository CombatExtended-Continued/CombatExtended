#!/usr/bin/python3
import os
import re
import sys
import asyncio
from pathlib import Path

tm = '-m' in sys.argv
parallel = '-j' in sys.argv

PROJECT_PATTERN = re.compile(r'''Project.".[0-9A-Za-z]{8}-[0-9A-Za-z]{4}-[0-9A-Za-z]{4}-[0-9A-Za-z]{4}-[0-9A-Za-z]{12}.". = '''
                             r'''"([0-9a-zA-Z]+Compat)", '''
                             r'''"([0-9A-zA-Z\/]+.csproj)", '''
                             r'''".[0-9A-Za-z]{8}-[0-9A-Za-z]{4}-[0-9A-Za-z]{4}-[0-9A-Za-z]{4}-[0-9A-Za-z]{12}."''')

PUBLICIZER = os.environ.get("PUBLICIZER", "./AssemblyPublicizer")
DOWNLOAD_LIBS = os.environ.get("DOWNLOAD_LIBS", "--download-libs")

async def run_command(*cmd):
    proc = await asyncio.create_subprocess_exec(*cmd)
    await proc.wait()
    if proc.returncode != 0:
        raise SystemExit(proc.returncode)

async def main():
    tasks = []

    async def process_line(line):
        match = PROJECT_PATTERN.match(line)
        if match:
            name, csproj = match.groups()
            if tm and name not in sys.argv:
                return
            csproj_path = Path("Source").joinpath(*csproj.split('/')).as_posix()
            print(f"Building {name}")
            output_path = Path("AssembliesCompat").joinpath(f"{name}.dll").as_posix()
            cmd = [
                "python3", "Make.py", "--csproj", csproj_path, "--output", output_path,
                DOWNLOAD_LIBS, "--all-libs", "--publicizer", PUBLICIZER, "--",
                "-r:Assemblies/CombatExtended.dll"
            ]
            if parallel:
                tasks.append(run_command(*cmd))
            else:
                await run_command(*cmd)

    with open("Source/CombatExtended.sln") as f:
        for line in f:
            await process_line(line.strip())

    if parallel:
        await asyncio.gather(*tasks)

if __name__ == "__main__":
    asyncio.run(main())
