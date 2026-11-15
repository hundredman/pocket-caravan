# 도트 크기 검사 (칸당 픽셀)

기준: 칸당 16픽셀 (ArtGuide 2절). 실제 칸당 도트 = PPU ÷ 업스케일 배수. 읽기 전용 후보 목록이며 판정은 사람이 한다.


- 검사한 그림: 19912개 · 기준과 다른 그림: 434개 → **고칠 후보(맵 위) 0** · 코드가 PPU 16으로 다시 만듦 368 · 코드에서 안 씀 16 · 화면 UI·미리보기 50

## 1. 가져온 그림 (맵 위)

| 폴더 | 개수 | PPU | 업스케일 | 칸당 도트 | 그림 크기 예 |
|---|---|---|---|---|---|

## 2. 문제 없음 · 참고

| 폴더 | 개수 | PPU | 업스케일 | 이유 |
|---|---|---|---|---|
| `Resources/North/Minigame` | 339 | 100 | ×1 | 코드가 Sprite.Create(…, 16)으로 다시 만듦 → 문제 없음 |
| `MapModules/Previews` | 40 | 100 | ×1 | 화면 UI·미리보기 |
| `Resources/Goryeo/VillageLife/Cells` | 16 | 100 | ×1 | 코드·데이터에 경로가 안 보임 (안 쓰는 듯) |
| `Resources/North` | 12 | 100 | ×1 | 코드가 Sprite.Create(…, 16)으로 다시 만듦 → 문제 없음 |
| `Resources/North/Critters` | 11 | 100 | ×1 | 코드가 Sprite.Create(…, 16)으로 다시 만듦 → 문제 없음 |
| `Resources/WorldMap` | 6 | 100 | ×1 | 화면 UI·미리보기 |
| `Resources/WorldMap/Unified` | 5 | 100 | ×1 | 코드가 Sprite.Create(…, 16)으로 다시 만듦 → 문제 없음 |
| `Resources/StartScreen` | 4 | 100 | ×1 | 화면 UI·미리보기 |
| `Resources/Goryeo/Fishing` | 1 | 100 | ×1 | 코드가 Sprite.Create(…, 16)으로 다시 만듦 → 문제 없음 |

## 3. 코드에서 만든 스프라이트 (PPU ≠ 16)

그림 파일의 PPU 대신 이 값이 쓰인다. 16이 아니면 같은 그림도 다른 도트 크기로 보인다.

| 위치 | PPU | 코드 |
|---|---|---|
| `Scripts/MapModules/WeatherOverlay.cs:19` | `2` | `sprite=Sprite.Create(texture,new Rect(0,0,2,2),new Vector2(.5f,.5f),2);` |
| `Scripts/Obstacles/ObstacleField.cs:59` | `1` | `if(footprintSprite==null)footprintSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);` |
| `Scripts/Venice/VeniceQuestDirector.cs:206` | `1f` | `white = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);` |

## 4. 코드 배율 (localScale, 정수 아님·움직임)

먼지·물보라·숨쉬기 같은 효과와 애니메이션은 정상일 수 있다. 사람·동물·소품 몸에 주는 고정 배율만 고칠 후보.

| 위치 | 배율 |
|---|---|
| `Scripts/Arabia/ArabiaAmbience.cs:158` | `Random.Range(1f, 2f)` |
| `Scripts/Arabia/ArabiaAmbience.cs:280` | `(wind.x > 0 ? 1 : -1) * (0.8f + s.z), 1, 1` |
| `Scripts/Arabia/ArabiaAmbience.cs:352` | `(1f + (i % 3) * 0.5f)` |
| `Scripts/Arabia/ArabiaAmbience.cs:504` | `(u > 0.9f ? 2f : 1.2f)` |
| `Scripts/Arabia/ArabiaAmbience.cs:536` | `1.5f` |
| `Scripts/Arabia/ArabiaAmbience.cs:551` | `1.5f` |
| `Scripts/Arabia/ArabiaAmbience.cs:593` | `Mathf.Abs(Mathf.Sin(Time.time * 6f + s.max * 9f)) * 2f + 0.4f, 1f, 1f` |
| `Scripts/Arabia/ArabiaDunesLife.cs:82` | `4.5f` |
| `Scripts/Economy/Peddler.cs:209` | `1f, 1f + Mathf.Sin(Time.time * 2.4f) * .02f, 1f` |
| `Scripts/Net/RemotePlayers.cs:353` | `a.bodyScale.x * (sq < 0.99f ? 1.04f : 1f), a.bodyScale.y * sq, a.bodyScale.z` |
| `Scripts/North/NorthAtmosphere.cs:177` | `w * 1.3f / 8f * (1f + 0.05f * Mathf.Sin(t * 0.5f + i)), (1.6f + 0.4f * Mathf.Sin(t * 0.7f ` |
| `Scripts/North/NorthDaily.cs:267` | `Mathf.Lerp(1f, 0.2f, eaten)` |
| `Scripts/North/NorthDaily.cs:420` | `(1f + Mathf.Sin(Time.time * 4f + i) * 0.08f)` |
| `Scripts/North/NorthFireSeat.cs:144` | `1.04f, 0.86f, 1f` |
| `Scripts/North/NorthLife.cs:168` | `s, s * (1f + 0.1f * Mathf.Sin(t * 7f + phase)), 1f` |
| `Scripts/UI/AmbientFX.cs:48` | `Mathf.Abs(flip) < 0.3f ? 0.5f : 1f, 1f, 1f` |
| `Scripts/UI/AmbientFX.cs:88` | `Random.Range(1f, 1.6f)` |
| `Scripts/Venice/VeniceMaskSeller.cs:31` | `1f, 1f + Mathf.Abs(Mathf.Sin(Time.time * 5f)) * .05f, 1f` |
| `Scripts/Venice/VeniceQuestDirector.cs:212` | `px.x / 16f, px.y / 16f, 1f` |
| `Scripts/Venice/VeniceSeat.cs:71` | `restScale.x * 1.06f, restScale.y * .86f, restScale.z` |
| `Scripts/World/DroppedCargo.cs:44` | `(1f + Mathf.Floor(Mathf.Sin(t * 4f) * 1.5f + 1.5f) * 0.04f)` |
| `Scripts/World/LostTraveler.cs:164` | `1f + Mathf.Sin(Time.time * 6f) * .02f, 1f, 1f` |
| `Scripts/World/LostTraveler.cs:219` | `1f, 1f + Mathf.Abs(Mathf.Sin(Time.time * 8f)) * .04f, 1f` |
