// 위키미디어 커먼즈의 화투 SVG 48장을 월 순서(1월~12월, 달마다 광/열끗/띠/피 순)로 8열 격자에 놓은 HTML을 만든다.
// 사용: node atlas.js <svg 폴더> <출력 html> [배율=3] [여백=4]
// 49번째 칸(7째 줄 첫 칸)에는 이 폴더의 back.svg(화투 뒷면)를 넣는다.
// 그 HTML을 크롬 헤드리스로 찍어 Assets/Resources/Cards/hwatu_atlas.png 로 쓴다 (README.md 참고).
//
// 칸마다 둘레에 투명 여백을 둔다. 여백이 없으면 패를 기울이거나(부채, 날아가는 연출) 작게 줄일 때
// 패 가장자리가 그림 끝에서 잘려 계단처럼 깨져 보인다. 여백이 있으면 텍스처 보간이 가장자리를 부드럽게 해 준다.
const fs = require('fs'), path = require('path');
const [, , dir, out, scaleArg, padArg] = process.argv;
const months = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
const order = ['Hikari', 'Tane', 'Tanzaku', 'Kasu', 'Kasu_1', 'Kasu_2', 'Kasu_3'];
const files = fs.readdirSync(dir).filter(f => f.endsWith('.svg') && !f.includes('flipped'));
const S = Number(scaleArg || 3), P = Number(padArg || 4);
// 패 그림 크기(원본 비율 그대로)
const IW = Math.round(103.2 * S), IH = Math.round(168.2 * S);
// 칸 크기: 여백을 더하고, 아틀라스 전체가 GPU 압축 블록(4x4)에 맞게 4의 배수로 (8열·7줄이라 가로는 짝수면 충분)
const W = Math.ceil((IW + 2 * P) / 2) * 2, H = Math.ceil((IH + 2 * P) / 4) * 4;
const cells = [];
for (const m of months) {
  const prefix = 'Hwatu_' + m + '_';
  const kind = f => order.indexOf(f.slice(prefix.length, -4));
  const mine = files.filter(f => f.startsWith(prefix)).sort((a, b) => kind(a) - kind(b));
  if (mine.length !== 4) throw new Error(m + ': ' + mine.join(','));
  cells.push(...mine);
}
fs.writeFileSync(path.join(path.dirname(out), 'order.txt'), cells.join('\n'));
const cell = url => '<div style="width:' + W + 'px;height:' + H + 'px;display:flex;align-items:center;justify-content:center">'
  + '<img src="' + url + '" style="width:' + IW + 'px;height:' + IH + 'px;display:block"></div>';
let html = '<html><body style="margin:0;background:transparent"><div style="display:grid;grid-template-columns:repeat(8,' + W + 'px);grid-auto-rows:' + H + 'px">';
for (const f of cells) html += cell('file:///' + path.resolve(dir, f).split(path.sep).join('/'));
html += cell('file:///' + path.resolve(__dirname, 'back.svg').split(path.sep).join('/'));
html += '</div></body></html>';
fs.writeFileSync(out, html);
console.log(cells.length + ' cards, card ' + IW + 'x' + IH + ', cell ' + W + 'x' + H + ', page ' + (W * 8) + 'x' + (H * 7));
