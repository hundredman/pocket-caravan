// 시험용 가짜 플레이어. 다른 접속자 한 명을 골라 같은 맵에 들어가, 그 사람 둘레를 원을 그리며 걷는다.
// 혼자서도 "다른 사람이 걸어 다니는 모습"을 시험할 수 있게 한다. 벽은 무시하고 걷는다(시험용).
// 사용: node test/bot.mjs [주소] [이름] [초] [출신] [--emote] [--trade] [--say]
//   예) node test/bot.mjs ws://127.0.0.1:8787/ws 시험순록 120 viking --emote --trade
//   --emote  4초마다 멈춰 서서 리액션 (인사·깡총·박수·춤·하트·빙글·낮잠 차례로)
//   --trade  8초마다 리알토 장터에서 유리그릇 3개 팔기 / 2개 사기를 번갈아 (공유 장터 시험)
//   --say    10초마다 채팅 한 줄
//   --gift   (2026-11-08) 따라간 사람에게 100냥, 이어서 유리그릇 2개를 선물한다 (--gift-after=<초>, 기본 6초 뒤). 받은 선물은 저절로 받고 고맙다고 한다
//   --score --pass=<접속키>   계정으로 들어와 곤돌라 경주 점수 42점을 순위표에 올린다 (시험 뒤 관리 도구로 계정을 지운다)
const ARGS = process.argv.slice(2).filter((a) => !a.startsWith("--"));
const FLAGS = new Set(process.argv.slice(2).filter((a) => a.startsWith("--")));
const GIFT_AFTER = Number(process.argv.slice(2).find((a) => a.startsWith("--gift-after="))?.slice("--gift-after=".length) ?? 6) * 1000;
const PASS = process.argv.slice(2).find((a) => a.startsWith("--pass="))?.slice("--pass=".length);
const URL = ARGS[0] ?? "ws://127.0.0.1:8787/ws";
const NAME = ARGS[1] ?? "시험고양이";
const SECONDS = Number(ARGS[2] ?? 120);

const ws = new WebSocket(URL);
const send = (m) => ws.send(JSON.stringify(m));
let waitingSaid = false, gifted = false;
let me = "", target = null, targetName = "", map = "", center = { x: 0, y: 0 }, angle = 0;

const LOOK = ARGS[3] ?? "viking";   // 생김새(출신): venice · goryeo · viking · arabia
ws.onopen = () => send({ t: "hello", v: 1, name: NAME, look: LOOK, ...(PASS ? { pass: PASS } : {}) });
ws.onerror = () => { console.log(`접속 실패: ${URL}`); process.exit(1); };
ws.onclose = (e) => { console.log(`연결 끊김 (${e.code})`); process.exit(0); };
ws.onmessage = (e) => {
  const m = JSON.parse(e.data);
  if (m.t === "welcome") {
    me = m.id;
    console.log(`접속: ${me} (${m.account ? "계정" : "손님"}, 이름 ${m.name})`);
    send({ t: "who" });
    if (FLAGS.has("--score")) {
      if (!m.account) console.log("--score는 --pass=<접속키>로 계정으로 들어와야 해요");
      else setTimeout(() => send({ t: "score", board: "gondola", value: 42 }), 1500);
    }
  }
  if (m.t === "error") console.log(`서버: ${m.why}`);
  if (m.t === "scored") console.log(`순위표: ${m.board} ${m.best}점 (${m.rank}위)`);
  if (m.t === "giftResult") console.log(`선물 ${m.ok ? "전함" : `못 전함: ${m.why}`}`);
  if (m.t === "gift") {   // 받은 선물: 저절로 받는다
    send({ t: "giftAck", gid: m.gid, ok: true });
    console.log(`선물 받음: ${m.from} 님이 ${m.money ? `${m.money}냥` : `${m.item} ${m.n}개`}`);
    setTimeout(() => { send({ t: "say", text: `${m.from} 님 선물 고마워요!` }); send({ t: "emote", e: "love", name: "하트" }); }, 800);
  }
  if (m.t === "who") {
    const other = m.players.find((p) => p.id !== me && p.map);
    if (!other) { if (!waitingSaid) console.log("따라갈 접속자를 기다립니다…"); waitingSaid = true; setTimeout(() => send({ t: "who" }), 2000); return; }
    waitingSaid = false;
    target = other.id; targetName = other.name; map = other.map;
    console.log(`${other.name}(${other.id})을 따라 ${map}에 들어갑니다`);
    send({ t: "enter", map });
    if (FLAGS.has("--gift") && !gifted) {
      gifted = true;
      setTimeout(() => { send({ t: "gift", gid: "b1", to: targetName, money: 100 }); console.log(`${targetName} 님에게 100냥 선물`); }, GIFT_AFTER);
      setTimeout(() => { send({ t: "gift", gid: "b2", to: targetName, item: "venice_glass", n: 2, cost: 60 }); console.log(`${targetName} 님에게 유리그릇 2개 선물`); }, GIFT_AFTER + 4000);
    }
  }
  if (m.t === "room") { const p = m.players.find((q) => q.id === target); if (p) center = { x: p.x, y: p.y }; }
  if ((m.t === "pos" || m.t === "join") && m.id === target) center = { x: m.x, y: m.y };
  if (m.t === "leave" && m.id === target) { console.log("따라가던 사람이 맵을 떠났습니다. 다시 찾습니다"); target = null; map = ""; send({ t: "who" }); }
};

// 1초에 10번: 따라가는 사람 둘레 반지름 2.5칸 원 (초당 약 2칸). 리액션하는 동안은 멈춰 선다
let pausedUntil = 0;
setInterval(() => {
  if (!map || Date.now() < pausedUntil) return;
  angle += 0.08;
  const x = center.x + Math.cos(angle) * 2.5, y = center.y + Math.sin(angle) * 2.5;
  const vx = -Math.sin(angle), vy = Math.cos(angle);
  const [fx, fy] = Math.abs(vx) > Math.abs(vy) ? [Math.sign(vx), 0] : [0, Math.sign(vy)];
  send({ t: "pos", x, y, fx, fy, m: true });
}, 100);

const EMOTES = [["bow", "인사"], ["hop", "깡총"], ["clap", "박수"], ["dance", "춤"], ["love", "하트"], ["spin", "빙글"], ["nap", "낮잠"]];
let emoteAt = 0;
if (FLAGS.has("--emote")) setInterval(() => {
  if (!map) return;
  const [e, label] = EMOTES[emoteAt++ % EMOTES.length];
  send({ t: "pos", x: center.x + 1.5, y: center.y, fx: -1, fy: 0, m: false });
  pausedUntil = Date.now() + 2600;
  send({ t: "emote", e, name: label });
  console.log(`리액션: ${label}`);
}, 4000);

let tradeAt = 0;
if (FLAGS.has("--trade")) setInterval(() => {
  if (!me) return;
  const sell = tradeAt++ % 2 === 0;
  send({ t: "trade", town: "venice_rialto", item: "venice_glass", n: sell ? 3 : 2, d: sell ? 3 : -2, sell });
  console.log(`리알토 장터: 유리그릇 ${sell ? "3개 팔기" : "2개 사기"}`);
}, 8000);

const LINES = ["안녕하세요!", "리알토 유리그릇 값 어때요?", "같이 장사해요", "여기 경치 좋다"];
let sayAt = 0;
if (FLAGS.has("--say")) setInterval(() => { if (me) send({ t: "say", text: LINES[sayAt++ % LINES.length] }); }, 10000);

setTimeout(() => { console.log(`${SECONDS}초가 지나 끝냅니다`); ws.close(); setTimeout(() => process.exit(0), 1000); }, SECONDS * 1000);
