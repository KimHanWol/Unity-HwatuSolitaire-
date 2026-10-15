# 효과음

`Sfx.cs` 는 `Assets/Resources/Audio/<이름>.ogg` 가 있으면 코드 합성 대신 그 클립을 쓴다.

| 파일 | 쓰임 | 만든 방법 |
|---|---|---|
| snap, snap2, snap3 | 짝을 맞출 때 "짝!" (셋 중 무작위) | 박수 녹음 둘을 최고점에 맞춰 겹침 (`make_sfx.js`) |
| stack_slap | 쌌다: 짝·짝·짝! | 위 짝 소리 셋 + 약한 펀치. 북·징(코드 합성)과 함께 울린다 |
| select | 패 고르기 | Kenney card-slide 앞부분 |
| shuffle | 섞기, 판 깔기 | Kenney card-shuffle |

나머지(폭탄, 클리어, 광, 배경음 등)는 지금도 코드 합성.

## 재료 (모두 CC0)
- 박수: Freesound — 561119 Sorinious_Genious "Clap-9", 463688 Landau00 "HandClap5", 478657 xUMR "clap.wav",
  789991 randbsoundbites "Single clap". `https://freesound.org/s/<번호>/` 에서 받아 `<번호>.mp3|wav` 로 한 폴더에 둔다.
- Kenney: [Casino Audio](https://kenney.nl/assets/casino-audio), [Impact Sounds](https://kenney.nl/assets/impact-sounds).

## 다시 만들기
`node Tools/Audio/make_sfx.js <casino 폴더> <impact 폴더> <박수 폴더>` (ffmpeg 필요)

고스톱 앱의 "짝!"은 박수에 가깝다. 다른 박수로 바꾸려면 `make_sfx.js` 의 `snap(...)` 줄에서 주 박수·보조 박수 번호와
보조 박수 크기, 밝기(2.2kHz 올림 dB)를 바꾼다.
