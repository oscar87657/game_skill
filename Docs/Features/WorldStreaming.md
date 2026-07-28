# Additive 구역 Scene 스트리밍

## 목표

플레이어와 핵심 충돌 지형을 가진 `Main` Scene은 계속 유지하면서, 현재 구역의
시각 콘텐츠만 별도 Scene으로 비동기 로드한다. 구역을 이동하면 새 콘텐츠를
먼저 로드하고 이전 콘텐츠를 언로드하여 로딩 화면 없이 Scene 분할 구조를
검증한다.

현재 단계에서는 스트리밍 안정성을 먼저 확인하기 위해 각 Additive Scene이
구역별 색상의 배경 패널을 소유한다. 실제 지형과 게임플레이 오브젝트의 이전은
구역 전환이 충분히 안정화된 뒤 진행한다.

## Scene 구성

```text
Main.unity                         항상 유지
├── Player
├── WorldZoneStreaming
├── WorldZoneVolume × 4
└── 게임플레이 지형·능력·전투

Zones/Zone_BacktrackShaft.unity    현재 구역일 때만 로드
Zones/Zone_StartHall.unity         현재 구역일 때만 로드
Zones/Zone_TraversalLab.unity      현재 구역일 때만 로드
Zones/Zone_BossRoom.unity          보스방 구역일 때만 로드
```

네 구역 Scene은 Build Settings에 등록되어 에디터와 빌드에서 같은 경로로
로드된다.

## 구현 후보 비교

| 방식 | 장점 | 단점 | 적합한 상황 |
|---|---|---|---|
| 모든 콘텐츠를 Main에 유지 | 가장 단순하고 참조가 쉬움 | 월드가 커질수록 메모리와 편집 충돌 증가 | 작은 단일 스테이지 |
| 구역 GameObject 활성 전환 | Scene 참조 문제 없이 빠르게 구현 | 비활성 콘텐츠도 Main Scene과 파일을 공유 | 중간 규모 프로토타입 |
| Additive Scene 비동기 전환 | 구역별 협업·메모리 관리·독립 편집에 유리 | 영구 오브젝트와 Scene 간 참조 규칙 필요 | 연결된 메트로배니아 월드 |

## 런타임 흐름

```text
WorldZoneVolume.Enter
        ↓
PlayerWorldState.EnterZone
        ↓ ZoneEntered
WorldZoneStreamController.RequestZone
        ↓
새 Scene LoadSceneAsync(Additive)
        ↓
이전 Scene UnloadSceneAsync
```

`WorldZoneSceneBinding`이 구역 ScriptableObject와 Scene 경로를 연결한다.
스트리밍 컨트롤러는 빠르게 경계를 왕복해 요청이 바뀌어도 코루틴 하나에서
마지막 요청까지 순서대로 처리한다. 전환 중에는 새 Scene을 먼저 로드해 콘텐츠가
모두 사라지는 빈 프레임을 피한다.

## 영구 오브젝트 규칙

- 플레이어, 진행 상태, 카메라와 스트리밍 제어기는 `Main`에 둔다.
- Additive Scene은 현재 구역의 시각 콘텐츠만 소유한다.
- 구역 Scene에서 다른 구역의 GameObject를 직접 참조하지 않는다.
- 저장 데이터는 Scene 경로 대신 구역 영구 ID를 기록한다.
- 동시에 스트리밍 제어 대상이 되는 구역 콘텐츠 Scene은 최대 하나다.

## 테스트

- [x] 같은 현재 구역의 중복 진입 이벤트 방지
- [x] 다른 구역을 거친 재방문 이벤트 발행
- [x] 구역 ID와 Scene 경로 바인딩 검증
- [x] 시작 홀 콘텐츠 자동 Additive 로드
- [x] 이동 실험실 콘텐츠 로드 후 시작 홀 콘텐츠 언로드
- [x] 능력 시험실 콘텐츠 로드 후 이동 실험실 콘텐츠 언로드
- [x] 전환 뒤 목표 Scene의 콘텐츠 루트 확인
- [ ] 실제 빌드에서 네 구역 장시간 왕복 테스트

## 수동 시연 방법

1. `Main` Scene을 Play한다.
2. Console에서 `구역 Scene 전환 완료: start_hall`을 확인한다.
3. 오른쪽 이동 실험실로 넘어가 배경색이 바뀌는지 확인한다.
4. Console에서 `구역 Scene 전환 완료: traversal_lab`을 확인한다.
5. 시작 홀과 백트래킹 샤프트를 왕복하며 전환 로그와 끊김을 확인한다.
6. Hierarchy에서 현재 구역 Scene 하나만 Main 옆에 로드되는지 확인한다.
7. 보스방 입장 시 붉은 전용 배경 Scene으로 전환되는지 확인한다.

## 다음 단계

- 실제 장식·적·상호작용 오브젝트를 Additive Scene으로 단계적으로 이전한다.
- 로딩 실패와 빌드 설정 누락을 플레이어에게 보이지 않는 복구 경로로 처리한다.
