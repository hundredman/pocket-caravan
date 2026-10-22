# 기존 검사 회귀 2026-10-23 00:12:31 · Unity 6000.3.26f1
- [PASS] 지역 통합 등록 검사
```
ok
```
- [PASS] 고려 거점·시설·저장 이주 검사
```
{"passed":["고려 수도 포함 독립5거점","모든 고려 거점 기본 시설 보유","문턱 직전 시설 거절·재산 불변","다른 플레이어 신용은 공유 안 함","정확한 문턱에서 해금","다른 마을의 시설 참조 거절","개인 신용 충족해도 마을 등급 부족 거절","일반 상품 입고","원가를 유지한 입고/출고","빈 창고 재출고 거절","의뢰/퀘스트 물품 입고 거절","창고 한도 초과 입고 거절","적재 초과 출고 거절","저장 왕복 수량/원가/마을 보존","두 캐릭터의 창고 분리","알 수 없는 창고 ID 보존","손상 창고 거절 후 기존 화물 보존","버전1 저장의 보관함 누락 호환","기존 낙하 화물 적재초과도 복구","낙하 화물 원가 보존","이주 재호출 중복 없음·미지 ID 보존","의뢰 화물 이주·상태 보존","새 저장 파일 쓰기","원자 교체와 이전 저장 백업","미지원 미래 저장 버전 거절"]}
```
- [PASS] 게시판 지역·경로 검사
```
{"passed":["같은 지역 연결된 마을 허용","연결되어도 관문 전 다른 지역 신규 의뢰 차단","같은 지역 단절 마을 차단","자기 마을/미등록 목적지 차단","지역 미지정 차단","출신이 달라도 현재 출발 지역 기준","신규 생성 목적지 필터","적합한 목적지 없으면 빈 게시판","일반 의뢰 보장 유지","유효한 게시판 재초기화 시 재추첨 없음","오래된 잘못된 제안 숨김/수락 차단","불러온 미수락 제안 정리","게시판 정리 시 기존 유효 제안 보존","경로 변경 후 수락 재검사","유효한 의뢰 수락","기수락 타 지역 의뢰/짐 보존","기수락 타 지역 의뢰 기존 납품 유지","기존 게시판 JSON 형식 호환"]}
```
- [PASS] 여정 안내 데이터 검사
```
{"passed":["five connected regional destinations","no foreign regional destination","all five authored with return trade hints","unknown map offers no false destination","missing region offers no destination","credit zero previews info at one","credit one previews warehouse at three","premium preview mentions conditional board availability","credit three does not invent further tier","lost credit previews basic rest access at zero","empty region has no phantom premium unlock","unobserved town not treated as visited market","observed market distinguished from unknown"]}
```
- [PASS] 고려 체험 데이터 검사
```
{"passed":["goryeo_songak -> gaegyeong/ginseng batch profitable","goryeo_songak/ginseng supply persists after stock recovery","goryeo_jangdan -> gaegyeong/grain batch profitable","goryeo_jangdan/grain supply persists after stock recovery","goryeo_jangdan -> dakmok/firewood batch profitable","goryeo_jangdan/firewood supply persists after stock recovery","dakmok -> goryeo_jangdan/paper batch profitable","dakmok/paper supply persists after stock recovery","naru/salt same-market resale has no profit","naru/grain same-market resale has no profit","naru/firewood same-market resale has no profit","naru/ginseng same-market resale has no profit","naru/paper same-market resale has no profit","gaegyeong/ginseng same-market resale has no profit","gaegyeong/najeon same-market resale has no profit","gaegyeong/paper same-market resale has no profit","gaegyeong/salt same-market resale has no profit","gaegyeong/grain same-market resale has no profit","gaegyeong/firewood same-market resale has no profit","dakmok/paper same-market resale has no profit","dakmok/ginseng same-market resale has no profit","dakmok/salt same-market resale has no profit","dakmok/najeon same-market resale has no profit","dakmok/grain same-market resale has no profit","dakmok/firewood same-market resale has no profit","goryeo_songak/ginseng same-market resale has no profit","goryeo_songak/paper same-market resale has no profit","goryeo_songak/salt same-market resale has no profit","goryeo_songak/grain same-market resale has no profit","goryeo_songak/firewood same-market resale has no profit","goryeo_jangdan/grain same-market resale has no profit","goryeo_jangdan/firewood same-market resale has no profit","goryeo_jangdan/salt same-market resale has no profit","goryeo_jangdan/paper same-market resale has no profit","transit_woodtown/transit_resin same-market resale has no profit","transit_woodtown/transit_driedfish same-market resale has no profit","transit_woodtown/firewood same-market resale has no profit","transit_woodtown/grain same-market resale has no profit","transit_woodtown/paper same-market resale has no profit","transit_woodtown/salt same-market resale has no profit","transit_ferry/transit_resin same-market resale has no profit","transit_ferry/transit_driedfish same-market resale has no profit","transit_ferry/grain same-market resale has no profit","transit_ferry/salt same-market resale has no profit","transit_ferry/paper same-market resale has no profit","transit_ferry/firewood same-market resale has no profit","arabia_ahsa/arabia_dates same-market resale has no profit","arabia_ahsa/arabia_grain same-market resale has no profit","arabia_zabid/arabia_grain same-market resale has no profit","arabia_zabid/arabia_dates same-market resale has no profit","arabia_taiz/arabia_cloth same-market resale has no profit","arabia_taiz/arabia_grain same-market resale has no profit","arabia_taiz/arabia_dates same-market resale has no profit","arabia_jeddah/arabia_spice same-market resale has no profit","arabia_jeddah/arabia_cloth same-market resale has no profit","arabia_jeddah/arabia_dates same-market resale has no profit","arabia_jeddah/arabia_grain same-market resale has no profit","arabia_sanaa/arabia_cloth same-market resale has no profit","arabia_sanaa/arabia_spice same-market resale has no profit","arabia_sanaa/arabia_grain same-market resale has no profit","arabia_sanaa/arabia_dates same-market resale has no profit","zero serialized factor preserves legacy base price","invalid serialized factor preserves legacy base price","paper purchase explicitly targets Dakmok","cart available after two introductory quests","old objective indices preserved","goryeo_songak_trade uniquely registered","goryeo_jangdan_trade uniquely registered","goryeo_cart_supply uniquely registered","post-cart shipment does not force legacy tutorial completion","cart supply exceeds backpack capacity","cart supply fits handcart"],"trades":["goryeo_songak -> gaegyeong / ginseng x2: cost 53, revenue 65, profit 12","goryeo_jangdan -> gaegyeong / grain x3: cost 15, revenue 31, profit 16","goryeo_jangdan -> dakmok / firewood x4: cost 8, revenue 20, profit 12","dakmok -> goryeo_jangdan / paper x2: cost 26, revenue 35, profit 9"]}
```
- [PASS] 중간권 왕복 시세 검사
```
{
    "checks": [
        "transit_resin: 공급지/거리2 및 1·3·5개 거래 수수료 후 이익 확인",
        "transit_driedfish: 공급지/거리2 및 1·3·5개 거래 수수료 후 이익 확인",
        "중간권 등록 씬 존재"
    ],
    "trades": [
        {
            "item": "transit_resin",
            "count": 1,
            "cost": 8,
            "revenue": 11,
            "profit": 3
        },
        {
            "item": "transit_resin",
            "count": 3,
            "cost": 24,
            "revenue": 33,
            "profit": 9
        },
        {
            "item": "transit_resin",
            "count": 5,
            "cost": 41,
            "revenue": 54,
            "profit": 13
        },
        {
            "item": "transit_resin",
            "count": 10,
            "cost": 86,
            "revenue": 102,
            "profit": 16
        },
        {
            "item": "transit_driedfish",
            "count": 1,
            "cost": 9,
            "revenue": 12,
            "profit": 3
        },
        {
            "item": "transit_driedfish",
            "count": 3,
            "cost": 27,
            "revenue": 35,
            "profit": 8
        },
        {
            "item": "transit_driedfish",
            "count": 5,
            "cost": 46,
            "revenue": 57,
            "profit": 11
        },
        {
            "item": "transit_driedfish",
            "count": 10,
            "cost": 96,
            "revenue": 112,
            "profit": 16
        }
    ],
    "scope": "실제 MarketState의 초기 재고·출신 능력 없음·시간 회복 없음. 실제 이동 시간/멀티 수익 검증은 별도."
}
```
