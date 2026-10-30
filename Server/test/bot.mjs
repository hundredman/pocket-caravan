// 시험용 가짜 플레이어. 다른 접속자 한 명을 골라 같은 맵에 들어가, 그 사람 둘레를 원을 그리며 걷는다.
// 혼자서도 "다른 사람이 걸어 다니는 모습"을 시험할 수 있게 한다. 벽은 무시하고 걷는다(시험용).
// 사용: node test/bot.mjs [주소] [이름] [초] [출신]
//   예) node test/bot.mjs ws://127.0.0.1:8787/ws 시험순록 120 viking
const URL = process.argv[2] ?? "ws://127.0.0.1:8787/ws";
const NAME = process.argv[3] ?? "시험고양이";
const SECONDS = Number(process.argv[4] ?? 120);

const ws = new WebSocket(URL);
const send = (m) => ws.send(JSON.stringify(m));
let waitingSaid = false;
let me = "", target = null, map = "", center = { x: 0, y: 0 }, angle = 0;

const LOOK = process.argv[5] ?? "viking";   // 생김새(출신): venice · goryeo · viking · arabia
ws.onopen = () => send({ t: "hello", v: 1, name: NAME, look: LOOK });
ws.onerror = () => { console.log(`접속 실패: ${URL}`); process.exit(1); };
ws.onclose = () => { console.log("연결 끊김"); process.exit(0); };
ws.onmessage = (e) => {
  const m = JSON.parse(e.data);
  if (m.t === "welcome") { me = m.id; console.log(`접속: ${me}`); send({ t: "who" }); }
  if (m.t === "who") {
    const other = m.players.find((p) => p.id !== me && p.map);
    if (!other) { if (!waitingSaid) console.log("따라갈 접속자를 기다립니다…"); waitingSaid = true; setTimeout(() => send({ t: "who" }), 2000); return; }
    waitingSaid = false;
    target = other.id; map = other.map;
    console.log(`${other.name}(${other.id})을 따라 ${map}에 들어갑니다`);
    send({ t: "enter", map });
  }
  if (m.t === "room") { const p = m.players.find((q) => q.id === target); if (p) center = { x: p.x, y: p.y }; }
  if ((m.t === "pos" || m.t === "join") && m.id === target) center = { x: m.x, y: m.y };
  if (m.t === "leave" && m.id === target) { console.log("따라가던 사람이 맵을 떠났습니다. 다시 찾습니다"); target = null; map = ""; send({ t: "who" }); }
};

// 1초에 10번: 따라가는 사람 둘레 반지름 2.5칸 원 (초당 약 2칸)
setInterval(() => {
  if (!map) return;
  angle += 0.08;
  const x = center.x + Math.cos(angle) * 2.5, y = center.y + Math.sin(angle) * 2.5;
  const vx = -Math.sin(angle), vy = Math.cos(angle);
  const [fx, fy] = Math.abs(vx) > Math.abs(vy) ? [Math.sign(vx), 0] : [0, Math.sign(vy)];
  send({ t: "pos", x, y, fx, fy, m: true });
}, 100);

setTimeout(() => { console.log(`${SECONDS}초가 지나 끝냅니다`); ws.close(); }, SECONDS * 1000);
