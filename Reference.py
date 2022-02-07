#!/usr/bin/python3

from twisted.python.filepath import FilePath
import sys
import re

class Counter(object):
    val = 0
    def incr(self):
        self.val += 1
        return self.val
    def decr(self):
        self.val -= 1
        return self.val
    def __int__(self):
        return self.val

indent = Counter()

TAB=8

METHOD = re.compile(r'(private )?(public )?(static )?(override )? ?([a-zA-Z0-9_<>]+) ([a-zA-Z0-9_]+)(\(.*?\))$')

def findClose(source, out=None):
    count = 0
    dquote = False
    squote = False
    if out is None:
        out = []
    
    for idx, c in enumerate(source):
        out.append(c)
        if c == '"':
            if squote:
                continue
            dquote = not dquote
            continue
        if c == "'":
            if dquote:
                continue
            squote = not squote
            continue
        if dquote or squote:
            continue
        if c == '{':
            count += 1
            continue
        if c == '}':
            count -= 1
            if count == 0:
                return idx
            
def findClasses(source, out):
    lines = iter(source.split('\n'))
    for line in lines:
        if ' class ' in line:
            c = Class(line, lines)
            out.append(c)
            yield c
        else:
            out.append(line)

def wrappedIter(lines):
    for line in lines:
        yield from line
        yield '\n'

def findMethods(source, out):
    lines = iter(source)
    for line in lines:
        if isinstance(line, Class):
            out.append(line)
            continue
        if match := METHOD.match(line.strip()):
            m = Method(match, line, lines)
            out.append(m)
            yield m
        else:
            out.append(line)

class Method(object):
    def __init__(self, match, first, lines):
        self.level = indent.incr()
        self.name = match.group(6)
        self.header = first
        o = []
        findClose(wrappedIter(lines), o)
        self.body = str.join('', o).strip()[1:-1]
        indent.decr()
        
    def __repr__(self):
        return f"Method(name={self.name})"
    def __str__(self):

        body = self.body
        
        return f"{self.header}\n{' '*(TAB*(self.level-1))}{{\n{body}}}"
    def makeReference(self):
        self.body = f"{' '*TAB*(self.level)}throw null;\n{' '*TAB*(self.level-1)}"
        
        
            
class Class(object):
    def __init__(self, first, lines):
        self.level = indent.incr()
        self.name = first.split('class ', 1)[1].split(' ', 1)[0].split('\n', 1)[0]
        self.header = first
        o = []
        findClose(wrappedIter(lines), o)
        body = str.join('', o).strip()[1:-1]
        self.body = []
        self.inner = list(findClasses(body, self.body))
        body = self.body
        self.body = []
        self.methods = list(findMethods(body, self.body))
        indent.decr()
        
        
    def __repr__(self):
        return f"Class(name={self.name}, methods={self.methods}, internal={self.inner})"
    def __str__(self):

        body = str.join('\n', (str(i) for i in self.body))
        
        return f"{self.header}\n{' '*(TAB*(self.level-1))}{{{body}}}"
    def makeReference(self):
        for c in self.inner:
            c.makeReference()
        for m in self.methods:
            m.makeReference()
        b = iter(self.body)
        self.body = []
        for line in b:
            if not isinstance(line, str):
                self.body.append(line)
                continue
            if line.strip().startswith("private"):
                if not line.strip().endswith(';'):
                    c = []
                    findClose(wrappedIter(b), c)
                continue
            self.body.append(line)
            
class Namespace(object):
    def __init__(self, source):
        self.level = indent.incr()
        self.header, source = source.split('namespace', 1)
        self.namespace, source = source.strip().split('\n', 1)
        idx = findClose(source)
        body = source[1:idx]
        self.body = []
        self.classes = list(findClasses(body, self.body))
        indent.decr()
        
    def __repr__(self):
        return f"Namespace(name={self.namespace}, classes={self.classes})"

    def __str__(self):
        body = str.join("\n", (str(i) for i in self.body))
        return f"{self.header}\nnamespace {self.namespace}\n{{{body}\n}}"

for f in FilePath(sys.argv[1]).walk():
    if f.isdir():
        continue
    if not f.path.endswith('.cs'):
        continue
    source = f.getContent().decode('utf-8')
    if 'namespace' in source:
        namespace = Namespace(source)
        print(namespace)
        for c in namespace.classes:
            c.makeReference()
        print(str(namespace))
        f.setContent(str(namespace).encode('utf-8'))
    
