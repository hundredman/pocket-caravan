# Pocket Caravan — 팀 공용 작업 안내

사람과 AI 에이전트(Claude, Codex 등)가 이 저장소에서 작업을 시작할 때 읽는 문서다. `CLAUDE.md`는 이 파일을 불러온다.
게임개발(2026-2) 팀 프로젝트. 대항해시대 이전 상인 문화를 각색한 2D 탑다운 픽셀 교역 게임.

## 지역 담당과 작업 안내

| 담당 | 지역 | 지역별 작업 안내 |
|---|---|---|
| 은샘 | 고려 + 중앙 대륙 동쪽 | [Docs/Goryeo/AgentGuide.md](Docs/Goryeo/AgentGuide.md) |
| 윤채 | 베네치아 | 담당자가 정함 |
| 은인 | 아라비아 | 담당자가 정함 |
| 기우 | 북유럽 | 담당자가 정함 |

자기 지역 작업을 할 때는 해당 안내를 따른다. 다른 지역의 안내·작업 계획은 그 지역 담당자에게만 적용된다.

## 모두 지킬 것

- **다른 지역은 고치지 않는다.** 다른 담당의 씬·데이터·그림·브랜치를 다시 만들거나 덮어쓰지 않는다.
- **공용 파일은 조심해서 고친다.** `Assets/PocketCaravan/Scripts/Core·Economy·Quests·UI`, `Resources/GameDatabase.asset`·`WorldIndex.asset`, `ProjectSettings/EditorBuildSettings.asset`, `World/Connections`는 여러 지역이 함께 쓴다. 고칠 때는 PR에 적고 팀에 알린다.
- **.asset 충돌은 양쪽을 다 살린다.** `.gitattributes`에서 `.asset`·`.unity`가 `merge=binary`라, 서로 다른 항목을 추가해도 충돌로 표시된다. "한쪽 선택"으로 풀면 다른 지역 항목이 사라진다.
- **합치기 전 검사:** `python Tools/Integration/validate_world.py`, `python -m unittest discover -s Tools/Integration -p 'test_*.py'`, Unity 컴파일·시작 맵 실행.
- **아이디 유지:** 시장·상품·퀘스트·NPC·시설 아이디와 저장 형식을 바꾸면 옛 저장 이주를 함께 만든다.

## 환경

- Unity **6000.3.26f1**. 버전을 올리지 않는다.
- 코드: `Assets/PocketCaravan/Scripts/` (namespace `PocketCaravan`), 에디터 도구: `Assets/PocketCaravan/Editor/`.
- 16px 타일, PPU 16. 실행·폴더 구조는 [README](README.md).
