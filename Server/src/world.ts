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
 *   (2026-11-08) enter에도 "look"을 실을 수 있다. 시작 화면에서 로그인하면 hello의 look은 아직 고르기 전 기본 출신이라
 *   맵에 들어갈 때(출신이 바뀌면 같은 맵에 다시 enter) 고친다. 같은 맵 사람들에게는 join으로 새 생김새가 간다
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
 *  9단계 (2026-11-05) 관리 도구. 입구(index.ts)가 관리 키를 확인한 HTTP 요청만 여기 admin()으로 온다
 *   GET  /admin/status    접속자(이름·맵·계정인지·조용한 시간) · 계정 수 · 거래 기록
 *   GET  /admin/accounts  계정 목록 (접속키·소금은 빼고)
 *   POST /admin/reset     {"name":"윤채"}  접속키를 지운다. 다음에 들어올 때 넣는 접속키가 새 접속키가 된다(돈·짐·번호는 그대로)
 *   POST /admin/delete    {"names":[…]} 또는 {"prefix":"계정시험"}  "confirm":true가 없으면 지울 목록만 알려 준다
 *   POST /admin/kick      {"name":"윤채"}  접속을 4006으로 닫는다
 *   POST /admin/notice    {"text":"5분 뒤 서버 점검"}  →  서버 → 모두  {"t":"notice","text":…}
 *   welcome의 "newPass": 접속키를 지운 계정에 새 접속키를 정하고 들어왔다
 *   (11단계) welcome의 "extras":true = 이 서버가 순위표·선물을 안다. 게임은 이게 있을 때만 score·getBoards·gift를 보낸다
 *  10단계 (2026-11-07) 게임 저장 통째로 (수레·창고·평판·퀘스트까지). 돈·짐 저장(8단계)도 예전 게임을 위해 그대로 둔다
 *   welcome에 "fullOk":true(이 서버가 통째 저장을 받는다)와, 있으면 "full":{at,h,day,money,map,origin,size}(요약만)가 붙는다
 *   게임 → 서버  {"t":"full","at":…,"h":"sha256","zip":"gzip+base64","day":12,"money":1200,"map":"venice_rialto","origin":"venice"}
 *                계정만. 5초에 한 번까지. at은 그 컴퓨터에서 저장 파일을 쓴 시각, h는 저장 글(JSON)의 SHA-256
 *   서버 → 게임  {"t":"fullSaved","at":…,"h":…}
 *   게임 → 서버  {"t":"getFull"}  →  서버 → 게임  {"t":"fullData","at":…,"h":…,"zip":…}  (없으면 zip이 "")
 *   서버는 압축을 풀지 않는다. 모양(base64·길이)만 본다
 *  11단계 (2026-11-07) 무료 플랜 안에서 더한 것
 *   저장 기록: 게임 저장을 덮을 때 10분에 한 번(또는 날짜가 줄면 = 새로 시작) 예전 것을 "fullv:이름:칸"에 남긴다 (최근 5개)
 *     POST /admin/saves {"name"} 기록 보기 · POST /admin/restore {"name","slot"} 되돌리기 (지금 것도 기록에 남긴다.
 *     되돌린 저장은 at이 지금이 되어, 다음 로그인 때 게임이 "서버 것 / 이 컴퓨터 것" 고르는 창을 띄운다)
 *   순위표: 게임 → 서버 {"t":"score","board":"gondola","value":123}  계정만, 판마다 사람별 최고점 (위 20명)
 *           서버 → 게임 {"t":"scored","board","value","best","rank"}  (rank 0 = 20위 밖)
 *           게임 → 서버 {"t":"getBoards"}  →  {"t":"boards","boards":[{id,rows:[{name,value}]}]}  "money" 판은 계정 저장의 돈으로 만든다
 *   선물: 게임 → 서버 {"t":"gift","gid":"a1","to":"은샘","money":100} 또는 {…,"item":"silk","n":3,"cost":270}
 *         서버 → 받는 사람 {"t":"gift","gid":"p3:a1","from":"윤채",…}  →  받는 사람 → 서버 {"t":"giftAck","gid":"p3:a1","ok":true}
 *         서버 → 보낸 사람 {"t":"giftResult","gid":"a1","ok":…,"why":…,"to":…}  (받는 사람이 없거나 10초 안에 답이 없으면 ok:false)
 *         보낸 게임은 보낼 때 돈·짐을 빼 두고, ok:false면 돌려받는다
 *   사용량: 접속·메시지·관리 요청 수를 날마다(UTC) 세어 GET /admin/status의 "usage"로 보여 준다 (무료 한도 하루 10만 번 어림)
 *  12단계 (2026-11-08) 백업 · 되살리기 · 자동 백업
 *   GET /admin/backup?after=<키>&limit=<수>  저장소를 키 순서대로 조금씩 통째로 준다 (계정의 접속키 해시·소금 포함)
 *     → {"at":…,"entries":[[키,값]…],"next":"마지막 키" 또는 null}   next가 null이 될 때까지 after=next로 이어 받는다
 *   POST /admin/load {"confirm":true,"put":[[키,값]…],"del":[키…],"done":true,"names":[이름…]}  백업을 되살린다
 *     put·del은 나눠 여러 번 보낼 수 있다. "nextId"는 줄이지 않는다(예전 번호를 새 손님에게 주면 계정 번호와 겹친다)
 *     done이면 접속한 사람(names가 있으면 그 계정만)에게 알리고 1012로 닫는다. 게임은 저절로 다시 접속해서 되살린 저장을 받는다
 *   자동 백업: 매일 한국 새벽 3시(cron, index.ts scheduled)에 저장소 통째로를 gzip해서 KV(BACKUPS) "auto:날짜:칸"에 둔다 (최근 7일치)
 *     GET /admin/autobackups 목록 · POST /admin/autobackup 지금 하기 · GET /admin/autobackup?day=2026-11-08 받기(gzip, 백업 파일과 같은 JSON)
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
/** 메시지 한 개의 최대 글자 수 (짐 저장이 가장 길다). 통째 저장(full)만 FULL_CHARS까지 */
const MAX_CHARS = 16000;
/** 통째 저장: 압축 글(base64) 최대 길이 (≈ 압축 90KB, 저장 JSON 몇백 KB) · 최소 간격 */
const FULL_ZIP_MAX = 120_000, FULL_CHARS = FULL_ZIP_MAX + 2000, FULL_GAP_MS = 5000;
/** 게임 저장 기록: 남겨 두는 수 · 새 기록을 남기는 최소 간격 */
const HISTORY_KEEP = 5, HISTORY_GAP_MS = 10 * 60_000;
/** 순위표: 판 종류 수 · 판마다 남기는 사람 수 · 보내 주는 사람 수 · 점수 최소 간격 */
const BOARD_MAX = 30, BOARD_KEEP = 20, BOARD_SHOW = 10, SCORE_GAP_MS = 1000;
/** 선물: 최소 간격 · 받는 쪽 답을 기다리는 시간 · 한 번에 보내는 최대 돈·개수 */
const GIFT_GAP_MS = 2000, GIFT_WAIT_MS = 10_000, GIFT_MONEY_MAX = 1_000_000, GIFT_COUNT_MAX = 1000;
/** 사용량을 저장소에 적는 간격 */
const USAGE_SAVE_MS = 60_000;
/** 백업: 한 번에 읽는 키 수 · 한 번에 보내는 글자 수 (게임 저장 하나가 12만 자까지라 나눠 보낸다) */
const BACKUP_PAGE = 100, BACKUP_CHARS = 4_000_000;
/** 자동 백업: 남기는 날 수 · KV 값 하나의 크기 (무료 KV는 값 하나 25MB까지라 넘으면 칸을 나눈다) */
const AUTO_KEEP = 7, AUTO_PART = 20 * 1024 * 1024;
/** 짐 저장: 물건 종류 수 */
const SAVE_ITEMS = 200;
/** 거래 기록: 남겨 두는 수 · 새로 들어온 사람에게 다시 보내 주는 기간 */
const TRADE_KEEP = 300, TRADE_REPLAY_MS = 30 * 60_000;
/** 리액션 최소 간격 */
const EMOTE_GAP_MS = 400;
/** 관리자 알림 글자 수 */
const NOTICE_MAX = 120;

export const Close = { TooMany: 4001, Version: 4002, Timeout: 4003, Replaced: 4004, Login: 4005, Admin: 4006 } as const;

/** 서버에 저장하는 돈·짐 (계정마다) */
interface SaveData { money: number; items: string[]; counts: number[]; costs: number[]; at: number; }

/** 통째 저장 요약 (계정에 같이 둔다). 압축 글은 "full:" + 소문자 이름에 따로 둔다 */
interface FullInfo { at: number; h: string; day: number; money: number; map: string; origin: string; size: number; saved: number; }

/** 남겨 둔 예전 게임 저장 (압축 글은 "fullv:" + 소문자 이름 + ":" + slot) */
interface FullHist extends FullInfo { slot: number; }

/** 계정 (저장소 키 "acct:" + 소문자 이름). hash가 ""이면 관리자가 접속키를 지운 것: 다음 접속키로 새로 정한다 */
interface Account {
  name: string; salt: string; hash: string; id: string; created: number; save?: SaveData; full?: FullInfo;
  /** 예전 게임 저장 요약 (최근 것이 앞) · 다음에 쓸 칸 */
  hist?: FullHist[]; histNext?: number;
}

/** 순위표 한 줄 (저장소 키 "board:" + 판 아이디, 판 목록은 "boards") */
interface BoardRow { name: string; value: number; at: number; }

/** 받는 사람의 답을 기다리는 선물 */
interface PendingGift { from: WebSocket; clientGid: string; to: WebSocket; toName: string; at: number; }

/** 자동 백업 한 날의 정보 (KV "auto:날짜:칸"마다 같은 값을 붙여 둔다) */
interface AutoMeta { at: number; keys: number; bytes: number; parts: number; }

/** 날마다(UTC) 센 사용량 (저장소 키 "usage:" + 날짜) */
interface Usage { day: string; opens: number; msgs: number; admin: number; }

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
  private lastFull = new Map<string, number>();
  private lastScore = new Map<string, number>();
  private lastGift = new Map<string, number>();
  /** "보낸 사람 번호:보낸 게임의 gid" → 받는 사람 답을 기다리는 선물. 방이 잠들면 지워진다(보낸 게임이 시간이 지나 돌려받는다) */
  private gifts = new Map<string, PendingGift>();
  /** 오늘 사용량 (저장소와 USAGE_SAVE_MS마다 맞춘다) */
  private usage: Usage = { day: "", opens: 0, msgs: 0, admin: 0 };
  private usageSaved = 0;

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

    const path = new URL(request.url).pathname;
    const inside = path === "/cron/backup";   // 입구의 scheduled만 보낸다 (입구는 /ws·/admin/만 방으로 넘긴다)
    await this.tally(path.startsWith("/admin/") || inside ? "admin" : "opens");
    if (path.startsWith("/admin/")) return this.admin(request, path.slice("/admin/".length));
    if (inside) return this.autoBackup();

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
    await this.tally("msgs");
    if (!this.allow(ws, me, now)) return;
    me.heard = now;

    const size = typeof data === "string" ? data.length : data.byteLength;
    if (size > FULL_CHARS) { ws.serializeAttachment(me); return send(ws, { t: "error", why: "메시지가 너무 깁니다" }); }
    let msg: any;
    try {
      msg = JSON.parse(typeof data === "string" ? data : new TextDecoder().decode(data));
    } catch {
      ws.serializeAttachment(me);
      return send(ws, { t: "error", why: "JSON이 아닙니다" });
    }
    if (size > MAX_CHARS && msg?.t !== "full") { ws.serializeAttachment(me); return send(ws, { t: "error", why: "메시지가 너무 깁니다" }); }
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
        let newPass = false;
        if (name) {
          const ak = "acct:" + name.toLowerCase();
          acct = await this.ctx.storage.get<Account>(ak);
          if (pass) {
            if (pass.length < 4 || pass.length > 32) return this.refuse(ws, me, "접속키는 4~32자로 정해 주세요");
            if (acct && !acct.hash) {
              // 관리자가 접속키를 지웠다: 이번에 넣은 접속키를 새 접속키로 정한다
              acct.salt = randomHex(16);
              acct.hash = await hashKey(acct.salt, pass);
              await this.ctx.storage.put(ak, acct);
              newPass = true;
            } else if (acct) {
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
            return this.refuse(ws, me, acct.hash
              ? `'${acct.name}'은(는) 접속키가 있는 이름이에요. 접속키를 넣어 주세요`
              : `'${acct.name}'의 접속키를 새로 정해야 해요. 쓰고 싶은 접속키를 넣어 주세요`);
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
          fullOk: true, extras: true, ...(me.acct && acct?.full ? { full: acct.full } : {}),
          ...(newPass ? { newPass: true } : {}),
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
      case "full": {
        ws.serializeAttachment(me);
        if (!me.acct) return send(ws, { t: "error", why: "접속키로 들어와야 저장돼요" });
        if (now - (this.lastFull.get(me.id) ?? 0) < FULL_GAP_MS) return send(ws, { t: "error", why: "게임 저장을 너무 자주 올립니다" });
        const zip = typeof msg.zip === "string" && msg.zip.length > 0 && msg.zip.length <= FULL_ZIP_MAX && /^[A-Za-z0-9+/]+={0,2}$/.test(msg.zip) ? msg.zip as string : "";
        const h = typeof msg.h === "string" && /^[0-9a-f]{64}$/.test(msg.h) ? msg.h as string : "";
        const at = Number.isInteger(msg.at) && msg.at > 0 && msg.at < 1e14 ? msg.at as number : 0;
        if (!zip || !h || !at) return send(ws, { t: "error", why: "게임 저장 모양이 이상합니다" });
        this.lastFull.set(me.id, now);
        const acct = await this.ctx.storage.get<Account>(me.acct);
        if (!acct) return;
        const int = (v: unknown) => Number.isInteger(v) && Math.abs(v as number) <= 2147483647 ? v as number : 0;
        const name = me.acct.slice("acct:".length);
        const puts: Record<string, unknown> = {};
        // 11단계 저장 기록: 덮기 전에 예전 것을 남긴다 (10분에 한 번, 또는 날짜가 줄었으면 = 새로 시작했으면 바로)
        const prev = acct.full, last = acct.hist?.[0];
        if (prev && prev.h !== h && (!last || last.h !== prev.h) && (int(msg.day) < prev.day || !last || prev.saved - last.saved >= HISTORY_GAP_MS)) {
          const prevZip = await this.ctx.storage.get<string>("full:" + name);
          if (prevZip) this.keepHistory(acct, name, prev, prevZip, puts);
        }
        acct.full = { at, h, day: int(msg.day), money: int(msg.money), map: cleanMap(msg.map), origin: cleanMap(msg.origin), size: zip.length, saved: now };
        await this.ctx.storage.put({ ...puts, [me.acct]: acct, ["full:" + name]: zip });
        return send(ws, { t: "fullSaved", at, h });
      }
      case "getFull": {
        ws.serializeAttachment(me);
        if (!me.acct) return send(ws, { t: "error", why: "접속키로 들어와야 불러올 수 있어요" });
        const acct = await this.ctx.storage.get<Account>(me.acct);
        const zip = (await this.ctx.storage.get<string>("full:" + me.acct.slice("acct:".length))) ?? "";
        return send(ws, { t: "fullData", at: acct?.full?.at ?? 0, h: acct?.full?.h ?? "", zip: acct?.full ? zip : "" });
      }
      case "score": {
        ws.serializeAttachment(me);
        if (!me.acct) return send(ws, { t: "error", why: "접속키로 들어와야 순위에 올라요" });
        const board = cleanBoard(msg.board);
        const value = Number.isInteger(msg.value) && msg.value >= 0 && msg.value <= 1e9 ? msg.value as number : -1;
        if (!board || board === "money" || value < 0) return send(ws, { t: "error", why: "점수 모양이 이상합니다" });
        if (now - (this.lastScore.get(me.id) ?? 0) < SCORE_GAP_MS) return;
        this.lastScore.set(me.id, now);
        const ids = (await this.ctx.storage.get<string[]>("boards")) ?? [];
        if (!ids.includes(board) && ids.length >= BOARD_MAX) return send(ws, { t: "error", why: "순위표 종류가 너무 많아요" });
        const rows = (await this.ctx.storage.get<BoardRow[]>("board:" + board)) ?? [];
        const mine = rows.find((r) => r.name.toLowerCase() === me.name.toLowerCase());
        if (!mine || value > mine.value) {
          const next = rows.filter((r) => r !== mine).concat({ name: me.name, value, at: now })
            .sort((a, b) => b.value - a.value || a.at - b.at).slice(0, BOARD_KEEP);
          await this.ctx.storage.put({ ["board:" + board]: next, ...(ids.includes(board) ? {} : { boards: ids.concat(board) }) });
          rows.splice(0, rows.length, ...next);
        }
        const rank = rows.findIndex((r) => r.name.toLowerCase() === me.name.toLowerCase()) + 1;
        return send(ws, { t: "scored", board, value, best: Math.max(value, mine?.value ?? 0), rank });
      }
      case "getBoards": {
        ws.serializeAttachment(me);
        const boards: { id: string; rows: { name: string; value: number }[] }[] = [];
        const money = [...(await this.accounts()).values()]
          .map((a) => ({ name: a.name, value: a.full?.money ?? a.save?.money ?? -1 }))
          .filter((r) => r.value >= 0).sort((a, b) => b.value - a.value).slice(0, BOARD_SHOW);
        boards.push({ id: "money", rows: money });
        for (const id of (await this.ctx.storage.get<string[]>("boards")) ?? []) {
          const rows = (await this.ctx.storage.get<BoardRow[]>("board:" + id)) ?? [];
          boards.push({ id, rows: rows.slice(0, BOARD_SHOW).map((r) => ({ name: r.name, value: r.value })) });
        }
        return send(ws, { t: "boards", boards });
      }
      case "gift": {
        ws.serializeAttachment(me);
        const clientGid = typeof msg.gid === "string" && /^[A-Za-z0-9]{1,32}$/.test(msg.gid) ? msg.gid as string : "";
        const to = cleanName(msg.to);
        const money = Number.isInteger(msg.money) && msg.money > 0 && msg.money <= GIFT_MONEY_MAX ? msg.money as number : 0;
        const item = money ? "" : cleanMap(msg.item);
        const n = Number.isInteger(msg.n) && msg.n >= 1 && msg.n <= GIFT_COUNT_MAX ? msg.n as number : 0;
        const cost = typeof msg.cost === "number" && Number.isFinite(msg.cost) && msg.cost >= 0 && msg.cost <= 1e9 ? Math.round(msg.cost * 100) / 100 : 0;
        if (!clientGid || !to || (!money && !(item && n))) return send(ws, { t: "error", why: "선물 모양이 이상합니다" });
        const fail = (why: string) => send(ws, { t: "giftResult", gid: clientGid, ok: false, why, to });
        if (now - (this.lastGift.get(me.id) ?? 0) < GIFT_GAP_MS) return fail("선물은 2초에 한 번만 보낼 수 있어요");
        this.lastGift.set(me.id, now);
        const target = this.live().find(([s, p]) => s !== ws && p.name.toLowerCase() === to.toLowerCase());
        if (!target) return fail(`'${to}' 님이 지금 접속해 있지 않아요`);
        const gid = `${me.id}:${clientGid}`;
        this.gifts.set(gid, { from: ws, clientGid, to: target[0], toName: target[1].name, at: now });
        send(target[0], { t: "gift", gid, from: me.name, item, n: money ? 0 : n, cost: money ? 0 : cost, money });
        return send(ws, { t: "giftSent", gid: clientGid, to: target[1].name });
      }
      case "giftAck": {
        ws.serializeAttachment(me);
        const g = typeof msg.gid === "string" ? this.gifts.get(msg.gid) : undefined;
        if (!g || g.to !== ws) return;
        this.gifts.delete(msg.gid);
        return send(g.from, { t: "giftResult", gid: g.clientGid, ok: msg.ok === true, why: cleanText(msg.why), to: me.name });
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
        const look = cleanMap(msg.look);
        if (look) me.look = look;
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
    for (const [gid, g] of this.gifts)   // 받는 사람이 답이 없는 선물: 보낸 사람이 돌려받게 한다
      if (now - g.at > GIFT_WAIT_MS) { this.gifts.delete(gid); send(g.from, { t: "giftResult", gid: g.clientGid, ok: false, why: "받는 쪽이 답이 없어요", to: g.toName }); }
    await this.saveUsage(false);
    if (this.live().length > 0) await this.ctx.storage.setAlarm(Date.now() + SWEEP_MS);
    else await this.saveUsage(true);
  }

  // ---------------- 관리 도구 (2026-11-05) ----------------

  /** 관리 요청 하나. 관리 키는 입구(index.ts)가 이미 확인했다 */
  private async admin(request: Request, what: string): Promise<Response> {
    let body: any = {};
    if (request.method === "POST") {
      const text = await request.text();   // 비어 있으면 {} (자동백업 지금처럼 넣을 것이 없는 요청)
      try { body = text ? JSON.parse(text) : {}; } catch { return reply({ error: "JSON이 아닙니다" }, 400); }
      if (!body || typeof body !== "object") body = {};
    }
    const now = Date.now();
    switch (`${request.method} ${what}`) {
      case "GET status": {
        const players = this.live().map(([s, p]) => {
          const pinged = this.ctx.getWebSocketAutoResponseTimestamp(s)?.getTime() ?? 0;
          return { id: p.id, name: p.name, map: p.map, look: p.look, account: !!p.acct, quietSec: Math.round((now - Math.max(p.heard, pinged)) / 1000) };
        });
        const trades = (await this.ctx.storage.get<Trade[]>("trades")) ?? [];
        return reply({
          online: players.length, players,
          accounts: (await this.accounts()).size,
          tradeSeq: (await this.ctx.storage.get<number>("tradeSeq")) ?? 0,
          tradesKept: trades.length, lastTradeAt: trades.at(-1)?.at ?? 0,
          // 무료 한도는 하루(UTC) 요청 10만 번. 웹소켓 메시지는 20개가 요청 1번으로 센다 (어림값: 방이 잠든 사이의 ping 자동 응답은 안 센다)
          usage: { ...this.usage, estimate: this.usage.opens + this.usage.admin + Math.ceil(this.usage.msgs / 20), limit: 100_000 },
        });
      }
      case "POST saves": {
        const k = acctKey(body.name);
        const acct = k ? await this.ctx.storage.get<Account>(k) : undefined;
        if (!acct) return reply({ error: `'${cleanName(body.name)}' 계정이 없어요` }, 404);
        const info = (f?: FullInfo) => f && { at: f.at, saved: f.saved, day: f.day, money: f.money, map: f.map, origin: f.origin, size: f.size };
        return reply({ name: acct.name, current: info(acct.full) ?? null, history: (acct.hist ?? []).map((x) => ({ slot: x.slot, ...info(x) })) });
      }
      case "POST restore": {
        const k = acctKey(body.name);
        const acct = k ? await this.ctx.storage.get<Account>(k) : undefined;
        if (!acct) return reply({ error: `'${cleanName(body.name)}' 계정이 없어요` }, 404);
        const pick = (acct.hist ?? []).find((x) => x.slot === body.slot);
        const name = k.slice("acct:".length);
        const zip = pick ? await this.ctx.storage.get<string>("fullv:" + name + ":" + pick.slot) : undefined;
        if (!pick || !zip) return reply({ error: "그 번호의 저장 기록이 없어요" }, 404);
        const puts: Record<string, unknown> = {};
        const curZip = await this.ctx.storage.get<string>("full:" + name);
        if (acct.full && curZip && acct.full.h !== pick.h) this.keepHistory(acct, name, acct.full, curZip, puts);   // 되돌리기도 되돌릴 수 있게
        const { slot: _slot, ...picked } = pick;
        acct.full = { ...picked, at: now, saved: now };   // 지금 시각으로: 다음 로그인 때 게임이 고르는 창을 띄운다
        await this.ctx.storage.put({ ...puts, [k]: acct, ["full:" + name]: zip });
        return reply({ ok: true, name: acct.name, day: picked.day, money: picked.money, map: picked.map });
      }
      case "GET accounts": {
        const online = new Set(this.live().map(([, p]) => p.acct ?? ""));
        const list = [...(await this.accounts())].map(([k, a]) => ({
          name: a.name, id: a.id, created: a.created, online: online.has(k), needsPass: !a.hash,
          savedAt: a.save?.at ?? 0, money: a.save?.money ?? null, kinds: a.save?.items.length ?? 0,
          fullAt: a.full?.saved ?? 0, fullDay: a.full?.day ?? null, fullMap: a.full?.map ?? "", fullSize: a.full?.size ?? 0,
        }));
        list.sort((a, b) => a.created - b.created);
        return reply({ count: list.length, accounts: list });
      }
      case "POST reset": {
        const k = acctKey(body.name);
        const acct = k ? await this.ctx.storage.get<Account>(k) : undefined;
        if (!acct) return reply({ error: `'${cleanName(body.name)}' 계정이 없어요` }, 404);
        acct.salt = "";
        acct.hash = "";
        await this.ctx.storage.put(k, acct);
        return reply({ ok: true, name: acct.name });
      }
      case "POST delete": {
        const all = await this.accounts();
        let keys: string[];
        if (Array.isArray(body.names)) {
          keys = body.names.map(acctKey).filter((k: string) => k && all.has(k));
        } else if (typeof body.prefix === "string") {
          const pre = cleanName(body.prefix).toLowerCase();
          if ([...pre].length < 2) return reply({ error: "prefix는 2자 이상으로 넣어 주세요" }, 400);
          keys = [...all.keys()].filter((k) => k.startsWith("acct:" + pre));
        } else {
          return reply({ error: "names(이름 목록)나 prefix(이름 앞부분)를 넣어 주세요" }, 400);
        }
        const names = keys.map((k) => all.get(k)!.name);
        if (body.confirm !== true) return reply({ dry: true, match: names });
        const gone = new Set(keys);
        for (const [s, p] of this.live()) if (p.acct && gone.has(p.acct)) this.kick(s, p, "계정이 지워져서 접속을 끊었어요");
        const withFull = keys.flatMap((k) => {
          const n = k.slice("acct:".length);
          return [k, "full:" + n, ...Array.from({ length: HISTORY_KEEP }, (_, i) => `fullv:${n}:${i}`)];
        });
        for (let i = 0; i < withFull.length; i += 128) await this.ctx.storage.delete(withFull.slice(i, i + 128));
        // 순위표에서도 뺀다
        const lower = new Set(names.map((n) => n.toLowerCase()));
        for (const id of (await this.ctx.storage.get<string[]>("boards")) ?? []) {
          const rows = (await this.ctx.storage.get<BoardRow[]>("board:" + id)) ?? [];
          const kept = rows.filter((r) => !lower.has(r.name.toLowerCase()));
          if (kept.length !== rows.length) await this.ctx.storage.put("board:" + id, kept);
        }
        return reply({ deleted: names });
      }
      case "POST kick": {
        const name = cleanName(body.name).toLowerCase();
        if (!name) return reply({ error: "name(이름)을 넣어 주세요" }, 400);
        let kicked = 0;
        for (const [s, p] of this.live()) if (p.name.toLowerCase() === name) { this.kick(s, p, "관리자가 접속을 끊었어요"); kicked++; }
        return reply({ kicked });
      }
      case "POST notice": {
        const text = typeof body.text === "string" ? [...plain(body.text)].slice(0, NOTICE_MAX).join("").trim() : "";
        if (!text) return reply({ error: "text(알림 글)가 비어 있어요" }, 400);
        this.broadcast({ t: "notice", text }, () => true);
        return reply({ sent: this.count(), text });
      }
      case "GET backup": {
        const q = new URL(request.url).searchParams;
        const after = q.get("after") ?? "";
        const limit = Math.min(BACKUP_PAGE, Math.max(1, Number.parseInt(q.get("limit") ?? "", 10) || BACKUP_PAGE));
        const page = await this.ctx.storage.list({ ...(after ? { startAfter: after } : {}), limit });
        const entries: [string, unknown][] = [];
        let chars = 0, cut = false;
        for (const [k, v] of page) {
          const len = k.length + (JSON.stringify(v)?.length ?? 0);
          if (entries.length > 0 && chars + len > BACKUP_CHARS) { cut = true; break; }
          entries.push([k, v]);
          chars += len;
        }
        const more = cut || page.size === limit;
        return reply({ at: now, entries, next: more && entries.length > 0 ? entries[entries.length - 1][0] : null });
      }
      case "POST load": {
        if (body.confirm !== true) return reply({ error: "되살리려면 confirm:true를 넣어 주세요" }, 400);
        const put: unknown[] = Array.isArray(body.put) ? body.put : [];
        const del: unknown[] = Array.isArray(body.del) ? body.del : [];
        const okKey = (k: unknown): k is string => typeof k === "string" && k.length > 0 && k.length <= 2048;
        if (!put.every((e) => Array.isArray(e) && e.length === 2 && okKey(e[0]))) return reply({ error: "put은 [[키,값]…] 모양이어야 해요" }, 400);
        if (!del.every(okKey)) return reply({ error: "del은 [키…] 모양이어야 해요" }, 400);
        const nextId = (await this.ctx.storage.get<number>("nextId")) ?? 0;
        const puts = (put as [string, unknown][]).map(([k, v]): [string, unknown] => [k, k === "nextId" ? Math.max(nextId, Number(v) || 0) : v]);
        for (let i = 0; i < puts.length; i += 128) await this.ctx.storage.put(Object.fromEntries(puts.slice(i, i + 128)));
        for (let i = 0; i < del.length; i += 128) await this.ctx.storage.delete((del as string[]).slice(i, i + 128));
        let closed = 0;
        if (body.done === true) {
          const only = Array.isArray(body.names) ? new Set(body.names.map(acctKey).filter(Boolean)) : null;
          for (const [s, p] of this.live()) {
            if (only && !(p.acct && only.has(p.acct))) continue;
            send(s, { t: "notice", text: "관리자가 서버 저장을 되살렸어요. 잠깐 끊겼다가 다시 접속해요" });
            this.shut(s, p, 1012, "restored");
            closed++;
          }
        }
        return reply({ ok: true, put: puts.length, del: del.length, closed });
      }
      case "GET autobackups": {
        if (!this.env.BACKUPS) return reply({ error: "자동 백업 저장소(KV)가 없어요" }, 404);
        const days = (await this.autoDays()).map((d) => ({ day: d.day, ...(d.meta ?? { at: 0, keys: 0, bytes: 0, parts: d.keys.length }) }));
        return reply({ keep: AUTO_KEEP, days });
      }
      case "POST autobackup":
        return this.autoBackup();
      case "GET autobackup": {
        const kv = this.env.BACKUPS;
        if (!kv) return reply({ error: "자동 백업 저장소(KV)가 없어요" }, 404);
        const day = new URL(request.url).searchParams.get("day") ?? "";
        if (!/^\d{4}-\d{2}-\d{2}$/.test(day)) return reply({ error: "day=2026-11-08 모양으로 날짜를 넣어 주세요" }, 400);
        const first = await kv.getWithMetadata<AutoMeta>(`auto:${day}:0`, "arrayBuffer");
        if (!first.value) return reply({ error: `${day} 자동 백업이 없어요` }, 404);
        const bufs = [first.value];
        for (let i = 1; i < (first.metadata?.parts ?? 1); i++) {
          const b = await kv.get(`auto:${day}:${i}`, "arrayBuffer");
          if (!b) return reply({ error: `${day} 자동 백업의 ${i}번째 칸이 없어요` }, 500);
          bufs.push(b);
        }
        return new Response(new Blob(bufs), { headers: { "content-type": "application/gzip" } });
      }
      default:
        return reply({ error: `모르는 관리 요청: ${request.method} /admin/${what}` }, 404);
    }
  }

  /** 모든 계정 (저장소 키 → 계정) */
  private async accounts(): Promise<Map<string, Account>> {
    return this.ctx.storage.list<Account>({ prefix: "acct:" });
  }

  /** 자동 백업 하나: 저장소 통째로(백업 파일과 같은 JSON)를 gzip해서 KV에 둔다. 같은 날 다시 하면 덮고, 최근 AUTO_KEEP일치만 남긴다 */
  private async autoBackup(): Promise<Response> {
    const kv = this.env.BACKUPS;
    if (!kv) return reply({ error: "자동 백업 저장소(KV)가 없어요" }, 404);
    const now = Date.now();
    const entries = Object.fromEntries(await this.ctx.storage.list());
    const keys = Object.keys(entries).length;
    const json = JSON.stringify({ kind: "pocket-caravan-backup", v: 1, server: "auto", at: now, count: keys, entries });
    const zip = new Uint8Array(await new Response(new Blob([json]).stream().pipeThrough(new CompressionStream("gzip"))).arrayBuffer());
    const day = new Date(now).toISOString().slice(0, 10);
    const parts = Math.max(1, Math.ceil(zip.length / AUTO_PART));
    const meta: AutoMeta = { at: now, keys, bytes: zip.length, parts };
    for (let i = 0; i < parts; i++) await kv.put(`auto:${day}:${i}`, zip.subarray(i * AUTO_PART, (i + 1) * AUTO_PART), { metadata: meta });
    // 오래된 날을 지운다. 오늘 것을 덮으며 칸이 줄었으면 남은 칸도 지운다 (KV 목록은 몇십 초 늦을 수 있어 오늘은 늘 남긴다)
    const days = await this.autoDays();
    const keep = new Set([day, ...days.map((d) => d.day).filter((d) => d < day).slice(0, AUTO_KEEP - 1)]);
    let removed = 0;
    for (const d of days) {
      const gone = !keep.has(d.day) ? d.keys : d.day === day ? d.keys.filter((k) => Number(k.split(":")[2]) >= parts) : [];
      for (const k of gone) await kv.delete(k);
      if (!keep.has(d.day)) removed++;
    }
    return reply({ ok: true, day, keys, bytes: zip.length, parts, removed });
  }

  /** KV에 있는 자동 백업을 날짜별로 (최근 날이 앞) */
  private async autoDays(): Promise<{ day: string; meta: AutoMeta | null; keys: string[] }[]> {
    const byDay = new Map<string, { meta: AutoMeta | null; keys: string[] }>();
    let cursor: string | undefined;
    do {
      const page = await this.env.BACKUPS!.list<AutoMeta>({ prefix: "auto:", cursor });
      for (const k of page.keys) {
        const [, day, part] = k.name.split(":");
        const d = byDay.get(day) ?? { meta: null, keys: [] };
        d.keys.push(k.name);
        if (part === "0") d.meta = k.metadata ?? null;
        byDay.set(day, d);
      }
      cursor = page.list_complete ? undefined : page.cursor;
    } while (cursor);
    return [...byDay].map(([day, d]) => ({ day, ...d })).sort((a, b) => b.day.localeCompare(a.day));
  }

  /** 예전 게임 저장 하나를 기록 칸에 남긴다 (칸은 돌아가며 쓴다). 저장소에 쓸 것은 puts에 더하고 acct를 고친다 */
  private keepHistory(acct: Account, name: string, info: FullInfo, zip: string, puts: Record<string, unknown>): void {
    const slot = (acct.histNext ?? 0) % HISTORY_KEEP;
    acct.histNext = slot + 1;
    acct.hist = [{ ...info, slot }, ...(acct.hist ?? []).filter((x) => x.slot !== slot)].slice(0, HISTORY_KEEP);
    puts[`fullv:${name}:${slot}`] = zip;
  }

  /** 사용량 하나 세기. 날이 바뀌었거나 방이 새로 켜졌으면 저장소에 적어 둔 오늘 값부터 이어 센다 */
  private async tally(kind: "opens" | "msgs" | "admin"): Promise<void> {
    const day = new Date().toISOString().slice(0, 10);
    if (this.usage.day !== day) {
      if (this.usage.day) await this.saveUsage(true);
      this.usage = (await this.ctx.storage.get<Usage>("usage:" + day)) ?? { day, opens: 0, msgs: 0, admin: 0 };
      this.usageSaved = Date.now();
    }
    this.usage[kind]++;
  }

  /** 사용량을 저장소에 적는다 (now가 아니면 USAGE_SAVE_MS가 지났을 때만: 무료 한도의 '쓰기 수'를 아끼려고) */
  private async saveUsage(now: boolean): Promise<void> {
    if (!this.usage.day || (!now && Date.now() - this.usageSaved < USAGE_SAVE_MS)) return;
    this.usageSaved = Date.now();
    await this.ctx.storage.put("usage:" + this.usage.day, this.usage);
  }

  /** 관리자가 접속을 닫는다: 이유를 알리고 4006으로 닫는다 (게임은 다시 접속하지 않는다) */
  private kick(ws: WebSocket, me: Player, why: string): void {
    send(ws, { t: "error", why });
    this.shut(ws, me, Close.Admin, "admin");
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
    this.lastFull.delete(me.id);
    this.lastScore.delete(me.id);
    this.lastGift.delete(me.id);
    for (const [gid, g] of this.gifts) {
      if (g.to === ws) { this.gifts.delete(gid); send(g.from, { t: "giftResult", gid: g.clientGid, ok: false, why: "받는 사람이 접속을 끊었어요", to: g.toName }); }
      else if (g.from === ws) this.gifts.delete(gid);
    }
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

/** 관리 요청의 답 (JSON) */
function reply(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json; charset=utf-8" } });
}

/** 순위표 판 아이디: 영문 소문자·숫자·밑줄 32자까지 */
function cleanBoard(id: unknown): string {
  return typeof id === "string" && /^[a-z0-9_]{1,32}$/.test(id) ? id : "";
}

/** 이름 → 계정 저장소 키 ("" 이면 이름이 이상하다) */
function acctKey(name: unknown): string {
  const n = cleanName(name);
  return n ? "acct:" + n.toLowerCase() : "";
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
