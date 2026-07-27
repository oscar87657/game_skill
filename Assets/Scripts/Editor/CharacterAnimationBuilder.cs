// GOLDEN STANDARD
// 목적: Unity Editor에서 재현 가능한 Humanoid 시각물과 Animator 에셋을 만든다.
// 책임: 원본 에셋을 임포트하고 재질·컨트롤러를 생성하여 Main.unity에 저장한다.
// 불변식: 에디터 자동화는 멱등적이며 Play Mode 진입 중 실행되지 않는다.
// 선택 이유: AssetDatabase 기반 생성으로 바이너리 설정도 소스 코드에서 재현한다.
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
            // 컨트롤러가 없거나 필수 상태가 없을 때만 지연 작업을 등록한다.
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
            // 실행 중인 플레이어를 무효화하지 않도록 Edit Mode까지 기다린다.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            Build();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            // Unity가 안전한 편집 상태로 돌아오면 지연 설정을 재시도한다.
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                TryBuildMissingCharacterSetup();
            }
        }

        [MenuItem("Game Skill/Build Character Animation")]
        public static void Build()
        {
            // 표준 에셋 경로에서 시각 파이프라인 전체를 재생성한다.
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
            // 씬 빌더가 사용하는 공개 편의 진입점이다.
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
            // 반복 빌드 결과가 결정적이도록 생성된 시각 자식을 교체한다.
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

            // 임포트된 모델 계층의 모든 Renderer에 포트폴리오 재질을 적용한다.
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = new Material[renderer.sharedMaterials.Length];
                // 재질 슬롯은 유지하고 프로토타입 재질만 교체한다.
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
            // 두 모델 원본이 동일한 Humanoid 임포트 계약을 사용하게 한다.
            ConfigureModelImporter(CharacterModelPath, false);
            ConfigureModelImporter(AnimationSourcePath, true);
        }

        private static void ConfigureModelImporter(string assetPath, bool importAnimations)
        {
            // 하나의 FBX 임포터를 표준화하고 설정이 바뀐 경우에만 재임포트한다.
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
            // 이동 클립만 반복하고 일회성 액션은 원래 종료 동작을 유지한다.
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
            // 상태를 중복 생성하지 않고 컨트롤러를 생성하거나 점진적으로 업그레이드한다.
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
            controller.AddParameter("ComboStep", AnimatorControllerParameterType.Int);

            AnimationClip[] clips = LoadAnimationClips();

            AnimationClip idle = RequireClip(clips, "Idle_Loop");
            AnimationClip walk = RequireClip(clips, "Walk_Loop");
            AnimationClip jog = RequireClip(clips, "Jog_Fwd_Loop");
            AnimationClip sprint = RequireClip(clips, "Sprint_Loop");
            AnimationClip jump = RequireClip(clips, "Jump_Start");
            AnimationClip fall = RequireClip(clips, "Jump_Loop");
            AnimationClip attackOne = RequireClip(clips, "Punch_Jab");
            AnimationClip attackTwo = RequireClip(clips, "Punch_Cross");
            AnimationClip attackThree = RequireClip(clips, "Sword_Attack");

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
            AddAttackComboStates(
                stateMachine,
                locomotion,
                fallState,
                attackOne,
                attackTwo,
                attackThree);

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
            // 파라미터와 상태 이름을 컨트롤러 마이그레이션 버전처럼 사용한다.
            bool hasDashParameter = controller.parameters.Any(
                parameter => parameter.name == "Dodging"
                    && parameter.type == AnimatorControllerParameterType.Bool);
            bool hasAttackParameter = controller.parameters.Any(
                parameter => parameter.name == "Attacking"
                    && parameter.type == AnimatorControllerParameterType.Bool);
            bool hasComboStepParameter = controller.parameters.Any(
                parameter => parameter.name == "ComboStep"
                    && parameter.type == AnimatorControllerParameterType.Int);
            if (!hasDashParameter
                || !hasAttackParameter
                || !hasComboStepParameter
                || controller.layers.Length == 0)
            {
                return false;
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            return FindState(stateMachine, "Dash") != null
                && FindState(stateMachine, "Attack1") != null
                && FindState(stateMachine, "Attack2") != null
                && FindState(stateMachine, "Attack3") != null;
        }

        private static void EnsureActionSetup(AnimatorController controller)
        {
            // 기존 씬 참조를 보존하도록 누락된 게임플레이 상태를 제자리에서 추가한다.
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

            if (!controller.parameters.Any(parameter => parameter.name == "ComboStep"))
            {
                controller.AddParameter(
                    "ComboStep",
                    AnimatorControllerParameterType.Int);
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

            if (FindState(stateMachine, "Attack1") == null
                || FindState(stateMachine, "Attack2") == null
                || FindState(stateMachine, "Attack3") == null)
            {
                RemoveAttackStates(stateMachine);
                AddAttackComboStates(
                    stateMachine,
                    locomotion,
                    fall,
                    RequireClip(clips, "Punch_Jab"),
                    RequireClip(clips, "Punch_Cross"),
                    RequireClip(clips, "Sword_Attack"));
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
            // 대시는 이동 클립을 사용하고 실제 이동 곡선은 게임플레이 코드가 소유한다.
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

        private static void AddAttackComboStates(
            AnimatorStateMachine stateMachine,
            AnimatorState locomotion,
            AnimatorState fall,
            AnimationClip attackOneClip,
            AnimationClip attackTwoClip,
            AnimationClip attackThreeClip)
        {
            // 콤보 단계마다 다른 클립을 사용하되 진입·종료 규칙은 공통 함수에서 구성한다.
            AddAttackComboState(
                stateMachine,
                locomotion,
                fall,
                "Attack1",
                attackOneClip,
                1);
            AddAttackComboState(
                stateMachine,
                locomotion,
                fall,
                "Attack2",
                attackTwoClip,
                2);
            AddAttackComboState(
                stateMachine,
                locomotion,
                fall,
                "Attack3",
                attackThreeClip,
                3);
        }

        private static void AddAttackComboState(
            AnimatorStateMachine stateMachine,
            AnimatorState locomotion,
            AnimatorState fall,
            string stateName,
            AnimationClip attackClip,
            int comboStep)
        {
            // 단계 값과 상태 이름을 함께 생성하여 Animator와 PlayerCombat의 계약을 명확히 한다.
            AnimatorState attack = stateMachine.AddState(stateName);
            attack.motion = attackClip;
            attack.speed = Mathf.Max(1f, attackClip.length / 0.38f);

            AnimatorStateTransition enterAttack =
                stateMachine.AddAnyStateTransition(attack);
            enterAttack.canTransitionToSelf = false;
            ConfigureTransition(
                enterAttack,
                0.04f,
                new TransitionCondition(AnimatorConditionMode.If, 0f, "Attacking"),
                new TransitionCondition(
                    AnimatorConditionMode.Equals,
                    comboStep,
                    "ComboStep"));

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

        private static void RemoveAttackStates(AnimatorStateMachine stateMachine)
        {
            // 이전 단일 공격 상태나 불완전한 콤보 상태를 제거해 마이그레이션 중 중복 전이를 막는다.
            string[] attackStateNames = { "Attack", "Attack1", "Attack2", "Attack3" };
            foreach (string stateName in attackStateNames)
            {
                AnimatorState state = FindState(stateMachine, stateName);
                if (state != null)
                {
                    stateMachine.RemoveState(state);
                }
            }
        }

        private static void AddTransition(
            AnimatorState from,
            AnimatorState to,
            params TransitionCondition[] conditions)
        {
            // 생성되는 모든 상태의 전이 기본값을 일관되게 유지한다.
            AnimatorStateTransition transition = from.AddTransition(to);
            ConfigureTransition(transition, 0.1f, conditions);
        }

        private static void ConfigureTransition(
            AnimatorStateTransition transition,
            float duration,
            params TransitionCondition[] conditions)
        {
            // 타이밍과 조건을 한곳에서 적용하여 Animator의 미세한 차이를 막는다.
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = duration;

            // 여러 조건은 Unity Animator에서 AND 게이트로 동작한다.
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
            // 이 프로토타입은 한 레이어만 사용하므로 Base Layer만 검색한다.
            return stateMachine.states
                .Select(childState => childState.state)
                .FirstOrDefault(state => state.name == name);
        }

        private static AnimationClip[] LoadAnimationClips()
        {
            // 두 모델 에셋에서 클립을 모으고 Unity 미리보기 부산물은 버린다.
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
            // 깨진 컨트롤러를 만드는 대신 문제의 에셋 이름을 즉시 알려준다.
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
            // 재질 에셋을 재사용하여 재빌드 후에도 씬 참조를 안정적으로 유지한다.
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
            // 플레이어 충돌 크기는 유지하면서 임포트된 아트를 게임 캡슐에 맞춘다.
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            // 모든 Renderer를 감싸 전체 시각 경계를 계산한다.
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
            // 크기 조정 후 경계를 다시 계산하여 발을 groundHeight에 맞춘다.
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
