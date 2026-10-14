# MapGen: 고려 시작 지역 맵 만들기

맵은 이 파이썬 코드로 설계하고, Unity는 결과 JSON을 씬으로 만든다.

1. 에셋 팩(Ninja Adventure, CC0)을 저장소 옆 `팀 프로젝트/Ninja Adventure - Asset Pack`에 둔다. 다른 곳이면 `NA_PACK` 환경 변수로 경로를 준다.
2. `pip install pillow` 후 `python3 export.py` 를 실행한다.
   - `Assets/PocketCaravan/Maps/*.json`: 맵 데이터
   - `Assets/PocketCaravan/Art/NinjaAdventure/Tiles/*.png`: 쓴 타일만 잘라 낸 그림
   - `preview_*.png`: 맵 미리보기 (저장소에 올리지 않음)
3. Unity 메뉴 `Pocket Caravan > 2. 맵 씬 다시 만들기`.

## 파일
- `tiles.py`: 타일 목록, 오토타일(흙길·물 가장자리 자동 선택), 집·나무 같은 여러 칸짜리 묶음, 미리보기
- `maps.py`: 예성 나루 마을, 예성 길, 닥나무 마을
- `gaegyeong.py`: 개경 (성벽과 남문, 큰길, 시전 거리, 만월대)

## 맵 고치기
- `road_path`(굽은 길), `road_rect`(넓은 길), `water_rect`, `bridge`, `stamp`(집·나무), `add`(스폰·출구·NPC)
- NPC 종류: `npc`(퀘스트, npc id), `market`(장터, town id), `villager`(마을 사람)
- 출구는 `target`(씬 이름)과 `spawn`(도착 자리 id)을 맞춘다
