# C7 시험 4개 검증 기록

2026-10-26, `codex/c7`. 시험4개는 사용자 승인 대기다. 기술 검사 통과를 그림 승인이나 본편 적용으로 취급하지 않는다.

## 재개 후 실행한 검사

- Python3.10.20/Pillow12.3.0에서 A5 12개+A2 32개+C7 4개, 합계48개 통과. [실행 결과](related-tests.txt).
- C7 4개 검사에는 시험 에셋4개가 모두pending인지, 경계47종이 있는지, 기존64×48/발밑이 유지되는지, 실제 Aseprite 원본51개 재출력의 크기·RGBA가 PNG와 일치하는지, 촬영 결과가480×270 시험장으로 표시되는지 포함한다. Aseprite 검사를 건너뛰지 않았다.
- 본편 등록 출구92개/오류0. [실행 결과](world.txt).
- 원본 PNG51개를 기존 `native-rgba.json`의 크기·디코딩 RGBA SHA256과 비교해 모두 동일함을 확인했다. 초기 하이픈 이름 중 목록에서 참조하지 않는 C7 렌더 조각59개만 제거했다. [목록](duplicate-render-cells.json). 사용 중인 조각84개, 승인 원본, 본편 데이터는 유지했다.
- 전체 통합 검사, 저장 회귀, 실제 보행, 수동 플레이, 게임 빌드는 이번 재개 작업에서 실행하지 않았다. 본편 PNG 호환 수정은 별도 `codex/b3` 브랜치에 보존돼 있다.

실행 명령:

```sh
PYTHONPATH=Tools/Integration /tmp/pocket-b3-python310/bin/python -m unittest test_art_prep test_asset_manifest test_c7_trial -v
/tmp/pocket-b3-python310/bin/python Tools/Integration/validate_world.py
```

## Unity 촬영 범위

중단 전 같은 날짜에 Unity6000.3.26f1에서 `C7TrialReview.Run`을 실행했다. 기존 개경 씬을 `previewOnly` 복제한 뒤 아트 시험장을 구성해 실제 Play 카메라로 전후480×270 두 장을 촬영했다. PPU16/Point, 같은 카메라·배치다. [촬영 완료 기록](../../../ArtReview/C7-Trial4-20261026/Screens-v2/complete.txt), [전후 그림](../../../ArtReview/C7-Trial4-20261026/Submission-v2/game-comparison.png).

실제 B3 마을 배치나 정상 보행 동작 검사가 아니다. 화면에 기존 플레이어·아낙·촌장·작은 연못22칸을 함께 배치했고 저장을 쓰지 않는 시험 세션을 사용했다. 프로세스 종료0/캡처 완료 확인. 기존 Unity SearchDatabase 인덱싱의 `ArgumentOutOfRangeException` 1회가 있어 Console 전체 오류0으로 기록하지 않는다.

재개 후 그림·촬영 도구가 바뀌지 않아 Unity를 다시 실행하지 않았다. 중단 전 촬영본을 그대로 검토 자료로 사용한다. `Screens-v1`은 잘못 선택한 비교 바닥의 초기 이력으로 제외하고, `Screens-v2`와 `Submission-v2`를 제출한다.

## 그림 판단과 남은 일

팀 기준표 PNG를 참고했다. 제출 그림의 팀 자료는 기준표 배율이며 Figma 원본 도트 배율은 확인하지 않았다. 고려 기존/시안은 같은3배 배율이다.

제작자 의견은 플레이어·사람 NPC·작은 연못 **수정 불필요**다. 캐릭터는 작은 화면에서 윤곽과 얼굴이 구분되고, 연못은 단순한 물색과 낮은 돌 경계가 새 시안에 이어진다. 사용자 승인 전에는 승인본과 승인 기록을 갱신하지 않는다.

다음은 시험4개 그림 승인이다. 승인되면 나머지 C7을 제작하고, B3~B5에는 승인 범위에 맞춰 연결한다. B3 닥목 배치와 미완료 개경 검토는 별도 브랜치의 후속 작업으로 남아 있다.
