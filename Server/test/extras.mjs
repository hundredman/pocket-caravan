// 11단계 시험: 저장 기록·되돌리기 · 순위표 · 선물 · 사용량
// 사용: Server/.dev.vars에 ADMIN_KEY를 넣고 npm run dev 를 켠 상태에서 npm run test:extras [주소]   (40초쯤 걸린다)
// 만든 "추가시험…" 계정은 마지막에 지운다
import { readFileSync } from "node:fs";
import { gzipSync, gunzipSync } from "node:zlib";
import { createHash } from "node:crypto";

const URL_WS = process.argv[2] ?? "ws://127.0.0.1:8787/ws";
const BASE = URL_WS.replace(/^ws/, "http").replace(/\/ws$/, "");
const RUN = Math.random().toString(36).slice(2, 7);
let failed = 0;
const check = (ok, what) => { console.log(`${ok ? "통과" : "실패"}: ${what}`); if (!ok) failed++; };
const key = () => "k" + Math.random().toString(36).slice(2) + "0123456789abcdef";
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const ADMIN = process.env.ADMIN_KEY
  ?? readFileSync(new URL("../.dev.vars", import.meta.url), "utf8").match(/^\s*ADMIN_KEY\s*=\s*"?([^"\r\n]+)"?\s*$/m)?.[1];
if (!ADMIN) { console.error("ADMIN_KEY가 없어요 (Server/.dev.vars)"); process.exit(1); }

async function admin(method, path, body) {
  const res = await fetch(`${BASE}/admin/${path}`, { method, headers: { Authorization: `Bearer ${ADMIN}` }, body: body ? JSON.stringify(body) : undefined });
  return { status: res.status, data: await res.json().catch(() => null) };
}

function open() {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(URL_WS);
    const got = [];
    const waiters = [];
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
    ws.onerror = () => reject(new Error(`접속 실패 (${URL_WS})`));
    ws.onmessage = (e) => {
      const msg = JSON.parse(e.data);
      const w = waiters.find((x) => x.test(msg));
      if (w) { waiters.splice(waiters.indexOf(w), 1); w.resolve(msg); } else got.push(msg);
    };
  });
}

async function hello(fields) {
  const c = await open();
  c.send({ t: "hello", v: 1, key: key(), ...fields });
  c.welcome = await c.wait((m) => m.t === "welcome", 5000);
  return c;
}

const pack = (json, at, extra) => ({ t: "full", at, h: createHash("sha256").update(json).digest("hex"), zip: gzipSync(Buffer.from(json)).toString("base64"), ...extra });
const nameA = `추가A${RUN}`, nameB = `추가B${RUN}`;

// 1. 사용량
const a = await hello({ name: nameA, pass: "pass1234" });
const st = await admin("GET", "status");
const u = st.data?.usage;
check(u && u.opens >= 1 && u.msgs >= 1 && u.estimate >= u.opens && u.limit === 100000 && /^\d{4}-\d{2}-\d{2}$/.test(u.day), `상태에 오늘 사용량이 있다 (어림 ${u?.estimate}번)`);

// 2. 저장 기록: 날짜가 줄면(새로 시작) 예전 것을 남긴다 → 되돌리기
const day10 = JSON.stringify({ scene: "venice_rialto", day: 10, money: 5000, note: "열흘째 저장" });
const day2 = JSON.stringify({ scene: "venice_rialto", day: 2, money: 100, note: "새로 시작한 저장" });
a.send(pack(day10, Date.now() - 60_000, { day: 10, money: 5000, map: "venice_rialto", origin: "venice" }));
check(!!(await a.wait((m) => m.t === "fullSaved")), "10일째 저장을 올린다");
await sleep(5200);
a.send(pack(day2, Date.now(), { day: 2, money: 100, map: "venice_rialto", origin: "venice" }));
check(!!(await a.wait((m) => m.t === "fullSaved")), "새로 시작한 2일째 저장으로 덮는다");
let sv = await admin("POST", "saves", { name: nameA });
check(sv.data?.current?.day === 2 && sv.data?.history?.length === 1 && sv.data.history[0].day === 10 && sv.data.history[0].money === 5000,
  "날짜가 줄었으니 예전 10일째 저장이 기록에 남는다");
const slot = sv.data?.history?.[0]?.slot;
const before = Date.now();
const rs = await admin("POST", "restore", { name: nameA, slot });
check(rs.data?.ok === true && rs.data.day === 10, "기록 번호로 되돌린다");
sv = await admin("POST", "saves", { name: nameA });
check(sv.data?.current?.day === 10 && sv.data?.current?.at >= before && sv.data?.history?.some((x) => x.day === 2),
  "되돌린 저장은 지금 시각이 되고, 되돌리기 전 2일째도 기록에 남는다");
check((await admin("POST", "restore", { name: nameA, slot: 99 })).status === 404, "없는 번호는 404");
a.ws.close();
await sleep(300);
const a2 = await hello({ name: nameA, pass: "pass1234" });
check(a2.welcome?.full?.day === 10 && a2.welcome.full.at >= before, "다시 들어오면 welcome 요약이 되돌린 저장이다 (게임이 고르는 창을 띄울 수 있다)");
a2.send({ t: "getFull" });
const fd = await a2.wait((m) => m.t === "fullData");
check(fd?.zip && gunzipSync(Buffer.from(fd.zip, "base64")).toString() === day10, "받은 저장이 10일째 저장 글과 같다");
await sleep(5200);
a2.send(pack(JSON.stringify({ day: 11 }), Date.now(), { day: 11, money: 5100 }));
await a2.wait((m) => m.t === "fullSaved");
sv = await admin("POST", "saves", { name: nameA });
check(sv.data?.history?.length === 2, "10분 안에 날짜가 늘며 덮으면 기록을 더 남기지 않는다 (쓰기 아끼기)");

// 3. 순위표
const guest = await hello({ name: `손님${RUN}` });
guest.send({ t: "score", board: "gondola", value: 999 });
check(!!(await guest.wait((m) => m.t === "error" && m.why.includes("접속키"))), "손님 점수는 받지 않는다");
a2.send({ t: "score", board: "gondola", value: 120 });
let sc = await a2.wait((m) => m.t === "scored");
check(sc?.best === 120 && sc?.rank >= 1, `첫 점수 120 (${sc?.rank}위)`);
await sleep(1100);
a2.send({ t: "score", board: "gondola", value: 80 });
sc = await a2.wait((m) => m.t === "scored");
check(sc?.best === 120, "더 낮은 점수는 최고점을 바꾸지 않는다");
const b = await hello({ name: nameB, pass: "pass1234" });
b.send({ t: "score", board: "gondola", value: 150 });
const scB = await b.wait((m) => m.t === "scored");
check(scB?.best === 150, "다른 계정 150점");
await sleep(1100);
a2.send({ t: "score", board: "Bad Id", value: 1 });
check(!!(await a2.wait((m) => m.t === "error" && m.why.includes("점수"))), "판 이름이 이상하면 거절");
await sleep(1100);
a2.send({ t: "score", board: "money", value: 1 });
check(!!(await a2.wait((m) => m.t === "error" && m.why.includes("점수"))), "돈 순위는 직접 올릴 수 없다 (저장에서 만든다)");
a2.send({ t: "getBoards" });
const bd = await a2.wait((m) => m.t === "boards");
const gondola = bd?.boards?.find((x) => x.id === "gondola")?.rows ?? [];
const iA = gondola.findIndex((r) => r.name === nameA), iB = gondola.findIndex((r) => r.name === nameB);
check(iB >= 0 && iA > iB && gondola[iA].value === 120, "곤돌라 순위에 두 사람이 점수 순서로 있다");
const money = bd?.boards?.find((x) => x.id === "money")?.rows ?? [];
check(money.some((r) => r.name === nameA && r.value === 5100), "돈 순위는 계정 게임 저장의 돈으로 만든다");

// 4. 선물
a2.send({ t: "gift", gid: "g1", to: nameB, money: 100 });
const gotGift = await b.wait((m) => m.t === "gift");
check(gotGift?.from === nameA && gotGift?.money === 100 && !!(await a2.wait((m) => m.t === "giftSent" && m.gid === "g1")), "돈 선물이 받는 사람에게 간다");
b.send({ t: "giftAck", gid: gotGift?.gid, ok: true });
const r1 = await a2.wait((m) => m.t === "giftResult" && m.gid === "g1");
check(r1?.ok === true && r1?.to === nameB, "받는 사람이 받으면 보낸 사람에게 ok가 온다");
await sleep(2100);
a2.send({ t: "gift", gid: "g2", to: nameB, item: "silk", n: 3, cost: 270 });
const g2 = await b.wait((m) => m.t === "gift");
check(g2?.item === "silk" && g2?.n === 3 && g2?.cost === 270 && g2?.money === 0, "짐 선물에는 물건·개수·산 값이 같이 간다");
b.send({ t: "giftAck", gid: g2?.gid, ok: false, why: "짐이 꽉 찼어요" });
const r2 = await a2.wait((m) => m.t === "giftResult" && m.gid === "g2");
check(r2?.ok === false && r2?.why === "짐이 꽉 찼어요", "받는 사람이 못 받으면 이유와 함께 ok:false (보낸 게임이 돌려받는다)");
a2.send({ t: "gift", gid: "g3", to: nameB, money: 5 });
const r3 = await a2.wait((m) => m.t === "giftResult" && m.gid === "g3");
check(r3?.ok === false && r3.why.includes("2초"), "2초 안에 또 보내면 거절");
await sleep(2100);
a2.send({ t: "gift", gid: "g4", to: `없는사람${RUN}`, money: 5 });
const r4 = await a2.wait((m) => m.t === "giftResult" && m.gid === "g4");
check(r4?.ok === false && r4.why.includes("접속해 있지 않아요"), "접속하지 않은 사람에게는 바로 ok:false");
await sleep(2100);
a2.send({ t: "gift", gid: "g5", to: nameB, money: 7 });
await b.wait((m) => m.t === "gift");
b.ws.close();
const r5 = await a2.wait((m) => m.t === "giftResult" && m.gid === "g5", 5000);
check(r5?.ok === false && r5.why.includes("끊었어요"), "받는 사람이 답하기 전에 나가면 ok:false");
const b2 = await hello({ name: nameB, pass: "pass1234" });
await sleep(2100);
a2.send({ t: "gift", gid: "g6", to: nameB, money: 9 });
check(!!(await b2.wait((m) => m.t === "gift")), "(답하지 않는 받는 사람)");
const r6 = await a2.wait((m) => m.t === "giftResult" && m.gid === "g6", 25000);
check(r6?.ok === false && r6.why.includes("답이 없어요"), "10초 넘게 답이 없으면 ok:false");

// 5. 계정을 지우면 순위표에서도 빠진다
for (const c of [a2, b2, guest]) c.ws.close();
await sleep(300);
await admin("POST", "delete", { names: [nameA, nameB], confirm: true });
const c3 = await hello({ name: `손님${RUN}b` });
c3.send({ t: "getBoards" });
const bd2 = await c3.wait((m) => m.t === "boards");
const left = bd2?.boards?.find((x) => x.id === "gondola")?.rows ?? [];
check(!left.some((r) => r.name === nameA || r.name === nameB), "지운 계정은 순위표에서도 빠진다");
c3.ws.close();
await sleep(200);
console.log(failed ? `\n실패 ${failed}개` : "\n모두 통과");
process.exit(failed ? 1 : 0);
