# 승인 자연 에셋 적용 가능성 검사

[검토 결과와 조정한 적용 순서](../../NatureBinding.md)를 따른다. 본편/승인 그림/저장 수정 없음.

- `audit.json`: 산맥22+논3+숲6 지문·임시A2등록/4,668칸RGBA재합성·227접속·현재 맵 원본 지문·배치 공간 조사.
- `m4-tests.txt`: 현재 본편M4 관련 정적 테스트11개 통과.
- 재현: `python Tools/Performance/nature_readiness.py` (Pillow/NumPy/SciPy 필요). 임시 렌더 칸은 검사 뒤 삭제한다.
- 실제 Unity/Play/새 배치/새 그림/빌드 검사는 이번에 실행하지 않았다.
