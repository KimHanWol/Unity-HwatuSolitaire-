# 짝! 화투 솔리테어 (HwatuSolitaire)

화투패로 하는 상하이(마작 솔리테어) 방식 짝 맞추기 퍼즐. Unity 6 (6000.3.8f1), Android 우선, 세로 화면, 한국어/영어.
이름: 한국어 "짝! 화투 솔리테어", 영어 "Hwatu Solitaire". (처음엔 사천성 규칙이었다가 2026-10-15 상하이 방식으로 바꿈)

## 규칙 요약
- 패가 여러 층으로 쌓여 있다. **열린 패**(위가 덮이지 않고 왼쪽이나 오른쪽이 비어 있는 패) 두 장이 같은 **달**(1월 송학 ~ 12월 비)이면 "짝!" 하고 지운다.
  막힌 패는 살짝 어둡게 보이고, 누르면 흔들리며 이유(위가 덮임 / 양옆이 막힘)를 알려 준다.
- **쌌다**: 짝을 맞추면 두 장이 그 자리에 `stackWindow`초(2.0→1.2초, 금빛 고리) 동안 포개져 있다. 그 사이 같은 달 열린 패를 하나 더 누르면
  고스톱처럼 세 장이 뭉쳐 "쌌다!" → 뭉치가 아래 폭탄 칸으로 간다(최대 3, 살짝 벌린 부채 모양). 시간이 지나거나 다른 짝을 맞추면 두 장은 그냥 사라진다.
- **폭탄**: 폭탄 칸에 있는 달의 패를 판에서 하나 누르면(위만 드러나 있으면 옆이 막혀도 됨) 그 패 1장 + 가까운 드러난 짝(같은 달끼리) 2~5쌍이 한꺼번에 **펑**
  (`GameScreen.BlastTiles`, 남은 패 24장마다 1쌍 더). 짝 단위라 달마다 장수는 다시 짝수가 된다. 다른 폭탄이 걸린 달은 건드리지 않는다.
- 제한 시간 안에 다 지우면 클리어. 평가는 **광(光)** 표시: `star3`초 안 3광, `star2`초 안 2광, 그 밖 1광. 부활해서 깨면 1광.
- 시간 초과 → 보상형 광고로 이어하기(한 판 2번). 둘 곳이 없으면(쓸 수 있는 폭탄도 없으면) 자동으로 섞는다.
- 배치는 `Rules.Deal`이 "열린 두 자리에 짝을 넣고 비우기"를 거꾸로 되풀이해 만들어 **항상 끝까지 풀린다**. 자동 섞기도 같은 방법이라 섞은 판도 풀린다.

## 구조
- `Assets/Scripts/Core` (asmdef `HwatuSolitaire.Core`, UnityEngine 참조 금지): 자리·판(`Board.cs`: Tile, Layout, Board — 반 칸 단위 좌표와 층),
  판 모양(`Layouts`: 피라미드·계단·마름모·쌍봉·십자·테두리·탑), 짝·배치·섞기(`Rules`), 풀이기(`Solver`), 스테이지 생성(`StageGenerator`), 광 시간(`StarTimer`).
- `Assets/Scripts/Game`: 화면은 모두 코드로 조립 (씬에는 `App` 하나). `App` → `TitleScreen` / `StageSelectScreen` / `GameScreen`, 팝업 `Popup`, `Tips`(규칙 안내·설정·크레딧).
  `Art`(화투 아틀라스 + SDF 아이콘), `Ui`, `Anim`, `Fx`, `Sfx`(코드 합성 소리), `Ads`, `Loc`, `Settings`, `Progress`(PlayerPrefs).
- `Assets/Resources/Stages/stages.json`: 300스테이지 (자리 tiles + 패 cells, 도구로만 만든다). `Assets/Resources/AdConfig.json`: 광고 ID (docs/ADS.md).
- `Assets/Resources/Cards/hwatu_atlas.png`: 화투 48장 + 뒷면, 칸 318x516(투명 여백 4px — 기울이거나 줄여도 가장자리가 깨지지 않게) (Tools/Cards/README.md, CC BY-SA 4.0 — 크레딧 표시 필수).

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
