# 자연 에셋 연결·지역 이동 검토

2026-10-29. **검토용 13맵 연결과 검사 완료, 배치·전환 화면 승인 대기. 본편 맵은 그대로다.**

- [같은 좌표 원본 조립 전후](nature-before-after.png): 원본 그림 비교이며 Unity 캡처로 표시하지 않았다.
- [실제 게임 화면](game-review.png): Unity Play 480×270, 능선·논·큰 폭포 상하부·양방향 도착 지역명.
- [13맵 전체보기 전후](thirteen-maps.png): 실제 Unity 씬의 Edit 카메라 렌더. Play 화면과 구분한다.
- `Unity/`에 원본 프레임, `Maps/nature-exits.json`에 출입구·도착 위치 변경 6건.

큰 폭포를 유지하고 주변 출입구·도착 위치를 옮기는 안은 사용자 선택으로 확정했다. 이번 제출은 최종 배치 그림 확인이다. 기존 맵·기능물·출구/스폰 ID·노면 등급을 유지했다. 새로 열린 경계 15칸에 별도 ID를 추가했다. 능선은 송악의 짧은 대표 구간, 논은 장단 부근 두 구획이며 나루와 겹치는 그림 범위에도 가림을 연결했다. 22종 산맥을 전부 배치한 작업은 아니다.

그림은 승인 원본을 전체 크기로 배치한다. 스케일·회전·재색칠·부분 잘라 쓰기를 하지 않는다. 조립 뒤 16px 렌더 칸으로 나누고 M4 출처 인증을 한다. 숲은 원래 Chunk Tilemap 합성을 유지하며 교체 영역의 나무만 온전한 개체 단위로 제거한다. 논은 바닥 충돌을 추가하지 않고 플레이어 발 위치에 따라 앞줄만 Above로 옮긴다.

지역 이동은 기존 GameManager 이동 코루틴을 그대로 쓴다. 고려↔중앙 대륙은 도착 지역명을 크게, 마을명을 작게 표시한다. 같은 지역은 짧은 페이드이며 지역명 카드를 띄우지 않는다. 출발·도착 모두 담당 13맵인 경우만 연결해 다른 문명의 전환 등록을 보존한다.

## 재현

Python 환경: `Tools/WorldLayout/requirements-export.txt`. 로컬 검사에는 `/tmp/pocket-m1-python310/bin/python`을 사용했다.

```sh
M4_LAYERS="$PWD/ArtReview/NatureBinding-20261029/Layers" M4_REVIEW_ROOT="$PWD/ArtReview/NatureBinding-20261029/Base" M4_NATURE=review python Tools/WorldLayout/m1_tiles_preview.py --zones
python Tools/WorldLayout/m4_export.py ArtReview/NatureBinding-20261029/Layers --preview-root Assets/PocketCaravan/Editor/B2Preview/A7 --review-root ArtReview/NatureBinding-20261029/Maps
python -m unittest Tools.Integration.test_nature_binding Tools.Integration.test_m4_production
```

Unity에서 컴파일 후 `PocketCaravan.EditorTools.NatureIntegrationReview.Build()` → `Run()`. 이 도구는 검토 씬으로 시작해 실제 저장 적용·자동 저장을 끈다. 실제 이동 코루틴으로 검토 대상 씬에 도착하도록 **검사 중 이 worktree의 Build Settings 경로만 임시 대체**하고 Play 종료 후 API와 원본 파일 백업으로 원복한다. 실제 게임 저장 파일은 쓰지 않는다. 에디터가 비정상 종료되면 `Unity/EditorBuildSettings.before`로 작업 전 설정을 복원한다. 다른 Unity 프로젝트 설정은 변경 목적이 아니다.

직접 확인할 때는 Unity 메뉴 `Pocket Caravan → Review → 자연 배치 수동 검토 (저장 안 함)`을 쓴다. 입력/시간을 멈추는 자동 검사 러너 없이 13개 검토 씬으로 이동하며, Play 종료 때 설정을 원복한다. 입장 시 previewOnly=true·플레이어 입력 켬·timeScale=1·자동저장 꺼짐을 확인했다. 사람이 직접 걸어 본 결과는 아니다.

`Tools/Performance/nature_capture.cs.txt`를 Unity execute_code로 실행하면 전후 26장을 다시 만든다. `python Tools/Performance/nature_report.py`는 원본 그림/캡처를 변형하지 않고 검토판에 배치한다. 중간 Layers/Base와 미리보기 씬/타일은 재생성 대상으로 Git에서 제외했다.

본편 생성은 별도 승인 스냅샷 `Tools/WorldLayout/nature_live.json`이 있을 때만 활성화된다. 현재는 만들지 않았다. 검토 원본 `nature_layout.json`으로 본편 export를 시도하면 거절한다. 승인 뒤 스냅샷을 만들고 생성→export→본편 씬 재생성→회귀 검사를 이어간다.

검사 결과와 한계는 [검증 기록](../../Docs/Validation/Nature-Integration-2026-10-29/README.md)에 있다. 실제 사람의 수동 플레이·대상 기기 FPS·빌드 검사는 하지 않았다.
