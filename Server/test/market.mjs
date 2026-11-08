// 11/08 공유 장터 재고: 서버가 물건마다 적정 재고와의 차이를 들고 있다가 접속할 때 통째로 보낸다
// 사용: npm run dev 를 켠 상태에서 node test/market.mjs [주소]
const URL = process.argv[2] ?? "ws://127.0.0.1:8787/ws";
let failed = 0;
const check = (ok, what) => { console.log(`${ok ? "통과" : "실패"}: ${what}`); if (!ok) failed++; };
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

function join(name, hello) {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(URL);
    const got = [];
    const timer = setTimeout(() => reject(new Error(`${name}: 응답 없음`)), 5000);
    ws.onopen = () => ws.send(JSON.stringify({ t: "hello", v: 1, name, ...hello }));
    ws.onmessage = (e) => {
      const msg = JSON.parse(e.data);
      got.push(msg);
      if (msg.t === "welcome") { clearTimeout(timer); resolve({ ws, got, welcome: msg }); }
    };
    ws.onerror = () => { clearTimeout(timer); reject(new Error(`${name}: 접속 실패 (${URL})`)); };
  });
}

const run = Math.random().toString(36).slice(2, 7);
const item = "test_item_" + run;
const a = await join("장터A", { snap: true });
check(a.welcome.marketOk === true && Array.isArray(a.welcome.market), "snap을 보내면 welcome에 공유 장터 재고(market)가 온다");
check(!a.got.some((m) => m.t === "trades"), "재고를 통째로 받으면 지난 거래를 다시 보내지 않는다");
a.ws.send(JSON.stringify({ t: "trade", town: "gaegyeong", item, n: 5, d: -5, sell: false }));
a.ws.send(JSON.stringify({ t: "trade", town: "gaegyeong", item, n: 2, d: 1.5, sell: true }));
await sleep(500);

const b = await join("장터B", { snap: true });
const row = b.welcome.market?.find((r) => r.town === "gaegyeong" && r.item === item);
check(!!row && Math.abs(row.dev - -3.5) < 0.01, `나중에 들어온 B는 A의 거래가 쌓인 차이를 받는다 (${row?.dev})`);

const c = await join("장터C", { since: -1 });
await sleep(300);
check(c.welcome.marketOk !== true, "예전 게임(snap 없음)에는 재고를 보내지 않는다");
const replay = c.got.find((m) => m.t === "trades");
check(!!replay && replay.list.filter((t) => t.item === item).length === 2, "예전 게임은 지난 거래를 다시 받는다");

a.ws.close(); b.ws.close(); c.ws.close();
console.log(failed ? `실패 ${failed}개` : "모두 통과");
process.exit(failed ? 1 : 0);
