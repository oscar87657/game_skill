# 기능 구현 인덱스

이 문서는 기능 이름을 알파벳순으로 나열하지 않는다. 실제 프로젝트에 처음
통합한 순서대로 `01`부터 번호를 붙여, 앞 단계의 코드가 다음 시스템으로 어떻게
확장됐는지 읽을 수 있게 구성한다.

각 기능 문서는 다음 질문에 답하는 기술 기록이다.

1. 어떤 플레이 문제를 해결하려 했는가?
2. 어떤 코드가 입력·판단·상태·표현을 담당하는가?
3. 여러 구현 방식 중 왜 현재 방식을 골랐는가?
4. 화면에서는 어떤 동작으로 확인할 수 있는가?
5. 테스트와 시연 자료가 그 설명을 어떻게 증명하는가?

## 구현 순서

| 순서 | 단계 | 해결한 문제 | 핵심 코드 | 사용한 구조와 선택 이유 | 화면에서 확인할 결과 | 문서 |
|---:|---|---|---|---|---|---|
| 01 | M1 | 정밀한 2.5D 이동, 점프, 대시, 경사·벽 이동 | `SideScrollerMotor`, `MovementMath`, `WallTraversalMath` | `CharacterController`로 물리 충돌과 직접 제어를 결합하고 계산은 순수 함수로 분리 | 가속 이동, 코요테 타임, 점프 버퍼, 곡선 대시, 2단 점프, 벽 점프 | [이동 시스템](Features/01-Movement.md) |
| 02 | M2 | 이동을 막지 않는 지상·공중 콤보와 높이 차 공격 | `PlayerCombat`, `CombatMath`, `TargetingMath`, `SideScrollerTargeting` | `OverlapBox` 판정과 NonAlloc 자동 조준을 분리해 공격 타이밍과 대상 선택을 독립 검증 | 3단 콤보, 입력 버퍼, 공중 공격 체공, 정면·상하 자동 조준 | [전투 시스템](Features/02-Combat.md) |
| 03 | M2 | 체크포인트 표현과 플레이어 진행 상태의 결합 제거 | `Checkpoint`, `PlayerCheckpointState`, `Health` | Trigger는 활성 요청만 하고 ID·부활 좌표와 체력은 각 상태 객체가 소유 | 체크포인트 활성화, 완전 회복, 재시작 위치 갱신 | [체크포인트](Features/03-Checkpoint.md) |
| 04 | M2 | 사망 뒤 이동·전투·체력을 일관된 순서로 복구 | `PlayerRespawnController`, `RespawnMath`, `DamageVolume` | 사망 이벤트 이후의 복구 순서를 하나의 오케스트레이터에 집중 | 조작 잠금, 체크포인트 이동, 체력 회복, 적 상태 초기화 | [사망과 재시작](Features/04-Respawn.md) |
| 05 | M3 | 능력 수가 늘어도 플레이어와 저장 코드를 계속 수정하는 문제 | `AbilityDefinition`, `PlayerAbilityState`, `AbilityPickup`, `AbilityGate` | ScriptableObject 정의와 ID `HashSet`으로 데이터와 플레이어 보유 상태를 분리 | 2단 점프·공중 대시·벽 잡기 획득, 요구 게이트 개방 | [능력 해금과 게이트](Features/05-AbilitiesAndGates.md) |
| 06 | M4 | 구역·지도·저장이 서로 다른 위치 식별자를 사용하는 문제 | `WorldZoneDefinition`, `WorldZoneVolume`, `PlayerWorldState`, `WorldZoneBoundaryMath` | 영구 ID 정의와 경계 히스테리시스로 방문 상태와 물리 Trigger를 분리 | 네 구역 방문, 경계에서 카메라 판정이 흔들리지 않는 전환 | [월드 구역](Features/06-WorldZones.md) |
| 07 | M4 | 백트래킹이 같은 길을 반복하게 되는 문제 | `WorldShortcutGate`, `ShortcutUnlockVolume`, `PlayerWorldState` | 지름길 ID를 영구 진행 상태로 기록하고 월드 표현은 이벤트로 복원 | 샤프트 정상에서 시작 홀 귀환 통로가 영구 개방 | [영구 지름길](Features/07-WorldShortcuts.md) |
| 08 | M4 | 하나의 큰 Scene에 모든 구역을 계속 유지하는 문제 | `WorldZoneStreamController`, `WorldZoneSceneBinding` | 영구 시스템은 Main에 두고 구역 표현만 Additive 비동기 로드 | 현재 구역과 이웃 구역을 로딩 화면 없이 교체 | [Scene 스트리밍](Features/08-WorldStreaming.md) |
| 09 | M4 | 구역 밖이나 빈 공간을 비추는 추적 카메라 | `CameraZoneBounds`, `CameraBoundsMath`, `SideScrollerCamera` | 원하는 카메라 위치를 구역별 허용 범위로 제한 | 구역 전환 중 플레이어를 놓치지 않는 중심점 추적 | [카메라 제한 영역](Features/09-CameraBounds.md) |
| 10 | M4 | 방문 여부와 현재 위치를 플레이어가 파악하기 어려운 문제 | `WorldMapPresenter`, `WorldMapNodeView`, `WorldMapConnectionView` | 월드 상태 이벤트를 지도 시각 상태로 투영 | 미발견·방문·현재 구역이 다른 상태로 표시 | [월드 지도](Features/10-WorldMap.md) |
| 11 | M4 | 새 이동 능력을 얻어도 이전 구역을 다시 찾을 이유가 부족한 문제 | `BacktrackRewardPickup`, `PlayerWorldState`, `Health` | 수집 ID와 최대 체력 효과를 분리해 저장 복원도 결정적으로 처리 | 벽 이동으로 샤프트를 올라 체력 조각 획득 | [백트래킹 보상](Features/11-BacktrackRewards.md) |
| 12 | M1 보강 | 완전한 정면 2D 구도의 입체감 부족 | `CameraPerspectiveMath`, `SideScrollerCamera` | 정면 회전은 고정하고 FOV와 거리만 계산하는 Perspective 카메라 사용 | 화면 가장자리에서 배경과 오브젝트 옆면이 드러나는 2.5D 구도 | [원근 카메라](Features/12-PerspectiveCamera.md) |
| 13 | M5 | 공격 경계에서 상태가 떨리고 선딜 회피가 무시되는 근거리 AI | `EnemyDecisionMath`, `MeleeEnemyController`, `DamageRules` | 판단은 순수 상태 계산, 실행은 MonoBehaviour로 분리하고 탐지 히스테리시스 적용 | 탐지·추적·선딜·재검사·공격·후딜·피격 상태 | [근거리 적 상태 머신](Features/13-EnemyStateMachine.md) |
| 14 | M5 | 원거리 공격이 회피 무적과 충돌 생명주기를 구분하지 못하는 문제 | `RangedEnemyDecisionMath`, `RangedEnemyController`, `EnemyProjectile` | 발사 판단과 투사체 이동·충돌을 별도 객체로 분리 | 충전 예고, 직선 탄환, 대시 중 통과 후 계속 날아가는 투사체 | [원거리 적](Features/14-RangedEnemy.md) |
| 15 | M5 | 돌진 중 방향 변경과 발판 이탈로 패턴이 읽히지 않는 문제 | `ChargeEnemyDecisionMath`, `ChargeEnemyController` | 선딜 순간 방향을 잠그고 벽·피격·발판 끝을 명시적 중단 조건으로 처리 | 방향 예고, 직선 돌진, 회피 뒤 명확한 후딜 | [돌진 적](Features/15-ChargeEnemy.md) |
| 16 | M5 | 획득한 세 이동 능력을 전투에서 사용할 이유가 부족한 문제 | `AbilityTrialBossController`, `BossPatternDecisionMath`, `BossPattern` | 패턴 선택 계산과 실제 생성·연출을 분리한 순환 패턴 | 점프·공중 대시·벽 잡기를 요구하는 세 보스 패턴 | [능력 시험 보스](Features/16-AbilityTrialBoss.md) |
| 17 | M6 | Unity 참조와 Scene 구조에 묶인 진행 저장 | `GameProgressSaveData`, `GameProgressSaveCodec`, `GameProgressSaveController` | 버전형 DTO와 영구 ID JSON으로 저장하고 v1을 v2로 명시적 마이그레이션 | 능력·체크포인트·구역·지름길·보상·보스 상태 왕복 | [진행 저장](Features/17-ProgressSave.md) |
| 18 | M6 | 체력·능력·저장 결과가 Console에서만 보이는 문제 | `GameProgressHud`, `AbilityHudSlot` | 상태를 매 프레임 조회하지 않고 변경 이벤트를 구독 | 체력, 해금 능력, 저장·불러오기 결과 HUD | [진행 HUD](Features/18-ProgressHud.md) |
| 19 | M6 | 정지 중 시간·입력·오디오 상태가 따로 움직이는 문제 | `PauseMenuController` | Pause 상태를 한 컴포넌트가 소유하고 시간 배율과 AudioMixer를 함께 제어 | 재개·저장·불러오기·마스터 음량 조절 | [일시정지와 옵션](Features/19-PauseAndOptions.md) |
| 20 | M7 | 판정 코드에 VFX·SFX 호출이 섞이는 문제 | `PlayerFeedbackController`, `PrototypeAudioSynth` | 게임플레이의 확정 이벤트를 표현 계층이 구독 | 대시 Trail, 공격·명중·피격·능력 획득 파티클과 임시 음향 | [플레이어 피드백](Features/20-PlayerFeedback.md) |
| 21 | M7 | 안내 오브젝트가 늘고 실제 진행과 튜토리얼이 어긋나는 문제 | `GuidanceProgression`, `PlayerGuidanceController`, `WorldGuidanceMarker` | 진행 상태를 순수 규칙으로 계산하고 하나의 비콘을 목적지 사이에서 재사용 | 조작 성공에 따라 목표·힌트·월드 비콘이 단계적으로 변경 | [길 찾기와 튜토리얼](Features/21-GuidanceAndTutorial.md) |
| 22 | M7 | 최적화 여부를 느낌으로 판단하는 문제 | `PerformanceStatistics`, `RuntimePerformanceProbe` | 워밍업 뒤 정해진 창의 p95·최댓값을 측정하고 예산과 비교 | 프레임·GC·렌더 카운터의 Editor·Player 기준선 | [성능 기준선](Features/22-PerformanceProfiling.md) |
| 23 | M7 | 수동 Build Settings에 따라 결과가 달라지는 문제 | `DesktopBuildPipeline` | Scene 순서·ARM64·Development 옵션을 코드로 고정하고 실행 스모크 수행 | 재현 가능한 macOS 앱 생성과 12초 실행 검증 | [데스크톱 빌드](Features/23-DesktopBuild.md) |

## 문서와 촬영 자료 연결 규칙

- 기능 문서 번호와 촬영 파일 번호를 동일하게 사용한다.
- GIF는 플레이어가 보는 결과를 증명하고, 스크린샷은 핵심 코드나 상태 흐름과
  나란히 배치할 장면을 남긴다.
- README에는 대표 기능만 짧게 보여주고, 구현 판단과 대안 비교는 각 기능
  문서에서 설명한다.
- 실제 미디어가 준비되면 `Media/GIF/01-...`, `Media/Screenshots/01-...`
  형식으로 저장한다.
- 새 기능은 마지막 번호 다음에 추가한다. 파일명 알파벳순으로 다시 섞지 않는다.
