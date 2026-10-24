# 팀 개발 시작 안내



안내 갱신: 2026-10-23. [인수인계 시작 문서](../Archive/Handoff.md)에서 최신 상태·연속 지형 원본·다음 작업을 먼저 확인한다. 최신 역할·운반 변경은 [변경 요약](DesignUpdate_2026-10-17.md)을 먼저 확인한다. 기존 고려 로컬 프로토타입을 팀 공통 출발점으로 사용한다. 온라인은 미구현이다. 북유럽은 PR #2에서 검토 중이며 main에 병합되지 않았다. 다른 팀 지역의 미통합과 미개발은 구분한다.

## 1. 처음 받기

공통 기준 소스는 **`main`** 브랜치다. 고려 개선·중간권·제작 라이브러리·게시판 목적지 제한을 반영한 공통 기준이다. 네 명 모두 최신 main에서 지역별 브랜치를 시작한다. 이미 자기 지역 작업을 시작했다면 폴더 전체를 덮어쓰지 말고 자기 브랜치에 최신 main을 병합해 충돌을 확인한다. 이전 `codex/goryeo-2d-preview` 브랜치는 기록과 기존 작업 연결을 위해 유지한다.

```sh
git clone -b main https://github.com/hundredman/pocket-caravan.git
cd pocket-caravan
```

1. Unity **6000.3.26f1**로 프로젝트를 연다. 패키지는 저장소의 `manifest.json`과 `packages-lock.json`을 사용한다.
2. `Assets/PocketCaravan/Scenes/Goryeo/Naru.unity`를 열어 Play한다. 첫 실행에 데이터/씬을 다시 생성할 필요가 없다.
3. 메뉴 **Pocket Caravan > Tests > 지역 통합 등록 검사**로 등록 상태를 확인한다. 이 검사는 씬·저장을 수정하지 않는다.
4. 자기 작업은 기준 브랜치에서 지역별 브랜치를 만들어 진행한다. 예: `region/north`. 이미 브랜치가 있다면 새로 만들지 않아도 된다.

## 2. 담당별로 수정할 파일

| 범위 | 편집 위치 | 규칙 |
|---|---|---|
| 지역 등록 | `Assets/PocketCaravan/World/Regions/<region>.json` | 자기 지역 파일만 추가/수정. `goryeo.json`을 구조 참고로 사용 |
| 맵 JSON | 등록 파일의 `jsonPath` | 맵마다 기준 원본을 정한다. 현재 고려는 Python이 원본 |
| 씬 | 등록 파일의 `scenePath` | JSON 생성 대상 씬의 수동 변경은 재생성하면 사라짐 |
| 지역 데이터 | `Assets/PocketCaravan/Data/<Region>/` | Region/Town/Item/Quest 등 에셋과 `.meta` 함께 전달 |
| 퀘스트 NPC·주민 대사 | 자기 지역 등록 파일의 `npcs`, `villagers` | 공통 빌더의 고려 대사를 복사해 수정하지 않음 |
| 공통 기능 | `Scripts/`, `Editor/`, `GameDatabase.asset`, 공통 타일·패키지 | 변경 전 관련 담당과 조율. 지역 브랜치에서 각각 다른 사본으로 교체하지 않음 |

지역 담당: 은샘 고려 / 윤채 베네치아 / 은인 아라비아 / 기우 북유럽. 네트워크·경제/저장·UI 등 공통 기능 담당은 별도 확정한다.

## 3. 새 지역을 등록하는 순서

1. [통합 규칙 합의안](TeamIntegrationContract.md)에서 좌표 구역·접두어·접속부를 합의한다. x 구역 배정은 아직 제안이며 코드가 강제하지 않는다. 다른 문명 사이의 중간 구역 담당도 정한다.
2. 자기 RegionData를 만들고 안정적인 `id`를 넣는다. 지역 JSON의 `regionId`와 `regionAssetPath`가 이 에셋을 가리키게 한다.
3. 지역 JSON에 `schemaVersion: 1`, `maps`, `npcs`, `villagers`를 넣는다. 각 맵에는 `mapId`, `jsonPath`, `scenePath`, 정수 `worldX`, `worldY`가 필요하다. 씬 파일 이름은 mapId와 같아야 한다. 등록 파일은 확장자가 `.json`인 것만 자동으로 읽는다. 빈 초안을 먼저 등록하면 검사에 실패하므로 실제 데이터와 함께 올린다.
4. Python 생성기를 쓰는 맵만 `generator: "모듈:함수"`를 기록한다. 공용 exporter는 World/settings.json의 atlasRegions(현재 goryeo·transit)에 등록된 생성기만 실행한다. 다른 지역의 JSON/씬을 자동 생성하거나 타일 아트를 자동 제작해 주는 기능은 아니다.
5. 퀘스트 NPC는 `npcs`에 `id`, `name`, `sprite`, `greeting`을 정의하고 맵 객체의 `npc`가 같은 ID를 참조하게 한다. 일반 주민은 지역의 `villagers`에서 `name`, `sprite`, `lines`를 선택한다. sprite는 기존 공용 캐릭터 로더가 읽는 이름이며 새 아트 형식은 별도 연동이 필요하다.
6. 마을·상품·퀘스트 에셋은 **공용 GameDatabase에도 등록**한다. 에셋 파일을 복사했다고 자동으로 시장·퀘스트 목록에 들어가지는 않는다. 이 공용 에셋은 통합 담당자가 지역별 납품을 모아 수정한다.
7. 출구 `target`에 목적 맵/씬 이름, `spawn`에 목적 맵의 도착 스폰 ID를 넣는다. 아직 없는 외부 맵을 가짜 연결로 등록하지 않는다. 작업 중 막힌 경계는 출구 객체를 만들지 않고, 연결 계획은 문서로 남긴다.
8. 생성 전 `python Tools/Integration/validate_world.py --allow-unbuilt-scenes`를 실행한다. Unity 메뉴의 등록 검사는 GameDatabase 참조도 검사한다. 메뉴 2는 **등록된 JSON 맵 전체의 씬을 다시 생성**한다. 지역 작업은 Editor 도구에서 `MapJsonBuilder.BuildSelected("자기_map_id", ...)`를 호출하는 선택 생성 흐름을 우선한다. 어느 방식이든 등록된 모든 market/board가 공용 DB에 있어야 검증을 통과한다. 다른 지역 데이터를 DB에서 제거한 뒤 전체 재생성을 실행하지 않는다.
9. 생성 씬과 `.meta`를 포함한 뒤 아래 최종 검사를 실행하고 출구를 실제로 왕복한다. 다리/스폰 통로와 수레 폭도 확인한다.

시작 맵은 공통 `World/settings.json`에서 지정한다. 시작 맵의 `S0`는 유지한다. 등록 파일을 추가하면 씬 빌더·WorldIndex·macOS 프리뷰 빌드가 같은 맵 목록을 읽는다. 개별 시험장과 3D 실험은 본편 지역 등록에 넣지 않는다.

현재 공용 타일 로더·시장 상인·플레이어 그림 등에는 고려 프로토타입의 공용 미술 전제가 남아 있다. 등록 구조 공통화가 지역별 최종 아트·지도·경제·온라인 통합의 완료를 뜻하지 않는다. 지형 약도 생성 범위는 atlasRegions(고려·중간권)이며 다른 팀 지역 추가는 별도 통합한다.

## 현재 통합/지역별 시험 주의

공용 게시판 목적지 제한은 main의 #7에 반영됐다. 퀘스트 지역/장 제한은 아직 후속이다. 북유럽 Draft PR #2는 미병합이며 [상세 검토 댓글](https://github.com/hundredman/pocket-caravan/pull/2#issuecomment-6235707730)의 DB 테스트/이벤트 저장/Q6 수정 후 검증한다. 구체적인 역할은 해당 댓글에서 구분한다. 현재 소스 변경이 기존 ZIP에 포함됐다고 안내하지 않는다.

## 4. 통합 검사

저장소 루트에서 Python 3.10 이상으로 실행한다. 등록 검사 자체는 표준 라이브러리만 사용한다. 전체 통합 테스트는 Pillow가 필요하며 Unity·원본 에셋 팩 없이 실행된다.

```sh
python -m pip install "Pillow>=10,<13"
python Tools/Integration/validate_world.py
python -m unittest discover -s Tools/Integration -p 'test_*.py'
```

검사 범위: 등록 맵/지역/NPC ID와 논리 좌표·경로 중복, 지역 에셋 ID 대응, 맵 JSON/씬 존재, 타일 층 길이·값, 시작 S0, 도착 스폰·통로 폭/축·역방향 연결, 지역 주민 정의, Data 아래 종류별 콘텐츠 ID 중복. 대소문자만 다른 ID도 충돌로 취급한다. 의도한 일방통행은 JSON 출구에 `oneWay: true`로 명시한다. 검사는 씬·맵·저장을 변경하지 않는다.

Unity 메뉴 검사는 공용 GameDatabase의 ID·마을 참조와 실제 RegionData 로딩도 검사한다. Python 검사가 Unity 컴파일·실제 통행·저장 호환·온라인 검증을 대신하지 않는다. 두 검사와 실제 출구 왕복을 함께 확인한다.

GitHub Actions의 **Region content checks**가 push/PR에서 같은 Python 검사를 실행한다. Actions 성공 여부를 확인하고 합친다. 고려 원본을 바꿨다면 Pillow 환경에서 `Tools/MapGen/validate.py`, `validate_route_choice.py`도 실행한다.

## 5. 합치기 전 전달할 것

- 지역 등록 JSON, 원본·맵 JSON·생성 씬·사용 에셋과 `.meta`, ID/좌표/연결표.
- Unity 버전, 시작 씬, 대표 화면, 검사 결과와 알려진 오류.
- 공용 코드·데이터 변경이 있다면 그 파일과 이유를 별도로 명시.
- 기존 ID/좌표 변경 여부와 저장·가격 영향. 기존 고려는 이번 정리에서 변경하지 않았다.

씬·프리팹·에셋 충돌을 ‘내 것/상대 것’으로 일괄 선택하지 않는다. 현재 저장소는 이 파일들의 자동 병합을 제한하므로 파일별 소유자를 정하고, 생성 씬은 원본에서 다시 만든다. 공용 GameDatabase에는 양쪽 참조를 의도적으로 합친다. 빌드 결과물은 소스 커밋 대신 Release로 전달한다.

**메뉴 1 ‘시작 세트 만들기’는 최초 데이터 재생성 도구다.** 기존 고려 수치와 공용 데이터 목록을 초기값으로 덮어쓰므로 팀 작업 중 일반 실행 방법으로 사용하지 않는다. 덮어쓰기 확인창을 추가했다. 맵 갱신은 자기 맵 선택 생성이 우선이며 메뉴 2는 전체 재생성임을 확인한다.

지역 납품에는 수도1+수도 외 최소4마을 집계, 마을 등급/시설·개인 신용 해금표, 실제 시설 이용/잠금·다섯 거점 왕복 검사도 포함한다. 기존 고려 JSON은 현재 데이터 구조 참고이며 지역 완성 수량의 예제가 아니다.
