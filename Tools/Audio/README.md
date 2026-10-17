# 효과음

`Sfx.cs` 는 `Assets/Resources/Audio/<이름>.ogg` 가 있으면 코드 합성 대신 그 클립을 쓴다.

| 파일 | 쓰임 | 만든 방법 |
|---|---|---|
| snap, snap2, snap3 | 짝을 맞출 때 "쨕!" (셋 중 무작위) | 같은 뺨 소리 한 대(232928, 2.128초)로 음높이만 ±2% 다르게. 낮은 쿵을 빼고 고음·때리는 순간을 키워 아주 짧게 |
| stack_slap | 쌌다 (한 번) | 같은 뺨 소리 한 대를 짝보다 크고 조금 길게 (쿵 없이) |
| select | 패 고르기 | Kenney card-slide 앞부분 |
| shuffle | 섞기, 판 깔기 | Kenney card-shuffle |
| fail | 시간 초과 | Kenney jingles_PIZZI07 (반음씩 내려가는 가락) |
| revive | 이어하기 | Kenney confirmation_002 |

나머지는 코드 합성(Sfx.cs): 폭탄 "쾅"(BuildBomb: 떨어지는 쾅 + 터지는 잡음 + 파편 알갱이 + 짧은 잔향, 폰 스피커에서 들리게 쾅을 살짝 포화), 클리어(BuildClear), 버튼 "뽁"(BuildClick, 250→750Hz), 시간 경고 "띡"(BuildTick), 도화선(BuildFuse), 광(BuildSparkle), 황금 광(BuildGolden), 덮인 패 "톡"(BuildBlocked), 배경음.
폭탄·클리어·버튼·째깍은 Kenney 녹음도 써 봤지만 다른 소리와 결이 달라 합성으로 되돌렸다.

## 재료 (모두 CC0)
- 뺨 치는 소리: Freesound — 232928 bmusic92 "Smacking Face", 539163 khenshom "Slap hit on the face or shoulder".
  `https://freesound.org/s/<번호>/` 에서 받아 `<번호>.mp3|wav` 로 한 폴더에 둔다 (미리듣기 mp3 로도 충분).
- Kenney: [Casino Audio](https://kenney.nl/assets/casino-audio), [Impact Sounds](https://kenney.nl/assets/impact-sounds),
  [Interface Sounds](https://kenney.nl/assets/interface-sounds), [Music Jingles](https://kenney.nl/assets/music-jingles) (뒤 셋은 한 폴더 아래 풀어 둔다).

## 다시 만들기
`node Tools/Audio/make_sfx.js <casino 폴더> <impact 폴더> <뺨 소리 폴더> [Kenney 묶음 폴더]` (ffmpeg 필요, 넷째 폴더가 없으면 시간 초과·이어하기 소리는 건너뜀)

고스톱 앱의 "짝!"은 박수·뺨 치는 소리처럼 높고 날카롭다. 여러 번 친 녹음이 많아서 `snap(이름, 번호, 시각초)`로 어느 한 대를 쓸지 고른다.
다른 소리를 겹치지 않는다 (섞인 소리로 들린다). 더 높게/낮게 하려면 `snap()` 안 `highshelf` 의 g(dB)를 바꾼다.

## 소리 크기 맞춤
`Sfx.cs` 맨 위 `Play(…, 볼륨)` 값으로 맞춘다. 기준은 짝(snap, 볼륨 1)이고, 100ms 창 RMS × 볼륨으로 쟀을 때:
쌌다·폭탄은 짝과 비슷하거나 조금 크게(+0~3dB), 광·클리어·황금 광은 짝보다 조금 크게(+2~4dB), 실패·이어하기는 짝과 비슷하게,
패 고르기·버튼·째깍·덮인 패는 짝보다 10dB 안팎 작게, 배경음은 13dB 안팎 작게.
(합성 소리를 wav로 뽑아 재는 방법: Sfx.cs 를 UnityEngine 흉내 클래스와 함께 콘솔 프로그램으로 빌드해 Awake 뒤 클립 필드를 저장.)
