/**
 * 자동 백업을 두는 곳 (2026-11-28 AWS 이전: Cloudflare KV → S3).
 * - 운영: S3 버킷 하나에 "auto/날짜.json.gz" 파일 하나. 7일 지난 파일은 버킷의 수명 주기 규칙이 지운다(지우는 코드 없음).
 *   버킷은 공개 차단을 켜 두고, EC2의 IAM 역할에만 권한을 준다 (백업에 계정의 접속키 해시가 들어 있다).
 * - 내 컴퓨터(BACKUP_BUCKET 환경 변수가 없을 때): 메모리에 최근 7일치.
 */
import { S3Client, PutObjectCommand, GetObjectCommand, HeadObjectCommand, ListObjectsV2Command } from "@aws-sdk/client-s3";

/** 자동 백업 한 날의 정보 */
export interface AutoMeta { at: number; keys: number; bytes: number; parts: number; }

export interface BackupBox {
  put(day: string, gz: Uint8Array, meta: AutoMeta): Promise<void>;
  /** 날짜별 (최근 날이 앞) */
  days(): Promise<{ day: string; meta: AutoMeta }[]>;
  get(day: string): Promise<Uint8Array | undefined>;
}

/** 남기는 날 수 (S3는 수명 주기 규칙도 7일로 맞춘다) */
export const AUTO_KEEP = 7;

/** 내 컴퓨터 시험용: 메모리 */
export class MemoryBox implements BackupBox {
  private files = new Map<string, { gz: Uint8Array; meta: AutoMeta }>();

  async put(day: string, gz: Uint8Array, meta: AutoMeta): Promise<void> {
    this.files.set(day, { gz, meta });
    const old = [...this.files.keys()].sort().reverse().slice(AUTO_KEEP);
    for (const d of old) this.files.delete(d);
  }

  async days(): Promise<{ day: string; meta: AutoMeta }[]> {
    return [...this.files].map(([day, f]) => ({ day, meta: f.meta })).sort((a, b) => b.day.localeCompare(a.day));
  }

  async get(day: string): Promise<Uint8Array | undefined> {
    return this.files.get(day)?.gz;
  }
}

const PREFIX = "auto/", SUFFIX = ".json.gz";

/** 운영: S3 버킷 */
export class S3Box implements BackupBox {
  private bucket: string;
  private s3: { send(command: unknown): Promise<any> };

  /** s3는 시험할 때 가짜를 넣는다 (없으면 EC2의 IAM 역할로 접속) */
  constructor(bucket: string, region: string, s3?: { send(command: unknown): Promise<any> }) {
    this.bucket = bucket;
    this.s3 = s3 ?? new S3Client({ region });
  }

  async put(day: string, gz: Uint8Array, meta: AutoMeta): Promise<void> {
    await this.s3.send(new PutObjectCommand({
      Bucket: this.bucket, Key: PREFIX + day + SUFFIX, Body: gz, ContentType: "application/gzip",
      Metadata: { at: String(meta.at), keys: String(meta.keys) },
    }));
  }

  async days(): Promise<{ day: string; meta: AutoMeta }[]> {
    const found: { day: string; key: string; bytes: number }[] = [];
    let token: string | undefined;
    do {
      const out = await this.s3.send(new ListObjectsV2Command({ Bucket: this.bucket, Prefix: PREFIX, ContinuationToken: token }));
      for (const o of out.Contents ?? []) {
        const key: string = o.Key ?? "";
        if (key.endsWith(SUFFIX)) found.push({ day: key.slice(PREFIX.length, -SUFFIX.length), key, bytes: o.Size ?? 0 });
      }
      token = out.IsTruncated ? out.NextContinuationToken : undefined;
    } while (token);
    // 한 때·키 수는 파일에 붙여 둔 값 (S3 목록의 시각은 초 단위라 쓰지 않는다)
    const days = await Promise.all(found.map(async (f) => {
      const head = await this.s3.send(new HeadObjectCommand({ Bucket: this.bucket, Key: f.key }));
      const m = head.Metadata ?? {};
      return { day: f.day, meta: { at: Number(m.at) || 0, keys: Number(m.keys) || 0, bytes: f.bytes, parts: 1 } };
    }));
    return days.sort((a, b) => b.day.localeCompare(a.day));
  }

  async get(day: string): Promise<Uint8Array | undefined> {
    try {
      const out = await this.s3.send(new GetObjectCommand({ Bucket: this.bucket, Key: PREFIX + day + SUFFIX }));
      return await out.Body?.transformToByteArray();
    } catch (e) {
      if ((e as { name?: string }).name === "NoSuchKey") return undefined;
      throw e;
    }
  }
}
