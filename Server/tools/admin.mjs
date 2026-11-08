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
//   백업                    서버 저장소를 통째로 Server/backups/에 파일로 받는다 (--out <파일>로 위치를 바꿀 수 있다)
//                           접속키 해시도 들어 있으니 깃에 올리거나 남에게 보내지 않는다 (backups/는 깃이 무시한다)
//   되살리기 <백업 파일>     백업 파일을 서버에 다시 넣는다. 먼저 바뀔 것만 보여 주고, --yes를 붙이면 실제로 넣는다
//                           넣기 전에 지금 서버를 backups/에 먼저 백업한다. 접속한 사람은 잠깐 끊겼다가 다시 접속한다
//                           백업 뒤에 생긴 계정 등 백업에 없는 것은 그대로 둔다 (--똑같이: 그것도 지워 백업과 똑같게)
//                           오늘 사용량(usage)은 되살리지 않는다
//   되살리기 <백업 파일> --이름 <이름>   그 계정(돈·짐·게임 저장·저장 기록)만 되살린다. 지운 계정 되살리기 (순위표 기록은 안 돌아온다)
//   자동백업                 서버가 매일 한국 새벽 3시에 스스로 해 두는 백업 목록 (최근 7일치)
//   자동백업 지금            자동 백업을 지금 한 번 한다 (오늘 것을 덮는다)
//   자동백업 받기 <날짜>      그날 자동 백업을 backups/에 파일로 받는다. 받은 파일은 되살리기에 그대로 쓴다
// 영어 명령도 된다: status · accounts · reset · delete (--prefix) · kick · notice · saves · restore · backup · load (--name, --exact) · auto (now · get)
// 관리 키: 환경 변수 ADMIN_KEY, 없으면 Server/.dev.vars의 ADMIN_KEY. 올린 서버에도 같은 값을 넣어 둔다
//   (npx wrangler secret put ADMIN_KEY)
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import { gunzipSync } from "node:zlib";

const LIVE = "https://pocket-caravan-server.baribari.workers.dev";
const LOCAL = "http://127.0.0.1:8787";
/** 백업 파일을 두는 곳 (시험은 PC_BACKUP_DIR로 바꾼다) */
const BACKUP_DIR = process.env.PC_BACKUP_DIR || fileURLToPath(new URL("../backups/", import.meta.url));
/** 되살리기: 한 번에 보내는 글자 수 */
const LOAD_CHARS = 3_000_000;

const args = process.argv.slice(2);
const flags = new Set();
const opts = {};
const words = [];
const optAlias = { "--앞": "--prefix", "--이름": "--name" };
const flagAlias = { "--똑같이": "--exact" };
for (let i = 0; i < args.length; i++) {
  const a = args[i];
  if (["--server", "--prefix", "--앞", "--out", "--name", "--이름"].includes(a)) opts[optAlias[a] ?? a] = args[++i] ?? "";
  else if (a.startsWith("--")) flags.add(flagAlias[a] ?? a);
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

/** 관리 요청 하나 (답을 그대로) */
async function request(method, path, body) {
  const key = adminKey();
  if (!key) fail("관리 키가 없어요. Server/.dev.vars에 ADMIN_KEY=… 를 넣거나 환경 변수 ADMIN_KEY를 정해 주세요");
  try {
    return await fetch(`${base}/admin/${path}`, {
      method,
      headers: { Authorization: `Bearer ${key}`, "content-type": "application/json" },
      body: body ? JSON.stringify(body) : undefined,
    });
  } catch (e) {
    fail(`서버에 닿지 않아요 (${base}): ${e.message}`);
  }
}

/** 관리 요청 하나 (JSON 답. 실패면 끝낸다) */
async function call(method, path, body) {
  const res = await request(method, path, body);
  let data;
  try { data = await res.json(); } catch { data = { error: `답이 JSON이 아니에요 (${res.status})` }; }
  if (!res.ok) fail(`${data.error ?? "실패"} (${res.status})`);
  return data;
}

/** 서버 저장소를 통째로 받는다 (서버가 나눠 주는 만큼 이어 받는다) */
async function download() {
  const entries = {};
  let after = "", at = 0, pages = 0;
  do {
    const r = await call("GET", "backup" + (after ? `?after=${encodeURIComponent(after)}` : ""));
    at ||= r.at;
    for (const [k, v] of r.entries) entries[k] = v;
    after = r.next ?? "";
    if (++pages > 10000) fail("백업이 끝나지 않아요 (서버 답이 이상해요)");
  } while (after);
  return { data: { kind: "pocket-caravan-backup", v: 1, server: base, at, count: Object.keys(entries).length, entries }, pages };
}

/** 백업을 파일로 쓴다 (out이 없으면 backups/backup-서버-[tag-]시각.json) */
function writeBackup(data, tag, out) {
  const p = (n) => String(n).padStart(2, "0");
  const d = new Date(data.at || Date.now());
  const stamp = `${d.getFullYear()}${p(d.getMonth() + 1)}${p(d.getDate())}-${p(d.getHours())}${p(d.getMinutes())}${p(d.getSeconds())}`;
  const where = base === LIVE ? "live" : base === LOCAL ? "local" : "server";
  if (!out) mkdirSync(BACKUP_DIR, { recursive: true });
  const file = out || join(BACKUP_DIR, `backup-${where}${tag ? "-" + tag : ""}-${stamp}.json`);
  const text = JSON.stringify(data);
  writeFileSync(file, text);
  return { file, size: kb(Buffer.byteLength(text)) };
}

/** 백업 안에 든 것 한 줄 */
function describe(entries) {
  const keys = Object.keys(entries);
  const count = (pre) => keys.filter((k) => k.startsWith(pre)).length;
  return `계정 ${count("acct:")}개 · 게임 저장 ${count("full:")}개 · 저장 기록 ${count("fullv:")}개 · 순위표 판 ${count("board:")}개 · `
    + `거래 기록 ${entries.trades?.length ?? 0}건 · 사용량 ${count("usage:")}일치`;
}

const kb = (bytes) => bytes >= 1024 * 1024 ? `${(bytes / 1024 / 1024).toFixed(1)}MB` : `${Math.max(1, Math.round(bytes / 1024))}KB`;
const SECRET_NOTE = "이 파일에는 접속키 해시도 들어 있어요. 깃에 올리거나 남에게 보내지 마세요.";

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
  case "백업": case "backup": {
    const { data, pages } = await download();
    const { file, size } = writeBackup(data, "", opts["--out"]);
    console.log(`백업했어요: ${file} (${size}, 키 ${data.count}개${pages > 1 ? `, ${pages}번에 나눠 받음` : ""})`);
    console.log(describe(data.entries));
    console.log(SECRET_NOTE);
    break;
  }
  case "되살리기": case "load": {
    if (!rest[0]) fail("백업 파일을 넣어 주세요: 되살리기 <백업 파일> [--이름 <이름>] [--똑같이] [--yes]");
    let backup;
    try { backup = JSON.parse(readFileSync(rest[0], "utf8")); } catch (e) { fail(`백업 파일을 읽지 못했어요: ${e.message}`); }
    if (backup?.kind !== "pocket-caravan-backup" || backup.v !== 1 || !backup.entries || typeof backup.entries !== "object") fail("바리바리 서버 백업 파일이 아니에요");
    const who = opts["--name"] !== undefined ? opts["--name"].trim().toLowerCase() : null;
    if (who === "") fail("--이름 뒤에 계정 이름을 넣어 주세요");
    // 한 계정만이면 그 계정 키만. 사용량은 되살리지 않는다 (지금 센 것이 맞다)
    const pick = (k) => !k.startsWith("usage:") && (who === null || k === `acct:${who}` || k === `full:${who}` || k.startsWith(`fullv:${who}:`));
    const want = Object.entries(backup.entries).filter(([k]) => pick(k));
    if (who !== null && !want.some(([k]) => k === `acct:${who}`)) fail(`백업에 '${opts["--name"]}' 계정이 없어요`);
    const { data: now } = await download();
    const cur = new Map(Object.entries(now.entries).filter(([k]) => pick(k)));
    const add = want.filter(([k]) => !cur.has(k));
    const change = want.filter(([k, v]) => cur.has(k) && JSON.stringify(cur.get(k)) !== JSON.stringify(v));
    const wanted = new Set(want.map(([k]) => k));
    const extra = [...cur.keys()].filter((k) => !wanted.has(k));
    const del = who !== null || flags.has("--exact") ? extra : [];
    const names = (list) => list.filter(([k]) => k.startsWith("acct:")).map(([, v]) => v?.name ?? "?");

    console.log(`백업 파일: ${rest[0]} (${when(backup.at)}에 받은 것, 키 ${Object.keys(backup.entries).length}개)`);
    console.log(`되살릴 것: ${who === null ? "서버 전체" : `'${backup.entries[`acct:${who}`].name}' 계정`}`);
    console.log(`새로 넣을 키 ${add.length}개 · 바꿀 키 ${change.length}개 · 같은 키 ${want.length - add.length - change.length}개 · 지울 키 ${del.length}개`);
    if (names(add).length) console.log(`  다시 생기는 계정: ${names(add).join(", ")}`);
    if (names(change).length) console.log(`  백업 때로 돌아가는 계정: ${names(change).join(", ")}`);
    if (del.length && who === null) console.log(`  지우는 계정: ${del.filter((k) => k.startsWith("acct:")).map((k) => cur.get(k)?.name ?? k).join(", ") || "(없음)"}`);
    if (who === null && !flags.has("--exact") && extra.length)
      console.log(`  백업에 없는 키 ${extra.length}개(백업 뒤에 생긴 계정 등)는 그대로 둬요. 그것도 지우려면 --똑같이`);
    if (add.length + change.length + del.length === 0) { console.log("서버가 이미 백업과 같아요."); break; }
    if (!flags.has("--yes")) {
      console.log("정말 되살리려면 같은 명령 끝에 --yes를 붙여 주세요. 넣기 전에 지금 서버를 먼저 백업해 둬요.");
      break;
    }

    const safe = writeBackup(now, "before-load");
    console.log(`되살리기 전 지금 서버를 백업했어요: ${safe.file}`);
    const todo = [...add, ...change];
    let sent = 0, batch = [], chars = 0;
    for (const e of todo) {
      const n = JSON.stringify(e).length;
      if (batch.length && chars + n > LOAD_CHARS) {
        sent += (await call("POST", "load", { confirm: true, put: batch })).put;
        batch = [];
        chars = 0;
      }
      batch.push(e);
      chars += n;
    }
    const r = await call("POST", "load", { confirm: true, put: batch, del, done: true, ...(who !== null ? { names: [who] } : {}) });
    sent += r.put;
    console.log(`되살렸어요: 넣은 키 ${sent}개 · 지운 키 ${r.del}개 · 다시 접속하게 한 사람 ${r.closed}명`);
    break;
  }
  case "자동백업": case "auto": {
    const sub = rest[0] ?? "";
    if (sub === "지금" || sub === "now") {
      const r = await call("POST", "autobackup");
      console.log(`자동 백업을 지금 했어요: ${r.day} (키 ${r.keys}개, 압축해서 ${kb(r.bytes)}${r.parts > 1 ? `, ${r.parts}칸` : ""}${r.removed ? `, 오래된 ${r.removed}일치 지움` : ""})`);
    } else if (sub === "받기" || sub === "get") {
      const day = rest[1] ?? "";
      if (!/^\d{4}-\d{2}-\d{2}$/.test(day)) fail("날짜를 넣어 주세요: 자동백업 받기 2026-11-08  (날짜는 '자동백업'으로 볼 수 있어요)");
      const res = await request("GET", `autobackup?day=${day}`);
      if (!res.ok) {
        let why = `실패 (${res.status})`;
        try { why = `${(await res.json()).error} (${res.status})`; } catch { /* JSON 아님 */ }
        fail(why);
      }
      const data = JSON.parse(gunzipSync(Buffer.from(await res.arrayBuffer())).toString("utf8"));
      const { file, size } = writeBackup(data, "auto", opts["--out"]);
      console.log(`${day} 자동 백업을 받았어요: ${file} (${size}, 키 ${data.count}개, ${when(data.at)}에 한 것)`);
      console.log(describe(data.entries));
      console.log(`되살리려면: 되살리기 "${file}"`);
      console.log(SECRET_NOTE);
    } else if (!sub) {
      const r = await call("GET", "autobackups");
      if (!r.days.length) { console.log("아직 자동 백업이 없어요. 매일 한국 새벽 3시에 해요 (지금 하려면: 자동백업 지금)"); break; }
      table(r.days.map((d) => [d.day, when(d.at), d.keys, kb(d.bytes)]), ["날짜(UTC)", "한 때", "키", "크기"]);
      console.log(`최근 ${r.keep}일치를 남겨요. 받으려면: 자동백업 받기 <날짜>`);
    } else {
      fail("자동백업 · 자동백업 지금 · 자동백업 받기 <날짜> 중 하나로 써 주세요");
    }
    break;
  }
  default:
    console.log(readFileSync(new URL(import.meta.url), "utf8").split("\n").filter((l) => l.startsWith("//")).map((l) => l.slice(3)).join("\n"));
    if (cmd) process.exit(1);
}
