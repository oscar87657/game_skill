# game_skill

Unity 기능을 작은 단위로 직접 구현하면서 완성하는 3D 메트로배니아 개인
프로젝트입니다.

## 개발 환경

- 엔진: Unity 6.5 (`6000.5.5f1`)
- 에디터 아키텍처: macOS ARM64 권장
- 렌더 파이프라인: URP 예정
- 언어: C#
- 대상 플랫폼: PC 우선
- 저장소: `oscar87657/game_skill`

> Unity Hub에서 Unity `6000.5.5f1`을 설치한 뒤 이 폴더를 프로젝트로 열어
> 주세요. Apple Silicon Mac에서는 macOS ARM64 에디터를 사용합니다.

## 목표

- 서로 연결된 3D 공간을 탐험하고 새로운 이동 능력으로 기존 지역을 다시
  탐색하는 메트로배니아 구조를 구현합니다.
- 이동, 전투, 능력 해금, 게이트, 체크포인트를 독립된 시스템으로 구성합니다.
- 첫 공개 목표는 20~30분 분량의 플레이 가능한 수직 슬라이스입니다.

## 진행 현황

- [x] Git 및 Unity 프로젝트 기본 구조
- [x] 개발 계획서
- [x] 3D 메트로배니아 장르와 수직 슬라이스 범위 확정
- [x] CC0 캐릭터 및 프로토타입 에셋
- [x] URP Main Scene과 플레이어 이동 프로토타입
- [x] 3인칭 카메라 프로토타입
- [x] Kenney 캐릭터 및 이동·점프 애니메이션
- [ ] 상호작용 및 체크포인트
- [ ] 체력, 데미지 및 전투
- [ ] 이동 능력 해금 및 능력 게이트
- [ ] 연결된 월드와 백트래킹
- [ ] 적 AI 및 상태 머신
- [ ] 세이브/로드
- [ ] 보스와 수직 슬라이스 빌드

자세한 내용은 [게임 디자인](Docs/GAME_DESIGN.md)과
[프로젝트 계획서](Docs/PROJECT_PLAN.md)를 참고하세요.

## 프로젝트 열기

1. Unity Hub에서 Unity `6000.5.5f1` macOS ARM64를 설치합니다.
2. `Add project from disk`를 선택합니다.
3. 이 저장소의 루트 폴더를 선택합니다.
4. Unity가 패키지와 `Library` 폴더 생성을 마칠 때까지 기다립니다.
5. `Assets/Scenes`에 첫 Scene을 만들고 실행합니다.

## 현재 조작

- 이동: `WASD` 또는 왼쪽 스틱
- 시점: 마우스 또는 오른쪽 스틱
- 점프: `Space` 또는 게임패드 South 버튼
- 달리기: `Left Shift` 또는 왼쪽 스틱 버튼
- 마우스 잠금 해제: `Escape`
- 마우스 다시 잠금: Game View 왼쪽 클릭

현재 플레이어는 Kenney `character-oobi` 모델을 사용합니다. 이동 속도에 따라
Idle, Walk, Sprint가 블렌딩되며 점프 중에는 Jump와 Fall 상태가 재생됩니다.

## 폴더 구조

```text
Assets/
├── Art/
│   └── ThirdParty/
├── Audio/
├── Materials/
├── Prefabs/
├── Scenes/
├── Scripts/
│   ├── Editor/
│   └── Runtime/
└── Tests/
    ├── EditMode/
    └── PlayMode/
Docs/
Packages/
ProjectSettings/
```

## 작업 규칙

- 기능 작업은 `feature/<기능명>` 브랜치에서 진행합니다.
- 한 커밋에는 하나의 논리적인 변경만 담습니다.
- `Library`, `Temp`, `Logs`, 빌드 결과물은 커밋하지 않습니다.
- 큰 바이너리 에셋을 추가하기 전에 Git LFS를 설치하고 추적 규칙을 정합니다.

## 포함된 외부 에셋

- Kenney Blocky Characters 2.0: 애니메이션 캐릭터 18종
- Kenney Prototype Kit 1.0: 테스트 레벨용 모델 145종
- Kenney Platformer Kit 4.1: 플레이어 캐릭터와 이동·점프 애니메이션

세 에셋은 CC0 라이선스이며 원본 라이선스는
[`Assets/Art/ThirdParty/Kenney`](Assets/Art/ThirdParty/Kenney) 아래에 보존합니다.
FBX와 PNG 파일은 Git LFS로 관리합니다.

커밋 메시지 예시:

```text
feat: 플레이어 이동 기능 추가
fix: 공중에서 점프가 중복 실행되는 문제 수정
refactor: 체력 계산을 공통 컴포넌트로 분리
docs: 능력 게이트 설계 설명 추가
test: 데미지 계산 EditMode 테스트 추가
```
