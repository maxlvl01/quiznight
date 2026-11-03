"""Folds the test versions into the public one in CHANGELOG.md, before a public release.

Test builds (tools/release.sh --test) each get their own "## [x.y.zb]" section. When one goes public, players should
see one version with everything since the last public release: this moves the entries of every section newer than
LAST_PUBLIC and older than VERSION into VERSION's section, grouped by kind (Added, Changed, Fixed, Removed, Coming
next; newest version's entries first), and removes those test sections.

    py -I tools/fold_changelog.py CHANGELOG.md VERSION LAST_PUBLIC
"""
import re
import sys

# ("Before we start": a joke line that opens the notes, so it stays on top)
KINDS = ["Before we start", "Added", "Changed", "Fixed", "Removed", "Coming next"]
HEAD = re.compile(r"^## \[([^\]]+)\](.*)$")


def key(version):
    """0.10.3b → (0, 10, 3, 1, 'b', 0); a test build 0.12.0d3 → (0, 12, 0, 0, 'd', 3), before any other version with the
    same numbers (as AppVersion.CompareTo in the game); anything else (Unreleased) → None."""
    m = re.fullmatch(r"(\d+)\.(\d+)(?:\.(\d+))?([a-z]?)(\d*)", version.strip(), re.I)
    if not m:
        return None
    letter = m[4].lower()
    # (finished versions after lettered ones: "~" sorts after any letter)
    return (int(m[1]), int(m[2]), int(m[3] or 0), 0 if letter == "d" else 1, letter or "~", int(m[5] or 0))


LINK = re.compile(r"^\[[^\]]+\]:\s*https?://\S+\s*$")
# (the code repo has the downloads from 0.13.5b on; the older ones are on the old downloads-only repo, which has every
# version from 0.5.0a on)
NEW_REPO, OLD_REPO = "maxlvl01/quiznight", "maxlvl01/quiznight-releases"


def with_links(text):
    """The file with a link line for each public version at the end ("[0.13.5b]: https://github.com/…/releases/tag/v0.13.5b"):
    on GitHub every "## [version]" heading is then a link to that version's release (its downloads). Test versions
    (0.13.6d4) and Unreleased have none. The game skips these lines (Changelog.Parse)."""
    lines = text.rstrip("\n").split("\n")
    while lines and (LINK.match(lines[-1]) or not lines[-1].strip()):
        lines.pop()
    links = []
    for line in lines:
        m = HEAD.match(line)
        if not m or (k := key(m[1])) is None or k[4] == "d" or k < (0, 5, 0, 1, "a", 0):
            continue
        repo = NEW_REPO if k >= (0, 13, 5, 1, "b", 0) else OLD_REPO
        links.append(f"[{m[1]}]: https://github.com/{repo}/releases/tag/v{m[1]}")
    return "\n".join(lines) + "\n\n" + "\n".join(links) + "\n"


def parse(text):
    """The file as (preamble, [ (version, header line, [(kind, [lines])], loose lines) ])."""
    lines = text.split("\n")
    pre, releases, cur, kind = [], [], None, None
    for line in lines:
        m = HEAD.match(line)
        if m:
            cur = {"version": m[1], "head": line, "sections": [], "loose": []}
            releases.append(cur)
            kind = None
            continue
        if cur is None:
            pre.append(line)
        elif line.startswith("### "):
            kind = line[4:].strip()
            cur["sections"].append((kind, []))
        elif kind is None:
            cur["loose"].append(line)
        else:
            cur["sections"][-1][1].append(line)
    return pre, releases


def items(lines):
    """A section's lines without the blank lines around them."""
    while lines and not lines[0].strip():
        lines = lines[1:]
    while lines and not lines[-1].strip():
        lines = lines[:-1]
    return lines


def fold(text, version, last_public):
    pre, releases = parse(text)
    target, last = key(version), key(last_public)
    main = next((r for r in releases if key(r["version"]) == target), None)
    if main is None:
        raise SystemExit(f"CHANGELOG.md has no section for {version}")
    tests = [r for r in releases if (k := key(r["version"])) and last < k < target]
    if not tests:
        return text, []
    # newest first: this version, then the test versions from newest to oldest
    merged = {}
    for r in [main] + sorted(tests, key=lambda r: key(r["version"]), reverse=True):
        for kind, lines in r["sections"]:
            merged.setdefault(kind, []).extend(items(lines))
    order = [k for k in KINDS if k in merged] + [k for k in merged if k not in KINDS]
    main["sections"] = [(k, merged[k] + [""]) for k in order]
    out = list(pre)
    for r in releases:
        if r in tests:
            continue
        out.append(r["head"])
        out.extend(r["loose"] if r is not main else [""])
        for kind, lines in r["sections"]:
            out.append(f"### {kind}")
            out.extend(lines)
    return "\n".join(out), [r["version"] for r in tests]


if __name__ == "__main__":
    path, version, last_public = sys.argv[1:4]
    with open(path, encoding="utf-8") as f:
        text = f.read()
    new, folded = fold(text, version, last_public)
    new = with_links(new)
    if new != text:
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(new)
        print(f"CHANGELOG.md: test versions {', '.join(folded)} folded into {version}" if folded else "CHANGELOG.md: links updated")
    else:
        print(f"CHANGELOG.md: no test versions between {last_public} and {version}")
