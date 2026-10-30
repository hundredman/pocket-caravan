/**
 * 세계 방 (Durable Object 하나). 요청을 한 번에 하나씩 처리한다.
 * 쉬는 동안 방이 잠들어도(Hibernation) 접속은 유지되므로, 플레이어 정보는 소켓에 붙여 둔다(serializeAttachment).
 *
 * 메시지는 JSON 한 줄, "t"가 종류다.
 *  2단계 (2026-10-30)
 *   게임 → 서버  {"t":"hello","v":1,"name":"윤채"}
 *   서버 → 게임  {"t":"welcome","v":1,"id":"p3","online":2}
 *   서버 → 모두  {"t":"online","online":3}                       누가 들어오거나 나갈 때
 *   서버 → 게임  {"t":"error","why":"..."}
 *  4단계 (2026-10-30) 같은 맵 사람끼리만 위치를 주고받는다
 *   게임 → 서버  {"t":"enter","map":"venice_rialto"}             접속 직후·맵을 옮길 때
 *   서버 → 게임  {"t":"room","map":"venice_rialto","players":[{id,name,x,y,fx,fy,m}…]}   그 맵에 이미 있는 사람
 *   서버 → 그 맵  {"t":"join","id":"p3","name":"윤채","x":..,"y":..,"fx":0,"fy":-1,"m":false}
 *   서버 → 그 맵  {"t":"leave","id":"p3"}                         맵을 떠나거나 접속을 끊을 때
 *   게임 → 서버  {"t":"pos","x":12.5,"y":-3.25,"fx":1,"fy":0,"m":true}   움직이는 동안 1초에 10번, 가만히 있으면 1초에 1번
 *   서버 → 그 맵  {"t":"pos","id":"p3","x":..,"y":..,"fx":..,"fy":..,"m":..}
 *   게임 → 서버  {"t":"who"}  →  서버 → 게임 {"t":"who","players":[{id,name,map}…]}   접속자 목록(시험용)
 *  5단계 (2026-10-30) 생김새: hello에 "look"(출신 아이디: venice · goryeo · viking · arabia)을 실어 보내면
 *   room · join의 플레이어 정보에 "look"이 함께 간다. 게임이 그 출신의 캐릭터 그림으로 그린다.
 */
import { DurableObject } from "cloudflare:workers";
import type { Env } from "./index";

export const PROTOCOL = 1;

interface Player {
  id: string;
  name: string;
  look: string;
  map: string;
  x: number;
  y: number;
  fx: number;
  fy: number;
  m: boolean;
}

export class WorldRoom extends DurableObject<Env> {
  async fetch(_request: Request): Promise<Response> {
    const pair = new WebSocketPair();
    const [client, server] = Object.values(pair);
    this.ctx.acceptWebSocket(server);
    const n = ((await this.ctx.storage.get<number>("nextId")) ?? 0) + 1;
    await this.ctx.storage.put("nextId", n);
    const me: Player = { id: `p${n}`, name: "", look: "", map: "", x: 0, y: 0, fx: 0, fy: -1, m: false };
    server.serializeAttachment(me);
    return new Response(null, { status: 101, webSocket: client });
  }

  async webSocketMessage(ws: WebSocket, data: string | ArrayBuffer): Promise<void> {
    let msg: any;
    try {
      msg = JSON.parse(typeof data === "string" ? data : new TextDecoder().decode(data));
    } catch {
      return send(ws, { t: "error", why: "JSON이 아닙니다" });
    }
    const me = ws.deserializeAttachment() as Player;
    switch (msg?.t) {
      case "hello": {
        if (msg.v !== PROTOCOL) return send(ws, { t: "error", why: `프로토콜 버전이 다릅니다 (서버 ${PROTOCOL})` });
        me.name = cleanName(msg.name) || me.id;
        me.look = cleanMap(msg.look);
        ws.serializeAttachment(me);
        send(ws, { t: "welcome", v: PROTOCOL, id: me.id, online: this.count() });
        this.broadcast({ t: "online", online: this.count() }, (o) => o !== ws);
        return;
      }
      case "enter": {
        const map = cleanMap(msg.map);
        if (!map) return send(ws, { t: "error", why: "맵 이름이 이상합니다" });
        if (me.map && me.map !== map) this.toMap(me.map, { t: "leave", id: me.id }, ws);
        me.map = map;
        me.m = false;
        ws.serializeAttachment(me);
        const others = this.peers(map, ws).map(publicInfo);
        send(ws, { t: "room", map, players: others });
        this.toMap(map, { t: "join", ...publicInfo(me) }, ws);
        return;
      }
      case "pos": {
        if (!me.map) return;
        const x = num(msg.x), y = num(msg.y);
        if (x === null || y === null) return;
        me.x = x;
        me.y = y;
        me.fx = dir(msg.fx);
        me.fy = dir(msg.fy);
        me.m = msg.m === true;
        ws.serializeAttachment(me);
        this.toMap(me.map, { t: "pos", id: me.id, x: me.x, y: me.y, fx: me.fx, fy: me.fy, m: me.m }, ws);
        return;
      }
      case "who": {
        const players = this.ctx.getWebSockets().map((s) => s.deserializeAttachment() as Player)
          .map((p) => ({ id: p.id, name: p.name, map: p.map }));
        return send(ws, { t: "who", players });
      }
      default:
        return send(ws, { t: "error", why: `모르는 메시지: ${String(msg?.t)}` });
    }
  }

  async webSocketClose(ws: WebSocket, code: number, reason: string): Promise<void> {
    this.left(ws);
    try { ws.close(code, reason); } catch { /* 이미 닫힘 */ }
  }

  async webSocketError(ws: WebSocket): Promise<void> {
    this.left(ws);
  }

  /** 접속이 끊긴 사람을 그 맵 사람들에게 알리고 접속자 수를 갱신한다 */
  private left(ws: WebSocket): void {
    const me = ws.deserializeAttachment() as Player | null;
    if (me?.map) this.toMap(me.map, { t: "leave", id: me.id }, ws);
    this.broadcast({ t: "online", online: this.count(ws) }, (o) => o !== ws);
  }

  private count(except?: WebSocket): number {
    return this.ctx.getWebSockets().filter((s) => s !== except).length;
  }

  /** 같은 맵에 있는 다른 사람들 (except 소켓은 뺀다) */
  private peers(map: string, except: WebSocket): Player[] {
    return this.ctx.getWebSockets().filter((s) => s !== except)
      .map((s) => s.deserializeAttachment() as Player)
      .filter((p) => p && p.map === map);
  }

  private toMap(map: string, msg: unknown, except: WebSocket): void {
    const json = JSON.stringify(msg);
    for (const s of this.ctx.getWebSockets()) {
      if (s === except) continue;
      const p = s.deserializeAttachment() as Player | null;
      if (p?.map === map) sendRaw(s, json);
    }
  }

  private broadcast(msg: unknown, filter: (s: WebSocket) => boolean): void {
    const json = JSON.stringify(msg);
    for (const s of this.ctx.getWebSockets()) if (filter(s)) sendRaw(s, json);
  }
}

function publicInfo(p: Player) {
  return { id: p.id, name: p.name, look: p.look, x: p.x, y: p.y, fx: p.fx, fy: p.fy, m: p.m };
}

function send(ws: WebSocket, msg: unknown): void {
  sendRaw(ws, JSON.stringify(msg));
}

function sendRaw(ws: WebSocket, json: string): void {
  try { ws.send(json); } catch { /* 끊긴 소켓 */ }
}

/** 이름: 앞뒤 공백을 자르고 16자까지 */
function cleanName(name: unknown): string {
  return typeof name === "string" ? name.trim().slice(0, 16) : "";
}

/** 맵·출신 아이디: 영문·숫자·밑줄만, 64자까지 (고려 일부 맵은 Gaegyeong처럼 대문자로 시작) */
function cleanMap(map: unknown): string {
  return typeof map === "string" && /^[A-Za-z0-9_]{1,64}$/.test(map) ? map : "";
}

/** 좌표: 유한한 숫자만, 소수 둘째 자리까지 */
function num(v: unknown): number | null {
  return typeof v === "number" && Number.isFinite(v) && Math.abs(v) < 100000 ? Math.round(v * 100) / 100 : null;
}

/** 바라보는 방향 한 축: -1 · 0 · 1 */
function dir(v: unknown): number {
  return v === 1 || v === -1 ? v : 0;
}
