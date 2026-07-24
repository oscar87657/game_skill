using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameSkill.Editor
{
    public static class CharacterAnimationBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string CharacterModelPath =
            "Assets/Art/ThirdParty/Kenney/PlatformerCharacter/Models/character-oobi.fbx";
        private const string CharacterTexturePath =
            "Assets/Art/ThirdParty/Kenney/PlatformerCharacter/Models/Textures/colormap.png";
        private const string CharacterMaterialPath =
            "Assets/Materials/PlatformerCharacter.mat";
        private const string AnimatorControllerPath =
            "Assets/Animations/Player.controller";

        [InitializeOnLoadMethod]
        private static void BuildMissingCharacterSetup()
        {
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    AnimatorControllerPath) != null)
            {
                return;
            }

            EditorApplication.delayCall += TryBuildMissingCharacterSetup;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void TryBuildMissingCharacterSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            Build();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                TryBuildMissingCharacterSetup();
            }
        }

        [MenuItem("Game Skill/Build Character Animation")]
        public static void Build()
        {
            ConfigureModelImporter();
            Material material = GetOrCreateCharacterMaterial();
            AnimatorController controller = CreateAnimatorController();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                throw new MissingReferenceException(
                    "Player가 없습니다. 먼저 Game Skill > Build Prototype Scene을 실행하세요.");
            }

            ConfigurePlayerVisual(player, material, controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = player;
            Debug.Log(
                "Kenney character and Idle/Walk/Sprint/Jump/Fall animations configured.");
        }

        public static void ConfigurePlayerVisual(GameObject player)
        {
            ConfigureModelImporter();
            ConfigurePlayerVisual(
                player,
                GetOrCreateCharacterMaterial(),
                CreateAnimatorController());
        }

        private static void ConfigurePlayerVisual(
            GameObject player,
            Material material,
            RuntimeAnimatorController controller)
        {
            Transform existingVisual = player.transform.Find("Visual");
            if (existingVisual != null)
            {
                UnityEngine.Object.DestroyImmediate(existingVisual.gameObject);
            }

            Transform existingModel = player.transform.Find("CharacterModel");
            if (existingModel != null)
            {
                UnityEngine.Object.DestroyImmediate(existingModel.gameObject);
            }

            GameObject modelPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CharacterModelPath);
            if (modelPrefab == null)
            {
                throw new MissingReferenceException(
                    $"Character model not found: {CharacterModelPath}");
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, player.transform);
            model.name = "CharacterModel";
            model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = new Material[renderer.sharedMaterials.Length];
                for (int index = 0; index < materials.Length; index++)
                {
                    materials[index] = material;
                }

                renderer.sharedMaterials = materials;
            }

            NormalizeModelHeight(model.transform, player.transform.position.y, 1.7f);

            Animator animator = model.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            PlayerAnimator playerAnimator =
                player.GetComponent<PlayerAnimator>() ?? player.AddComponent<PlayerAnimator>();
            playerAnimator.Configure(animator);
            EditorUtility.SetDirty(playerAnimator);
        }

        private static void ConfigureModelImporter()
        {
            var importer = AssetImporter.GetAtPath(CharacterModelPath) as ModelImporter;
            if (importer == null)
            {
                throw new MissingReferenceException(
                    $"ModelImporter not found: {CharacterModelPath}");
            }

            bool needsReimport =
                importer.animationType != ModelImporterAnimationType.Generic
                || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0
                ? importer.clipAnimations
                : importer.defaultClipAnimations;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                bool shouldLoop =
                    clip.name.Equals("idle", StringComparison.OrdinalIgnoreCase)
                    || clip.name.Equals("walk", StringComparison.OrdinalIgnoreCase)
                    || clip.name.Equals("sprint", StringComparison.OrdinalIgnoreCase)
                    || clip.name.Equals("fall", StringComparison.OrdinalIgnoreCase);
                if (clip.loopTime != shouldLoop)
                {
                    clip.loopTime = shouldLoop;
                    needsReimport = true;
                }
            }

            if (needsReimport || importer.clipAnimations.Length == 0)
            {
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        private static AnimatorController CreateAnimatorController()
        {
            AnimatorController existingController =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (existingController != null)
            {
                return existingController;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            {
                AssetDatabase.CreateFolder("Assets", "Animations");
            }

            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);

            Dictionary<string, AnimationClip> clips =
                AssetDatabase.LoadAllAssetsAtPath(CharacterModelPath)
                    .OfType<AnimationClip>()
                    .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                    .ToDictionary(clip => clip.name, StringComparer.OrdinalIgnoreCase);

            AnimationClip idle = RequireClip(clips, "idle");
            AnimationClip walk = RequireClip(clips, "walk");
            AnimationClip sprint = RequireClip(clips, "sprint");
            AnimationClip jump = RequireClip(clips, "jump");
            AnimationClip fall = RequireClip(clips, "fall");

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            var locomotionTree = new BlendTree
            {
                name = "Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(locomotionTree, controller);
            locomotionTree.AddChild(idle, 0f);
            locomotionTree.AddChild(walk, 0.5f);
            locomotionTree.AddChild(sprint, 1f);

            AnimatorState locomotion = stateMachine.AddState("Locomotion");
            locomotion.motion = locomotionTree;
            AnimatorState jumpState = stateMachine.AddState("Jump");
            jumpState.motion = jump;
            AnimatorState fallState = stateMachine.AddState("Fall");
            fallState.motion = fall;
            stateMachine.defaultState = locomotion;

            AddTransition(
                locomotion,
                jumpState,
                new TransitionCondition(AnimatorConditionMode.IfNot, 0f, "Grounded"),
                new TransitionCondition(AnimatorConditionMode.Greater, 0.1f, "VerticalSpeed"));
            AddTransition(
                locomotion,
                fallState,
                new TransitionCondition(AnimatorConditionMode.IfNot, 0f, "Grounded"),
                new TransitionCondition(AnimatorConditionMode.Less, -0.1f, "VerticalSpeed"));
            AddTransition(
                jumpState,
                fallState,
                new TransitionCondition(AnimatorConditionMode.Less, 0f, "VerticalSpeed"));
            AddTransition(
                jumpState,
                locomotion,
                new TransitionCondition(AnimatorConditionMode.If, 0f, "Grounded"));
            AddTransition(
                fallState,
                locomotion,
                new TransitionCondition(AnimatorConditionMode.If, 0f, "Grounded"));

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void AddTransition(
            AnimatorState from,
            AnimatorState to,
            params TransitionCondition[] conditions)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.1f;

            foreach (TransitionCondition condition in conditions)
            {
                transition.AddCondition(
                    condition.Mode,
                    condition.Threshold,
                    condition.Parameter);
            }
        }

        private static AnimationClip RequireClip(
            IReadOnlyDictionary<string, AnimationClip> clips,
            string name)
        {
            if (!clips.TryGetValue(name, out AnimationClip clip))
            {
                throw new MissingReferenceException(
                    $"Animation clip '{name}' not found in {CharacterModelPath}");
            }

            return clip;
        }

        private static Material GetOrCreateCharacterMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(CharacterMaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, CharacterMaterialPath);
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(CharacterTexturePath);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void NormalizeModelHeight(
            Transform model,
            float groundHeight,
            float targetHeight)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            if (bounds.size.y <= Mathf.Epsilon)
            {
                return;
            }

            model.localScale = Vector3.one * (targetHeight / bounds.size.y);

            bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            model.position += Vector3.up * (groundHeight - bounds.min.y);
        }

        private readonly struct TransitionCondition
        {
            public TransitionCondition(
                AnimatorConditionMode mode,
                float threshold,
                string parameter)
            {
                Mode = mode;
                Threshold = threshold;
                Parameter = parameter;
            }

            public AnimatorConditionMode Mode { get; }
            public float Threshold { get; }
            public string Parameter { get; }
        }
    }
}
