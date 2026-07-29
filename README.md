# game_skill

Unity 기능을 작은 단위로 직접 구현하면서 완성하는 2.5D 횡스크롤 메트로배니아
개인 프로젝트입니다.

## 개발 환경

- 엔진: Unity 6.5 (`6000.5.5f1`)
- 에디터 아키텍처: macOS ARM64 권장
- 렌더 파이프라인: URP
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

1. **M0 기반 구성 — 완료**
   Unity 6.5, URP, Input System, Git LFS, CC0 프로토타입 에셋과 2.5D 장르
   범위를 고정했습니다.
2. **M1 이동과 카메라 — 완료**
   CharacterController 이동, 경사·계단, 점프 보정, 달리기, 곡선 대시,
   무적, 2단 점프, 공중 대시, 벽 이동과 Humanoid 애니메이션을 연결했습니다.
3. **M2 전투와 생존 — 완료**
   3단 콤보, 입력 버퍼, 공중 공격, 자동 조준, 공통 체력·데미지 계약,
   체크포인트와 사망 재시작을 구현했습니다.
4. **M3 능력과 게이트 — 완료**
   ScriptableObject 능력 정의, ID 기반 보유 상태와 2단 점프·공중 대시·벽
   잡기 게이트를 구현했습니다.
5. **M4 연결된 월드 — 완료**
   영구 구역 ID, 지름길, Additive Scene 스트리밍, 카메라 경계, 지도,
   백트래킹 체력 보상과 정면 Perspective 구도를 구현했습니다.
6. **M5 적과 보스 — 완료**
   판단 계산과 실행 Controller를 분리한 근거리·원거리·돌진 적, 세 이동
   능력을 시험하는 보스와 독립 아레나를 구현했습니다.
7. **M6 저장과 UI — 완료**
   능력·체크포인트·구역·지름길·보상·보스 상태의 JSON 저장, `v1 → v2`
   마이그레이션, HUD와 Pause·음량 옵션을 구현했습니다.
8. **M7 공개 준비 — 진행 중**
   이벤트 기반 VFX·SFX, 진행형 튜토리얼, 성능 기준선과 macOS Development
   Build 검증을 완료했고, README 시연 자료와 `v0.1.0` Release를 남겼습니다.

자세한 내용은 [게임 디자인](Docs/GAME_DESIGN.md)과
[프로젝트 계획서](Docs/PROJECT_PLAN.md)를 참고하세요.

## 포트폴리오 문서

이 저장소의 중심은 기능 개수가 아니라, 플레이 문제를 코드 책임으로 나누고
Unity에서 동작하는 결과까지 연결한 과정입니다. 문서는 알파벳순이 아니라 실제
구현 순서인 `01 → 23`으로 읽습니다.

- [포트폴리오 운영 계획](Docs/PORTFOLIO_PLAN.md)
- [기능 구현 인덱스 — 문제·핵심 코드·선택 이유·화면 결과](Docs/FEATURE_INDEX.md)
- [아키텍처 규칙](Docs/ARCHITECTURE.md)
- [기능 문서 템플릿](Docs/FEATURE_TEMPLATE.md)
- 시연 자료: [Media/README](Media/README.md)

### 구현 흐름 한눈에 보기

| 구현 단계 | 사용한 코드 구조 | 구현한 동작 |
|---|---|---|
| [01 이동](Docs/Features/01-Movement.md) | `SideScrollerMotor`는 Unity 충돌 실행, `MovementMath`는 순수 계산을 담당 | 가속 이동, 코요테 타임, 점프 버퍼, 곡선 대시, 2단 점프, 벽 이동 |
| [02 전투](Docs/Features/02-Combat.md) | `PlayerCombat`의 공격 타이밍과 `TargetingMath`의 대상 선택을 분리 | 이동 가능한 3단 콤보, 공중 공격, 높이 차 자동 조준 |
| [03~05 생존·능력](Docs/Features/03-Checkpoint.md) | 상태 객체, 이벤트, ScriptableObject 정의와 ID 집합 사용 | 체크포인트, 재시작, 능력 획득과 게이트 |
| [06~12 연결된 월드](Docs/Features/06-WorldZones.md) | 영구 구역 ID, Additive Scene, 상태 기반 지도와 카메라 경계 | 구역 전환, 지름길, 스트리밍, 지도, 백트래킹 보상, 2.5D 원근 |
| [13~16 적과 보스](Docs/Features/13-EnemyStateMachine.md) | 판단용 순수 `*DecisionMath`와 실행용 Controller 분리 | 근거리·원거리·돌진 AI와 세 이동 능력 시험 보스 |
| [17~21 저장·UI·UX](Docs/Features/17-ProgressSave.md) | 버전형 JSON DTO와 변경 이벤트 구독 | 진행 저장, HUD, Pause, 피드백, 단계형 튜토리얼 |
| [22~23 검증·배포](Docs/Features/22-PerformanceProfiling.md) | 측정 Probe와 코드 기반 Build Pipeline | 성능 예산 검증과 재현 가능한 macOS 빌드 |

세부 인덱스에서는 23개 기능마다 해결한 문제, 핵심 클래스, 선택한 구현 방식,
화면에서 확인할 결과를 같은 순서로 연결합니다.

## 프로젝트 열기

1. Unity Hub에서 Unity `6000.5.5f1` macOS ARM64를 설치합니다.
2. `Add project from disk`를 선택합니다.
3. 이 저장소의 루트 폴더를 선택합니다.
4. Unity가 패키지와 `Library` 폴더 생성을 마칠 때까지 기다립니다.
5. `Assets/Scenes/Main.unity`를 열고 Play합니다.

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
