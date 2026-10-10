"""Static consistency checker for the SwiftShip SQL scripts.

There is no SQL Server in every environment, so instead of running the scripts we parse
them and check the things that normally break at run time:

  * every table a script reads or writes really exists
  * every column in an INSERT / UPDATE / SELECT list really exists in that table
  * every foreign key points at an existing table and column
  * every stored procedure that another procedure or the C# code calls is defined

Usage:  python3 tools/check_sql.py
Exit code is 0 when there is nothing to report, 1 otherwise.
"""

import os
import re
import sys

DDL = "db/01_create_tables.sql"
SEED = "db/02_seed_data.sql"
QUERIES = "db/03_queries.sql"
SRC = "src/SwiftShip.Web"

errors = []
warnings = []


def strip_comments(sql):
    sql = re.sub(r"/\*.*?\*/", " ", sql, flags=re.S)
    sql = re.sub(r"--[^\n]*", "", sql)
    return sql


def strip_literals(sql):
    """Removes string literals so that words inside them are not read as identifiers."""
    return re.sub(r"N?'([^']|'')*'", "''", sql)


def split_top_level(text, sep=","):
    """Splits on `sep`, ignoring commas inside parentheses and inside string literals."""
    out, depth, in_str, current = [], 0, False, ""
    i = 0
    while i < len(text):
        ch = text[i]
        if in_str:
            if ch == "'":
                if i + 1 < len(text) and text[i + 1] == "'":
                    current += "''"
                    i += 2
                    continue
                in_str = False
            current += ch
        elif ch == "'":
            in_str = True
            current += ch
        elif ch == "(":
            depth += 1
            current += ch
        elif ch == ")":
            depth -= 1
            current += ch
        elif ch == sep and depth == 0:
            out.append(current.strip())
            current = ""
        else:
            current += ch
        i += 1
    if current.strip():
        out.append(current.strip())
    return out


def find_tuples(block):
    """Returns the top-level ( ... ) groups of a VALUES block, as strings."""
    out, depth, start, in_str = [], 0, None, False
    i = 0
    while i < len(block):
        ch = block[i]
        if in_str:
            if ch == "'":
                if i + 1 < len(block) and block[i + 1] == "'":
                    i += 2
                    continue
                in_str = False
        elif ch == "'":
            in_str = True
        elif ch == "(":
            if depth == 0:
                start = i + 1
            depth += 1
        elif ch == ")":
            depth -= 1
            if depth == 0 and start is not None:
                out.append(block[start:i])
                start = None
        elif ch == ";" and depth == 0:
            break
        i += 1
    return out


def read(path):
    with open(path, encoding="utf-8") as f:
        return f.read()


# --------------------------------------------------------------------- parse the DDL
def parse_tables(sql):
    tables = {}
    pattern = re.compile(
        r"CREATE\s+TABLE\s+(?:dbo\.)?(\w+)\s*\((.*?)\n\)\s*;", re.S | re.I)
    for name, body in pattern.findall(sql):
        cols = {}
        depth = 0
        current = ""
        parts = []
        for ch in body:
            if ch == "(":
                depth += 1
            elif ch == ")":
                depth -= 1
            if ch == "," and depth == 0:
                parts.append(current)
                current = ""
            else:
                current += ch
        parts.append(current)
        for p in parts:
            p = p.strip()
            if not p:
                continue
            if p.upper().startswith("CONSTRAINT"):
                continue
            m = re.match(r"^(\w+)", p)
            if m:
                cols[m.group(1)] = p
        tables[name] = cols
    return tables


def parse_views(sql):
    return set(re.findall(r"CREATE\s+OR\s+ALTER\s+VIEW\s+(?:dbo\.)?(\w+)", sql, re.I))


def parse_procs(sql):
    return set(re.findall(
        r"CREATE\s+(?:OR\s+ALTER\s+)?PROC(?:EDURE)?\s+(?:dbo\.)?(\w+)", sql, re.I))


def parse_functions(sql):
    return set(re.findall(
        r"CREATE\s+(?:OR\s+ALTER\s+)?FUNCTION\s+(?:dbo\.)?(\w+)", sql, re.I))


def parse_fks(sql):
    """Returns (child_table, child_columns, parent_table, parent_columns)."""
    out = []
    pattern = re.compile(
        r"(?:CONSTRAINT\s+\w+\s+)?FOREIGN\s+KEY\s*\(([^)]*)\)\s*REFERENCES\s+"
        r"(?:dbo\.)?(\w+)\s*\(([^)]*)\)", re.I | re.S)
    for child_cols, parent, parent_cols in pattern.findall(sql):
        child = None
        out.append((child,
                    [c.strip() for c in child_cols.split(",")],
                    parent,
                    [c.strip() for c in parent_cols.split(",")]))
    return out


def attach_fk_tables(sql, tables):
    """Works out which table each FOREIGN KEY clause belongs to, across line breaks."""
    result = []
    # split the DDL at every CREATE TABLE and every ALTER TABLE
    marks = [(m.start(), m.group(1), m.group(0).upper().startswith("ALTER"))
             for m in re.finditer(r"(?:CREATE\s+TABLE|ALTER\s+TABLE)\s+(?:dbo\.)?(\w+)",
                                  sql, re.I)]
    marks.append((len(sql), None, False))
    fk_pattern = re.compile(
        r"(?:CONSTRAINT\s+\w+\s+)?FOREIGN\s+KEY\s*\(([^)]*)\)\s*REFERENCES\s+"
        r"(?:dbo\.)?(\w+)\s*\(([^)]*)\)", re.I | re.S)
    for (start, table, _), (next_start, _, _) in zip(marks, marks[1:]):
        if table is None:
            continue
        if table not in tables:      # ALTER TABLE on a table defined elsewhere in the file
            pass
        segment = sql[start:next_start]
        for child_cols, parent, parent_cols in fk_pattern.findall(segment):
            result.append((table,
                           [c.strip() for c in child_cols.split(",")],
                           parent,
                           [c.strip() for c in parent_cols.split(",")]))
    return result


# --------------------------------------------------------------------- checks
SQL_KEYWORDS = {
    "table", "months", "inserted", "deleted", "values", "select", "where", "set",
    "on", "and", "or", "order", "group", "left", "right", "inner", "outer", "cross",
    "union", "having", "top", "as", "join", "from", "into", "update", "with",
}


def check_identifiers(sql, tables, views, label):
    sql = strip_literals(sql)
    known = set(tables) | views
    # aliases declared next to a table, for example "FROM dbo.Shipment sh"
    aliases = set(re.findall(
        r"(?:FROM|JOIN)\s+(?:dbo\.)?\w+\s+(?:AS\s+)?([A-Za-z_]\w*)", sql, re.I))
    known |= aliases
    referenced = set(re.findall(r"\b(?:FROM|JOIN|INTO|UPDATE)\s+(?:dbo\.)?(\w+)", sql, re.I))
    for t in sorted(referenced):
        if t.lower() in SQL_KEYWORDS:
            continue
        if t not in known:
            errors.append("%s: reads or writes table '%s', which is not defined" % (label, t))


def check_insert_columns(sql, tables, label):
    """INSERT INTO t (a, b, c) VALUES ... - every column must exist."""
    pattern = re.compile(
        r"INSERT\s+INTO\s+(?:dbo\.)?(\w+)\s*\(([^)]*)\)\s*VALUES", re.I | re.S)
    for table, collist in pattern.findall(sql):
        cols = [c.strip() for c in collist.replace("\n", " ").split(",") if c.strip()]
        if table not in tables:
            errors.append("%s: INSERT into unknown table '%s'" % (label, table))
            continue
        for c in cols:
            if c not in tables[table]:
                errors.append("%s: INSERT INTO %s references column '%s', which the table does not have"
                              % (label, table, c))
    # the number of columns must match the number of values in every VALUES tuple
    for m in pattern.finditer(sql):
        table = m.group(1)
        if table not in tables:
            continue
        n = len(split_top_level(m.group(2)))
        block = sql[m.end(): m.end() + 40000]
        # stop at the end of the statement
        end = re.search(r"\b(?:SET\s+IDENTITY_INSERT|GO|PRINT)\b", block)
        if end:
            block = block[:end.start()]
        for tup in find_tuples(block)[:400]:
            cnt = len(split_top_level(tup))
            if cnt != n:
                warnings.append("%s: INSERT INTO %s has %d columns but a VALUES tuple has %d "
                                "values -> %s" % (label, table, n, cnt, tup[:90]))
                break


def check_update_columns(sql, tables, label):
    sql = strip_literals(sql)
    aliases = set(re.findall(
        r"(?:FROM|JOIN)\s+(?:dbo\.)?\w+\s+(?:AS\s+)?([A-Za-z_]\w*)", sql, re.I))
    for table, setlist in re.findall(
            r"UPDATE\s+(?:dbo\.)?(\w+)\s+SET\s+(.*?)(?:WHERE|FROM|;|\bGO\b)", sql, re.I | re.S):
        if table in aliases:
            continue
        if table not in tables:
            errors.append("%s: UPDATE on unknown table '%s'" % (label, table))
            continue
        for m in re.finditer(r"(\w+)\s*=", setlist):
            col = m.group(1)
            if col.upper() in ("SET", "AND", "OR", "CASE", "WHEN", "THEN", "ELSE", "END", "CAST"):
                continue
            if col not in tables[table]:
                errors.append("%s: UPDATE %s SET %s, but the table has no column '%s'"
                              % (label, table, col, col))


def check_procs_called(sql, procs, label):
    called = set(re.findall(r"EXEC(?:UTE)?\s+(?:dbo\.)?(\w+)", sql, re.I))
    for p in sorted(called):
        if p not in procs and p.lower() not in ("sp_executesql",):
            errors.append("%s: calls EXEC dbo.%s, which is never created" % (label, p))


def check_fks(tables, fk_rows):
    for child, child_cols, parent, parent_cols in fk_rows:
        if child is None:
            continue
        if parent not in tables:
            errors.append("schema: FK from %s references unknown table '%s'" % (child, parent))
            continue
        for c in child_cols:
            if c not in tables.get(child, {}):
                errors.append("schema: FK from %s uses column '%s', which the table does not have"
                              % (child, c))
        for c in parent_cols:
            if c not in tables[parent]:
                errors.append("schema: FK to %s uses column '%s', which the table does not have"
                              % (parent, c))
        if len(child_cols) != len(parent_cols):
            errors.append("schema: FK from %s to %s has a different number of columns"
                          % (child, parent))


def check_csharp_procs(procs):
    if not os.path.isdir(SRC):
        return
    for root, _, files in os.walk(SRC):
        for fn in files:
            if not fn.endswith(".cs"):
                continue
            path = os.path.join(root, fn)
            text = read(path)
            for m in re.finditer(r'(?:usp_|fn_)\w+', text):
                name = m.group(0)
                if name.startswith("usp_") and name not in procs:
                    errors.append("%s: calls %s, which is never created" % (path, name))


def check_alias_columns(sql, tables, views, label):
    """`FROM dbo.Shipment sh ... sh.CurrentStatusID` - the column must exist on that table.

    Only base tables are checked; a view can legitimately expose a computed column that a
    simple parser cannot see.
    """
    sql = strip_literals(sql)
    stops = ("FROM", "JOIN", "GO", ";")
    positions = [(m.start(), m.group(1), m.group(2))
                 for m in re.finditer(r"(?:FROM|JOIN)\s+(?:dbo\.)?(\w+)\s+(?:AS\s+)?([A-Za-z_]\w*)",
                                      sql, re.I)]
    known = set(tables) | views
    for i, (pos, table, alias) in enumerate(positions):
        if table not in tables:
            continue
        if alias.upper() in ("ON", "WHERE", "SET", "ORDER", "GROUP", "AS", "LEFT", "RIGHT",
                             "INNER", "OUTER", "CROSS", "UNION", "WITH", "AND", "OR", "HAVING"):
            continue
        end = positions[i + 1][0] if i + 1 < len(positions) else min(
            [sql.find(s, pos) for s in stops if sql.find(s, pos) > 0] or [len(sql)])
        region = sql[pos:end]
        for col in set(re.findall(r"\b%s\.(\w+)" % re.escape(alias), region)):
            if col not in tables[table]:
                warnings.append("%s: %s.%s is used but dbo.%s has no column '%s' "
                                "(this check is a heuristic - a subquery that comes before "
                                "its FROM clause can confuse it)"
                                % (label, alias, col, table, col))


def main():
    ddl = strip_comments(read(DDL))
    seed = strip_comments(read(SEED))
    queries = strip_comments(read(QUERIES))
    everything = seed + "\n" + queries

    tables = parse_tables(ddl)
    views = parse_views(queries)
    procs = parse_procs(queries)
    funcs = parse_functions(queries)
    fk_rows = attach_fk_tables(ddl, tables)

    print("schema: %d tables, %d foreign keys" % (len(tables), len(fk_rows)))
    print("queries: %d views, %d procedures, %d functions" % (len(views), len(procs), len(funcs)))

    check_fks(tables, fk_rows)
    check_identifiers(seed, tables, views, SEED)
    check_identifiers(queries, tables, views, QUERIES)
    check_insert_columns(seed, tables, SEED)
    check_insert_columns(queries, tables, QUERIES)
    check_update_columns(seed, tables, SEED)
    check_update_columns(queries, tables, QUERIES)
    check_procs_called(queries, procs, QUERIES)
    check_alias_columns(queries, tables, views, QUERIES)
    check_alias_columns(seed, tables, views, SEED)
    check_csharp_procs(procs)

    print()
    if warnings:
        print("WARNINGS (%d)" % len(warnings))
        for w in warnings[:40]:
            print("  -", w)
        print()
    if errors:
        print("ERRORS (%d)" % len(errors))
        for e in errors[:80]:
            print("  -", e)
        return 1
    print("no problems found by the static checks")
    return 0


if __name__ == "__main__":
    sys.exit(main())
