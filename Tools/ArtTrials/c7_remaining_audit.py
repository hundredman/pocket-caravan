"""Current frozen whole-object review coverage, not production replacement mapping."""
import json
from pathlib import Path
from c7_catalogue import rgba_evidence
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/C7-LowNature-20261027/whole-object-review-audit.json'
CANDIDATES={
'house_tile':'C7-Buildings-v6/tile-house.png','house_plain':'C7-Buildings-v6/tile-house.png',
'house_thatch':'C7-Buildings-v5/thatch.png','goryeo_manor':'C7-Gaekju-v1/gaekju.png',
'goryeo_celadon':'C7-WorkshopProps-v4/celadon.png','goryeo_lantern':'C7-GateProps-v1/stone_lantern.png',
'goryeo_royal':'C7-Palace-v1/palace.png','goryeo_stall':'C7-Shop-v1/shop.png',
'goryeo_cottage':'C7-Batch1-v1/cottage.png','goryeo_wall':'C7-CourtyardProps-v1/garden_wall.png',
'paper_rack':'C7-WorkshopProps-v4/paper_rack.png','goryeo_paper':'C7-CourtyardProps-v1/paper_large.png',
'goryeo_pavilion':'C7-Batch1-v1/pavilion.png','goryeo_harvest':'C7-WorkshopProps-v4/harvest.png',
'well':'C7-GateProps-v1/well.png',
'goryeo_woodland1':'C7-Batch2-v1/forest_round.png',
'goryeo_woodland2':'C7-Batch2-v1/forest_bent.png',
'goryeo_woodland3':'C7-Batch2-v1/forest_wide.png',
'goryeo_woodland4':'C7-Batch3-v1/broadleaf.png',
'goryeo_woodland5':'C7-Batch1-v1/willow.png',
'goryeo_woodland6':'C7-Batch3-v1/light_tree.png',
'goryeo_woodland7':'C7-Batch3-v1/golden_tree.png',
}
PENDING={'goryeo_woodland8':'moss_rock','goryeo_woodland9':'shrub','goryeo_woodland10':'tall_grass','goryeo_woodland11':'fallen_log'}
def build():
    doc=json.loads((ROOT/'Assets/PocketCaravan/Art/Baked/catalogue.json').read_text())
    maps=[(p.name,json.loads(p.read_text())) for p in sorted((ROOT/'Assets/PocketCaravan/Maps').glob('*.json'))]
    records=[]
    for a in doc['assets']:
        if not a.get('image','').startswith('Assets/PocketCaravan/Art/Baked/Objects/'):continue
        family=a['id'][3:].rsplit('_',1)[0]
        tiles={t['tile'] for t in a.get('tiles',[])};uses=[]
        for name,m in maps:
            indices={i for i,t in enumerate(m.get('palette',[])) if t in tiles}
            count=sum(1 for layer in ['ground','obj','high','road','deco'] for v in m.get(layer,[]) if isinstance(v,int) and v in indices)
            if count:uses.append(dict(map=name,renderCells=count))
        r=dict(assetId=a['id'],size=a['size'],image=a['image'],usage=uses)
        if family in CANDIDATES:
            candidate=ROOT/'ArtReview/Approved'/CANDIDATES[family]
            # Missing records are surfaced, never silently called approved.
            evidence=rgba_evidence(candidate) if candidate.exists() else None
            approval=candidate.parent/'approval.json'
            recorded=bool(evidence and approval.exists() and evidence['rgbaSHA256'] in approval.read_text())
            r.update(status='approved_visual_candidate_binding_not_done' if recorded else 'candidate_record_needs_check',candidate=str(candidate.relative_to(ROOT)),candidateRGBA=evidence,nativeSizeMatches=bool(evidence and evidence['size']==[16*v for v in a['size']]),note='Potential same-purpose visual. Per-ID aliases, dimensions, foot anchor, placement and collision still need explicit binding review; this report does not approve a replacement.')
        elif family in PENDING:
            candidate=ROOT/'ArtReview/Approved/C7-LowNature-v1'/(PENDING[family]+'.png')
            evidence=rgba_evidence(candidate) if candidate.exists() else None
            approval=candidate.parent/'approval.json'
            recorded=bool(evidence and approval.exists() and evidence['rgbaSHA256'] in approval.read_text())
            r.update(status='approved_visual_candidate_binding_not_done' if recorded else 'current_trial_pending',candidate=str(candidate.relative_to(ROOT)) if recorded else 'ArtReview/C7-LowNature-20261027/Native/'+PENDING[family]+'.png',candidateRGBA=evidence,nativeSizeMatches=bool(evidence and evidence['size']==[16*v for v in a['size']]))
        elif family=='cart':r.update(status='retain_original',note='User selected existing game cart. No generated cart replacement.')
        elif family=='wheat':r.update(status='terrain_retained',note='Original terrain direction retained; field layout review is separate.')
        elif family=='goryeo_woodland0':r.update(status='earlier_pine_direction_record',note='Pine retained by earlier user direction; formal source/approval consolidation required before binding.')
        else:r.update(status='remaining_variant_or_role_review',note='No full replacement approval inferred from a larger/smaller or similarly named asset.')
        records.append(r)
    report=dict(scope='Only complete PNG objects in current C6 catalogue. Single-cell props, modular terrain, characters, legacy/unbound cherry and new gate are outside this inventory; this is NOT a complete C7 finish checklist.',productionChanged=False,records=records)
    OUT.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print('Whole-object audit:',len(records),'objects;',sum(r['status']=='remaining_variant_or_role_review' for r in records),'variant/role reviews remain')
if __name__=='__main__':build()
