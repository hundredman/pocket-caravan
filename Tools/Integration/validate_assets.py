"""Read-only A2 CLI. Catalogue paths are relative to --root, not the current directory."""
import argparse
import json
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'MapGen'))
from asset_manifest import validate


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('manifest', type=Path)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--map', type=Path, action='append', default=[])
    parser.add_argument('--preview', action='store_true', help='technical preview only; allow assets without user approval')
    parser.add_argument('--report', type=Path, help='optional report destination; inputs are never written')
    args = parser.parse_args(argv)
    catalogue = None
    try:
        catalogue = json.loads(args.manifest.read_text(encoding='utf-8'))
        maps = [(str(path), json.loads(path.read_text(encoding='utf-8'))) for path in args.map]
        errors = validate(catalogue, maps, args.root, not args.preview)
    except (OSError, ValueError) as exc:
        errors = [dict(code='input_read', location='$', message=str(exc))]
    entries = catalogue.get('assets', []) if isinstance(catalogue, dict) else []
    unprofiled = [a.get('id') for a in entries if isinstance(a, dict) and 'profile' not in a] if isinstance(entries, list) else []
    report = dict(mode='technical_preview' if args.preview else 'production', manifest=str(args.manifest),
                  maps=[str(p) for p in args.map], placementsChecked=bool(args.map), unprofiledAssets=unprofiled, errors=errors, errorCount=len(errors),
                  notChecked=['visual_style', 'actual_user_authorization', 'Unity_scene', 'gameplay'],
                  note='Only explicitly configured pixel profiles and terrain rules are checked. No art standard is chosen.')
    payload = json.dumps(report, ensure_ascii=False, indent=2)
    if args.report:
        inputs = {args.manifest.resolve(), *(p.resolve() for p in args.map)}
        def protect_files(value):
            if isinstance(value, dict):
                for key, item in value.items():
                    if key in ('image', 'path', 'evidence') and isinstance(item, str):
                        inputs.add((args.root / item).resolve())
                    else:
                        protect_files(item)
            elif isinstance(value, list):
                for item in value:
                    protect_files(item)
        protect_files(catalogue)
        if args.report.resolve() in inputs:
            parser.error('--report must not overwrite an input')
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(payload + '\n', encoding='utf-8')
    print(payload)
    return int(bool(errors))


if __name__ == '__main__':
    sys.exit(main())
