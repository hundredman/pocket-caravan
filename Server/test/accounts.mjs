// 8단계 시험: 접속키 로그인 · 돈·짐 저장 · 장터 공유(거래 전달·다시 보내기) · 리액션
// 사용: npm run dev 를 켠 상태에서 npm run test:accounts [주소]
// 실제 서버에서 돌리면 "계정시험…" 이름의 시험 계정이 서버에 남는다 (작다)
const URL = process.argv[2] ?? "ws://127.0.0.1:8787/ws";
const RUN = Math.random().toString(36).slice(2, 7);
let failed = 0;
const check = (ok, what) => { console.log(`${ok ? "통과" : "실패"}: ${what}`); if (!ok) failed++; };
const key = () => "k" + Math.random().toString(36).slice(2) + "0123456789abcdef";

function open() {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(URL);
    const got = [];
    const waiters = [];
    const c = {
      ws, got, closed: null,
      send: (m) => ws.send(JSON.stringify(m)),
      wait: (test, ms = 3000) => new Promise((res) => {
        const hit = got.find(test);
        if (hit) return res(hit);
        const w = { test, resolve: res };
        waiters.push(w);
        setTimeout(() => { const i = waiters.indexOf(w); if (i >= 0) { waiters.splice(i, 1); res(null); } }, ms);
      }),
      waitClose: (ms = 3000) => new Promise((res) => {
        if (c.closed) return res(c.closed);
        const t = setTimeout(() => res(null), ms);
        c.onClosed = (code) => { clearTimeout(t); res(code); };
      }),
    };
    ws.onopen = () => resolve(c);
    ws.onerror = () => reject(new Error(`접속 실패 (${URL})`));
    ws.onclose = (e) => { c.closed = e.code; c.onClosed?.(e.code); };
    ws.onmessage = (e) => {
      const msg = JSON.parse(e.data);
      got.push(msg);
      for (const w of [...waiters]) if (w.test(msg)) { waiters.splice(waiters.indexOf(w), 1); w.resolve(msg); }
    };
  });
}

async function hello(fields) {
  const c = await open();
  c.send({ t: "hello", v: 1, key: key(), ...fields });
  c.welcome = await c.wait((m) => m.t === "welcome", 5000);
  return c;
}

// 1. 처음 보는 이름 + 접속키 → 계정이 생긴다
const name = `계정시험${RUN}`;
const a1 = await hello({ name, pass: "abcd1234" });
check(a1.welcome?.account === true && a1.welcome?.hasSave === false, `새 이름+접속키면 계정으로 들어온다 (${a1.welcome?.id})`);

// 2. 돈·짐 저장
a1.send({ t: "save", money: 1234, items: ["silk", "glass_bead"], counts: [3, 10], costs: [90.5, 12] });
check(!!(await a1.wait((m) => m.t === "saved")), "돈·짐을 저장하면 '저장됨'이 온다");
a1.send({ t: "save", money: -5, items: [], counts: [], costs: [] });
await new Promise((r) => setTimeout(r, 1100));
a1.send({ t: "save", money: -5, items: [], counts: [], costs: [] });
check(!!(await a1.wait((m) => m.t === "error" && m.why.includes("모양"))), "이상한 저장(돈이 음수)은 거절한다");
a1.ws.close();
await new Promise((r) => setTimeout(r, 300));

// 3. 다시 들어오면 같은 번호 + 저장한 돈·짐
const a2 = await hello({ name, pass: "abcd1234" });
const sv = a2.welcome?.save;
check(a2.welcome?.id === a1.welcome?.id, `다시 들어오면 같은 번호 (${a2.welcome?.id})`);
check(a2.welcome?.hasSave === true && sv?.money === 1234 && sv?.items?.join() === "silk,glass_bead" && sv?.counts?.join() === "3,10" && sv?.costs?.[0] === 90.5,
  `저장한 돈·짐이 돌아온다 (${sv?.money}냥, ${sv?.items?.join("·")})`);

// 4. 같은 계정이 다른 곳에서 들어오면 예전 접속은 4004로 닫힌다
const a3 = await hello({ name, pass: "abcd1234" });
check((await a2.waitClose()) === 4004 && a3.welcome?.id === a1.welcome?.id, "같은 계정이 다른 곳에서 들어오면 예전 접속은 4004로 닫히고 번호는 그대로");

// 5. 틀린 접속키 · 접속키 없이 계정 이름 · 너무 짧은 접속키 → 4005
const wrong = await open();
wrong.send({ t: "hello", v: 1, key: key(), name, pass: "zzzz9999" });
check((await wrong.waitClose()) === 4005 && !!wrong.got.find((m) => m.t === "error" && m.why.includes("다른 접속키")), "접속키가 틀리면 '다른 접속키' 안내 후 4005로 닫힌다");
const nopass = await open();
nopass.send({ t: "hello", v: 1, key: key(), name });
check((await nopass.waitClose()) === 4005, "계정이 있는 이름을 접속키 없이 쓰면 4005로 닫힌다");
const short = await open();
short.send({ t: "hello", v: 1, key: key(), name: `짧은키${RUN}`, pass: "ab" });
check((await short.waitClose()) === 4005, "접속키가 4자보다 짧으면 4005로 닫힌다");

// 6. 손님은 저장할 수 없다
const guest = await hello({ name: `손님${RUN}` });
check(guest.welcome?.account === false, "접속키 없이 들어오면 손님");
guest.send({ t: "save", money: 1, items: [], counts: [], costs: [] });
check(!!(await guest.wait((m) => m.t === "error" && m.why.includes("접속키"))), "손님이 저장하면 '접속키로 들어와야' 안내");

// 7. 공유 장터: 거래는 다른 사람 모두에게 가고, 보낸 사람에게는 안 간다
const map = `test_market_${RUN}`;
a3.send({ t: "enter", map }); await a3.wait((m) => m.t === "room");
guest.send({ t: "enter", map: `${map}_other` }); await guest.wait((m) => m.t === "room");
a3.send({ t: "trade", town: "venice_rialto", item: "silk", n: 3, d: 2.5, sell: true });
const tr = await guest.wait((m) => m.t === "trade" && m.id === a3.welcome.id);
check(tr?.town === "venice_rialto" && tr?.item === "silk" && tr?.n === 3 && tr?.d === 2.5 && tr?.sell === true && tr?.seq > 0 && tr?.name === name,
  `다른 맵 사람도 거래를 받는다 (번호 ${tr?.seq})`);
check(!(await a3.wait((m) => m.t === "trade", 500)), "거래한 사람 자신에게는 다시 오지 않는다");
a3.send({ t: "trade", town: "venice_rialto", item: "silk", n: 0, d: 1 });
check(!!(await a3.wait((m) => m.t === "error" && m.why.includes("거래"))), "이상한 거래(0개)는 거절한다");

// 8. 나중에 들어온 사람: since보다 뒤의 남의 거래를 한 번에 받는다
const late = await hello({ name: `늦게${RUN}`, since: tr.seq - 1 });
const replay = await late.wait((m) => m.t === "trades");
check(replay?.list?.some((t) => t.seq === tr.seq), `나중에 들어오면 놓친 거래를 받는다 (${replay?.list?.length}건)`);
check(late.welcome?.seq >= tr.seq, "welcome에 마지막 거래 번호가 온다");
const upToDate = await hello({ name: `최신${RUN}`, since: late.welcome.seq });
check(!(await upToDate.wait((m) => m.t === "trades", 600)), "이미 다 받은 사람은 다시 받지 않는다");
const again = await hello({ name, pass: "abcd1234", since: tr.seq - 1 });
check(!(await again.wait((m) => m.t === "trades" && m.list.some((t) => t.seq === tr.seq), 600)), "자기 거래는 다시 받지 않는다");

// 9. 리액션: 같은 맵 사람에게만, 이름과 함께
again.send({ t: "enter", map }); await again.wait((m) => m.t === "room");
late.send({ t: "enter", map }); await late.wait((m) => m.t === "room");
again.send({ t: "emote", e: "dance", name: "춤" });
const em = await late.wait((m) => m.t === "emote");
check(em?.id === again.welcome.id && em?.e === "dance" && em?.name === "춤", "같은 맵 사람이 리액션(춤)을 받는다");
check(!(await guest.wait((m) => m.t === "emote", 500)), "다른 맵 사람은 리액션을 받지 않는다");

for (const c of [guest, late, upToDate, again]) c.ws.close();
await new Promise((r) => setTimeout(r, 200));
console.log(failed ? `실패 ${failed}건` : "모두 통과");
process.exit(failed ? 1 : 0);
