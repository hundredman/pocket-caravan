# Pocket Caravan — 작업 안내 (Claude용)

게임개발(2026-2) 팀 프로젝트. 2D 탑다운 픽셀 교역 게임. 4명 × 4주.
기획서: claude.ai 문서 "Pocket Caravan — 출신 상인과 성장 구조 계획".

## 환경
- Unity 6000.3.26f1 (6.3 LTS), Universal 2D (URP 2D Renderer), Input System
- 16px 타일, PPU 16, Point 필터, 압축 없음 (`Editor/PixelArtImporter`가 Art 폴더에 자동 적용)
- 카메라: Pixel Perfect Camera, 기준 해상도 320×180
- 글은 한국어, 짧고 담백하게

## 구조
- namespace `PocketCaravan`, 코드 `Assets/PocketCaravan/Scripts/`
- `GameManager`는 첫 씬 전에 자동 생성(RuntimeInitializeOnLoadMethod). 씬에 넣지 않는다
- 맵 하나 = 씬 하나. `MapRoot`(Grid에 붙음): Ground/Road/Blocked 타일맵. Blocked에 타일이 있으면 못 지나감
- 출구 `MapExit` → `GameManager.TravelTo(scene, spawnId)` → 도착 씬의 `SpawnPoint(id)`
- 시세: `MarketMath`(공식) + `MarketState`(마을별 실행 상태). 데이터는 `TownData`, `ItemData`
- 길: 맵에 미리 칠한 Road 타일의 등급만 읽는다 (`RoadGrid`). 플레이어가 길을 만드는 기능은 넣지 않는다 (MMORPG에서 맵이 망가짐)
- 퀘스트: `QuestData`(단계 목록) + `QuestManager`(GameEvents를 듣고 진행) + `QuestNpc`(!/? 표시). 대화창 `DialogueUI`, 진행 표시 `QuestTracker`
- 밸런스 값은 `Resources/GameDatabase.asset`
- 에디터 메뉴 `Pocket Caravan > 1. 시작 세트 만들기`가 임시 아트·타일·데이터·씬을 만든다 (있는 것은 건드리지 않음)

## 주의
- ScriptableObject 클래스는 파일 이름과 클래스 이름이 같아야 한다 (한 파일에 하나)
- 스크립트를 바꾼 뒤 컴파일이 끝난 것을 확인하고 메뉴를 실행할 것
- "맵 씬 다시 만들기"는 씬에서 손으로 고친 내용을 지운다
