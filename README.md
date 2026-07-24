# game_skill

Unity 기능을 작은 단위로 직접 구현하고, 재사용 가능한 예제와 학습 기록으로
정리하는 개인 프로젝트입니다.

## 개발 환경

- 엔진: Unity 6 LTS
- 언어: C#
- 대상 플랫폼: PC 우선
- 저장소: `oscar87657/game_skill`

> 현재 저장소는 Unity 프로젝트 뼈대만 포함합니다. Unity Hub에서 Unity 6 LTS
> 에디터를 설치한 뒤 이 폴더를 프로젝트로 열어 주세요.

## 목표

- 플레이어 조작부터 저장 시스템까지 핵심 게임 기능을 단계적으로 구현합니다.
- 기능마다 독립된 Scene, Prefab, Script 구조를 유지합니다.
- 완성된 기능에는 사용법, 구현 의도, 테스트 결과를 기록합니다.
- 마지막에는 구현한 기능을 하나의 작은 플레이 가능한 데모로 통합합니다.

## 진행 현황

- [x] Git 및 Unity 프로젝트 기본 구조
- [x] 개발 계획서
- [x] CC0 캐릭터 및 프로토타입 에셋
- [ ] 기본 Scene과 플레이어 이동
- [ ] 카메라 및 상호작용
- [ ] 체력, 데미지 및 전투
- [ ] 아이템 및 인벤토리
- [ ] 적 AI 및 상태 머신
- [ ] 세이브/로드
- [ ] 통합 데모와 빌드

자세한 일정과 완료 기준은 [프로젝트 계획서](Docs/PROJECT_PLAN.md)를 참고하세요.

## 프로젝트 열기

1. Unity Hub에서 Unity 6 LTS를 설치합니다.
2. `Add project from disk`를 선택합니다.
3. 이 저장소의 루트 폴더를 선택합니다.
4. Unity가 패키지와 `Library` 폴더 생성을 마칠 때까지 기다립니다.
5. `Assets/Scenes`에 첫 Scene을 만들고 실행합니다.

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

두 에셋은 CC0 라이선스이며 원본 라이선스는
[`Assets/Art/ThirdParty/Kenney`](Assets/Art/ThirdParty/Kenney) 아래에 보존합니다.
FBX와 PNG 파일은 Git LFS로 관리합니다.

커밋 메시지 예시:

```text
feat: 플레이어 이동 기능 추가
fix: 공중에서 점프가 중복 실행되는 문제 수정
refactor: 체력 계산을 공통 컴포넌트로 분리
docs: 인벤토리 사용법 추가
test: 데미지 계산 EditMode 테스트 추가
```
