/**
 * 저장소 모듈 (2026-11-28 AWS 이전). Durable Object의 ctx.storage와 같은 모양(get · put · delete · list)이라
 * world.ts는 this.ctx.storage 대신 this.store를 부르면 된다.
 *
 * - 운영(EC2): DynamoDB 테이블 하나. 파티션 키 kind, 정렬 키 id, 값은 JSON 글 한 칸(v).
 *     "acct:윤채" → kind "acct", id "윤채"   ·   "fullv:윤채:2" → kind "fullv", id "윤채:2"
 *     ":"가 없는 키("market" · "trades" · "tradeSeq" · "nextId" · "boards") → kind "world"
 *   계정 목록(prefix "acct:")은 kind = acct Query 한 번, 전체 목록은 Scan을 페이지로 읽는다.
 *   여러 키를 한 번에 쓰면(게임 저장: 계정 + full: + fullv:) TransactWriteItems로 묶는다.
 * - 내 컴퓨터(TABLE 환경 변수가 없을 때): 메모리. 서버를 끄면 사라진다.
 *
 * 세계 값(kind "world")은 자주 읽으므로 처음 한 번만 읽어 메모리에 들고 있고, 바뀔 때만 쓴다(서버가 한 대라 늘 맞다).
 * 처리 순서는 world.ts의 줄(run)이 지킨다. 여기서는 쓰기가 끝날 때까지 기다린 뒤 돌아간다.
 */
import {
  DynamoDBClient, GetItemCommand, PutItemCommand, DeleteItemCommand, TransactWriteItemsCommand, QueryCommand, ScanCommand,
  type AttributeValue, type TransactWriteItem,
} from "@aws-sdk/client-dynamodb";

/** 저장소 밑바탕: 값은 JSON 글로 주고받는다 */
export interface Backend {
  get(key: string): Promise<string | undefined>;
  /** puts와 dels를 쓴다. 작으면 한 번에(전부 되거나 전부 안 되거나) */
  write(puts: [string, string][], dels: string[]): Promise<void>;
  /** prefix로 시작하는 키 전부 (순서 없음) */
  scan(prefix: string): Promise<[string, string][]>;
}

export interface ListOptions { prefix?: string; startAfter?: string; limit?: number; }

const WORLD = "world";

/** 저장소 키 → DynamoDB 키 */
export function splitKey(key: string): { kind: string; id: string } {
  const i = key.indexOf(":");
  if (i < 0) return { kind: WORLD, id: key };
  const kind = key.slice(0, i), id = key.slice(i + 1);
  if (!kind || !id || kind === WORLD) throw new Error(`저장소 키 모양이 이상합니다: ${key}`);
  return { kind, id };
}

/** DynamoDB 키 → 저장소 키 */
export function joinKey(kind: string, id: string): string {
  return kind === WORLD ? id : `${kind}:${id}`;
}

const isHot = (key: string) => !key.includes(":");

export class Store {
  /** 세계 값: 키 → JSON 글 (null = 없음) */
  private hot = new Map<string, string | null>();
  private backend: Backend;

  constructor(backend: Backend) {
    this.backend = backend;
  }

  async get<T>(key: string): Promise<T | undefined> {
    let text: string | null | undefined;
    if (isHot(key)) {
      if (!this.hot.has(key)) this.hot.set(key, (await this.backend.get(key)) ?? null);
      text = this.hot.get(key);
    } else {
      text = await this.backend.get(key);
    }
    return text == null ? undefined : JSON.parse(text) as T;
  }

  put(key: string, value: unknown): Promise<void>;
  put(entries: Record<string, unknown>): Promise<void>;
  async put(a: string | Record<string, unknown>, b?: unknown): Promise<void> {
    const entries = typeof a === "string" ? { [a]: b } : a;
    const puts = Object.entries(entries).map(([k, v]): [string, string] => {
      const text = JSON.stringify(v);
      if (text === undefined) throw new Error(`저장할 값이 없습니다: ${k}`);
      return [k, text];
    });
    if (puts.length === 0) return;
    await this.backend.write(puts, []);
    for (const [k, text] of puts) if (isHot(k)) this.hot.set(k, text);
  }

  async delete(keys: string | string[]): Promise<void> {
    const list = [...new Set(typeof keys === "string" ? [keys] : keys)];
    if (list.length === 0) return;
    await this.backend.write([], list);
    for (const k of list) if (isHot(k)) this.hot.set(k, null);
  }

  /** Durable Object의 list와 같다: 키 순서대로, startAfter 다음부터 limit개까지 */
  async list<T = unknown>(opts: ListOptions = {}): Promise<Map<string, T>> {
    const rows = await this.backend.scan(opts.prefix ?? "");
    rows.sort((x, y) => (x[0] < y[0] ? -1 : x[0] > y[0] ? 1 : 0));
    const out = new Map<string, T>();
    for (const [k, text] of rows) {
      if (opts.startAfter !== undefined && k <= opts.startAfter) continue;
      if (opts.limit !== undefined && out.size >= opts.limit) break;
      out.set(k, JSON.parse(text) as T);
    }
    return out;
  }
}

/** 내 컴퓨터 시험용: 메모리 */
export class MemoryBackend implements Backend {
  private data = new Map<string, string>();

  async get(key: string): Promise<string | undefined> {
    return this.data.get(key);
  }

  async write(puts: [string, string][], dels: string[]): Promise<void> {
    for (const k of dels) this.data.delete(k);
    for (const [k, text] of puts) this.data.set(k, text);
  }

  async scan(prefix: string): Promise<[string, string][]> {
    return [...this.data].filter(([k]) => k.startsWith(prefix));
  }
}

/** TransactWriteItems 한 번: 항목 100개 · 전체 4MB까지 (넉넉히 3MB에서 끊는다) */
const TX_ITEMS = 100, TX_BYTES = 3 * 1024 * 1024;

/** 운영: DynamoDB 테이블 하나 */
export class DynamoBackend implements Backend {
  private table: string;
  private db: { send(command: unknown): Promise<any> };

  /** db는 시험할 때 가짜를 넣는다 (없으면 EC2의 IAM 역할로 접속) */
  constructor(table: string, region: string, db?: { send(command: unknown): Promise<any> }) {
    this.table = table;
    this.db = db ?? new DynamoDBClient({ region });
  }

  private static key(key: string): Record<string, AttributeValue> {
    const { kind, id } = splitKey(key);
    return { kind: { S: kind }, id: { S: id } };
  }

  async get(key: string): Promise<string | undefined> {
    const out = await this.db.send(new GetItemCommand({
      TableName: this.table, Key: DynamoBackend.key(key), ConsistentRead: true,
      ProjectionExpression: "#v", ExpressionAttributeNames: { "#v": "v" },
    }));
    return out.Item?.v?.S;
  }

  async write(puts: [string, string][], dels: string[]): Promise<void> {
    const ops: { item: TransactWriteItem; bytes: number }[] = [
      ...puts.map(([k, text]) => ({ item: { Put: { TableName: this.table, Item: { ...DynamoBackend.key(k), v: { S: text } } } }, bytes: Buffer.byteLength(k) + Buffer.byteLength(text) + 16 })),
      ...dels.map((k) => ({ item: { Delete: { TableName: this.table, Key: DynamoBackend.key(k) } }, bytes: Buffer.byteLength(k) + 16 })),
    ];
    if (ops.length === 1) {
      const one = ops[0].item;
      if (one.Put) await this.db.send(new PutItemCommand(one.Put as ConstructorParameters<typeof PutItemCommand>[0]));
      else await this.db.send(new DeleteItemCommand(one.Delete as ConstructorParameters<typeof DeleteItemCommand>[0]));
      return;
    }
    // 여러 개: 한도 안에서 묶어 한 번에 (게임 저장은 늘 한 묶음 안에 든다)
    let batch: TransactWriteItem[] = [], bytes = 0;
    for (const op of ops) {
      if (batch.length > 0 && (batch.length >= TX_ITEMS || bytes + op.bytes > TX_BYTES)) {
        await this.db.send(new TransactWriteItemsCommand({ TransactItems: batch }));
        batch = []; bytes = 0;
      }
      batch.push(op.item);
      bytes += op.bytes;
    }
    if (batch.length > 0) await this.db.send(new TransactWriteItemsCommand({ TransactItems: batch }));
  }

  async scan(prefix: string): Promise<[string, string][]> {
    const rows: [string, string][] = [];
    const take = (items: Record<string, AttributeValue>[] | undefined) => {
      for (const it of items ?? []) {
        const k = joinKey(it.kind?.S ?? "", it.id?.S ?? "");
        if (k.startsWith(prefix)) rows.push([k, it.v?.S ?? "null"]);
      }
    };
    const i = prefix.indexOf(":");
    let start: Record<string, AttributeValue> | undefined;
    if (i > 0) {
      // "acct:" 같은 앞부분: 그 kind만 Query
      const kind = prefix.slice(0, i), rest = prefix.slice(i + 1);
      do {
        const out = await this.db.send(new QueryCommand({
          TableName: this.table, ConsistentRead: true, ExclusiveStartKey: start,
          KeyConditionExpression: rest ? "#k = :k AND begins_with(#i, :p)" : "#k = :k",
          ExpressionAttributeNames: rest ? { "#k": "kind", "#i": "id" } : { "#k": "kind" },
          ExpressionAttributeValues: rest ? { ":k": { S: kind }, ":p": { S: rest } } : { ":k": { S: kind } },
        }));
        take(out.Items);
        start = out.LastEvaluatedKey;
      } while (start);
    } else {
      // 전체(백업): Scan을 페이지로
      do {
        const out = await this.db.send(new ScanCommand({ TableName: this.table, ConsistentRead: true, ExclusiveStartKey: start }));
        take(out.Items);
        start = out.LastEvaluatedKey;
      } while (start);
    }
    return rows;
  }
}
