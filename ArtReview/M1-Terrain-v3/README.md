# M1 지형 — 승인 2026-10-28

원본: `Tools/WorldLayout/terrain_v3.py`. 재현: `python Tools/WorldLayout/terrain_v3.py --roads --variant small-hamlets` (seed 고정).
승인 그림과 해시: `approval.json`. 승인 대상은 `*-small-hamlets.*`와 `lake-zoom.png`다. 나머지(`report.json`, `report-small.json`, `*-small.png`, 접미사 없는 그림, `compare-b2-v3.png`)는 검토 중 비교본이다.

- 맵 13개 면적: 고려 20,000칸, 중앙 동쪽 25,000칸(앞바다 포함).
- 마을 11곳: 고려 5곳, 중앙 동쪽 4곳(750~850칸), 쉼터 마을 2곳(북쪽 쉼터 마을, 남쪽 호숫가 마을, 각 약 320칸).
- 남쪽 큰 호수(약 720칸): 수원 산(호수 북쪽) → 강 → 호수 → 남동 포구 옆 바다. 남쪽 호숫가 마을이 서쪽 물가에 있다.
- 맵 경계: 마을과 길에서 정한다. 마을 둘레 10칸은 그 마을 맵에 넣는다.
- 길은 확인용이다. 길·다리·출입구는 M2에서 다시 다룬다. 남은 점: 동문 마을→중앙 길목 마을, 북동 포구→중앙 길목 마을 길이 경계를 3번 넘음. 호수로 들어오는 강 위에 맵 경계가 겹침.
