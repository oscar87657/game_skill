# 실험 인덱스

진도는 기존 기능 번호가 아니라 질문·비교 실행·조건 변경으로 관리한다.
이전 23개 문서와 당시 상태는 [이전 구현 인덱스](REFERENCE_INDEX.md)에 보존한다.

| 실험 | 현재 상태 | 질문 | 다음 행동 |
|---|---|---|---|
| [J01 점프 제어](Experiments/J01-Jump.md) | 실행 준비 완료 | 같은 발판에서 높이·시간·해제 제어가 어떤 차이를 만드는가? | 원문 읽기와 탭·홀드 플레이 기록 |
| J02 이동과 충돌 | 계획 | 경사·이동 발판·넉백에서 어떤 실행 방식이 적합한가? | J01 뒤 비교 요구 선정 |
| C01 공격과 애니메이션 | 계획 | 판정·전진·취소 시점을 누가 소유해야 하는가? | 다른 길이의 공격 클립과 실패 조건 조사 |
| W01 능력과 재방문 | 계획 | 획득 전후 같은 공간의 의미가 어떻게 바뀌는가? | J01 결과를 사용할 작은 루프 설계 |
| R01 실패 복구 | 계획 | 사망 시 어떤 상태를 유지해야 탐험이 일관되는가? | 재로드와 선택 복구의 비교 조건 선정 |

계획 상태의 항목은 아직 구현·검증한 것으로 취급하지 않는다.

## 기존 코드에서 찾아볼 자료

- J01/J02: [이동 분석](Features/01-Movement.md), `SideScrollerMotor`, `MovementMath`
- C01: [전투 분석](Features/02-Combat.md), `PlayerCombat`, `PlayerAnimator`
- W01: [능력](Features/05-AbilitiesAndGates.md), [구역](Features/06-WorldZones.md), [지름길](Features/07-WorldShortcuts.md)
- R01: [체크포인트](Features/03-Checkpoint.md), [부활](Features/04-Respawn.md), [저장](Features/17-ProgressSave.md)

기존 문서는 과거 구현의 기록이다. 실험 결과가 나오기 전에 결론을 그대로 승계하지 않는다.
