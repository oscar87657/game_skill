# 실험 코드 경계

## 두 환경

- `Assets/Scripts/Runtime`과 `Main`: 기존 통합 구현. 회귀 비교 기준으로 보존한다.
- `Assets/Experiments`: 질문별 실행 후보. 별도 `GameSkill.Experiments` Assembly로 기존 런타임과 의존성을 분리한다.

실험은 기존 플레이어 Controller에 옵션을 계속 붙이는 방식으로 만들지 않는다.
입력·카메라·지형·계측처럼 동일해야 하는 조건은 공유하고, 비교할 알고리즘은 별도 파일에 둔다.
후보를 쉽게 읽기 위한 작은 중복은 허용한다. 공통 인터페이스·팩토리·범용 실험 프레임워크를 미리 만들지 않는다.

## J01 책임

| 코드 | 책임 |
|---|---|
| `GravityJump` | 초기 속도 이후 일정 중력의 이동량 적분 |
| `ReleaseCutJump` | 버튼을 놓은 상승 중 속도 제한 후 중력 적분 |
| `TimedCurveJump` | 경과 시간에 따른 정규화 높이 차이 |
| `JumpExperiment` | 동일 입력·충돌 실행·초기화·계측·애니메이션·화면 조작 |
| `JumpLabBuilder` | 기존 파일을 덮어쓰지 않는 씬 생성과 독립 실험 빌드 |

Root Motion은 사용하지 않는다. J01은 코드 이동량과 상태/시간 기반 애니메이션 연결을 비교한다.
Root Motion과 충돌 실행 방식의 조합은 별도 실험 질문이다.

## 변경 규칙

- 기존 촬영 Material과 `_Recovery` 파일은 건드리지 않는다.
- 실험 빌드는 Scene 목록을 명시적으로 전달한다. Main용 빌드 목록은 유지한다.
- 캐릭터는 CC0 VRoid 모델, 클립은 기존 Quaternius 에셋을 사용한다. 새 FBX의 Humanoid 매핑은 `AnimeCharacterBuilder`가 준비하며 기존 Quaternius Importer는 변경하지 않는다.
- C# 상단에 한글 목적·책임·경계를 쓰고 핵심 분기에는 의도를 기록한다.
- 계산 계약은 EditMode, 충돌·입력 이후 실행은 PlayMode로 검증한다.
- 자동 검증으로 확인하지 않은 손맛·장르 효과는 완료로 표시하지 않는다.
- 외부 코드 도입 전 출처·라이선스를 확인한다. 현재 후보는 자체 작성한 교육용 구현이다.
