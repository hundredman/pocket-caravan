// 6·7단계 시험: 다시 붙기 · 이름 겹침 · ping · 채팅 · 너무 많이 보내기 · 버전 다름 (· 유령 접속 정리)
// 사용: npm run dev 를 켠 상태에서 npm run test:safety [주소] [--slow]
//   --slow 를 붙이면 "30초 동안 아무것도 안 보내면 서버가 닫는지"도 본다 (40초쯤 걸림)
const args = process.argv.slice(2);
const SLOW = args.includes("--slow");
const URL = args.find((a) => !a.startsWith("--")) ?? "ws://127.0.0.1:8787/ws";
const RUN = Math.random().toString(36).slice(2, 7);   // 실제 서버에 다른 사람이 있어도 이름·맵이 겹치지 않게
let failed = 0;
const check = (ok, what) => { console.log(`${ok ? "통과" : "실패"}: ${what}`); if (!ok) failed++; };
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

/** 접속만 한다 (hello는 따로). 받은 메시지와 닫힌 번호를 모은다 */
function open() {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(URL);
    const got = [];
    const waiters = [];
    const c = {
      ws, got, closed: null,
      send: (m) => ws.send(typeof m === "string" ? m : JSON.stringify(m)),
      wait: (test, ms = 2000) => new Promise((res) => {
        const hit = got.find(test);
        if (hit) return res(hit);
        const w = { test, resolve: res };
        waiters.push(w);
        setTimeout(() => { const i = waiters.indexOf(w); if (i >= 0) { waiters.splice(i, 1); res(null); } }, ms);
      }),
      waitClose: (ms = 3000) => new Promise((res) => {
        if (c.closed) return res(c.closed);
        const t = setTimeout(() => res(null), ms);
        c.onClosed = (code) => { clearTimeout(t); res(code); };
      }),
    };
    ws.onopen = () => resolve(c);
    ws.onerror = () => reject(new Error(`접속 실패 (${URL})`));
    ws.onclose = (e) => { c.closed = e.code; c.onClosed?.(e.code); };
    ws.onmessage = (e) => {
      const msg = JSON.parse(e.data);
      got.push(msg);
      for (const w of [...waiters]) if (w.test(msg)) { waiters.splice(waiters.indexOf(w), 1); w.resolve(msg); }
    };
  });
}

async function hello(name, key) {
  const c = await open();
  c.send({ t: "hello", v: 1, name, key });
  c.welcome = await c.wait((m) => m.t === "welcome", 5000);
  return c;
}

// 1. 같은 key로 다시 들어오면 예전 번호를 그대로 받고, 예전 접속은 4004로 닫힌다
const key = "k" + RUN + "0123456789abcdef";
const first = await hello(`다시${RUN}`, key);
const second = await hello(`다시${RUN}`, key);
check(first.welcome && second.welcome?.id === first.welcome.id, `같은 key로 다시 들어오면 같은 번호 (${first.welcome?.id} → ${second.welcome?.id})`);
check((await first.waitClose()) === 4004, "예전 접속은 4004(다시 접속함)로 닫힌다");
check(second.welcome?.name === `다시${RUN}`, "다시 들어온 사람은 이름이 그대로다 (자기 이름과 겹친다고 보지 않음)");

// 2. 이름이 겹치면 (2)가 붙는다
const dupA = await hello(`겹${RUN}`);
const dupB = await hello(`겹${RUN}`);
check(dupA.welcome?.name === `겹${RUN}` && dupB.welcome?.name === `겹${RUN}(2)`, `이름이 겹치면 뒤 사람은 "${dupB.welcome?.name}"`);
const longA = await hello("가나다라마바사아자차카타파하가나");
const longB = await hello("가나다라마바사아자차카타파하가나");
check([...(longB.welcome?.name ?? "")].length <= 16 && longB.welcome?.name.endsWith("(2)"), `16자 이름도 16자 안에서 (2): "${longB.welcome?.name}"`);
longA.ws.close(); longB.ws.close();

// 3. ping → pong (방을 깨우지 않는 자동 응답)
second.send('{"t":"ping"}');
check(!!(await second.wait((m) => m.t === "pong")), "ping을 보내면 pong이 온다");

// 4. 채팅: 모두에게 가고(보낸 사람 포함), 꺾쇠는 바뀌고, 1초에 1번
const map = `test_chat_${RUN}`;
dupA.send({ t: "enter", map }); await dupA.wait((m) => m.t === "room");
dupB.send({ t: "enter", map }); await dupB.wait((m) => m.t === "room");
dupA.send({ t: "say", text: "  안녕 <color=red>빨강</color>\n둘째 줄  " });
const heardB = await dupB.wait((m) => m.t === "say" && m.id === dupA.welcome.id);
const heardA = await dupA.wait((m) => m.t === "say" && m.id === dupA.welcome.id);
const heardOther = await second.wait((m) => m.t === "say" && m.id === dupA.welcome.id);
check(heardB?.text === "안녕 ‹color=red›빨강‹/color› 둘째 줄", `같은 맵 사람이 받는다, 꺾쇠·줄바꿈 정리: "${heardB?.text}"`);
check(heardB?.map === map && heardB?.name === dupA.welcome.name, "채팅에 보낸 사람 이름과 맵이 붙는다");
check(!!heardA, "보낸 사람도 자기 말을 받는다 (서버가 받아 줬다는 표시)");
check(!!heardOther, "다른 맵 사람도 받는다 (게임은 말풍선 없이 목록에만 보여 준다)");
dupA.send({ t: "say", text: "너무 빨리" });
check(!!(await dupA.wait((m) => m.t === "error" && m.why.includes("천천히"))), "1초 안에 또 말하면 '천천히' 안내");
dupA.send({ t: "say", text: "가".repeat(200) });
await sleep(1100);
dupA.send({ t: "say", text: "가".repeat(200) });
const longSay = await dupB.wait((m) => m.t === "say" && m.text.startsWith("가가"));
check([...(longSay?.text ?? "")].length === 80, `긴 말은 80자로 자른다 (${[...(longSay?.text ?? "")].length}자)`);

// 5. 너무 많이 보내면: 30개 넘는 건 버리고, 90개 넘으면 4001로 닫는다. 다른 사람은 멀쩡하다
const spam = await hello(`폭주${RUN}`);
spam.send({ t: "enter", map }); await spam.wait((m) => m.t === "room");
for (let i = 0; i < 40; i++) spam.send({ t: "pos", x: i, y: 0, fx: 1, fy: 0, m: true });
await sleep(900);   // 실제 서버는 막 켜졌을 때 조금 늦다
const relayed = dupB.got.filter((m) => m.t === "pos" && m.id === spam.welcome.id).length;
check(relayed > 0 && relayed <= 30, `1초에 40개 보내면 다른 사람에게는 30개 이하만 전달 (${relayed}개)`);
check(!!spam.got.find((m) => m.t === "error" && m.why.includes("너무 많이")), "보낸 사람에게 '너무 많이' 안내");
await sleep(1100);
for (let i = 0; i < 150; i++) spam.send({ t: "pos", x: i, y: 1, fx: 1, fy: 0, m: true });
check((await spam.waitClose()) === 4001, "1초에 150개 보내면 4001(너무 많이)로 닫힌다");
check(!!(await dupB.wait((m) => m.t === "leave" && m.id === spam.welcome.id)), "닫힌 사람은 같은 맵 사람에게 '나감'으로 알려진다");
dupB.send('{"t":"ping"}');
check(!!(await dupB.wait((m) => m.t === "pong")), "다른 사람 접속은 멀쩡하다");

// 6. 버전이 다르면 4002로 닫는다
const old = await open();
old.send({ t: "hello", v: 999, name: "옛날" });
check((await old.waitClose()) === 4002, "프로토콜 버전이 다르면 4002로 닫힌다");

// 7. (--slow) 30초 넘게 아무것도 안 보내면 4003으로 닫는다. ping을 보내는 쪽은 살아 있다
if (SLOW) {
  const quiet = await hello(`조용${RUN}`);
  const alive = await hello(`핑${RUN}`);
  const pinger = setInterval(() => alive.send('{"t":"ping"}'), 5000);
  console.log("… 45초 기다립니다");
  const code = await quiet.waitClose(45000);
  clearInterval(pinger);
  check(code === 4003, `조용한 접속은 4003(응답 없음)으로 닫힌다 (${code})`);
  check(alive.closed === null, "5초마다 ping을 보낸 접속은 살아 있다");
  alive.ws.close();
}

for (const c of [second, dupA, dupB]) c.ws.close();
await sleep(200);
console.log(failed ? `실패 ${failed}건` : "모두 통과");
process.exit(failed ? 1 : 0);
