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
            EditorApplication.delayCall += TryMigrateOpenPrototype;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void TryMigrateOpenPrototype()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            if (SceneManager.GetActiveScene().path == ScenePath
                && GameObject.Find(GrayboxRootName) == null)
            {
                Build();
            }
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                TryMigrateOpenPrototype();
            }
        }

        [MenuItem("Game Skill/Build Side-Scroller Prototype")]
        public static void Build()
        {
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
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.05f, 0f);

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.35f;
            controller.stepOffset = 0.3f;

            InputActionAsset inputActions =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            PlayerInput playerInput = player.AddComponent<PlayerInput>();
            playerInput.actions = inputActions;
            playerInput.defaultActionMap = "Player";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            player.AddComponent<SideScrollerMotor>();
            CharacterAnimationBuilder.ConfigurePlayerVisual(player);

            return player;
        }

        private static void ConfigureCamera(GameObject player)
        {
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
        }

        private static GameObject CreateBlock(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<MeshRenderer>().sharedMaterial = material;
            return block;
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
