# game_skill — 메트로배니아 개발 실험실

목표는 **새로운 플레이 요구와 애니메이션을 받았을 때 구현 방식을 선택하고 수정할 수 있는 개발력**이다.
기능 개수나 문서 분량 대신 조사 → 서로 다른 구현 → 통제된 비교 → 애니메이션 결합 → 조건 변경 → 탐험 구간 적용으로 학습한다.

> Unity 임포트·EditMode 174개·JumpLab PlayMode·macOS 빌드와 앱 시작 화면 검증을 마쳤다. 비교 플레이와 학습 결론은 다음 단계다. 상세 상태는 [재개 지점](Docs/RESUME.md)을 따른다.

## 지금 시작하기

1. Unity Hub에서 이 폴더를 Unity **6000.5.5f1 / macOS ARM64**로 연다.
2. `Assets/Experiments/JumpLab/JumpLab.unity`를 열고 Play한다.
3. `A/D` 또는 방향키로 이동, `Space`로 점프한다. 게임패드는 왼쪽 스틱과 South 버튼이다.
4. `1/2/3` 또는 화면 버튼으로 고정 탄도 / 버튼 해제 제어 / 시간 곡선을 바꾼다. 전환 시 같은 출발점으로 초기화된다.
5. `R`로 재시작, `F`로 속도 기반 / 고정 시간 기반 공중 애니메이션 전환을 비교한다.
6. 화면의 높이·체공 시간·착지 거리와 실제 발판 결과를 [J01 실험 노트](Docs/Experiments/J01-Jump.md)에 기록한다.

씬이 없으면 `Game Skill > Experiments > Create Jump Lab`을 실행한다. 이미 있는 씬은 덮어쓰지 않고 연다.
실험용 macOS 빌드는 `Game Skill > Experiments > Build Jump Lab macOS`를 사용한다.
기존 `Game Skill > Build` 메뉴는 Main 프로토타입용이다.

## 현재 상태

- 준비된 실험: **J01 점프 제어** — 세 실행 후보, 공통 충돌·입력·지형, 기존 CC0 Humanoid 애니메이션, 계측 표시.
- 자료 조사: 1차 출처 목록과 읽기 질문 준비. 후보들은 직접 작성한 교육용 코드이며 상용 게임 내부 구현의 재현이라고 주장하지 않는다.
- 아직 하지 않은 일: 사용자 비교 플레이, 다른 애니메이션 세트 실험, 능력 획득 전후 탐험 구간, 적용 조건의 최종 결론.
- 이전 프로토타입과 23개 기능 문서는 비교 자료로 보존한다. 과거의 “연구 완료”는 새 실험 완료가 아니다.

## 읽는 순서

- [학습 계획](Docs/PROJECT_PLAN.md): 진행 순서와 완료 조건
- [실험 방법](Docs/METROIDVANIA_STUDY.md): 조사·변수 통제·근거 구분
- [실험 인덱스](Docs/FEATURE_INDEX.md): 현재 실험과 다음 질문
- [출처 노트](Docs/SOURCES.md): 원문 링크, 읽은 범위, 실험으로 확인할 내용
- [재개 지점](Docs/RESUME.md): 바로 다음 행동과 검증 상태
- [코드 경계](Docs/ARCHITECTURE.md): 실험과 기존 구현의 분리
- [장르 질문](Docs/GAME_DESIGN.md): 이동 실험을 탐험과 연결하는 기준
- [기록 템플릿](Docs/FEATURE_TEMPLATE.md)
- [이전 구현 인덱스](Docs/REFERENCE_INDEX.md)

## Unity 구성

| 위치 | 용도 |
|---|---|
| `Assets/Experiments/JumpLab` | 독립된 점프 후보와 비교 씬. 실험마다 필요한 최소 코드를 작성한다. |
| `Assets/Scenes/Main.unity` | 기존 이동·전투·능력·월드·저장이 연결된 비교 기준 |
| `Assets/Scenes/CaptureStudio.unity` | 기존 기능 촬영 환경 |
| `Assets/Scripts/Runtime` | 기존 Reference Implementation |
| `Assets/Tests` | 기존 회귀 테스트와 실험 계약 테스트 |
| `Docs/Experiments` | 가설·실행 방법·관찰·조건부 결론 |

URP, Input System과 Unity Test Framework를 유지한다. 비교 변수와 무관한 엔진 업그레이드는 하지 않는다.
기존 Kenney·Quaternius 에셋의 라이선스는 `Assets/Art/ThirdParty`에 보존한다.
