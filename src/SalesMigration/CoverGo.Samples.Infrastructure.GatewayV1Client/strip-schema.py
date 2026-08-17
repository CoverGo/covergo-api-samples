#!/usr/bin/env python3
"""Make a downloaded CoverGo gateway schema usable for client generation.

A schema introspected from a deployed gateway is not directly consumable:

  * The deployed V1 gateway stitches RequestManager in, which contributes ~1400 type
    definitions the samples never touch, including an interface with no implementing
    object type. Strawberry Shake rejects that with SCHEMA_INTERFACE_NO_IMPL. A local
    gateway does not stitch RequestManager, which is why the snapshots committed in the
    CoverGo service repositories carry none of it.
  * A handful of other interfaces are declared with no implementor and never referenced.

This script removes the RequestManager surface, then any field left pointing at a removed
type, then unreferenced orphan interfaces — and refuses to drop an orphan interface that
is still referenced, rather than silently producing a schema that fails later.

    python3 tools/strip-schema.py schema.downloaded.graphql schema.graphql
"""
import re
import sys
import pathlib

PREFIX = "requestManager"
BUILTIN = {"String", "Int", "Float", "Boolean", "ID"}


def top_level_blocks(sdl):
    """(name, kind, start, end) for every top-level definition."""
    out = []
    for m in re.finditer(r'^(type|input|interface|enum)\s+(\w+)[^{]*\{.*?^\}', sdl, re.S | re.M):
        out.append((m.group(2), m.group(1), m.start(), m.end()))
    # A union may be single-line (`= A | B`) or multi-line with leading pipes.
    for m in re.finditer(r'^union\s+(\w+)\s*=[^\n]*(?:\n\s*\|[^\n]*)*', sdl, re.M):
        out.append((m.group(1), "union", m.start(), m.end()))
    for m in re.finditer(r'^scalar\s+(\w+).*$', sdl, re.M):
        out.append((m.group(1), "scalar", m.start(), m.end()))
    return out


def defined_types(sdl):
    return set(re.findall(
        r'^(?:type|input|interface|enum|union|scalar)\s+(\w+)', sdl, re.M)) | BUILTIN


def without_descriptions(sdl):
    """Strip docstrings so prose is never read as a type reference."""
    sdl = re.sub(r'""".*?"""', '', sdl, flags=re.S)
    return re.sub(r'"(?:[^"\\]|\\.)*"', '', sdl)


def split_fields(body):
    """Split a type body into field definitions, keeping multi-line argument lists whole."""
    return re.split(r'\n(?=  \w+(?:\(|\s*:))', body)


def drop_root_fields(sdl, type_name, predicate):
    m = re.search(rf'^type {type_name} \{{(.*?)^\}}', sdl, re.S | re.M)
    if not m:
        return sdl, 0
    kept = [p for p in split_fields(m.group(1)) if not predicate(p)]
    dropped = len(split_fields(m.group(1))) - len(kept)
    return sdl[:m.start(1)] + "\n".join(kept) + sdl[m.end(1):], dropped


def strip(sdl):
    report = {}

    def is_request_manager(field):
        name = re.match(r'\s*(\w+)', field)
        return bool(name and name.group(1).startswith(PREFIX)) or bool(
            re.search(rf':\s*\[?{PREFIX}\w*', field))

    for root in ("Query", "Mutation", "Subscription"):
        sdl, n = drop_root_fields(sdl, root, is_request_manager)
        if n:
            report[f"{root} fields dropped"] = n

    removed = 0
    while True:
        hit = next(((s, e) for nm, _, s, e in top_level_blocks(sdl)
                    if nm.startswith(PREFIX)), None)
        if hit is None:
            break
        sdl = sdl[:hit[0]] + sdl[hit[1]:]
        removed += 1
    if removed:
        report[f"{PREFIX} definitions removed"] = removed

    # Any field still pointing at a removed type would dangle. Iterate to a fixed point,
    # because dropping a field can leave another type unreferenced in turn.
    dropped_fields = []
    for _ in range(6):
        defined = defined_types(sdl)
        missing = {t for t in re.findall(r':\s*\[?(\w+)', without_descriptions(sdl))
                   if t not in defined}
        if not missing:
            break
        pieces, cursor = [], 0
        for nm, _, st, en in sorted(top_level_blocks(sdl), key=lambda b: b[2]):
            block = sdl[st:en]
            if "{" in block:
                head, body = block.split("{", 1)
                body = body.rsplit("}", 1)[0]
                keep = []
                for field in split_fields(body):
                    hit = next((t for t in missing
                                if re.search(rf':\s*\[?{re.escape(t)}[\]!]*', field)), None)
                    if hit:
                        fname = re.match(r'\s*(\w+)', field)
                        dropped_fields.append(
                            f"{nm}.{fname.group(1) if fname else '?'} -> {hit}")
                    else:
                        keep.append(field)
                block = head + "{" + "\n".join(keep) + "}"
            pieces.append((st, en, block))
        rebuilt, cursor = [], 0
        for st, en, block in pieces:
            rebuilt += [sdl[cursor:st], block]
            cursor = en
        rebuilt.append(sdl[cursor:])
        sdl = "".join(rebuilt)
    if dropped_fields:
        report["fields referencing removed types dropped"] = dropped_fields

    for _ in range(5):
        interfaces = set(re.findall(r'^interface (\w+)', sdl, re.M))
        implemented = set()
        for m in re.finditer(r'^type \w+ implements ([^{]+)\{', sdl, re.M):
            implemented |= set(re.findall(r'\w+', m.group(1)))
        orphans = interfaces - implemented
        if not orphans:
            break
        for orphan in sorted(orphans):
            without = re.sub(rf'^interface {re.escape(orphan)}\b[^{{]*\{{.*?^\}}', '',
                             sdl, flags=re.S | re.M)
            uses = len(re.findall(rf':\s*\[?{re.escape(orphan)}[\]!]*',
                                  without_descriptions(without)))
            if uses:
                raise SystemExit(
                    f"orphan interface {orphan} is still referenced {uses}x; "
                    "it cannot be dropped safely - inspect the schema by hand")
            sdl = without
            report.setdefault("orphan interfaces dropped", []).append(orphan)

    return re.sub(r'\n{3,}', '\n\n', sdl), report


def verify(sdl):
    """Fail loudly rather than write a schema that breaks generation later."""
    defined = defined_types(sdl)
    missing = sorted({t for t in re.findall(r':\s*\[?(\w+)', without_descriptions(sdl))
                      if t not in defined})
    if missing:
        raise SystemExit(f"schema still references undefined types: {missing}")

    interfaces = set(re.findall(r'^interface (\w+)', sdl, re.M))
    implemented = set()
    for m in re.finditer(r'^type \w+ implements ([^{]+)\{', sdl, re.M):
        implemented |= set(re.findall(r'\w+', m.group(1)))
    if interfaces - implemented:
        raise SystemExit(f"orphan interfaces remain: {sorted(interfaces - implemented)}")


def main():
    if len(sys.argv) != 3:
        raise SystemExit(__doc__)
    source, destination = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2])
    result, report = strip(source.read_text())
    verify(result)
    destination.write_text(result)

    print(f"{destination}: {len(result):,} bytes, {len(result.splitlines()):,} lines")
    for key, value in report.items():
        if isinstance(value, list):
            print(f"  {key}:")
            for item in value:
                print(f"    - {item}")
        else:
            print(f"  {key}: {value}")


if __name__ == "__main__":
    main()
