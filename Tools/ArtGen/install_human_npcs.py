"""Install approved native frames by role; archive original animal assets without data loss."""
from pathlib import Path
import hashlib,json,shutil,sys
ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/'Assets/PocketCaravan/Art/Characters'
def install():
    approved=ROOT/'ArtReview/Approved/C1-Binding-Walk';approved.mkdir(exist_ok=True)
    for n in ['porter','merchant','merchant_silk','scholar','woman_ricecake']:
        for src in [ROOT/f'ArtReview/C1-Binding-Walk/{n}.aseprite',*(ROOT/'ArtReview/C1-Binding-Walk/frames').glob(n+'_*.png')]:
            dst=approved/src.relative_to(ROOT/'ArtReview/C1-Binding-Walk');dst.parent.mkdir(exist_ok=True)
            if dst.exists() and dst.read_bytes()!=src.read_bytes():raise ValueError('Frozen approved sprite changed: '+str(dst))
            if not dst.exists():shutil.copyfile(src,dst)
    (approved/'approval.md').write_text('# C1 추가5명 방향·걷기 승인\n\n2026-10-24 사용자: **“좋아.”**\n\n직전 제출한 새5명4방향/걷기 비교와 실제Unity 화면에 대한 승인이다. 현재80프레임/원본5개를 동결한다. 이 승인을 근거로 C1-Binding의 본편 일괄교체와 검증을 진행한다.\n')
    assets=[];profiles={};role_rows=json.loads((ROOT/'Tools/ArtGen/npc_roles.json').read_text())['roles']
    from PIL import Image
    for r in role_rows:
        n,key=r['art'],r['key'];source=approved if n in ['porter','merchant','merchant_silk','scholar','woman_ricecake'] else ROOT/'ArtReview/Approved/C1-Walk'
        folder=ART/'NPC'/key;folder.mkdir(parents=True,exist_ok=True);shutil.copyfile(source/f'{n}.aseprite',folder/f'{key}.aseprite')
        colors=set()
        for d in 'dulr':
            for f in range(4):
                dst=folder/f'{key}_tall_{d}{f}.png';src=source/'frames'/f'{n}_{d}{f}.png';shutil.copyfile(src,dst)
                im=Image.open(dst).convert('RGBA');colors.update(p[:3] for p in im.get_flattened_data() if p[3]);assert im.size==(16,24)
                aid=f'{key}_{d}{f}';path=str(dst.relative_to(ROOT));evidence='ArtReview/Approved/C1-Binding-Walk/approval.md' if source==approved else 'ArtReview/Approved/C1-Walk/approval.md'
                assets.append(dict(id=aid,kind='object',purpose='character',size=[1,1],image=path,source=dict(kind='authored',path=str((folder/f'{key}.aseprite').relative_to(ROOT))),review=dict(status='approved',evidence=evidence),profile=key,tiles=[dict(dx=0,dy=0,layer='obj',tile=aid,image=path)]))
        profiles[key]=dict(pixelsPerCell=[16,24],binaryAlpha=True,palette=['#%02x%02x%02x'%c for c in sorted(colors)])
    (ART/'NPC/catalogue.json').write_text(json.dumps(dict(schemaVersion=1,profiles=profiles,assets=assets),ensure_ascii=False,indent=2)+'\n')
    archive=ART/'ArchivedAnimals';archive.mkdir(exist_ok=True);records=[]
    # tanuki is the live player. shadow is shared. Only original NPC species move.
    sys.path.insert(0,str(ROOT/'Tools/ArtGen'));import chars
    for n in chars.SPECIES:
        if n=='tanuki':continue
        for d in 'dulr':
            for f in range(4):
                file=f'{n}_tall_{d}{f}.png';p=ART/file;dst=archive/file
                if p.exists():shutil.move(p,dst)
                if Path(str(p)+'.meta').exists():shutil.move(str(p)+'.meta',str(dst)+'.meta')
                assert dst.exists();records.append(dict(file=file,sha256=hashlib.sha256(dst.read_bytes()).hexdigest(),usageState='보관(꾸미기 후보)',source='Tools/ArtGen/chars.py'))
    (archive/'archive-index.json').write_text(json.dumps(dict(schemaVersion=1,status='보관(꾸미기 후보)',runtimeUse=False,assets=records),ensure_ascii=False,indent=2)+'\n')
    archived_assets=[];archive_profiles={}
    for record in records:
        dst=archive/record['file'];im=Image.open(dst).convert('RGBA');key=dst.stem
        archive_profiles[key]=dict(pixelsPerCell=[16,24],binaryAlpha=True,palette=sorted({'#%02x%02x%02x'%p[:3] for p in im.get_flattened_data() if p[3]}))
        path=str(dst.relative_to(ROOT));aid='archived_'+key
        archived_assets.append(dict(id=aid,kind='object',purpose='character',size=[1,1],image=path,source=dict(kind='authored',path='Tools/ArtGen/chars.py'),review=dict(status='draft'),profile=key,usageState='보관(꾸미기 후보)',tiles=[dict(dx=0,dy=0,layer='obj',tile=aid,image=path)]))
    (archive/'catalogue.json').write_text(json.dumps(dict(schemaVersion=1,profiles=archive_profiles,assets=archived_assets),ensure_ascii=False,indent=2)+'\n')
    print(len(role_rows),'roles installed,',len(assets),'frames;',len(records),'animal PNGs archived')
if __name__=='__main__':install()
