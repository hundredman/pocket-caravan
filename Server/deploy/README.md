# 바리바리 서버 EC2 배포 (2026-11-28)

구조: 게임 → `wss://도메인/ws` → **Caddy**(EC2 안, 인증서 자동) → **Node.js 서버**(127.0.0.1:8787) → **DynamoDB**(저장) · **S3**(매일 백업)

## 1. AWS 준비 (한 번만)

콘솔에서 해도 되고, AWS CLI면 아래 명령을 씁니다. 리전은 모두 서울(`ap-northeast-2`)입니다.

1. **DynamoDB 테이블**: 이름 `baribari`, 파티션 키 `kind`(문자열), 정렬 키 `id`(문자열), 용량은 온디맨드
   ```
   aws dynamodb create-table --table-name baribari --billing-mode PAY_PER_REQUEST \
     --attribute-definitions AttributeName=kind,AttributeType=S AttributeName=id,AttributeType=S \
     --key-schema AttributeName=kind,KeyType=HASH AttributeName=id,KeyType=RANGE --region ap-northeast-2
   ```
2. **S3 버킷**: 공개 차단을 켠 채로 만들고, `auto/` 파일을 7일 뒤 지우는 수명 주기 규칙을 겁니다
   ```
   aws s3api create-bucket --bucket 버킷이름 --region ap-northeast-2 --create-bucket-configuration LocationConstraint=ap-northeast-2
   aws s3api put-public-access-block --bucket 버킷이름 --public-access-block-configuration BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true
   aws s3api put-bucket-lifecycle-configuration --bucket 버킷이름 --lifecycle-configuration file://s3-lifecycle.json
   ```
3. **IAM 역할**: EC2용 역할을 만들고 `iam-policy.json`을 붙입니다 (`ACCOUNT_ID`, `BUCKET_NAME`을 바꿔서). 액세스 키는 만들지 않습니다.
4. **EC2**: Ubuntu 24.04, 서울, 위 IAM 역할을 붙입니다.
   - 보안 그룹: 80·443은 모두에게(80은 Caddy 인증서 발급용), 22는 우리 IP에만
   - 탄력적 IP를 붙입니다
5. **도메인**: A 레코드를 탄력적 IP로
6. **예산 알림**: Billing → Budgets에서 월 10달러·20달러 알림

## 2. EC2 안에서 한 번만

```
# Node.js 22와 Caddy
curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash -
sudo apt-get install -y nodejs caddy

# 서버 자리와 설정 (값은 baribari.env.example 설명대로 채운다)
sudo mkdir -p /opt/baribari && sudo chown ubuntu /opt/baribari
sudo cp baribari.env.example /etc/baribari.env && sudo nano /etc/baribari.env && sudo chmod 600 /etc/baribari.env

# 자동 재시작 서비스와 Caddy (Caddyfile의 도메인을 고친 뒤)
sudo cp baribari.service /etc/systemd/system/ && sudo systemctl daemon-reload && sudo systemctl enable baribari
sudo cp Caddyfile /etc/caddy/Caddyfile && sudo systemctl reload caddy
```

`deploy/` 폴더의 파일은 `scp -r deploy ubuntu@탄력적IP:~/`로 올려서 씁니다.

## 3. 배포 (코드를 바꿀 때마다)

내 컴퓨터의 `Server` 폴더에서 빌드해서 올립니다. EC2에는 깃 저장소를 받지 않습니다.

```
npm run build
scp -r dist package.json package-lock.json ubuntu@탄력적IP:/opt/baribari/
ssh ubuntu@탄력적IP "cd /opt/baribari && npm ci --omit=dev && sudo systemctl restart baribari"
```

다시 켜면 접속한 사람은 모두 끊겼다가 게임이 저절로 다시 접속합니다.

## 4. 확인

- `https://도메인/` → `Pocket Caravan server OK`
- `npm run admin -- 상태 --server https://도메인`
- 서버 기록: `journalctl -u baribari -f` (켤 때 저장소를 못 읽으면 테이블 이름·IAM 권한을 알려 주고 멈춥니다)
- 시험 스크립트: `npm run test:two wss://도메인/ws` 처럼 주소를 붙입니다.
  **실제 데이터를 넣기 전, 빈 서버에서만** 돌립니다. 시험은 계정을 만들고 지우고, `test:backup`은 서버 전체를 백업대로 되돌립니다.
- 처음 며칠은 CloudWatch에서 CPU와 `CPUCreditBalance`를 봅니다.

## 5. 데이터 옮기기 (이전하는 날)

1. 공지 → 옛 서버 최종 백업: `npm run admin -- 백업` (지금 `LIVE`는 Cloudflare 주소)
2. 새 서버에 넣기: `npm run admin -- 되살리기 backups/받은파일.json --server https://도메인` 으로 바뀔 것을 보고, 맞으면 끝에 `--yes`
3. 확인: `npm run admin -- 계정 --server https://도메인` (기존 계정으로 로그인, 장터 가격 그대로인지)
4. 옮긴 뒤 고칠 것: `tools/admin.mjs`의 `LIVE`, Unity `NetClient.DefaultAddress`를 새 도메인으로

백업 파일에는 접속키 해시가 들어 있습니다. 깃에 올리거나 남에게 보내지 않습니다.

## 내 컴퓨터에서 시험

- `npm run dev`: 메모리 저장소로 8787에서 켭니다 (포트가 차 있으면 `PORT=8788 npm run dev`)
- `npm run build` 뒤 `node test/serve_fake_aws.mjs`: 가짜 DynamoDB·S3로 8789에서 켭니다. 운영과 같은 저장소 연결 코드를 지나므로 AWS 계정 없이 시험할 수 있습니다
- 시험은 `npm run test:market ws://127.0.0.1:8789/ws`처럼 주소를 붙여 돌립니다
