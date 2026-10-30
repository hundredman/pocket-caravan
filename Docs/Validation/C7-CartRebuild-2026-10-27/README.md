# C7 수레 네이티브32 재제작 검사

2026-10-27. `rebuild-tests.txt` 4개, `previous-tests.txt` 4개 통과. 새 검사는 A5 원본/출력32×32·gridStep1, A2 미승인 거부와 전체4칸 재조합/누락 거부, Aseprite 실제 재출력RGBA, 우물·석등 보존, 실제Unity 촬영 전후 동일성을 확인한다. PNG 비교는 크기·디코딩RGBA 기준이다.

Unity6000.3.26f1 검토용 Play에서480×270 전후를 촬영했다. complete.txt 생성 확인. before는 앞 수정본trial과 완전히 같고 차이는 수레영역에 한정된다. 새 불투명 원본과 화면RGB오차≤1. 로그의 C# 컴파일 오류와 SearchDatabase ArgumentOutOfRangeException은0개. 프로세스 종료 코드는 세션 만료로 재확인하지 못해 기록하지 않는다.

실제 보행·수동 플레이·저장 회귀·전체 통합·게임 빌드는 실행하지 않았다. 형태 인지성과 그림 승인은 사용자 검토 대기다. 본편 적용 없음.
