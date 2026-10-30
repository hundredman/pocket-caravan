# B2 지형 생성기 — 지형 방향 승인·본편 미전환

사용자 지시: E1 후 B2 → R2 → C3a → C6. 처음 직선 중심 시안은 미승인. 사용자 피드백에 따라 자연스럽게 오가며 생긴 길로 수정했고, **“이번 길 방향 승인”**을 받았다. 승인 그림의 해시는 `approval.json`에 있다. 본편 교체 승인이 아니다.

- `Tools/MapGen/layout_d.py`: B1 원본 마스크에서 세계 좌표로 해안/강/산맥/길을 계산한 뒤 13개 소유 구역으로 나눈다. 담당 45,000칸(기준 육지38,253/앞바다6,747)과 마을9곳은 보존했다. 구역 경계상자의 빈 부분은 그리지 않고 통행을 막는다.
- 강3줄기: 승인된 경유점은 순서대로 유지, 산맥의 가까운 발원부로 상류를 잇고 바다 하구까지 4방향 연결을 확인한다. 기본 폭3칸이며 산자락/골짜기와 다리를 처리한다. 고도·유체 물리 시뮬레이션이 아니라 지형 생성과 흐름 연결 검사다.
- 길12연결: 지형 비용을 따르는 완만한 통행로와 기존 길 합류, 8방향 이동·코너 끼어들기 금지. 산지/바다를 피하고 강은 건너도록 한다. 해안·강 경유점·산맥·마을 위치를 길 모양에 맞춰 옮기지 않았다. 실제 생성 길의 모든 연결점과 길 폭 안의 지름길도 포함해 36쌍의 최단 보행 시간을 다시 구했다.
- `TownDistance`는 이 실제 보행 표가 있을 때 사용한다. 겹치는 길의 합류점을 무시하고 12개 중심선만 더하는 오류를 피한다. 최소제곱 환산을 다시 했으나 새 설정은 `layoutD-routes.json` 검토본에만 있다. **본편 Resources/TownRoutes는 current/B1 자료 그대로**이며 B5에서 최종 지형과 함께 옮긴다.
- `layout_d_art.py`: 기존 게임의 동결된 색 PNG가 있으면 그대로 재사용, 나머지는 Ninja Adventure의 원래 16px 조립 타일이다. 절벽은 전용 Relief 조각을 풀 바닥 위에 미리 굽는다. 동작 중 자르기·늘이기·색 보정은 없다. A2 카탈로그의 새 자산 승인은 pending으로 남겼다. 이번 승인은 지형 방향이며 일반 생산용 아트 전체를 포괄하지 않는다. D3 공통 팔레트를 임의 결정하지 않았다.

## 실행 결과

- A2 **기술 미리보기 오류0**, 모든 45,000개 ground 배치를 등록했다. 사용자가 지형 방향을 승인했어도 생산 모드의 자산 승인 요구를 임의 해제하지 않았다.
- Unity 6000.3.26f1 Edit mode: **13씬/45,000 ground 칸**, 패딩·충돌·16px/PPU16/Point·정상 시작점·미등록/previewOnly 확인. **169,411개 검사**, 구역 내부 길 **813구간/16,159 실제 PlayerMotor 틱** 통과. 페이드 경계 왕복과 수동 플레이는 아니다.
- 실제 480×270 Unity 카메라 그림 제공. 길 수정 비교용 합성 씬은 임시 Editor 폴더의 정적 모자이크이며 D5의 런타임을 한 씬으로 바꾸는 구현이 아니다. 그림에 인접 지형을 함께 보여 주기 위한 시험장이다.
- .NET 본편 C# 연결: 가격 oracle1680/견적6468/고정난수 의뢰2100은 current 이전 결과와 동일. 실제 보행 표 81방향 조합도 확인. `calibration.layoutD.json`에 모든 계산이 있다. Python143개 통과, 기존 등록 출구92개 오류0.
- `MapJsonBuilder.BuildSingleMap`에 격리된 Editor 검토용 타일 디렉터리와 previewOnly를 추가했다. 기존 등록/맵JSON/본편 씬/빌드 목록/현재ID/저장 형식은 변경하지 않았다. 게임 빌드를 하지 않았다.

## 다음 단계의 제한

- 건물·시설·NPC 재배치와 마을 광장은 B3/B4. 기존 시장·상품·퀘스트·NPC·시설 ID 및 승인된 작은 연못 원본을 보존한다. 옛 개경70×60의 연못 좌표를 새 지형에 그대로 적용하지 않는다.
- B5: 한 맵씩 로딩할 때 불규칙 경계 바깥의 이웃 지형이 검게 보이는 문제를 해소하고, 전체 경계 이동·지도·저장 위치 이주를 확인한다. 검토 모자이크를 실제 경계 이동 검사로 세지 않는다.
- B5 전환 검사 미완료. 무게4 일반 의뢰/난수1의 10쌍 예시 평균은 current 기한31.4 → layoutD27.8(-11.5%), 보수4.3 →5.2(+20.9%)다. **보수 예시는 ±15%를 벗어나므로 B5 통과라고 쓰지 않는다.** 실제 최종 의뢰의 비교 조건/수치 검토가 필요하며 보수 공식이나 환산 규칙을 임의 바꾸지 않았다. 실제 보행70%·Q1② 이익·기수락 약속 보존도 B5에서 다시 검사한다.

## 재현

```sh
python Tools/MapGen/layout_d_art.py
python Tools/MapGen/layout_d_review.py
dotnet run --project Tools/Routing/DistanceProbe -- . /tmp/b2-current.json ArtReview/B2-LayoutD/layoutD-routes.json
python -m unittest discover -s Tools/Integration -p 'test_*.py'
python Tools/Integration/validate_world.py
```

아트 재생성에는 기존 인접 Ninja Adventure 팩과 Pillow가 필요하다. Unity 별도 작업 폴더에서 `PocketCaravan.EditorTools.LayoutDTerrainChecks.RunBatch`. 검토 씬/가져온 타일은 `Assets/PocketCaravan/Editor/B2Preview`(ignored), 원본과 결과는 이 폴더다. `rejected-straight-*`는 처음 보여 준 미승인 시안 이력이다.
