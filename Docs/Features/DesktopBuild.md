# macOS 데스크톱 빌드

## 목표

수직 슬라이스를 Unity Build Settings의 수동 클릭에 의존하지 않고 같은 Scene,
아키텍처와 Development 옵션으로 반복 생성한다. 생성 성공뿐 아니라 실제
Player가 시작 Scene과 첫 구역을 오류 없이 불러오는지까지 확인한다.

## 빌드 계약

| 항목 | 설정 |
|---|---|
| Unity | `6000.5.5f1` |
| 대상 | `StandaloneOSX` |
| 요청 아키텍처 | ARM64 |
| 옵션 | Development, Allow Debugging, Detailed Report |
| 앱 식별자 | `com.oscar87657.gameskill` |
| 버전 | `0.1.0` |
| 기본 출력 | `Builds/macOS/game_skill.app` |

`Main`은 반드시 활성 Scene 목록의 첫 번째여야 한다. 이후 네 구역 Scene은
Additive 스트리밍 대상으로 빌드에 함께 포함한다. 이 계약이 깨지면
`DesktopBuildPipeline`은 불완전한 Player를 생성하지 않고 예외로 종료한다.

## 빌드 Scene 순서

```text
0. Assets/Scenes/Main.unity
1. Assets/Scenes/Zones/Zone_BacktrackShaft.unity
2. Assets/Scenes/Zones/Zone_StartHall.unity
3. Assets/Scenes/Zones/Zone_TraversalLab.unity
4. Assets/Scenes/Zones/Zone_BossRoom.unity
```

## 실행 방법

Unity 메뉴에서는 다음 항목을 선택한다.

```text
Game Skill
└── Build
    └── macOS Development Build
```

명령행 자동화는 같은 내부 함수를 호출한다.

```bash
GAME_SKILL_MACOS_BUILD_PATH=/absolute/path/game_skill.app \
/Applications/Unity/Hub/Editor/6000.5.5f1-arm64/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -nographics \
  -projectPath /absolute/path/game_skill \
  -executeMethod GameSkill.Editor.DesktopBuildPipeline.BuildMacOsDevelopmentFromCommandLine \
  -quit
```

환경 변수는 출력 위치만 바꾼다. Scene과 Development 옵션을 외부 입력으로
노출하지 않아 로컬 메뉴 빌드와 자동화 빌드가 달라지는 것을 막는다.

## 2026-07-29 검증 결과

| 항목 | 결과 |
|---|---|
| BuildReport | `Succeeded`, 경고 0개 |
| 빌드 시간 | `48.53초` |
| 보고된 크기 | `302,767,189 bytes` |
| 디스크 크기 | 약 `289 MB` |
| 실행 파일 | Mach-O `x86_64 + arm64` 슬라이스 확인 |
| Info.plist | 식별자 `com.oscar87657.gameskill`, 버전 `0.1.0` |
| 실행 확인 | 1280×720 창 모드로 12초 유지 |
| 시작 Scene | `Main` 로드와 `start_hall` 구역 스트리밍 성공 |
| Player 로그 | 예외·오류 없음 |

ARM64를 요청한 현재 Unity 빌드의 앱 번들 실행 파일과 Unity 네이티브
라이브러리에는 Intel과 Apple Silicon 슬라이스가 모두 포함됐다. 실제 산출물
검사는 `file` 명령으로 수행했다.

## Player 성능 스모크

Development Player에서 자동 Probe가 완료된 결과는 다음과 같다.

- 최근 600프레임: 평균 `0.98 ms`, p95 `3.63 ms`, 최대 `4.22 ms`
- 메인 스레드 GC: 평균·p95 모두 `0 B/frame`
- 할당 메모리 최고점: `86.8 MB`
- 드로우 콜: 현재 Player 세션에서 `unsupported`
- 종합 예산: `PASS`

이는 시작 구역의 짧은 스모크 기준선이다. 보스 탄막과 전체 플레이의 최소
사양 성능을 대신하지 않으며, 드로우 콜은 Unity Profiler를 연결한 그래픽
Player 세션에서 다시 확인한다.

## 배포 전 남은 작업

- 현재 앱은 로컬 실행을 위한 ad-hoc 서명이며 Apple Developer ID 서명과
  notarization을 하지 않았다.
- GitHub Release에 올릴 때 앱 번들을 ZIP으로 보존하고 SHA-256을 기록한다.
- Windows 배포본은 Windows Build Support 모듈을 설치한 뒤 별도 빌드와
  실행 검증이 필요하다.
- 공개 전 최종 비 Development 빌드를 만들어 진단 심볼과 개발 로그 비용을
  제외한다.
