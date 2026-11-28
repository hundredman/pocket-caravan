/**
 * 바리바리 온라인 서버 시작점 (2026-11-28 AWS 이전: Cloudflare Workers → EC2 한 대의 Node.js).
 * 설정은 환경 변수로 받는다 (EC2에서는 systemd의 EnvironmentFile, 내 컴퓨터에서는 Server/.dev.vars):
 *   ADMIN_KEY      관리 키 (16자 이상). 없으면 관리 기능이 꺼진다
 *   TABLE          DynamoDB 테이블 이름 (파티션 키 kind, 정렬 키 id: 문자열)
 *   BACKUP_BUCKET  자동 백업을 두는 S3 버킷 (수명 주기 규칙: 7일 뒤 삭제)
 *   AWS_REGION     기본 ap-northeast-2 (서울)
 *   PORT · HOST    기본 8787 · 127.0.0.1 (앞의 Caddy만 들어오게)
 * TABLE과 BACKUP_BUCKET이 둘 다 없으면 내 컴퓨터 시험용: 메모리에 둔다(끄면 사라진다).
 * AWS 권한은 EC2에 붙인 IAM 역할에서 받는다. 액세스 키를 코드나 서버에 넣지 않는다.
 * 나중에 사람이 늘면 맵마다 방을 나누고 서버를 여러 대로 늘린다.
 */
import { WorldRoom } from "./world.js";
import { Store, MemoryBackend, DynamoBackend } from "./store.js";
import { MemoryBox, S3Box } from "./backups.js";
import { startServer } from "./server.js";

const env = process.env;
const table = env.TABLE ?? "", bucket = env.BACKUP_BUCKET ?? "";
const region = env.AWS_REGION ?? "ap-northeast-2";
if (!table !== !bucket) {
  console.error("TABLE과 BACKUP_BUCKET은 둘 다 정하거나(AWS) 둘 다 비워 두세요(내 컴퓨터 시험)");
  process.exit(1);
}

const store = new Store(table ? new DynamoBackend(table, region) : new MemoryBackend());
const backups = table ? new S3Box(bucket, region) : new MemoryBox();

// 켤 때 저장소를 한 번 읽어 본다: IAM 권한·테이블 이름이 틀렸으면 여기서 멈춘다 (systemd가 잠시 뒤 다시 켠다)
try {
  await store.get("nextId");
} catch (e) {
  console.error(`저장소(${table || "메모리"})를 읽지 못했어요. 테이블 이름과 IAM 역할 권한을 확인하세요`, e);
  process.exit(1);
}

const room = new WorldRoom(store, backups);
const port = Number(env.PORT ?? 8787), host = env.HOST ?? "127.0.0.1";
const server = await startServer(room, {
  port, host,
  adminKey: (env.ADMIN_KEY ?? "").trim(),
  testScheduled: process.argv.includes("--test-scheduled"),
});
console.log(`바리바리 서버 켜짐: ws://${host}:${port}/ws · 저장소 ${table ? `DynamoDB ${table} · 백업 S3 ${bucket} (${region})` : "메모리 (시험용, 끄면 사라짐)"}`);

// 배포·재시작 때 (systemctl restart → SIGTERM): 사용량을 적고 접속을 1012로 닫은 뒤 끈다
let closing = false;
for (const signal of ["SIGTERM", "SIGINT"] as const) {
  process.on(signal, () => {
    if (closing) return;
    closing = true;
    console.log(`${signal}: 끄는 중`);
    server.close().catch((e) => console.error("끄기 실패", e)).finally(() => process.exit(0));
  });
}
