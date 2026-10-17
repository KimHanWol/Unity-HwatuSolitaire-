# 광고 (AdMob)

## 어디에 뜨나
| 종류 | 자리 | 코드 |
|---|---|---|
| 하단 배너 | 모든 화면 맨 아래. 화면은 배너 높이만큼 위로 비켜 그린다 | `Ads.LoadBanner`, `App.ApplySafeArea` |
| 전면 | 스테이지를 깨고 **다음 스테이지**, 또는 결과·시간 초과 창의 **다시 하기**를 눌렀을 때 (빈도 조건은 같음) | `GameScreen.ShowResult`, `RetryWithAd` → `Ads.TryShowInterstitial` |
| 보상형(추가 시간) | 클리어 창에서 광을 다 못 땄을 때 "+N초 받고 다시 하기" → N초(3광 기준의 20%, 최소 10초)를 미리 받고 그 판을 다시 시작 | `GameScreen.ShowResult`, `App.Play(stage, bonus)` |
| 보상형(부활) | 시간 초과 창의 "+N초 이어하기" (한 판에 2번까지, N = 제한 시간의 30%, 최소 30초) | `GameScreen.TimeUp` |

전면 광고 빈도(`AdConfig.json`):
- `interstitialFromStage`: 이 스테이지를 깬 뒤부터 (기본 3 → 1·2탄은 광고 없음)
- `interstitialMinInterval`: 전면 광고 사이 최소 간격(초), 기본 60
- `quietAfterRewarded`: 보상형 광고를 본 뒤 이 시간(초) 동안 전면 광고 안 함, 기본 45

## ID 넣는 곳 (한 곳)
`Assets/Resources/AdConfig.json`
```json
{
  "useTestAds": false,
  "android": {
    "appId": "ca-app-pub-XXXXXXXX~YYYYYYYY",
    "banner": "ca-app-pub-XXXXXXXX/1111111111",
    "interstitial": "ca-app-pub-XXXXXXXX/2222222222",
    "rewardedTime": "ca-app-pub-XXXXXXXX/3333333333",
    "rewardedRevive": "ca-app-pub-XXXXXXXX/4444444444"
  }
}
```
- `appId`(물결표 `~`)는 에디터의 `AdsSetup`이 저장 즉시 Google Mobile Ads 설정으로 옮긴다 (메뉴 `Tools/Hwatu Solitaire/Sync AdMob App IDs`로도 가능).
- 광고 단위 ID(슬래시 `/`)가 비었거나 형식이 틀리면 그 자리는 구글 테스트 광고로 대체된다 (로그 경고).
- `useTestAds: true`이면 모든 자리에 구글 테스트 광고. **출시 전 false로.** 릴리스 AAB 빌드 때 true면 경고한다.
- 내 폰에서 실제 ID로 시험할 때는 `testDeviceIds`에 기기 ID(로그캣에 찍힘)를 넣는다.

## AdMob에서 만들 광고 단위
앱 1개 + 광고 단위 4개: 배너(적응형) 1, 전면 1, 보상형 2(추가 시간, 부활 — 따로 만들어야 어느 쪽이 얼마나 보였는지 나뉜다).

## 동작 메모
- 플러그인: `Packages/manifest.json`의 `com.google.ads.mobile`(OpenUPM). `Assets/csc.rsp`의 `-define:ADMOB`이 있어야 실제 광고 코드가 컴파일된다.
- 광고는 안드로이드·iOS·에디터에서만 켠다 (PC 확인용 빌드는 꺼짐, 개발 빌드에서는 보상형을 "본 것으로" 처리).
- 광고가 떠 있는 동안은 게임 시간이 멈추고 소리도 멈춘다 (`Ads.Showing`).
- 안드로이드 빌드는 반드시 활성 대상이 Android여야 한다 (배치는 `-buildTarget Android`). 아니면 앱 ID가 매니페스트에 안 들어가 실행 직후 죽는다.
