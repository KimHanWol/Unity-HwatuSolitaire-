# 화투 카드 그림

`Assets/Resources/Cards/hwatu_atlas.png` 는 258x420 칸을 8열 x 7줄로 붙인 그림이다.
칸 번호 = 월(0~11) * 4 + 그 달의 몇 번째 패(광/열끗/띠/피 순). 49번째 칸(번호 48)은 뒷면.

## 출처와 라이선스
- 48장 앞면: Wikimedia Commons [Category:SVG Hwatu](https://commons.wikimedia.org/wiki/Category:SVG_Hwatu)
  작성자 Spenĉjo (Louie Mantia, Jr. 의 Hanafuda 그림과 Marcus Richert 의 Hwatu 작업을 바탕으로 함),
  라이선스 [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/).
  원본 SVG 를 크기만 바꿔 PNG 로 렌더링했다. 이 PNG(아틀라스)도 CC BY-SA 4.0 을 따른다.
  게임 안 설정 → 만든 사람(크레딧)에 같은 내용을 표시한다.
- 뒷면 `back.svg`: 이 프로젝트에서 직접 그림.

## 다시 만드는 법
1. SVG 48장을 한 폴더에 받는다 (파일 이름 `Hwatu_<Month>_<Hikari|Tane|Tanzaku|Kasu|Kasu_1..3>.svg`, flipped 는 제외).
   커먼즈 API 로 받을 때는 요청 사이에 3초쯤 쉬어야 429(요청 과다)를 피한다.
2. `node Tools/Cards/atlas.js <svg 폴더> <폴더>/atlas.html 2.5`
3. 크롬 헤드리스로 찍는다:
   `chrome --headless=new --disable-gpu --hide-scrollbars --allow-file-access-from-files --default-background-color=00000000 --window-size=2064,2940 --screenshot=<폴더>/atlas.png file:///<폴더>/atlas.html`
4. 결과를 `Assets/Resources/Cards/hwatu_atlas.png` 로 복사한다.
