// 13단계 시험: 만국 경매 · 보물 상자 (2026-11-18)
// 사용: npm run dev 를 켠 상태에서 node test/games.mjs [주소]   (손님 둘로 들어온다 · 계정을 만들지 않는다)
const URL_WS = process.argv[2] ?? "ws://127.0.0.1:8787/ws";
let failed = 0;
const check = (ok, what) => { console.log(`${ok ? "통과" : "실패"}: ${what}`); if (!ok) failed++; };
const key = () => "k" + Math.random().toString(36).slice(2) + "0123456789abcdef";

function open() {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(URL_WS);
    const got = [], waiters = [];
    const c = {
      ws, got,
      send: (m) => ws.send(JSON.stringify(m)),
      wait: (test, ms = 3000) => new Promise((res) => {
        const hit = got.find(test);
        if (hit) { got.splice(got.indexOf(hit), 1); return res(hit); }
        const w = { test, resolve: res };
        waiters.push(w);
        setTimeout(() => { const i = waiters.indexOf(w); if (i >= 0) { waiters.splice(i, 1); res(null); } }, ms);
      }),
    };
    ws.onopen = () => resolve(c);
    ws.onerror = (e) => reject(e);
    ws.onmessage = (e) => {
      const m = JSON.parse(e.data);
      const w = waiters.find((x) => x.test(m));
      if (w) { waiters.splice(waiters.indexOf(w), 1); w.resolve(m); } else got.push(m);
    };
  });
}

const a = await open(), b = await open();
a.send({ t: "hello", v: 1, name: "경매시험A", key: key() });
b.send({ t: "hello", v: 1, name: "경매시험B", key: key() });
const wa = await a.wait((m) => m.t === "welcome"), wb = await b.wait((m) => m.t === "welcome");
check(wa?.games === true && wb?.games === true, "welcome에 games:true");

// 경매
a.send({ t: "auc" });
const s1 = await a.wait((m) => m.t === "auc");
check(!!s1 && typeof s1.lot === "string" && s1.lot.length > 0 && s1.price > 0 && s1.left > 100000, `경매대를 열면 물건이 올라온다 (${s1?.lot} ${s1?.price}냥, ${Math.round((s1?.left ?? 0) / 1000)}초)`);
const bcast = await b.wait((m) => m.t === "auc" && m.lot === s1?.lot);
check(!!bcast, "다른 사람에게도 새 물건이 알려진다");
a.send({ t: "bid", price: s1.price - 1 });
const low = await a.wait((m) => m.t === "auc" && !!m.why);
check(!!low && low.top === "", `시작가보다 낮으면 안 받는다 (${low?.why})`);
a.send({ t: "bid", price: s1.price });
const ba = await b.wait((m) => m.t === "auc" && m.top === "경매시험A");
check(!!ba && ba.price === s1.price, "A가 시작가를 부르면 모두에게 최고가가 간다");
await new Promise((r) => setTimeout(r, 450));
a.send({ t: "bid", price: s1.price + 10 });
const self = await a.wait((m) => m.t === "auc" && !!m.why);
check(!!self && /이미/.test(self.why), `이미 최고가면 또 못 부른다 (${self?.why})`);
b.send({ t: "bid", price: s1.price + 5 });
const step = await b.wait((m) => m.t === "auc" && !!m.why);
check(!!step && /\d+냥부터/.test(step.why), `10냥 올림보다 적으면 안 받는다 (${step?.why})`);
b.send({ t: "bid", price: s1.price + 10 });
const bb = await a.wait((m) => m.t === "auc" && m.top === "경매시험B");
check(!!bb && bb.price === s1.price + 10 && bb.left >= 14000, `B가 더 부르면 최고가가 바뀐다 (${bb?.price}냥, ${Math.round((bb?.left ?? 0) / 1000)}초)`);
b.send({ t: "auc" });
const again = await b.wait((m) => m.t === "auc" && !m.why);
check(!!again && again.lot === s1.lot && again.top === "경매시험B", "경매 중에 다시 열면 같은 물건을 보여 준다");

// 보물 상자
a.send({ t: "trs" });
const t1 = await a.wait((m) => m.t === "trs");
check(!!t1 && t1.tid > 0 && Number.isInteger(t1.idx) && Number.isInteger(t1.seed) && t1.found === "", `보물 상자 소문 (tid ${t1?.tid}, idx ${t1?.idx})`);
b.send({ t: "trsFind", tid: 12345 });
const wrong = await b.wait((m) => m.t === "trsLate");
check(!!wrong, "번호가 다르면 trsLate");
a.send({ t: "trsFind", tid: t1.tid });
const won = await a.wait((m) => m.t === "trsWon");
check(!!won && won.tid === t1.tid, "먼저 연 사람은 trsWon");
const seen = await b.wait((m) => m.t === "trs" && m.found === "경매시험A");
check(!!seen, "다른 사람에게 누가 찾았는지 알려진다");
b.send({ t: "trsFind", tid: t1.tid });
const late = await b.wait((m) => m.t === "trsLate");
check(!!late && late.found === "경매시험A", "늦게 열면 trsLate + 먼저 찾은 사람");

a.ws.close(); b.ws.close();
console.log(failed ? `실패 ${failed}개` : "모두 통과");
process.exit(failed ? 1 : 0);
