# 짝! 화투 사천성 (HwatuSolitaire)

화투패로 하는 사천성(Shisen-Sho) 퍼즐. Unity 6 (6000.3.8f1), Android 우선, 세로 화면, 한국어/영어.

## 규칙 요약
- 같은 **달**(1월 송학 ~ 12월 비) 화투 두 장을 꺾임 2번 이하의 선(빈칸·판 바깥 테두리만 지남)으로 이으면 "짝!" 하고 사라진다.
- 짝을 맞춘 뒤 `comboWindow`초(4.0→3.0초) 안에 또 맞추면 연속. **3연속마다 "쌌다!"** → 그 달이 폭탄이 되어 아래 칸에 모인다(최대 3).
  폭탄 달의 패를 아무거나 하나 누르면 그 달 패가 한꺼번에 **펑** 터진다(선 연결 불필요). 폭탄 칸이 차 있으면 대신 +5초.
- 제한 시간 안에 다 지우면 클리어. 별: `star3`초 안 ★★★, `star2`초 안 ★★, 그 밖 ★. 부활해서 깨면 ★.
- 시간 초과 → 보상형 광고로 이어하기(한 판 2번). 둘 곳이 없으면(폭탄도 없으면) 자동으로 섞는다.
- 중력(3장부터): 짝을 지우면 남은 패가 정해진 방향으로 쏠린다. 엎어진 패(5장부터): 없앨 수 없고 길을 막는다.

## 구조
- `Assets/Scripts/Core` (asmdef `HwatuSolitaire.Core`, UnityEngine 참조 금지): 판, 경로(`PathFinder`), 중력, 규칙·섞기(`Rules`), 풀이기(`Solver`), 스테이지 생성(`StageGenerator`), 별 시간(`StarTimer`).
- `Assets/Scripts/Game`: 화면은 모두 코드로 조립 (씬에는 `App` 하나). `App` → `TitleScreen` / `StageSelectScreen` / `GameScreen`, 팝업 `Popup`, `Tips`(규칙 안내·설정·크레딧).
  `Art`(화투 아틀라스 + SDF 아이콘), `Ui`, `Anim`, `Fx`, `Sfx`(코드 합성 소리), `Ads`, `Loc`, `Settings`, `Progress`(PlayerPrefs).
- `Assets/Resources/Stages/stages.json`: 300스테이지 (도구로만 만든다). `Assets/Resources/AdConfig.json`: 광고 ID (docs/ADS.md).
- `Assets/Resources/Cards/hwatu_atlas.png`: 화투 48장 + 뒷면 (Tools/Cards/README.md, CC BY-SA 4.0 — 크레딧 표시 필수).

## 명령
```bash
dotnet test Tools/CoreTests                                   # 코어 테스트
dotnet run -c Release --project Tools/StageTool -- build      # stages.json + Tools/StageTool/report.tsv
dotnet run -c Release --project Tools/StageTool -- validate   # 모든 판 풀리는지 확인
Unity -batchmode -quit -projectPath . -buildTarget Win64 -executeMethod ProjectSetup.BuildWindows
Builds/Windows/HwatuSolitaire.exe -screen-width 540 -screen-height 1170 -screen-fullscreen 0 -autoshot Builds/shots   # 주요 화면 스크린샷
Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod ProjectSetup.BuildTestApk
Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod ProjectSetup.BuildReleaseAab   # Keystore/keystore.env 필요
```

## 작업 원칙
- 답변·문서·커밋 메시지는 한국어. 커밋 메시지: 첫 줄 주제, 빈 줄, `- 세부 사항` 목록.
- 규칙·생성기를 고치면 코어 테스트 → StageTool build → validate. 스테이지 JSON을 손으로 고치지 않는다.
- 난이도 손잡이는 `StageGenerator` 맨 위 주석, 별 시간 모형은 `StarTimer` 주석에 있다. 실제로 해 보고 너무 빡빡/느슨하면 `StarTimer`의 배수만 바꾸고 build.
- C# 9 까지 (Unity 6).

## 남은 일
- [ ] AdMob 앱/광고 단위 만들고 `AdConfig.json` 채우기, `useTestAds: false`
- [ ] 앱 아이콘(`Assets/Icon/AppIcon.png`), 스토어 이미지, 개인정보처리방침
- [ ] 실기기에서 별 시간 체감 조정 (지금은 모의 풀이 모형 기준)
- [ ] 소리 체감 확인 (짝·쌌다·폭탄은 코드 합성. 녹음으로 바꾸려면 `Resources/Audio/<이름>` 에 클립을 두면 우선 사용)
