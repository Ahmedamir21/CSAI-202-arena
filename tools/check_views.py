#!/usr/bin/env python3
"""A small check for the Razor views, because this machine has no .NET compiler.

It reads the C# sources and the .cshtml files and reports the mistakes that a
compiler would catch:

  1. a view whose @model type does not exist;
  2. "Model.Something" where the type has no such property;
  3. the loop variable of "@foreach (var x in Model.Items)" used with a property
     the element type does not have;
  4. an "asp-controller"/"asp-action" pair that no controller defines;
  5. a partial that does not exist;
  6. a controller action that returns View() but has no .cshtml.

Run it from anywhere:  python3 tools/check_views.py
Exit status is 0 when nothing was found.
"""
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
WEB = os.path.join(ROOT, "src", "SwiftShip.Web")
VIEWS = os.path.join(WEB, "Views")

problems = []

# the things every collection answers to, so a foreach variable is not flagged for using them
COLLECTION_MEMBERS = {"Count", "FirstOrDefault", "First", "Last", "Select", "Where", "Any",
                      "OrderBy", "OrderByDescending", "Skip", "Take", "Sum", "ToList",
                      "Average", "Max", "Min", "Contains", "Add", "DefaultIfEmpty"}

# types we never look inside
PRIMITIVES = {"string", "int", "bool", "decimal", "double", "long", "float", "DateTime",
              "byte[]", "Guid", "object", "dynamic", "TimeSpan"}


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


# ---------------------------------------------------------------- the C# types
CLASSES = {}       # name -> {member: type}


def load_types():
    """Every 'public class X' anywhere in the project, with its public members.

    A class ends at the first '}' written at the same indentation as the class
    declaration, so a property that spans several lines does not end it early.
    """
    declare = re.compile(r"^(\s*)(?:public|internal)\s+(?:static\s+|sealed\s+|abstract\s+)*class\s+(\w+)")
    auto = re.compile(r"public\s+([\w<>?\[\],\s\.]+?)\s+(\w+)\s*\{\s*get;")
    computed = re.compile(r"public\s+([\w<>?\[\],\s\.]+?)\s+(\w+)\s*(?:\{\s*get\s*\{.*)?$")
    field = re.compile(r"public\s+([\w<>?\[\],\s\.]+?)\s+(\w+)\s*=")

    for path in all_files(WEB, ".cs"):
        lines = read(path).splitlines()
        members = None
        indent = None
        for line in lines:
            match = declare.match(line)
            if match and (indent is None or len(match.group(1)) > len(indent)):
                indent = match.group(1)
                members = CLASSES.setdefault(match.group(2), {})
                continue
            if members is None:
                continue
            if line == indent + "}":
                members = None
                indent = None
                continue

            stripped = line.strip()
            match = auto.search(stripped)
            if match:
                members[match.group(2)] = match.group(1).strip()
                continue
            match = field.search(stripped)
            if match:
                members[match.group(2)] = match.group(1).strip()
                continue
            match = computed.match(stripped)
            if match:
                members[match.group(2)] = match.group(1).strip()


def properties_of(type_name):
    return CLASSES.get(type_name, {})


def element_type(type_text):
    """List<Foo> -> Foo,  Foo[] -> Foo,  Foo -> None."""
    match = re.search(r"List<\s*(?:[\w]+\.)*([\w]+)\s*>", type_text)
    if match:
        return match.group(1)
    match = re.match(r"(?:[\w]+\.)*([\w]+)\[\]", type_text)
    if match:
        return match.group(1)
    return None


def short_name(type_text):
    """SwiftShip.Web.Models.TimelineRow -> TimelineRow, and unwraps generics."""
    text = type_text.strip()
    inner = element_type(text)
    if inner:
        return inner
    return text.rsplit(".", 1)[-1]


# ---------------------------------------------------------------- the controllers
CONTROLLERS = {}   # Name -> {action: body_text}


def load_controllers():
    for path in all_files(os.path.join(WEB, "Controllers"), ".cs"):
        text = read(path)
        name = os.path.basename(path).replace("Controller.cs", "")
        actions = {}
        CONTROLLERS[name] = actions

        starts = [m for m in re.finditer(
            r"\n        (?:\[[^\]]*\]\n        )*public\s+(?:async\s+)?"
            r"(?:IActionResult|ActionResult|FileResult|JsonResult)\s+(\w+)\s*\(", text)]
        for index, match in enumerate(starts):
            end = starts[index + 1].start() if index + 1 < len(starts) else len(text)
            actions[match.group(1)] = text[match.end():end]


# ---------------------------------------------------------------- the views
VIEW_PATHS = {}


def load_views():
    for path in all_files(VIEWS, ".cshtml"):
        relative = os.path.relpath(path, VIEWS).replace("\\", "/")
        VIEW_PATHS[relative] = path


def check_model_properties(path, text):
    relative = os.path.relpath(path, ROOT)
    match = re.search(r"@model\s+([\w\.<>\[\]]+)", text)
    if not match:
        return
    type_name = short_name(match.group(1))
    if type_name in PRIMITIVES:
        return

    props = properties_of(type_name)
    if not props:
        problems.append("%s: the type '%s' is not declared in the project" % (relative, type_name))
        return

    for m in re.finditer(r"\bModel\.(\w+)(?:\.(\w+))?", text):
        first, second = m.group(1), m.group(2)
        if first not in props and first not in COLLECTION_MEMBERS:
            problems.append("%s: '%s' has no member '%s' (Model.%s)" % (relative, type_name, first, first))
            continue
        if second:
            inner = properties_of(short_name(props[first]))
            if inner and second not in inner and second not in COLLECTION_MEMBERS:
                problems.append("%s: '%s.%s' has no member '%s'" % (relative, type_name, first, second))

    for m in re.finditer(r"foreach\s*\(\s*var\s+(\w+)\s+in\s+(Model\.\w+[\.\w]*)", text):
        variable, expression = m.group(1), m.group(2)
        parts = expression.split(".")
        if len(parts) < 2 or parts[1] not in props:
            continue
        current = props[parts[1]]
        for part in parts[2:]:
            current_name = short_name(current)
            if part in COLLECTION_MEMBERS:
                continue
            inner = properties_of(current_name)
            if not inner or part not in inner:
                break
            current = inner[part]
        item_type = short_name(current)
        inner = properties_of(item_type)
        if not inner:
            continue
        block = text[m.end():m.end() + 8000]
        for use in re.finditer(r"\b%s\.(\w+)" % re.escape(variable), block):
            if use.end() == len(block):
                continue      # a name cut in half by the end of the window
            if use.group(1) not in inner and use.group(1) not in COLLECTION_MEMBERS:
                problems.append("%s: '%s' (from %s) has no member '%s'"
                                % (relative, item_type, expression, use.group(1)))


def check_links(path, text):
    relative = os.path.relpath(path, ROOT)
    for m in re.finditer(r"<(?:a|form)\s+[^>]*asp-controller=\"(\w+)\"[^>]*asp-action=\"(\w+)\"", text):
        controller, action = m.group(1), m.group(2)
        if controller not in CONTROLLERS:
            problems.append("%s: there is no %sController" % (relative, controller))
        elif action not in CONTROLLERS[controller]:
            problems.append("%s: %sController has no action '%s'" % (relative, controller, action))

    for m in re.finditer(r"PartialAsync\(\"([^\"]+)\"", text):
        partial = m.group(1)
        if not partial.endswith(".cshtml"):
            partial += ".cshtml"
        if ("Shared/" + partial) not in VIEW_PATHS and partial not in VIEW_PATHS:
            problems.append("%s: the partial '%s' does not exist" % (relative, partial))


def check_actions_have_views():
    for name, actions in sorted(CONTROLLERS.items()):
        for action, body in sorted(actions.items()):
            if "return View" not in body:
                continue
            if os.path.exists(os.path.join(VIEWS, name, action + ".cshtml")):
                continue
            if os.path.exists(os.path.join(VIEWS, "Shared", action + ".cshtml")):
                continue
            problems.append("%sController.%s returns a View() but Views/%s/%s.cshtml is missing"
                            % (name, action, name, action))


def main():
    load_types()
    load_controllers()
    load_views()

    for _relative, path in sorted(VIEW_PATHS.items()):
        text = read(path)
        check_model_properties(path, text)
        check_links(path, text)

    check_actions_have_views()

    if not problems:
        print("The views look consistent: %d views, %d controllers, %d types."
              % (len(VIEW_PATHS), len(CONTROLLERS), len(CLASSES)))
        return 0

    print("%d thing(s) to look at:" % len(problems))
    for problem in problems:
        print("  - " + problem)
    return 1


if __name__ == "__main__":
    sys.exit(main())
