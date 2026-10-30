# 건물·배 교체와 산자락 숲 보강 (2026-10-29)

**건물·배는 본편 교체 완료. 숲을 보강한 산맥·논 배치는 검토용이며 최종 그림 확인 후 통합한다.**

사용자 “교체해줘. … 바위산 근처에 숲 … 비어있어서 어색해” 지시를 반영했다. 새 그림을 그리거나 승인 원본을 고치지 않았다.

| 구분 | 결과 |
|---|---|
| 승인10종 연결 | `building_bindings.py`가 승인 PNG 크기·디코딩 RGBA를 검사하고 원래 이름/전체 크기로 연결 |
| 본편 배치 교체 | 실제 존재하는 건물8종62채·배6척. 성문은 현재 M2 배치0이므로 새 원본 연결만 준비, 성문을 임의로 새로 놓지 않음 |
| 본편 생성 | 원본→M4 export `--production --preserve-objects`→13개 본편 씬 생성. 시장·NPC·기능물·ID·충돌·노면·출입구·도착 위치 포함 모든 비그래픽 JSON 필드 동일 |
| 산자락 숲 | 직사각형 전체 대신 바위 그림의 실제 불투명 윤곽+1칸 여유를 기준으로 나무를 걷어냄. 기존 나무·자연물97개를 더 보존하고 자르지 않음 |
| 검토 배치 | 기존 능선·큰 폭포/하류·논을 유지. 기존 출입구 조정6건, 새 경계 보강은 숲 복원 후10칸. 이 자료는 previewOnly이며 본편 지형은 유지 |

- [건물·배 전후](buildings-before-after.png): 동일 좌표의 원본 조립 비교, Unity로 표시하지 않음.
- [실제 Unity Play](game-review.png): 위 두 줄은 교체된 **본편**, 아래 줄은 **숲·산맥 검토안**. 모두480×270 원본.
- [숲 전후](forest-before-after.png): 이전 검토안 대비 빈 산자락을 줄인 모습.
- [본편13맵 전후](production-13-maps.png), [현재 본편/자연 검토13맵](review-13-maps.png): 실제 Unity Edit 카메라 전체보기.
- `ProductionUnity/`는 실제 본편 Play/전체보기, `Unity/`는 previewOnly Play/전체보기. 실제 사람의 수동 플레이와 구분한다.

## 지형·배치 추가 점검

산 뒤/양옆이 넓게 비는 것은 원래 숲을 넓은 사각형으로 제거한 결과였다. 윤곽에 맞춰 제거하고, 남긴3,898개 기존 자연 개체는 원래 렌더 칸 전체가 보존됨을 확인했다. 산·나무를 줄이거나 임의 조각으로 채우지 않았다. 길·물·건물·기능물 통행과13맵 도달을 검사했다. 새 길/해안 형상 변경이나 새 성문 배치는 하지 않았다. 호숫가 배 주변 연꽃은 실제 남쪽 호수에 놓인 것으로 확인했다.

별도 발견: 여러 마을/방향 이름표가 겹쳐 건물 지붕을 가리는 기존 UI 문제가 실제 Play 캡처에 보인다. 에셋 문제와 구분해 기록했으며 이번에 공용 UI 코드는 고치지 않았다. 화면 캡처의 이름표를 지우거나 보정하지 않았다.

## 재현과 통합 경계

```sh
M4_LAYERS="$PWD/ArtReview/BuildingNatureBinding-20261029/BuildingLayers" M4_REVIEW_ROOT="$PWD/ArtReview/BuildingNatureBinding-20261029/BuildingBase" python Tools/WorldLayout/m1_tiles_preview.py --zones
python Tools/WorldLayout/m4_export.py ArtReview/BuildingNatureBinding-20261029/BuildingLayers --production --preserve-objects

M4_LAYERS="$PWD/ArtReview/BuildingNatureBinding-20261029/Layers" M4_REVIEW_ROOT="$PWD/ArtReview/BuildingNatureBinding-20261029/Base" M4_NATURE=review python Tools/WorldLayout/m1_tiles_preview.py --zones
python Tools/WorldLayout/m4_export.py ArtReview/BuildingNatureBinding-20261029/Layers --preview-root Assets/PocketCaravan/Editor/B2Preview/A7 --review-root ArtReview/BuildingNatureBinding-20261029/Maps
```

본편 씬은 등록13개의 JSON을 `MapJsonBuilder.BuildSingleMap(json, entry.scenePath)`로 생성해 다른 문명/공용 등록 자료를 다시 만들지 않았다. 그림만 교체할 때 `--preserve-objects`를 계속 사용한다. 일반 M4 객체 재생성으로 통합 브랜치의 외부 연결 ID를 잃지 않도록 하는 옵션이다.

Unity 검토 출력은 SessionState `Codex.NatureIntegrationReview.Output`로 폴더를 지정한다. `NatureIntegrationReview.Build()`·`Run()`은 검토 씬 검사, `RunProduction()`은 previewOnly 세션에서 자동저장을 끈 뒤 실제 본편13씬을 검사한다. 본편 검사에서는 Build Settings를 교체하지 않는다. Play 종료 시 설정 백업을 복원한다.

검토 전용 원본/레이어는 재생성 대상으로 Git에서 제외했다. 원본 조립을 다시 생성한5층의 크기·RGBA가 동일함을 재확인했다. 검토판은 `Tools/Performance/building_nature_report.py`, 실제 전체보기는 `Tools/Performance/nature_capture.cs.txt`로 재현한다.

검증·한계: [검증 기록](../../Docs/Validation/Building-Nature-Binding-2026-10-29/README.md). 승인 동결본의 `productionApplied:false`는 동결 당시 기록이며 수정하지 않았다. 현재 연결 상태는 [building-binding.json](building-binding.json)이 기준이다. 산맥·논·숲의 live 스냅샷은 아직 만들지 않았다.
