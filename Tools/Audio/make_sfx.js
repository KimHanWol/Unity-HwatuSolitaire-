// 고스톱 앱 느낌의 효과음을 Kenney CC0 녹음으로 만든다 → Assets/Resources/Audio/*.ogg
// (Sfx.cs 는 Resources/Audio/<이름> 이 있으면 코드 합성 대신 그 클립을 쓴다.)
//
// 재료 (둘 다 CC0, https://kenney.nl):
//   Casino Audio  https://kenney.nl/assets/casino-audio   (card-place, card-slide, card-shuffle ...)
//   Impact Sounds https://kenney.nl/assets/impact-sounds  (impactPunch, impactWood ...)
// 사용: node Tools/Audio/make_sfx.js <casino 압축 푼 폴더> <impact 압축 푼 폴더>   (ffmpeg 필요)
//
// 짝: 플라스틱 화투패가 탁 부딪는 고음(card-place) + 손으로 내리치는 몸통(impactPunch) + 바닥 쿵(impactWood)을
//     각 소리의 "가장 큰 순간"에 정확히 맞춰 겹친다 (시작 순간으로 맞추면 최고점이 어긋나 "타닥"처럼 갈라진다).
//     그 뒤 짧게 빠르게 사라지게 다듬고 살짝 포화시켜 단단한 "짝!"으로 만든다.
const { execFileSync } = require('child_process');
const fs = require('fs'), path = require('path'), os = require('os');

const [, , CASINO, IMPACT] = process.argv;
if (!CASINO || !IMPACT) { console.error('usage: node make_sfx.js <casino dir> <impact dir>'); process.exit(1); }
const OUT = path.resolve(__dirname, '../../Assets/Resources/Audio');
fs.mkdirSync(OUT, { recursive: true });
const SR = 44100;

function find(dir, name) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) { const r = find(p, name); if (r) return r; }
    else if (e.name === name) return p;
  }
  return null;
}

/** ffmpeg 필터를 거쳐 모노 float 배열로 읽는다. */
function decode(file, filter) {
  const args = ['-hide_banner', '-loglevel', 'error', '-i', file];
  if (filter) args.push('-af', filter);
  args.push('-ac', '1', '-ar', String(SR), '-f', 'f32le', '-');
  const buf = execFileSync('ffmpeg', args, { maxBuffer: 1 << 28 });
  return new Float32Array(buf.buffer, buf.byteOffset, buf.length / 4);
}

/** 가장 큰 순간(절댓값 최대)이 pre초 뒤에 오도록 앞을 잘라 낸다. */
function alignPeak(x, pre) {
  let idx = 0, max = 0;
  for (let i = 0; i < x.length; i++) if (Math.abs(x[i]) > max) { max = Math.abs(x[i]); idx = i; }
  const start = Math.max(0, idx - Math.round(pre * SR));
  return x.slice(start);
}

function mix(layers, seconds) {
  const out = new Float32Array(Math.round(seconds * SR));
  for (const { data, gain, delay = 0 } of layers) {
    const d = Math.round(delay * SR);
    for (let i = 0; i < data.length && i + d < out.length; i++) out[i + d] += data[i] * gain;
  }
  return out;
}

/** 앞은 1ms에 걸쳐 열고, hold초 뒤부터 tau초 시간 상수로 빠르게 줄인 뒤 끝 10ms는 0으로. */
function envelope(x, hold, tau) {
  for (let i = 0; i < x.length; i++) {
    const t = i / SR;
    let g = Math.min(1, t / 0.001);
    if (t > hold) g *= Math.exp(-(t - hold) / tau);
    const tail = (x.length - i) / SR;
    if (tail < 0.01) g *= tail / 0.01;
    x[i] *= g;
  }
  return x;
}

/** 살짝 포화(부드러운 자르기)시켜 단단하게, 그리고 최고점을 peakDb에 맞춘다. */
function finish(x, drive, peakDb) {
  for (let i = 0; i < x.length; i++) x[i] = Math.tanh(x[i] * drive);
  let max = 0;
  for (const v of x) max = Math.max(max, Math.abs(v));
  const g = Math.pow(10, peakDb / 20) / (max || 1);
  for (let i = 0; i < x.length; i++) x[i] *= g;
  return x;
}

function write(name, x, quality = 6) {
  const tmp = path.join(os.tmpdir(), 'sfx_' + name + '.f32');
  fs.writeFileSync(tmp, Buffer.from(x.buffer, x.byteOffset, x.length * 4));
  execFileSync('ffmpeg', ['-hide_banner', '-loglevel', 'error', '-y', '-f', 'f32le', '-ar', String(SR), '-ac', '1', '-i', tmp,
    '-c:a', 'libvorbis', '-q:a', String(quality), path.join(OUT, name + '.ogg')]);
  fs.unlinkSync(tmp);
  console.log('wrote ' + name + '.ogg (' + (x.length / SR).toFixed(2) + 's)');
}

// ------------------------------------------------------------ 짝
function snap(name, crackFile, bodyFile, thumpFile, gains) {
  const crack = alignPeak(decode(find(CASINO, crackFile), 'highpass=f=1200'), 0.003);
  const body = alignPeak(decode(find(IMPACT, bodyFile), 'highpass=f=150,lowpass=f=5000'), 0.003);
  const thump = alignPeak(decode(find(IMPACT, thumpFile), 'lowpass=f=700'), 0.004);
  let x = mix([
    { data: crack, gain: gains[0] },
    { data: body, gain: gains[1] },
    { data: thump, gain: gains[2] },
  ], 0.22);
  x = envelope(x, 0.025, 0.045);
  write(name, finish(x, 1.6, -0.8));
  return x;
}
const s1 = snap('snap', 'card-place-3.ogg', 'impactPunch_medium_000.ogg', 'impactWood_light_000.ogg', [1.0, 0.85, 0.7]);
const s2 = snap('snap2', 'card-place-1.ogg', 'impactPunch_medium_001.ogg', 'impactWood_light_001.ogg', [1.0, 0.8, 0.7]);
const s3 = snap('snap3', 'card-place-4.ogg', 'impactPunch_medium_003.ogg', 'impactWood_light_002.ogg', [1.0, 0.85, 0.6]);

// ------------------------------------------------------------ 쌌다: 짝·짝·짝! 세 번, 마지막은 묵직하게
{
  const heavy = alignPeak(decode(find(IMPACT, 'impactPunch_heavy_000.ogg'), 'lowpass=f=2500'), 0.004);
  let x = mix([
    { data: s1, gain: 0.55, delay: 0.0 },
    { data: s2, gain: 0.7, delay: 0.12 },
    { data: s3, gain: 1.0, delay: 0.26 },
    { data: heavy, gain: 0.9, delay: 0.26 },
  ], 0.9);
  write('stack_slap', finish(envelope(x, 0.45, 0.15), 1.4, -0.8));
}

// ------------------------------------------------------------ 패 집기: 카드를 살짝 미는 소리 앞부분
{
  let x = alignPeak(decode(find(CASINO, 'card-slide-2.ogg'), 'highpass=f=700'), 0.01);
  x = x.slice(0, Math.round(0.13 * SR));
  write('select', finish(envelope(x, 0.04, 0.04), 1.0, -8), 5);
}

// ------------------------------------------------------------ 섞기 / 패 돌리기
{
  let x = decode(find(CASINO, 'card-shuffle.ogg'), 'silenceremove=start_periods=1:start_threshold=-45dB');
  x = x.slice(0, Math.round(0.9 * SR));
  write('shuffle', finish(envelope(x, 0.6, 0.15), 1.0, -4), 5);
}
