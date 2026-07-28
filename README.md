# game_skill

Unity 기능을 작은 단위로 직접 구현하면서 완성하는 2.5D 횡스크롤 메트로배니아
개인 프로젝트입니다.

## 개발 환경

- 엔진: Unity 6.5 (`6000.5.5f1`)
- 에디터 아키텍처: macOS ARM64 권장
- 렌더 파이프라인: URP 예정
- 언어: C#
- 대상 플랫폼: PC 우선
- 저장소: `oscar87657/game_skill`

> Unity Hub에서 Unity `6000.5.5f1`을 설치한 뒤 이 폴더를 프로젝트로 열어
> 주세요. Apple Silicon Mac에서는 macOS ARM64 에디터를 사용합니다.

## 목표

- 서로 연결된 3D 공간을 탐험하고 새로운 이동 능력으로 기존 지역을 다시
  탐색하는 메트로배니아 구조를 구현합니다.
- 이동, 전투, 능력 해금, 게이트, 체크포인트를 독립된 시스템으로 구성합니다.
- 첫 공개 목표는 20~30분 분량의 플레이 가능한 수직 슬라이스입니다.

## 진행 현황

- [x] Git 및 Unity 프로젝트 기본 구조
- [x] 개발 계획서
- [x] 2.5D 횡스크롤 메트로배니아 장르와 수직 슬라이스 범위 확정
- [x] CC0 캐릭터 및 프로토타입 에셋
- [x] URP Main Scene과 플레이어 이동 프로토타입
- [x] 횡스크롤 이동 및 측면 추적 카메라 프로토타입
- [x] 정면 구도를 유지하는 약한 Perspective 카메라
- [x] Kenney 캐릭터 및 이동·점프 애니메이션
- [x] 지상 대시와 짧은 무적 시간
- [x] 기본 공격·체력·훈련용 더미 프로토타입
- [x] 3단 기본 공격 콤보와 입력 버퍼
- [x] 상호작용 및 체크포인트
- [x] 체력, 데미지 및 전투
- [x] ScriptableObject 기반 이동 능력 해금 및 첫 능력 게이트
- [x] 짧은 벽 잡기·벽 미끄러짐·벽 점프 능력
- [x] 세 구역 Graybox와 ID 기반 방문 상태
- [x] 백트래킹 샤프트에서 시작 홀로 돌아오는 영구 지름길
- [x] 세 구역 Additive Scene 비동기 스트리밍
- [x] 구역 진입 이벤트 기반 카메라 제한 영역
- [x] 현재 위치와 방문 구역을 표시하는 지도 HUD
- [x] 벽 잡기로 다시 방문해 획득하는 최대 체력 조각
- [x] 연결된 월드와 백트래킹
- [x] 첫 근거리 적과 탐지·추적·공격 상태 머신
- [x] 고정형 원거리 적과 회피 가능한 직선 투사체
- [x] 방향을 잠그고 발판 끝에서 멈추는 돌진 적
- [x] 세 이동 능력을 활용하게 만드는 순환 패턴 보스
- [x] 능력·체크포인트·월드 상태 세이브/로드
- [x] 체력·능력·저장 상태 HUD
- [x] 일시정지·저장 메뉴와 마스터 음량 옵션
- [ ] 보스와 수직 슬라이스 빌드

자세한 내용은 [게임 디자인](Docs/GAME_DESIGN.md)과
[프로젝트 계획서](Docs/PROJECT_PLAN.md)를 참고하세요.

## 포트폴리오 문서

이 저장소는 하나의 게임을 완성하면서 기능별 구현 방식과 선택 근거를
비교·기록하는 포트폴리오로 운영합니다.

- [포트폴리오 운영 계획](Docs/PORTFOLIO_PLAN.md)
- [기능 문서 인덱스](Docs/FEATURE_INDEX.md)
- [아키텍처 규칙](Docs/ARCHITECTURE.md)
- [기능 문서 템플릿](Docs/FEATURE_TEMPLATE.md)
- [이동 시스템](Docs/Features/Movement.md)
- [전투 시스템](Docs/Features/Combat.md)
- [체크포인트 시스템](Docs/Features/Checkpoint.md)
- [사망과 재시작](Docs/Features/Respawn.md)
- [능력 해금과 게이트](Docs/Features/AbilitiesAndGates.md)
- [월드 구역과 방문 상태](Docs/Features/WorldZones.md)
- [구역 연결과 영구 지름길](Docs/Features/WorldShortcuts.md)
- [Additive 구역 Scene 스트리밍](Docs/Features/WorldStreaming.md)
- [구역별 카메라 제한 영역](Docs/Features/CameraBounds.md)
- [정면 원근 2.5D 카메라](Docs/Features/PerspectiveCamera.md)
- [현재 위치와 방문 상태 지도 HUD](Docs/Features/WorldMap.md)
- [능력 기반 백트래킹과 최대 체력 보상](Docs/Features/BacktrackRewards.md)
- [근거리 적과 탐지·추적·공격 상태 머신](Docs/Features/EnemyStateMachine.md)
- [고정형 원거리 적과 직선 투사체](Docs/Features/RangedEnemy.md)
- [방향 잠금 돌진 적과 상태 중단](Docs/Features/ChargeEnemy.md)
- [능력 시험 보스와 독립 보스방](Docs/Features/AbilityTrialBoss.md)
- [플레이어 진행 저장](Docs/Features/ProgressSave.md)
- [체력·능력·저장 상태 HUD](Docs/Features/ProgressHud.md)
- [일시정지와 옵션 메뉴](Docs/Features/PauseAndOptions.md)
- 시연 자료: [Media/README](Media/README.md)

## 프로젝트 열기

1. Unity Hub에서 Unity `6000.5.5f1` macOS ARM64를 설치합니다.
2. `Add project from disk`를 선택합니다.
3. 이 저장소의 루트 폴더를 선택합니다.
4. Unity가 패키지와 `Library` 폴더 생성을 마칠 때까지 기다립니다.
5. `Assets/Scenes`에 첫 Scene을 만들고 실행합니다.

## 현재 조작

- 이동: `A/D` 또는 왼쪽 스틱 좌우
- 점프: `Space` 또는 게임패드 South 버튼
- 대시: `Left Shift` 또는 게임패드 East 버튼
- 대시 후 계속 달리기: `Left Shift`를 계속 누르고 이동
- 공격: `Enter` 또는 게임패드 West 버튼
- 일시정지: `Esc` 또는 게임패드 Start 버튼

현재 플레이어는 Quaternius의 사람 비율 Humanoid 모델을 사용합니다. 이동
속도에 따라 Idle, Walk, Jog, Sprint가 블렌딩되며 점프와 공격 상태도 Humanoid
애니메이션으로 재생됩니다. 대시 속도는 시작과 종료가 느리고 중간이 빠른
포물선형으로 적용됩니다. 점프는 기본 점프 후 한 번 더 사용할 수 있으며,
2단 점프와 공중 대시는 월드의 능력 구체를 획득한 뒤 사용할 수 있습니다.
점프할 때마다 획득한 공중 대시가 다시 충전됩니다. 지상 대시는 처음부터 사용할 수
있고, 공중 대시는 한 번 사용하면 착지하거나 다시 점프할 때까지 충전되지 않습니다.
대시 중에는 중력이 멈추며
이동 `0.2초`와 종료 후 여유를 포함해 총 `0.3초` 동안 무적 상태가 됩니다.
공중 공격은 공격 순간에만 약 0.06초
동안 수직 속도가 완화되고 곧바로 다시 낙하합니다.
긴 부유 대신 공격 중 수평 조작과 대시 연계를 우선합니다. 대시 키를 계속 누르면 대시 종료 후 달리기로
이어집니다. 이동은 X축, 점프는 Y축을 사용하며 깊이 Z축은 고정됩니다.

## 능력 진행 테스트

1. 시작 상태에서는 2단 점프와 공중 대시가 잠겨 있습니다.
2. 오른쪽 체크포인트와 얇은 위험 지대를 지나 `x=7`의 구체를 획득합니다.
3. `2단 점프` 획득 로그를 확인하고 시작 지점 왼쪽으로 돌아옵니다.
4. 기존 `Wall_Gate`가 열렸는지 확인합니다.
5. 오른쪽 계단을 2단 점프로 올라 `x=20`의 높은 발판에서 `공중 대시`를
   획득합니다.
6. 공중 대시 게이트 뒤의 구체에서 `벽 잡기`를 획득합니다.
7. 시작 지점 왼쪽으로 돌아가 두 벽 사이에서 방향키를 벽 쪽으로 유지합니다.
8. 짧은 정지와 느린 미끄러짐을 확인하고 Space로 반대편 벽을 향해 점프합니다.

## 방향성 참고

`ENDER MAGNOLIA: Bloom in the Mist`의 횡스크롤 탐색, 수직 동선과 능력 기반
백트래킹을 참고합니다. 원작의 캐릭터, 세계관, 맵과 시각 자산은 복제하지 않고
조작 감각과 레벨 설계 원칙만 연구합니다.

## 폴더 구조

```text
Assets/
├── Art/
│   └── ThirdParty/
├── Audio/
├── Materials/
├── Prefabs/
├── Scenes/
├── Scripts/
│   ├── Editor/
│   └── Runtime/
└── Tests/
    ├── EditMode/
    └── PlayMode/
Docs/
├── Features/
├── ARCHITECTURE.md
├── FEATURE_INDEX.md
├── FEATURE_TEMPLATE.md
├── PORTFOLIO_PLAN.md
Media/
├── GIF/
├── Screenshots/
└── Diagrams/
Packages/
ProjectSettings/
```

## 작업 규칙

- 기능 작업은 `feature/<기능명>` 브랜치에서 진행합니다.
- 한 커밋에는 하나의 논리적인 변경만 담습니다.
- `Library`, `Temp`, `Logs`, 빌드 결과물은 커밋하지 않습니다.
- 큰 바이너리 에셋을 추가하기 전에 Git LFS를 설치하고 추적 규칙을 정합니다.

## 포함된 외부 에셋

- Kenney Blocky Characters 2.0: 애니메이션 캐릭터 18종
- Kenney Prototype Kit 1.0: 테스트 레벨용 모델 145종
- Kenney Platformer Kit 4.1: 플레이어 캐릭터와 이동·점프 애니메이션
- Quaternius Universal Base Characters: 사람 비율 Humanoid 플레이어 모델
- Quaternius Universal Animation Library: Humanoid 이동·점프 애니메이션

외부 에셋은 CC0 라이선스이며 원본 라이선스는 `Assets/Art/ThirdParty` 아래에
보존합니다. FBX와 PNG 파일은 Git LFS로 관리합니다.

커밋 메시지 예시:

```text
feat: 플레이어 이동 기능 추가
fix: 공중에서 점프가 중복 실행되는 문제 수정
refactor: 체력 계산을 공통 컴포넌트로 분리
docs: 능력 게이트 설계 설명 추가
test: 데미지 계산 EditMode 테스트 추가
```
