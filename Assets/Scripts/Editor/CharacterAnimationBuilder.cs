// GOLDEN STANDARD
// Purpose: Build reproducible Humanoid visuals and Animator assets in the Unity Editor.
// Responsibility: Import source assets, create materials/controllers, and save Main.unity.
// Invariant: Editor automation must be idempotent and must not run while entering Play Mode.
// Design choice: AssetDatabase-driven generation keeps binary setup reproducible from source code.
using System;
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
            "Assets/Art/ThirdParty/Quaternius/UniversalBaseCharacter/Models/"
            + "Superhero_Female_FullBody.fbx";
        private const string AnimationSourcePath =
            "Assets/Art/ThirdParty/Quaternius/UniversalAnimationLibrary/"
            + "UAL1_Standard.fbx";
        private const string CharacterBodyTexturePath =
            "Assets/Art/ThirdParty/Quaternius/UniversalBaseCharacter/Textures/"
            + "T_Superhero_Female_Light_BaseColor.png";
        private const string CharacterEyeTexturePath =
            "Assets/Art/ThirdParty/Quaternius/UniversalBaseCharacter/Textures/"
            + "T_Eye_Brown.png";
        private const string CharacterBodyMaterialPath =
            "Assets/Materials/HumanoidCharacterBody.mat";
        private const string CharacterEyeMaterialPath =
            "Assets/Materials/HumanoidCharacterEyes.mat";
        private const string AnimatorControllerPath =
            "Assets/Animations/HumanoidPlayer.controller";

        [InitializeOnLoadMethod]
        private static void BuildMissingCharacterSetup()
        {
            // Register deferred work only when the controller is absent or missing required states.
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (controller != null && HasActionSetup(controller))
            {
                return;
            }

            EditorApplication.delayCall += TryBuildMissingCharacterSetup;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        private static void TryBuildMissingCharacterSetup()
        {
            // Delay until edit mode so asset writes cannot invalidate a running player.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            Build();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            // Retry deferred setup after Unity returns to a safe editable state.
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                TryBuildMissingCharacterSetup();
            }
        }

        [MenuItem("Game Skill/Build Character Animation")]
        public static void Build()
        {
            // Rebuild the complete visual pipeline from canonical asset paths.
            ConfigureModelImporters();
            Material bodyMaterial = GetOrCreateCharacterMaterial(
                CharacterBodyMaterialPath,
                CharacterBodyTexturePath);
            Material eyeMaterial = GetOrCreateCharacterMaterial(
                CharacterEyeMaterialPath,
                CharacterEyeTexturePath);
            AnimatorController controller = CreateAnimatorController();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                throw new MissingReferenceException(
                    "Player가 없습니다. 먼저 Game Skill > Build Prototype Scene을 실행하세요.");
            }

            ConfigurePlayerVisual(player, bodyMaterial, eyeMaterial, controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Selection.activeGameObject = player;
            Debug.Log(
                "Quaternius Humanoid and side-scroller animations configured.");
        }

        public static void ConfigurePlayerVisual(GameObject player)
        {
            // Public convenience entry point used by the scene builder.
            ConfigureModelImporters();
            ConfigurePlayerVisual(
                player,
                GetOrCreateCharacterMaterial(
                    CharacterBodyMaterialPath,
                    CharacterBodyTexturePath),
                GetOrCreateCharacterMaterial(
                    CharacterEyeMaterialPath,
                    CharacterEyeTexturePath),
                CreateAnimatorController());
        }

        private static void ConfigurePlayerVisual(
            GameObject player,
            Material bodyMaterial,
            Material eyeMaterial,
            RuntimeAnimatorController controller)
        {
            // Replace generated visual children so repeated builds remain deterministic.
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

            // Apply portfolio materials to every renderer in the imported model hierarchy.
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = new Material[renderer.sharedMaterials.Length];
                // Preserve material slots while swapping only the authored prototype materials.
                for (int index = 0; index < materials.Length; index++)
                {
                    string sourceName = renderer.sharedMaterials[index] != null
                        ? renderer.sharedMaterials[index].name
                        : string.Empty;
                    materials[index] = sourceName.Contains(
                        "eye",
                        StringComparison.OrdinalIgnoreCase)
                        ? eyeMaterial
                        : bodyMaterial;
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

        private static void ConfigureModelImporters()
        {
            // Force both model sources into the same Humanoid import contract.
            ConfigureModelImporter(CharacterModelPath, false);
            ConfigureModelImporter(AnimationSourcePath, true);
        }

        private static void ConfigureModelImporter(string assetPath, bool importAnimations)
        {
            // Normalize one FBX importer and reimport only when settings changed.
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                throw new MissingReferenceException(
                    $"ModelImporter not found: {assetPath}");
            }

            bool needsReimport =
                importer.animationType != ModelImporterAnimationType.Human
                || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel
                || importer.importAnimation != importAnimations;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = importAnimations;

            if (!importAnimations)
            {
                if (needsReimport)
                {
                    importer.SaveAndReimport();
                }

                return;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0
                ? importer.clipAnimations
                : importer.defaultClipAnimations;
            // Loop only locomotion clips; one-shot actions must retain their authored exit.
            foreach (ModelImporterClipAnimation clip in clips)
            {
                string clipName = NormalizeClipName(clip.name);
                bool shouldLoop = clipName is
                    "Idle_Loop"
                    or "Walk_Loop"
                    or "Jog_Fwd_Loop"
                    or "Sprint_Loop"
                    or "Jump_Loop";
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
            // Create or incrementally upgrade the controller without duplicating states.
            AnimatorController existingController =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (existingController != null)
            {
                EnsureActionSetup(existingController);
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
            controller.AddParameter("Dodging", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attacking", AnimatorControllerParameterType.Bool);

            AnimationClip[] clips = LoadAnimationClips();

            AnimationClip idle = RequireClip(clips, "Idle_Loop");
            AnimationClip walk = RequireClip(clips, "Walk_Loop");
            AnimationClip jog = RequireClip(clips, "Jog_Fwd_Loop");
            AnimationClip sprint = RequireClip(clips, "Sprint_Loop");
            AnimationClip jump = RequireClip(clips, "Jump_Start");
            AnimationClip fall = RequireClip(clips, "Jump_Loop");
            AnimationClip attack = RequireClip(clips, "Punch_Cross");

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
            locomotionTree.AddChild(walk, 0.35f);
            locomotionTree.AddChild(jog, 0.7f);
            locomotionTree.AddChild(sprint, 1f);

            AnimatorState locomotion = stateMachine.AddState("Locomotion");
            locomotion.motion = locomotionTree;
            AnimatorState jumpState = stateMachine.AddState("Jump");
            jumpState.motion = jump;
            AnimatorState fallState = stateMachine.AddState("Fall");
            fallState.motion = fall;
            stateMachine.defaultState = locomotion;
            AddDashState(stateMachine, locomotion, fallState, sprint);
            AddAttackState(stateMachine, locomotion, fallState, attack);

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

        private static bool HasActionSetup(AnimatorController controller)
        {
            // Treat parameters and state names as the controller's migration version.
            bool hasDashParameter = controller.parameters.Any(
                parameter => parameter.name == "Dodging"
                    && parameter.type == AnimatorControllerParameterType.Bool);
            bool hasAttackParameter = controller.parameters.Any(
                parameter => parameter.name == "Attacking"
                    && parameter.type == AnimatorControllerParameterType.Bool);
            if (!hasDashParameter
                || !hasAttackParameter
                || controller.layers.Length == 0)
            {
                return false;
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            return FindState(stateMachine, "Dash") != null
                && FindState(stateMachine, "Attack") != null;
        }

        private static void EnsureActionSetup(AnimatorController controller)
        {
            // Add missing gameplay states in place so existing scene references survive upgrades.
            if (!controller.parameters.Any(parameter => parameter.name == "Dodging"))
            {
                controller.AddParameter(
                    "Dodging",
                    AnimatorControllerParameterType.Bool);
            }

            if (!controller.parameters.Any(parameter => parameter.name == "Attacking"))
            {
                controller.AddParameter(
                    "Attacking",
                    AnimatorControllerParameterType.Bool);
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState locomotion = FindState(stateMachine, "Locomotion");
            AnimatorState fall = FindState(stateMachine, "Fall");
            if (locomotion == null || fall == null)
            {
                throw new MissingReferenceException(
                    "HumanoidPlayer controller requires Locomotion and Fall states.");
            }

            AnimationClip[] clips = LoadAnimationClips();
            AnimatorState dash =
                FindState(stateMachine, "Dash")
                ?? FindState(stateMachine, "Dodge");
            if (dash == null)
            {
                AddDashState(
                    stateMachine,
                    locomotion,
                    fall,
                    RequireClip(clips, "Sprint_Loop"));
            }
            else
            {
                dash.name = "Dash";
                dash.motion = RequireClip(clips, "Sprint_Loop");
                dash.speed = 1.35f;
            }

            if (FindState(stateMachine, "Attack") == null)
            {
                AddAttackState(
                    stateMachine,
                    locomotion,
                    fall,
                    RequireClip(clips, "Punch_Cross"));
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        private static void AddDashState(
            AnimatorStateMachine stateMachine,
            AnimatorState locomotion,
            AnimatorState fall,
            AnimationClip dashClip)
        {
            // Dash uses a locomotion clip while gameplay owns the actual displacement curve.
            AnimatorState dash = stateMachine.AddState("Dash");
            dash.motion = dashClip;
            dash.speed = 1.35f;

            AnimatorStateTransition enterDash =
                stateMachine.AddAnyStateTransition(dash);
            enterDash.canTransitionToSelf = false;
            ConfigureTransition(
                enterDash,
                0.04f,
                new TransitionCondition(AnimatorConditionMode.If, 0f, "Dodging"));

            AddTransition(
                dash,
                locomotion,
                new TransitionCondition(AnimatorConditionMode.IfNot, 0f, "Dodging"),
                new TransitionCondition(AnimatorConditionMode.If, 0f, "Grounded"));
            AddTransition(
                dash,
                fall,
                new TransitionCondition(AnimatorConditionMode.IfNot, 0f, "Dodging"),
                new TransitionCondition(AnimatorConditionMode.IfNot, 0f, "Grounded"));
        }

        private static void AddAttackState(
            AnimatorStateMachine stateMachine,
            AnimatorState locomotion,
            AnimatorState fall,
            AnimationClip attackClip)
        {
            // Attack is an interruptible presentation state driven by a single bool parameter.
            AnimatorState attack = stateMachine.AddState("Attack");
            attack.motion = attackClip;
            attack.speed = Mathf.Max(1f, attackClip.length / 0.38f);

            AnimatorStateTransition enterAttack =
                stateMachine.AddAnyStateTransition(attack);
            enterAttack.canTransitionToSelf = false;
            ConfigureTransition(
                enterAttack,
                0.04f,
                new TransitionCondition(AnimatorConditionMode.If, 0f, "Attacking"));

            AddTransition(
                attack,
                locomotion,
                new TransitionCondition(AnimatorConditionMode.IfNot, 0f, "Attacking"),
                new TransitionCondition(AnimatorConditionMode.If, 0f, "Grounded"));
            AddTransition(
                attack,
                fall,
                new TransitionCondition(AnimatorConditionMode.IfNot, 0f, "Attacking"),
                new TransitionCondition(AnimatorConditionMode.IfNot, 0f, "Grounded"));
        }

        private static void AddTransition(
            AnimatorState from,
            AnimatorState to,
            params TransitionCondition[] conditions)
        {
            // Keep transition defaults consistent across every generated state.
            AnimatorStateTransition transition = from.AddTransition(to);
            ConfigureTransition(transition, 0.1f, conditions);
        }

        private static void ConfigureTransition(
            AnimatorStateTransition transition,
            float duration,
            params TransitionCondition[] conditions)
        {
            // Apply timing and conditions in one place to avoid subtle Animator differences.
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;

            // Multiple conditions form an AND gate in Unity's Animator transition system.
            foreach (TransitionCondition condition in conditions)
            {
                transition.AddCondition(
                    condition.Mode,
                    condition.Threshold,
                    condition.Parameter);
            }
        }

        private static AnimatorState FindState(
            AnimatorStateMachine stateMachine,
            string name)
        {
            // Search only the base layer because this prototype intentionally has one layer.
            return stateMachine.states
                .Select(childState => childState.state)
                .FirstOrDefault(state => state.name == name);
        }

        private static AnimationClip[] LoadAnimationClips()
        {
            // Collect clips from both model assets and discard Unity preview artifacts.
            return AssetDatabase.LoadAllAssetsAtPath(CharacterModelPath)
                .Concat(AssetDatabase.LoadAllAssetsAtPath(AnimationSourcePath))
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith(
                    "__preview__",
                    StringComparison.Ordinal))
                .ToArray();
        }

        private static AnimationClip RequireClip(
            AnimationClip[] clips,
            string name)
        {
            // Fail early with an actionable asset name instead of creating a broken controller.
            AnimationClip clip = clips.FirstOrDefault(
                candidate => NormalizeClipName(candidate.name).Equals(
                    name,
                    StringComparison.OrdinalIgnoreCase));
            if (clip == null)
            {
                throw new MissingReferenceException(
                    $"Animation clip '{name}' not found in {AnimationSourcePath}");
            }

            return clip;
        }

        private static string NormalizeClipName(string clipName)
        {
            int separatorIndex = clipName.LastIndexOf('|');
            return separatorIndex >= 0 ? clipName[(separatorIndex + 1)..] : clipName;
        }

        private static Material GetOrCreateCharacterMaterial(
            string materialPath,
            string texturePath)
        {
            // Reuse material assets so scene references remain stable across rebuilds.
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
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
            // Fit imported art to the gameplay capsule without changing the player's collision size.
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            // Encapsulate all renderers to calculate the complete visual bounds.
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
            // Recalculate bounds after scaling so the feet can be aligned to groundHeight.
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
