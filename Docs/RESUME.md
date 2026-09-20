# 작업 재개 지점

마지막 갱신: 2026-09-21

## 현재 목표

기존 기능을 차례로 설명하던 작업을 중단하고, 외부 조사와 서로 다른 실행 후보를 비교하는 개발 실험실로 전환했다.
다음 문서는 `04-Respawn`이 아니다. **J01 점프 실험의 원문 조사와 사용자 비교 플레이**부터 시작한다.

## 반영한 준비

- README·학습 계획·실험 방법·템플릿·장르 질문·코드 경계·미디어 지침 개편.
- 이전 23개 분석은 `REFERENCE_INDEX.md`와 `Docs/Features`에 보존하고 과거 자료로 표시.
- `Assets/Experiments/JumpLab/JumpLab.unity`: 공통 평지·천장·발판, 기존 CC0 캐릭터와 Animator.
- 세 후보: 일정 중력 / 해제 상승 제한 / 시간-높이 곡선.
- 같은 클립의 수직 속도 기반 / 고정 시간 기반 전환 비교.
- 후보 선택·초기화·높이·체공 시간·입력 유지 시간·착지 거리 계측.
- 독립 실험 Assembly, 씬 생성/열기 메뉴, Main과 별도의 macOS 빌드 메뉴.
- EditMode 계산 계약과 PlayMode 실제 씬·천장·초기화 검사 추가.
- 출처 노트와 J01 실행·관찰 기록지 준비.

## 검증 결과

로그인 후 Unity 라이선스 문제가 해소됐다. 현재 실행 차단은 없다.

- Unity `6000.5.5f1`에서 임포트·컴파일·JumpLab 씬 열기 성공.
- 전체 EditMode **174/174 통과**: `/tmp/game_skill_lab_editmode.xml`.
- JumpLab PlayMode **1/1 통과**: `/tmp/game_skill_lab_playmode.xml`.
- 실제 천장 테스트의 시작 조건을 접지 완료 후 점프하도록 수정했다. 세 후보의 착지·짧은 점프·천장·초기화·Animator 연결 검증 완료.
- macOS Development Build 성공: `Builds/JumpLab/JumpLab.app`.
- 독립 앱 시작 화면의 캐릭터·지형·비교 UI·접지 표시 확인. 시작 로그에 예외 없음.
- 캡처: `Media/Screenshots/J01-lab-ready.png`.
- 기존 정적 컴파일·계산 실행·씬 참조·문서 링크 검사도 통과.
- Unity가 자동 저장한 ProjectSettings 변경은 검토 후 이번 작업 전 상태로 복원했다.

로그: `/tmp/game_skill_jumplab_retry.log`, `/tmp/game_skill_lab_editmode.log`, `/tmp/game_skill_lab_playmode.log`, `/tmp/game_skill_lab_build.log`, `/tmp/game_skill_lab_player.log`.

## 다음 행동

1. `Assets/Experiments/JumpLab/JumpLab.unity` 또는 빌드된 앱을 연다.
2. `Docs/Experiments/J01-Jump.md` 순서대로 탭·홀드·천장·발판을 직접 비교한다.
3. 출처 원문과 공개 Player.cs를 읽고 자신의 예측·실제 관찰을 기록한다.
4. F로 애니메이션 연결 방식을 바꿔 같은 클립이 어긋나는 조건을 찾는다.
5. 비교 뒤 다른 클립·입력 보정·능력 획득 전후 작은 탐험 구간을 순서대로 실험한다.

자동 테스트 통과를 손맛·학습 완료로 취급하지 않는다. 실제 게임패드와 다른 클립 조합도 아직 검증하지 않았다.

메뉴 `Create Jump Lab`은 기존 실험 씬을 덮어쓰지 않는다. 현재 씬을 Additive로 열고 Play 시작 씬을 JumpLab으로 지정한다.
기존 Main을 다시 실행하려면 `Game Skill > Experiments > Use Current Scene for Play`를 선택한다.

명령행 테스트 (Unity Editor를 닫은 상태):

```bash
/Applications/Unity/Hub/Editor/6000.5.5f1-arm64/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath /Users/rain/game_skill \
  -runTests -testPlatform EditMode \
  -testResults /tmp/game_skill_lab_editmode.xml -logFile /tmp/game_skill_lab_editmode.log
```

PlayMode는 `-testPlatform PlayMode -testFilter GameSkill.Tests.JumpLabPlayModeTests`로 실행하고 결과 경로도 구분한다.
테스트에는 `-quit`를 붙이지 않는다. 화면 검증은 일반 Editor에서 따로 수행한다.

## 보존할 변경

작업 시작 전부터 있던 `Assets/Materials/CaptureStudio_03~08.mat`, `ZoneBackdrop_*.mat`, `Assets/_Recovery`는 이번 변경과 무관하며 건드리지 않았다.
Main·CaptureStudio·기존 빌드 씬 목록·원본 에셋 Importer도 유지했다.
이번 개편의 커밋 범위는 실험 코드·씬·테스트·문서·시작 화면 캡처다.
촬영 재질·복구 파일과 Unity 실행 뒤 생긴 `Assets/Settings` 변경은 커밋에서 제외한다.
