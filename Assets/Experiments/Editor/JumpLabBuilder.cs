// GOLDEN STANDARD
// 목적: 비교 실험을 바로 실행할 독립 JumpLab 씬을 생성한다.
// 책임: 공통 지형·카메라·기존 시각 모델 복제·독립 빌드를 준비한다.
// 불변식: 기존 Main·Importer·Build Settings와 이미 생성한 실험 씬을 덮어쓰지 않는다.
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameSkill.Experiments.Editor
{
    public static class JumpLabBuilder
    {
        public const string ScenePath = "Assets/Experiments/JumpLab/JumpLab.unity";

        [MenuItem("Game Skill/Experiments/Use Current Scene for Play")]
        public static void UseCurrentSceneForPlay()
        {
            // 실험에서 지정한 시작 씬을 해제해 Main 등 현재 열린 씬으로 돌아간다.
            EditorSceneManager.playModeStartScene = null;
        }

        [MenuItem("Game Skill/Experiments/Create Jump Lab")]
        public static void Create()
        {
            // 기존 열린 씬의 미저장 변경은 닫지 않고 새 씬을 Additive로 연다.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play 모드를 종료한 뒤 실험 씬을 여세요.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Scene existing = SceneManager.GetSceneByPath(ScenePath);
                if (!existing.isLoaded) existing = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                SceneManager.SetActiveScene(existing);
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            Material ground = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PrototypeGround.mat");
            Platform("Flat trial floor", new Vector3(0f, -0.5f, 0f), new Vector3(30f, 1f, 5f), ground);
            Platform("Low ceiling", new Vector3(-7f, 3.2f, 0f), new Vector3(5f, 0.4f, 4f), ground);
            Platform("Platform 1 - top 0.8m", new Vector3(5f, 0.4f, 0f), new Vector3(2f, 0.8f, 3f), ground);
            Platform("Platform 2 - top 2m", new Vector3(8f, 1f, 0f), new Vector3(2f, 2f, 3f), ground);
            Platform("Platform 3 - top 3.5m", new Vector3(11f, 1.75f, 0f), new Vector3(2f, 3.5f, 3f), ground);

            var cameraObject = new GameObject("JumpLab Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 3.5f, -24f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 9f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.065f, 0.1f);
            var lightObject = new GameObject("JumpLab Light", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -25f, 0f);

            var player = new GameObject("JumpLab Player");
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            CharacterController body = player.AddComponent<CharacterController>();
            body.height = 1.7f;
            body.radius = 0.3f;
            body.center = Vector3.up * 0.85f;
            body.skinWidth = 0.02f;
            body.stepOffset = 0.15f;
            body.minMoveDistance = 0f;
            JumpExperiment experiment = player.AddComponent<JumpExperiment>();
            experiment.ConfigureVisual(CopyReferenceVisual(player.transform));

            Physics.SyncTransforms();
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("JumpLab 씬을 저장하지 못했습니다.");
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Selection.activeGameObject = player;
            Debug.Log("JumpLab 준비 완료: 1/2/3 후보, Space 점프, R 초기화, F 애니메이션 전환");
        }

        private static void Platform(string name, Vector3 position, Vector3 scale, Material material)
        {
            // 모든 후보가 동일한 Collider를 사용하도록 씬에 지형을 한 벌만 둔다.
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetPositionAndRotation(position, Quaternion.identity);
            cube.transform.localScale = scale;
            if (material != null) cube.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Animator CopyReferenceVisual(Transform parent)
        {
            // 준비된 실험 캐릭터를 우선 사용해 씬 재생성 후에도 선택한 시각물을 유지한다.
            GameObject experimentModel = AssetDatabase.LoadAssetAtPath<GameObject>(AnimeCharacterBuilder.PrefabPath);
            if (experimentModel != null)
                return ((GameObject)PrefabUtility.InstantiatePrefab(experimentModel, parent)).GetComponent<Animator>();
            // 읽기 전용 Preview Scene에서 이미 보정된 CC0 시각물만 복제해 기존 빌더의 재임포트를 피한다.
            Scene reference = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Main.unity");
            try
            {
                Transform source = reference.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .First(item => item.name == "Player").Find("CharacterModel");
                if (source == null) throw new InvalidOperationException("Main의 CharacterModel이 없습니다.");
                GameObject visual = UnityEngine.Object.Instantiate(source.gameObject, parent);
                visual.name = "Reference Character Visual";
                Animator animator = visual.GetComponentInChildren<Animator>();
                if (animator == null || animator.runtimeAnimatorController == null)
                    throw new InvalidOperationException("비교용 Animator가 없습니다.");
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                return animator;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(reference);
            }
        }

        [MenuItem("Game Skill/Experiments/Build Jump Lab macOS")]
        public static void BuildMacOS()
        {
            // 명시적 씬 목록으로 기존 Main용 Build Settings를 바꾸지 않는다.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("JumpLab을 먼저 생성하세요.");
            System.IO.Directory.CreateDirectory("Builds/JumpLab");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/JumpLab/JumpLab.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"JumpLab 빌드 실패: {report.summary.result}");
            Debug.Log($"JumpLab 빌드 완료: {report.summary.outputPath}");
        }
    }
}
