// 위키미디어 커먼즈의 화투 SVG 48장을 월 순서(1월~12월, 달마다 광/열끗/띠/피 순)로 8x6 격자에 놓은 HTML을 만든다.
// 사용: node atlas.js <svg 폴더> <출력 html> [배율]
// 49번째 칸(7째 줄 첫 칸)에는 이 폴더의 back.svg(화투 뒷면)를 넣는다.
// 그 HTML을 크롬 헤드리스로 찍어 Assets/Resources/Cards/hwatu_atlas.png 로 쓴다 (README.md 참고).
const fs = require('fs'), path = require('path');
const [, , dir, out, scaleArg] = process.argv;
const months = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
const order = ['Hikari', 'Tane', 'Tanzaku', 'Kasu', 'Kasu_1', 'Kasu_2', 'Kasu_3'];
const files = fs.readdirSync(dir).filter(f => f.endsWith('.svg') && !f.includes('flipped'));
// 칸 크기를 4의 배수로 맞춰야 아틀라스 전체가 GPU 압축(4x4 블록)에 맞는다.
const S = Number(scaleArg || 3), W = Math.round(103.2 * S / 2) * 2, H = Math.floor(168.2 * S / 4) * 4;
const cells = [];
for (const m of months) {
  const prefix = 'Hwatu_' + m + '_';
  const kind = f => order.indexOf(f.slice(prefix.length, -4));
  const mine = files.filter(f => f.startsWith(prefix)).sort((a, b) => kind(a) - kind(b));
  if (mine.length !== 4) throw new Error(m + ': ' + mine.join(','));
  cells.push(...mine);
}
fs.writeFileSync(path.join(path.dirname(out), 'order.txt'), cells.join('\n'));
let html = '<html><body style="margin:0;background:transparent"><div style="display:grid;grid-template-columns:repeat(8,' + W + 'px);grid-auto-rows:' + H + 'px">';
for (const f of cells) {
  const url = 'file:///' + path.resolve(dir, f).split(path.sep).join('/');
  html += '<img src="' + url + '" style="width:' + W + 'px;height:' + H + 'px;display:block">';
}
html += '<img src="file:///' + path.resolve(__dirname, 'back.svg').split(path.sep).join('/') + '" style="width:' + W + 'px;height:' + H + 'px;display:block">';
html += '</div></body></html>';
fs.writeFileSync(out, html);
console.log(cells.length + ' cards, cell ' + W + 'x' + H + ', page ' + (W * 8) + 'x' + (H * 7));
