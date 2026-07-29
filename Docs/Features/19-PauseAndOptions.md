# 19. 일시정지와 옵션 메뉴

> `PauseMenuController`가 Pause 상태와 시간 배율·입력·AudioMixer 제어를
> 함께 소유해 재개·저장·불러오기·마스터 음량을 한 흐름으로 관리한다.

## 목표

플레이 중 `Esc` 또는 게임패드 Start로 게임을 안전하게 멈추고 같은 입력이나
`RESUME` 버튼으로 원래 상태를 복원한다. 메뉴에서는 기존 진행 저장 기능과
마스터 음량 옵션을 함께 제공한다.

## 동작

- `Esc` / 게임패드 Start: Pause 메뉴 열기·닫기
- `RESUME`: 게임 재개
- `SAVE` / `LOAD`: 기존 `GameProgressHud` 저장 API 재사용
- `-` / `+`: 마스터 음량을 10% 단위로 조절
- 메뉴 진입 시 `Time.timeScale = 0`과 `AudioListener.pause = true`
- 메뉴 종료·Scene 전환·컴포넌트 비활성화 시 진입 전 시간·오디오 상태 복원
- 변경된 음량은 `PlayerPrefs`에 저장해 다음 실행에서 복원

## 선택한 구조

```text
PlayerInput/Pause ─┐
Pause UI Buttons ──┼─ PauseMenuController
                   ├─ Time.timeScale / AudioListener
                   ├─ GameProgressHud ─ GameProgressSaveController
                   └─ PlayerPrefs(master volume)
```

이동·전투·적 AI마다 `IsPaused` 조건을 추가하지 않았다. Unity의 시간 배율을
한 경계에서 제어하면 `deltaTime`을 사용하는 게임플레이 시스템 전체가 같은
프레임에 멈춘다. Pause 입력과 UI는 시간 배율과 무관하게 동작하는 Input
System 이벤트를 사용한다.

`PauseMenuController`는 일시정지 전의 시간 배율과 오디오 정지 상태를
기억한다. 따라서 강제로 비활성화되더라도 무조건 `1`과 `false`로 덮어쓰지
않고 자신이 변경하기 전 값을 돌려놓는다.

## 구현 후보 비교

| 방식 | 장점 | 단점 | 적합한 상황 |
|---|---|---|---|
| 각 시스템에 Pause 분기 | 선택적으로 멈추기 쉬움 | 이동·전투·AI마다 중복 코드가 생김 | 일부 시스템만 계속 움직여야 하는 게임 |
| `Time.timeScale`만 변경 | 구현이 작고 대부분 자동 정지 | 오디오와 unscaled 로직은 별도 처리 필요 | 일반적인 싱글 플레이 Pause |
| 별도 게임 상태 머신 | 전환 규칙과 중첩 메뉴 확장이 쉬움 | 현재 범위에는 구조 비용이 큼 | 타이틀·인벤토리·컷신 상태가 많은 게임 |

현재 수직 슬라이스는 `Time.timeScale`과 `AudioListener.pause`를 함께
제어하고, 이후 메뉴 종류가 늘어날 때 상태 머신으로 확장할 수 있게 Pause
수명 주기를 한 컴포넌트에 모았다.

## 테스트

- [x] Pause 진입 시 시간과 오디오 정지
- [x] 재개 시 진입 전 시간 배율과 오디오 상태 복원
- [x] 메뉴 오버레이 활성·비활성 전환
- [x] 마스터 음량 0~100% 제한과 문구 갱신
- [x] Pause Input Action과 Main 씬 참조 구성
- [x] 비활성 메뉴를 빌더가 중복 생성하지 않음
- [ ] 실제 게임패드 Start와 UI 탐색 확인 — 장비 확보 후 진행

## 수동 확인

1. `Main` 씬을 실행하고 이동 중 `Esc`를 누른다.
2. 캐릭터, 적, 탄환과 카메라가 모두 멈추고 중앙 메뉴가 나타나는지 확인한다.
3. `-`, `+` 버튼으로 음량 문구가 10% 단위로 변하는지 확인한다.
4. 메뉴 안의 `SAVE`, `LOAD`가 각각 `SAVED`, `LOADED` 또는 실패 문구를
   표시하는지 확인한다.
5. `RESUME` 또는 `Esc`를 눌러 플레이가 즉시 이어지는지 확인한다.

## 다음 단계

- 최종 오디오가 추가되면 UI 효과음만 Pause 중 재생하도록 분리한다.
- 해상도·화면 모드·언어 옵션이 필요해질 때 별도 옵션 데이터 DTO로 확장한다.
- 게임패드가 준비되면 자동 탐색 순서와 Start·Submit·Cancel 입력을 실제로
  조정한다.
