/**
 * Pocket Caravan 온라인 서버 입구 (2026-10-30).
 * - GET /    : 서버가 살아 있는지 확인용 글자
 * - GET /ws  : 게임 접속(WebSocket). 지금은 세계 방 하나("world")로 모두 보낸다.
 * - /admin/… : 관리 도구 (2026-11-05). "Authorization: Bearer <ADMIN_KEY>"가 맞을 때만 세계 방으로 보낸다.
 *              ADMIN_KEY는 올린 서버는 `npx wrangler secret put ADMIN_KEY`, 내 컴퓨터는 Server/.dev.vars에 둔다.
 *              없으면 관리 기능이 꺼진다. 쓰는 법은 tools/admin.mjs, 요청 목록은 src/world.ts 9단계.
 * 나중에 사람이 늘면 맵마다 방을 나눈다(idFromName(mapId)).
 * 2026-10-30: 누군가 계속 접속해 있으면 방이 쉬지 않아 새로 올린 코드가 반영되지 않았다.
 *   그래서 방 이름을 "world-2"로 옮기고(예전 "world" 방은 남은 사람이 나가면 저절로 꺼진다),
 *   요청마다 올린 코드 시각(x-pc-version)을 실어 보내 방이 예전 코드면 스스로 다시 켜지게 했다.
 */
import { WorldRoom } from "./world";

export { WorldRoom };

export interface Env {
  WORLD: DurableObjectNamespace<WorldRoom>;
  VERSION: WorkerVersionMetadata;
  /** 관리 키 (16자 이상). 없으면 관리 기능이 꺼진다 */
  ADMIN_KEY?: string;
}

/** 세계 방 이름 */
const ROOM = "world-2";

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);
    if (url.pathname === "/ws") {
      if (request.headers.get("Upgrade") !== "websocket") {
        return new Response("WebSocket으로만 접속할 수 있습니다", { status: 426 });
      }
      const headers = new Headers(request.headers);
      headers.set("x-pc-version", env.VERSION?.timestamp ?? "");
      const forwarded = new Request(request, { headers });
      const id = env.WORLD.idFromName(ROOM);
      try {
        return await env.WORLD.get(id).fetch(forwarded);
      } catch {
        return await env.WORLD.get(id).fetch(forwarded);   // 방이 새 코드로 다시 켜지는 중이었다: 한 번 더
      }
    }
    if (url.pathname.startsWith("/admin/")) {
      if (!env.ADMIN_KEY || env.ADMIN_KEY.length < 16) return json({ error: "관리 기능이 꺼져 있어요 (ADMIN_KEY 없음)" }, 404);
      if (!(await sameSecret(request.headers.get("Authorization") ?? "", `Bearer ${env.ADMIN_KEY}`))) {
        return json({ error: "관리 키가 틀렸어요" }, 401);
      }
      if (request.method !== "GET" && request.method !== "POST") return json({ error: "GET이나 POST만 받아요" }, 405);
      const headers = new Headers(request.headers);
      headers.delete("Authorization");
      headers.set("x-pc-version", env.VERSION?.timestamp ?? "");
      const body = request.method === "POST" ? await request.text() : undefined;
      const make = () => new Request(url.toString(), { method: request.method, headers, body });
      const id = env.WORLD.idFromName(ROOM);
      try {
        return await env.WORLD.get(id).fetch(make());
      } catch {
        return await env.WORLD.get(id).fetch(make());
      }
    }
    if (url.pathname === "/") {
      return new Response("Pocket Caravan server OK");
    }
    return new Response("Not found", { status: 404 });
  },
} satisfies ExportedHandler<Env>;

function json(body: unknown, status: number): Response {
  return new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json; charset=utf-8" } });
}

/** 관리 키 비교: 길이·글자에 따라 걸리는 시간이 달라지지 않게 해시끼리 비교한다 */
async function sameSecret(a: string, b: string): Promise<boolean> {
  const enc = new TextEncoder();
  const [x, y] = await Promise.all([crypto.subtle.digest("SHA-256", enc.encode(a)), crypto.subtle.digest("SHA-256", enc.encode(b))]);
  return crypto.subtle.timingSafeEqual(x, y);
}
