// 스토어 그래픽(앱 아이콘 512, 피처 그래픽 1024x500)을 HTML로 그려 크롬 헤드리스로 찍는다.
// 사용: node StoreAssets/src/make.js <화투 SVG 폴더>   (Tools/Cards/README.md 의 위키미디어 SVG)
// 결과: StoreAssets/icon-512.png, StoreAssets/feature-ko.png, StoreAssets/feature-en.png, Assets/Icon/AppIcon.png
const fs = require('fs'), path = require('path'), { execFileSync } = require('child_process');
const SVG = process.argv[2];
if (!SVG) { console.error('usage: node make.js <svg dir>'); process.exit(1); }
const ROOT = path.resolve(__dirname, '../..');
const url = p => 'file:///' + path.resolve(p).split(path.sep).join('/');
const card = n => url(path.join(SVG, 'Hwatu_' + n + '.svg'));
const font = url(path.join(ROOT, 'Assets/Resources/Fonts/Jua-Regular.ttf'));
const CHROME = process.env.CHROME || 'C:/Program Files/Google/Chrome/Application/chrome.exe';

const base = `
@font-face { font-family: Jua; src: url('${font}'); }
* { margin: 0; padding: 0; box-sizing: border-box; }
html, body { width: 100%; height: 100%; overflow: hidden; }
body { font-family: Jua, sans-serif; background: radial-gradient(ellipse at 50% 40%, #2E8C55 0%, #1E6E40 55%, #0F4A2A 100%); position: relative; }
.card { position: absolute; border-radius: 6%; overflow: hidden; box-shadow: 0 10px 24px rgba(0,0,0,.45); }
.card img { width: 100%; height: 100%; display: block; }
.stamp { position: absolute; display: flex; align-items: center; justify-content: center; border-radius: 50%;
  background: radial-gradient(circle at 40% 35%, #F0303F, #D01E2E 55%, #8A0C18); color: #FFF5DF;
  border: solid #FFF5DF; box-shadow: 0 8px 20px rgba(0,0,0,.45); text-shadow: 0 4px 0 #8A0C18; }
`;
function c(name, x, y, w, rot, z = 1) {
  const h = w * 168.2 / 103.2;
  return `<div class="card" style="left:${x - w / 2}px;top:${y - h / 2}px;width:${w}px;height:${h}px;transform:rotate(${rot}deg);z-index:${z}"><img src="${card(name)}"></div>`;
}

const icon = `<html><head><style>${base}</style></head><body>
${c('March_Tanzaku', 200, 226, 196, -14, 1)}
${c('March_Hikari', 290, 236, 196, 9, 2)}
<div class="stamp" style="left:262px;top:276px;width:170px;height:170px;border-width:9px;font-size:82px;z-index:3;transform:rotate(-10deg)">짝!</div>
</body></html>`;

function feature(title, sub, tagline) {
  return `<html><head><style>${base}
  .title { position: absolute; left: 64px; top: 120px; color: #FFF5DF; font-size: ${title.size}px; line-height: 1.05;
           text-shadow: 0 5px 0 #3A200E, 0 0 2px #3A200E; }
  .title b { color: #F5C54A; }
  .sub { position: absolute; left: 68px; top: ${120 + title.size * 1.15}px; color: #F5C54A; font-size: ${sub.size}px; text-shadow: 0 3px 0 #3A200E; }
  .tag { position: absolute; left: 68px; top: 360px; color: #FFF5DF; font-size: 30px; opacity: .92; text-shadow: 0 2px 0 #0F4A2A; }
  </style></head><body>
  <div class="title">${title.text}</div>
  <div class="sub">${sub.text}</div>
  <div class="tag">${tagline}</div>
  ${c('January_Hikari', 745, 245, 150, -16, 1)}
  ${c('August_Hikari', 830, 215, 150, -4, 2)}
  ${c('December_Hikari', 915, 237, 150, 10, 3)}
  ${c('March_Hikari', 805, 300, 150, 4, 4)}
  ${c('March_Tanzaku', 890, 318, 150, 15, 5)}
  <div class="stamp" style="left:880px;top:345px;width:116px;height:116px;border-width:7px;font-size:58px;z-index:6;transform:rotate(-10deg)">짝!</div>
  </body></html>`;
}

function shot(html, out, w, h) {
  const tmp = path.join(__dirname, '_tmp.html');
  fs.writeFileSync(tmp, html);
  execFileSync(CHROME, ['--headless=new', '--disable-gpu', '--hide-scrollbars', '--allow-file-access-from-files', '--force-device-scale-factor=1',
    '--virtual-time-budget=3000', '--window-size=' + w + ',' + h, '--screenshot=' + path.resolve(out), url(tmp)], { stdio: 'ignore' });
  fs.unlinkSync(tmp);
  console.log('wrote ' + out);
}

const OUT = path.join(ROOT, 'StoreAssets');
shot(icon, path.join(OUT, 'icon-512.png'), 512, 512);
fs.mkdirSync(path.join(ROOT, 'Assets/Icon'), { recursive: true });
shot(icon, path.join(ROOT, 'Assets/Icon/AppIcon.png'), 512, 512);
shot(feature({ text: '<b>짝!</b> 화투 사천성', size: 82 }, { text: 'Hwatu Solitaire', size: 40 }, '층층이 쌓인 화투, 같은 달끼리 짝!'), path.join(OUT, 'feature-ko.png'), 1024, 500);
shot(feature({ text: 'Hwatu<br>Solitaire', size: 88 }, { text: '', size: 1 }, 'Match Korean flower cards, month by month!'), path.join(OUT, 'feature-en.png'), 1024, 500);
