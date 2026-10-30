# A7b·T1 검증 (2026-10-29)

검토 원본·previewOnly 13맵을 검사했다. 본편 맵/씬·WorldIndex·기존 게임 저장은 바꾸지 않았다. 본편 배치 승인과 통합 완료로 표시하지 않는다.

| 구분 | 실행 결과 |
|---|---|
| Python 전체 회귀 | 319개, 실패0·건너뜀16. `all-tests.txt`의 실제 실행 결과 기준 |
| 자연 연결·기존 M4 정적 검사 | 20개 통과. `nature-m4-tests.txt` |
| 기존 세계 등록 검사 | 오류0, 등록 출구1,398개. `world-validation.txt` |
| 검토 M4 합성 인증 | 승인50원본·1,492합성 칸, 오류0. PNG는 디코딩 RGBA로 비교 |
| 13맵 정적 확인 | ID/크기/원점 유지, 노면 등급 변화0, 모든 도착 칸 통행 가능·출구와 겹침0. 원래 기능물/기존 ID 보존 |
| Unity 컴파일 | 최종 갱신 뒤 콘솔 오류0 |
| 실제 Unity Play | 155개 확인 통과, `playCompleted=true`. 13맵 로딩과 직렬화/런타임 충돌 일치, 능선 실제 모터 충돌6곳·막힌 저장 좌표 복구, 논180틱 통행·플레이어 표시, 조정 출구6건 왕복, 지역 전환4회·도착 지역명 캡처 |
| 전환 선택 정책 | 양방향 지역 교차80조합·같은 지역89조합, 다른 지역 기존 선택 유지. `transition-policy.json` |
| 저장 | 별도 임시 JSON 쓰기/읽기/검증/실제 TryApplySave 적용. 돈·신용/맵·퀘스트 저장 자료 확인. 실제 저장 루트 파일6개 내용/목록 동일, `game-save-before/after.json` |
| 수동 검토 진입 도구 | 자동 러너 없이 실제 Play 입장, 입력 켬·시간1·previewOnly·자동저장 꺼짐 확인 후 종료/설정 원복. `manual-entry.json`, 수동 보행 결과는 아님 |
| Unity Edit 화면/구조 | 13맵 전후26장, Renderer 수 증가0. 자연 개체 GameObject 추가0, 경계15칸 보강으로 exit/spawn 객체30개 증가. 논 가림 컴포넌트만 장단/나루의 기존 맵 오브젝트에 추가. `scene-counts.json` |

`result.json`에 최종 Play 결과와 실제 게임 저장 불변 결과를 함께 남겼다. 원시 Play 기록은 `ArtReview/NatureBinding-20261029/Unity/play-checks.json`과 같다. `map-deltas.json`은 막힘 변화와 노면 불변을 맵별로 기록한다. 수원 물/바위와 숲 교체에 따른 막힘 변화이며 통행 전체가 불변이라는 뜻은 아니다.

큰 폭포 때문에 기존 출입구/도착 위치를 바꾸는 판단은 사용자에게 확인했다. “큰 폭포를 유지하고 주변 출입구·도착 위치 조정” 선택에 따라 원래 ID를 유지하고 가까운 경계로 옮겼다. 변경 목록 `ArtReview/NatureBinding-20261029/Maps/nature-exits.json`. 나머지 기존 출입구·도착 자료는 정확히 같음을 별도 정적 검사한다.

검사 중 발견·수정한 것: Editor 폴더 MonoBehaviour를 Play에 붙일 수 없어 테스트 러너를 Runtime 경로의 UNITY_EDITOR 전용 클래스로 이동했다. 전체 생성 후 남는 숲의 직사각형 그늘과 가림 분리, 논에서 캐릭터가 완전히 가려지는 문제, 능선 앞 여유를 수정하고 다시 생성/Play 검사했다. 새 export 승인 검사에서 Python 지역 import가 hashlib을 가리던 오류도 수정 후 재생성했다. 중간 실패를 통과로 처리하지 않았으며 최종 결과는 위 파일 기준이다.

검사 도구는 해당 worktree의 Build Settings를 잠시 검토 씬 경로로 바꾸고 종료 시 백업과 API로 복원했다. 최종 Build Settings 변경0. Unity가 자동 저장한 runInBackground/VersionControl 설정도 원복한다. 실제 게임 저장 검증은 루트 파일6개만 대상으로 한다. 최초 `save-before.json`에 포함된 Unity Analytics 캐시는 검사 과정에서 변하므로 그 전체가 불변이라고 주장하지 않는다.

현재 숲 Chunk Tilemap 방식과 Renderer 수가 유지된다는 구조 확인이다. 대상 기기 실제 FPS·프레임 상한 보장·atlas 최적화·빌드·사람이 직접 하는 플레이/실제 사용자 저장으로 이어 하기·새 농사/고도 기능은 검사하거나 구현하지 않았다. 새로운 배치와 전환 그림 승인 뒤 본편 생성과 통합 회귀가 남아 있다.
