"""Generate Q1 assets from the approved QuestMain1 design, preserving legacy goals."""
import json
from pathlib import Path
import re
import uuid

ROOT = Path(__file__).resolve().parents[2]
QDIR = ROOT/'Assets/PocketCaravan/Data/Goryeo/Quests'
ORDER = ['q1_naru','q1_jangdan','q1_songak','q1_dakmok','q1_gaegyeong']
GUIDS = {q: uuid.uuid5(uuid.NAMESPACE_URL, 'PocketCaravan/quest/'+q).hex for q in ORDER}


def generate():
    design = (ROOT/'Docs/QuestMain1.md').read_text()
    script = re.search(r'guid: (\w+)', (ROOT/'Assets/PocketCaravan/Scripts/Quests/QuestData.cs.meta').read_text())[1]
    items = {}
    for p in (ROOT/'Assets/PocketCaravan/Data').rglob('*.asset'):
        text = p.read_text()
        m = re.search(r'^  id: (\S+)$',text,re.M)
        if m and p.with_suffix('.asset.meta').exists():
            items[m[1]] = re.search(r'guid: (\w+)',p.with_suffix('.asset.meta').read_text())[1]
    def ref(guid): return '{fileID: 11400000, guid: '+guid+', type: 2}' if guid else '{fileID: 0}'
    def string(s): return json.dumps(s,ensure_ascii=False)
    specs = [
        ('naru_chief','salt_parcel',30,1,[(1,'goryeo_jangdan_farmer','salt_parcel',1,'장단 농부에게 소금 꾸러미 전하기')]),
        ('goryeo_jangdan_farmer',None,15,0,[(2,'goryeo_jangdan','grain',3,'장단 장터에서 곡식 3개 사기'),(3,'goryeo_songak','grain',3,'송악 장터에서 곡식 3개 팔기')]),
        ('goryeo_songak_merchant',None,25,0,[(8,'goryeo_songak',None,1,'송악 게시판 의뢰 하나 마치기'),(0,'goryeo_songak_merchant',None,1,'송악 상인에게 알리기')]),
        ('dakmok_chief',None,10,0,[(2,'dakmok','paper',2,'닥목 장터에서 종이 2개 사기'),(0,'dakmok_chief',None,1,'닥목 촌장에게 보이기')]),
        ('dakmok_chief','hanji_parcel',45,1,[(5,'Gaegyeong',None,1,'개경에 들어가기'),(1,'gaekju','hanji_parcel',1,'개경 객주에게 한지 꾸러미 전하기'),(3,'gaegyeong','paper',2,'개경 장터에서 종이 2개 팔기')]),
    ]
    header = '\n'.join(['%YAML 1.1','%TAG !u! tag:unity3d.com,2011:','--- !u!114 &11400000','MonoBehaviour:',
        '  m_ObjectHideFlags: 0','  m_CorrespondingSourceObject: {fileID: 0}','  m_PrefabInstance: {fileID: 0}',
        '  m_PrefabAsset: {fileID: 0}','  m_GameObject: {fileID: 0}','  m_Enabled: 1','  m_EditorHideFlags: 0',
        '  m_Script: {fileID: 11500000, guid: '+script+', type: 3}'])+'\n'
    for i,(id,spec) in enumerate(zip(ORDER,specs)):
        block = re.search(r'### [^\n]*`'+id+r'`[^\n]*\n(.*?)(?=\n### |\n## )',design,re.S)[0]
        title = re.search(r'` — (.*?) \(',block)[1]
        offer = re.search(r'- 제안: "(.*?)"',block)[1]
        finish = re.search(r'- 마무리: "(.*?)"',block)
        complete = re.search(r'- 완료(?:\([^)]*\))?: "(.*?)"',block)
        giver,give,money,credit,goals=spec
        text=header+f'  m_Name: Q1_{id[3:]}\n  m_EditorClassIdentifier: Assembly-CSharp::PocketCaravan.QuestData\n  id: {id}\n  retired: 0\n  title: '+string(title)+'\n  chain: "고려 한 바퀴"\n  summary: '+string(title)+f'\n  giverNpc: {giver}\n'
        text+='  requires: []\n' if not i else '  requires:\n  - '+ref(GUIDS[ORDER[i-1]])+'\n'
        text+='  requiresAny: []\n  choiceGroup: \n  offerLine: '+string(offer)+'\n'
        text+='  giveItem: '+ref(items[give] if give else None)+f'\n  giveCount: {1 if give else 0}\n  offerItem: {{fileID: 0}}\n  offerCount: 0\n  offerPrice: 0\n  objectives:\n'
        for kind,target,item,count,label in goals:
            # Place dialogue on the NPC interaction goal, not on a trade event.
            line=complete[1] if complete and kind in (0,1) else ''
            text+=f'  - type: {kind}\n    text: '+string(label)+f'\n    targetId: {target}\n    item: '+ref(items[item] if item else None)+f'\n    vehicle: {{fileID: 0}}\n    count: {count}\n    completeLine: '+string(line)+'\n'
        text+=f'  rewardMoney: {money}\n  rewardCredit: {credit}\n  rewardReputationScope: 0\n  rewardReputationTarget: '+('goryeo' if not credit else '')+f'\n  rewardReputationSize: {2 if not credit else 0}\n  hasLegacyReward: 0\n  legacyRewardMoney: 0\n  legacyRewardCredit: 0\n  finishNote: '+string(finish[1] if finish else '')+'\n'
        p=QDIR/f'Q1_{id[3:]}.asset';p.write_text(text);p.with_suffix('.asset.meta').write_text('fileFormatVersion: 2\nguid: '+GUIDS[id]+'\n')
    side={'tutorial_3':'q1_gaegyeong','tutorial_4':'q1_songak','tutorial_5':'q1_dakmok','goryeo_jangdan_trade':'q1_jangdan','goryeo_songak_trade':'q1_songak'}
    for p in QDIR.glob('*.asset'):
        s=p.read_text();id=re.search(r'^  id: (\S+)$',s,re.M)[1]
        if id in {'tutorial_1','tutorial_2','naru_board','dakmok_hanji'} and '  retired: 1' not in s: s=s.replace('  id: '+id,'  retired: 1\n  id: '+id)
        if id in side: s=re.sub(r'^  requires:.*?(?=^  \w)', '  requires:\n  - '+ref(GUIDS[side[id]])+'\n',s,flags=re.M|re.S)
        p.write_text(s)
    p=ROOT/'Assets/PocketCaravan/Resources/GameDatabase.asset';s=p.read_text()
    for guid in GUIDS.values():
        if guid not in s: s=s.replace('  crateIcon:','  - '+ref(guid)+'\n  crateIcon:')
    p.write_text(s)
    print('Q1: 5 new assets registered; legacy objectives/rewards preserved')


if __name__=='__main__': generate()
