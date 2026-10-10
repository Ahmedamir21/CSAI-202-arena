# Deliverable file naming

The requirements file asks for:

```
CSAI202_F25_T[TeamNumber]_[PhaseNumberName]
```

The `F25` part comes from the Fall 2025 version of the document. Our term is **Fall 2026**,
so the honest name is `CSAI202_F26_...`. Because this is a grading detail we do not want to
guess, every export script reads the term from one place: `docs/FILE_NAMING.md` (this file).

## Set the term once, here

```
TERM        = F26        <-- change to F25 if the TA confirms the old term code
TEAM_NUMBER = T--        <-- replace with the real team number
```

## Names we will use at export time

| Phase | File name |
|---|---|
| Proposal | `CSAI202_F26_T--_Proposal` |
| ER diagram (initial) | `CSAI202_F26_T--_ERDiagram_Initial` |
| ER diagram (final) | `CSAI202_F26_T--_ERDiagram_Final` |
| Database schema | `CSAI202_F26_T--_DatabaseSchema` |
| Project phase 1 | `CSAI202_F26_T--_Phase1` |
| Project phase 2 | `CSAI202_F26_T--_Phase2` |

`tools/export_docx.py` reads `TERM` and `TEAM_NUMBER` from this file and writes the exported
files into `export/` with those names, so changing the term is a one-line edit.
