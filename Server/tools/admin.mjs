// 관리 도구 (2026-11-05): 접속자 보기 · 계정 목록 · 접속키 초기화 · 계정 지우기 · 내보내기 · 알림
// 사용: npm run admin -- <명령> [...]          (Server 폴더에서. 내 컴퓨터 서버는 끝에 --local)
//   상태                    지금 접속한 사람, 계정 수, 공유 장터 거래 기록
//   계정                    계정 목록 (만든 날, 마지막 저장, 돈)
//   접속키초기화 <이름>      잊은 접속키를 지운다. 그 사람이 다음에 넣는 접속키가 새 접속키가 된다 (돈·짐은 그대로)
//   지우기 <이름…>          계정을 지운다. 먼저 지울 목록만 보여 주고, --yes를 붙이면 실제로 지운다
//   지우기 --앞 계정시험     이름이 "계정시험"으로 시작하는 계정 모두 (시험 계정 정리)
//   내보내기 <이름>          그 사람의 접속을 끊는다 (게임이 다시 접속하지 않는다)
//   알림 "<글>"              접속한 모두에게 알림 (120자까지)
//   저장기록 <이름>          그 사람의 게임 저장 기록 (지금 것 + 예전 것 최근 5개, 번호 포함)
//   되돌리기 <이름> <번호>    예전 게임 저장으로 되돌린다. 그 사람이 다시 로그인하면 "서버 것 / 이 컴퓨터 것" 고르는 창이 뜬다
// 영어 명령도 된다: status · accounts · reset · delete (--prefix) · kick · notice · saves · restore
// 관리 키: 환경 변수 ADMIN_KEY, 없으면 Server/.dev.vars의 ADMIN_KEY. 올린 서버에도 같은 값을 넣어 둔다
//   (npx wrangler secret put ADMIN_KEY)
import { readFileSync } from "node:fs";

const LIVE = "https://pocket-caravan-server.baribari.workers.dev";
const LOCAL = "http://127.0.0.1:8787";

const args = process.argv.slice(2);
const flags = new Set();
const opts = {};
const words = [];
for (let i = 0; i < args.length; i++) {
  const a = args[i];
  if (a === "--server" || a === "--prefix" || a === "--앞") opts[a === "--앞" ? "--prefix" : a] = args[++i] ?? "";
  else if (a.startsWith("--")) flags.add(a);
  else words.push(a);
}
const base = (opts["--server"] || (flags.has("--local") ? LOCAL : LIVE)).replace(/\/+$/, "").replace(/^ws/, "http").replace(/\/ws$/, "");
const [cmd, ...rest] = words;

function adminKey() {
  if (process.env.ADMIN_KEY) return process.env.ADMIN_KEY.trim();
  try {
    const text = readFileSync(new URL("../.dev.vars", import.meta.url), "utf8");
    const m = text.match(/^\s*ADMIN_KEY\s*=\s*"?([^"\r\n]+)"?\s*$/m);
    if (m) return m[1].trim();
  } catch { /* 파일 없음 */ }
  return "";
}

async function call(method, path, body) {
  const key = adminKey();
  if (!key) fail("관리 키가 없어요. Server/.dev.vars에 ADMIN_KEY=… 를 넣거나 환경 변수 ADMIN_KEY를 정해 주세요");
  let res;
  try {
    res = await fetch(`${base}/admin/${path}`, {
      method,
      headers: { Authorization: `Bearer ${key}`, "content-type": "application/json" },
      body: body ? JSON.stringify(body) : undefined,
    });
  } catch (e) {
    fail(`서버에 닿지 않아요 (${base}): ${e.message}`);
  }
  let data;
  try { data = await res.json(); } catch { data = { error: `답이 JSON이 아니에요 (${res.status})` }; }
  if (!res.ok) fail(`${data.error ?? "실패"} (${res.status})`);
  return data;
}

function fail(why) {
  console.error(`실패: ${why}`);
  process.exit(1);
}

const when = (ms) => {
  if (!ms) return "-";
  const d = new Date(ms);
  const p = (n) => String(n).padStart(2, "0");
  return `${p(d.getMonth() + 1)}/${p(d.getDate())} ${p(d.getHours())}:${p(d.getMinutes())}`;
};

function table(rows, head) {
  const width = (s) => [...String(s)].reduce((w, ch) => w + (/[ᄀ-ￜ]/.test(ch) ? 2 : 1), 0);
  const all = [head, ...rows];
  const cols = head.map((_, i) => Math.max(...all.map((r) => width(r[i]))));
  for (const r of all) console.log(r.map((c, i) => String(c) + " ".repeat(cols[i] - width(c))).join("  ").trimEnd());
}

console.log(`서버: ${base}`);
switch (cmd) {
  case "상태": case "status": {
    const s = await call("GET", "status");
    console.log(`접속 ${s.online}명 · 계정 ${s.accounts}개 · 공유 장터 거래 ${s.tradeSeq}번째까지 (남겨 둔 것 ${s.tradesKept}건, 마지막 ${when(s.lastTradeAt)})`);
    if (s.usage) console.log(`오늘(${s.usage.day} UTC) 사용량 어림: ${s.usage.estimate.toLocaleString()} / ${s.usage.limit.toLocaleString()}번 `
      + `(${(s.usage.estimate / s.usage.limit * 100).toFixed(1)}% · 접속 ${s.usage.opens} · 메시지 ${s.usage.msgs} · 관리 ${s.usage.admin})`);
    if (s.players.length) table(s.players.map((p) => [p.name, p.id, p.account ? "계정" : "손님", p.look || "-", p.map || "-", `${p.quietSec}초`]),
      ["이름", "번호", "종류", "출신", "맵", "조용한 시간"]);
    break;
  }
  case "계정": case "accounts": {
    const s = await call("GET", "accounts");
    console.log(`계정 ${s.count}개`);
    if (s.count) table(s.accounts.map((a) => [a.name, a.id, when(a.created), when(a.savedAt), a.money ?? "-", a.kinds,
      a.fullAt ? `${a.fullDay}일째 ${when(a.fullAt)}` : "-", (a.online ? "접속 중 " : "") + (a.needsPass ? "접속키 새로 정하는 중" : "")]),
      ["이름", "번호", "만든 날", "마지막 저장", "돈", "짐 종류", "게임 저장", "상태"]);
    break;
  }
  case "접속키초기화": case "reset": {
    if (!rest[0]) fail("이름을 넣어 주세요: 접속키초기화 <이름>");
    const r = await call("POST", "reset", { name: rest[0] });
    console.log(`'${r.name}' 접속키를 지웠어요. 그 사람이 다음에 이 이름으로 들어올 때 넣는 접속키가 새 접속키가 돼요 (돈·짐은 그대로).`);
    console.log("그 전에 다른 사람이 같은 이름으로 먼저 들어오면 그 사람 접속키가 되니, 바로 들어오라고 알려 주세요.");
    break;
  }
  case "지우기": case "delete": {
    const body = opts["--prefix"] !== undefined ? { prefix: opts["--prefix"] } : { names: rest };
    if (!opts["--prefix"] && rest.length === 0) fail("지울 이름을 넣어 주세요: 지우기 <이름…> 또는 지우기 --앞 <이름 앞부분>");
    const yes = flags.has("--yes");
    const r = await call("POST", "delete", { ...body, confirm: yes });
    const list = yes ? r.deleted : r.match;
    if (list.length === 0) { console.log("맞는 계정이 없어요."); break; }
    console.log(`${yes ? "지운" : "지울"} 계정 ${list.length}개: ${list.join(", ")}`);
    if (!yes) console.log("정말 지우려면 같은 명령 끝에 --yes를 붙여 주세요. 지운 계정의 돈·짐 저장은 되살릴 수 없어요.");
    break;
  }
  case "내보내기": case "kick": {
    if (!rest[0]) fail("이름을 넣어 주세요: 내보내기 <이름>");
    const r = await call("POST", "kick", { name: rest[0] });
    console.log(r.kicked ? `'${rest[0]}' 접속을 끊었어요 (${r.kicked}개).` : `'${rest[0]}'(이)라는 접속자가 없어요.`);
    break;
  }
  case "알림": case "notice": {
    const text = rest.join(" ");
    if (!text) fail('알림 글을 넣어 주세요: 알림 "5분 뒤 서버 점검"');
    const r = await call("POST", "notice", { text });
    console.log(`${r.sent}명에게 보냈어요: ${r.text}`);
    break;
  }
  case "저장기록": case "saves": {
    if (!rest[0]) fail("이름을 넣어 주세요: 저장기록 <이름>");
    const r = await call("POST", "saves", { name: rest[0] });
    const row = (f) => [`${f.day}일째`, `${f.money}냥`, f.map || "-", when(f.saved)];
    if (!r.current) { console.log(`'${r.name}'은(는) 서버에 게임 저장이 없어요.`); break; }
    table([["지금", ...row(r.current)], ...r.history.map((x) => [`${x.slot}번`, ...row(x)])], ["번호", "날", "돈", "맵", "올린 때"]);
    if (r.history.length) console.log(`되돌리려면: 되돌리기 ${r.name} <번호>`);
    break;
  }
  case "되돌리기": case "restore": {
    const slot = Number(rest[1]);
    if (!rest[0] || !Number.isInteger(slot)) fail("이름과 번호를 넣어 주세요: 되돌리기 <이름> <번호>  (번호는 저장기록에서)");
    const r = await call("POST", "restore", { name: rest[0], slot });
    console.log(`'${r.name}' 게임 저장을 ${r.day}일째 · ${r.money}냥 (${r.map || "-"})으로 되돌렸어요. 지금 것도 기록에 남겼어요.`);
    console.log("그 사람이 게임을 다시 켜서 로그인하면 고르는 창이 떠요. \"서버 것으로 하기\"를 고르면 돼요.");
    break;
  }
  default:
    console.log(readFileSync(new URL(import.meta.url), "utf8").split("\n").filter((l) => l.startsWith("//")).map((l) => l.slice(3)).join("\n"));
    if (cmd) process.exit(1);
}
