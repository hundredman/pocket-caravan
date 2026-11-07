// 10단계 시험: 게임 저장 통째로 (올리기 · 요약 · 받기 · 모양 검사 · 계정 지우면 같이 지움)
// 사용: Server/.dev.vars에 ADMIN_KEY를 넣고 npm run dev 를 켠 상태에서 npm run test:full [주소]
// 만든 "통째시험…" 계정은 마지막에 지운다
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
      ws, got, closed: null,
      send: (m) => ws.send(JSON.stringify(m)),
      wait: (test, ms = 3000) => new Promise((res) => {
        const hit = got.find(test);
        if (hit) return res(hit);
        const w = { test, resolve: res };
        waiters.push(w);
        setTimeout(() => { const i = waiters.indexOf(w); if (i >= 0) { waiters.splice(i, 1); res(null); } }, ms);
      }),
    };
    ws.onopen = () => resolve(c);
    ws.onerror = () => reject(new Error(`접속 실패 (${URL_WS})`));
    ws.onclose = (e) => { c.closed = e.code; };
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

/** 게임이 보내는 것처럼: 저장 JSON → gzip → base64, 해시는 JSON 글의 SHA-256 */
function pack(json, at, extra = {}) {
  return { t: "full", at, h: createHash("sha256").update(json, "utf8").digest("hex"), zip: gzipSync(Buffer.from(json, "utf8")).toString("base64"), ...extra };
}

const saveJson = JSON.stringify({ version: 3, scene: "venice_rialto", money: 1500, day: 12, vehicle: "cart_big", storedCargo: [{ item: "silk", count: 4 }], filler: "가".repeat(20000) });

// 1. 손님은 통째 저장을 못 한다
const guest = await hello({ name: `손님${RUN}` });
check(guest.welcome?.fullOk === true && guest.welcome?.full === undefined, "welcome에 fullOk가 오고, 손님은 저장 요약이 없다");
guest.send(pack(saveJson, Date.now()));
check(!!(await guest.wait((m) => m.t === "error" && m.why.includes("접속키"))), "손님이 올리면 거절한다");

// 2. 계정: 모양이 이상하면 거절
const name = `통째시험${RUN}`;
const a1 = await hello({ name, pass: "pass1234" });
check(a1.welcome?.account === true && a1.welcome?.fullOk === true && a1.welcome?.full === undefined, "새 계정은 저장 요약이 없다");
a1.send({ ...pack(saveJson, Date.now()), zip: "이건base64아님!" });
check(!!(await a1.wait((m) => m.t === "error" && m.why.includes("모양"))), "base64가 아니면 거절한다");
a1.send({ ...pack(saveJson, Date.now()), h: "abc" });
check(!!(await a1.wait((m) => m.t === "error" && m.why.includes("모양") && a1.got.filter((x) => x.t === "error").length >= 2)), "해시 모양이 이상하면 거절한다");
a1.send({ t: "full", at: Date.now(), h: "0".repeat(64), zip: "A".repeat(130_000) });
check(!!(await a1.wait((m) => m.t === "error" && m.why.includes("너무 깁니다"))), "너무 큰 저장은 거절한다");
a1.send({ t: "say", text: "안녕", pad: "x".repeat(17_000) });
check(!!(await a1.wait((m) => m.t === "error" && m.why.includes("너무 깁니다") && a1.got.filter((x) => x.why?.includes("너무 깁니다")).length >= 2)), "통째 저장이 아닌 메시지는 예전처럼 16000자까지만");

// 3. 올리기 → 저장됨, 바로 또 올리면 거절
const at1 = Date.now() - 60_000;
const p1 = pack(saveJson, at1, { day: 12, money: 1500, map: "venice_rialto", origin: "venice" });
console.log(`  (저장 JSON ${saveJson.length}자 → 압축 ${p1.zip.length}자)`);
a1.send(p1);
const saved = await a1.wait((m) => m.t === "fullSaved");
check(saved?.at === at1 && saved?.h === p1.h, "올리면 fullSaved에 at·h가 온다");
a1.send(pack(saveJson + " ", Date.now()));
check(!!(await a1.wait((m) => m.t === "error" && m.why.includes("자주"))), "5초 안에 또 올리면 거절한다");

// 4. 돈·짐 저장(8단계)도 그대로 된다
a1.send({ t: "save", money: 1500, items: ["silk"], counts: [4], costs: [200] });
check(!!(await a1.wait((m) => m.t === "saved")), "돈·짐 저장도 같이 된다");

// 5. 다시 들어오면 요약만 온다 (압축 글은 안 옴)
a1.ws.close();
await sleep(300);
const a2 = await hello({ name, pass: "pass1234" });
const f = a2.welcome?.full;
check(f?.at === at1 && f?.h === p1.h && f?.day === 12 && f?.money === 1500 && f?.map === "venice_rialto" && f?.origin === "venice" && f?.size === p1.zip.length,
  "다시 들어오면 welcome에 저장 요약(at·h·날·돈·맵·출신·크기)이 온다");
check(!JSON.stringify(a2.welcome).includes(p1.zip.slice(0, 40)), "welcome에는 압축 글이 없다");
check(a2.welcome?.hasSave === true && a2.welcome?.save?.money === 1500, "돈·짐 저장 요약도 그대로 온다");

// 6. 받기: 압축을 풀면 처음 JSON과 같다
a2.send({ t: "getFull" });
const data = await a2.wait((m) => m.t === "fullData");
const back = data?.zip ? gunzipSync(Buffer.from(data.zip, "base64")).toString("utf8") : "";
check(data?.h === p1.h && back === saveJson, "getFull로 받은 저장을 풀면 올린 것과 같다");

// 7. 관리 도구: 계정 목록에 게임 저장이 보이고, 계정을 지우면 저장도 지운다
if (ADMIN) {
  const row = (await admin("GET", "accounts")).data?.accounts?.find((a) => a.name === name);
  check(row?.fullDay === 12 && row?.fullSize === p1.zip.length && row?.fullMap === "venice_rialto", "관리 계정 목록에 게임 저장(날·크기·맵)이 보인다");
  a2.ws.close();
  await sleep(300);
  const del = await admin("POST", "delete", { names: [name], confirm: true });
  check(del.data?.deleted?.[0] === name, "계정을 지운다");
  const a3 = await hello({ name, pass: "other-pass" });
  check(a3.welcome?.account === true && a3.welcome?.full === undefined, "같은 이름으로 새로 만들면 예전 게임 저장 요약이 없다");
  a3.send({ t: "getFull" });
  const empty = await a3.wait((m) => m.t === "fullData");
  check(empty?.zip === "" && empty?.h === "", "예전 게임 저장도 같이 지워졌다");
  a3.ws.close();
  await sleep(200);
  await admin("POST", "delete", { names: [name], confirm: true });
} else {
  console.log("  (ADMIN_KEY가 없어 관리 도구 부분은 건너뜀. 시험 계정이 남는다)");
  a2.ws.close();
}

guest.ws.close();
await sleep(200);
console.log(failed ? `\n실패 ${failed}개` : "\n모두 통과");
process.exit(failed ? 1 : 0);
