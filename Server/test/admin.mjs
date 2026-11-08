// 9단계 시험: 관리 도구 (관리 키 · 접속자/계정 보기 · 접속키 초기화 · 내보내기 · 알림 · 백업 · 계정 지우기)
// 사용: Server/.dev.vars에 ADMIN_KEY를 넣고 npm run dev 를 켠 상태에서 npm run test:admin [주소]
// 만든 "관리시험…" 계정은 마지막에 지운다
import { readFileSync } from "node:fs";

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

async function admin(method, path, body, auth = ADMIN) {
  const res = await fetch(`${BASE}/admin/${path}`, {
    method,
    headers: auth ? { Authorization: `Bearer ${auth}` } : {},
    body: body ? JSON.stringify(body) : undefined,
  });
  let data = null;
  try { data = await res.json(); } catch { /* JSON 아님 */ }
  return { status: res.status, data };
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
      waitClose: (ms = 3000) => new Promise((res) => {
        if (c.closed) return res(c.closed);
        const t = setTimeout(() => res(null), ms);
        c.onClosed = (code) => { clearTimeout(t); res(code); };
      }),
    };
    ws.onopen = () => resolve(c);
    ws.onerror = () => reject(new Error(`접속 실패 (${URL_WS})`));
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

// 1. 관리 키가 없거나 틀리면 막는다
check((await admin("GET", "status", null, "")).status === 401, "관리 키 없이 부르면 401");
check((await admin("GET", "status", null, ADMIN + "x")).status === 401, "틀린 관리 키는 401");
check((await admin("GET", "nothing")).status === 404, "모르는 관리 요청은 404");

// 2. 접속자 · 계정 보기
const name = `관리시험${RUN}`;
const a1 = await hello({ name, pass: "first-pass", look: "venice" });
check(a1.welcome?.account === true, `계정으로 들어온다 (${a1.welcome?.id})`);
a1.send({ t: "enter", map: "venice_rialto" });
await a1.wait((m) => m.t === "room");
a1.send({ t: "save", money: 777, items: ["silk"], counts: [2], costs: [50] });
await a1.wait((m) => m.t === "saved");
const st = await admin("GET", "status");
const meThere = st.data?.players?.find((p) => p.name === name);
check(st.status === 200 && meThere?.account === true && meThere?.map === "venice_rialto" && meThere?.look === "venice", "상태에 접속자(이름·맵·출신·계정)가 보인다");
check(Number.isInteger(st.data?.accounts) && st.data.accounts >= 1, `상태에 계정 수가 있다 (${st.data?.accounts})`);
const ac = await admin("GET", "accounts");
const row = ac.data?.accounts?.find((a) => a.name === name);
check(row?.online === true && row?.money === 777 && row?.kinds === 1 && row?.needsPass === false, "계정 목록에 접속 중·돈·짐 종류가 보인다");
check(!JSON.stringify(ac.data).includes("hash") && !JSON.stringify(ac.data).includes("salt"), "계정 목록에 접속키 해시·소금은 없다");

// 3. 알림: 접속한 모두에게 간다
const watcher = await hello({ name: `손님${RUN}` });
const sent = await admin("POST", "notice", { text: "  5분 뒤 <b>점검</b>\n해요 " });
const n1 = await a1.wait((m) => m.t === "notice");
const n2 = await watcher.wait((m) => m.t === "notice");
check(sent.status === 200 && sent.data.sent >= 2, `알림을 보낸 사람 수가 온다 (${sent.data?.sent})`);
check(n1?.text === "5분 뒤 ‹b›점검‹/b› 해요" && n2?.text === n1?.text, `알림이 모두에게 간다, 꺾쇠·줄바꿈은 정리된다 (${n1?.text})`);
check((await admin("POST", "notice", { text: "   " })).status === 400, "빈 알림은 거절한다");

// 4. 접속키 초기화: 돈·짐·번호는 그대로, 다음 접속키가 새 접속키
const id = a1.welcome.id;
const rs = await admin("POST", "reset", { name: name.toUpperCase() });
check(rs.status === 200 && rs.data?.name === name, "접속키를 지운다 (이름 대소문자는 상관없다)");
check((await admin("POST", "reset", { name: `없는이름${RUN}` })).status === 404, "없는 계정은 404");
const listed = (await admin("GET", "accounts")).data?.accounts?.find((a) => a.name === name);
check(listed?.needsPass === true, "계정 목록에 '접속키 새로 정하는 중'이 보인다");
const noPass = await hello({ name });
check(!noPass.welcome && (await noPass.waitClose()) === 4005 && noPass.got.some((m) => m.t === "error" && m.why.includes("새로 정해야")), "접속키 없이 들어오면 새로 정하라고 하고 닫는다");
const a2 = await hello({ name, pass: "second-pass" });
check(a2.welcome?.account === true && a2.welcome?.id === id && a2.welcome?.newPass === true, "새 접속키로 들어오면 같은 번호에 newPass가 온다");
check(a2.welcome?.save?.money === 777 && a2.welcome?.save?.items?.[0] === "silk", "돈·짐 저장은 그대로다");
check((await a1.waitClose()) === 4004, "예전 접속은 4004로 닫힌다");
a2.ws.close();
await sleep(200);
const old = await hello({ name, pass: "first-pass" });
check(!old.welcome && (await old.waitClose()) === 4005, "예전 접속키로는 이제 못 들어온다");
const a3 = await hello({ name, pass: "second-pass" });
check(a3.welcome?.account === true && !a3.welcome?.newPass, "새 접속키로 다시 들어오면 newPass는 없다");

// 5. 내보내기: 이유를 알리고 4006으로 닫는다
const kk = await admin("POST", "kick", { name });
check(kk.data?.kicked === 1, "내보내기가 한 명을 끊었다고 답한다");
check((await a3.waitClose()) === 4006 && a3.got.some((m) => m.t === "error" && m.why.includes("관리자")), "내보낸 사람은 이유를 받고 4006으로 닫힌다");
check((await admin("POST", "kick", { name: `없는이름${RUN}` })).data?.kicked === 0, "없는 사람을 내보내면 0");

// 6. 백업: 저장소를 키 순서대로 나눠 받는다
async function backup(limit) {
  const all = new Map();
  let after = "", pages = 0;
  do {
    const r = await admin("GET", `backup?limit=${limit}` + (after ? `&after=${encodeURIComponent(after)}` : ""));
    if (r.status !== 200 || !Array.isArray(r.data?.entries)) return null;
    for (const [k, v] of r.data.entries) all.set(k, v);
    after = r.data.next ?? "";
    pages++;
  } while (after && pages < 10000);
  return { all, pages };
}
check((await admin("GET", "backup", null, ADMIN + "x")).status === 401, "백업도 관리 키가 틀리면 401");
const whole = await backup(100), bits = await backup(2);
const mine = whole?.all.get("acct:" + name.toLowerCase());
check(mine?.save?.money === 777 && typeof mine.hash === "string" && mine.hash.length > 0, "백업에 계정(접속키 해시 포함)과 돈·짐 저장이 들어 있다");
check(!!bits && bits.pages > 1 && bits.all.size === whole.all.size && [...whole.all.keys()].every((k) => bits.all.has(k)),
  `2개씩 나눠 받아도 같은 키를 다 받는다 (${bits?.pages}번, 키 ${bits?.all.size}개)`);

// 7. 지우기: 확인 없이 부르면 목록만, 확인하면 지운다
check((await admin("POST", "delete", { prefix: "관" })).status === 400, "prefix가 1자면 거절한다");
check((await admin("POST", "delete", {})).status === 400, "names·prefix가 없으면 거절한다");
const online = await hello({ name, pass: "second-pass" });
const dry = await admin("POST", "delete", { prefix: `관리시험${RUN}` });
check(dry.data?.dry === true && dry.data?.match?.length === 1 && dry.data.match[0] === name, "확인 없이 부르면 지울 목록만 보여 준다");
check((await admin("GET", "accounts")).data?.accounts?.some((a) => a.name === name), "목록만 볼 때는 지우지 않는다");
const del = await admin("POST", "delete", { names: [name, `없는이름${RUN}`], confirm: true });
check(del.data?.deleted?.length === 1 && del.data.deleted[0] === name, "확인하면 있는 계정만 지운다");
check((await online.waitClose()) === 4006, "지운 계정으로 접속 중이던 사람은 4006으로 닫힌다");
check(!(await admin("GET", "accounts")).data?.accounts?.some((a) => a.name === name), "지운 계정은 목록에서 사라진다");
const guest = await hello({ name });
check(guest.welcome?.account === false, "지운 이름은 손님도 쓸 수 있다");

for (const c of [watcher, guest]) c.ws.close();
await sleep(200);
console.log(failed ? `\n실패 ${failed}개` : "\n모두 통과");
process.exit(failed ? 1 : 0);
