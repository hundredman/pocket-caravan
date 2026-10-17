# MapGen: 본편 지형·지도 생성

2026-10-18 안내. 본편은 고려6맵+중간권4맵이다. exporter는 `World/settings.json`의 `atlasRegions`(goryeo·transit)를 생성하며, 다른 팀 지역을 자동 생성하지 않는다. 환경 원형40개와 장애물48종은 별도 제작 라이브러리다. [문서 안내](../../Docs/README.md).

맵은 이 파이썬 코드로 설계하고, Unity는 결과 JSON을 씬으로 만든다.

1. 에셋 팩(Ninja Adventure, CC0)을 저장소 옆 `팀 프로젝트/Ninja Adventure - Asset Pack`에 둔다. 다른 곳이면 `NA_PACK` 환경 변수로 경로를 준다.
2. `pip install pillow` 후 저장소 루트에서 `python3 Tools/MapGen/export.py` 를 실행한다.
   - `Assets/PocketCaravan/Maps/*.json`: 맵 데이터
   - `Assets/PocketCaravan/Art/NinjaAdventure/Tiles/*.png`: 쓴 타일만 잘라 낸 그림
   - `preview_*.png`: 맵 미리보기 (저장소에 올리지 않음)
   - `Assets/PocketCaravan/Resources/WorldMap/`: 지형 지도 PNG 및 맵 배치 JSON
3. Unity에서 자기 맵 선택 재생성을 우선한다. 메뉴 `Pocket Caravan > 2. 맵 씬 다시 만들기`는 등록 맵 전체를 덮어쓰므로 다른 팀 씬까지 생성하려는 경우에만 실행한다. 선택 생성 API는 [팀 안내](../../Docs/TeamDevelopment.md)를 따른다.

## 파일
- `tiles.py`: 타일 목록, 오토타일(흙길·물 가장자리 자동 선택), 집·나무 같은 여러 칸짜리 묶음, 미리보기
- `maps.py`: 예성 나루 마을, 예성 길, 닥나무 마을
- `gaegyeong.py`: 개경 (성벽과 남문, 큰길, 시전 거리, 만월대)
- `cartography.py`: 실제 출구 방향·스폰 정렬로 맵을 이어 붙인 지형 약도. 강·숲·길·집·다리를 축약해 표시
- `validate.py`: 나무 묶음 보존, 길·다리 통행, 스폰·상호작용 접근성, 출구 연결 검사 (`python3 validate.py`)

## 맵 고치기
- `road_path`(굽은 길), `road_rect`(넓은 길), `water_rect`, `bridge`, `stamp`(집·나무), `add`(스폰·출구·NPC)
- NPC 종류: `npc`(퀘스트, npc id), `market`(장터, town id), `villager`(마을 사람), `worker`(작업 왕복 연출)
- 출구는 `target`(씬 이름)과 `spawn`(도착 자리 id)을 맞춘다

맵 목록·생성 함수는 `World/Regions/goryeo.json`·`transit.json`이 기준이고 생성 범위는 `World/settings.json`의 atlasRegions가 결정한다. Unity 빌더·WorldIndex·macOS 프리뷰 빌드도 같은 지역 등록을 읽는다. Python exporter는 atlasRegions에 명시하지 않은 다른 팀 지역을 자동 생성하지 않는다. 새 지역의 JSON/씬/데이터 등록 순서는 [팀 개발 시작 안내](../../Docs/TeamDevelopment.md)를 따른다.

생성기를 다시 돌리면 씬을 덮어쓰고, 시작 세트 생성은 데이터 값도 다시 쓴다.
맵 원본 코드/JSON과 공용 생성기의 담당자를 정하고, 씬에서만 수정한 내용을 먼저 보존한다.
읽기 전용 통합 검사: 저장소 루트에서 `python Tools/Integration/validate_world.py`.

## 2D 유지보수 (2026-10-15)

- 숲은 나무 한 그루/묶음 전체를 놓거나 건너뛴다. 예약 칸만 피하면서 일부 타일을 남기는 배치는 하지 않는다.
- `force=True`도 이미 있는 나무·집의 조각을 덮어쓰지 않는다. 맵 밖에 걸치는 묶음도 배치하지 않는다.
- 폭 1 대각선 길은 상하좌우로 연결한다. 집 문 앞 길과 선착장 접근로도 주 길까지 연결한다.
- `bridge(..., dock=True)`는 선착장, 기본은 양안 다리다. 방향에 맞는 판자와 난간을 생성하고 바닥 전체는 통행 가능하다.
- 지형 지도 좌표는 시세의 `worldCoord`와 별개다. 맵 배열 모양만 바꾸려고 가격용 좌표를 수정하지 않는다.
- 지도 PNG는 씬 빌더가 Point/무압축/밉맵 없음/읽기 가능으로 임포트한다. 읽기 가능은 OnGUI 색 공간 보정용 사본을 만드는 데 필요하다.

이 작업은 기존 2D 버전 유지보수이며 최종 시점·아트 방향을 확정하지 않는다.


## 예성 길 경로 선택 (2026-10-15)

`Route01`은 주막·큰 다리의 큰길(노면 3)과 북쪽 작은 목교를 지나는 숲길(노면 1)이 분기·합류한다. 길 등급과 너비로 기존 VehicleData의 차이를 활용하며 수단 속도 값은 바꾸지 않았다. `python3 validate_route_choice.py`는 두 경로의 연결과 수단별 비용 차이를 실제 이동 수단 에셋으로 검사한다. 출력 시간은 직교 그래프 계산이며 실제 조작·재미 검증과 구분한다. 전체 회귀는 `python3 validate.py`로 실행한다. Pillow가 있는 Python 환경이 필요하다.

## 작업장과 주변음 (2026-10-15)

`m.add("worker", x, y, name="짐꾼", character="villager_dog_tall", lines=["짧은 대사"], path=[(x, y), (x2, y2)])`로 두 작업 지점을 연결한다. 좌표는 다른 맵 객체처럼 좌상단 기준 타일 좌표이며 JSON에서는 평탄한 `[x, y, x2, y2]` 배열로 내보낸다. 작업자는 양쪽에서 쉬며 짐을 들거나 내리고, 플레이어 가까이서는 멈춘다. 통행을 막거나 실제 재고를 생산하지 않는다. 두 지점 사이 직선 전체가 통행 가능해야 하며 검증기가 왕복 구간을 검사한다.

`m.add("ambience", x, y, id="river", radius=12)`는 `Resources/Audio/Sfx/river`의 반복 주변음을 배치한다. 반경은 월드 단위이며 맵은 타일 1칸이 1단위다. 플레이어와 멀어지면 음량이 줄어들고 기존 음소거를 따른다. 겹치는 영역은 과도하게 배치하지 않는다.

`paper_rack`은 이 생성기에서 그리는 3×2칸 종이 건조대 스탬프다. 집·나무와 같은 묶음 배치 규칙을 사용한다. 원본을 수정한 뒤 export하고, 필요한 씬만 다시 생성한다.

2026-10-17 개발 반영: 신규 주행 낙하/게이지/정차 안정화는 사용하지 않는다. 노면/짐/속도에 반응하는 시각 흔들림은 감속 연출로 유지한다. 현재 기준은 Docs/DevelopmentStatus.md 및 실제 시험장 UI를 따른다.
