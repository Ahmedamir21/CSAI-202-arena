#!/usr/bin/env python3
"""A small check for the C# sources, because this machine has no .NET compiler.

It cannot type-check the code, but it catches the mistake that hurts most in a
project this size: calling a method that the other class does not have. It reads
every .cs file, remembers each class with its methods and fields, and then looks
at every "_service.Method(...)" call and asks whether that method exists.

Things it knows about and stays quiet on:
  - anything from the framework (db., reader., Mapper., session., TempData, ...);
  - calls on a local variable, because it does not track their types;
  - the number of arguments, because C# has optional parameters and overloads.

Run it from anywhere:  python3 tools/check_csharp.py
Exit status is 0 when nothing was found.
"""
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
WEB = os.path.join(ROOT, "src", "SwiftShip.Web")

problems = []

# receivers that are not one of our classes, so their methods are not ours to check
FOREIGN = {
    "db", "reader", "cmd", "connection", "parameters", "p", "r", "sb", "list", "items",
    "session", "httpcontext", "tempdata", "viewbag", "viewdata", "model", "result",
    "this", "base", "string", "console", "convert", "decimal", "int", "datetime",
    "math", "guid", "encoding", "file", "path", "task", "tuple", "array", "enumerable",
    "json", "text", "csv", "hash", "bytes", "value", "s", "x", "e", "ex",
}

CLASSES = {}      # name -> {"methods": {name: [arity, ...]}, "fields": {name: type}}


def read(path):
    with open(path, "r", encoding="utf-8", errors="replace") as handle:
        return handle.read()


def all_files(folder, extension):
    found = []
    for base, _dirs, names in os.walk(folder):
        for name in names:
            if name.endswith(extension):
                found.append(os.path.join(base, name))
    return found


def logical_lines(lines):
    """Joins the lines of a signature that was wrapped on to several lines.

    A method like "public Result Book(int a,\n int b)" is one declaration, but it
    is written on two lines, so we keep reading until the brackets balance.
    """
    buffer = ""
    for line in lines:
        buffer = (buffer + " " + line) if buffer else line
        if buffer.count("(") > buffer.count(")"):
            continue
        yield buffer
        buffer = ""
    if buffer:
        yield buffer


def load_classes():
    declare = re.compile(r"^(\s*)(?:public|internal)\s+(?:static\s+|sealed\s+|abstract\s+)*class\s+(\w+)")
    method = re.compile(r"^\s+(?:public|private|protected|internal)\s+"
                        r"(?:static\s+|virtual\s+|override\s+|async\s+)*"
                        r"[\w<>\[\],\s\.]+\s+(\w+)\s*\(([^)]*)\)")
    field = re.compile(r"^\s+(?:private|protected|public)\s+(?:readonly\s+)?([\w<>\[\],\s\.]+?)\s+(_\w+)\s*;")

    for path in all_files(WEB, ".cs"):
        lines = read(path).splitlines()
        info = None
        indent = None
        for line in logical_lines(lines):
            match = declare.match(line)
            if match and (indent is None or len(match.group(1)) > len(indent)):
                indent = match.group(1)
                name = match.group(2)
                info = CLASSES.setdefault(name, {"methods": {}, "fields": {}})
                continue
            if info is None:
                continue
            if line.strip() == "}" and line.startswith(indent) and len(line) == len(indent) + 1:
                info = None
                indent = None
                continue

            match = field.match(line)
            if match:
                info["fields"][match.group(2)] = match.group(1).strip().rsplit(".", 1)[-1]
                continue
            match = method.match(line)
            if match and not line.strip().startswith("//"):
                parameters = split_top_level(match.group(2))
                info["methods"].setdefault(match.group(1), []).append(len(parameters))


def split_top_level(text):
    """Splits "a, B(c, d), e" into ["a", "B(c, d)", "e"]."""
    parts, depth, current = [], 0, ""
    for char in text:
        if char in "([{":
            depth += 1
        elif char in ")]}":
            depth -= 1
        if char == "," and depth == 0:
            parts.append(current)
            current = ""
        else:
            current += char
    if current.strip():
        parts.append(current)
    return [p.strip() for p in parts if p.strip()]


def check_calls():
    call = re.compile(r"\b(_[A-Za-z]\w*)\.(\w+)\s*\(")

    for path in all_files(WEB, ".cs"):
        lines = read(path).splitlines()
        relative = os.path.relpath(path, ROOT)
        owner = None
        fields = {}

        for number, line in enumerate(lines, start=1):
            match = re.match(r"^\s*(?:public|internal)\s+(?:static\s+|sealed\s+|abstract\s+)*class\s+(\w+)", line)
            if match:
                owner = match.group(1)
                fields = CLASSES.get(owner, {}).get("fields", {})
                continue

            code = line.split("//")[0]
            for use in call.finditer(code):
                receiver, name = use.group(1), use.group(2)
                if receiver.lower() in FOREIGN:
                    continue
                receiver_type = fields.get(receiver)
                if not receiver_type:
                    continue      # not one of our injected services
                if receiver_type.startswith("List<") or receiver_type.startswith("I"):
                    continue
                known = CLASSES.get(receiver_type)
                if known is None:
                    if receiver_type.endswith("[]") or receiver_type.startswith("List<"):
                        continue
                    problems.append("%s:%d: no class '%s' for %s.%s()"
                                    % (relative, number, receiver_type, receiver, name))
                    continue
                if name in known["methods"]:
                    continue
                # a property returning an object we then call a method on
                problems.append("%s:%d: '%s' has no method '%s' (called as %s.%s)"
                                % (relative, number, receiver_type, name, receiver, name))


def main():
    load_classes()
    check_calls()

    if not problems:
        print("Every service call resolves: %d classes read." % len(CLASSES))
        return 0

    print("%d thing(s) to look at:" % len(problems))
    for problem in problems:
        print("  - " + problem)
    return 1


if __name__ == "__main__":
    sys.exit(main())
