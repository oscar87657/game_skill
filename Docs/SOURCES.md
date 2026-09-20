# 출처 노트

확인일: 2026-09-21. 현재는 J01을 시작하기 위한 1차 조사다. 전체 장르 조사 완료를 뜻하지 않는다.
외부 코드를 복사하지 않았으며 점프 후보는 자체 작성했다. 공개 코드 재사용 시 해당 버전의 라이선스를 별도로 확인한다.

| 원문 | 확인한 범위와 핵심 | 실험에서 확인할 질문 | 상태 |
|---|---|---|---|
| [Maddy Thorson — Celeste & TowerFall Physics](https://www.mattmakesgames.com/articles/celeste_and_towerfall_physics/index.html) | Actors가 이동량을 전달하고 축별 충돌을 처리하는 구조. 이동 발판의 밀기·태우기 설명 | 점프의 이동량 생성과 충돌 실행을 분리하면 무엇이 비교 가능해지는가? | 글 본문 확인. Unity 코드로 이식한 것은 아님 |
| [NoelFB/Celeste — Player 설명](https://github.com/NoelFB/Celeste/blob/master/Source/Player/Readme.md) · [Player.cs](https://github.com/NoelFB/Celeste/blob/master/Source/Player/Player.cs) | 개발자가 공개한 플레이어 구현 자료 | 점프 유지·취소와 상태 전환을 실제 코드에서 추적하고 우리 후보와 차이를 적기 | 위치 확인. Player.cs 전체 추적·커밋 고정은 다음 작업 |
| [Unity 6 — CharacterController.Move](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/CharacterController.Move.html) | 이동량을 전달하고 충돌 결과를 받는다. 중력은 자동 적용하지 않음 | 같은 충돌 실행 위에서 이동량 생성만 바꿀 수 있는가? | API 설명 확인. J01 공통 실행에 사용 |
| [Unity 6 — Root Motion](https://docs.unity3d.com/6000.0/Documentation/Manual/RootMotion.html) | 애니메이션의 루트 변환과 실제 오브젝트 이동의 관계 | 애니메이션 이동량과 충돌 실행은 어떻게 조합되는가? | 개념 확인. J01에서는 applyRootMotion=false |

## 원문을 읽으며 남길 것

1. 저자가 해결하려던 플레이 문제와 엔진·콘텐츠 전제.
2. 입력에서 상태 변화와 이동·판정까지의 실제 함수 흐름.
3. 우리 코드와 동일한 부분, 다른 부분, 아직 모르는 부분.
4. 가져올 원리와 직접 비교할 실패 조건.
5. 원문 URL, 저장소라면 커밋, 코드 도입 시 라이선스와 변경 범위.

## 다음 조사

- Celeste Player.cs에서 점프 유지·속도 변경과 상태 전환을 추적한다. Celeste는 정밀 이동 참고 사례이며 메트로배니아 월드 구조의 증거로 사용하지 않는다.
- 성격이 다른 점프·공격 사례의 개발자 원문을 찾고 현재 후보 구분이 충분한지 다시 판단한다.
- 실제 클립의 root 위치, 길이, 접지 프레임을 확인한다. 파일 이름만으로 적합성을 판단하지 않는다.
- 장르 공간 연구에서는 개발자 인터뷰와 직접 플레이 동선을 함께 기록한다. 화면만으로 내부 코드를 추정하지 않는다.
