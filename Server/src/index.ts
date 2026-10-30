/**
 * Pocket Caravan 온라인 서버 입구 (2026-10-30).
 * - GET /    : 서버가 살아 있는지 확인용 글자
 * - GET /ws  : 게임 접속(WebSocket). 지금은 세계 방 하나("world")로 모두 보낸다.
 * 나중에 사람이 늘면 맵마다 방을 나눈다(idFromName(mapId)).
 */
import { WorldRoom } from "./world";

export { WorldRoom };

export interface Env {
  WORLD: DurableObjectNamespace<WorldRoom>;
}

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);
    if (url.pathname === "/ws") {
      if (request.headers.get("Upgrade") !== "websocket") {
        return new Response("WebSocket으로만 접속할 수 있습니다", { status: 426 });
      }
      const room = env.WORLD.get(env.WORLD.idFromName("world"));
      return room.fetch(request);
    }
    if (url.pathname === "/") {
      return new Response("Pocket Caravan server OK");
    }
    return new Response("Not found", { status: 404 });
  },
} satisfies ExportedHandler<Env>;
