# 메트로배니아 시스템 연구 인덱스

이 문서는 기능 목록이 아니라 연구 질문을 찾는 지도다. 주제별 경로에서는 서로
연관된 장르 시스템을 함께 읽고, 구현 순서 경로에서는 `01 → 23`으로 코드 계약의
발전 과정을 읽는다.

문서 상태 표기:

- `기존`: 구현 기록은 있으나 새 비교 템플릿으로 개편하기 전
- `개편 중`: 연구 질문과 비교 기준을 보강하는 중
- `연구 완료`: 코드·대안·전환 기준·증거가 모두 정리됨

## 1. 연구 축별 읽기

### A. 이동 범위와 공간 전달

핵심 질문: 조작 반응성과 충돌 안정성, 획득 능력에 따른 이동 범위, 2.5D 공간의
가독성을 어떻게 함께 유지할 것인가?

| 문서 | 현재 선택 | 비교할 핵심 대안 |
|---|---|---|
| [01 이동 시스템](Features/01-Movement.md) | `CharacterController` 실행 + 순수 계산 + 명시적 상태 타이머 | Rigidbody 기반 이동, Root Motion, 단일 거대 Controller |
| [09 카메라 제한 영역](Features/09-CameraBounds.md) | 구역별 Bounds와 추적 위치 Clamp | Collider Confiner, 방 단위 고정 카메라 |
| [12 원근 카메라](Features/12-PerspectiveCamera.md) | 정면 Perspective와 FOV·거리 계산 | Orthographic, Cinemachine 렌즈 전환 |

연결해서 볼 항목: 능력 해금 `05`, 월드 구역 `06`, 벽 이동을 시험하는 보스
`16`.

### B. 전투와 이동 권한

핵심 질문: 공격·회피·피격이 플레이어 이동을 과도하게 잠그지 않으면서 명확한
위험과 대응 시간을 만들려면 책임을 어떻게 나눌 것인가?

| 문서 | 현재 선택 | 비교할 핵심 대안 |
|---|---|---|
| [02 전투 시스템](Features/02-Combat.md) | 코드 기반 콤보 상태·입력 버퍼·분리된 대상 선택 | Animator 상태 머신 중심, 데이터 기반 공격 에셋 |
| [13 근거리 적](Features/13-EnemyStateMachine.md) | 순수 판단 계산 + 실행 Controller | MonoBehaviour 단일 FSM, Behavior Tree |
| [14 원거리 적](Features/14-RangedEnemy.md) | 발사 판단과 독립 투사체 생명주기 | Raycast 즉발, 풀링된 투사체 |
| [15 돌진 적](Features/15-ChargeEnemy.md) | 선딜 시 방향 고정과 명시적 중단 조건 | 지속 추적 돌진, NavMesh 이동 |
| [20 플레이어 피드백](Features/20-PlayerFeedback.md) | 확정 이벤트를 표현 계층이 구독 | 판정 코드의 직접 효과 호출, 중앙 이벤트 버스 |

### C. 능력 진행과 게이팅

핵심 질문: 능력이 Boolean 열쇠에 머물지 않고 이동·전투·월드 재해석을 동시에
바꾸게 하려면 데이터와 조건을 어떻게 표현할 것인가?

| 문서 | 현재 선택 | 비교할 핵심 대안 |
|---|---|---|
| [05 능력 해금과 게이트](Features/05-AbilitiesAndGates.md) | ScriptableObject 정의 + 영구 ID `HashSet` | 개별 Boolean, Enum 비트 플래그, 조건식 그래프 |
| [11 백트래킹 보상](Features/11-BacktrackRewards.md) | 수집 ID와 영구 능력치 효과 분리 | 오브젝트 활성 상태 저장, 범용 퀘스트 플래그 |
| [16 능력 시험 보스](Features/16-AbilityTrialBoss.md) | 이동 능력을 요구하는 순환 패턴 | 확률 가중 패턴, 체력 단계별 패턴 그래프 |

### D. 연결된 월드와 백트래킹

핵심 질문: 플레이어가 로딩과 경계 흔들림 없이 구역을 이동하고, 재방문 시
현재 위치와 새 경로를 이해하게 하려면 무엇을 영구 상태로 관리할 것인가?

| 문서 | 현재 선택 | 비교할 핵심 대안 |
|---|---|---|
| [06 월드 구역](Features/06-WorldZones.md) | 영구 구역 ID + Trigger + 경계 히스테리시스 | Scene 이름 식별, 좌표 기반 판정 |
| [07 영구 지름길](Features/07-WorldShortcuts.md) | 지름길 ID를 월드 상태에 저장 | Scene 오브젝트 자체 상태 저장, 능력 게이트 재사용 |
| [08 Scene 스트리밍](Features/08-WorldStreaming.md) | Main의 영구 시스템 + Additive 표현 Scene | 단일 Scene, Addressables 기반 스트리밍 |
| [10 월드 지도](Features/10-WorldMap.md) | 월드 상태를 노드 시각 상태로 투영 | 실제 지형 렌더, Tile 기반 지도 |

카메라 경계 `09`, 원근 구도 `12`, 저장 `17`이 이 축의 결과를 플레이어 화면과
다음 세션까지 연결한다.

### E. 실패 복구와 영구 진행

핵심 질문: 사망·Scene 전환·앱 종료처럼 실행 범위가 달라지는 사건에서 무엇을
즉시 복구하고 무엇을 영구 보존해야 하는가?

| 문서 | 현재 선택 | 비교할 핵심 대안 |
|---|---|---|
| [03 체크포인트](Features/03-Checkpoint.md) | 표현 Trigger와 플레이어 체크포인트 상태 분리 | 정적 전역 매니저, Scene별 Spawn Point |
| [04 사망과 재시작](Features/04-Respawn.md) | 사망 이벤트 뒤 중앙 복구 순서 실행 | Scene 전체 Reload, 시스템별 자율 복구 |
| [17 진행 저장](Features/17-ProgressSave.md) | 버전형 JSON DTO + 영구 ID + 명시적 마이그레이션 | PlayerPrefs, Unity 참조 직렬화, 데이터베이스 |
| [18 진행 HUD](Features/18-ProgressHud.md) | 변경 이벤트 구독 | 매 프레임 Polling, MVVM 바인딩 |
| [19 일시정지와 옵션](Features/19-PauseAndOptions.md) | 한 소유자가 시간·입력·오디오 상태 조정 | 각 시스템 개별 Pause, 상태 스택 |

### F. 학습 전달과 제작 검증

핵심 질문: 플레이어에게 다음 행동을 가르치고, 개발자는 성능과 빌드 결과를
반복해서 같은 방식으로 검증하려면 어떤 관찰 장치가 필요한가?

| 문서 | 현재 선택 | 비교할 핵심 대안 |
|---|---|---|
| [21 길 찾기와 튜토리얼](Features/21-GuidanceAndTutorial.md) | 실제 성공 이벤트 기반 단계 진행 + 재사용 비콘 | 위치 Trigger만 사용하는 튜토리얼, 다수 표식 배치 |
| [22 성능 기준선](Features/22-PerformanceProfiling.md) | 워밍업과 측정 창을 둔 런타임 Probe | Editor Profiler 수동 캡처, 자동 Performance Test |
| [23 데스크톱 빌드](Features/23-DesktopBuild.md) | 코드로 Scene·플랫폼·옵션 고정 | 수동 Build Settings, CI 빌드 |

## 2. 구현 순서와 연구 상태

기존 번호는 구현 이력, Git 커밋, `Media/GIF/NN-*` 촬영 파일을 안정적으로
연결하기 위해 유지한다.

| 순서 | 기능 | 앞 단계에서 이어받은 계약 | 이번 단계가 추가한 장르 의미 | 상태 |
|---:|---|---|---|---|
| 01 | [이동](Features/01-Movement.md) | 입력과 물리 기반 | 탐험 가능한 거리와 조작 문법 | 개편 중 |
| 02 | [전투](Features/02-Combat.md) | 이동 상태와 방향 | 이동을 유지하는 공격 리듬 | 기존 |
| 03 | [체크포인트](Features/03-Checkpoint.md) | 체력과 위치 | 탐험 실패 비용의 기준점 | 기존 |
| 04 | [사망·재시작](Features/04-Respawn.md) | 체크포인트 상태 | 실패 뒤 일관된 월드 복귀 | 기존 |
| 05 | [능력·게이트](Features/05-AbilitiesAndGates.md) | 이동 능력과 영구 ID | 획득 전후 월드 해석 변화 | 기존 |
| 06 | [월드 구역](Features/06-WorldZones.md) | 영구 ID와 플레이어 위치 | 연결된 장소의 정체성과 방문 상태 | 기존 |
| 07 | [영구 지름길](Features/07-WorldShortcuts.md) | 월드 진행 상태 | 재방문의 이동 비용 감소 | 기존 |
| 08 | [Scene 스트리밍](Features/08-WorldStreaming.md) | 현재·이웃 구역 | 연결감을 유지하는 콘텐츠 수명주기 | 기존 |
| 09 | [카메라 경계](Features/09-CameraBounds.md) | 현재 구역 | 방 구도와 플레이어 가시성 | 기존 |
| 10 | [월드 지도](Features/10-WorldMap.md) | 방문·현재 구역 | 백트래킹 중 위치 이해 | 기존 |
| 11 | [백트래킹 보상](Features/11-BacktrackRewards.md) | 능력과 수집 상태 | 과거 장소의 새 가치 | 기존 |
| 12 | [원근 카메라](Features/12-PerspectiveCamera.md) | 측면 추적 | 2.5D 공간의 깊이 전달 | 기존 |
| 13 | [근거리 적](Features/13-EnemyStateMachine.md) | 전투·피격 계약 | 거리와 선딜을 읽는 조우 | 기존 |
| 14 | [원거리 적](Features/14-RangedEnemy.md) | 무적·투사체 충돌 | 대시 타이밍 시험 | 기존 |
| 15 | [돌진 적](Features/15-ChargeEnemy.md) | 예고·회피 계약 | 위치 선정과 직선 회피 시험 | 기존 |
| 16 | [능력 시험 보스](Features/16-AbilityTrialBoss.md) | 세 이동 능력과 적 패턴 | 획득 능력의 종합 시험 | 기존 |
| 17 | [진행 저장](Features/17-ProgressSave.md) | 모든 영구 ID | 세션을 넘는 장르 진행 | 기존 |
| 18 | [진행 HUD](Features/18-ProgressHud.md) | 체력·능력·저장 이벤트 | 현재 상태의 즉시 전달 | 기존 |
| 19 | [Pause·옵션](Features/19-PauseAndOptions.md) | 저장 제어와 입력 | 안전한 중단과 설정 유지 | 기존 |
| 20 | [피드백](Features/20-PlayerFeedback.md) | 확정된 gameplay 이벤트 | 공격·회피 성공의 가독성 | 기존 |
| 21 | [안내·튜토리얼](Features/21-GuidanceAndTutorial.md) | 진행 상태와 월드 위치 | 설명보다 행동으로 배우는 흐름 | 기존 |
| 22 | [성능 측정](Features/22-PerformanceProfiling.md) | 통합 플레이 구간 | 규모 확장 전 기술 예산 | 기존 |
| 23 | [데스크톱 빌드](Features/23-DesktopBuild.md) | Scene과 프로젝트 설정 | 재현 가능한 공개 결과 | 기존 |

## 3. 문서와 미디어 연결 규칙

- 기능 번호와 촬영 파일 번호는 동일하게 유지한다.
- GIF는 단순 동작 나열보다 비교 조건이나 상태 전환 전후를 보여준다.
- 스크린샷은 코드 책임, 상태 흐름, 월드 연결 중 화면만으로 보이지 않는 구조를
  설명할 때 사용한다.
- README에는 연구 축별 대표 결론만 두고, 자세한 비교와 실패 사례는 각 기능
  문서에 둔다.
- 새 기능은 기존 번호 뒤에 무조건 추가하지 않는다. 먼저 어느 연구 질문을
  검증하는지 정하고, 독립된 연구 가치가 있을 때만 번호를 부여한다.

비교 방법과 증거 수준은 [메트로배니아 개발 연구 계획](METROIDVANIA_STUDY.md),
작성 형식은 [기능 문서 템플릿](FEATURE_TEMPLATE.md)을 따른다.
