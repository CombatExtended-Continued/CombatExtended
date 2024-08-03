from xml.dom.minidom import parse as XMLOpen
from twisted.python.filepath import FilePath
import sys
err = "-c" not in sys.argv

used = {}
mods = set()
ModPatches = FilePath("ModPatches")
ec = 0

def process(node):
    global ec
    mod = c.getAttribute("IfModActive")
    dp = c.firstChild.data.replace('&apos;', "'")
    if (mod, dp) in mods:
        ec += 1
        msg = f"Mod {mod} is listed in LoadFolders.xml twice, both pointing to {c.firstChild.data}"
        if err:
            raise RuntimeError(msg)
        else:
            print(msg)
    mods.add((mod, dp))
    root = FilePath(dp)
    if not ModPatches in root.parents():
        return
    for fd in root.walk():
        if fd.isdir():
            continue
        if not fd.basename().endswith(".xml"):
            continue
        relative = tuple(fd.segmentsFrom(root))
        if relative in used:
            msg = f"Mod {mod} is trying to use {relative}, but {used[relative]} already owns that path"
            if err:
                raise RuntimeError(msg)
            else:
                ec += 1
                print(msg)
        used[relative] = mod

with XMLOpen("LoadFolders.xml") as x:
    v1_5 = x.getElementsByTagName("v1.5")
    for n in v1_5:
        for c in n.getElementsByTagName("li"):
            if c.getAttribute("IfModActive"):
                process(c)


raise SystemExit(ec)
