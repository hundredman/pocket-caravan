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
 *  6단계 (2026-10-30) 끊겨도 다시 붙기 · 서버 보호
 *   hello에 "key"(게임을 켤 때마다 새로 만드는 비밀 글자)를 실어 보낸다. 같은 key로 다시 들어오면
 *   예전 접속을 닫고 예전 번호(id)를 그대로 준다. welcome의 "name"이 실제로 쓰인 이름이다(겹치면 "윤채(2)").
 *   게임 → 서버  {"t":"ping"}  →  서버 → 게임 {"t":"pong"}      5초마다. 방을 깨우지 않는 자동 응답이다
 *   30초 넘게 아무 소식이 없는 접속은 닫는다(와이파이가 끊긴 채 남은 유령 접속 정리).
 *   한 사람이 1초에 30개 넘게 보내면 넘친 것은 버리고, 90개를 넘으면 접속을 닫는다.
 *   서버가 닫을 때 이유(닫는 번호): 4001 너무 많이 보냄 · 4002 버전이 다름 · 4003 응답 없음 · 4004 같은 게임이 다시 접속함
 *  7단계 (2026-10-30) 채팅
 *   게임 → 서버  {"t":"say","text":"안녕"}                         80자까지, 1초에 1번
 *   서버 → 모두  {"t":"say","id":"p3","name":"윤채","map":"venice_rialto","text":"안녕"}   보낸 사람에게도 간다
 *  8단계 (2026-10-30) 접속키 로그인 · 돈·짐 저장 · 장터 공유 · 리액션
 *   hello에 "pass"(접속키 4~32자)를 넣으면 계정으로 들어온다. 처음 보는 이름이면 계정을 만들고, 있는 이름이면 접속키가 맞아야 한다.
 *   계정은 번호(id)가 늘 같고, 같은 계정이 다른 곳에서 들어오면 예전 접속을 4004로 닫는다. 접속키가 틀리면 4005로 닫는다.
 *   접속키가 없으면 손님(저장 없음). 계정이 있는 이름은 손님이 쓸 수 없다.
 *   welcome에 "account"(계정인지) · "hasSave" · "save"({money,items,counts,costs,at}) · "seq"(마지막 거래 번호)가 붙는다.
 *   게임 → 서버  {"t":"save","money":1200,"items":["silk"],"counts":[3],"costs":[90]}     계정만. 바뀌었을 때 몇 초에 한 번
 *   게임 → 서버  {"t":"trade","town":"venice_rialto","item":"silk","n":3,"d":2.5,"sell":true}   공유 장터에서 사고판 것 (d: 재고 변화)
 *   서버 → 다른 사람 모두  {"t":"trade","seq":12,"at":…,"id":"p3","name":"윤채","town":…,"item":…,"n":3,"d":2.5,"sell":true}
 *   hello의 "since"(이 게임이 마지막으로 받은 거래 번호, 처음이면 -1)보다 뒤의, 30분 안 남의 거래를 welcome 다음에 {"t":"trades","list":[…]}로 보낸다.
 *   게임 → 서버  {"t":"emote","e":"dance","name":"춤"}  →  서버 → 같은 맵  {"t":"emote","id":"p3","e":"dance","name":"춤"}
 */
import { DurableObject } from "cloudflare:workers";
import type { Env } from "./index";

export const PROTOCOL = 1;

/** 소식이 끊긴 지 이만큼 지나면 닫는다 */
const STALE_MS = 30_000;
/** 유령 접속을 살피는 간격 */
const SWEEP_MS = 10_000;
/** 1초에 받는 메시지 수: 넘치면 버리고, 크게 넘치면 닫는다 */
const RATE_SOFT = 30, RATE_HARD = 90;
/** 채팅: 글자 수 · 최소 간격 */
const SAY_MAX = 80, SAY_GAP_MS = 1000;
/** 메시지 한 개의 최대 글자 수 (짐 저장이 가장 길다) */
const MAX_CHARS = 16000;
/** 짐 저장: 물건 종류 수 */
const SAVE_ITEMS = 200;
/** 거래 기록: 남겨 두는 수 · 새로 들어온 사람에게 다시 보내 주는 기간 */
const TRADE_KEEP = 300, TRADE_REPLAY_MS = 30 * 60_000;
/** 리액션 최소 간격 */
const EMOTE_GAP_MS = 400;

export const Close = { TooMany: 4001, Version: 4002, Timeout: 4003, Replaced: 4004, Login: 4005 } as const;

/** 서버에 저장하는 돈·짐 (계정마다) */
interface SaveData { money: number; items: string[]; counts: number[]; costs: number[]; at: number; }

/** 계정 (저장소 키 "acct:" + 소문자 이름) */
interface Account { name: string; salt: string; hash: string; id: string; created: number; save?: SaveData; }

/** 공유 장터 거래 한 건 */
interface Trade { seq: number; at: number; id: string; name: string; town: string; item: string; n: number; d: number; sell: boolean; }

interface Player {
  id: string;
  key: string;
  /** 계정 저장소 키 ("" 이면 손님) */
  acct?: string;
  name: string;
  look: string;
  map: string;
  x: number;
  y: number;
  fx: number;
  fy: number;
  m: boolean;
  /** 마지막으로 메시지를 받은 시각 (ms) */
  heard: number;
  /** 서버가 닫기로 한 접속 (닫히는 중) */
  gone?: boolean;
}

export class WorldRoom extends DurableObject<Env> {
  /** 아이디 → 1초 칸의 시작 시각·받은 수. 방이 잠들면 지워져도 괜찮다 */
  private rate = new Map<string, { start: number; n: number; warned: boolean }>();
  private lastSay = new Map<string, number>();
  private lastEmote = new Map<string, number>();
  private lastSave = new Map<string, number>();

  constructor(ctx: DurableObjectState, env: Env) {
    super(ctx, env);
    // ping은 방을 깨우지 않고 Cloudflare가 바로 pong으로 답한다 (마지막 ping 시각은 따로 남는다)
    ctx.setWebSocketAutoResponse(new WebSocketRequestResponsePair('{"t":"ping"}', '{"t":"pong"}'));
  }

  async fetch(request: Request): Promise<Response> {
    // 입구(Worker)가 이 방보다 새 코드면, 이 방은 예전 코드로 돌고 있는 것이다. 스스로 다시 켜서 새 코드로 바꾼다.
    // (접속이 끊기면 게임이 저절로 다시 붙는다. 입구는 한 번 더 요청한다)
    const want = request.headers.get("x-pc-version") ?? "";
    const mine = this.env.VERSION?.timestamp ?? "";
    if (want && mine && Date.parse(want) > Date.parse(mine)) this.ctx.abort("새 코드로 다시 켭니다");

    const pair = new WebSocketPair();
    const [client, server] = Object.values(pair);
    this.ctx.acceptWebSocket(server);
    const n = ((await this.ctx.storage.get<number>("nextId")) ?? 0) + 1;
    await this.ctx.storage.put("nextId", n);
    const me: Player = { id: `p${n}`, key: "", name: "", look: "", map: "", x: 0, y: 0, fx: 0, fy: -1, m: false, heard: Date.now() };
    server.serializeAttachment(me);
    if ((await this.ctx.storage.getAlarm()) === null) await this.ctx.storage.setAlarm(Date.now() + SWEEP_MS);
    return new Response(null, { status: 101, webSocket: client });
  }

  async webSocketMessage(ws: WebSocket, data: string | ArrayBuffer): Promise<void> {
    const me = ws.deserializeAttachment() as Player;
    if (!me || me.gone) return;
    const now = Date.now();
    if (!this.allow(ws, me, now)) return;
    me.heard = now;

    const size = typeof data === "string" ? data.length : data.byteLength;
    if (size > MAX_CHARS) { ws.serializeAttachment(me); return send(ws, { t: "error", why: "메시지가 너무 깁니다" }); }
    let msg: any;
    try {
      msg = JSON.parse(typeof data === "string" ? data : new TextDecoder().decode(data));
    } catch {
      ws.serializeAttachment(me);
      return send(ws, { t: "error", why: "JSON이 아닙니다" });
    }
    switch (msg?.t) {
      case "hello": {
        if (msg.v !== PROTOCOL) {
          send(ws, { t: "error", why: `프로토콜 버전이 다릅니다 (서버 ${PROTOCOL}). 게임을 새로 받아 주세요` });
          return this.shut(ws, me, Close.Version, "version");
        }
        me.key = cleanKey(msg.key);
        const name = cleanName(msg.name);
        const pass = typeof msg.pass === "string" ? msg.pass : "";
        let acct: Account | undefined;
        if (name) {
          const ak = "acct:" + name.toLowerCase();
          acct = await this.ctx.storage.get<Account>(ak);
          if (pass) {
            if (pass.length < 4 || pass.length > 32) return this.refuse(ws, me, "접속키는 4~32자로 정해 주세요");
            if (acct) {
              if ((await hashKey(acct.salt, pass)) !== acct.hash) return this.refuse(ws, me, `'${acct.name}'은(는) 다른 접속키로 쓰고 있는 이름이에요`);
            } else {
              const salt = randomHex(16);
              acct = { name, salt, hash: await hashKey(salt, pass), id: me.id, created: now };
              await this.ctx.storage.put(ak, acct);
            }
            me.acct = ak;
            me.id = acct.id;
            me.name = acct.name;
          } else if (acct) {
            return this.refuse(ws, me, `'${acct.name}'은(는) 접속키가 있는 이름이에요. 접속키를 넣어 주세요`);
          }
        }
        if (me.key || me.acct) this.replaceOld(ws, me);
        if (!me.acct) me.name = this.uniqueName(name || me.id, ws);
        me.look = cleanMap(msg.look);
        ws.serializeAttachment(me);
        const seq = (await this.ctx.storage.get<number>("tradeSeq")) ?? 0;
        send(ws, {
          t: "welcome", v: PROTOCOL, id: me.id, name: me.name, online: this.count(), seq,
          account: !!me.acct, hasSave: !!acct?.save && !!me.acct, save: me.acct ? acct?.save ?? null : null,
        });
        this.broadcast({ t: "online", online: this.count() }, (o) => o !== ws);
        await this.replayTrades(ws, me, msg.since, seq, now);
        return;
      }
      case "save": {
        ws.serializeAttachment(me);
        if (!me.acct) return send(ws, { t: "error", why: "접속키로 들어와야 저장돼요" });
        if (now - (this.lastSave.get(me.id) ?? 0) < 1000) return;
        const save = cleanSave(msg, now);
        if (!save) return send(ws, { t: "error", why: "저장할 돈·짐 모양이 이상합니다" });
        this.lastSave.set(me.id, now);
        const acct = await this.ctx.storage.get<Account>(me.acct);
        if (!acct) return;
        acct.save = save;
        await this.ctx.storage.put(me.acct, acct);
        return send(ws, { t: "saved", at: save.at });
      }
      case "trade": {
        ws.serializeAttachment(me);
        const town = cleanMap(msg.town), item = cleanMap(msg.item);
        const n = Number.isInteger(msg.n) && msg.n >= 1 && msg.n <= 1000 ? msg.n as number : 0;
        const d = typeof msg.d === "number" && Number.isFinite(msg.d) && Math.abs(msg.d) <= 10000 ? Math.round(msg.d * 1000) / 1000 : null;
        if (!town || !item || !n || d === null) return send(ws, { t: "error", why: "거래 모양이 이상합니다" });
        const seq = ((await this.ctx.storage.get<number>("tradeSeq")) ?? 0) + 1;
        const trade: Trade = { seq, at: now, id: me.id, name: me.name, town, item, n, d, sell: msg.sell === true };
        const log = ((await this.ctx.storage.get<Trade[]>("trades")) ?? []).concat(trade).slice(-TRADE_KEEP);
        await this.ctx.storage.put({ tradeSeq: seq, trades: log });
        return this.broadcast({ t: "trade", ...trade }, (o) => o !== ws);
      }
      case "emote": {
        ws.serializeAttachment(me);
        const e = cleanMap(msg.e);
        if (!e || !me.map || now - (this.lastEmote.get(me.id) ?? 0) < EMOTE_GAP_MS) return;
        this.lastEmote.set(me.id, now);
        return this.toMap(me.map, { t: "emote", id: me.id, e, name: cleanName(msg.name) }, ws);
      }
      case "enter": {
        const map = cleanMap(msg.map);
        if (!map) { ws.serializeAttachment(me); return send(ws, { t: "error", why: "맵 이름이 이상합니다" }); }
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
        const x = num(msg.x), y = num(msg.y);
        if (!me.map || x === null || y === null) { ws.serializeAttachment(me); return; }
        me.x = x;
        me.y = y;
        me.fx = dir(msg.fx);
        me.fy = dir(msg.fy);
        me.m = msg.m === true;
        ws.serializeAttachment(me);
        this.toMap(me.map, { t: "pos", id: me.id, x: me.x, y: me.y, fx: me.fx, fy: me.fy, m: me.m }, ws);
        return;
      }
      case "say": {
        ws.serializeAttachment(me);
        const text = cleanText(msg.text);
        if (!text) return;
        if (now - (this.lastSay.get(me.id) ?? 0) < SAY_GAP_MS) return send(ws, { t: "error", why: "조금 천천히 말해 주세요" });
        this.lastSay.set(me.id, now);
        return this.broadcast({ t: "say", id: me.id, name: me.name, map: me.map, text }, () => true);
      }
      case "ping":   // 자동 응답이 못 받은 경우(글자가 조금 다를 때)
        ws.serializeAttachment(me);
        return send(ws, { t: "pong" });
      case "who": {
        ws.serializeAttachment(me);
        const players = this.live().map(([, p]) => ({ id: p.id, name: p.name, map: p.map }));
        return send(ws, { t: "who", players });
      }
      default:
        ws.serializeAttachment(me);
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

  /** 10초마다: 30초 넘게 소식 없는 접속(ping 포함)을 닫는다. 아무도 없으면 쉰다 */
  async alarm(): Promise<void> {
    const now = Date.now();
    for (const [s, p] of this.live()) {
      const pinged = this.ctx.getWebSocketAutoResponseTimestamp(s)?.getTime() ?? 0;
      if (now - Math.max(p.heard, pinged) > STALE_MS) this.shut(s, p, Close.Timeout, "timeout");
    }
    if (this.live().length > 0) await this.ctx.storage.setAlarm(Date.now() + SWEEP_MS);
  }

  /** 로그인 실패: 이유를 알리고 4005로 닫는다 (게임은 다시 접속하지 않는다) */
  private refuse(ws: WebSocket, me: Player, why: string): void {
    send(ws, { t: "error", why });
    this.shut(ws, me, Close.Login, "login");
  }

  /** 들어온 사람이 아직 못 받은 남의 거래(30분 안)를 한 번에 보낸다 */
  private async replayTrades(ws: WebSocket, me: Player, since: unknown, seq: number, now: number): Promise<void> {
    let from = typeof since === "number" && Number.isInteger(since) ? since : -1;
    if (from > seq) from = -1;   // 서버 기록이 지워졌다
    const log = (await this.ctx.storage.get<Trade[]>("trades")) ?? [];
    const list = log.filter((t) => t.seq > from && now - t.at < TRADE_REPLAY_MS && t.id !== me.id);
    if (list.length > 0) send(ws, { t: "trades", list });
  }

  /** 1초 칸마다 받은 수를 센다. 넘치면 false(버림), 크게 넘치면 닫는다 */
  private allow(ws: WebSocket, me: Player, now: number): boolean {
    let r = this.rate.get(me.id);
    if (!r || now - r.start >= 1000) this.rate.set(me.id, r = { start: now, n: 0, warned: false });
    r.n++;
    if (r.n > RATE_HARD) { this.shut(ws, me, Close.TooMany, "too_many"); return false; }
    if (r.n > RATE_SOFT) {
      if (!r.warned) { r.warned = true; send(ws, { t: "error", why: "메시지를 너무 많이 보냅니다" }); }
      return false;
    }
    return true;
  }

  /**
   * 같은 게임(key)이나 같은 계정으로 남아 있던 예전 접속을 닫는다.
   * 손님은 예전 번호를 이어받고, 계정은 원래 계정 번호를 쓴다.
   */
  private replaceOld(ws: WebSocket, me: Player): void {
    for (const [s, p] of this.live()) {
      if (s === ws) continue;
      const sameGame = !!me.key && p.key === me.key;
      const sameAcct = !!me.acct && p.acct === me.acct;
      if (!sameGame && !sameAcct) continue;
      if (!me.acct) { this.rate.delete(me.id); me.id = p.id; }
      this.shut(s, p, Close.Replaced, "replaced");
    }
  }

  /** 다른 사람이 이미 쓰는 이름이면 뒤에 (2), (3)… 을 붙인다 (16자 안에서) */
  private uniqueName(name: string, ws: WebSocket): string {
    const taken = new Set(this.live().filter(([s]) => s !== ws).map(([, p]) => p.name.toLowerCase()));
    if (!taken.has(name.toLowerCase())) return name;
    for (let i = 2; ; i++) {
      const tail = `(${i})`;
      const candidate = [...name].slice(0, 16 - tail.length).join("") + tail;
      if (!taken.has(candidate.toLowerCase())) return candidate;
    }
  }

  /** 서버 쪽에서 접속을 닫는다. 맵 사람들과 접속자 수를 바로 맞춘다 */
  private shut(ws: WebSocket, me: Player, code: number, reason: string): void {
    this.left(ws);
    me.gone = true;
    try { ws.serializeAttachment(me); } catch { /* 이미 닫힘 */ }
    try { ws.close(code, reason); } catch { /* 이미 닫힘 */ }
  }

  /** 접속이 끊긴 사람을 그 맵 사람들에게 알리고 접속자 수를 갱신한다 (한 번만) */
  private left(ws: WebSocket): void {
    const me = ws.deserializeAttachment() as Player | null;
    if (!me || me.gone) return;
    this.rate.delete(me.id);
    this.lastSay.delete(me.id);
    this.lastEmote.delete(me.id);
    this.lastSave.delete(me.id);
    if (me.map) this.toMap(me.map, { t: "leave", id: me.id }, ws);
    this.broadcast({ t: "online", online: this.count(ws) }, (o) => o !== ws);
  }

  /** 살아 있는 접속 (서버가 닫는 중인 것은 뺀다) */
  private live(): [WebSocket, Player][] {
    const out: [WebSocket, Player][] = [];
    for (const s of this.ctx.getWebSockets()) {
      const p = s.deserializeAttachment() as Player | null;
      if (p && !p.gone) out.push([s, p]);
    }
    return out;
  }

  private count(except?: WebSocket): number {
    return this.live().filter(([s]) => s !== except).length;
  }

  /** 같은 맵에 있는 다른 사람들 (except 소켓은 뺀다) */
  private peers(map: string, except: WebSocket): Player[] {
    return this.live().filter(([s, p]) => s !== except && p.map === map).map(([, p]) => p);
  }

  private toMap(map: string, msg: unknown, except: WebSocket): void {
    const json = JSON.stringify(msg);
    for (const [s, p] of this.live()) if (s !== except && p.map === map) sendRaw(s, json);
  }

  private broadcast(msg: unknown, filter: (s: WebSocket) => boolean): void {
    const json = JSON.stringify(msg);
    for (const [s] of this.live()) if (filter(s)) sendRaw(s, json);
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

/** 화면에 그대로 찍히는 글: 제어 문자는 빈칸으로, 꺾쇠(<>)는 글자 꾸미기 태그가 되지 않게 바꾼다 */
function plain(s: string): string {
  return s.replace(/[\p{Cc}\p{Cf}]/gu, " ").replace(/</g, "‹").replace(/>/g, "›").replace(/\s+/g, " ").trim();
}

/** 이름: 16자까지 */
function cleanName(name: unknown): string {
  return typeof name === "string" ? [...plain(name)].slice(0, 16).join("").trim() : "";
}

/** 채팅: 80자까지 */
function cleanText(text: unknown): string {
  return typeof text === "string" ? [...plain(text)].slice(0, SAY_MAX).join("").trim() : "";
}

/** 저장할 돈·짐: 모양이 맞을 때만 */
function cleanSave(m: any, now: number): SaveData | null {
  const money = m?.money;
  if (!Number.isInteger(money) || money < 0 || money > 2147483647) return null;
  const items = m.items, counts = m.counts, costs = m.costs;
  if (!Array.isArray(items) || !Array.isArray(counts) || !Array.isArray(costs)) return null;
  if (items.length > SAVE_ITEMS || items.length !== counts.length || items.length !== costs.length) return null;
  for (let i = 0; i < items.length; i++) {
    if (!cleanMap(items[i])) return null;
    if (!Number.isInteger(counts[i]) || counts[i] < 1 || counts[i] > 1_000_000) return null;
    if (typeof costs[i] !== "number" || !Number.isFinite(costs[i]) || costs[i] < 0 || costs[i] > 1e9) return null;
  }
  return { money, items, counts, costs: costs.map((c: number) => Math.round(c * 100) / 100), at: now };
}

/** 접속키 확인용: 소금 + 접속키의 SHA-256 (접속키 자체는 저장하지 않는다) */
async function hashKey(salt: string, pass: string): Promise<string> {
  const buf = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(`${salt}:${pass}`));
  return [...new Uint8Array(buf)].map((b) => b.toString(16).padStart(2, "0")).join("");
}

function randomHex(bytes: number): string {
  const a = new Uint8Array(bytes);
  crypto.getRandomValues(a);
  return [...a].map((b) => b.toString(16).padStart(2, "0")).join("");
}

/** 다시 접속 확인용 비밀 글자: 영문·숫자 16~64자 */
function cleanKey(key: unknown): string {
  return typeof key === "string" && /^[A-Za-z0-9]{16,64}$/.test(key) ? key : "";
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
