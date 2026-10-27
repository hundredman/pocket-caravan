# C6 삭제 전 목록

삭제 예정: 미등록 옛 맵8개의 JSON·씬·각 meta, 합계32파일. 현재 등록11맵/씬은 삭제하지 않는다. 아래목록을 사용자에게 보고한 뒤 진행한다.

- `Assets/PocketCaravan/Maps/Route01.json`
- `Assets/PocketCaravan/Maps/Route01.json.meta`
- `Assets/PocketCaravan/Scenes/Goryeo/Route01.unity`
- `Assets/PocketCaravan/Scenes/Goryeo/Route01.unity.meta`
- `Assets/PocketCaravan/Maps/goryeo_descent.json`
- `Assets/PocketCaravan/Maps/goryeo_descent.json.meta`
- `Assets/PocketCaravan/Scenes/Goryeo/goryeo_descent.unity`
- `Assets/PocketCaravan/Scenes/Goryeo/goryeo_descent.unity.meta`
- `Assets/PocketCaravan/Maps/goryeo_fieldroad.json`
- `Assets/PocketCaravan/Maps/goryeo_fieldroad.json.meta`
- `Assets/PocketCaravan/Scenes/Goryeo/goryeo_fieldroad.unity`
- `Assets/PocketCaravan/Scenes/Goryeo/goryeo_fieldroad.unity.meta`
- `Assets/PocketCaravan/Maps/goryeo_ridge.json`
- `Assets/PocketCaravan/Maps/goryeo_ridge.json.meta`
- `Assets/PocketCaravan/Scenes/Goryeo/goryeo_ridge.unity`
- `Assets/PocketCaravan/Scenes/Goryeo/goryeo_ridge.unity.meta`
- `Assets/PocketCaravan/Maps/goryeo_rivervalley.json`
- `Assets/PocketCaravan/Maps/goryeo_rivervalley.json.meta`
- `Assets/PocketCaravan/Scenes/Goryeo/goryeo_rivervalley.unity`
- `Assets/PocketCaravan/Scenes/Goryeo/goryeo_rivervalley.unity.meta`
- `Assets/PocketCaravan/Maps/goryeo_southcoast.json`
- `Assets/PocketCaravan/Maps/goryeo_southcoast.json.meta`
- `Assets/PocketCaravan/Scenes/Goryeo/goryeo_southcoast.unity`
- `Assets/PocketCaravan/Scenes/Goryeo/goryeo_southcoast.unity.meta`
- `Assets/PocketCaravan/Maps/goryeo_tidalcoast.json`
- `Assets/PocketCaravan/Maps/goryeo_tidalcoast.json.meta`
- `Assets/PocketCaravan/Scenes/Goryeo/goryeo_tidalcoast.unity`
- `Assets/PocketCaravan/Scenes/Goryeo/goryeo_tidalcoast.unity.meta`
- `Assets/PocketCaravan/Maps/goryeo_westcoast.json`
- `Assets/PocketCaravan/Maps/goryeo_westcoast.json.meta`
- `Assets/PocketCaravan/Scenes/Goryeo/goryeo_westcoast.unity`
- `Assets/PocketCaravan/Scenes/Goryeo/goryeo_westcoast.unity.meta`

코드는 `tiles.py`의 시트 추출/색 변경, `goryeo_art.py`의 원본 추출/색 변경/물결 변환, `module_art.py`의 실행 중 그리기를 동결된 PNG 읽기로 대체한다. `maps.py`, `gaegyeong.py`, `frontier.py`, `goryeo_roads.py`, `settlement_details.py`의 옛 동물 NPC 그림 경로는 이미 승인된 사람 역할 경로로 정리한다.

보존: 구 저장8ID 별칭/높이·위치 이주 정보, current 지형에 필요한 옛 생성 함수/등록 메타데이터, 외부 팩/아트 원본, 동물 보관본/플레이어, 다른 팀 작업/3D 실험 폴더.
