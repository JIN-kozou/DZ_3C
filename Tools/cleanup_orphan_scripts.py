import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Assets"
ORPHAN_GUIDS = {
    "bfd6cec7710802146be56bb22888bf57",
    "89cc64d817b36ec4ca1c35e36c475efc",
    "1b38a8e4b08771846a740d16d639448f",
    "2a02432966727384b82a32b8286e0b56",
    "6b35cc6899fff02448041643089f1095",
    "9191aec90ae808243abe5adaecbdd3b1",
    "198545d8ce5e33a409cb2dc74535d034",
    "0875e312e2778aa498ecf50c9e711735",
    "885a6c9ce99659b4b898720b19ab8230",
    "9b506dbe2557886448f9bb34290d8cd1",
    "30f12e0386cf06346a9a35b6a51a9077",
    "e3f09fd6e74d1584791995c33df307b1",
    "f5270bb7f9f146f409ff8c180ff94363",
    "0e8f0bf51e9547d4d92885e7b0e8aba9",
    "a811bde74b26b53498b4f6d872b09b6d",
}

GUID_BLOCK = re.compile(
    r"--- !u!114 &(\d+)\n"
    r"MonoBehaviour:\n"
    r"(?:  .*\n)*?"
    r"  m_Script: \{fileID: (?:11500000, )?guid: ([0-9a-f]+), type: 3\}\n"
    r"(?:  .*\n)*?",
    re.MULTILINE,
)


def clean_file(path: Path) -> list[str]:
    text = path.read_text(encoding="utf-8")
    original = text
    removed_ids: list[str] = []

    def repl(match: re.Match) -> str:
        component_id, guid = match.group(1), match.group(2)
        if guid in ORPHAN_GUIDS:
            removed_ids.append(component_id)
            return ""
        return match.group(0)

    text = GUID_BLOCK.sub(repl, text)
    for component_id in removed_ids:
        text = text.replace(f"  - component: {{fileID: {component_id}}}\n", "")
    if text != original:
        path.write_text(text, encoding="utf-8", newline="\n")
    return removed_ids


def main() -> None:
    changed = []
    for pattern in ("*.unity", "*.prefab", "*.asset"):
        for path in ROOT.rglob(pattern):
            removed = clean_file(path)
            if removed:
                changed.append((path.relative_to(ROOT.parent), removed))

    deleted = []
    for path in ROOT.rglob("*.asset"):
        text = path.read_text(encoding="utf-8")
        match = re.search(
            r"m_Script: \{fileID: (?:11500000, )?guid: ([0-9a-f]+), type: 3\}", text
        )
        if match and match.group(1) in ORPHAN_GUIDS:
            meta = Path(str(path) + ".meta")
            path.unlink()
            if meta.exists():
                meta.unlink()
            deleted.append(path.relative_to(ROOT.parent))

    print(f"YAML files cleaned: {len(changed)}")
    for rel, ids in changed:
        print(f"  {rel}: removed component ids {ids}")
    print(f"Deleted orphan assets: {len(deleted)}")
    for rel in deleted:
        print(f"  {rel}")


if __name__ == "__main__":
    main()
