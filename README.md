# game_skill

플레이 가능한 2.5D 메트로배니아를 실험장으로 삼아, 장르 시스템의 구현 방식을
분석하고 상황별 선택 기준을 기록하는 Unity 연구 프로젝트입니다.

이 저장소의 핵심 질문은 “무엇을 구현했는가?”에서 끝나지 않습니다.

- 왜 이 코드 구조가 현재 규모에 적합한가?
- 같은 결과를 만드는 다른 방식은 무엇인가?
- 플레이 감각, 레벨 디자인, 테스트와 확장 비용은 어떻게 달라지는가?
- 요구사항이 바뀌면 언제 다른 구조로 전환해야 하는가?

현재 프로토타입은 이동 능력 획득, 게이트, 연결된 구역, 백트래킹, 적과 보스,
체크포인트와 저장까지 하나의 루프로 검증하는 **Reference Implementation**입니다.

## 연구 안내

- [메트로배니아 개발 연구 계획](Docs/METROIDVANIA_STUDY.md) — 비교 기준과 증거 수준
- [시스템 연구 인덱스](Docs/FEATURE_INDEX.md) — 연구 주제와 23개 구현 사례
- [프로젝트 로드맵](Docs/PROJECT_PLAN.md) — 문서 개편과 비교 실험 순서
- [포트폴리오 운영 계획](Docs/PORTFOLIO_PLAN.md) — 독자, 완료 기준, Git 단위
- [아키텍처 규칙](Docs/ARCHITECTURE.md) — 코드 책임과 실험 경계
- [게임 경험 기준](Docs/GAME_DESIGN.md) — 비교 결과를 평가할 플레이 목표

### 연구 축

| 연구 축 | 핵심 질문 | 대표 문서 |
|---|---|---|
| 이동 범위와 공간 전달 | 직접 제어, 충돌 안정성, 능력별 이동 범위를 어떻게 함께 유지하는가? | [01 이동](Docs/Features/01-Movement.md), [09 카메라](Docs/Features/09-CameraBounds.md) |
| 전투와 이동 권한 | 공격·회피·피격 중에도 자유로운 연계를 어떻게 보장하는가? | [02 전투](Docs/Features/02-Combat.md), [13 적 FSM](Docs/Features/13-EnemyStateMachine.md) |
| 능력 진행과 게이팅 | 능력을 열쇠가 아니라 이동·전투·월드 변화로 만드는가? | [05 능력](Docs/Features/05-AbilitiesAndGates.md), [16 보스](Docs/Features/16-AbilityTrialBoss.md) |
| 연결 월드와 백트래킹 | 재방문을 반복 이동이 아닌 새로운 판단으로 만드는가? | [06 구역](Docs/Features/06-WorldZones.md), [11 보상](Docs/Features/11-BacktrackRewards.md) |
| 실패 복구와 영구 진행 | 사망, Scene 전환, 앱 종료에서 어떤 상태를 복원하는가? | [03 체크포인트](Docs/Features/03-Checkpoint.md), [17 저장](Docs/Features/17-ProgressSave.md) |
| 제작 검증 | 손맛·성능·빌드를 어떤 증거로 반복 검증하는가? | [21 안내](Docs/Features/21-GuidanceAndTutorial.md), [22 성능](Docs/Features/22-PerformanceProfiling.md) |

## 대표 구현 사례

### 이동 상태와 순수 계산 분리

![이동·점프·2단 점프·공중 대시](Media/GIF/01-01-movement-flow.gif)

`SideScrollerMotor`는 Unity 입력, 상태 타이머와 `CharacterController` 실행을
담당하고 `MovementMath`와 `WallTraversalMath`는 Scene 없이 검증 가능한 계산을
담당합니다. 이 선택은 정밀한 직접 제어와 경사면 대응에 유리하지만, 물리적
상호작용을 자동으로 얻는 Rigidbody 방식보다 충돌 규칙을 직접 관리해야 합니다.

자세한 적용 조건과 대안은 [01 이동 시스템](Docs/Features/01-Movement.md)에
정리합니다.

## 프로토타입 범위

- 가속 이동, 코요테 타임, 점프 버퍼, 곡선 대시
- 2단 점프, 공중 대시, 벽 잡기·벽 점프
- 3단 콤보, 공중 공격, 높이 차 자동 조준, 회피 무적
- 능력 획득과 게이트, 네 구역, 영구 지름길, 백트래킹 보상
- Additive Scene 스트리밍, 구역 카메라, 지도와 2.5D Perspective
- 근거리·원거리·돌진 적과 이동 능력 시험 보스
- 체크포인트, 부활, 버전형 JSON 진행 저장
- HUD, Pause, 피드백, 진행형 튜토리얼, 성능 측정과 macOS 빌드

기능 수는 앞으로의 목표가 아닙니다. 이 범위는 장르 시스템 사이의 결합과 구현
대안을 분석하기에 충분한 사례 집합으로 유지합니다.

## 개발 환경

- Unity 6.5 (`6000.5.5f1`)
- macOS ARM64 Editor
- Universal Render Pipeline
- C# / Unity Input System / Unity Test Framework
- PC 우선, Git LFS 사용

## 프로젝트 실행

1. Unity Hub에서 Unity `6000.5.5f1` macOS ARM64를 설치합니다.
2. `Add project from disk`로 저장소 루트를 선택합니다.
3. 패키지와 `Library` 임포트가 끝날 때까지 기다립니다.
4. `Assets/Scenes/Main.unity`를 열고 Play합니다.

이전 Editor가 비정상 종료되어 Scene 복구 창이 나타나면 먼저 `Yes`로 백업을
보존하고, 원본 `Main`과 비교한 뒤 필요한 복구 파일만 사용합니다.

## 조작

| 행동 | 키보드 | 게임패드 |
|---|---|---|
| 이동 | `A/D` | 왼쪽 스틱 |
| 점프 | `Space` | South 버튼 |
| 대시·회피 | `Left Shift` | East 버튼 |
| 대시 후 달리기 | 대시 키를 유지하며 이동 | East 버튼 유지 |
| 공격 | `Enter` | West 버튼 |
| 일시정지 | `Esc` | Start 버튼 |

게임패드 액션 매핑은 구성되어 있지만 실제 장치 감각 조정은 연구 로드맵에 남아
있습니다.

## 코드와 검증 구조

```text
Assets/
├── Scenes/
│   ├── Main.unity              # 통합 장르 루프
│   └── CaptureStudio.unity     # 단일 기능 실험·촬영
├── Scripts/
│   ├── Runtime/                # Reference Implementation
│   └── Editor/                 # 빌드·제작 도구
└── Tests/
    ├── EditMode/               # 계산·상태·저장 계약
    └── PlayMode/               # 물리·Scene 통합
Docs/
├── Features/                   # 구현 사례별 비교 연구
├── METROIDVANIA_STUDY.md
├── FEATURE_INDEX.md
└── FEATURE_TEMPLATE.md
Media/
├── GIF/
├── Screenshots/
└── Diagrams/
```

기본 의존 방향은 다음과 같습니다.

```text
Input → Controller → 순수 규칙·런타임 상태 → Physics·World
                                               ↓
                                      Feedback·UI·Save
```

세부 원칙과 예외는 [아키텍처 규칙](Docs/ARCHITECTURE.md)에 기록합니다.

## 현재 진행 상태

- 플레이 가능한 Reference Implementation: 완료
- 전체 동선 수동 회귀 테스트: 완료
- macOS Development Build 스모크 테스트: 완료
- 연구 방법과 중앙 문서 체계 개편: 진행 중
- 기능 문서 23개의 비교 연구 형식 전환: 진행 중
- 게임패드 실기와 Windows 빌드: 장비·모듈 준비 후 진행

## 문서·코드 작업 규칙

- 현재 동작을 테스트로 고정한 뒤 구조를 변경합니다.
- 기능 문서 하나를 완료하면 관련 파일만 커밋하고 푸시합니다.
- 새 기능보다 기존 선택의 분석과 유효한 비교 실험을 우선합니다.
- C# 코드 상단에는 골든 스탠다드와 책임 경계를 작성합니다.
- 함수, 조건과 반복문에는 결과가 아니라 의도를 설명하는 한글 주석을 둡니다.
- `Library`, `Temp`, `Logs`, 빌드 결과와 Unity 복구 파일은 커밋하지 않습니다.

## 참고 방향과 에셋

`ENDER MAGNOLIA: Bloom in the Mist`의 횡스크롤 탐색, 수직 동선, 능력 기반
백트래킹과 전투 가독성을 디자인 관찰 기준으로 사용합니다. 원작의 코드,
캐릭터, 세계관, 맵과 시각 자산은 복제하지 않습니다.

프로토타입은 Kenney와 Quaternius의 CC0 모델·애니메이션을 사용하며 원본
라이선스는 `Assets/Art/ThirdParty` 아래에 보존합니다.
