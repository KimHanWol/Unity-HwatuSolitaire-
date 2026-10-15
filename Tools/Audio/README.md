# 효과음

`Sfx.cs` 는 `Assets/Resources/Audio/<이름>.ogg` 가 있으면 코드 합성 대신 그 클립을 쓴다.

| 파일 | 쓰임 | 만든 방법 |
|---|---|---|
| snap, snap2, snap3 | 짝을 맞출 때 "쨕!" (셋 중 무작위) | 뺨 치는 소리에서 가장 밝은 한 대씩 잘라, 낮은 쿵을 빼고 고음·때리는 순간을 키워 아주 짧게 |
| stack_slap | 쌌다 (한 번) | 같은 뺨 소리 한 대를 짝보다 크고 조금 길게 (쿵 없이) |
| stamp, stamp2, stamp3 | 클리어 창에서 광이 하나씩 찍힐 때 | Kenney chips-stack (칩·동전 차르륵), 찍힐수록 조금씩 높게 |
| select | 패 고르기 | Kenney card-slide 앞부분 |
| shuffle | 섞기, 판 깔기 | Kenney card-shuffle |

나머지(폭탄, 클리어, 광, 배경음 등)는 지금도 코드 합성.

## 재료 (모두 CC0)
- 뺨 치는 소리: Freesound — 232928 bmusic92 "Smacking Face", 539163 khenshom "Slap hit on the face or shoulder".
  `https://freesound.org/s/<번호>/` 에서 받아 `<번호>.mp3|wav` 로 한 폴더에 둔다 (미리듣기 mp3 로도 충분).
- Kenney: [Casino Audio](https://kenney.nl/assets/casino-audio), [Impact Sounds](https://kenney.nl/assets/impact-sounds).

## 다시 만들기
`node Tools/Audio/make_sfx.js <casino 폴더> <impact 폴더> <뺨 소리 폴더>` (ffmpeg 필요)

고스톱 앱의 "짝!"은 박수·뺨 치는 소리처럼 높고 날카롭다. 여러 번 친 녹음이 많아서 `snap(이름, 번호, 시각초)`로 어느 한 대를 쓸지 고른다.
다른 소리를 겹치지 않는다 (섞인 소리로 들린다). 더 높게/낮게 하려면 `snap()` 안 `highshelf` 의 g(dB)를 바꾼다.
