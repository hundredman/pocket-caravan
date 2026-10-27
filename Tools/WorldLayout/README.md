# B1 세계 배치 원본

`world-layout.json`이 **승인된 배치안 D 해안선v2/seed7의 셀 단위 원본**이다. Claude의 `Docs/Decisions-2026-10-25.md` 1절 정정 면적을 따른다. 경계·모양을 바꾸지 않았다. 기존 본편11맵과 별도이며 이 데이터의13구역을 아직 본편 맵/씬으로 생성하지 않았다.

## 소비

```sh
python Tools/WorldLayout/world_layout.py --report /tmp/world-layout-validation.json
```

`WorldLayout.read()`는 표준 Python만으로 읽고 검사한다. 마을이나 지형 생성기는 승인안을 다시 계산하거나 근사 폴리곤을 다시 채우지 않고 이 원본을 읽는다. `owner_at(x,y)`는 소유 구역, `local_cell(key,x,y)`는 해당 구역 경계상자의 로컬 셀을 반환한다. 경계상자 안의 빈 칸을 소유 셀로 늘리지 않는다.

- 좌표: x동쪽/y남쪽, 정수는 셀 모서리·중심은+0.5. `bounds`는 `[x0,y0,x1,y1]`의 끝 미포함 범위.
- `maps[].runs`, `landRuns`: `[y,x시작,x끝]`, 양끝 포함. 마스크는 손실 없이 보존하며 원래 폴리곤의 근사 오차가 생기지 않는다.
- `maps`: 고려5 + 중앙동쪽8 =13개. map별 area/landArea/nearSeaArea와 정확한 셀을 담는다. 고려20000/동쪽25000·육지38253/앞바다6747이다.
- `landRuns`: 승인 계산의 전체 기준 육지다. 동쪽 소유 면적을 판정하는 **참조**이며 다른 팀 구역의 플레이 맵/씬/데이터를 만들지 않는다. 실제 담당 구역은 maps의45000셀만이다.
- `towns`, `roads`, `mainQuest`: 승인 마을9곳·도식 연결12개·메인 순서. 지리 key는 런타임 시장/NPC/시설/저장 ID를 바꾸는 이름이 아니다. `runtimeBinding`은 미결정이다.
- `rivers`, `mountains`: 원안의 도식 좌표. 강 검증 값은 원안의 표본점 검사이며, 선분 전체 수문학/실제 물길/통행 가능 길·다리/폭은 B2에서 별도 검증한다.
- `externalLinks`: D4/D7 합의 전 원안의 연결 방향. 런타임 연결로 활성화하지 않았다.
- `coastRecipe`, `provenance`: 기본 윤곽·거칠기·섬·평활화와 원안 코드 해시/환경. 실제 소비에서는 고정 셀이 기준이며 시작할 때 노이즈 계산을 다시 돌리지 않는다.

검사는 면적/소유 중복/누락·타운 육지 위치/도식 연결/메인 순서/수도와 중앙 경계 비접촉을 확인한다. 도식 연결 검사는 실제 플레이 경로를 검증한 것이 아니다.

## 승인 계산에서 다시 내보내기

내보내기에만 numpy/OpenCV가 필요하다. 별도 환경에 `requirements-export.txt`를 설치한 뒤:

```sh
python Tools/WorldLayout/export_world_layout.py
```

기존 `layout_proposal_d_coast_v2.py`와 `_terrain.py`를 임시 폴더에서 실행한다. 승인된 맵별 면적과 다르면 원본을 쓰기 전에 실패하고 재현 보고서만 남긴다. 면적을 맞추려고 경계를 재배분하지 않는다. 원안 스크립트와 과거 보고서는 검토 근거로 보존한다.

정적 검사·재실행 결과/한계는 `Docs/Validation/B1-Migration-2026-10-25/`를 본다. 다음 E1의 거리 단위·런타임 ID 연결 및 B2의 D5는 미결정이다. 본편 지도/경제/저장 연결은 승인된 답변 이후다.
