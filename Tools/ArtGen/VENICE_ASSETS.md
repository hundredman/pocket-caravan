# 베네치아 에셋 목록 (임시 아트, 팀 아트 승인 전)

갱신: 2026-10-21. 원본은 저장소 밖(`~/Downloads/venice_gpt`, `Venice_World_Claude_Pack`, `Venice_District_Expansion_Pack`)에 보존한다.
`venice_gpt_atlas.py`가 원본을 잘라 `venice_atlas.png/json`(16px 칸)으로 만들고, `MapGen/export_venice.py`가 맵에서 쓴 칸만 공용 Tiles 폴더에 PNG로 내보낸다.
정리한 개별 스프라이트는 `~/Downloads/venice_gpt/cleaned/`에 저장한다(저장소 밖).

## 픽셀 기준
- 타일 16×16, PPU 16, Point 필터, 압축 없음, 밉맵 없음 (`Editor/PixelArtImporter`가 Art 폴더에 자동 적용). 캐릭터 16×24(발밑 피벗).
- Pixel Perfect Camera: Asset PPU 16, 기준 해상도 384×216, Grid Snapping = Pixel Snapping.
- 건물·큰 구조물: 원본 6.5px = 게임 1px(시트별 원본 도트 4~9px을 측정). 생활 소품: 캐릭터 키(24px) 기준 실제 크기로 칸 수를 정한다.
- 도트 정리(`tidy`): 물체별 색 수 제한(건물 10, 소품 7) → 비슷한 색 합치기 → 명암 차이가 작은 잔무늬(기와선·나뭇결·잔점)를 주변 색으로 흡수, 대비가 큰 창·문은 유지 → 바깥 외곽선 한 색.
- 충돌: 그림이 30% 이상 찬 칸만 막는다(`VeniceMap.building`). 맨 윗줄의 얕은 칸·나무 윗부분은 플레이어 위에 그린다.

## 사용 현황 (venice_lagoon)
| 묶음 | 사용 | 미사용(이유) |
|---|---|---|
| 지역 건물 `cap_/gls_/orc_/fsh_/prt_` (30) | 대부분 사용 | `orc_pergola`(서쪽 길과 겹침), `orc_lemon`(→ `px_orc_lemon_tree2`로 대체) |
| 지역 색 변형 `cap_narrow/row/ell/inn`, `gls_narrow/row`, `fsh_small` | 수도·유리공방·어촌 | 같은 집 대량 복제를 피하려 마을당 2~4채만 |
| 생활 소품 `px_*` (31) | 29 | `px_cap_fountain`(우물과 경쟁, 지시대로 선택 사용 안 함), `px_cap_bunting`(광장 통로 폭) |
| 이전 GPT 집 `house_ochre/rose/brick/altana`, `row_houses`, `house_ell/narrow`, `inn`, `loggia`, `workshop2` | `house_ochre`(과수원) | 지역 시트 건물로 대체. 주황 기와가 수도·어촌 색 규칙과 맞지 않음. 다른 마을 확장 때 후보 |
| 이전 GPT 소품(우물·노점·상자·통·배·말뚝·나무 등) | 대부분 사용 | `stall_blue`·`glass_workshop`(자리 없음), `crate_lemon/fish/glass`, `sack_*`, `luggage` 일부 |
| 염전 소품 `salt_*` | 사용 안 함 | 지시에 따라 염전 경관 제외. 소금 상품은 키오자 창고 앞 장터 상인 |
| 바닥 무늬 `m_*` | 연결로·기본 바닥에 사용(`vq:*`가 간접 사용) | — |
| 지역 바닥 `g_*` (sheet15, 20종) | 수도 크림 석판·흰 물가 돌·광장 모자이크 / 유리공방 벽돌·그을음 벽돌·짙은 흙 / 과수원 풀밭·꽃잎 풀밭·흙길 / 어촌 자갈·젖은 모래 / 항구 화강암·바퀴 자국·드문 얼룩 | `g_fsh_deck`(부두는 기존 목재 다리 사용). 반복이 눈에 띄는 모자이크·얼룩·바퀴 자국·꽃잎은 포인트로만 |

| 수도 랜드마크·장식 `cp_*` (sheet16·17, 15종) | 대성당·종탑·궁전·사자/성인 기둥·장식 우물·비단 노점·화로·태피스트리·벤치·화분·깃발·계류 말뚝 | `cp_bridge`(위에서 본 걷는 면 불분명 → 기존 나무 다리) |
| 마을 새 건물·소품 `n_*` (sheet18~21, 36종) | 마을별 주 건물을 우선 교체(`swap`: 자리가 안 맞으면 기존 건물). 이미 굵은 도트라 색 정리 생략(파란 문·유리 포인트 보존), 축척 7.5px→1px | `n_orc_olive`(빈자리 없음), `n_prt_crane`(기존 하역기 유지) |

| 수변 팩 `w_*` (Venice_Waterfront_Pack, 14종 분리) | 대기 쉼터·밧줄 난간·짐수레·관리소·짐 실은 배·계류 소품·덩굴 벤치·꽃 화분·레몬 화분·꽃수레·축제 깃발·과일 바구니 벤치 | 석재 선착장·ㄱ자/T자 부두·목교·물가 계단·둥근 데크: 늘리거나 90도 돌리면 시점이 틀어져 부두·다리는 목재 다리 타일로 조립(계단·데크는 아틀라스에만 두고 미사용) |

## 남은 문제
- `fsh_house` 등 일부 지역 시트 건물은 원본 기와선 대비가 커서 정리 후에도 격자무늬가 남는다(재생성 또는 손질 대기).
- 좌상단 광원·시점 비율은 자동으로 고칠 수 없다. 전면 벽이 높은 `cap_residence`, `gls_artisan`은 높은 건물로 주의해서 쓴다.
- Unity에서 실제 표시·충돌·정렬은 아직 확인하지 않았다.
