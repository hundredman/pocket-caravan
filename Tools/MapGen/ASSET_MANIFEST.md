# A2 에셋 목록과 배치 검사 계약 v1

확정된 Direction 2절을 검사하는 형식이다. D2 타일/PPU, D3 공용 팔레트, D9 해안 모양, D10 수치를 정하지 않는다. 도구는 목록·PNG·배치된 타일을 읽고 오류를 반환한다. 파일을 변경하지 않으며 선택한 JSON 보고서만 쓴다.

## 파일과 실행

- 구현: `Tools/MapGen/asset_manifest.py`
- 명령: `Tools/Integration/validate_assets.py`
- 시험: `Tools/Integration/test_asset_manifest.py`
- 의존: Python 3.10 이상, 기존 프로젝트 의존인 Pillow (`Pillow>=10,<13`).

저장소 루트에서:

```sh
python3 Tools/Integration/validate_assets.py /absolute/path/catalogue.json --map /absolute/path/map.json
python3 Tools/Integration/validate_assets.py /absolute/path/catalogue.json --map /absolute/path/preview.json --preview
python3 -m unittest discover -s Tools/Integration -p 'test_asset_manifest.py'
```

CLI manifest/map 인수는 파일 경로다. 목록 안 이미지·원본·승인 근거 경로는 `--root` 기준이며 기본값은 저장소 루트다. 외부 절대 경로·저장소 밖 파일은 등록할 수 없다. 다른 검토 폴더를 쓸 때 `--root`로 지정한다. `--map`은 여러 번 사용할 수 있다. `--report`로 보고서 경로를 지정할 수 있으나 입력·참조 이미지·원본·승인 근거에 덮어쓸 수 없다.

종료값: 0 = 요청한 기술 검사 통과, 1 = 데이터/파일/배치 위반, 2 = 명령 인수 오류. 잘못된 JSON과 누락 입력은 1이다. `--map` 없이 실행하면 목록·이미지 검사만 하고 `placementsChecked: false`를 기록한다. 미술 승인·Unity·플레이 검증 완료를 뜻하지 않는다.

## 목록

아래는 **형식 설명용**이다. 실제 등록/승인 에셋이 아니고 파일·해상도·색·마을 배치를 제안하지 않는다. 이 예시를 입력하려면 참조 파일도 제공해야 한다.

```json
{
  "schemaVersion": 1,
  "profiles": {},
  "assets": [{
    "id": "example_tree",
    "kind": "object",
    "purpose": "tree",
    "size": [2, 1],
    "image": "Art/example_tree.png",
    "source": {"kind": "authored", "path": "Art/example_tree.aseprite"},
    "review": {"status": "pending"},
    "rules": {"onTerrain": ["grass"], "forbidRoad": true},
    "tiles": [
      {"dx": 0, "dy": 0, "layer": "obj", "tile": "example_tree_left", "image": "Export/example_tree_left.png"},
      {"dx": 1, "dy": 0, "layer": "high", "tile": "example_tree_right", "image": "Export/example_tree_right.png"}
    ]
  }]
}
```

- `schemaVersion`: 정수 1.
- `assets`: 비어 있지 않은 목록.
- `id`: 소문자 영문으로 시작하는 영문 소문자·숫자·밑줄. 중복 불가.
- `kind`: `object`(온전한 개체) 또는 `modular`(처음부터 조립용으로 제작한 세트).
- `purpose`: 용도 식별 문자열. 배치의 purpose와 일치해야 한다. 새 의미 판단은 Claude가 한다.
- `size`: 개체/조각의 전체 폭·높이(칸), 양의 정수. 투명 여백도 포함한다.
- `image`: 온전한 개체/조각의 내보낸 PNG. 타일 조각 파일과 따로 보존한다.
- `source`: `kind`는 `authored`, `ai_processed`, `external`. `path`에는 실제 보존 원본/출처 파일을 지정한다. 외부 팩은 `license` 설명도 필요하다. 원본 제작 형식의 미술 적합성을 자동 판정하지 않는다.
- `review.status`: `draft`, `pending`, `approved`, `changes_requested`. `approved`에는 존재하는 `evidence` 파일 경로가 필요하다. 실제 은샘 검토 기록을 사람이 등록한다. 파일 존재만으로 검토 발언의 진위를 자동 확인할 수는 없다.
- `rules`: 아래 명시적 배치 규칙. 정하지 않은 조건을 자동 추정하지 않는다.
- `tiles`: 전체 개체를 렌더링용으로 나눈 구성. 모든 (dx,dy)에 최소 한 타일이 있어야 하며 투명 타일도 빼지 않는다. layer는 ground/deco/obj/high. 하나의 (layer,dx,dy)는 중복 불가. `tile`은 최종 맵 팔레트의 타일 키, `image`는 실제 내보낸 타일 PNG.

온전한 PNG의 크기를 size로 나눠 렌더링 셀 크기를 구하고, 각 타일 PNG가 해당 위치의 픽셀과 같은지 검사한다. 크기가 나누어떨어지지 않거나 일부를 늘이거나 색을 바꾼 조각이면 실패한다. **원본 개체를 엔진 셀로 나누는 것은 허용된 렌더링**이며, 부분만 다른 물체로 재사용하는 것은 배치 누락/용도 검사에서 거절한다. 서로 다른 asset ID는 같은 타일 키를 공유할 수 없다. 같은 조립 세트 안에서는 공통 타일 키를 쓸 수 있다.

프로파일이 없어도 PNG 읽기·온전한 분할 일치·알파 0/255는 검사한다. D2/D3 값은 추정하지 않는다. 규격이 확정되면 선택 프로파일을 연결한다:

```json
{
  "profiles": {
    "approved_profile_id": {
      "pixelsPerCell": [16, 16],
      "palette": ["#123456", "#ABCDEF"],
      "binaryAlpha": true
    }
  }
}
```

위 16px·색 두 개는 문법 예시이며 채택한 값이 아니다. `asset.profile`에 이 프로파일 ID를 지정하면 전체 PNG의 칸당 크기와 보이는 픽셀의 팔레트 소속도 검사한다. PPU·피벗·임포트 필터·캐릭터 프레임은 Unity 쪽 인수 조건으로 남기며 이 Python 검사가 확인했다고 쓰지 않는다.

## 조립용 세트 (A2b 보완)

`modular`는 object의 size/image/tiles 대신 `moduleType`, `requiredPieces`, `pieces`를 갖는다. 각 pieces 값은 size/image/tiles를 가진다. source/review/purpose/rules/profile은 세트가 공유한다.

| moduleType | 최소 requiredPieces (pieces 안에도 실제 정의 필요) |
|---|---|
| water, coast, field, ground_boundary, cliff | center, edge, outer_corner, inner_corner |
| river, road, wall | 위 네 가지 + end |
| bridge (다리·나루) | start, center, end |

`moduleType` 생략·알 수 없는 종류는 실패한다. center=가운데, edge=가장자리, outer_corner=바깥 모서리, inner_corner=안쪽 모서리, start/end=시작/끝으로 ArtGuide 3절과 대응한다. 유형을 이름이나 PNG에서 추정하지 않는다.

예를 들어 물 세트는 `"moduleType": "water"`, `"requiredPieces": ["center", "edge", "outer_corner", "inner_corner"]`를 선언하고, pieces에도 네 이름을 모두 정의한다. 각 정의는 단일 개체와 같은 size/image/tiles 형식이다. 최소 이름을 목록에서 빼는 방법으로 검사를 우회할 수 없다. 강·길·성벽의 end 역시 뺄 수 없다.

위 목록은 최소 역할 검사다. 가장자리 4방향·각 모서리 4방향·강 폭/하구·문 등 ArtGuide의 전체 조각은 requiredPieces에 추가해 선언한다. 최소 네 이름만 있다고 A4 전체 구성이나 실제 이음매가 승인되는 것은 아니다. 세트/조각 이름과 제작 목적의 일치, 이음매는 Claude/은샘 검토와 시험 씬에서 확인한다. A2의 단순 straight/end 조립 예시는 이 보완 이후 더 이상 충분한 지형 세트가 아니다.

## 최종 맵 배치

기존 compact 맵 JSON의 w/h/palette/ground/deco/obj/high를 그대로 읽는다. 새 형식에 두 필드를 추가한다:

```json
{
  "assetPlacementVersion": 1,
  "assetPlacements": [
    {"id": "tree_001", "assetId": "example_tree", "x": 0, "y": 0, "purpose": "tree"}
  ]
}
```

- 좌표는 현재 JSON처럼 왼쪽 위부터 x 오른쪽/y 아래쪽, 정수 칸이다. 기존 게임의 지리 좌표·시장 ID·저장 구조와 무관한 추가 제작 메타데이터다.
- placement ID는 맵 안에서 유일하다. modular에는 `piece` 식별자를 반드시 적는다. object는 piece로 나눠 놓지 않는다.
- 인라인 scale/crop/tint/rotation 등 추가 필드는 실패한다. 크기/색 변형은 별도의 등록 asset ID로 만든다.
- 모든 개체의 전체 경계가 맵 안에 있어야 하고, 최종 타일 값·층·좌표가 목록과 전부 일치해야 한다.
- 사용한 모든 시각 타일에는 등록된 배치 소유자가 있어야 한다. 바닥·장식도 조립 조각/단일 개체로 기록한다. 빈 타일(팔레트 0)만 예외다.
- 같은 레이어의 한 칸을 둘 이상이 소유하면 실패한다. 바닥 위의 개체처럼 서로 다른 층의 겹침은 허용한다.
- `onTerrain`: 전체 개체 발자국의 태그가 허용 목록 중 하나여야 한다. map.terrainTags는 w*h 길이의 비어 있지 않은 문자열 배열이다.
- `adjacentTo`: 개체 발자국 바깥 상하좌우 칸 중 최소 한 곳이 지정 태그여야 한다. 물가 전용 등 명시된 조건을 검사한다.
- `separateFromSame`: 단일 개체끼리 같은 asset ID·같은 렌더링 층에서 최소 N칸의 빈 간격을 둔다. 0 이상 정수이며 생략/0은 제한 없음이다. 전체 발자국 사각형의 x/y 빈 간격 중 큰 값(Chebyshev 방식)을 쓴다. N=1이면 옆면·모서리 접촉 모두 거절하고 빈 한 칸부터 허용한다. 호수·연못·언덕·밭 개체에 필요한 값을 등록한다. 모여 자라는 나무 등은 규칙을 넣지 않는다. modular에는 이 규칙을 쓰지 않는다. 서로 다른 ID로 같은 개체를 우회 등록하는 의미 판단은 감사/미술 검토 범위다.
- `forbidRoad`: 전체 발자국에서 map.road 값이 모두 0이어야 한다. 필요한 terrainTags/road 정보가 없으면 검사 생략이 아니라 실패다.

한 개체를 의도와 다른 용도로 쓰면서 purpose만 거짓으로 적는 것까지 의미 추론으로 찾아내지는 않는다. 그림의 용도 정의·태그의 진실성은 원본 담당자와 검토자의 책임이고, B2 생성기는 원본 데이터에서 이를 일관되게 내보내야 한다.

## 기존 맵과의 연결 및 남은 검토

현재 본편 맵은 새 에셋 목록/배치 기록 도입 전이다. A2에서 기존 아트의 사용을 승인하거나 옛 STAMPS를 새 목록으로 일괄 인정하지 않는다. 이 CLI에 기존 맵을 넣으면 `placement_schema`로 실패한다. 기존 `validate_world.py`는 그 맵의 기존 연결 검사를 계속 담당한다.

A2는 검사 계약과 도구, 고의 오류 사례 테스트를 제공한다. A3a/b가 옛 에셋을 감사하고, A4/C 단계가 새 아트를 등록·승인받은 뒤, B2가 그 목록에 따라 타일과 배치 메타데이터를 함께 생성한다. 본편 생성기나 씬 재생성은 A2에서 하지 않았다.

미술 스타일·2.5등신·귀여움·시점·빛 방향, Unity 씬 외형·충돌·실제 이동·애니메이션 프레임은 이 검사로 통과 처리하지 않는다. Claude의 Direction 2절 규칙 교차 검토도 별도다.
