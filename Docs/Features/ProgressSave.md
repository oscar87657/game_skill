# 플레이어 진행 저장

## 목표

M6의 진행 상태를 버전이 있는 JSON 데이터로 왕복한다. 획득 능력, 마지막
체크포인트, 방문 구역, 열린 지름길과 수집 보상을 저장한다. 저장 데이터에는
Unity 오브젝트 참조나 Scene 경로를 넣지 않고 영구 ID와 재시작 좌표만 기록한다.

## 저장 형식

```json
{
  "version": 1,
  "unlockedAbilityIds": [
    "double_jump",
    "air_dash"
  ],
  "hasCheckpoint": true,
  "checkpointId": "start_hall",
  "respawnX": 2.0,
  "respawnY": 1.25,
  "respawnZ": 0.0,
  "visitedZoneIds": [
    "start_hall",
    "traversal_lab"
  ],
  "unlockedShortcutIds": [
    "shortcut_shaft_return"
  ],
  "collectedRewardIds": [
    "reward_shaft_health_fragment"
  ]
}
```

`version`이 현재 지원 버전과 다르거나 JSON이 손상되면 기존 런타임 상태를
변경하지 않는다. 저장된 능력 ID도 현재 빌드의 `AbilityDefinition`
카탈로그에 있는 항목만 복원한다. 월드 상태 복원 완료 이벤트는 지도, 지름길
게이트·활성 장치와 보상 픽업을 한 번에 갱신한다. 수집 보상이 추가되거나
제거된 저장 데이터를 적용하면 최대 체력 효과도 함께 보정한다.

## 선택한 구조

```text
PlayerAbilityState ─┐
PlayerCheckpointState ─┼─ GameProgressSaveCodec ─ JSON
PlayerWorldState ──────┘              ↑
                                     │
                         GameProgressSaveController
```

- `GameProgressSaveData`: Unity 참조가 없는 버전형 DTO
- `GameProgressSaveCodec`: 캡처·직렬화·검증·적용 순수 경계
- `GameProgressSaveController`: 파일 입출력과 능력·구역 카탈로그
- `PlayerAbilityState`: 정렬된 능력 ID 복사와 알려진 정의 기반 복원
- `PlayerCheckpointState`: 접촉 보상 없이 체크포인트 상태만 복원
- `PlayerWorldState`: 방문·지름길·보상 ID 복사와 전체 복원 이벤트
- `WorldShortcutGate`·`ShortcutUnlockVolume`: 열린 통로와 활성 장치 표현 복원
- `BacktrackRewardPickup`: 수집 표현과 최대 체력 효과의 결정적 재구성

자동 저장과 자동 불러오기는 아직 사용하지 않는다. 개발 중 Play를 누르는
것만으로 세이브 파일이 덮어써지는 일을 막고, 이후 저장 UI나 체크포인트가
`SaveNow`와 `LoadNow`를 명시적으로 호출하도록 경계를 먼저 만든다.

## 구현 후보 비교

| 방식 | 장점 | 단점 | 적합한 상황 |
|---|---|---|---|
| `PlayerPrefs`에 개별 키 저장 | 매우 빠른 구현 | 구조 변경·슬롯·진단이 어려움 | 소수 옵션 값 |
| ScriptableObject에 런타임 상태 저장 | 에디터에서 보기 쉬움 | 에셋과 사용자 상태가 섞임 | 제작용 설정 |
| 버전형 DTO + JSON Codec | 테스트·마이그레이션·저장소 교체가 쉬움 | 스키마 관리가 필요함 | 확장되는 진행 저장 |

## 테스트

- [x] 능력 ID와 체크포인트 좌표 JSON 왕복
- [x] 현재 빌드에서 제거된 능력 ID 무시
- [x] 손상 JSON 거부
- [x] 지원하지 않는 버전 거부
- [x] Main 플레이어의 저장 제어기와 버전 필드 구성
- [x] 방문 구역·열린 지름길·수집 보상 JSON 왕복
- [x] 전체 복원 직후 지도·게이트·활성 장치 표현 갱신
- [x] 수집 보상 추가·제거에 따른 최대 체력 `5 ↔ 6` 재구성
- [x] 현재 빌드에서 제거된 구역 ID 무시
- [ ] 실제 UI에서 저장·불러오기 호출
- [ ] 버전 1 이후 마이그레이션 테스트

## 다음 단계

- 보스 처치 ID를 같은 DTO에 추가한다.
- 체크포인트 또는 메뉴 UI에서 저장·불러오기 성공과 실패를 표시한다.
- 실제 버전 변경이 생기면 이전 버전을 현재 버전으로 바꾸는 마이그레이션을
  추가한다.
