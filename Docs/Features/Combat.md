# 전투 시스템

## 포트폴리오 목표

공격, 이동, 체력, 피격을 서로 결합하지 않고 교체할 수 있는 계약으로
설계한다. 특히 공중 공격과 대시가 수평 이동을 차단하지 않는지 검증한다.

## 현재 구현

- `PlayerCombat`: 입력·공격 타이밍·OverlapBox 판정
- `Health`: 데미지·사망 이벤트
- `TrainingDummy`: 피격 반응과 자동 부활
- 공중 공격의 짧은 수직 속도 완화
- 공격 중에도 수평 이동과 대시 허용
- 3단 기본 공격과 공격 중 다음 입력 버퍼
- 1·2타 기본 데미지, 3타 마무리 보너스
- `ComboStep` 기반 Punch Jab → Punch Cross → Sword Attack 애니메이션

## 구현 방식 비교

| 판정 방식 | 장점 | 단점 |
|---|---|---|
| `Physics.OverlapBox` | 타이밍 제어가 쉽고 테스트하기 좋음 | 애니메이션과 수동 동기화 필요 |
| Trigger Collider | 시각적 범위 확인이 쉬움 | 활성화·중복 피격 관리 필요 |
| Animation Event | 동작과 판정 타이밍이 직관적 | 애니메이션 에셋 의존성이 커짐 |

현재 프로토타입은 `OverlapBox`를 사용하고, 콤보 사이클이 확정되면 공격 데이터를
ScriptableObject로 분리한다.

## 다음 개선

- 공격별 ScriptableObject 데이터 분리
- Hitbox/Hurtbox 계약 분리
- 히트 스톱·피격 방향·경직
- 공격별 데이터와 시연용 디버그 UI
