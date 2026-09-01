# Yogabase SDKs

한국어 | [English](README.md)

**Yogabase** 기반 애플리케이션을 위한 클라이언트 SDK 프리뷰입니다.

Yogabase는 Kubernetes 전반에서 데이터 서비스를 운영하기 위한 .NET 10 Control Plane입니다. 이 저장소는 비공개 Yogabase 제품 저장소, Helm 차트, 이미지, Control Plane 및 Marketplace 전달 그래프와 의도적으로 분리되어 있습니다. 이곳에는 애플리케이션용 클라이언트 라이브러리와 계약 픽스처만 있으며, Kubernetes 오퍼레이터, 클라우드 프로비저닝 도구 또는 관리 SDK가 아닙니다.

> **프리뷰 상태:** 이 저장소의 패키지는 아직 게시되지 않은 프리뷰입니다. 로컬 빌드가 성공하더라도 NuGet, npm, PyPI, Maven Central, pub.dev, GitHub Packages 또는 다른 레지스트리에 게시할 권한이 부여되는 것은 아닙니다.

## Yogabase가 하는 일

Yogabase는 팀이 Kubernetes에서 데이터 서비스를 계획, 승인, 배포, 관찰 및 운영할 수 있는 하나의 권위 있는 위치를 제공합니다. 첫 제품 범위는 **Yogabase Core**입니다. 여기에는 관리형 YugabyteDB 수명 주기, 지원되는 데이터베이스 API인 **YSQL**, Blazor Console, 중앙 Control Plane, 그리고 AWS, Azure, Google Cloud, Oracle Cloud 적격성 프로필에 이식 가능한 Kubernetes 프로비저닝이 포함됩니다.

Yogabase는 하나의 Helm 차트와 하나의 조정된 이미지 제품군으로 제공되는 단일 Kubernetes Marketplace 제품입니다. 차트에는 두 가지 설치 모드가 있습니다.

| 모드 | 역할 |
|---|---|
| `control-plane` | 권위 있는 Control Plane, Console, REST API, fleet gateway, 프로비저닝 워크플로, 청구/계량, 로컬 actuator/agent 및 전용 RF1 YSQL 카탈로그를 실행합니다. |
| `member` | 각 관리 대상에 설치되는 경량 서비스를 실행합니다. 즉, 아웃바운드 fleet agent와 결정론적 로컬 actuator입니다. |

Control Plane은 Yogabase에서 허용된 관리 의도의 유일한 출처입니다. member는 인바운드 관리 API를 노출하지 않고, 제품 정책을 결정하지 않으며, 동등한 Control Plane이 되지 않습니다. member는 상호 TLS 인증을 사용하는 gRPC로 Control Plane에 **아웃바운드** 연결하고, 서명된 대상 바인딩 의도를 지속하며, Control Plane을 사용할 수 없을 때에도 이미 수락된 로컬 작업을 계속 조정합니다.

## Console 경험

![Yogabase Blazor Console 개념 이미지](assets/yogabase-console-concept-v2.png)

_이 이미지는 소스에 근거한 Console 개념도이며 실제 환경이나 상태 화면이 아닙니다. 빈 상태를 의도적으로 사용하며 고객 데이터, 지표 또는 활성화된 기능을 나타내지 않습니다._

## 아키텍처 작동 방식

![Yogabase Control Plane 및 member 아키텍처](assets/yogabase-architecture.svg)

로컬 actuator는 의도적으로 종속적입니다. Yogabase 소유 Kubernetes 워크로드를 쓰는 유일한 구성 요소이지만, Control Plane이 서명하고 허용 목록에 올린 의도만을 결정론적 리소스로 변환할 수 있습니다. 임의의 Secret을 읽거나 Pod에서 명령을 실행하거나 관리 정책을 생성하지 않습니다.

### 클라우드 및 대상 ID

클라우드 공급자 선택은 Console 드롭다운이나 클라이언트 API 매개변수가 아니라 설치 ID입니다. Marketplace 릴리스가 Control Plane 공급자를 고정합니다. 각 member의 공급자와 관리 클러스터 종류는 등록 시 증명되며 변경할 수 없는 대상 ID가 됩니다. 프로비저닝은 .NET `ICloudProvisioner` 경계 뒤에 남고 Pulumi .NET 프로그램을 사용하지만, 제품은 하나의 공급자 중립 Control Plane과 하나의 Helm 차트를 유지합니다.

### 데이터베이스 작업

Core는 선택된 Kubernetes 대상에서 논리적 YugabyteDB 데이터베이스 서비스를 관리합니다. YSQL 우선 데이터베이스 수명 주기, 상태/토폴로지 가시성, 명시적 RF1/RF3/RF5 토폴로지 선택, 보호된 scale-out, 컴퓨트 크기 조정, 스토리지 확장, 선택적 논리 암호화 백업, 새 대상 또는 비어 있는 대상에 대한 복원, 허용 목록 기반 패치 업그레이드, 안전한 삭제, 유지된 PVC, 진단 및 민감 정보가 제거된 지원 번들을 지원합니다.

모든 변경은 내구성 있는 작업으로 설계됩니다.

1. Yogabase가 요청한 정확한 변경의 미리보기를 생성합니다.
2. 요청은 현재 리소스 revision 및 idempotency key와 대조하여 검사됩니다.
3. 위험한 작업에는 적절한 승인이 필요합니다.
4. 수락된 작업은 감사 가능하고 복구 가능하며, 꾸며 낸 상태가 아닌 제한된 관찰 사실을 보고합니다.

Yogabase는 임의 SQL, 엔진 내부 토폴로지를 클라이언트 계약으로 노출하거나, 원격 셸 접근, `pods/exec` 워크플로, peer gossip 또는 대체 Control Plane을 제공하지 않습니다. YCQL, 자동 scale-in, PITR, 다운그레이드, 교차 클러스터 데이터베이스 배포는 현재 Core 범위 밖입니다.

### 작업 중 일어나는 일

![Yogabase 내구성 작업 흐름](assets/yogabase-operation-flow.svg)

### 보안 경계

YSQL 트래픽은 인증서 기반이며 fail-closed입니다. 노드 간 및 클라이언트-서버 TLS가 필수이고, 클라이언트 인증서 ID는 명시적으로 허용 목록에 등록됩니다. Secret 값은 브라우저 상태, 리소스 상태, 로그, 감사 기록, Pulumi 출력 또는 지원 번들에 속하지 않습니다.

제품은 공개 Kubernetes API를 의도적으로 작게 유지합니다. `YogaDatabase`, `YogaDatabaseOperation`, `YogaApplicationRuntime`, `YogaValkey`, `YogaRobustMQCluster`입니다. 이 리소스는 Control Plane의 대체물이 아니라, 서명된 desired-state 입력과 제한된 로컬 관찰입니다.

### Direct 및 GitOps 사용

Yogabase를 통한 Direct 운영이 권위 있습니다. GitOps 지원은 고객의 자체 로컬 Git 워크플로를 위해 결정론적 YAML과 diff를 내보냅니다. Yogabase는 브랜치를 push하거나 pull request를 만들거나 고객 저장소에 쓰지 않습니다.

## 애플리케이션 런타임 및 SDK 경계

Yogabase는 승인된 애플리케이션 작업을 위한 대상 로컬 애플리케이션 게이트웨이를 노출할 수 있습니다. 이 SDK가 구현하는 경계는 다음과 같습니다.

- Bootstrap은 Secret이 아닌 프로젝트 바인딩 REST, ID 및 별도 배포된 realtime 엔드포인트 구성을 반환합니다.
- 인증은 회원가입, 로그인/로그아웃, 이메일 확인, 비밀번호 재설정, MFA 및 복구 코드, 외부 ID 콜백과 함께 PKCE authorization code 및 순환 refresh token을 지원합니다. SDK에는 기밀 클라이언트 자격 증명이 포함되지 않습니다.
- 데이터는 승인된 형식화 작업만 노출합니다. 임의 SQL은 클라이언트 표면의 일부가 아닙니다.
- 스토리지는 매개되며 digest 및 idempotency에 바인딩되고, 공급자 자격 증명이나 서명된 URL을 반환하지 않습니다.
- Stateless Functions는 제한된 공개 계약으로 정확히 등록된 버전을 호출합니다.
- Actor Lambdas는 버전이 지정된 프로젝트 로컬 인스턴스 하나를 대상으로 하며, Workflow Lambdas는 변경 불가능한 정의 버전 하나를 시작하고 v1 cancel 작업만 노출합니다.
- Realtime은 별도로 반환된 SignalR origin을 사용하여 topic subscribe/unsubscribe, 서명된 Lambda binding 호출, `eventId`로 중복 제거되는 at-least-once 완료 이벤트를 제공합니다. 작성된 .NET 클라이언트는 이 흐름을 직접 구현하며 JSON이 기본 Hub 프로토콜입니다.

클라이언트는 공급자 중립적이고 shard를 인식하지 않습니다. 클라우드 선택기, 클라우드 자격 증명, Kubernetes ID, TServer 주소, tablet/shard 식별자, shard key, 물리적 배치 맵 또는 토폴로지 생성을 받지 않습니다. YugabyteDB가 내부 라우팅과 배치를 소유합니다. Yogabase는 또 다른 데이터 경로 라우터를 도입하지 않고 제한된 상태 및 용량 사실을 관찰합니다.

SDK는 Control Plane 관리 API, 인프라 프로비저닝, fleet 명령, Kubernetes CRD, 승인 워크플로, 원격 셸, `pods/exec`, 일반 알림, 분석, GameSessions 및 공급자 자격 증명을 의도적으로 제외합니다.

## 기능 상태

Yogabase는 서명된 관찰과 릴리스에 고정된 증거를 통해 모듈 및 프로토콜 가용성을 보고합니다. 구성만으로 성공적인 배포로 간주하지 않습니다.

| 영역 | 현재 경계 |
|---|---|
| Core | 주 제품 범위입니다. 내구성 있고 승인 인식적인 워크플로를 통한 관리형 YugabyteDB/YSQL 작업입니다. |
| Extended | Functions, Actor Lambdas, Workflow Lambdas, Dynamic Code, sandboxing 및 Scheduler/Jobs로 범위가 한정됩니다. 각각의 런타임 증거가 적격성을 충족할 때까지 기능은 사용할 수 없습니다. |
| Valkey | Yogabase 소유 C# 조정 경로를 사용합니다. 승격 및 Redis 프로토콜 주장은 실제 적격성 증거에 의해 계속 제한됩니다. |
| RobustMQ | 제한된 Yogabase 소유 C# 수명 주기 포트를 사용합니다. 수명 주기 및 Kafka/AMQP/MQTT 프로토콜 주장은 독립적으로 적격성이 확인될 때까지 사용할 수 없습니다. |

이 README는 제품 아키텍처와 경계를 설명합니다. 비활성화되었거나 증거에 의해 제한된 기능이 현재 사용 가능하다는 약속이 아닙니다.

## 패키지

.NET 이외의 패키지 식별자는 연속성을 위해 기존 프리뷰 이름을 유지합니다. 이는 레지스트리 게시나 지원 약속을 의미하지 않습니다. 버전 `0.2`는 확장된 인증, Actor Lambda, Workflow Lambda 및 bootstrap 계약과 소스 표기 `Yogabase` 수정 사항을 반영합니다.

| 플랫폼 | 위치 | 현재 패키지 ID | 적격성 |
|---|---|---|---|
| .NET | `src/dotnet/Yogabase.Yogabase.Client` | `Yogabase.Yogabase.Client` `0.2.0-preview.1` | 작성된 REST, PKCE 및 SignalR 클라이언트; `netstandard2.1` 및 `net10.0` |
| TypeScript | `languages/typescript` | `@stackyoga/yogabase-client-preview` `0.2.0-preview.1` | Kiota 생성 REST 프리뷰와 작성된 storage-key adapter |
| Python | `languages/python` | `stackyoga-yogabase-client-preview` `0.2.0.dev1` | Kiota 생성 REST 프리뷰와 작성된 storage-key adapter |
| Java/JVM | `languages/java` | `io.stackyoga:yogabase-client-preview` `0.2.0-preview.1` | Kiota 생성 REST 프리뷰 및 현재 Kotlin/JVM 상호 운용 경로 |
| Dart | `languages/dart` | `stackyoga_yogabase_client_preview` `0.2.0-dev.1` | 실험적 Kiota 생성 REST 소스와 작성된 storage-key adapter |

변경 불가능한 API 스냅샷은 `contracts/yogabase-client-v1.openapi.json`이며 SHA-256은 `a5f0e01985dc0e6ef8ecdbb26b9505eff02dd083a529a83e3f97f19826d96497`입니다. 인접한 checksum, Kiota lock 및 `MANIFEST.json`은 이 저장소를 정확한 계약과 generator에 바인딩합니다.

## 로컬 검증

```text
dotnet restore Yogabase.SDKs.slnx --locked-mode
dotnet build Yogabase.SDKs.slnx -c Release --no-restore
dotnet test Yogabase.SDKs.slnx -c Release --no-restore --no-build
dotnet pack src/dotnet/Yogabase.Yogabase.Client/Yogabase.Yogabase.Client.csproj -c Release --no-restore

cd languages/typescript && npm ci && npm test
cd languages/python && python -m unittest discover -s tests
cd languages/java && mvn test
cd languages/dart && dart analyze
```

`scripts/regenerate-clients.sh`를 실행하면 고정된 Kiota 1.34.1 도구를 복원하고 체크인된 계약에서 네 개의 생성 클라이언트를 다시 만듭니다. 각 생성 언어 폴더는 자체 package manifest, 사용 가능한 경우 dependency lock, Kiota lock, 소스, 문서 및 conformance helper를 유지합니다. 다운로드한 도구, 캐시, 의존성 트리와 빌드 중간 산출물은 제외됩니다. 과거 가져온 아티팩트는 `artifacts/imported` 아래에서 변경하지 않으며 현재 빌드는 `artifacts/local` 아래에 둡니다.

## 플랫폼 적격성

생성된 REST 소스만으로 production 플랫폼 통합이 적격해지는 것은 아닙니다. `netstandard2.1` 호환성만으로 Unity 또는 AOT가 적격하다는 뜻도 아닙니다. Android, Flutter, Dart, Kotlin/JVM, Unity IL2CPP, 네이티브 보안 저장소, interactive cookie 처리, redirect 처리, .NET 이외의 SignalR 통합 및 live-gateway conformance는 지원을 주장하기 전에 각각 명시적인 플랫폼 증거가 필요합니다.
