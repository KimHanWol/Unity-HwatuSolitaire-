// 고스톱 앱 느낌의 효과음을 CC0 녹음으로 만든다 → Assets/Resources/Audio/*.ogg
// (Sfx.cs 는 Resources/Audio/<이름> 이 있으면 코드 합성 대신 그 클립을 쓴다.)
//
// 재료 (둘 다 CC0, https://kenney.nl):
//   Casino Audio  https://kenney.nl/assets/casino-audio   (card-place, card-slide, card-shuffle ...)
//   Impact Sounds https://kenney.nl/assets/impact-sounds  (impactPunch ...)
//   박수·뺨 치는 소리 (Freesound, 모두 CC0 — 파일 이름은 소리 번호, 한 폴더에 둔다):
//     232928 bmusic92 "Smacking Face", 539163 khenshom "Slap hit on the face or shoulder", 478657 xUMR "clap.wav"
//     https://freesound.org/s/<번호>/ 에서 받는다 (미리듣기 mp3 로도 충분하다).
// 사용: node Tools/Audio/make_sfx.js <casino 폴더> <impact 폴더> <박수·뺨 소리 폴더>   (ffmpeg 필요)
//
// 짝: 고스톱 앱의 "짝!"은 박수·뺨 치는 소리처럼 높고 날카롭다. 녹음 가운데 가장 밝은(고음이 많은) 한 대만 잘라 쓰고
//     (다른 소리를 겹치면 섞인 소리로 들린다), 고음을 조금 더 올려 짧게 다듬는다.
//     어느 한 대를 고를지는 파일 안 시각(초)으로 정한다 — 여러 번 친 녹음이 많아서.
const { execFileSync } = require('child_process');
const fs = require('fs'), path = require('path'), os = require('os');

const [, , CASINO, IMPACT, CLAPS] = process.argv;
if (!CASINO || !IMPACT || !CLAPS) { console.error('usage: node make_sfx.js <casino dir> <impact dir> <clap dir>'); process.exit(1); }
function clap(id) {
  for (const ext of ['.wav', '.mp3', '.ogg', '.flac']) {
    const f = path.join(CLAPS, id + ext);
    if (fs.existsSync(f)) return f;
  }
  throw new Error('clap ' + id + ' not found in ' + CLAPS);
}
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
function decode(file, filter, start = 0, dur = 0) {
  const args = ['-hide_banner', '-loglevel', 'error', '-i', file];
  if (start > 0) args.push('-ss', String(start));
  if (dur > 0) args.push('-t', String(dur));
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

// ------------------------------------------------------------ 짝 (뺨·박수 한 대)
/** id 녹음의 at초 근처 한 대만 잘라, 최고점을 맨 앞에 맞추고 고음을 올려 짧게 다듬는다. */
function snap(name, id, at) {
  let x = decode(clap(id), 'highpass=f=350,highshelf=f=3000:g=4', Math.max(0, at - 0.05), 0.5);
  x = alignPeak(x, 0.003);
  x = x.slice(0, Math.round(0.24 * SR));
  x = envelope(x, 0.03, 0.06);
  write(name, finish(x, 1.0, -0.8));
  return x;
}
const s1 = snap('snap', '232928', 2.128);   // 가장 밝은 뺨 소리 (평균 6.2kHz)
const s2 = snap('snap2', '539163', 0.184);  // 손바닥으로 탁 (4.8kHz)
const s3 = snap('snap3', '232928', 5.098);  // 같은 녹음의 다른 한 대 (5.0kHz)

// ------------------------------------------------------------ 쌌다: 박수 짝·짝·짝! 세 번만 (다른 소리 없이), 마지막이 가장 크게
{
  let x = mix([
    { data: s1, gain: 0.6, delay: 0.0 },
    { data: s2, gain: 0.8, delay: 0.13 },
    { data: s3, gain: 1.0, delay: 0.27 },
  ], 0.6);
  write('stack_slap', finish(envelope(x, 0.4, 0.08), 1.0, -0.8));
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
