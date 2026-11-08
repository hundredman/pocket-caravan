// 12단계 시험: 백업 · 되살리기 · 자동 백업 (관리 도구 tools/admin.mjs도 직접 돌려 본다)
// 사용: Server/.dev.vars에 ADMIN_KEY를 넣고 npx wrangler dev --test-scheduled 를 켠 상태에서 npm run test:backup [주소]
// 내 컴퓨터 서버에서만 돌린다: 서버 전체 되살리기(--똑같이)로 시험 뒤에 생긴 것을 지운다. 만든 "백업시험…" 계정은 마지막에 지운다
import { readFileSync, mkdtempSync, readdirSync, rmSync, existsSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { execFileSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const URL_WS = process.argv[2] ?? "ws://127.0.0.1:8787/ws";
const BASE = URL_WS.replace(/^ws/, "http").replace(/\/ws$/, "");
if (BASE.includes("workers.dev")) { console.error("올린 서버에서는 돌리지 않아요 (서버 전체를 되살려요)"); process.exit(1); }
const RUN = Math.random().toString(36).slice(2, 7);
let failed = 0;
const check = (ok, what) => { console.log(`${ok ? "통과" : "실패"}: ${what}`); if (!ok) failed++; };
const key = () => "k" + Math.random().toString(36).slice(2) + "0123456789abcdef";
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const ADMIN = process.env.ADMIN_KEY
  ?? readFileSync(new URL("../.dev.vars", import.meta.url), "utf8").match(/^\s*ADMIN_KEY\s*=\s*"?([^"\r\n]+)"?\s*$/m)?.[1];
if (!ADMIN) { console.error("ADMIN_KEY가 없어요 (Server/.dev.vars)"); process.exit(1); }

const DIR = mkdtempSync(join(tmpdir(), "pc-backup-"));
const TOOL = fileURLToPath(new URL("../tools/admin.mjs", import.meta.url));
/** 관리 도구를 돌리고 출력(글)을 돌려준다. 실패하면 출력 앞에 "!"를 붙인다 */
function tool(...words) {
  try {
    return execFileSync(process.execPath, [TOOL, ...words, "--server", BASE], { env: { ...process.env, PC_BACKUP_DIR: DIR }, encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] });
  } catch (e) {
    return "!" + (e.stdout ?? "") + (e.stderr ?? "");
  }
}

async function admin(method, path, body) {
  const res = await fetch(`${BASE}/admin/${path}`, { method, headers: { Authorization: `Bearer ${ADMIN}` }, body: body ? JSON.stringify(body) : undefined });
  return { status: res.status, res, data: res.headers.get("content-type")?.includes("json") ? await res.json() : null };
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

const accounts = async () => (await admin("GET", "accounts")).data?.accounts ?? [];
const nameA = `백업시험A${RUN}`, nameB = `백업시험B${RUN}`;

// 1. 백업 파일 받기 (관리 도구)
const a = await hello({ name: nameA, pass: "pass1234" });
a.send({ t: "save", money: 500, items: ["silk"], counts: [2], costs: [40] });
check(!!(await a.wait((m) => m.t === "saved")), "A 계정에 500냥 저장");
const file1 = join(DIR, "b1.json");
const out1 = tool("백업", "--out", file1);
const b1 = existsSync(file1) ? JSON.parse(readFileSync(file1, "utf8")) : null;
check(!out1.startsWith("!") && b1?.entries?.[`acct:${nameA.toLowerCase()}`]?.save?.money === 500, "관리 도구 '백업'이 파일에 A 계정을 담는다");

// 2. 실수로 지운 계정 하나만 되살리기
await sleep(1100);
a.send({ t: "save", money: 900, items: [], counts: [], costs: [] });
await a.wait((m) => m.t === "saved");
const b = await hello({ name: nameB, pass: "pass1234" });
check(b.welcome?.account === true, "백업 뒤에 B 계정이 생긴다");
await admin("POST", "delete", { names: [nameA], confirm: true });
check((await a.waitClose()) === 4006 && !(await accounts()).some((x) => x.name === nameA), "A 계정을 지웠다 (실수했다고 치자)");
const dry = tool("되살리기", file1, "--이름", nameA);
check(dry.includes("다시 생기는 계정: " + nameA) && dry.includes("--yes") && !(await accounts()).some((x) => x.name === nameA),
  "--yes 없이는 바뀔 것만 보여 주고 넣지 않는다");
check(tool("되살리기", file1, "--이름", `없는사람${RUN}`).startsWith("!"), "백업에 없는 계정 이름이면 실패");
const did = tool("되살리기", file1, "--이름", nameA, "--yes");
check(did.includes("되살렸어요") && readdirSync(DIR).some((f) => f.includes("before-load")), "되살리기 전에 지금 서버를 먼저 백업해 둔다");
const backA = (await accounts()).find((x) => x.name === nameA);
check(backA?.money === 500, `A 계정이 백업 때 돈(500냥)으로 돌아온다 (${backA?.money})`);
const a2 = await hello({ name: nameA, pass: "pass1234" });
check(a2.welcome?.account === true && a2.welcome?.save?.money === 500, "A는 예전 접속키로 다시 들어온다");
check(tool("되살리기", file1, "--이름", nameA).includes("이미 백업과 같아요"), "한 번 더 하면 바뀔 것이 없다");

// 3. 서버 전체 되살리기: 백업에 없는 것은 그대로, --똑같이면 지운다
const before = (await admin("GET", "backup")).data?.entries?.find(([k]) => k === "nextId")?.[1] ?? 0;
const keepDry = tool("되살리기", file1);
check(keepDry.includes("그대로 둬요") && keepDry.includes("--똑같이"), "백업 뒤에 생긴 것(B 계정)은 그대로 둔다고 알려 준다");
const exactDry = tool("되살리기", file1, "--똑같이");
check(exactDry.includes("지우는 계정: ") && exactDry.includes(nameB), "--똑같이면 B 계정을 지운다고 보여 준다");
const watcher = await hello({ name: `손님${RUN}` });
const whole = tool("되살리기", file1, "--똑같이", "--yes");
const note = await watcher.wait((m) => m.t === "notice" && m.text.includes("되살렸어요"));
check(whole.includes("되살렸어요") && !!note && (await watcher.waitClose()) === 1012, "접속한 사람은 알림을 받고 1012로 닫힌다 (게임은 다시 접속한다)");
check((await a2.waitClose()) === 1012, "계정으로 접속한 사람도 1012로 닫힌다");
const list = await accounts();
check(!list.some((x) => x.name === nameB) && list.find((x) => x.name === nameA)?.money === 500, "B 계정은 지워지고 A는 백업 그대로다");
const after = (await admin("GET", "backup")).data?.entries?.find(([k]) => k === "nextId")?.[1] ?? 0;
check(after >= before, `번호(nextId)는 줄이지 않는다 (${before} → ${after})`);
check((await admin("POST", "load", { put: [] })).status === 400, "confirm 없이는 거절");
check((await admin("POST", "load", { confirm: true, put: [["", 1]] })).status === 400, "이상한 키는 거절");

// 4. 자동 백업
const today = new Date().toISOString().slice(0, 10);
const t0 = Date.now();
const nowRes = await admin("POST", "autobackup");
check(nowRes.data?.ok === true && nowRes.data.day === today && nowRes.data.keys > 0 && nowRes.data.parts === 1, `자동 백업을 지금 한다 (키 ${nowRes.data?.keys}개, ${nowRes.data?.bytes}바이트)`);
const autos = (await admin("GET", "autobackups")).data;
check(autos?.keep === 7 && autos.days?.[0]?.day === today && autos.days[0].at >= t0, "자동 백업 목록 맨 앞에 오늘 것이 있다");
const autoFile = join(DIR, "auto.json");
const got = tool("자동백업", "받기", today, "--out", autoFile);
const auto = existsSync(autoFile) ? JSON.parse(readFileSync(autoFile, "utf8")) : null;
check(got.includes("받았어요") && auto?.kind === "pocket-caravan-backup" && auto.entries?.[`acct:${nameA.toLowerCase()}`]?.save?.money === 500,
  "관리 도구로 받은 자동 백업은 백업 파일과 같은 모양이다 (되살리기에 그대로 쓴다)");
check(tool("되살리기", autoFile).includes("이미 백업과 같아요"), "방금 받은 자동 백업으로 되살리면 바뀔 것이 없다");
check((await admin("GET", "autobackup?day=2000-01-01")).status === 404 && (await admin("GET", "autobackup?day=어제")).status === 400, "없는 날은 404, 날짜 모양이 틀리면 400");
const t1 = Date.now();
const cron = await fetch(`${BASE}/cdn-cgi/handler/scheduled?cron=${encodeURIComponent("0 18 * * *")}`);   // wrangler dev --test-scheduled
await sleep(500);
const autos2 = (await admin("GET", "autobackups")).data;
check(cron.status === 200 && autos2?.days?.[0]?.day === today && autos2.days[0].at >= t1 && autos2.days.length === autos.days.length,
  "매일 도는 cron이 자동 백업을 해서 오늘 것을 덮는다");

// 정리
await admin("POST", "delete", { names: [nameA, nameB], confirm: true });
for (const c of [b, watcher]) c.ws.close();
rmSync(DIR, { recursive: true, force: true });
await sleep(200);
console.log(failed ? `\n실패 ${failed}개` : "\n모두 통과");
process.exit(failed ? 1 : 0);
