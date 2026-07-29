# 포트폴리오 미디어

기능별 시연 GIF와 스크린샷을 이 폴더에 저장한다. 파일 번호는
[기능 구현 인덱스](../Docs/FEATURE_INDEX.md)의 실제 구현 순서와 일치시킨다.

```text
Media/
├── GIF/
├── Screenshots/
└── Diagrams/
```

권장 GIF 길이는 5~10초이며 파일명은 `NN-기능-상황.gif` 형식을 사용한다.

```text
01-movement-dash-curve.gif
02-combat-air-combo.gif
13-melee-state-machine.gif
17-progress-save-round-trip.gif
```

각 GIF는 단순한 플레이 장면이 아니라 문서의 핵심 코드 설명을 증명해야 한다.
예를 들어 대시 GIF에는 시작·가속·감속 곡선과 무적 통과가 함께 보여야 하며,
설명에는 `MovementMath`와 `SideScrollerMotor`의 역할을 연결한다.
