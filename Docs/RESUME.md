# 작업 재개 지점

마지막 갱신: 2026-09-21

## 현재 방향

이 프로젝트는 기능 개수를 보여주는 수직 슬라이스에서, 플레이 가능한 2.5D
메트로배니아를 Reference Implementation으로 사용해 구현 방식을 비교·분석하는
연구형 포트폴리오로 전환했다.

중앙 기준 문서:

- `METROIDVANIA_STUDY.md`: 연구 질문, 비교 기준과 증거 수준
- `PROJECT_PLAN.md`: S0~S6 연구 로드맵
- `FEATURE_INDEX.md`: 연구 축과 23개 구현 사례 상태
- `FEATURE_TEMPLATE.md`: 기능 문서 공통 형식
- `ARCHITECTURE.md`: 코드 책임, 상태 소유권과 리팩터링 규칙

## 완료된 작업

| 범위 | 상태 | 마지막 커밋 |
|---|---|---|
| 중앙 문서와 README 전면 개편 | 완료 | `eba6837` |
| 01 이동 시스템 연구 문서 | 연구 완료 | `91d7c0c` |
| 02 전투 시스템 연구 문서 | 분석 완료, 촬영 대기 | `a8222d1` |
| 03 체크포인트 연구 문서 | 분석 완료, 촬영 대기 | `fcd4e30` |

함께 반영한 코드·테스트 개선:

- 대시 곡선을 `MovementMath.DashSpeedMultiplier`로 분리하고 경계값 테스트 추가
- 설정된 콤보 길이의 마지막 타격만 보너스를 받도록 수정
- 공중 공격 체공 요청이 최대 시간을 넘지 않도록 수정
- `CombatSystemTests`, `CheckpointSystemTests`로 기능별 테스트 책임 분리
- 전체 EditMode 테스트 `171/171` 통과

## 다음 작업

첫 작업은 `Docs/Features/04-Respawn.md` 개편이다.

1. `PlayerRespawnController`, `RespawnMath`, `DamageVolume`과 관련 테스트를 읽는다.
2. 다음 질문을 중심으로 현재 구현을 분석한다.
   - Scene 전체 Reload와 상태별 복구 중 현재 방식이 적합한 이유는 무엇인가?
   - 사망 시 이동·전투·체력·적·카메라를 어떤 순서로 복구해야 하는가?
   - 런타임 상태와 영구 진행 중 무엇을 유지해야 하는가?
3. 관련 테스트가 `MovementMathTests` 등에 섞여 있으면 `RespawnSystemTests`로
   분리한다.
4. 새 템플릿에 맞춰 대안, 적용 조건, 구조 전환 신호와 증거를 기록한다.
5. 테스트 후 `Docs/FEATURE_INDEX.md` 상태를 갱신한다.
6. `04` 관련 파일만 커밋하고 `origin/main`에 푸시한다.

이후 `05 → 23`을 같은 방식으로 한 문서씩 진행한다. 기존 번호와 미디어 파일명은
링크 안정성을 위해 유지한다.

## Git 상태와 주의할 파일

현재 브랜치와 원격 기준:

```text
branch: main
HEAD: fcd4e30
origin/main: fcd4e30
```

아래 파일은 이번 연구 문서 작업 전부터 존재한 촬영·복구 변경이다. 내용을
확인하기 전에는 수정, 삭제, Stage 또는 Commit하지 않는다.

```text
Assets/Materials/CaptureStudio_03.mat
Assets/Materials/CaptureStudio_04.mat
Assets/Materials/CaptureStudio_05.mat
Assets/Materials/CaptureStudio_06.mat
Assets/Materials/CaptureStudio_07.mat
Assets/Materials/CaptureStudio_08.mat
Assets/Materials/ZoneBackdrop_Backtrack.mat
Assets/Materials/ZoneBackdrop_Boss.mat
Assets/Materials/ZoneBackdrop_Start.mat
Assets/Materials/ZoneBackdrop_Traversal.mat
Assets/_Recovery.meta
Assets/_Recovery/
```

항상 관련 파일을 명시적으로 `git add`하고 `git add .`는 사용하지 않는다.

## 테스트 재개 명령

Unity 실행 파일:

```text
/Applications/Unity/Hub/Editor/6000.5.5f1-arm64/Unity.app/Contents/MacOS/Unity
```

전체 EditMode 테스트 예시:

```bash
/Applications/Unity/Hub/Editor/6000.5.5f1-arm64/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath /Users/rain/game_skill \
  -runTests -testPlatform EditMode \
  -testResults /tmp/game_skill_editmode_results.xml \
  -logFile /tmp/game_skill_editmode_tests.log
```

`-quit`를 함께 사용하면 현재 Test Framework에서 임포트 뒤 테스트 전에 종료될
수 있으므로 붙이지 않는다.

Unity 배치 실행은 때때로 `ProjectSettings/DynamicsManager.asset`과
`ProjectSettings/TimeManager.asset`을 자동 재직렬화한다. 테스트 후 `git status`와
`git diff`를 확인하고 연구 범위와 무관한 자동 변경은 커밋하지 않는다.

## 유지할 작업 규칙

- 새 C# 파일 상단에 한글 `GOLDEN STANDARD`를 작성한다.
- 함수·조건·반복문 주석은 문법이 아니라 의도와 경계를 한글로 설명한다.
- 잘 동작하는 코드는 비교 문서를 위해 무조건 교체하지 않는다.
- 실제 계약 오류나 테스트 불가능한 핵심 경계가 발견될 때만 작은 리팩터링을
  함께 진행한다.
- 기능 문서 하나를 완료할 때 관련 코드·테스트·문서만 커밋하고 푸시한다.
- 시각 자료가 없으면 `분석 완료`, 코드·대안·테스트·시각 증거가 모두 있으면
  `연구 완료`로 표시한다.
