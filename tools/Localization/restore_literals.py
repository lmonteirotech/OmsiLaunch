"""Restores enum and boolean literals in translated tables.

A table cell whose English value is exactly one of LITERALS is a product
value (PublicCapabilityKind member or a boolean), not prose. Translated pages
keep the English table shape, so the cell at the same row and column is set
back to the English value. Usage: python tools/Localization/restore_literals.py
"""
import io
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import l10n  # noqa: E402

LITERALS = {"Read", "Write", "Action", "Event", "true", "false"}


def cells(row):
    # Split on pipes outside inline code.
    out, cur, code = [], "", False
    for ch in row:
        if ch == "`":
            code = not code
        if ch == "|" and not code:
            out.append(cur)
            cur = ""
        else:
            cur += ch
    out.append(cur)
    return out


changed = 0
for locale in l10n.LOCALES:
    for page in l10n.PAGES:
        path = l10n.localized_path(locale, page)
        if not os.path.exists(path):
            continue
        en_prose = list(l10n.prose_lines(l10n.read(l10n.english_path(page))))
        en_table = [l for l in en_prose if l.strip().startswith("|")]
        text = l10n.read(path)
        lines = text.split("\n")
        # indices of table lines outside code blocks
        idx, pos = [], 0
        for kind, block in l10n.split_blocks(text):
            for j, line in enumerate(block):
                if kind == "text" and line.strip().startswith("|"):
                    idx.append(pos + j)
            pos += len(block)
        if len(idx) != len(en_table):
            print("SKIP shape", locale, page)
            continue
        for k, i in enumerate(idx):
            en_cells, loc_cells = cells(en_table[k].strip()), cells(lines[i].strip())
            if len(en_cells) != len(loc_cells):
                continue
            modified = False
            for c, value in enumerate(en_cells):
                if value.strip() in LITERALS and loc_cells[c].strip() != value.strip():
                    loc_cells[c] = " " + value.strip() + " "
                    modified = True
            if modified:
                lines[i] = "|".join(loc_cells)
                changed += 1
        new = "\n".join(lines)
        if new != text:
            l10n.write(path, new)
print("rows restored:", changed)
