from xml.dom.minidom import parse as XMLOpen
from twisted.python.filepath import FilePath
import sys
err = "-c" not in sys.argv

used = {}
mods = set()
ContentRoot = FilePath(".")
ec = 0

def process(node):
    global ec
    mod = c.getAttribute("IfModActive")
    if not mod:
        mod = "CETeam.CombatExtended"
    dp = c.firstChild.data.replace('&apos;', "'")
    if (mod, dp) in mods:
        ec += 1
        msg = f"Mod {mod} is listed in LoadFolders.xml twice, both pointing to {c.firstChild.data}"
        if err:
            raise RuntimeError(msg)
        else:
            print(msg)
    mods.add((mod, dp))
    root = FilePath("./" + dp)
    if not ContentRoot in root.parents():
        return
    for fd in root.walk():
        if fd.isdir():
            continue
        if not fd.basename().endswith(".xml"):
            continue
        relative = tuple(fd.segmentsFrom(root))
        if relative in used:
            msg = f"""Relative paths collision found: {str.join("/", fd.segmentsFrom(ContentRoot))} overrides {str.join("/", used[relative].segmentsFrom(ContentRoot))}"""
            if err:
                raise RuntimeError(msg)
            else:
                ec += 1
                print(msg)
        used[relative] = fd

with XMLOpen("LoadFolders.xml") as x:
    v1_5 = x.getElementsByTagName("v1.5")
    for n in v1_5:
        for c in n.getElementsByTagName("li"):
            process(c)


raise SystemExit(ec)
