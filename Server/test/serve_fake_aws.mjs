// 가짜 AWS(DynamoDB · S3)로 서버를 켠다 (2026-11-28). 운영과 같은 DynamoBackend · S3Box 코드를 지나므로,
// 이걸 켜 두고 다른 시험(npm run test:two 등)을 이 주소로 돌리면 AWS 계정 없이 저장소 연결 코드를 시험할 수 있다.
// 사용: npm run build 뒤에  node test/serve_fake_aws.mjs [포트=8789]   →   npm run test:market ws://127.0.0.1:8789/ws
// 끌 때(Ctrl+C) 가짜 DynamoDB · S3가 받은 명령 수를 찍는다.
import { readFileSync } from "node:fs";
import { WorldRoom } from "../dist/world.js";
import { Store, DynamoBackend } from "../dist/store.js";
import { S3Box } from "../dist/backups.js";
import { startServer } from "../dist/server.js";
import { FakeDynamo, FakeS3 } from "./fake_aws.mjs";

const PORT = Number(process.argv[2] ?? 8789);
const ADMIN = process.env.ADMIN_KEY
  ?? readFileSync(new URL("../.dev.vars", import.meta.url), "utf8").match(/^\s*ADMIN_KEY\s*=\s*"?([^"\r\n]+)"?\s*$/m)?.[1] ?? "";

const db = new FakeDynamo("baribari"), s3 = new FakeS3("baribari-backups");
const room = new WorldRoom(new Store(new DynamoBackend("baribari", "ap-northeast-2", db)), new S3Box("baribari-backups", "ap-northeast-2", s3));
const server = await startServer(room, { port: PORT, host: "127.0.0.1", adminKey: ADMIN, testScheduled: true });
console.log(`가짜 AWS 서버 켜짐: ws://127.0.0.1:${PORT}/ws`);

const stop = async () => {
  await server.close();
  console.log("가짜 DynamoDB 명령:", JSON.stringify(db.calls), "· 항목", db.items.size);
  console.log("가짜 S3 명령:", JSON.stringify(s3.calls), "· 파일", s3.objects.size);
  process.exit(0);
};
process.on("SIGINT", stop);
process.on("SIGTERM", stop);
