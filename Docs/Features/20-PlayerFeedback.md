# 20. 이벤트 기반 플레이어 VFX·SFX

> `PlayerFeedbackController`가 대시·공격·명중·피격·능력 획득의 확정
> 이벤트를 구독해 게임 판정 코드와 VFX·SFX 실행을 분리했다.

## 목표

최종 캐릭터 모델과 음원이 없는 단계에서도 대시, 공격, 명중, 피격과 능력
획득을 즉시 구별할 수 있게 한다. 게임 판정을 표현 코드에서 다시 계산하지
않고, 이미 확정된 이벤트를 VFX와 SFX로 번역하는 구조를 포트폴리오에
기록한다.

## 범위

- 입력: `DashStarted`, `AttackStarted`, `HitConfirmed`, `Damaged`,
  `AbilityUnlocked` 이벤트
- 상태: 대시 Trail 표시 여부와 런타임 생성 AudioClip
- 출력: 방향성·방사형 파티클, 대시 Trail, 2D OneShot 효과음
- 연결 시스템: 이동, 전투, 체력, 능력 해금, Pause 마스터 음량

## 구현 후보 비교

| 방식 | 장점 | 단점 | 적합한 상황 |
|---|---|---|---|
| 판정 코드에서 VFX 직접 실행 | 구현이 빠름 | 모델·이펙트 교체가 판정 코드에 전파됨 | 폐기 예정인 짧은 실험 |
| 상태를 매 프레임 감시 | 기존 코드 수정이 적음 | 짧은 명중 같은 순간 이벤트를 놓치거나 중복 재생함 | 연속 상태 표시 |
| 확정 이벤트를 Presenter가 구독 | 판정과 표현의 수명·책임이 분리됨 | 이벤트 계약을 명시해야 함 | 교체 가능한 실제 게임 피드백 |

현재 구현은 세 번째 방식을 사용한다. `PlayerFeedbackController`는 이벤트를
받아 표현만 만들며 이동 속도, 데미지, 무적 시간과 능력 상태를 변경할 권한이
없다.

## 코드 구조

```text
SideScrollerMotor ── DashStarted ──────────────┐
PlayerCombat ─────── AttackStarted/HitConfirmed ├─ PlayerFeedbackController
Health ───────────── Damaged ──────────────────┤  ├─ TrailRenderer
PlayerAbilityState ─ AbilityUnlocked ──────────┘  ├─ ParticleSystem
                                                    └─ AudioSource
                                                        └─ PrototypeAudioSynth
```

- `SideScrollerMotor`는 대시가 실제 승인된 순간과 방향만 알린다.
- `PlayerCombat`은 공격 시작 콤보 단계와 `Health.TakeDamage`가 승인한
  타격 위치만 알린다.
- `PlayerFeedbackController`는 활성화 수명에 맞춰 이벤트를 한 번만
  구독하고, 비활성화될 때 해제한다.
- 하나의 `ParticleSystem`을 공유하고 `EmitParams`의 색·크기·수명을 이벤트마다
  바꾼다. 짧은 버스트마다 컴포넌트를 따로 두는 것보다 씬 직렬화와 관리 비용이
  작다.
- `PrototypeAudioSynth`는 사인파 스윕과 시드 기반 노이즈 버스트를 생성한다.
  이는 최종 음원이 아니라 타이밍·믹싱 구조를 검증하는 임시 자산이다.
- 모든 효과음은 플레이어의 2D `AudioSource.PlayOneShot`으로 재생되어 빠른
  연속 입력도 앞선 소리를 중단하지 않는다.

## 색과 Cue 규칙

| 이벤트 | VFX | 임시 SFX |
|---|---|---|
| 대시 | 청록 Trail과 반대 방향 파편 | 짧은 노이즈 |
| 공격 시작 | 진행 방향의 금색 파편 | 하강 톤 |
| 명중 확정 | 타격점의 주황 방사형 파편 | 강한 노이즈 |
| 플레이어 피격 | 몸 중심의 붉은 방사형 파편 | 낮아지는 톤 |
| 능력 획득 | 위로 상승하는 초록 파편 | 상승 톤 |

## 테스트 시나리오

- [x] 톤 스윕이 유효한 샘플레이트와 비영점 파형 생성
- [x] 생성 샘플의 NaN·무한대·클리핑 방지
- [x] 같은 노이즈 시드가 같은 파형 생성
- [x] Main 씬의 Presenter, AudioSource, Trail과 공유 파티클 참조
- [x] EditMode 137개와 PlayMode 4개 회귀 테스트
- [ ] 최종 모델 애니메이션과 실제 음원으로 교체 후 믹싱

## 수동 확인

1. `Main` 씬을 실행하고 `Left Shift`로 대시해 청록 Trail과 짧은 소리를
   확인한다.
2. `Enter`로 허공 공격 시 금색 파편만, 적 명중 시 주황 파편과 타격음이
   추가되는지 비교한다.
3. 적 공격을 받아 붉은 파편과 피격음이 발생하는지 확인한다.
4. 능력 구체를 처음 획득할 때 초록 상승 파편과 높은 확인음이 발생하는지
   확인한다.
5. Pause 메뉴의 음량 조절이 모든 임시 효과음에 적용되는지 확인한다.

## 한계와 다음 단계

- 현재 파티클과 코드 합성음은 타이밍 검증용이며 최종 품질의 아트·사운드가
  아니다.
- 최종 애니메이션의 Animation Event에 판정을 맡기지 않고, 현재 확정 이벤트와
  애니메이션 상태를 같은 타임라인 데이터에 연결할 예정이다.
- 다음 표현 패스에서는 히트 스톱, 화면 흔들림과 적 전용 피드백을 접근성 옵션
  및 카메라 연출과 함께 설계한다.
