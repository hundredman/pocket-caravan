// 서버 약속 시험 (가짜 플레이어 셋). 사용: npm run dev 를 켠 상태에서 npm run test:two [주소]
// 2단계: 서로 다른 번호 · 4단계: 같은 맵 사람에게만 입장·위치·퇴장이 전달되는지
const URL = process.argv[2] ?? "ws://127.0.0.1:8787/ws";
let failed = 0;
const check = (ok, what) => { console.log(`${ok ? "통과" : "실패"}: ${what}`); if (!ok) failed++; };

function join(name) {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(URL);
    const got = [];
    const waiters = [];
    const timer = setTimeout(() => reject(new Error(`${name}: 응답 없음`)), 5000);
    ws.onopen = () => ws.send(JSON.stringify({ t: "hello", v: 1, name }));
    ws.onmessage = (e) => {
      const msg = JSON.parse(e.data);
      got.push(msg);
      for (const w of [...waiters]) if (w.test(msg)) { waiters.splice(waiters.indexOf(w), 1); w.resolve(msg); }
      if (msg.t === "welcome") { clearTimeout(timer); resolve(client); }
      if (msg.t === "error") console.log(`${name} 서버 오류: ${msg.why}`);
    };
    ws.onerror = () => { clearTimeout(timer); reject(new Error(`${name}: 접속 실패 (${URL})`)); };
    const client = {
      name, ws, got,
      get id() { return got.find((m) => m.t === "welcome")?.id; },
      send: (m) => ws.send(JSON.stringify(m)),
      wait: (test, ms = 2000) => new Promise((res) => {
        const hit = got.find(test);
        if (hit) return res(hit);
        const w = { test, resolve: res };
        waiters.push(w);
        setTimeout(() => { const i = waiters.indexOf(w); if (i >= 0) { waiters.splice(i, 1); res(null); } }, ms);
      }),
    };
  });
}

const a = await join("윤채");
const b = await join("은샘");
const c = await join("기우");
check(a.id && b.id && c.id && new Set([a.id, b.id, c.id]).size === 3, `세 명이 서로 다른 번호 (${a.id}, ${b.id}, ${c.id})`);

// 11/08 공용 세상 시계: 접속하면 서버 시각과 기준 시각을 받는다 (모두 같은 게임 시간을 계산)
const clockA = a.got.find((m) => m.t === "welcome")?.clock, clockB = b.got.find((m) => m.t === "welcome")?.clock;
check(Number.isFinite(clockA?.now) && Math.abs(clockA.now - Date.now()) < 60_000, "welcome에 서버 시각(clock.now)이 있다");
check(clockA?.epoch === clockB?.epoch && clockA?.secondsPerDay === 360 && clockA.epoch === Date.UTC(2026, 10, 1), "모두 같은 기준 시각·하루 길이를 받는다");

a.send({ t: "enter", map: "test_rialto" });
await a.wait((m) => m.t === "room");
b.send({ t: "enter", map: "test_rialto" });
const roomB = await b.wait((m) => m.t === "room");
check(roomB?.players?.some((p) => p.id === a.id), "B가 들어가면 같은 맵의 A가 목록에 있다");
check(!!(await a.wait((m) => m.t === "join" && m.id === b.id)), "A는 B가 들어온 것을 받는다");
c.send({ t: "enter", map: "test_murano" });
await c.wait((m) => m.t === "room");

a.send({ t: "pos", x: 12.5, y: -3.25, fx: 1, fy: 0, m: true });
const posB = await b.wait((m) => m.t === "pos" && m.id === a.id);
check(posB?.x === 12.5 && posB?.y === -3.25 && posB?.fx === 1 && posB?.m === true, "B는 A의 위치·방향·걷기를 받는다");
const posC = await c.wait((m) => m.t === "pos" && m.id === a.id, 600);
check(posC === null, "다른 맵의 C는 A의 위치를 받지 않는다");

c.send({ t: "enter", map: "test_rialto" });
const roomC = await c.wait((m) => m.t === "room" && m.map === "test_rialto");
const aInC = roomC?.players?.find((p) => p.id === a.id);
check(aInC?.x === 12.5, "나중에 들어온 C도 A의 마지막 위치를 받는다");

// 11/08 생김새: 시작 화면에서 로그인한 뒤 출신을 고르면, 같은 맵에 enter를 다시 보내며 look을 고친다
const joinsOfA = () => b.got.filter((m) => m.t === "join" && m.id === a.id);
a.send({ t: "enter", map: "test_rialto", look: "arabia" });
const lookB = await b.wait((m) => m.t === "join" && m.id === a.id && m.look === "arabia");
const roomA = await a.wait((m) => m.t === "room" && m.players?.some((p) => p.id === b.id));
check(!!lookB && !!roomA, "같은 맵에 look을 실어 다시 들어오면 B는 A의 새 생김새를 받고, A는 맵 사람 목록을 다시 받는다");
a.send({ t: "enter", map: "test_rialto" });
await b.wait((m) => m.t === "join" && m.id === a.id && joinsOfA().length >= 2);
check(joinsOfA().length === 2 && joinsOfA()[1].look === "arabia", "look 없이 들어오면 생김새는 그대로 (예전 게임)");

// 11/08 운반 수단: enter에 veh를 실으면 같은 맵 사람에게 join으로, 나중에 온 사람에게 room으로 간다
a.send({ t: "enter", map: "test_rialto", veh: "arabia_camel" });
const vehB = await b.wait((m) => m.t === "join" && m.id === a.id && m.veh === "arabia_camel");
check(!!vehB, "운반 수단을 실어 다시 들어오면 B는 A의 운반 수단을 받는다");
a.send({ t: "enter", map: "test_rialto" });
await b.wait((m) => m.t === "join" && m.id === a.id && joinsOfA().length >= 4);
check(joinsOfA()[3]?.veh === "arabia_camel", "veh 없이 들어오면 운반 수단은 그대로 (예전 게임)");
a.send({ t: "enter", map: "test_rialto", veh: "" });
const walkB = await b.wait((m) => m.t === "join" && m.id === a.id && m.veh === "" && joinsOfA().length >= 5);
check(!!walkB, "veh를 빈칸으로 실으면 걷기로 돌아간다");

// 11/08 앉기 · 짐 상자 수: pos에 실으면 같은 맵 사람에게 그대로 간다 (범위 밖은 막음)
a.send({ t: "pos", x: 12.5, y: -3.25, fx: 0, fy: -1, m: false, s: true, sy: 0.5, sq: 0.86, ld: 2 });
const sitB = await b.wait((m) => m.t === "pos" && m.id === a.id && m.s === true);
check(sitB?.sy === 0.5 && sitB?.sq === 0.86 && sitB?.ld === 2, "앉은 모습(sy·sq)과 짐 상자 수(ld)가 같은 맵 사람에게 간다");
a.send({ t: "pos", x: 12.5, y: -3.25, fx: 0, fy: -1, m: false, s: true, sy: 99, sq: 0, ld: 9 });
const clampB = await b.wait((m) => m.t === "pos" && m.id === a.id && m.sy === 1);
check(clampB?.sq === 0.3 && clampB?.ld === 3, "범위 밖 값은 서버가 범위 안으로 막는다");

// 실제 서버엔 다른 사람이 있을 수 있으니, A가 나가기 전 수에서 1 줄었는지 본다
const before = [...b.got].reverse().find((m) => m.t === "online" || m.t === "welcome")?.online ?? 0;
const seen = b.got.length;
a.ws.close();
check(!!(await b.wait((m) => m.t === "leave" && m.id === a.id)), "A가 끊으면 B는 A가 나간 것을 받는다");
const online = await b.wait((m) => m.t === "online" && b.got.indexOf(m) >= seen);
check(online?.online === before - 1, `접속자 수가 하나 준 것을 받는다 (${before} → ${online?.online})`);

b.ws.close(); c.ws.close();
console.log(failed ? `실패 ${failed}건` : "모두 통과");
process.exit(failed ? 1 : 0);
