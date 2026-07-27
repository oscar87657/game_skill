// GOLDEN STANDARD
// 목적: 가장 작은 플레이 가능한 2.5D Showcase 씬을 생성하고 마이그레이션한다.
// 책임: 플레이어·그레이박스·카메라·전투 더미와 에디터 메뉴를 생성한다.
// 불변식: 빌더를 다시 실행해도 자신이 만든 이름의 프로토타입 루트만 제거한다.
// 선택 이유: 씬 생성은 에디터 전용으로 두어 런타임 스크립트를 게임플레이에 집중시킨다.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace GameSkill.Editor
{
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string GrayboxRootName = "SideScrollerGraybox";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string GroundMaterialPath = "Assets/Materials/PrototypeGround.mat";
        private const string AccentMaterialPath = "Assets/Materials/PrototypeAccent.mat";

        [InitializeOnLoadMethod]
        private static void ScheduleSideScrollerMigration()
        {
            // Unity가 활성 씬과 어셈블리를 모두 로드한 뒤 마이그레이션을 지연 실행한다.
            EditorApplication.delayCall += TryMigrateOpenPrototype;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void TryMigrateOpenPrototype()
        {
            // Play Mode 진입 중에는 씬을 절대 변경하지 않는다.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                return;
            }

            if (GameObject.Find(GrayboxRootName) == null)
            {
                Build();
                return;
            }

            EnsureCombatPrototype();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            // 에디터가 다시 수정 가능한 상태가 되면 마이그레이션을 재시도한다.
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                TryMigrateOpenPrototype();
            }
        }

        [MenuItem("Game Skill/Build Side-Scroller Prototype")]
        public static void Build()
        {
            // 결정적인 크기와 에셋 경로로 표준 그레이박스를 다시 만든다.
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveExistingPrototypeRoots();

            Material groundMaterial = GetOrCreateMaterial(
                GroundMaterialPath,
                new Color(0.16f, 0.2f, 0.24f));
            Material accentMaterial = GetOrCreateMaterial(
                AccentMaterialPath,
                new Color(1f, 0.45f, 0.12f));

            GameObject player = CreatePlayer();
            CreateGraybox(groundMaterial, accentMaterial);
            ConfigureCamera(player);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log("2.5D side-scroller prototype created: Assets/Scenes/Main.unity");
        }

        private static GameObject CreatePlayer()
        {
            // 여기서는 게임플레이 컴포넌트만 조립하고 시각 자식은 애니메이션 빌더가 소유한다.
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.05f, 0f);

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.slopeLimit = 45f;
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.08f;
            controller.minMoveDistance = 0f;

            InputActionAsset inputActions =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            PlayerInput playerInput = player.AddComponent<PlayerInput>();
            playerInput.actions = inputActions;
            playerInput.defaultActionMap = "Player";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            player.AddComponent<SideScrollerMotor>();
            player.AddComponent<SideScrollerTargeting>();
            player.AddComponent<PlayerCombat>();
            CharacterAnimationBuilder.ConfigurePlayerVisual(player);

            return player;
        }

        private static void ConfigureCamera(GameObject player)
        {
            // 씬 참조와 태그를 유지하기 위해 Main Camera가 있으면 재사용한다.
            Camera camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            SideScrollerCamera sideScrollerCamera =
                camera.GetComponent<SideScrollerCamera>()
                ?? camera.gameObject.AddComponent<SideScrollerCamera>();
            sideScrollerCamera.Configure(player.transform);

            camera.orthographic = true;
            camera.orthographicSize = 5.2f;
            camera.transform.SetPositionAndRotation(
                new Vector3(1.35f, 2.4f, -9f),
                Quaternion.identity);
            EditorUtility.SetDirty(sideScrollerCamera);
            EditorUtility.SetDirty(camera);
        }

        private static void CreateGraybox(Material groundMaterial, Material accentMaterial)
        {
            // 계단·게이트·보상·전투 대상을 포함한 탐색 테스트 공간을 조합한다.
            var root = new GameObject(GrayboxRootName);

            CreateBlock(
                root.transform,
                "Ground",
                new Vector3(0f, -0.5f, 0f),
                new Vector3(30f, 1f, 3f),
                groundMaterial);
            CreateBlock(
                root.transform,
                "Step_A",
                new Vector3(4f, 0.5f, 0f),
                new Vector3(3f, 1f, 3f),
                groundMaterial);
            CreateBlock(
                root.transform,
                "Step_B",
                new Vector3(8f, 1.5f, 0f),
                new Vector3(3f, 3f, 3f),
                groundMaterial);
            CreateRamp(
                root.transform,
                "Slope_Test",
                new Vector3(-2.5f, 0.5f, 0f),
                new Vector3(4f, 1f, 3f),
                15f,
                groundMaterial);
            CreateBlock(
                root.transform,
                "High_Platform",
                new Vector3(13f, 3.2f, 0f),
                new Vector3(7f, 0.6f, 3f),
                groundMaterial);
            CreateBlock(
                root.transform,
                "Wall_Gate",
                new Vector3(-6f, 2f, 0f),
                new Vector3(0.6f, 4f, 3f),
                accentMaterial);
            CreateBlock(
                root.transform,
                "Backtrack_Reward",
                new Vector3(-9f, 4.5f, 0f),
                new Vector3(1f, 1f, 1f),
                accentMaterial);
            CreateTrainingDummy(root.transform, accentMaterial);
        }

        [MenuItem("Game Skill/Add Combat Prototype")]
        public static void AddCombatPrototype()
        {
            // 전체 씬을 재생성하지 않고 전투만 추가하는 공개 메뉴 명령이다.
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!EnsureCombatPrototype())
            {
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Basic attack and training dummy added to Main scene.");
        }

        private static bool EnsureCombatPrototype()
        {
            // 기존 씬을 멱등적으로 마이그레이션하고 변경 여부를 반환한다.
            bool changed = false;
            GameObject player = GameObject.Find("Player");
            if (player != null && player.GetComponent<PlayerCombat>() == null)
            {
                player.AddComponent<PlayerCombat>();
                changed = true;
            }

            if (player != null && player.GetComponent<SideScrollerTargeting>() == null)
            {
                // 이전 씬에도 자동 조준을 추가해 전체 재생성 없이 최신 전투 구성을 유지한다.
                player.AddComponent<SideScrollerTargeting>();
                changed = true;
            }

            if (GameObject.Find("TrainingDummy") == null)
            {
                GameObject root = GameObject.Find(GrayboxRootName);
                Material accentMaterial =
                    AssetDatabase.LoadAssetAtPath<Material>(AccentMaterialPath);
                if (root != null && accentMaterial != null)
                {
                    CreateTrainingDummy(root.transform, accentMaterial);
                    changed = true;
                }
            }

            if (changed)
            {
                Scene scene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
            }

            return changed;
        }

        private static GameObject CreateTrainingDummy(
            Transform parent,
            Material material)
        {
            // 공격 시연을 위한 보이고 충돌하는 Health 대상을 만든다.
            GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummy.name = "TrainingDummy";
            dummy.transform.SetParent(parent);
            dummy.transform.position = new Vector3(3.25f, 1f, 0f);
            dummy.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
            dummy.GetComponent<MeshRenderer>().sharedMaterial = material;

            Health health = dummy.AddComponent<Health>();
            health.Configure(3);
            dummy.AddComponent<TrainingDummy>();
            return dummy;
        }

        private static GameObject CreateBlock(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            // 그레이박스 생성 규칙을 한곳에 모아 모든 플랫폼의 콜라이더·재질을 통일한다.
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<MeshRenderer>().sharedMaterial = material;
            return block;
        }

        private static GameObject CreateRamp(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            float zRotation,
            Material material)
        {
            // 회전한 큐브로 경사면을 단순화하여 CharacterController 경계값을 빠르게 검증한다.
            GameObject ramp = CreateBlock(parent, name, position, scale, material);
            ramp.transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
            return ramp;
        }

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void RemoveExistingPrototypeRoots()
        {
            Object.DestroyImmediate(GameObject.Find("Player"));
            Object.DestroyImmediate(GameObject.Find("Graybox"));
            Object.DestroyImmediate(GameObject.Find(GrayboxRootName));
        }
    }
}
