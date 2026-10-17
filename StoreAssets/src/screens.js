// 스토어 스크린샷: 게임 화면(Builds/shots, DevAutoShot 810x1440)에 위쪽 문구를 붙여 1080x1920 으로 만든다.
// 개발 빌드 표시("Development Build")가 있는 맨 아래 28px 은 잘라 낸다.
// 사용: node StoreAssets/src/screens.js   → StoreAssets/screenshots/ko/*.png, en/*.png
const fs = require('fs'), path = require('path'), { execFileSync } = require('child_process');
const ROOT = path.resolve(__dirname, '../..');
const url = p => 'file:///' + path.resolve(p).split(path.sep).join('/');
const CHROME = process.env.CHROME || 'C:/Program Files/Google/Chrome/Application/chrome.exe';
const font = url(path.join(ROOT, 'Assets/Resources/Fonts/Jua-Regular.ttf'));
const shots = path.join(ROOT, 'Builds/shots');
const list = [
  ['06_snap', '같은 달 두 장을 누르면 <b>짝!</b>', 'Tap two cards of the same month: <b>Snap!</b>'],
  ['08_stacked', '셋째 패까지 얹으면 <b>쌌다!</b> 뻑 폭탄', 'Add a third card for a <b>Ppeok bomb</b>'],
  ['18_stage300', '층층이 쌓인 패, <b>6층</b>까지', 'Stacked up to <b>6 layers</b> high'],
  ['16_stage130', '<b>300</b>개 스테이지, 12달 테마', '<b>300</b> stages, 12 monthly themes'],
  ['19_clear', '빨리 깰수록 <b>3광!</b>', 'Clear fast to earn <b>3 Gwang!</b>'],
  ['01_title', '화투로 즐기는 <b>짝 맞추기</b> 퍼즐', 'A matching puzzle with <b>Korean flower cards</b>'],
];
for (const lang of ['ko', 'en']) {
  const dir = path.join(ROOT, 'StoreAssets/screenshots', lang);
  fs.mkdirSync(dir, { recursive: true });
  list.forEach(([shot, ko, en], i) => {
    const html = `<html><head><style>
@font-face { font-family: Jua; src: url('${font}'); }
* { margin: 0; padding: 0; box-sizing: border-box; }
html, body { width: 1080px; height: 1920px; overflow: hidden; }
body { font-family: Jua, sans-serif; background: linear-gradient(#2E8C55, #0F4A2A); }
.cap { position: absolute; left: 40px; right: 40px; top: 70px; height: 200px; display: flex; align-items: center; justify-content: center;
  text-align: center; color: #FFF5DF; font-size: ${lang === 'ko' ? 76 : 64}px; line-height: 1.15; text-shadow: 0 5px 0 #3A200E; }
.cap b { color: #F5C54A; font-weight: normal; }
.frame { position: absolute; left: 110px; top: 340px; width: 860px; height: 1482px; border-radius: 44px; overflow: hidden;
  border: 10px solid #3A200E; box-shadow: 0 20px 50px rgba(0,0,0,.5); background: #1E6E40; }
.frame img { width: 840px; height: auto; display: block; }
</style></head><body>
<div class="cap"><span>${lang === 'ko' ? ko : en}</span></div>
<div class="frame"><img src="${url(path.join(shots, shot + '.png'))}"></div>
</body></html>`;
    const tmp = path.join(__dirname, '_tmp.html');
    fs.writeFileSync(tmp, html);
    const out = path.join(dir, (i + 1) + '_' + shot.replace(/^\d+_/, '') + '.png');
    execFileSync(CHROME, ['--headless=new', '--disable-gpu', '--hide-scrollbars', '--allow-file-access-from-files', '--force-device-scale-factor=1',
      '--virtual-time-budget=3000', '--window-size=1080,1920', '--screenshot=' + out, url(tmp)], { stdio: 'ignore' });
    fs.unlinkSync(tmp);
    console.log('wrote ' + path.relative(ROOT, out));
  });
}
