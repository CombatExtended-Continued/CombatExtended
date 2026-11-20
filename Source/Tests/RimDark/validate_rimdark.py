"""Apply the RimDark XML operations to the downloaded defs without launching RimWorld.

Checks XPath matches, available XML parents, duplicate fields and selected
behavioral invariants. Does not instantiate C# classes, load missing base-game
parents, apply other mods' patches, or test gameplay.
"""
from copy import deepcopy
from collections import Counter
from pathlib import Path
import argparse
import sys
from lxml import etree as ET

REPO = Path(__file__).resolve().parents[3]
WORKSPACE = REPO.parent
REFS = WORKSPACE / 'References'
PARSER = ET.XMLParser(remove_comments=True, remove_blank_text=True)

MODULES = {
    'base': ('RimDark40KMankindsFinest', '1.7', 'RimDark 40k - Mankinds Finest'),
    'armory': ('RimDark40KMankindsFinestArmory', '1.6', 'RimDark 40k - Mankinds Finest Armory'),
    'templars': ('RimDark40KBlackTemplars', '1.6', 'RimDark 40k - Black Templars'),
}

def merge_fields(parent, fields):
    for field in fields:
        for old in parent.findall(field.tag):
            parent.remove(old)
        parent.append(deepcopy(field))

def child(parent, name):
    found = parent.find(name)
    return found if found is not None else ET.SubElement(parent, name)

def apply_operation(tree, op):
    kind = op.get('Class')
    xpath = (op.findtext('xpath') or '').strip()
    if kind == 'PatchOperationSequence':
        for sub in op.find('operations'):
            apply_operation(tree, sub)
        return
    if kind == 'PatchOperationConditional':
        branch = op.find('match' if tree.xpath('/' + xpath) else 'nomatch')
        if branch is not None:
            apply_operation(tree, branch)
        return
    if kind == 'CombatExtended.PatchOperationMakeGunCECompatible':
        name = op.findtext('defName')
        nodes = tree.xpath('/Defs/ThingDef[defName=$name]', name=name)
        assert len(nodes) == 1, f'gun target {name}: {len(nodes)} matches'
        node = nodes[0]
        stats = op.find('statBases')
        if stats is not None:
            dest = child(node, 'statBases')
            for old in dest.xpath('AccuracyTouch|AccuracyShort|AccuracyMedium|AccuracyLong'):
                dest.remove(old)
            merge_fields(dest, stats)
        for field, cls in [('AmmoUser', 'CompProperties_AmmoUser'), ('FireModes', 'CompProperties_FireModes')]:
            value = op.find(field)
            if value is not None:
                comp = ET.SubElement(child(node, 'comps'), 'li', Class=f'CombatExtended.{cls}')
                comp.extend(deepcopy(list(value)))
        for field in ['weaponTags', 'weaponClasses']:
            value = op.find(field)
            if value is not None:
                child(node, field).extend(deepcopy(list(value)))
        assert op.find('Properties') is None, 'Extend validator before using gun Properties'
        return
    nodes = tree.xpath('/' + xpath)
    assert nodes, f'{kind}: no match for {xpath}'
    if kind == 'PatchOperationRemove':
        for node in nodes:
            node.getparent().remove(node)
    elif kind == 'PatchOperationReplace':
        for node in nodes:
            parent, index = node.getparent(), node.getparent().index(node)
            for value in reversed(list(op.find('value'))):
                parent.insert(index, deepcopy(value))
            parent.remove(node)
    elif kind == 'PatchOperationAdd':
        for node in nodes:
            node.extend(deepcopy(list(op.find('value'))))
    elif kind == 'PatchOperationAttributeSet':
        for node in nodes:
            node.set(op.findtext('attribute'), op.findtext('value'))
    else:
        raise AssertionError(f'Unsupported operation: {kind}')

def load_defs(folders):
    root = ET.Element('Defs')
    for folder in folders:
        if not folder.exists():
            continue
        for file in sorted(folder.rglob('*.xml')):
            document = ET.parse(str(file), PARSER)
            if document.getroot().tag == 'Defs':
                root.extend(deepcopy(list(document.getroot())))
    return ET.ElementTree(root)

def rimdark(definition):
    return (definition.findtext('defName') or definition.get('Name') or '').startswith('BEWH_')

def identity(definition):
    return definition.tag, definition.findtext('defName') or definition.get('Name')

def duplicate_fields(tree):
    issues = set()
    for definition in tree.getroot():
        if not rimdark(definition):
            continue
        # Top-level fields must be unique, as must stat keys inside a stat list.
        containers = [definition] + [node for node in definition.iter()
            if isinstance(node.tag, str) and ('stat' in node.tag.lower())
            and (node.tag.endswith('Offsets') or node.tag.endswith('Factors') or node.tag in ['statBases','statFactorsPerLevel','statOffsetsPerLevel'])]
        for container in containers:
            for tag, count in Counter(field.tag for field in container).items():
                if count > 1 and tag != 'li':
                    issues.add(f'{identity(definition)}/{tree.getpath(container).split("/")[-1]}: duplicate {tag}')
    return issues

def resolved_definitions(tree):
    """Resolve only available XML parents; absent base-game parents are unknown.

    This is enough to examine custom weapon tools, ammo and armor. It is not
    a replacement for RimWorld's XmlInheritance or Def loading.
    """
    named = {node.get('Name'): node for node in tree.getroot() if node.get('Name')}
    cache = {}
    def merge(base, own):
        if own.get('Inherit', '').lower() == 'false':
            return deepcopy(own)
        result = deepcopy(base)
        result.attrib.update(own.attrib)
        if not len(own):
            result.text = own.text
            return result
        for field in own:
            match = result.find(field.tag) if field.tag != 'li' else None
            if match is None:
                result.append(deepcopy(field))
            else:
                index = result.index(match)
                result.remove(match)
                result.insert(index, merge(match, field))
        return result
    def resolve(node, seen=()):
        key = node
        if key in cache:
            return cache[key]
        parent = node.get('ParentName')
        assert parent not in seen, f'XML inheritance cycle: {seen} -> {parent}'
        result = merge(resolve(named[parent], (*seen, parent)), node) if parent in named else deepcopy(node)
        cache[key] = result
        return result
    return [resolve(node) for node in tree.getroot()]

def check_load_folders():
    document = ET.parse(str(REPO/'LoadFolders.xml'), PARSER)
    failures = []
    paths, entries = set(), set()
    relative_files = {}
    for entry in document.findall('.//li'):
        relative = (entry.text or '').strip()
        path = REPO/relative
        package = entry.get('IfModActive', '').lower()
        if not path.is_dir():
            failures.append(f'LoadFolders: missing {relative}')
        if relative in paths:
            failures.append(f'LoadFolders: duplicate directory {relative}')
        paths.add(relative)
        if (package, relative) in entries:
            failures.append(f'LoadFolders: duplicate entry {package}: {relative}')
        entries.add((package, relative))
        for subdir in ['Defs','Patches']:
            for file in (path/subdir).rglob('*.xml'):
                key = file.relative_to(path).as_posix().lower()
                if key in relative_files:
                    failures.append(f'LoadFolders: duplicate relative XML path {key}')
                relative_files[key] = file
    for module in MODULES.values():
        if 'ModPatches/'+module[2] not in paths:
            failures.append(f'LoadFolders: unregistered module {module[2]}')
    print(f'LoadFolders: {len(paths)} folders, {len(failures)} failures')
    for failure in failures:
        print('  '+failure)
    return not failures

def check_behavior(tree, before, enabled):
    failures = []
    definitions = resolved_definitions(tree)
    things = {node.findtext('defName'):node for node in definitions if node.tag == 'ThingDef' and node.findtext('defName')}
    ammo_sets = {node.findtext('defName'):node for node in definitions if node.tag == 'CombatExtended.AmmoSetDef'}
    weapons = []
    for node in definitions:
        name = node.findtext('defName')
        if node.tag != 'ThingDef' or not name or not name.startswith('BEWH_'):
            continue
        verbs = node.findall('verbs/li')
        for verb in verbs:
            projectile = verb.findtext('defaultProjectile')
            if not projectile:
                continue
            if verb.get('Class') != 'CombatExtended.VerbPropertiesCE':
                failures.append(f'{name}: unconverted ranged verb {verb.findtext("verbClass")}')
                continue
            if projectile not in things:
                failures.append(f'{name}: missing projectile {projectile}')
            elif things[projectile].find('projectile').get('Class') != 'CombatExtended.ProjectilePropertiesCE':
                failures.append(f'{name}: non-CE projectile properties {projectile}')
            ammo = node.find('comps/li[@Class="CombatExtended.CompProperties_AmmoUser"]')
            if node.find('race') is None:
                weapons.append(name)
                if ammo is None:
                    failures.append(f'{name}: no CE ammo component')
                elif ammo.findtext('ammoSet') not in ammo_sets:
                    failures.append(f'{name}: missing ammo set {ammo.findtext("ammoSet")}')
                elif projectile not in [field.text for field in ammo_sets[ammo.findtext('ammoSet')].find('ammoTypes')]:
                    failures.append(f'{name}: default projectile does not belong to its ammo set')
        for tool in node.findall('tools/li'):
            if tool.get('Class') != 'CombatExtended.ToolCE':
                # The pawn race also uses CE tools; abstract vanilla parents
                # unavailable here are deliberately not assumed to be patched.
                failures.append(f'{name}: non-CE melee tool {tool.findtext("label")}')
        if name.startswith('BEWH_Astartes') and 'Plasma' not in name:
            if node.find('comps/li[@Class="Core40k.CompProperties_AmmoChanger"]') is not None:
                failures.append(f'{name}: native ammo changer conflicts with CE ammunition')
    # A source of silent regressions in the original PR: whole-node stat
    # replacement deleted unrelated implant/chapter modifiers.
    old_defs = {identity(node):node for node in before.getroot() if rimdark(node)}
    new_defs = {identity(node):node for node in tree.getroot() if rimdark(node)}
    for key, old in old_defs.items():
        if old.tag not in ['GeneDef','HediffDef','Core40k.ExtraDecorationDef','Genes40k.ChapterTraitDef','Genes40k.ChapterRankDef']:
            continue
        new = new_defs[key]
        for group in old.iter():
            if group.tag not in ['statOffsets','statFactors','pawnStatOffsets','equippedStatOffsets']:
                continue
            relative = before.getpath(group)[len(before.getpath(old))+1:]
            changed = new.find(relative)
            for field in group:
                if field.tag == 'CarryingCapacity' or field.tag.startswith('ShootingAccuracyFactor_'):
                    continue
                if changed is None or changed.find(field.tag) is None:
                    failures.append(f'{key}/{relative}: lost unrelated stat {field.tag}')
    pod = things['BEWH_SteelRainDropPodBuilding']
    if pod.findtext('thingClass') != 'Genes40k.Building_DropDrop':
        failures.append('Steel Rain: native deployment class was replaced')
    for name in ['BEWH_SteelRainDropPodBuilding','BEWH_RaisedTurret']:
        if things[name].find('comps/li[@Class="CombatExtended.CompProperties_RimDarkSummonedTurret"]') is None:
            failures.append(f'{name}: no initial ammunition component')
    for name in ['BEWH_Ability_ThrowFragGrenadePair','BEWH_Ability_ThrowKrakGrenadePair']:
        node = next(node for node in definitions if node.findtext('defName') == name)
        if node.findtext('abilityClass') != 'Core40k.Ability_ShootProjectileWithCharges':
            failures.append(f'{name}: native charge logic was replaced')
        if node.find('modExtensions/li[@Class="CombatExtended.HarmonyCE.Compatibility.RimDarkGrenadeExtension"]') is None:
            failures.append(f'{name}: no CE projectile bridge marker')
    if 'armory' in enabled:
        for name in ['BEWH_AstartesPlasmaPistol','BEWH_AstartesPlasmaIncinerator']:
            if things[name].find('comps/li[@Class="Core40k.CompProperties_AmmoChanger"]') is None:
                failures.append(f'{name}: lost native plasma mode selector')
        for name in ['BEWH_Projectile_PlasmaCharged','BEWH_Projectile_PlasmaCharged_Heavy']:
            if things[name].find('modExtensions/li[@Class="CombatExtended.RimDarkPlasmaModeExtension"]') is None:
                failures.append(f'{name}: no CE plasma mode marker')
    return failures, len(set(weapons))

def run(enabled):
    folders = [REPO/'Defs', REFS/'VanillaExpandedFramework/1.7/Defs', REFS/'RimDark40KFramework/1.7/Defs']
    patches = []
    for key in enabled:
        mod, version, patch = MODULES[key]
        folders.append(REFS/mod/version/'Defs')
        folders.append(REPO/'ModPatches'/patch/'Defs')
        patches.extend(sorted((REPO/'ModPatches'/patch/'Patches').rglob('*.xml')))
    tree = load_defs(folders)
    before = deepcopy(tree)
    failures = []
    count = 0
    for file in patches:
        doc = ET.parse(str(file), PARSER)
        for op in doc.getroot():
            count += 1
            try:
                apply_operation(tree, op)
            except AssertionError as error:
                failures.append(f'{file.name}:{op.sourceline}: {error}')
    failures.extend(sorted(duplicate_fields(tree)-duplicate_fields(before)))
    behavior, gun_count = check_behavior(tree, before, enabled)
    failures.extend(behavior)
    print(f'{"+".join(enabled)}: {count} operations, {gun_count} guns, {len(failures)} failures')
    for failure in failures:
        print('  ' + failure)
    return not failures

def main():
    global REFS
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--references', type=Path, default=REFS,
                        help='Directory containing the named reference mod downloads (see RimDark README).')
    args = parser.parse_args()
    REFS = args.references.resolve()
    required = [REFS/mod/version/'Defs' for mod,version,_ in MODULES.values()]
    required.extend([REFS/'RimDark40KFramework/1.7/Defs', REFS/'VanillaExpandedFramework/1.7/Defs'])
    missing = [str(path) for path in required if not path.is_dir()]
    if missing:
        parser.error('Missing reference definition folders: '+', '.join(missing))
    combinations = [('base',), ('base', 'armory'), ('base', 'templars'), ('base', 'armory', 'templars')]
    results = [check_load_folders()] + [run(enabled) for enabled in combinations]
    return 0 if all(results) else 1

if __name__ == '__main__':
    sys.exit(main())
