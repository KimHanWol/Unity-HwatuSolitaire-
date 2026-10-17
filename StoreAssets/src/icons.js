// 앱 아이콘 시안 여러 개를 찍는다: node StoreAssets/src/icons.js <화투 SVG 폴더> <출력 폴더>
// 글자 없이, 층층이 쌓인 패에서 같은 짝 두 장이 빛나는 "타일 매치 퍼즐" 느낌. 광(光) 패와 초록 담요는 피한다 (도박판 느낌).
const fs = require('fs'), path = require('path'), { execFileSync } = require('child_process');
const [, , SVG, OUT] = process.argv;
const url = p => 'file:///' + path.resolve(p).split(path.sep).join('/');
const card = n => url(path.join(SVG, 'Hwatu_' + n + '.svg'));
const CHROME = process.env.CHROME || 'C:/Program Files/Google/Chrome/Application/chrome.exe';
const AR = 168.2 / 103.2;
const base = `* { margin: 0; padding: 0; box-sizing: border-box; } html, body { width: 512px; height: 512px; overflow: hidden; }
body { position: relative; }
.card { position: absolute; border-radius: 7%; overflow: hidden; }
.card img { width: 100%; height: 100%; display: block; }
.dim::after { content: ''; position: absolute; inset: 0; background: rgba(40,20,10,.38); }
.glow {}
.spark { position: absolute; width: 60px; height: 60px; margin: -30px 0 0 -30px; }`;
const c = (n, x, y, w, rot = 0, cls = '', z = 1, shadow = '0 6px 14px rgba(0,0,0,.35)') =>
  `<div class="card ${cls}" style="left:${x - w / 2}px;top:${y - w * AR / 2}px;width:${w}px;height:${w * AR}px;transform:rotate(${rot}deg);z-index:${z};box-shadow:0 7px 0 #7A0A14,${cls.includes('glow') ? '0 0 0 6px #FFE38A, 0 0 34px 10px rgba(255,214,90,.9)' : shadow}"><img src="${card(n)}"></div>`;
// 네 갈래 반짝이 (끝이 뾰족한 별)
const spark = (x, y, s, z = 9) => `<svg class="spark" viewBox="-30 -30 60 60" style="left:${x}px;top:${y}px;transform:scale(${s});z-index:${z};filter:drop-shadow(0 0 6px #FFE38A)"><path d="M0-30 C3-6 6-3 30 0 C6 3 3 6 0 30 C-3 6-6 3-30 0 C-6-3-3-6 0-30Z" fill="#fff"/></svg>`;

const variants = {
  // A: 피라미드로 쌓인 패, 맨 위 두 장(벚꽃 짝)이 빛난다. 하늘색 배경
  A: { bg: 'radial-gradient(circle at 50% 35%, #8FD3F4 0%, #4A9FE0 60%, #2F6FB8 100%)', body: () => {
    let s = '', w = 128;
    for (let i = 0; i < 3; i++) s += c(['May_Kasu_1', 'February_Kasu_1', 'April_Kasu_1'][i], 112 + i * 144, 330, w, 0, 'dim', 1);
    for (let i = 0; i < 2; i++) s += c(['April_Tanzaku', 'May_Tanzaku'][i], 184 + i * 144, 290, w, 0, 'dim', 2);
    s += c('March_Kasu_1', 186, 236, 140, -8, 'glow', 3) + c('March_Kasu_2', 326, 236, 140, 8, 'glow', 4);
    s += spark(256, 120, 1.1) + spark(420, 150, 0.6) + spark(96, 160, 0.5);
    return s; } },
  // B: 짝 두 장이 가운데서 맞부딪치며 반짝, 따뜻한 산호색 배경 + 뒤에 흐린 패 더미
  B: { bg: 'radial-gradient(circle at 50% 45%, #FFD9A8 0%, #FF9F6B 55%, #E8664A 100%)', body: () => {
    let s = c('February_Kasu_1', 256, 300, 170, 0, 'dim', 1) + c('April_Kasu_1', 236, 280, 170, -4, 'dim', 1);
    s += c('March_Kasu_1', 196, 250, 180, -14, 'glow', 3) + c('March_Kasu_2', 316, 262, 180, 12, 'glow', 4);
    s += `<div style="position:absolute;left:256px;top:90px;width:0;height:0;z-index:8">${''}</div>`;
    s += spark(258, 92, 1.3) + spark(420, 140, 0.7) + spark(90, 150, 0.6) + spark(430, 420, 0.5);
    return s; } },
  // B2: B를 더 단순하게 — 뒤 패 없이 벚꽃 짝 두 장과 큰 반짝이 하나 (작게 보여도 또렷하게)
  B2: { bg: 'radial-gradient(circle at 50% 45%, #FFD9A8 0%, #FF9F6B 55%, #E8664A 100%)', body: () => {
    let s = c('March_Kasu_1', 200, 268, 190, -14, 'glow', 3) + c('March_Kasu_2', 318, 276, 190, 12, 'glow', 4);
    s += spark(262, 96, 1.4);
    return s; } },
  // D: 2층 — 짝 두 장 뒤로 아래층 패가 살짝만 보인다 (하늘색)
  D: { bg: 'radial-gradient(circle at 50% 35%, #8FD3F4 0%, #4A9FE0 60%, #2F6FB8 100%)', body: () => {
    // 아래층: 짝 두 장 바로 뒤에 같은 크기로 깔려 테두리와 아랫부분만 살짝 보인다
    let s = c('February_Kasu_1', 168, 300, 176, -2, 'dim', 1) + c('May_Kasu_1', 344, 300, 176, 2, 'dim', 1);
    // 위층: 빛나는 벚꽃 짝
    s += c('March_Kasu_1', 200, 252, 176, -10, 'glow', 3) + c('March_Kasu_2', 312, 252, 176, 10, 'glow', 4);
    s += spark(256, 92, 1.3);
    return s; } },
  // D2: D와 같고 산호색 배경
  D2: { bg: 'radial-gradient(circle at 50% 45%, #FFD9A8 0%, #FF9F6B 55%, #E8664A 100%)', body: () => {
    // 아래층: 짝 두 장 바로 뒤에 같은 크기로 깔려 테두리와 아랫부분만 살짝 보인다
    let s = c('February_Kasu_1', 168, 300, 176, -2, 'dim', 1) + c('May_Kasu_1', 344, 300, 176, 2, 'dim', 1);
    // 위층: 빛나는 벚꽃 짝
    s += c('March_Kasu_1', 200, 252, 176, -10, 'glow', 3) + c('March_Kasu_2', 312, 252, 176, 10, 'glow', 4);
    s += spark(256, 92, 1.3);
    return s; } },
  // C: 3층으로 비스듬히 쌓인 더미(위로 갈수록 밝게), 맨 위 벚꽃 한 쌍. 크림색 배경
  C: { bg: 'radial-gradient(circle at 50% 30%, #FFF8E6 0%, #F6E2B8 60%, #E2BF84 100%)', body: () => {
    let s = '', w = 116;
    const L0 = [[120, 360], [256, 360], [392, 360]], L1 = [[188, 318], [324, 318]];
    ['May_Kasu_1', 'February_Kasu_2', 'April_Kasu_2'].forEach((n, i) => s += c(n, L0[i][0], L0[i][1], w, 0, 'dim', 1));
    ['February_Tane', 'May_Tane'].forEach((n, i) => s += c(n, L1[i][0], L1[i][1], w, 0, 'dim', 2));
    s += c('March_Kasu_1', 196, 250, 136, -6, 'glow', 3) + c('March_Kasu_2', 318, 250, 136, 6, 'glow', 4);
    s += spark(257, 132, 1.1) + spark(410, 170, 0.55) + spark(104, 180, 0.5);
    return s; } },
};
fs.mkdirSync(OUT, { recursive: true });
function render(html, file, size, transparent) {
  const tmp = path.join(OUT, '_tmp.html');
  fs.writeFileSync(tmp, html);
  const args = ['--headless=new', '--disable-gpu', '--hide-scrollbars', '--allow-file-access-from-files', '--force-device-scale-factor=1',
    '--virtual-time-budget=3000', '--window-size=' + size + ',' + size, '--screenshot=' + path.resolve(OUT, file), url(tmp)];
  if (transparent) args.splice(1, 0, '--default-background-color=00000000');
  execFileSync(CHROME, args, { stdio: 'ignore' });
  fs.unlinkSync(tmp);
}
// 512 그림(body)을 size 크기 화면에 scale 배로 가운데 놓는다. radius: 모서리 둥글기(%), 50이면 원.
function page(v, size, { scale = 1, bg = true, art = true, radius = 0 } = {}) {
  const inner = `<div style="position:absolute;left:${(size - 512 * scale) / 2}px;top:${(size - 512 * scale) / 2}px;width:512px;height:512px;transform:scale(${scale});transform-origin:0 0">${art ? v.body() : ''}</div>`;
  return `<html><head><style>${base} html, body { width: ${size}px; height: ${size}px; background: transparent; }
  .shape { position: absolute; inset: 0; overflow: hidden; border-radius: ${radius}%; ${bg ? 'background:' + v.bg : ''} }</style></head>
  <body><div class="shape">${inner}</div></body></html>`;
}
for (const [k, v] of Object.entries(variants)) {
  // 스토어용: 정사각형 (Play 가 모서리를 둥글게 자르므로 그림은 86%로 줄여 잘리지 않게)
  render(page(v, 512, { scale: 0.86 }), 'icon_' + k + '.png', 512, false);
  // 미리 보기·레거시 런처용: 둥근 모서리 정사각형(앱 아이콘 모양)과 원형
  render(page(v, 512, { radius: 22, scale: 0.86 }), 'icon_' + k + '_rounded.png', 512, true);
  render(page(v, 512, { radius: 50, scale: 0.78 }), 'icon_' + k + '_round.png', 512, true);
  // 안드로이드 적응형 아이콘 레이어(432): 배경(그라데이션)과 전경(그림, 가운데 안전 영역 66% 안에 들게 줄임)
  render(page(v, 432, { art: false }), 'icon_' + k + '_adaptive_bg.png', 432, false);
  render(page(v, 432, { bg: false, scale: 0.66 }), 'icon_' + k + '_adaptive_fg.png', 432, true);
}
console.log('ok');
