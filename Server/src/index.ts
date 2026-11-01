/**
 * Pocket Caravan 온라인 서버 입구 (2026-10-30).
 * - GET /    : 서버가 살아 있는지 확인용 글자
 * - GET /ws  : 게임 접속(WebSocket). 지금은 세계 방 하나("world")로 모두 보낸다.
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
    if (url.pathname === "/") {
      return new Response("Pocket Caravan server OK");
    }
    return new Response("Not found", { status: 404 });
  },
} satisfies ExportedHandler<Env>;
