# Sendagaya Shino — 애니풍 여성 캐릭터

- 제작: pixiv Inc. / VRoid Project
- 모델별 라이선스: **CC0 1.0**. [공식 안내](https://vroid.pixiv.help/hc/en-us/articles/360013482714-Sendagaya-Shino)
- 다운로드한 원본: [VRM 샘플 미러의 고정 버전](https://github.com/madjin/vrm-samples/blob/e16eb187100149a315ad92c3c9968f1d5baa6c7d/vroid/beta/Sendagaya_Shino.vrm)
- 원본 SHA-256: `1e177c1a7b14f783a9c48395831db8616260d3bddd4154cb2784b779adca49b5`
- 원본 VRM의 `title=Sendagaya Shino`, `licenseName=CC0` 확인.

## 변환과 적용

Blender 5.1.1의 기본 glTF importer와 FBX exporter로 변환했다.
`Tools/convert_shino.py`가 기본색 PNG와 Humanoid 본 대응표도 추출한다.
Unity VRM 플러그인을 설치하지 않고 Unity 기본 FBX·Humanoid를 사용한다.

```bash
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup \
  --python Tools/convert_shino.py -- /path/to/Sendagaya_Shino.glb \
  /Users/rain/game_skill/Assets/Art/ThirdParty/VRoid/SendagayaShino
```

다운로드한 `.vrm`은 glTF 바이너리이므로 변환 입력 파일명을 `.glb`로 지정한다.
Unity 메뉴 `Game Skill > Experiments > Apply Anime Character`로 ImportMap의 본·재질을 적용하고 실험 프리팹과 JumpLab 씬을 저장한다.

## 표현 범위

- JumpLab 시각물만 교체한다. 기존 Main·CaptureStudio는 비교 자료로 유지한다.
- 기존 Quaternius Humanoid 애니메이션을 재타기팅한다. 충돌 캡슐과 점프 수치는 유지한다.
- MToon 대신 URP Unlit·양면·알파 컷아웃으로 원본 텍스처 색과 윤곽을 표시한다.
- 원본의 Spring Bone, 표정 제어, 전용 MToon 그림자·외곽선은 이 변환에 포함하지 않는다.
- 머리카락·의상은 기본 스키닝으로 움직인다. 전용 물리와 클립별 의상 관통 보정은 별도 작업이다.
