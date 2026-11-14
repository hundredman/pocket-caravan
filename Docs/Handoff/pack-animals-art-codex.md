# 짐말·낙타 그림 다시 그리기 (Codex 인계, 11/14 은샘)

## 할 일
짐말(`packhorse`)과 낙타(`arabia_camel`)의 그림을 **옆·앞·뒤 세 방향이 같은 동물로 보이게** 다시 그린다.
지금 그림은 방향마다 비율·짐 위치·안장 천이 따로 놀아 일관성이 떨어진다 (은샘 판단).
코드(따라오기·방향 고르기·걸음 프레임)는 그대로 쓰고, **그림 파일만 바꾸면 된다**.

현재 모습: `pack-animals-current-horse.png`, `pack-animals-current-camel.png` (이 폴더, 세로줄 = 옆·앞·뒤, 가로줄 = 짐 0~3단계).

## 파일 규칙 (코드가 이 이름으로 읽는다)
- 위치: `Assets/PocketCaravan/Resources/Transport/PackHorse/`, `.../Transport/Camel/`
- 이름: `{side|front|back}_{0..3}.png` + 걸음 프레임 `{view}_{stage}_a.png`, `{view}_{stage}_b.png` → 방향 3 × 짐 4 × 프레임 3 = 동물마다 36장
  - `side`: 왼쪽을 보는 옆모습 (오른쪽은 코드가 좌우 뒤집기)
  - `front`: 아래로 걸어올 때 (화면 쪽을 봄). 짐승은 사람 **위**에서 따라 내려온다
  - `back`: 위로 걸어갈 때 (등을 보임). 짐승은 사람 **아래**에서 따라 올라간다
  - 짐 단계 0~3: 실은 짐 / 적재 한도 비율 (0, 1/3 이하, 2/3 이하, 그 이상)
  - 걸음: 기본 → a → 기본 → b 순서로 1초에 8장. a·b는 다리 한 쌍씩 번갈아 든 모습
- 가져오기 설정: 기존 png의 `.meta`를 복사해 guid만 새로 (PPU 16, Point, 압축 없음, 피벗 가운데). 이미 있는 파일은 meta를 그대로 둔다
- 크기: 자유. 단 **발바닥이 그림 맨 아래에서 2px 위**여야 한다. 코드가 `높이/32 − 0.55`칸만큼 그림을 올려 발을 사람 발 높이에 맞춘다 (`Scripts/Player/PackAnimal.cs` Place). 앞모습만 0.4칸 더 내린다
- 1px = 1/16칸. 사람은 키 약 24px. 손수레 옆모습 32×30이 크기 기준

## 그림 기준 (은샘이 지금까지 정한 것)
- 화풍: 손수레·짐 썰매와 같은 굵은 어두운 외곽선(46,36,29), 3/4 내려다보기. 손수레 생성기 `Tools/ArtGen/handcart_v3.py` 색표 참고
- 짐말: 고려 과하마처럼 작고 다부진 갈색 말, 붉은 안장 천 + 나무 길마
- 낙타: 단봉낙타, 말보다 키가 크다. **파란 줄무늬 안장 천이 혹 꼭대기부터 덮어야 한다**
- 짐: 손수레와 같은 무명 자루·보따리·옥색 항아리. **세 방향 모두 같은 순서**: 1 양옆 자루 → 2 혹(등) 위 가운데 보따리 → 3 그 위 작은 보따리 + 양옆 항아리. 짐은 혹(말은 안장) 가운데에 맞춘다
- 앞·뒷모습 비율: 위에서 비스듬히 본 말은 폭이 옆 길이의 1/3쯤, 위아래로 길다. 참고한 그림: [LPC horses](https://opengameart.org/content/lpc-horses), [Horses](https://opengameart.org/content/horses) (비율만 참고, 그림은 직접)
- 받은 지적: 엉덩이 과장 금지, 머리는 외곽선으로 몸과 구분, 낙타 앞모습 목 외곽선은 위쪽 절반만, 양옆 자루 바깥 외곽선이 잘리지 않게(캔버스 여유), 앞모습에서 짐이 목을 가로질러 토막나 보이지 않게

## 지금 생성기 (버려도 됨)
`Tools/ArtGen/pack_animals_v1.py` — 글자 그림 + 부위별 테두리 겹치기. 새로 그릴 때 이 방식에 묶일 필요 없다.

## 확인
1. `python3 Tools/Integration/validate_world.py`, `python3 -m unittest discover -s Tools/Integration -p 'test_*.py'`
2. Unity에서 테스트 저장 칸으로 (`PocketCaravan.SaveSystem.UseSlot("claude_test_xxx.json")`, 서버 저장 고르기 창은 절대 누르지 않는다) 개경에서 짐말·낙타로 상하좌우 걸어 보기. 짐 0단계·3/4 둘 다
3. 은샘에게 세 방향 × 짐 단계 한 장짜리 그림 + 상하좌우 걷는 GIF로 보여 주기

## 관련 코드 (바꿀 필요 없음)
- `Scripts/Player/PackAnimal.cs`: 그림 고르기(방향 0.1초 유지, 걸음 프레임), 자리, 정렬
- `Scripts/Net/VehicleLook.cs`: `VehicleTrail`(지나온 길 따라오기, Heading), 온라인 다른 사람 짐승
- 수치: `Data/Goryeo/Vehicle_PackHorse.asset`(짐 50, 360냥), `Data/Arabia/Vehicle_ArabiaCamel.asset`(짐 50, 360냥)
