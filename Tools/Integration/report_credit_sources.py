"""Read-only R6 inventory. Facts only: does not classify or change rewards."""
import argparse
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
TYPES = ('Talk', 'Deliver', 'Buy', 'Sell', 'Profit', 'VisitMap',
         'BuyVehicle', 'HaveMoney', 'CompleteContract')


def inventory(root):
    def relative(path):
        return path.relative_to(root).as_posix()

    def field(text, name):
        match = re.search(r'^  ' + re.escape(name) + r':[^\S\r\n]*([^\r\n]*)$', text, re.M)
        return match.group(1).strip() if match else None

    quests = []
    for path in sorted((root/'Assets/PocketCaravan/Data/Goryeo/Quests').glob('*.asset')):
        text = path.read_text()
        goals = []
        for block in re.finditer(r'^  - type: (\d+)\r?\n(.*?)(?=^  - type: |^  rewardMoney:)', text, re.M | re.S):
            kind = int(block.group(1))
            target = re.search(r'^    targetId:[^\S\r\n]*([^\r\n]*)$', block.group(2), re.M)
            goals.append(dict(type=TYPES[kind], targetId=target.group(1).strip() if target else ''))
        if not goals:
            raise ValueError('No objectives parsed: ' + relative(path))
        quests.append(dict(path=relative(path), sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
            id=field(text, 'id'), giverNpc=field(text, 'giverNpc'),
            rewardMoney=int(field(text, 'rewardMoney')), rewardCredit=int(field(text, 'rewardCredit')),
            hasLegacyReward=field(text, 'hasLegacyReward') == '1',
            legacyRewardMoney=int(field(text, 'legacyRewardMoney') or 0),
            legacyRewardCredit=int(field(text, 'legacyRewardCredit') or 0),
            objectives=goals, classification='pending Claude review'))
    writes = []
    pattern = re.compile(r'\bcredit\s*(?:[+\-]?=(?!=)|\+\+|--)')
    for path in sorted((root/'Assets/PocketCaravan/Scripts').rglob('*.cs')):
        for line, text in enumerate(path.read_text().splitlines(), 1):
            if pattern.search(text):
                writes.append(dict(path=relative(path), line=line, source=text.strip()))
    return dict(task='R6 inventory only', questCount=len(quests), quests=quests,
                creditAssignments=writes, runtimeChanged=False,
                limitations=['Limited Unity YAML fields; not a Unity asset deserializer',
                    'Text search can miss aliases, reflection, multiline assignments and external tooling',
                    'No classification, legacy reward conversion, reputation amount or allocation is approved'])


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=ROOT)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = inventory(args.root.resolve())
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n')
    print(f"{result['questCount']} quests; {len(result['creditAssignments'])} credit assignment lines; no game data changed")
