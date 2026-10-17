// 스토어 피처 그래픽(1024x500)을 앱 아이콘 시안 A와 같은 모양으로 그려 크롬 헤드리스로 찍는다.
// (하늘색 배경, 층층이 쌓인 패 위에서 벚꽃 짝 두 장이 빛난다. 광 패·초록 담요는 쓰지 않는다.)
// 사용: node StoreAssets/src/make.js <화투 SVG 폴더>   (Tools/Cards/README.md 의 위키미디어 SVG)
// 결과: StoreAssets/feature-ko.png, StoreAssets/feature-en.png
// 앱 아이콘은 icons.js (시안 D를 icon-512.png 와 Assets/Icon/AppIcon.png 로 복사해 쓴다)
const fs = require('fs'), path = require('path'), { execFileSync } = require('child_process');
const SVG = process.argv[2];
if (!SVG) { console.error('usage: node make.js <svg dir>'); process.exit(1); }
const ROOT = path.resolve(__dirname, '../..');
const url = p => 'file:///' + path.resolve(p).split(path.sep).join('/');
const card = n => url(path.join(SVG, 'Hwatu_' + n + '.svg'));
const font = url(path.join(ROOT, 'Assets/Resources/Fonts/Jua-Regular.ttf'));
const CHROME = process.env.CHROME || 'C:/Program Files/Google/Chrome/Application/chrome.exe';
const AR = 168.2 / 103.2;

const base = `
@font-face { font-family: Jua; src: url('${font}'); }
* { margin: 0; padding: 0; box-sizing: border-box; }
html, body { width: 1024px; height: 500px; overflow: hidden; }
body { font-family: Jua, sans-serif; position: relative;
  background: radial-gradient(ellipse at 72% 40%, #8FD3F4 0%, #4A9FE0 55%, #2F6FB8 100%); }
.card { position: absolute; border-radius: 7%; overflow: hidden; }
.card img { width: 100%; height: 100%; display: block; }
.dim::after { content: ''; position: absolute; inset: 0; background: rgba(40,20,10,.38); }
.spark { position: absolute; width: 60px; height: 60px; margin: -30px 0 0 -30px; }
.title { position: absolute; left: 64px; color: #FFFFFF; text-shadow: 0 5px 0 #1A4F91, 0 0 2px #1A4F91; line-height: 1.05; }
.title b { color: #FFE38A; font-weight: normal; }
.sub { position: absolute; left: 68px; color: #FFE38A; text-shadow: 0 3px 0 #1A4F91; }
.tag { position: absolute; left: 68px; top: 372px; color: #FFFFFF; font-size: 30px; text-shadow: 0 2px 0 #1A4F91; }
`;
const c = (n, x, y, w, rot = 0, cls = '', z = 1) =>
  `<div class="card ${cls}" style="left:${x - w / 2}px;top:${y - w * AR / 2}px;width:${w}px;height:${w * AR}px;transform:rotate(${rot}deg);z-index:${z};`
  + `box-shadow:0 7px 0 #7A0A14,${cls.includes('glow') ? '0 0 0 6px #FFE38A, 0 0 34px 10px rgba(255,214,90,.9)' : '0 6px 14px rgba(0,0,0,.35)'}"><img src="${card(n)}"></div>`;
const spark = (x, y, s, z = 9) => `<svg class="spark" viewBox="-30 -30 60 60" style="left:${x}px;top:${y}px;transform:scale(${s});z-index:${z};filter:drop-shadow(0 0 6px #FFE38A)"><path d="M0-30 C3-6 6-3 30 0 C6 3 3 6 0 30 C-3 6-6 3-30 0 C-6-3-3-6 0-30Z" fill="#fff"/></svg>`;

// 오른쪽: 아이콘 A의 피라미드 (아래 3장 + 2장, 맨 위 벚꽃 짝이 빛남)
function stack() {
  const ox = 790, w = 104;
  let s = '';
  ['May_Kasu_1', 'February_Kasu_1', 'April_Kasu_1'].forEach((n, i) => s += c(n, ox - 118 + i * 118, 330, w, 0, 'dim', 1));
  ['April_Tanzaku', 'May_Tanzaku'].forEach((n, i) => s += c(n, ox - 59 + i * 118, 296, w, 0, 'dim', 2));
  s += c('March_Kasu_1', ox - 58, 222, 116, -8, 'glow', 3) + c('March_Kasu_2', ox + 58, 222, 116, 8, 'glow', 4);
  s += spark(ox, 112, 1.0) + spark(ox + 150, 140, 0.55) + spark(ox - 160, 150, 0.5) + spark(ox + 175, 420, 0.4);
  return s;
}

function feature(titleHtml, titleSize, titleTop, subText, subTop, tagline) {
  return `<html><head><style>${base}</style></head><body>
  <div class="title" style="top:${titleTop}px;font-size:${titleSize}px">${titleHtml}</div>
  ${subText ? `<div class="sub" style="top:${subTop}px;font-size:40px">${subText}</div>` : ''}
  <div class="tag">${tagline}</div>
  ${stack()}
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
shot(feature('<b>짝!</b> 화투 사천성', 80, 128, 'Hwatu Solitaire', 228, '층층이 쌓인 화투, 같은 달끼리 짝!'), path.join(OUT, 'feature-ko.png'), 1024, 500);
shot(feature('Hwatu<br>Solitaire', 84, 96, 'Tile Match', 284, 'Match Korean flower cards!'), path.join(OUT, 'feature-en.png'), 1024, 500);
