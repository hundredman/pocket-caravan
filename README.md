# Pocket Caravan

대항해시대 이전의 상인이 되어 짐을 싣고, 길을 골라 나르고, 돈을 버는 2D 픽셀 교역 게임. 게임개발(2026-2) 팀 프로젝트.
지역 담당: 은샘 고려 · 윤채 베네치아 · 은인 아라비아 · 기우 북유럽.

**작업을 시작하는 사람과 에이전트는 [AGENTS.md](AGENTS.md)부터 읽는다.** 지역별 작업 안내는 그 문서에서 고른다.

고려·중앙 대륙 동쪽(은샘 담당) 문서:

| 알고 싶은 것 | 문서 |
|---|---|
| 확정 방향(아트, 에셋 규칙, 세계 면적, 고려 구조) | [Docs/Direction.md](Docs/Direction.md) |
| 할 일과 결정 대기 | [Docs/Plan.md](Docs/Plan.md) |
| 지금 구현된 것 | [Docs/CurrentState.md](Docs/CurrentState.md) |
| 문서 전체 목록 | [Docs/README.md](Docs/README.md) |

> 2026-10-24 기준으로 고려·중앙 대륙 동쪽의 맵과 아트를 새 방향에 맞춰 다시 만드는 중이다. 지금 맵과 그림은 이전 버전이다.

## 실행

1. Unity Hub에서 **6000.3.26f1 (Unity 6.3 LTS)** 를 설치한다. 팀 전원이 같은 버전을 쓴다.
2. 저장소를 받아 Unity Hub → Add로 이 폴더를 연다.
3. `Assets/PocketCaravan/Scenes/Goryeo/Naru.unity`를 열고 Play한다. 메뉴 `Pocket Caravan > 1. 시작 세트 만들기`는 데이터를 초기화하므로 평소에는 쓰지 않는다.

조작: 방향키/WASD 이동 · Shift 달리기 · Space 말 걸기·확인 · Q 퀘스트 · M 지도 · L 교역 장부 · Esc 메뉴

## 검사

```sh
python -m pip install "Pillow>=10,<13"
python Tools/Integration/validate_world.py
python -m unittest discover -s Tools/Integration -p 'test_*.py'
```

## 폴더

```
Assets/PocketCaravan/
  Art/          그림 (지역별)
  Data/         ScriptableObject 데이터 (지역별)
  Maps/         생성된 맵 JSON (직접 고치지 않음)
  Resources/    GameDatabase, 지도 이미지, 폰트, 소리
  Scenes/       맵 하나 = 씬 하나 (생성물)
  Scripts/      게임 코드 (namespace PocketCaravan)
  Editor/       에디터 도구와 검사
  World/        지역·맵 등록 JSON
Tools/
  MapGen/       맵 생성기 (Python)
  ArtGen/       코드로 그린 캐릭터·아이콘
  Integration/  자동 검사
  WorldLayout/  세계 배치 계산 (검토안)
Docs/           문서 (Docs/README.md 참고)
```

## 협업

- `main`은 항상 실행되는 상태로 둔다. 기능은 브랜치에서 작업하고 PR로 합친다.
- 씬 파일은 한 사람만 고친다.
- Unity 버전을 올리지 않는다.

## 라이선스와 출처

- 현재 일부 타일과 소리: Ninja Adventure 에셋 팩 (CC0). 새 아트 기준에 따라 교체할 예정이다.
- UI 폰트: 갈무리 (OFL).
