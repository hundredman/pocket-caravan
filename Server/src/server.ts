/**
 * HTTP + WebSocket 서버 (2026-11-28 AWS 이전: Cloudflare Worker 입구를 Node.js http 서버 + ws 라이브러리로).
 * - GET /     : 살아 있는지 확인용 글자
 * - GET /ws   : 게임 접속(WebSocket). 세계 방 하나(WorldRoom)로 모두 보낸다
 * - /admin/…  : 관리 도구. "Authorization: Bearer <ADMIN_KEY>"가 맞을 때만 (쓰는 법은 tools/admin.mjs, 요청 목록은 world.ts 9단계부터)
 * - 매일 한국 새벽 3시(UTC 18시): 자동 백업
 * 모든 일은 room.run 줄에 세워 하나씩 처리한다 (world.ts run 설명).
 * 암호화(wss)는 앞의 Caddy가 풀어서 넘긴다. 이 서버는 보통 127.0.0.1에서 ws로만 받는다 (deploy/Caddyfile).
 */
import http from "node:http";
import { createHash, timingSafeEqual } from "node:crypto";
import { WebSocketServer, type WebSocket, type RawData } from "ws";
import type { WorldRoom } from "./world.js";

export interface ServerOptions {
  port: number;
  host: string;
  /** 관리 키 (16자 이상). 없으면 관리 기능이 꺼진다 */
  adminKey: string;
  /** 시험용: GET /test/scheduled 가 매일 자동 백업을 바로 부른다 (npm run dev에서만 켠다) */
  testScheduled?: boolean;
}

/** 관리 요청 본문 최대 크기 (되살리기는 나눠 보낸다) */
const ADMIN_BODY_MAX = 32 * 1024 * 1024;
/** 메시지 한 개 최대 크기 (Cloudflare와 같은 1MB. 게임 저장 메시지가 12만 자 남짓) */
const WS_MAX = 1024 * 1024;
/** 매일 자동 백업 시각: UTC 18시 = 한국 새벽 3시 (Cloudflare cron "0 18 * * *"과 같다) */
const BACKUP_HOUR_UTC = 18;

export function startServer(room: WorldRoom, opts: ServerOptions): Promise<{ close(): Promise<void> }> {
  const http1 = http.createServer((req, res) => {
    handle(room, opts, req, res).catch((e) => {
      console.error("요청 처리 실패", e);
      if (!res.headersSent) void write(res, json({ error: "서버 오류" }, 500));
      else res.end();
    });
  });

  const wss = new WebSocketServer({ noServer: true, maxPayload: WS_MAX });
  http1.on("upgrade", (req, socket, head) => {
    if (new URL(req.url ?? "/", "http://localhost").pathname !== "/ws") { socket.destroy(); return; }
    wss.handleUpgrade(req, socket, head, (ws) => connect(room, ws));
  });

  let daily: ReturnType<typeof setTimeout> | undefined;
  const scheduleDaily = () => {
    const now = new Date();
    const next = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate(), BACKUP_HOUR_UTC));
    if (next.getTime() <= now.getTime()) next.setUTCDate(next.getUTCDate() + 1);
    daily = setTimeout(() => { void dailyBackup(room).finally(scheduleDaily); }, next.getTime() - now.getTime());
  };
  scheduleDaily();

  return new Promise((resolve) => {
    http1.listen(opts.port, opts.host, () => {
      resolve({
        close: async () => {
          clearTimeout(daily);
          http1.close();
          await room.run(() => room.shutdown());
          await new Promise((r) => setTimeout(r, 300));   // 1012 닫기 알림이 나갈 틈
          for (const ws of wss.clients) ws.terminate();
        },
      });
    });
  });
}

/** 접속 하나: 열림 → 메시지들 → 닫힘을 차례로 줄에 세운다 */
function connect(room: WorldRoom, ws: WebSocket): void {
  const log = (what: string) => (e: unknown) => console.error(what, e);
  void room.run(() => room.open(ws)).catch(log("접속 처리 실패"));
  ws.on("message", (data: RawData, isBinary: boolean) => {
    const buf = Buffer.isBuffer(data) ? data : Array.isArray(data) ? Buffer.concat(data) : Buffer.from(data);
    const msg = isBinary ? buf.buffer.slice(buf.byteOffset, buf.byteOffset + buf.byteLength) as ArrayBuffer : buf.toString("utf8");
    void room.run(() => room.webSocketMessage(ws, msg)).catch(log("메시지 처리 실패"));
  });
  ws.on("close", () => { void room.run(() => room.webSocketClose(ws)).catch(log("닫힘 처리 실패")); });
  ws.on("error", (e) => console.error("접속 오류", e.message));   // 뒤이어 close가 온다
}

async function handle(room: WorldRoom, opts: ServerOptions, req: http.IncomingMessage, res: http.ServerResponse): Promise<void> {
  const url = new URL(req.url ?? "/", "http://localhost");
  if (url.pathname === "/ws") return write(res, new Response("WebSocket으로만 접속할 수 있습니다", { status: 426 }));
  if (url.pathname.startsWith("/admin/")) {
    if (opts.adminKey.length < 16) return write(res, json({ error: "관리 기능이 꺼져 있어요 (ADMIN_KEY 없음)" }, 404));
    if (!sameSecret(req.headers.authorization ?? "", `Bearer ${opts.adminKey}`)) return write(res, json({ error: "관리 키가 틀렸어요" }, 401));
    if (req.method !== "GET" && req.method !== "POST") return write(res, json({ error: "GET이나 POST만 받아요" }, 405));
    const body = req.method === "POST" ? await readBody(req) : undefined;
    if (body === null) return write(res, json({ error: "요청이 너무 커요" }, 413));
    const request = new Request(url.toString(), { method: req.method, body });
    const what = url.pathname.slice("/admin/".length);
    return write(res, await room.run(() => room.adminRequest(request, what)));
  }
  if (opts.testScheduled && url.pathname === "/test/scheduled") {
    return write(res, await dailyBackup(room) ? new Response("ok") : json({ error: "자동 백업 실패" }, 500));
  }
  if (url.pathname === "/") return write(res, new Response("Pocket Caravan server OK"));
  return write(res, new Response("Not found", { status: 404 }));
}

/** 매일 자동 백업. 실패하면 서버 기록(journalctl)에 남긴다 */
async function dailyBackup(room: WorldRoom): Promise<boolean> {
  try {
    const res = await room.run(() => room.cronBackup());
    const text = await res.text();
    if (!res.ok) { console.error(`자동 백업 실패 (${res.status}): ${text}`); return false; }
    console.log(`자동 백업: ${text}`);
    return true;
  } catch (e) {
    console.error("자동 백업 실패", e);
    return false;
  }
}

async function write(res: http.ServerResponse, r: Response): Promise<void> {
  const body = Buffer.from(await r.arrayBuffer());
  res.writeHead(r.status, { "content-type": r.headers.get("content-type") ?? "text/plain; charset=utf-8", "content-length": body.length });
  res.end(body);
}

function json(body: unknown, status: number): Response {
  return new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json; charset=utf-8" } });
}

/** 요청 본문 (너무 크면 null) */
function readBody(req: http.IncomingMessage): Promise<string | null> {
  return new Promise((resolve, reject) => {
    const parts: Buffer[] = [];
    let size = 0;
    req.on("data", (b: Buffer) => {
      size += b.length;
      if (size > ADMIN_BODY_MAX) { resolve(null); req.destroy(); return; }
      parts.push(b);
    });
    req.on("end", () => resolve(Buffer.concat(parts).toString("utf8")));
    req.on("error", reject);
  });
}

/** 관리 키 비교: 길이·글자에 따라 걸리는 시간이 달라지지 않게 해시끼리 비교한다 */
function sameSecret(a: string, b: string): boolean {
  const x = createHash("sha256").update(a).digest(), y = createHash("sha256").update(b).digest();
  return timingSafeEqual(x, y);
}
