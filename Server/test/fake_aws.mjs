// 가짜 DynamoDB · S3 (2026-11-28): AWS 계정 없이 store.ts의 DynamoBackend와 backups.ts의 S3Box를 시험한다.
// 우리가 쓰는 명령만 안다. AWS 규칙 중 틀리기 쉬운 것을 검사해서 어기면 AWS처럼 오류를 낸다:
//   키 칸은 빈 글자 안 됨 · 항목 하나 400KB · TransactWriteItems 100개·4MB · 한 묶음 안에 같은 항목 두 번 안 됨
// 목록(Query · Scan · ListObjectsV2)은 일부러 조금씩(3개) 나눠 줘서 다음 페이지를 이어 읽는지 본다.

const PAGE = 3;
const enc = new TextEncoder();
const bytes = (s) => enc.encode(s).length;

function fail(name, message) {
  const e = new Error(message);
  e.name = name;
  throw e;
}

function str(av, what) {
  if (!av || typeof av.S !== "string") fail("ValidationException", `${what}: 문자열(S)이어야 합니다`);
  return av.S;
}

function keyOf(key, what) {
  const names = Object.keys(key ?? {}).sort().join(",");
  if (names !== "id,kind") fail("ValidationException", `${what}: 키는 kind·id 두 칸이어야 합니다 (${names})`);
  const kind = str(key.kind, `${what} kind`), id = str(key.id, `${what} id`);
  if (!kind || !id) fail("ValidationException", `${what}: 키 칸은 빈 글자일 수 없습니다`);
  if (bytes(kind) > 2048 || bytes(id) > 1024) fail("ValidationException", `${what}: 키가 너무 깁니다`);
  return { kind, id };
}

function itemSize(item) {
  let n = 0;
  for (const [k, v] of Object.entries(item)) n += bytes(k) + bytes(str(v, `칸 ${k}`));
  return n;
}

/** UTF-8 바이트 순서 비교 (DynamoDB 정렬 키 순서) */
function byteCompare(a, b) {
  const x = enc.encode(a), y = enc.encode(b);
  for (let i = 0; i < Math.min(x.length, y.length); i++) if (x[i] !== y[i]) return x[i] - y[i];
  return x.length - y.length;
}

export class FakeDynamo {
  constructor(table) {
    this.table = table;
    this.items = new Map();   // "kind\u0000id" → item
    this.calls = {};
  }

  checkTable(input) {
    if (input.TableName !== this.table) fail("ResourceNotFoundException", `테이블이 없습니다: ${input.TableName}`);
  }

  names(input, expr) {
    const out = {};
    for (const m of (expr ?? "").matchAll(/#\w+/g)) {
      const real = input.ExpressionAttributeNames?.[m[0]];
      if (!real) fail("ValidationException", `${m[0]}의 이름이 없습니다`);
      out[m[0]] = real;
    }
    return out;
  }

  async send(command) {
    const name = command.constructor.name.replace(/Command$/, "");
    const input = command.input;
    this.calls[name] = (this.calls[name] ?? 0) + 1;
    switch (name) {
      case "GetItem": {
        this.checkTable(input);
        const { kind, id } = keyOf(input.Key, "GetItem");
        if (input.ConsistentRead !== true) fail("Test", "GetItem은 ConsistentRead여야 방금 쓴 값을 읽는다");
        const names = this.names(input, input.ProjectionExpression);
        const it = this.items.get(kind + "\u0000" + id);
        if (!it) return {};
        const keep = new Set(Object.values(names));
        return { Item: Object.fromEntries(Object.entries(it).filter(([k]) => keep.size === 0 || keep.has(k))) };
      }
      case "PutItem": {
        this.checkTable(input);
        this.apply([{ Put: input }]);
        return {};
      }
      case "DeleteItem": {
        this.checkTable(input);
        this.apply([{ Delete: input }]);
        return {};
      }
      case "TransactWriteItems": {
        const list = input.TransactItems ?? [];
        if (list.length < 1 || list.length > 100) fail("ValidationException", `TransactWriteItems는 1~100개 (${list.length})`);
        let total = 0;
        const seen = new Set();
        for (const op of list) {
          const one = op.Put ?? op.Delete;
          if (!one || (op.Put && op.Delete)) fail("ValidationException", "Put이나 Delete 하나만");
          this.checkTable(one);
          const { kind, id } = keyOf(op.Put ? { kind: op.Put.Item.kind, id: op.Put.Item.id } : op.Delete.Key, "TransactWriteItems");
          const k = kind + "\u0000" + id;
          if (seen.has(k)) fail("ValidationException", "Transaction request cannot include multiple operations on one item");
          seen.add(k);
          if (op.Put) total += itemSize(op.Put.Item);
        }
        if (total > 4 * 1024 * 1024) fail("ValidationException", "TransactWriteItems 4MB 넘음");
        this.apply(list);
        return {};
      }
      case "Query": {
        this.checkTable(input);
        if (input.ConsistentRead !== true) fail("Test", "Query는 ConsistentRead여야 한다");
        const names = this.names(input, input.KeyConditionExpression);
        const v = input.ExpressionAttributeValues ?? {};
        const m = /^#\w+ = :k( AND begins_with\(#\w+, :p\))?$/.exec(input.KeyConditionExpression ?? "");
        if (!m || Object.values(names)[0] !== "kind") fail("ValidationException", `모르는 조건: ${input.KeyConditionExpression}`);
        const kind = str(v[":k"], ":k"), pre = m[1] ? str(v[":p"], ":p") : "";
        const rows = [...this.items.values()].filter((it) => it.kind.S === kind && it.id.S.startsWith(pre))
          .sort((a, b) => byteCompare(a.id.S, b.id.S));
        return this.page(rows, input.ExclusiveStartKey);
      }
      case "Scan": {
        this.checkTable(input);
        if (input.ConsistentRead !== true) fail("Test", "Scan은 ConsistentRead여야 한다");
        const rows = [...this.items.entries()].sort((a, b) => byteCompare(a[0], b[0])).map(([, it]) => it);
        return this.page(rows, input.ExclusiveStartKey);
      }
      default:
        fail("Test", `가짜 DynamoDB가 모르는 명령: ${name}`);
    }
  }

  apply(list) {
    for (const op of list) {
      if (op.Put) {
        const it = op.Put.Item;
        keyOf({ kind: it.kind, id: it.id }, "PutItem");
        if (itemSize(it) > 400 * 1024) fail("ValidationException", "Item size has exceeded the maximum allowed size");
        this.items.set(it.kind.S + "\u0000" + it.id.S, structuredClone(it));
      } else {
        const { kind, id } = keyOf(op.Delete.Key, "DeleteItem");
        this.items.delete(kind + "\u0000" + id);
      }
    }
  }

  page(rows, start) {
    let i = 0;
    if (start) {
      const { kind, id } = keyOf(start, "ExclusiveStartKey");
      i = rows.findIndex((it) => it.kind.S === kind && it.id.S === id) + 1;
      if (i === 0) fail("Test", "ExclusiveStartKey가 목록에 없다");
    }
    const Items = rows.slice(i, i + PAGE).map((it) => structuredClone(it));
    const last = Items.at(-1);
    return { Items, ...(i + PAGE < rows.length && last ? { LastEvaluatedKey: { kind: last.kind, id: last.id } } : {}) };
  }
}

export class FakeS3 {
  constructor(bucket) {
    this.bucket = bucket;
    this.objects = new Map();   // key → { body, meta }
    this.calls = {};
  }

  async send(command) {
    const name = command.constructor.name.replace(/Command$/, "");
    const input = command.input;
    this.calls[name] = (this.calls[name] ?? 0) + 1;
    if (input.Bucket !== this.bucket) fail("NoSuchBucket", `버킷이 없습니다: ${input.Bucket}`);
    switch (name) {
      case "PutObject": {
        if (!(input.Body instanceof Uint8Array)) fail("Test", "Body는 바이트여야 한다");
        for (const [k, v] of Object.entries(input.Metadata ?? {})) if (typeof v !== "string" || k !== k.toLowerCase()) fail("Test", `메타데이터는 소문자 이름 · 문자열 값: ${k}`);
        this.objects.set(input.Key, { body: new Uint8Array(input.Body), meta: { ...input.Metadata } });
        return {};
      }
      case "HeadObject": {
        const o = this.objects.get(input.Key);
        if (!o) fail("NotFound", "없음");
        return { Metadata: { ...o.meta }, ContentLength: o.body.length };
      }
      case "GetObject": {
        const o = this.objects.get(input.Key);
        if (!o) fail("NoSuchKey", "The specified key does not exist.");
        return { Body: { transformToByteArray: async () => new Uint8Array(o.body) } };
      }
      case "ListObjectsV2": {
        const keys = [...this.objects.keys()].filter((k) => k.startsWith(input.Prefix ?? "")).sort();
        const from = input.ContinuationToken ? Number(input.ContinuationToken) : 0;
        const part = keys.slice(from, from + 2);
        const more = from + 2 < keys.length;
        return { Contents: part.map((Key) => ({ Key, Size: this.objects.get(Key).body.length })), IsTruncated: more, ...(more ? { NextContinuationToken: String(from + 2) } : {}) };
      }
      default:
        fail("Test", `가짜 S3가 모르는 명령: ${name}`);
    }
  }
}
