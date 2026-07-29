// GOLDEN STANDARD
// 목적: 본편 Main 씬을 변경하지 않고 기능별 포트폴리오 영상을 반복 촬영할 전용 스튜디오 씬을 만든다.
// 책임: Main 복제·불필요 시스템 비활성화·넓은 구역 생성·기존 전투 대상 재배치·촬영 단축키 연결을 수행한다.
// 불변식: Main.unity는 읽기 원본으로만 사용하며 생성 결과는 CaptureStudio.unity와 전용 재질에만 저장한다.
// 선택 이유: 본편 동선과 촬영 동선을 분리하면 실제 게임 밸런스를 보존하면서도 기능을 독립적으로 보여줄 수 있다.
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameSkill.Editor
{
    public static class CaptureStudioBuilder
    {
        private const string MainScenePath =
            "Assets/Scenes/Main.unity";
        private const string CaptureScenePath =
            "Assets/Scenes/CaptureStudio.unity";
        private const string MaterialFolder =
            "Assets/Materials";
        private const float StageSpacing = 46f;
        private const float StageWidth = 34f;

        private static readonly string[] StageNames =
        {
            "01_MOVEMENT",
            "02_CHECKPOINT",
            "03_COMBO_LAB",
            "04_MELEE_ENEMY",
            "05_RANGED_ENEMY",
            "06_CHARGE_ENEMY",
            "07_WALL_TRAVERSAL",
            "08_BOSS_ARENA"
        };

        private static readonly Color[] StageColors =
        {
            new(0.10f, 0.20f, 0.32f),
            new(0.10f, 0.30f, 0.23f),
            new(0.34f, 0.22f, 0.08f),
            new(0.34f, 0.10f, 0.12f),
            new(0.20f, 0.11f, 0.36f),
            new(0.08f, 0.30f, 0.32f),
            new(0.10f, 0.22f, 0.42f),
            new(0.38f, 0.07f, 0.14f)
        };

        [MenuItem("Game Skill/Capture/Rebuild Capture Studio")]
        public static void RebuildCaptureStudio()
        {
            // 재생 중 씬 파일을 교체하면 런타임 상태가 저장될 수 있으므로 편집 모드에서만 생성한다.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning(
                    "Play 모드를 종료한 뒤 Capture Studio를 다시 생성하세요.");
                return;
            }

            BuildCaptureStudio();
        }

        public static void BuildCaptureStudio()
        {
            // 디스크에 저장된 Main을 복제해 사용자가 편집 중인 본편 오브젝트를 직접 변경하지 않는다.
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.Refresh();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    CaptureScenePath) != null)
            {
                AssetDatabase.DeleteAsset(CaptureScenePath);
            }

            if (!AssetDatabase.CopyAsset(
                    MainScenePath,
                    CaptureScenePath))
            {
                throw new InvalidOperationException(
                    "Main 씬을 CaptureStudio 씬으로 복제하지 못했습니다.");
            }

            AssetDatabase.Refresh();
            Scene scene = EditorSceneManager.OpenScene(
                CaptureScenePath,
                OpenSceneMode.Single);
            ConfigureCopiedScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            ValidateCaptureStudio(scene);
            Debug.Log(
                "CaptureStudio 생성 완료: 1~8 구역 이동 / 0 초기화");
        }

        public static void BuildCaptureStudioFromCommandLine()
        {
            // 자동 검증 환경에서도 메뉴와 같은 생성 경로를 사용해 결과 차이를 막는다.
            BuildCaptureStudio();
        }

        [MenuItem("Game Skill/Capture/Validate Capture Studio")]
        public static void ValidateCaptureStudioMenu()
        {
            // 현재 파일을 다시 열어 저장된 결과만으로 촬영 조건을 만족하는지 확인한다.
            Scene scene = EditorSceneManager.OpenScene(
                CaptureScenePath,
                OpenSceneMode.Single);
            ValidateCaptureStudio(scene);
            Debug.Log("CaptureStudio 검증 완료");
        }

        private static void ConfigureCopiedScene(Scene scene)
        {
            // 촬영 대상은 기존 로직과 설정을 그대로 쓰고 공간 배치만 새 루트로 분리한다.
            GameObject sourceGraybox =
                FindSceneObject(scene, "SideScrollerGraybox");
            GameObject player =
                RequireSceneObject(scene, "Player");
            GameObject cameraObject =
                RequireSceneObject(scene, "Main Camera");

            DisableSceneObject(scene, "WorldZoneStreaming");
            DisableSceneObject(scene, "WorldMapHUD");
            DisableSceneObject(scene, "PerformanceDiagnostics");

            var studioRoot =
                new GameObject("CaptureStudioLayout");
            var stagesRoot =
                new GameObject("CaptureStudio_Stages");
            var actorsRoot =
                new GameObject("CaptureStudio_Actors");
            var systemRoot =
                new GameObject("CaptureStudio_System");
            stagesRoot.transform.SetParent(studioRoot.transform);
            actorsRoot.transform.SetParent(studioRoot.transform);
            systemRoot.transform.SetParent(studioRoot.transform);

            Material groundMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Materials/PrototypeGround.mat");
            if (groundMaterial == null)
            {
                throw new InvalidOperationException(
                    "PrototypeGround 재질을 찾지 못했습니다.");
            }

            List<Transform> anchors =
                CreateStages(stagesRoot.transform, groundMaterial);
            RepositionActors(scene, actorsRoot.transform);
            if (sourceGraybox != null)
            {
                sourceGraybox.SetActive(false);
            }

            player.transform.position =
                anchors[0].position;
            SideScrollerMotor motor =
                player.GetComponent<SideScrollerMotor>();
            PlayerAbilityState abilityState =
                player.GetComponent<PlayerAbilityState>();
            if (motor == null || abilityState == null)
            {
                throw new InvalidOperationException(
                    "Player의 이동 또는 능력 컴포넌트를 찾지 못했습니다.");
            }

            AbilityDefinition[] abilities =
            {
                LoadAbility("Ability_DoubleJump"),
                LoadAbility("Ability_AirDash"),
                LoadAbility("Ability_WallTraversal")
            };
            abilityState.ConfigureInitialAbilities(abilities);

            CaptureStudioController controller =
                systemRoot.AddComponent<CaptureStudioController>();
            controller.Configure(
                motor,
                abilityState,
                abilities,
                anchors);

            SideScrollerCamera sideCamera =
                cameraObject.GetComponent<SideScrollerCamera>();
            if (sideCamera == null)
            {
                throw new InvalidOperationException(
                    "Main Camera의 추적 컴포넌트를 찾지 못했습니다.");
            }

            sideCamera.Configure(player.transform);
            sideCamera.ConfigureWorldBounds(
                null,
                null,
                Array.Empty<CameraZoneBounds>());
            EditorUtility.SetDirty(sideCamera);
            EditorUtility.SetDirty(abilityState);
            EditorUtility.SetDirty(controller);
        }

        private static List<Transform> CreateStages(
            Transform parent,
            Material groundMaterial)
        {
            var anchors = new List<Transform>();

            // 같은 너비의 무대를 일정 간격으로 배치해 카메라 안에 다른 기능 오브젝트가 섞이지 않게 한다.
            for (int index = 0;
                 index < StageNames.Length;
                 index++)
            {
                float centerX = index * StageSpacing;
                Material stageMaterial =
                    GetOrCreateStageMaterial(
                        index,
                        StageColors[index]);
                Transform stage =
                    CreateStage(
                        parent,
                        StageNames[index],
                        centerX,
                        groundMaterial,
                        stageMaterial);
                var anchor =
                    new GameObject(
                        $"CaptureAnchor_{index + 1:00}");
                anchor.transform.SetParent(stage);
                anchor.transform.position =
                    new Vector3(
                        centerX - 12f,
                        0.05f,
                        0f);
                anchors.Add(anchor.transform);

                // 구역 사이도 같은 높이의 다리로 이어 촬영자가 순간이동 없이 전체를 달릴 수 있게 한다.
                if (index < StageNames.Length - 1)
                {
                    CreateBlock(
                        parent,
                        $"Connector_{index + 1:00}",
                        new Vector3(
                            centerX + StageSpacing * 0.5f,
                            -0.5f,
                            0f),
                        new Vector3(
                            StageSpacing - StageWidth,
                            1f,
                            3f),
                        groundMaterial,
                        true);
                }
            }

            return anchors;
        }

        private static Transform CreateStage(
            Transform parent,
            string stageName,
            float centerX,
            Material groundMaterial,
            Material stageMaterial)
        {
            // 바닥·배경·상단 표식을 하나의 자식 루트로 묶어 Hierarchy에서도 구역이 바로 구분되게 한다.
            var root = new GameObject(stageName);
            root.transform.SetParent(parent);
            CreateBlock(
                root.transform,
                $"{stageName}_Floor",
                new Vector3(centerX, -0.5f, 0f),
                new Vector3(StageWidth, 1f, 3f),
                groundMaterial,
                true);
            CreateBlock(
                root.transform,
                $"{stageName}_Backdrop",
                new Vector3(centerX, 4.25f, 2.4f),
                new Vector3(StageWidth, 9.5f, 0.2f),
                stageMaterial,
                false);
            CreateBlock(
                root.transform,
                $"{stageName}_Header",
                new Vector3(centerX, 5.7f, 2.1f),
                new Vector3(StageWidth - 2f, 0.18f, 0.18f),
                stageMaterial,
                false);
            CreateStageLabel(
                root.transform,
                stageName,
                centerX);
            return root.transform;
        }

        private static void CreateStageLabel(
            Transform parent,
            string stageName,
            float centerX)
        {
            // 기능 이름을 화면 상단에 작게 표시해 긴 스튜디오에서도 현재 촬영 구역을 즉시 식별한다.
            var labelObject =
                new GameObject($"{stageName}_Label");
            labelObject.transform.SetParent(parent);
            labelObject.transform.position =
                new Vector3(centerX, 5.15f, 2.05f);
            TextMesh label =
                labelObject.AddComponent<TextMesh>();
            label.text =
                stageName.Replace('_', ' ');
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.075f;
            label.fontSize = 64;
            label.color = Color.white;
        }

        private static void RepositionActors(
            Scene scene,
            Transform parent)
        {
            // 각 전투 대상을 46m씩 떨어진 독립 무대에 놓아 감지 범위와 이펙트가 겹치지 않게 한다.
            MoveActor(
                scene,
                "Checkpoint_Start",
                parent,
                new Vector3(46f, 1f, 0f));
            MoveActor(
                scene,
                "RespawnHazard",
                parent,
                new Vector3(58f, 0.15f, 0f));
            MoveActor(
                scene,
                "TrainingDummy",
                parent,
                new Vector3(86f, 1f, 0f));
            MoveActor(
                scene,
                "MeleeEnemy_Grunt",
                parent,
                new Vector3(132f, 0.05f, 0f));
            MoveActor(
                scene,
                "RangedEnemy_Sentry",
                parent,
                new Vector3(178f, 0.05f, 0f));
            MoveActor(
                scene,
                "ChargeEnemy_Rusher",
                parent,
                new Vector3(224f, 0.05f, 0f));
            MoveActor(
                scene,
                "Boss_AbilityWarden",
                parent,
                new Vector3(332f, 0.05f, 0f));

            Material wallMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Materials/PrototypeAccent.mat");
            CreateBlock(
                parent,
                "CaptureWall_Left",
                new Vector3(272f, 3.5f, 0f),
                new Vector3(0.7f, 7f, 3f),
                wallMaterial,
                true);
            CreateBlock(
                parent,
                "CaptureWall_Right",
                new Vector3(280f, 3.5f, 0f),
                new Vector3(0.7f, 7f, 3f),
                wallMaterial,
                true);
            CreateBlock(
                parent,
                "CaptureBossWall_Left",
                new Vector3(304.5f, 4f, 0f),
                new Vector3(0.7f, 8f, 3f),
                wallMaterial,
                true);
            CreateBlock(
                parent,
                "CaptureBossWall_Right",
                new Vector3(339.5f, 4f, 0f),
                new Vector3(0.7f, 8f, 3f),
                wallMaterial,
                true);
        }

        private static void MoveActor(
            Scene scene,
            string objectName,
            Transform parent,
            Vector3 position)
        {
            // 기존 컴포넌트와 자식 참조를 보존하기 위해 복제 대신 루트만 촬영 구역으로 옮긴다.
            GameObject actor =
                RequireSceneObject(scene, objectName);
            actor.transform.SetParent(parent, true);
            actor.transform.position = position;
            actor.SetActive(true);
            EditorUtility.SetDirty(actor);
        }

        private static GameObject CreateBlock(
            Transform parent,
            string objectName,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool keepCollider)
        {
            // Unity 기본 Cube를 사용해 외부 에셋 없이도 크기와 충돌을 예측 가능한 단위로 만든다.
            GameObject block =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube);
            block.name = objectName;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;
            MeshRenderer renderer =
                block.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            if (!keepCollider)
            {
                UnityEngine.Object.DestroyImmediate(
                    block.GetComponent<BoxCollider>());
            }

            return block;
        }

        private static Material GetOrCreateStageMaterial(
            int index,
            Color color)
        {
            string path =
                $"{MaterialFolder}/CaptureStudio_{index + 1:00}.mat";
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                // 프로젝트 렌더 파이프라인에 맞는 Lit 셰이더를 우선 사용하고 없을 때 기본 셰이더로 대체한다.
                Shader shader =
                    Shader.Find(
                        "Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static AbilityDefinition LoadAbility(
            string assetName)
        {
            string path =
                $"Assets/Settings/Abilities/{assetName}.asset";
            AbilityDefinition ability =
                AssetDatabase.LoadAssetAtPath<AbilityDefinition>(
                    path);
            if (ability == null)
            {
                throw new InvalidOperationException(
                    $"촬영 능력 에셋을 찾지 못했습니다: {path}");
            }

            return ability;
        }

        private static void DisableSceneObject(
            Scene scene,
            string objectName)
        {
            // 촬영 화면을 가리는 HUD와 본편 전용 스트리밍 시스템은 복제 씬에서만 비활성화한다.
            GameObject target =
                FindSceneObject(scene, objectName);
            if (target != null)
            {
                target.SetActive(false);
                EditorUtility.SetDirty(target);
            }
        }

        private static GameObject RequireSceneObject(
            Scene scene,
            string objectName)
        {
            // 필수 원본이 빠진 상태로 불완전한 촬영 씬을 저장하지 않도록 즉시 실패시킨다.
            GameObject target =
                FindSceneObject(scene, objectName);
            if (target == null)
            {
                throw new InvalidOperationException(
                    $"Main 씬 오브젝트를 찾지 못했습니다: {objectName}");
            }

            return target;
        }

        private static GameObject FindSceneObject(
            Scene scene,
            string objectName)
        {
            // 비활성 자식까지 순회해 본편 진행 상태와 무관하게 이름으로 원본을 찾는다.
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms =
                    root.GetComponentsInChildren<Transform>(true);
                // 각 루트의 모든 자식을 검사해 같은 씬 안의 정확한 이름만 반환한다.
                foreach (Transform candidate in transforms)
                {
                    if (candidate.name == objectName)
                    {
                        return candidate.gameObject;
                    }
                }
            }

            return null;
        }

        private static void ValidateCaptureStudio(Scene scene)
        {
            // 저장 결과가 촬영 편의성의 핵심 조건을 모두 만족하는지 생성 직후 한 번 검증한다.
            CaptureStudioController controller =
                FindSceneObject(
                        scene,
                        "CaptureStudio_System")
                    ?.GetComponent<CaptureStudioController>();
            GameObject originalGraybox =
                FindSceneObject(
                    scene,
                    "SideScrollerGraybox");
            GameObject movementStage =
                FindSceneObject(
                    scene,
                    "01_MOVEMENT");
            GameObject bossStage =
                FindSceneObject(
                    scene,
                    "08_BOSS_ARENA");

            if (controller == null
                || !controller.IsConfigured
                || controller.ZoneCount != 8
                || originalGraybox == null
                || originalGraybox.activeSelf
                || movementStage == null
                || bossStage == null)
            {
                throw new InvalidOperationException(
                    "CaptureStudio 검증에 실패했습니다.");
            }

            float separation =
                bossStage.transform
                    .Find("CaptureAnchor_08")
                    .position.x
                - movementStage.transform
                    .Find("CaptureAnchor_01")
                    .position.x;
            if (separation < StageSpacing * 7f - 0.01f)
            {
                throw new InvalidOperationException(
                    "촬영 구역 간격이 설정값보다 좁습니다.");
            }
        }
    }
}
