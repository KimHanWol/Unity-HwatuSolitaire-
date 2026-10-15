# 효과음

`Sfx.cs` 는 `Assets/Resources/Audio/<이름>.ogg` 가 있으면 코드 합성 대신 그 클립을 쓴다.

| 파일 | 쓰임 | 만든 방법 |
|---|---|---|
| snap, snap2, snap3 | 짝을 맞출 때 "짝!" (셋 중 무작위) | Kenney 녹음 3개를 최고점에 맞춰 겹침 (`make_sfx.js`) |
| stack_slap | 쌌다: 짝·짝·짝! | 위 짝 소리 셋 + 묵직한 펀치. 북·징(코드 합성)과 함께 울린다 |
| select | 패 고르기 | card-slide 앞부분 |
| shuffle | 섞기, 판 깔기 | card-shuffle |

나머지(폭탄, 클리어, 광, 배경음 등)는 지금도 코드 합성.

## 다시 만들기
1. Kenney (CC0) 받기: [Casino Audio](https://kenney.nl/assets/casino-audio), [Impact Sounds](https://kenney.nl/assets/impact-sounds) — 압축을 각각 풀어 둔다.
2. `node Tools/Audio/make_sfx.js <casino 폴더> <impact 폴더>` (ffmpeg 필요)

짝 소리는 화투패가 탁 부딪는 고음(card-place) + 손으로 내리치는 몸통(impactPunch) + 바닥 쿵(impactWood)이다.
너무 가볍거나 무거우면 `make_sfx.js` 의 `snap(...)` 마지막 배열(세 소리 크기)을 바꾼다.
