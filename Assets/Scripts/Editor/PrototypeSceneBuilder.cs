// GOLDEN STANDARD
// 목적: 가장 작은 플레이 가능한 2.5D Showcase 씬을 생성하고 마이그레이션한다.
// 책임: 플레이어·그레이박스·카메라·전투 적·능력·백트래킹 보상 루프와 에디터 메뉴를 생성한다.
// 불변식: 빌더를 다시 실행해도 자신이 만든 이름의 프로토타입 루트만 제거한다.
// 선택 이유: 씬 생성은 에디터 전용으로 두어 런타임 스크립트를 게임플레이에 집중시킨다.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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
        private const string AbilityFolderPath = "Assets/Settings/Abilities";
        private const string DoubleJumpAbilityPath =
            AbilityFolderPath + "/Ability_DoubleJump.asset";
        private const string AirDashAbilityPath =
            AbilityFolderPath + "/Ability_AirDash.asset";
        private const string WallTraversalAbilityPath =
            AbilityFolderPath + "/Ability_WallTraversal.asset";
        private const string WorldZoneFolderPath =
            "Assets/Settings/WorldZones";
        private const string BacktrackShaftZonePath =
            WorldZoneFolderPath + "/WorldZone_BacktrackShaft.asset";
        private const string StartHallZonePath =
            WorldZoneFolderPath + "/WorldZone_StartHall.asset";
        private const string TraversalLabZonePath =
            WorldZoneFolderPath + "/WorldZone_TraversalLab.asset";
        private const string ShaftReturnShortcutId =
            "shortcut_shaft_return";
        private const string ShaftHealthRewardId =
            "reward_shaft_health_fragment";
        private const string ZoneSceneFolderPath =
            "Assets/Scenes/Zones";
        private const string BacktrackShaftScenePath =
            ZoneSceneFolderPath + "/Zone_BacktrackShaft.unity";
        private const string StartHallScenePath =
            ZoneSceneFolderPath + "/Zone_StartHall.unity";
        private const string TraversalLabScenePath =
            ZoneSceneFolderPath + "/Zone_TraversalLab.unity";
        private const string BacktrackBackdropMaterialPath =
            "Assets/Materials/ZoneBackdrop_Backtrack.mat";
        private const string StartBackdropMaterialPath =
            "Assets/Materials/ZoneBackdrop_Start.mat";
        private const string TraversalBackdropMaterialPath =
            "Assets/Materials/ZoneBackdrop_Traversal.mat";
        private const float CameraVerticalHalfExtent = 5.2f;
        private const float CameraPerspectiveFieldOfView = 35f;
        private const float DashMovementDuration = 0.2f;
        private const float DashCooldownDuration = 0.48f;
        private const float DashInvulnerabilityDuration = 0.3f;
        private const float MeleeAttackWindupDuration = 0.55f;
        private const float MeleeAttackRecoveryDuration = 0.7f;
        private const float RangedAttackWindupDuration = 0.8f;
        private const float RangedAttackRecoveryDuration = 1.6f;

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
            EnsureMeleeEnemyPrototype(
                player,
                GameObject.Find(GrayboxRootName),
                accentMaterial);
            EnsureRangedEnemyPrototype(
                player,
                GameObject.Find(GrayboxRootName),
                accentMaterial);
            EnsureChargeEnemyPrototype(
                player,
                GameObject.Find(GrayboxRootName),
                accentMaterial);
            EnsureAbilityPrototype(
                player,
                GameObject.Find(GrayboxRootName),
                accentMaterial,
                groundMaterial);
            EnsureWorldZonePrototype(
                player,
                GameObject.Find(GrayboxRootName));
            EnsureWorldShortcutPrototype(
                player,
                GameObject.Find(GrayboxRootName),
                groundMaterial,
                accentMaterial);
            EnsureZoneStreamingPrototype(player);
            ConfigureCamera(player);
            EnsureWorldMapPrototype(player);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log("2.5D side-scroller prototype created: Assets/Scenes/Main.unity");
        }

        private static GameObject CreatePlayer()
        {
            // 여기서는 게임플레이 컴포넌트만 조립하고 시각 자식은 애니메이션 빌더가 소유한다.
            var player = new GameObject("Player");
            player.layer =
                CharacterBodyCollisionPolicy.PlayerBodyLayer;
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

            Health health = player.AddComponent<Health>();
            health.Configure(5);
            player.AddComponent<PlayerCheckpointState>();
            player.AddComponent<PlayerWorldState>();
            PlayerAbilityState abilityState =
                player.AddComponent<PlayerAbilityState>();
            AbilityDefinition doubleJumpAbility =
                GetOrCreateAbilityDefinition(
                    DoubleJumpAbilityPath,
                    "double_jump",
                    "2단 점프",
                    "공중에서 한 번 더 점프한다.");
            AbilityDefinition airDashAbility =
                GetOrCreateAbilityDefinition(
                    AirDashAbilityPath,
                    "air_dash",
                    "공중 대시",
                    "공중에서 수평 대시를 한 번 사용한다.");
            AbilityDefinition wallTraversalAbility =
                GetOrCreateAbilityDefinition(
                    WallTraversalAbilityPath,
                    "wall_traversal",
                    "벽 잡기",
                    "벽에 잠시 붙고 미끄러지며 반대편으로 점프한다.");
            SideScrollerMotor motor =
                player.AddComponent<SideScrollerMotor>();
            motor.ConfigureAbilityRequirements(
                abilityState,
                doubleJumpAbility,
                airDashAbility,
                wallTraversalAbility);
            motor.ConfigureDashTiming(
                DashMovementDuration,
                DashCooldownDuration,
                DashInvulnerabilityDuration);
            player.AddComponent<SideScrollerTargeting>();
            player.AddComponent<PlayerCombat>();
            player.AddComponent<PlayerRespawnController>();
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
            sideScrollerCamera.ConfigurePerspective(
                CameraVerticalHalfExtent,
                CameraPerspectiveFieldOfView);

            camera.transform.SetPositionAndRotation(
                new Vector3(
                    1.35f,
                    2.4f,
                    -sideScrollerCamera.CameraDistance),
                Quaternion.identity);
            EnsureCameraBoundsPrototype(player);
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
                new Vector3(50f, 1f, 3f),
                groundMaterial);
            CreateBlock(
                root.transform,
                "Step_A",
                new Vector3(10f, 0.5f, 0f),
                new Vector3(3f, 1f, 3f),
                groundMaterial);
            CreateBlock(
                root.transform,
                "Step_B",
                new Vector3(14f, 1.5f, 0f),
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
                new Vector3(20f, 3.2f, 0f),
                new Vector3(12f, 0.6f, 3f),
                groundMaterial);
            CreateBlock(
                root.transform,
                "Wall_Gate",
                new Vector3(-6f, 2f, 0f),
                new Vector3(0.6f, 4f, 3f),
                accentMaterial);
            CreateCheckpoint(root.transform, accentMaterial);
            CreateRespawnHazard(root.transform, accentMaterial);
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
            if (player != null
                && player.layer
                    != CharacterBodyCollisionPolicy.PlayerBodyLayer)
            {
                // CharacterController가 환경에는 막히고 적의 몸만 통과하도록 전용 레이어를 적용한다.
                player.layer =
                    CharacterBodyCollisionPolicy.PlayerBodyLayer;
                EditorUtility.SetDirty(player);
                changed = true;
            }

            if (player != null && player.GetComponent<Health>() == null)
            {
                // 생존 흐름의 단일 체력 원본을 플레이어 루트에 추가한다.
                Health health = player.AddComponent<Health>();
                health.Configure(5);
                changed = true;
            }

            if (player != null && player.GetComponent<PlayerCheckpointState>() == null)
            {
                // 기존 Main 씬도 재생성 없이 체크포인트 진행 상태를 사용할 수 있게 한다.
                player.AddComponent<PlayerCheckpointState>();
                changed = true;
            }

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

            if (player != null
                && player.GetComponent<PlayerRespawnController>() == null)
            {
                // 사망 이벤트가 체크포인트 상태를 소비하도록 기존 플레이어에 생존 컨트롤러를 추가한다.
                player.AddComponent<PlayerRespawnController>();
                changed = true;
            }

            if (player != null
                && player.GetComponent<PlayerAbilityState>() == null)
            {
                // 능력 진행의 단일 런타임 원본을 기존 플레이어 루트에 추가한다.
                player.AddComponent<PlayerAbilityState>();
                changed = true;
            }

            if (player != null
                && player.GetComponent<PlayerWorldState>() == null)
            {
                // 기존 Main 씬도 지도와 저장이 공유할 구역 방문 상태를 갖게 한다.
                player.AddComponent<PlayerWorldState>();
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

            GameObject trainingDummy =
                GameObject.Find("TrainingDummy");
            if (trainingDummy != null
                && trainingDummy.layer
                    != CharacterBodyCollisionPolicy.EnemyBodyLayer)
            {
                // 움직이지 않는 연습용 더미도 적 몸으로 분류해 플레이어와 실제 적을 밀어내지 않게 한다.
                trainingDummy.layer =
                    CharacterBodyCollisionPolicy.EnemyBodyLayer;
                EditorUtility.SetDirty(trainingDummy);
                changed = true;
            }

            if (GameObject.Find("Checkpoint_Start") == null)
            {
                GameObject root = GameObject.Find(GrayboxRootName);
                Material accentMaterial =
                    AssetDatabase.LoadAssetAtPath<Material>(AccentMaterialPath);
                if (root != null && accentMaterial != null)
                {
                    CreateCheckpoint(root.transform, accentMaterial);
                    changed = true;
                }
            }

            if (GameObject.Find("RespawnHazard") == null)
            {
                GameObject root = GameObject.Find(GrayboxRootName);
                Material accentMaterial =
                    AssetDatabase.LoadAssetAtPath<Material>(AccentMaterialPath);
                if (root != null && accentMaterial != null)
                {
                    CreateRespawnHazard(root.transform, accentMaterial);
                    changed = true;
                }
            }

            GameObject grayboxRoot = GameObject.Find(GrayboxRootName);
            Material abilityMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(AccentMaterialPath);
            Material groundMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            if (EnsureHighPlatformCombatWidth(
                grayboxRoot))
            {
                // 기존 Main 씬의 높은 발판도 돌진·탄환 회피를 반복할 수 있는 폭으로 마이그레이션한다.
                changed = true;
            }

            if (EnsureMeleeEnemyPrototype(
                player,
                grayboxRoot,
                abilityMaterial))
            {
                // 근거리 적의 상태 머신·체력·이동기 연결이 보완되면 Main 씬에 저장한다.
                changed = true;
            }

            if (EnsureRangedEnemyPrototype(
                player,
                grayboxRoot,
                abilityMaterial))
            {
                // 고정형 원거리 적·충전 표시·투사체 설정이 보완되면 Main 씬에 저장한다.
                changed = true;
            }

            if (EnsureChargeEnemyPrototype(
                player,
                grayboxRoot,
                abilityMaterial))
            {
                // 돌진 적·방향 표시·발판 안전 이동이 보완되면 Main 씬에 저장한다.
                changed = true;
            }

            if (EnsureAbilityPrototype(
                player,
                grayboxRoot,
                abilityMaterial,
                groundMaterial))
            {
                // 능력 에셋·픽업·게이트 중 하나라도 추가되면 씬을 저장 대상으로 표시한다.
                changed = true;
            }

            if (EnsureWorldZonePrototype(player, grayboxRoot))
            {
                // 세 구역 정의·방문 상태·Trigger 중 하나라도 보완되면 씬을 저장한다.
                changed = true;
            }

            if (EnsureWorldShortcutPrototype(
                player,
                grayboxRoot,
                groundMaterial,
                abilityMaterial))
            {
                // 샤프트 정상과 시작 홀을 잇는 다리·게이트·활성 장치를 한 진행 단위로 저장한다.
                changed = true;
            }

            if (EnsureZoneStreamingPrototype(player))
            {
                // Additive 구역 Scene·Build Settings·런타임 제어기 중 변경된 구성을 Main에 저장한다.
                changed = true;
            }

            if (EnsureCameraBoundsPrototype(player))
            {
                // 세 구역 카메라 중심점 경계가 추가·변경되면 Main Camera 구성을 저장한다.
                changed = true;
            }

            if (EnsureWorldMapPrototype(player))
            {
                // 지도 Canvas·노드·연결선·Presenter 중 하나라도 보완되면 Main에 저장한다.
                changed = true;
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

        private static bool EnsureHighPlatformCombatWidth(
            GameObject grayboxRoot)
        {
            // 돌진 적 양쪽으로 회피할 공간이 생기도록 기존 높은 발판의 배치와 폭을 멱등적으로 맞춘다.
            if (grayboxRoot == null)
            {
                return false;
            }

            Transform highPlatform =
                grayboxRoot.transform.Find(
                    "High_Platform");
            return highPlatform != null
                && SetTransformIfDifferent(
                    highPlatform,
                    new Vector3(20f, 3.2f, 0f),
                    new Vector3(12f, 0.6f, 3f));
        }

        private static bool EnsureWorldMapPrototype(
            GameObject player)
        {
            // 플레이어 월드 상태가 있어야 지도 UI를 진행 데이터와 연결할 수 있다.
            if (player == null)
            {
                return false;
            }

            PlayerWorldState worldState =
                player.GetComponent<PlayerWorldState>();
            if (worldState == null)
            {
                return false;
            }

            WorldZoneDefinition backtrackShaft =
                GetOrCreateWorldZoneDefinition(
                    BacktrackShaftZonePath,
                    "backtrack_shaft",
                    "백트래킹 샤프트",
                    "벽 잡기 해금 후 시작 홀로 되돌아와 오르는 수직 구역.");
            WorldZoneDefinition startHall =
                GetOrCreateWorldZoneDefinition(
                    StartHallZonePath,
                    "start_hall",
                    "시작 홀",
                    "체크포인트와 첫 능력 단서를 제공하는 중앙 구역.");
            WorldZoneDefinition traversalLab =
                GetOrCreateWorldZoneDefinition(
                    TraversalLabZonePath,
                    "traversal_lab",
                    "이동 실험실",
                    "계단과 높은 발판에서 2단 점프와 공중 대시를 익히는 구역.");

            bool changed = false;
            GameObject hud = GameObject.Find("WorldMapHUD");
            if (hud == null)
            {
                // Screen Space Overlay Canvas는 카메라 전환과 Additive Scene 언로드의 영향을 받지 않는다.
                hud = new GameObject(
                    "WorldMapHUD",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                changed = true;
            }

            Canvas canvas = hud.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler =
                hud.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution =
                new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panel = EnsureUiImage(
                "WorldMapPanel",
                hud.transform,
                ref changed);
            RectTransform panelRect =
                panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.one;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = Vector2.one;
            panelRect.anchoredPosition =
                new Vector2(-24f, -24f);
            panelRect.sizeDelta =
                new Vector2(330f, 160f);
            Image panelImage = panel.GetComponent<Image>();
            panelImage.color =
                new Color(0.035f, 0.045f, 0.065f, 0.82f);
            panelImage.raycastTarget = false;

            EnsureMapText(
                "WorldMapTitle",
                panel.transform,
                "MAP",
                new Vector2(0f, 58f),
                new Vector2(280f, 24f),
                17,
                ref changed);

            Vector2 shaftPosition =
                new(-105f, 15f);
            Vector2 startPosition =
                new(-20f, -35f);
            Vector2 labPosition =
                new(105f, -35f);
            Image shaftStartLine = EnsureMapConnection(
                "MapConnection_ShaftStart",
                panel.transform,
                shaftPosition,
                startPosition,
                ref changed);
            Image startLabLine = EnsureMapConnection(
                "MapConnection_StartLab",
                panel.transform,
                startPosition,
                labPosition,
                ref changed);

            WorldMapNodeView shaftNode = EnsureMapNode(
                "MapNode_BacktrackShaft",
                panel.transform,
                backtrackShaft,
                "SHAFT",
                shaftPosition,
                ref changed);
            WorldMapNodeView startNode = EnsureMapNode(
                "MapNode_StartHall",
                panel.transform,
                startHall,
                "START",
                startPosition,
                ref changed);
            WorldMapNodeView labNode = EnsureMapNode(
                "MapNode_TraversalLab",
                panel.transform,
                traversalLab,
                "LAB",
                labPosition,
                ref changed);

            WorldMapPresenter presenter =
                hud.GetComponent<WorldMapPresenter>();
            if (presenter == null)
            {
                presenter =
                    hud.AddComponent<WorldMapPresenter>();
                changed = true;
            }

            var nodes = new List<WorldMapNodeView>
            {
                shaftNode,
                startNode,
                labNode
            };
            var connections =
                new List<WorldMapConnectionView>
                {
                    new(
                        backtrackShaft,
                        startHall,
                        shaftStartLine),
                    new(
                        startHall,
                        traversalLab,
                        startLabLine)
                };
            if (presenter.Configure(
                worldState,
                startHall,
                nodes,
                connections))
            {
                EditorUtility.SetDirty(presenter);
                changed = true;
            }

            return changed;
        }

        private static GameObject EnsureUiImage(
            string objectName,
            Transform parent,
            ref bool changed)
        {
            // 이름을 UI 배치 키로 사용해 빌더 재실행 시 같은 Image 오브젝트를 재사용한다.
            GameObject imageObject =
                GameObject.Find(objectName);
            if (imageObject == null)
            {
                imageObject = new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                imageObject.transform.SetParent(
                    parent,
                    false);
                changed = true;
            }

            if (imageObject.GetComponent<Image>() == null)
            {
                // 부분 편집으로 Image가 제거된 경우에도 전체 HUD를 재생성하지 않고 복구한다.
                imageObject.AddComponent<Image>();
                changed = true;
            }

            return imageObject;
        }

        private static Text EnsureMapText(
            string objectName,
            Transform parent,
            string content,
            Vector2 position,
            Vector2 size,
            int fontSize,
            ref bool changed)
        {
            // 짧은 ASCII 레이블은 프로젝트 외부 폰트 에셋 없이 런타임 기본 폰트로 표시한다.
            GameObject textObject =
                GameObject.Find(objectName);
            if (textObject == null)
            {
                textObject = new GameObject(
                    objectName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));
                textObject.transform.SetParent(
                    parent,
                    false);
                changed = true;
            }

            Text text = textObject.GetComponent<Text>();
            RectTransform rect =
                textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            text.font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = content;
            return text;
        }

        private static Image EnsureMapConnection(
            string objectName,
            Transform parent,
            Vector2 start,
            Vector2 end,
            ref bool changed)
        {
            // 두 지도 노드 중심 사이에 회전한 얇은 Image를 배치해 연결 그래프를 표현한다.
            GameObject lineObject = EnsureUiImage(
                objectName,
                parent,
                ref changed);
            RectTransform rect =
                lineObject.GetComponent<RectTransform>();
            Vector2 difference = end - start;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (start + end) * 0.5f;
            rect.sizeDelta =
                new Vector2(difference.magnitude, 5f);
            rect.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(
                    difference.y,
                    difference.x)
                * Mathf.Rad2Deg);
            Image line = lineObject.GetComponent<Image>();
            line.raycastTarget = false;
            return line;
        }

        private static WorldMapNodeView EnsureMapNode(
            string objectName,
            Transform parent,
            WorldZoneDefinition zone,
            string labelText,
            Vector2 position,
            ref bool changed)
        {
            // 노드 배경과 자식 레이블을 고정된 이름으로 만들어 테스트와 포트폴리오 캡처에서 찾기 쉽게 한다.
            GameObject nodeObject = EnsureUiImage(
                objectName,
                parent,
                ref changed);
            RectTransform rect =
                nodeObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(72f, 36f);
            Image background =
                nodeObject.GetComponent<Image>();
            background.raycastTarget = false;

            Text label = EnsureMapText(
                objectName + "_Label",
                nodeObject.transform,
                labelText,
                Vector2.zero,
                new Vector2(68f, 32f),
                14,
                ref changed);
            return new WorldMapNodeView(
                zone,
                labelText,
                background,
                label);
        }

        private static bool EnsureCameraBoundsPrototype(
            GameObject player)
        {
            // 플레이어·월드 상태·Main Camera가 모두 있을 때만 경계 이벤트 연결을 구성한다.
            if (player == null || Camera.main == null)
            {
                return false;
            }

            PlayerWorldState worldState =
                player.GetComponent<PlayerWorldState>();
            SideScrollerCamera sideScrollerCamera =
                Camera.main.GetComponent<SideScrollerCamera>();
            if (worldState == null || sideScrollerCamera == null)
            {
                return false;
            }

            WorldZoneDefinition backtrackShaft =
                GetOrCreateWorldZoneDefinition(
                    BacktrackShaftZonePath,
                    "backtrack_shaft",
                    "백트래킹 샤프트",
                    "벽 잡기 해금 후 시작 홀로 되돌아와 오르는 수직 구역.");
            WorldZoneDefinition startHall =
                GetOrCreateWorldZoneDefinition(
                    StartHallZonePath,
                    "start_hall",
                    "시작 홀",
                    "체크포인트와 첫 능력 단서를 제공하는 중앙 구역.");
            WorldZoneDefinition traversalLab =
                GetOrCreateWorldZoneDefinition(
                    TraversalLabZonePath,
                    "traversal_lab",
                    "이동 실험실",
                    "계단과 높은 발판에서 2단 점프와 공중 대시를 익히는 구역.");

            var bounds = new List<CameraZoneBounds>
            {
                // 좁은 수직 샤프트는 X 중심을 고정하고 등반 높이에만 반응하게 한다.
                new(
                    backtrackShaft,
                    new Vector2(-10.75f, 2.8f),
                    new Vector2(-10.75f, 9f)),
                // 시작 홀은 체크포인트와 양쪽 출구가 동시에 읽히는 제한된 수평 이동을 허용한다.
                new(
                    startHall,
                    new Vector2(-4f, 2.4f),
                    new Vector2(2f, 9f)),
                // 이동 실험실은 긴 계단 동선을 따라가되 구역 밖을 과도하게 보여 주지 않는다.
                new(
                    traversalLab,
                    new Vector2(10f, 3f),
                    new Vector2(20f, 7.2f))
            };

            bool changed = false;
            sideScrollerCamera.Configure(player.transform);
            if (sideScrollerCamera.ConfigurePerspective(
                CameraVerticalHalfExtent,
                CameraPerspectiveFieldOfView))
            {
                // 정사영 구도를 유지하는 거리와 약한 원근 FOV가 바뀐 경우 두 컴포넌트를 저장한다.
                EditorUtility.SetDirty(Camera.main);
                changed = true;
            }

            Vector3 currentCameraPosition =
                Camera.main.transform.position;
            float requestedCameraDepth =
                -sideScrollerCamera.CameraDistance;
            if (!Mathf.Approximately(
                currentCameraPosition.z,
                requestedCameraDepth))
            {
                // Edit Mode Scene 뷰에서도 런타임과 같은 깊이에 카메라가 보이도록 직렬화 위치를 맞춘다.
                Camera.main.transform.position =
                    new Vector3(
                        currentCameraPosition.x,
                        currentCameraPosition.y,
                        requestedCameraDepth);
                EditorUtility.SetDirty(
                    Camera.main.transform);
                changed = true;
            }

            if (sideScrollerCamera.ConfigureWorldBounds(
                worldState,
                startHall,
                bounds))
            {
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(sideScrollerCamera);
            }

            return changed;
        }

        private static bool EnsureZoneStreamingPrototype(
            GameObject player)
        {
            // 플레이어 상태가 없으면 Scene만 생성하고 연결이 빠지는 부분 구성을 만들지 않는다.
            if (player == null)
            {
                return false;
            }

            PlayerWorldState worldState =
                player.GetComponent<PlayerWorldState>();
            if (worldState == null)
            {
                return false;
            }

            EnsureZoneSceneFolder();
            Material backtrackMaterial = GetOrCreateMaterial(
                BacktrackBackdropMaterialPath,
                new Color(0.08f, 0.14f, 0.22f));
            Material startMaterial = GetOrCreateMaterial(
                StartBackdropMaterialPath,
                new Color(0.18f, 0.1f, 0.24f));
            Material traversalMaterial = GetOrCreateMaterial(
                TraversalBackdropMaterialPath,
                new Color(0.24f, 0.16f, 0.06f));

            bool changed = false;
            changed |= EnsureZoneContentScene(
                BacktrackShaftScenePath,
                "ZoneContent_BacktrackShaft",
                new Vector3(-10.75f, 4f, 2.4f),
                new Vector3(4.5f, 10f, 0.2f),
                backtrackMaterial);
            changed |= EnsureZoneContentScene(
                StartHallScenePath,
                "ZoneContent_StartHall",
                new Vector3(-1f, 4f, 2.4f),
                new Vector3(15f, 10f, 0.2f),
                startMaterial);
            changed |= EnsureZoneContentScene(
                TraversalLabScenePath,
                "ZoneContent_TraversalLab",
                new Vector3(15.5f, 4f, 2.4f),
                new Vector3(18f, 10f, 0.2f),
                traversalMaterial);

            if (EnsureZoneScenesInBuildSettings())
            {
                changed = true;
            }

            WorldZoneDefinition backtrackShaft =
                GetOrCreateWorldZoneDefinition(
                    BacktrackShaftZonePath,
                    "backtrack_shaft",
                    "백트래킹 샤프트",
                    "벽 잡기 해금 후 시작 홀로 되돌아와 오르는 수직 구역.");
            WorldZoneDefinition startHall =
                GetOrCreateWorldZoneDefinition(
                    StartHallZonePath,
                    "start_hall",
                    "시작 홀",
                    "체크포인트와 첫 능력 단서를 제공하는 중앙 구역.");
            WorldZoneDefinition traversalLab =
                GetOrCreateWorldZoneDefinition(
                    TraversalLabZonePath,
                    "traversal_lab",
                    "이동 실험실",
                    "계단과 높은 발판에서 2단 점프와 공중 대시를 익히는 구역.");

            GameObject streamingObject =
                GameObject.Find("WorldZoneStreaming");
            if (streamingObject == null)
            {
                // Main Scene에 남는 제어기 루트는 Additive 콘텐츠와 함께 언로드되지 않는다.
                streamingObject =
                    new GameObject("WorldZoneStreaming");
                changed = true;
            }

            WorldZoneStreamController controller =
                streamingObject.GetComponent<WorldZoneStreamController>();
            if (controller == null)
            {
                controller =
                    streamingObject.AddComponent<WorldZoneStreamController>();
                changed = true;
            }

            var bindings = new List<WorldZoneSceneBinding>
            {
                new(backtrackShaft, BacktrackShaftScenePath),
                new(startHall, StartHallScenePath),
                new(traversalLab, TraversalLabScenePath)
            };
            if (controller.Configure(
                worldState,
                startHall,
                bindings))
            {
                EditorUtility.SetDirty(controller);
                changed = true;
            }

            return changed;
        }

        private static bool EnsureZoneContentScene(
            string scenePath,
            string rootName,
            Vector3 backdropPosition,
            Vector3 backdropScale,
            Material backdropMaterial)
        {
            // 이미 생성된 구역 Scene은 GUID와 사용자의 후속 편집을 보존하고 다시 만들지 않는다.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath)
                != null)
            {
                return false;
            }

            Scene mainScene = SceneManager.GetActiveScene();
            Scene zoneScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
            var root = new GameObject(rootName);
            GameObject backdrop = CreateBlock(
                root.transform,
                rootName + "_Backdrop",
                backdropPosition,
                backdropScale,
                backdropMaterial);
            Collider backdropCollider =
                backdrop.GetComponent<Collider>();
            if (backdropCollider != null)
            {
                // Additive 배경 콘텐츠는 Main의 게임플레이 충돌과 중복되지 않게 시각 역할만 가진다.
                Object.DestroyImmediate(backdropCollider);
            }

            EditorSceneManager.SaveScene(zoneScene, scenePath);
            EditorSceneManager.CloseScene(zoneScene, true);
            if (mainScene.IsValid() && mainScene.isLoaded)
            {
                // 임시 구역 Scene 저장 뒤 원래 Main을 다시 활성화해 이후 빌더 오브젝트의 소유 Scene을 보존한다.
                SceneManager.SetActiveScene(mainScene);
            }

            return true;
        }

        private static bool EnsureZoneScenesInBuildSettings()
        {
            string[] desiredPaths =
            {
                ScenePath,
                BacktrackShaftScenePath,
                StartHallScenePath,
                TraversalLabScenePath
            };
            var scenes =
                new List<EditorBuildSettingsScene>(
                    EditorBuildSettings.scenes);
            bool changed = false;

            // 기존 사용자 Scene 순서는 유지하고 누락된 스트리밍 Scene만 마지막에 추가한다.
            foreach (string desiredPath in desiredPaths)
            {
                bool exists = scenes.Exists(
                    scene => scene.path == desiredPath);
                if (!exists)
                {
                    scenes.Add(
                        new EditorBuildSettingsScene(
                            desiredPath,
                            true));
                    changed = true;
                }
            }

            if (changed)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            return changed;
        }

        private static void EnsureZoneSceneFolder()
        {
            // Additive Scene 전용 폴더를 AssetDatabase API로 생성해 메타 GUID를 안정적으로 관리한다.
            if (!AssetDatabase.IsValidFolder(ZoneSceneFolderPath))
            {
                AssetDatabase.CreateFolder(
                    "Assets/Scenes",
                    "Zones");
            }
        }

        private static bool EnsureWorldShortcutPrototype(
            GameObject player,
            GameObject grayboxRoot,
            Material groundMaterial,
            Material accentMaterial)
        {
            // 필수 오브젝트와 재질이 모두 준비된 경우에만 물리 지름길을 부분 생성한다.
            if (player == null
                || grayboxRoot == null
                || groundMaterial == null
                || accentMaterial == null)
            {
                return false;
            }

            bool changed = false;
            Transform parent = grayboxRoot.transform;
            PlayerWorldState worldState =
                player.GetComponent<PlayerWorldState>();
            if (worldState == null)
            {
                // 이전 씬에도 지름길 영구 ID를 소유할 단일 플레이어 상태를 보완한다.
                worldState = player.AddComponent<PlayerWorldState>();
                changed = true;
            }

            GameObject landing =
                GameObject.Find("Shortcut_ShaftLanding");
            if (landing == null)
            {
                // 샤프트 정상 보상 블록과 구역 경계 사이의 착지 공간을 만든다.
                landing = CreateBlock(
                    parent,
                    "Shortcut_ShaftLanding",
                    new Vector3(-9.6f, 6.25f, 0f),
                    new Vector3(1.8f, 0.5f, 3f),
                    groundMaterial);
                changed = true;
            }
            else if (SetTransformIfDifferent(
                landing.transform,
                new Vector3(-9.6f, 6.25f, 0f),
                new Vector3(1.8f, 0.5f, 3f)))
            {
                // 이전 배치를 설계 기준 위치로 옮기되 오브젝트 참조는 유지한다.
                changed = true;
            }

            GameObject bridge =
                GameObject.Find("Shortcut_ReturnBridge");
            if (bridge == null)
            {
                // 열린 뒤 시작 홀 상단으로 이동해 아래로 떨어지는 순환 귀환 다리를 만든다.
                bridge = CreateBlock(
                    parent,
                    "Shortcut_ReturnBridge",
                    new Vector3(-5.75f, 6.25f, 0f),
                    new Vector3(5.5f, 0.5f, 3f),
                    groundMaterial);
                changed = true;
            }
            else if (SetTransformIfDifferent(
                bridge.transform,
                new Vector3(-5.75f, 6.25f, 0f),
                new Vector3(5.5f, 0.5f, 3f)))
            {
                // 다리 크기를 결정적으로 유지해 구역 경계와 시작 홀 낙하 위치가 어긋나지 않게 한다.
                changed = true;
            }

            GameObject gateObject =
                GameObject.Find("ShortcutGate_ShaftReturn");
            if (gateObject == null)
            {
                // 시작 홀 쪽에서 먼저 통과할 수 없도록 경계 바로 오른쪽에 물리 게이트를 세운다.
                gateObject = CreateBlock(
                    parent,
                    "ShortcutGate_ShaftReturn",
                    new Vector3(-8.35f, 7.25f, 0f),
                    new Vector3(0.4f, 2f, 3f),
                    accentMaterial);
                changed = true;
            }
            else if (SetTransformIfDifferent(
                gateObject.transform,
                new Vector3(-8.35f, 7.25f, 0f),
                new Vector3(0.4f, 2f, 3f)))
            {
                // 기존 게이트 컴포넌트를 유지하면서 통로를 완전히 막는 위치와 크기로 복구한다.
                changed = true;
            }

            WorldShortcutGate gate =
                gateObject.GetComponent<WorldShortcutGate>();
            if (gate == null)
            {
                gate = gateObject.AddComponent<WorldShortcutGate>();
                changed = true;
            }

            if (gate.Configure(
                ShaftReturnShortcutId,
                worldState,
                gateObject.GetComponentInChildren<Renderer>()))
            {
                EditorUtility.SetDirty(gate);
                changed = true;
            }

            GameObject activatorObject =
                GameObject.Find("ShortcutActivator_ShaftTop");
            if (activatorObject == null)
            {
                // 게이트의 샤프트 쪽에만 접근 가능한 자동 활성 장치를 배치한다.
                activatorObject =
                    GameObject.CreatePrimitive(PrimitiveType.Sphere);
                activatorObject.name =
                    "ShortcutActivator_ShaftTop";
                activatorObject.transform.SetParent(parent);
                activatorObject.transform.position =
                    new Vector3(-9.4f, 7.1f, 0f);
                activatorObject.transform.localScale =
                    Vector3.one * 0.6f;
                activatorObject.GetComponent<MeshRenderer>()
                    .sharedMaterial = accentMaterial;
                changed = true;
            }
            else if (SetTransformIfDifferent(
                activatorObject.transform,
                new Vector3(-9.4f, 7.1f, 0f),
                Vector3.one * 0.6f))
            {
                // 활성 장치를 샤프트에서 올라온 플레이어의 착지 동선 안에 유지한다.
                changed = true;
            }

            Collider activatorCollider =
                activatorObject.GetComponent<Collider>();
            if (activatorCollider != null
                && !activatorCollider.isTrigger)
            {
                // 활성 장치는 접촉만 감지하고 플레이어의 상단 다리 이동을 막지 않는다.
                activatorCollider.isTrigger = true;
                EditorUtility.SetDirty(activatorCollider);
                changed = true;
            }

            ShortcutUnlockVolume activator =
                activatorObject.GetComponent<ShortcutUnlockVolume>();
            if (activator == null)
            {
                activator =
                    activatorObject.AddComponent<ShortcutUnlockVolume>();
                changed = true;
            }

            if (activator.Configure(
                gate,
                activatorObject.GetComponentInChildren<Renderer>()))
            {
                EditorUtility.SetDirty(activator);
                changed = true;
            }

            return changed;
        }

        private static bool EnsureWorldZonePrototype(
            GameObject player,
            GameObject grayboxRoot)
        {
            // 플레이어와 그레이박스가 모두 있을 때만 구역 상태와 물리 볼륨을 한 단위로 구성한다.
            if (player == null || grayboxRoot == null)
            {
                return false;
            }

            bool changed = false;
            PlayerWorldState worldState =
                player.GetComponent<PlayerWorldState>();
            if (worldState == null)
            {
                // 오래된 씬을 전체 재생성하지 않고 구역 방문 상태만 추가한다.
                player.AddComponent<PlayerWorldState>();
                changed = true;
            }

            WorldZoneDefinition backtrackShaft =
                GetOrCreateWorldZoneDefinition(
                    BacktrackShaftZonePath,
                    "backtrack_shaft",
                    "백트래킹 샤프트",
                    "벽 잡기 해금 후 시작 홀로 되돌아와 오르는 수직 구역.");
            WorldZoneDefinition startHall =
                GetOrCreateWorldZoneDefinition(
                    StartHallZonePath,
                    "start_hall",
                    "시작 홀",
                    "체크포인트와 첫 능력 단서를 제공하는 중앙 구역.");
            WorldZoneDefinition traversalLab =
                GetOrCreateWorldZoneDefinition(
                    TraversalLabZonePath,
                    "traversal_lab",
                    "이동 실험실",
                    "계단과 높은 발판에서 2단 점프와 공중 대시를 익히는 구역.");

            // 경계가 맞닿는 세 Trigger로 현재 Graybox를 수직·중앙·수평 구역으로 명시한다.
            changed |= EnsureWorldZoneVolume(
                grayboxRoot.transform,
                "Zone_BacktrackShaft",
                new Vector3(-10.75f, 4f, 0f),
                new Vector3(4.5f, 10f, 3.5f),
                backtrackShaft);
            changed |= EnsureWorldZoneVolume(
                grayboxRoot.transform,
                "Zone_StartHall",
                new Vector3(-1f, 4f, 0f),
                new Vector3(15f, 10f, 3.5f),
                startHall);
            changed |= EnsureWorldZoneVolume(
                grayboxRoot.transform,
                "Zone_TraversalLab",
                new Vector3(15.5f, 4f, 0f),
                new Vector3(18f, 10f, 3.5f),
                traversalLab);
            return changed;
        }

        private static bool EnsureWorldZoneVolume(
            Transform parent,
            string objectName,
            Vector3 position,
            Vector3 size,
            WorldZoneDefinition zone)
        {
            // 이름을 영구적인 씬 배치 키로 사용해 빌더 재실행 시 Trigger를 중복 생성하지 않는다.
            bool changed = false;
            GameObject volumeObject = GameObject.Find(objectName);
            if (volumeObject == null)
            {
                volumeObject = new GameObject(objectName);
                volumeObject.transform.SetParent(parent);
                changed = true;
            }

            if (SetTransformIfDifferent(
                volumeObject.transform,
                position,
                size))
            {
                changed = true;
            }

            BoxCollider trigger =
                volumeObject.GetComponent<BoxCollider>();
            if (trigger == null)
            {
                // 단위 크기 Collider에 Transform 크기를 적용해 Hierarchy에서도 구역 범위를 읽기 쉽게 한다.
                trigger = volumeObject.AddComponent<BoxCollider>();
                changed = true;
            }

            if (!trigger.isTrigger)
            {
                // 구역 경계는 기록만 하고 플레이어 이동을 물리적으로 막지 않는다.
                trigger.isTrigger = true;
                EditorUtility.SetDirty(trigger);
                changed = true;
            }

            WorldZoneVolume volume =
                volumeObject.GetComponent<WorldZoneVolume>();
            if (volume == null)
            {
                volume = volumeObject.AddComponent<WorldZoneVolume>();
                changed = true;
            }

            if (volume.Configure(zone))
            {
                EditorUtility.SetDirty(volume);
                changed = true;
            }

            return changed;
        }

        private static bool EnsureAbilityPrototype(
            GameObject player,
            GameObject grayboxRoot,
            Material abilityMaterial,
            Material groundMaterial)
        {
            // 진행 루프의 필수 씬 참조가 없으면 부분 배치로 씬을 깨뜨리지 않고 재시도를 기다린다.
            if (player == null
                || grayboxRoot == null
                || abilityMaterial == null
                || groundMaterial == null)
            {
                return false;
            }

            bool changed = false;
            PlayerAbilityState abilityState =
                player.GetComponent<PlayerAbilityState>();
            if (abilityState == null)
            {
                // 재생성하지 않은 오래된 씬에도 능력 보유 상태를 한 번만 추가한다.
                abilityState = player.AddComponent<PlayerAbilityState>();
                changed = true;
            }

            AbilityDefinition doubleJumpAbility =
                GetOrCreateAbilityDefinition(
                    DoubleJumpAbilityPath,
                    "double_jump",
                    "2단 점프",
                    "공중에서 한 번 더 점프한다.");
            AbilityDefinition airDashAbility =
                GetOrCreateAbilityDefinition(
                    AirDashAbilityPath,
                    "air_dash",
                    "공중 대시",
                    "공중에서 수평 대시를 한 번 사용한다.");
            AbilityDefinition wallTraversalAbility =
                GetOrCreateAbilityDefinition(
                    WallTraversalAbilityPath,
                    "wall_traversal",
                    "벽 잡기",
                    "벽에 잠시 붙고 미끄러지며 반대편으로 점프한다.");

            SideScrollerMotor motor =
                player.GetComponent<SideScrollerMotor>();
            if (motor != null
                && motor.ConfigureAbilityRequirements(
                    abilityState,
                    doubleJumpAbility,
                    airDashAbility,
                    wallTraversalAbility))
            {
                // 이동 능력의 실제 사용 조건을 진행 상태와 연결하고 씬 직렬화 대상으로 표시한다.
                EditorUtility.SetDirty(motor);
                changed = true;
            }

            if (motor != null
                && motor.ConfigureDashTiming(
                    DashMovementDuration,
                    DashCooldownDuration,
                    DashInvulnerabilityDuration))
            {
                // 대시 이동보다 0.1초 긴 무적 시간을 저장해 교차 회피의 입력 오차를 완화한다.
                EditorUtility.SetDirty(motor);
                changed = true;
            }

            if (GameObject.Find("AbilityPickup_DoubleJump") == null)
            {
                // 위험 지대를 통과한 뒤 첫 능력을 얻도록 시작 지점 오른쪽에 배치한다.
                CreateAbilityPickup(
                    grayboxRoot.transform,
                    "AbilityPickup_DoubleJump",
                    new Vector3(7f, 1f, 0f),
                    doubleJumpAbility,
                    abilityMaterial);
                changed = true;
            }

            if (GameObject.Find("AbilityPickup_AirDash") == null)
            {
                // 2단 점프로 계단과 높은 발판을 오른 뒤 다음 공중 능력을 얻게 한다.
                CreateAbilityPickup(
                    grayboxRoot.transform,
                    "AbilityPickup_AirDash",
                    new Vector3(20f, 4.2f, 0f),
                    airDashAbility,
                    abilityMaterial);
                changed = true;
            }

            if (GameObject.Find("AbilityPickup_WallTraversal") == null)
            {
                // 공중 대시 게이트 뒤에서 세 번째 벽 이동 능력을 획득하게 한다.
                CreateAbilityPickup(
                    grayboxRoot.transform,
                    "AbilityPickup_WallTraversal",
                    new Vector3(22.6f, 4.2f, 0f),
                    wallTraversalAbility,
                    abilityMaterial);
                changed = true;
            }

            GameObject gateObject = GameObject.Find("Wall_Gate");
            if (EnsureAbilityGate(
                gateObject,
                doubleJumpAbility,
                abilityState))
            {
                changed = true;
            }

            GameObject airDashGate =
                GameObject.Find("AirDash_Gate");
            if (airDashGate == null)
            {
                // 높은 발판에서 공중 대시를 얻은 뒤에만 세 번째 픽업으로 이동할 수 있게 막는다.
                airDashGate = CreateBlock(
                    grayboxRoot.transform,
                    "AirDash_Gate",
                    new Vector3(21.25f, 4.8f, 0f),
                    new Vector3(0.5f, 2.6f, 3f),
                    abilityMaterial);
                changed = true;
            }

            if (EnsureAbilityGate(
                airDashGate,
                airDashAbility,
                abilityState))
            {
                changed = true;
            }

            if (EnsureWallTraversalCourse(
                grayboxRoot.transform,
                groundMaterial,
                abilityMaterial,
                wallTraversalAbility,
                player.GetComponent<PlayerWorldState>(),
                abilityState,
                player.GetComponent<Health>()))
            {
                changed = true;
            }

            return changed;
        }

        private static bool EnsureAbilityGate(
            GameObject gateObject,
            AbilityDefinition requiredAbility,
            PlayerAbilityState abilityState)
        {
            // 물리 블록이 없으면 게이트 컴포넌트만 생성하지 않고 호출자가 배치를 보완하게 한다.
            if (gateObject == null)
            {
                return false;
            }

            bool changed = false;
            AbilityGate gate =
                gateObject.GetComponent<AbilityGate>();
            if (gate == null)
            {
                // 기존 Collider 블록을 능력 게이트로 승격해 충돌 표현을 중복 생성하지 않는다.
                gate = gateObject.AddComponent<AbilityGate>();
                changed = true;
            }

            if (gate.Configure(
                requiredAbility,
                abilityState,
                gateObject.GetComponentInChildren<Renderer>()))
            {
                EditorUtility.SetDirty(gate);
                changed = true;
            }

            return changed;
        }

        private static bool EnsureWallTraversalCourse(
            Transform parent,
            Material groundMaterial,
            Material rewardMaterial,
            AbilityDefinition requiredAbility,
            PlayerWorldState worldState,
            PlayerAbilityState abilityState,
            Health playerHealth)
        {
            // 오른쪽 아래로 진입한 뒤 두 벽 사이를 번갈아 점프하는 백트래킹 샤프트를 만든다.
            bool changed = false;
            if (GameObject.Find("WallTraversal_Left") == null)
            {
                CreateBlock(
                    parent,
                    "WallTraversal_Left",
                    new Vector3(-11f, 3f, 0f),
                    new Vector3(0.6f, 6f, 3f),
                    groundMaterial);
                changed = true;
            }

            if (GameObject.Find("WallTraversal_RightUpper") == null)
            {
                // 오른쪽 벽 아래에 2m 진입 공간을 남겨 능력 획득 전에도 샤프트 입구를 볼 수 있게 한다.
                CreateBlock(
                    parent,
                    "WallTraversal_RightUpper",
                    new Vector3(-8.5f, 4.25f, 0f),
                    new Vector3(0.6f, 4.5f, 3f),
                    groundMaterial);
                changed = true;
            }

            GameObject reward = GameObject.Find("Backtrack_Reward");
            if (reward == null
                || reward.GetComponent<CapsuleCollider>() == null)
            {
                // 이전 표식을 제거하고 지름길 구체와 모양이 다른 세로형 체력 조각으로 승격한다.
                if (reward != null)
                {
                    Object.DestroyImmediate(reward);
                }

                reward =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Capsule);
                reward.name = "Backtrack_Reward";
                reward.transform.SetParent(parent);
                changed = true;
            }

            if (SetTransformIfDifferent(
                reward.transform,
                new Vector3(-10.25f, 7.15f, 0f),
                new Vector3(0.42f, 0.58f, 0.42f)))
            {
                // 지름길 활성 장치와 겹치지 않는 샤프트 정상 왼쪽에 보상을 유지한다.
                changed = true;
            }

            MeshRenderer rewardRenderer =
                reward.GetComponent<MeshRenderer>();
            if (rewardRenderer != null
                && rewardRenderer.sharedMaterial
                    != rewardMaterial)
            {
                // 보상은 능력 구체와 같은 강조 재질을 사용해 획득 가능 오브젝트임을 알린다.
                rewardRenderer.sharedMaterial =
                    rewardMaterial;
                EditorUtility.SetDirty(rewardRenderer);
                changed = true;
            }

            Collider rewardTrigger =
                reward.GetComponent<Collider>();
            if (rewardTrigger != null
                && !rewardTrigger.isTrigger)
            {
                // 체력 조각은 접촉만 감지하고 샤프트 정상 이동을 물리적으로 막지 않는다.
                rewardTrigger.isTrigger = true;
                EditorUtility.SetDirty(rewardTrigger);
                changed = true;
            }

            BacktrackRewardPickup rewardPickup =
                reward.GetComponent<BacktrackRewardPickup>();
            if (rewardPickup == null)
            {
                rewardPickup =
                    reward.AddComponent<BacktrackRewardPickup>();
                changed = true;
            }

            if (rewardPickup.Configure(
                ShaftHealthRewardId,
                1,
                requiredAbility,
                worldState,
                abilityState,
                playerHealth,
                rewardRenderer))
            {
                // ID·요구 능력·효과 대상 연결이 바뀐 경우에만 씬 직렬화를 갱신한다.
                EditorUtility.SetDirty(rewardPickup);
                changed = true;
            }

            return changed;
        }

        private static bool SetTransformIfDifferent(
            Transform target,
            Vector3 position,
            Vector3 scale)
        {
            // 허용 오차 안에서 같은 배치면 씬을 Dirty 상태로 만들지 않는다.
            if ((target.position - position).sqrMagnitude < 0.000001f
                && (target.localScale - scale).sqrMagnitude < 0.000001f)
            {
                return false;
            }

            target.position = position;
            target.localScale = scale;
            EditorUtility.SetDirty(target);
            return true;
        }

        private static GameObject CreateAbilityPickup(
            Transform parent,
            string objectName,
            Vector3 position,
            AbilityDefinition ability,
            Material material)
        {
            // 단순한 구체 프리미티브로 획득 Trigger와 시각 표현을 한눈에 검증하게 한다.
            GameObject pickup =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickup.name = objectName;
            pickup.transform.SetParent(parent);
            pickup.transform.position = position;
            pickup.transform.localScale = Vector3.one * 0.7f;
            MeshRenderer renderer = pickup.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            SphereCollider trigger = pickup.GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            AbilityPickup pickupComponent =
                pickup.AddComponent<AbilityPickup>();
            pickupComponent.Configure(ability, renderer);
            return pickup;
        }

        private static AbilityDefinition GetOrCreateAbilityDefinition(
            string path,
            string id,
            string displayName,
            string description)
        {
            // 능력 에셋 폴더가 없는 최초 실행에서도 결정적인 경로를 먼저 준비한다.
            EnsureAbilityFolder();
            AbilityDefinition ability =
                AssetDatabase.LoadAssetAtPath<AbilityDefinition>(path);
            if (ability == null)
            {
                // 에셋은 한 번만 만들고 이후 빌더 실행에서는 같은 GUID를 재사용한다.
                ability =
                    ScriptableObject.CreateInstance<AbilityDefinition>();
                ability.Configure(id, displayName, description);
                AssetDatabase.CreateAsset(ability, path);
                return ability;
            }

            // 표시 문구를 코드 기준과 동기화하되 기존 에셋 참조와 GUID는 보존한다.
            ability.Configure(id, displayName, description);
            EditorUtility.SetDirty(ability);
            return ability;
        }

        private static void EnsureAbilityFolder()
        {
            // AssetDatabase 폴더 API를 사용해야 Unity가 폴더 메타 GUID를 안정적으로 관리한다.
            if (!AssetDatabase.IsValidFolder(AbilityFolderPath))
            {
                AssetDatabase.CreateFolder(
                    "Assets/Settings",
                    "Abilities");
            }
        }

        private static WorldZoneDefinition GetOrCreateWorldZoneDefinition(
            string path,
            string id,
            string displayName,
            string description)
        {
            // 구역 에셋 폴더를 먼저 준비해 모든 영구 ID 정의가 결정적인 경로를 갖게 한다.
            EnsureWorldZoneFolder();
            WorldZoneDefinition zone =
                AssetDatabase.LoadAssetAtPath<WorldZoneDefinition>(path);
            if (zone == null)
            {
                // 최초 생성 뒤에는 같은 에셋 GUID를 유지해 지도와 씬 참조가 끊기지 않게 한다.
                zone =
                    ScriptableObject.CreateInstance<WorldZoneDefinition>();
                zone.Configure(id, displayName, description);
                AssetDatabase.CreateAsset(zone, path);
                return zone;
            }

            // 문서 기준 표시 정보를 동기화하면서 기존 에셋과 씬 참조는 보존한다.
            zone.Configure(id, displayName, description);
            EditorUtility.SetDirty(zone);
            return zone;
        }

        private static void EnsureWorldZoneFolder()
        {
            // AssetDatabase API로 폴더를 생성해 Unity 메타 GUID를 안정적으로 관리한다.
            if (!AssetDatabase.IsValidFolder(WorldZoneFolderPath))
            {
                AssetDatabase.CreateFolder(
                    "Assets/Settings",
                    "WorldZones");
            }
        }

        private static GameObject CreateTrainingDummy(
            Transform parent,
            Material material)
        {
            // 공격 시연을 위한 보이고 충돌하는 Health 대상을 만든다.
            GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummy.name = "TrainingDummy";
            dummy.layer =
                CharacterBodyCollisionPolicy.EnemyBodyLayer;
            dummy.transform.SetParent(parent);
            dummy.transform.position = new Vector3(10f, 2f, 0f);
            dummy.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
            dummy.GetComponent<MeshRenderer>().sharedMaterial = material;

            Health health = dummy.AddComponent<Health>();
            health.Configure(3);
            dummy.AddComponent<TrainingDummy>();
            return dummy;
        }

        private static bool EnsureMeleeEnemyPrototype(
            GameObject player,
            GameObject parent,
            Material material)
        {
            // 전투 대상·배치 부모·표시 재질이 모두 준비된 경우에만 실제 적을 구성한다.
            if (player == null || parent == null || material == null)
            {
                return false;
            }

            bool changed = false;
            GameObject enemy = GameObject.Find("MeleeEnemy_Grunt");
            if (enemy == null)
            {
                // 발 기준 이동 루트와 중심 기준 메시를 분리하기 위해 빈 루트를 먼저 만든다.
                enemy = new GameObject("MeleeEnemy_Grunt");
                enemy.name = "MeleeEnemy_Grunt";
                enemy.transform.SetParent(parent.transform);
                changed = true;
            }

            if (enemy.layer
                != CharacterBodyCollisionPolicy.EnemyBodyLayer)
            {
                // 적 이동 루트의 CharacterController만 EnemyBody로 분류해 시각·공격 자식은 자유롭게 분리한다.
                enemy.layer =
                    CharacterBodyCollisionPolicy.EnemyBodyLayer;
                EditorUtility.SetDirty(enemy);
                changed = true;
            }

            if (SetTransformIfDifferent(
                enemy.transform,
                new Vector3(13.8f, 3.05f, 0f),
                Vector3.one))
            {
                changed = true;
            }

            MeshRenderer oldRootRenderer =
                enemy.GetComponent<MeshRenderer>();
            if (oldRootRenderer != null)
            {
                // 이전 버전의 중심 기준 메시를 제거해 절반이 지면 아래로 묻히는 배치를 마이그레이션한다.
                Object.DestroyImmediate(oldRootRenderer);
                changed = true;
            }

            MeshFilter oldRootMesh =
                enemy.GetComponent<MeshFilter>();
            if (oldRootMesh != null)
            {
                // 렌더러와 함께 남은 루트 MeshFilter도 제거해 루트를 순수 이동 기준점으로 유지한다.
                Object.DestroyImmediate(oldRootMesh);
                changed = true;
            }

            CapsuleCollider primitiveCollider =
                enemy.GetComponent<CapsuleCollider>();
            if (primitiveCollider != null)
            {
                // 중복 캡슐 충돌체는 CharacterController 이동과 접촉 판정을 흔들 수 있어 제거한다.
                Object.DestroyImmediate(primitiveCollider);
                changed = true;
            }

            Transform visualTransform =
                enemy.transform.Find("MeleeEnemy_Visual");
            if (visualTransform == null)
            {
                // 캡슐 중심을 발보다 0.9m 위에 둬 CharacterController와 같은 높이를 차지하게 한다.
                GameObject visual =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Capsule);
                visual.name = "MeleeEnemy_Visual";
                visual.transform.SetParent(
                    enemy.transform,
                    false);
                Object.DestroyImmediate(
                    visual.GetComponent<CapsuleCollider>());
                visualTransform = visual.transform;
                changed = true;
            }

            Vector3 visualLocalPosition =
                new(0f, 0.9f, 0f);
            Vector3 visualLocalScale =
                new(0.75f, 0.9f, 0.75f);
            if ((visualTransform.localPosition
                    - visualLocalPosition).sqrMagnitude
                    > 0.000001f
                || (visualTransform.localScale
                    - visualLocalScale).sqrMagnitude
                    > 0.000001f)
            {
                // 메시 바닥과 충돌체 바닥을 같은 Y=0에 맞추되 사람형에 가까운 비율을 유지한다.
                visualTransform.localPosition =
                    visualLocalPosition;
                visualTransform.localRotation =
                    Quaternion.identity;
                visualTransform.localScale =
                    visualLocalScale;
                EditorUtility.SetDirty(visualTransform);
                changed = true;
            }

            MeshRenderer renderer =
                visualTransform.GetComponent<MeshRenderer>();
            if (renderer != null
                && renderer.sharedMaterial != material)
            {
                // 첫 적은 포트폴리오 그레이박스의 강조색을 사용해 연습용 더미와 구분한다.
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
                changed = true;
            }

            Transform indicatorTransform =
                enemy.transform.Find(
                    "MeleeEnemy_AttackIndicator");
            if (indicatorTransform == null)
            {
                // 실제 VFX 전에는 작은 구체로 선딜·적중·무적 결과를 시각화한다.
                GameObject indicator =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Sphere);
                indicator.name =
                    "MeleeEnemy_AttackIndicator";
                indicator.transform.SetParent(
                    enemy.transform,
                    false);
                Object.DestroyImmediate(
                    indicator.GetComponent<SphereCollider>());
                indicatorTransform =
                    indicator.transform;
                changed = true;
            }

            Vector3 indicatorLocalPosition =
                new(-1.15f, 0.9f, 0f);
            Vector3 indicatorLocalScale =
                Vector3.one * 0.65f;
            if ((indicatorTransform.localPosition
                    - indicatorLocalPosition).sqrMagnitude
                    > 0.000001f
                || (indicatorTransform.localScale
                    - indicatorLocalScale).sqrMagnitude
                    > 0.000001f)
            {
                // 표시 구체를 기본 왼쪽 공격 위치에 두고 런타임에는 바라보는 방향으로 반전한다.
                indicatorTransform.localPosition =
                    indicatorLocalPosition;
                indicatorTransform.localRotation =
                    Quaternion.identity;
                indicatorTransform.localScale =
                    indicatorLocalScale;
                EditorUtility.SetDirty(indicatorTransform);
                changed = true;
            }

            MeshRenderer indicatorRenderer =
                indicatorTransform.GetComponent<MeshRenderer>();
            if (indicatorRenderer != null)
            {
                if (indicatorRenderer.sharedMaterial
                    != material)
                {
                    indicatorRenderer.sharedMaterial =
                        material;
                    changed = true;
                }

                // Play 전에는 숨기고 상태 머신이 선딜이나 결과가 있을 때만 켠다.
                if (indicatorRenderer.enabled)
                {
                    indicatorRenderer.enabled = false;
                    changed = true;
                }

                EditorUtility.SetDirty(
                    indicatorRenderer);
            }

            CharacterController controller =
                enemy.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller =
                    enemy.AddComponent<CharacterController>();
                changed = true;
            }

            if (controller.center != new Vector3(0f, 0.9f, 0f)
                || !Mathf.Approximately(controller.height, 1.8f)
                || !Mathf.Approximately(controller.radius, 0.35f)
                || !Mathf.Approximately(controller.slopeLimit, 45f)
                || !Mathf.Approximately(controller.stepOffset, 0.3f))
            {
                // 플레이어와 같은 계단·경사 기준을 사용해 추적 이동의 비교 조건을 통일한다.
                controller.center = new Vector3(0f, 0.9f, 0f);
                controller.height = 1.8f;
                controller.radius = 0.35f;
                controller.slopeLimit = 45f;
                controller.stepOffset = 0.3f;
                EditorUtility.SetDirty(controller);
                changed = true;
            }

            Health health = enemy.GetComponent<Health>();
            if (health == null)
            {
                health = enemy.AddComponent<Health>();
                health.Configure(3);
                changed = true;
            }

            MeleeEnemyController enemyController =
                enemy.GetComponent<MeleeEnemyController>();
            if (enemyController == null)
            {
                enemyController =
                    enemy.AddComponent<MeleeEnemyController>();
                changed = true;
            }

            if (enemyController.Configure(
                player.transform,
                renderer,
                indicatorRenderer))
            {
                // 씬 참조 변경만 Dirty로 표시해 멱등 빌더가 매 실행마다 씬을 수정하지 않게 한다.
                EditorUtility.SetDirty(enemyController);
                changed = true;
            }

            if (enemyController.ConfigureAttackTiming(
                MeleeAttackWindupDuration,
                MeleeAttackRecoveryDuration))
            {
                // 읽기 쉬운 선딜과 반격 가능한 후딜이 바뀐 경우 씬 직렬화 대상으로 표시한다.
                EditorUtility.SetDirty(enemyController);
                changed = true;
            }

            return changed;
        }

        private static bool EnsureRangedEnemyPrototype(
            GameObject player,
            GameObject parent,
            Material material)
        {
            // 플레이어·배치 루트·공유 재질이 준비된 경우에만 원거리 전투 대상을 구성한다.
            if (player == null || parent == null || material == null)
            {
                return false;
            }

            bool changed = false;
            GameObject enemy =
                GameObject.Find("RangedEnemy_Sentry");
            if (enemy == null)
            {
                // 발 위치를 나타내는 빈 루트에 몸 Collider와 시각 자식을 분리해 조립한다.
                enemy =
                    new GameObject("RangedEnemy_Sentry");
                enemy.transform.SetParent(parent.transform);
                changed = true;
            }

            if (enemy.layer
                != CharacterBodyCollisionPolicy.EnemyBodyLayer)
            {
                // 플레이어와 다른 적을 밀지 않으면서 공격 조회에는 남도록 EnemyBody를 사용한다.
                enemy.layer =
                    CharacterBodyCollisionPolicy.EnemyBodyLayer;
                EditorUtility.SetDirty(enemy);
                changed = true;
            }

            if (SetTransformIfDifferent(
                enemy.transform,
                new Vector3(18f, 3.55f, 0f),
                Vector3.one))
            {
                changed = true;
            }

            CapsuleCollider bodyCollider =
                enemy.GetComponent<CapsuleCollider>();
            if (bodyCollider == null)
            {
                bodyCollider =
                    enemy.AddComponent<CapsuleCollider>();
                changed = true;
            }

            if (bodyCollider.center
                    != new Vector3(0f, 0.9f, 0f)
                || !Mathf.Approximately(
                    bodyCollider.height,
                    1.8f)
                || !Mathf.Approximately(
                    bodyCollider.radius,
                    0.35f)
                || bodyCollider.isTrigger)
            {
                // 몸 Collider는 발 기준 루트 위에 놓고 공격 Overlap이 찾을 수 있는 일반 Collider로 유지한다.
                bodyCollider.center =
                    new Vector3(0f, 0.9f, 0f);
                bodyCollider.height = 1.8f;
                bodyCollider.radius = 0.35f;
                bodyCollider.isTrigger = false;
                EditorUtility.SetDirty(bodyCollider);
                changed = true;
            }

            Transform visualTransform =
                enemy.transform.Find("RangedEnemy_Visual");
            if (visualTransform == null)
            {
                // 보라색 상태 표현을 받을 임시 캡슐 메시를 별도 자식으로 만든다.
                GameObject visual =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Capsule);
                visual.name = "RangedEnemy_Visual";
                visual.transform.SetParent(
                    enemy.transform,
                    false);
                Object.DestroyImmediate(
                    visual.GetComponent<CapsuleCollider>());
                visualTransform = visual.transform;
                changed = true;
            }

            Vector3 visualLocalPosition =
                new(0f, 0.9f, 0f);
            Vector3 visualLocalScale =
                new(0.65f, 0.9f, 0.65f);
            if ((visualTransform.localPosition
                    - visualLocalPosition).sqrMagnitude
                    > 0.000001f
                || (visualTransform.localScale
                    - visualLocalScale).sqrMagnitude
                    > 0.000001f)
            {
                // 메시의 바닥을 루트 Y=0에 맞춰 발판 안으로 묻히지 않게 한다.
                visualTransform.localPosition =
                    visualLocalPosition;
                visualTransform.localRotation =
                    Quaternion.identity;
                visualTransform.localScale =
                    visualLocalScale;
                EditorUtility.SetDirty(visualTransform);
                changed = true;
            }

            MeshRenderer visualRenderer =
                visualTransform.GetComponent<MeshRenderer>();
            if (visualRenderer != null
                && visualRenderer.sharedMaterial != material)
            {
                visualRenderer.sharedMaterial = material;
                EditorUtility.SetDirty(visualRenderer);
                changed = true;
            }

            Transform muzzleTransform =
                enemy.transform.Find("RangedEnemy_Muzzle");
            if (muzzleTransform == null)
            {
                // 선딜 동안만 나타나는 작은 구체를 발사 원점과 충전 표시로 함께 사용한다.
                GameObject muzzle =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Sphere);
                muzzle.name = "RangedEnemy_Muzzle";
                muzzle.transform.SetParent(
                    enemy.transform,
                    false);
                Object.DestroyImmediate(
                    muzzle.GetComponent<SphereCollider>());
                muzzleTransform = muzzle.transform;
                changed = true;
            }

            Vector3 muzzleLocalPosition =
                new(-0.85f, 1f, 0f);
            Vector3 muzzleLocalScale =
                Vector3.one * 0.28f;
            if ((muzzleTransform.localPosition
                    - muzzleLocalPosition).sqrMagnitude
                    > 0.000001f
                || (muzzleTransform.localScale
                    - muzzleLocalScale).sqrMagnitude
                    > 0.000001f)
            {
                // 기본 왼쪽 위치는 런타임에 바라보는 방향에 따라 좌우 반전된다.
                muzzleTransform.localPosition =
                    muzzleLocalPosition;
                muzzleTransform.localRotation =
                    Quaternion.identity;
                muzzleTransform.localScale =
                    muzzleLocalScale;
                EditorUtility.SetDirty(muzzleTransform);
                changed = true;
            }

            MeshRenderer muzzleRenderer =
                muzzleTransform.GetComponent<MeshRenderer>();
            if (muzzleRenderer != null)
            {
                if (muzzleRenderer.sharedMaterial != material)
                {
                    muzzleRenderer.sharedMaterial = material;
                    changed = true;
                }

                // Play 전에는 숨기고 RangedEnemyController가 선딜 상태에서만 표시한다.
                if (muzzleRenderer.enabled)
                {
                    muzzleRenderer.enabled = false;
                    changed = true;
                }

                EditorUtility.SetDirty(muzzleRenderer);
            }

            Health health = enemy.GetComponent<Health>();
            if (health == null)
            {
                health = enemy.AddComponent<Health>();
                health.Configure(3);
                changed = true;
            }

            RangedEnemyController controller =
                enemy.GetComponent<RangedEnemyController>();
            if (controller == null)
            {
                controller =
                    enemy.AddComponent<RangedEnemyController>();
                changed = true;
            }

            if (controller.Configure(
                player.transform,
                visualRenderer,
                muzzleRenderer,
                muzzleTransform,
                material))
            {
                // 직렬화 참조가 달라진 경우에만 멱등 마이그레이션 결과를 저장한다.
                EditorUtility.SetDirty(controller);
                changed = true;
            }

            if (controller.ConfigureAttackTiming(
                RangedAttackWindupDuration,
                RangedAttackRecoveryDuration))
            {
                // 연속 탄막이 되지 않도록 늘린 충전과 발사 후 간격을 씬에 저장한다.
                EditorUtility.SetDirty(controller);
                changed = true;
            }

            return changed;
        }

        private static bool EnsureChargeEnemyPrototype(
            GameObject player,
            GameObject parent,
            Material material)
        {
            // 플레이어·배치 루트·공유 재질이 준비된 경우에만 돌진 전투 대상을 구성한다.
            if (player == null || parent == null || material == null)
            {
                return false;
            }

            bool changed = false;
            GameObject enemy =
                GameObject.Find("ChargeEnemy_Rusher");
            if (enemy == null)
            {
                // 발 위치 기준 루트에 이동 충돌체와 시각 자식을 분리해 조립한다.
                enemy =
                    new GameObject("ChargeEnemy_Rusher");
                enemy.transform.SetParent(parent.transform);
                changed = true;
            }

            if (enemy.layer
                != CharacterBodyCollisionPolicy.EnemyBodyLayer)
            {
                // 플레이어와 다른 적의 몸을 밀지 않되 공격 조회 대상에는 남도록 EnemyBody를 사용한다.
                enemy.layer =
                    CharacterBodyCollisionPolicy.EnemyBodyLayer;
                EditorUtility.SetDirty(enemy);
                changed = true;
            }

            if (SetTransformIfDifferent(
                enemy.transform,
                new Vector3(22.2f, 3.55f, 0f),
                Vector3.one))
            {
                changed = true;
            }

            CapsuleCollider primitiveCollider =
                enemy.GetComponent<CapsuleCollider>();
            if (primitiveCollider != null)
            {
                // 원시 오브젝트에서 남은 Collider가 CharacterController와 겹치면 발판 끝 검사가 흔들리므로 제거한다.
                Object.DestroyImmediate(primitiveCollider);
                changed = true;
            }

            CharacterController bodyController =
                enemy.GetComponent<CharacterController>();
            if (bodyController == null)
            {
                bodyController =
                    enemy.AddComponent<CharacterController>();
                changed = true;
            }

            if (bodyController.center
                    != new Vector3(0f, 0.65f, 0f)
                || !Mathf.Approximately(
                    bodyController.height,
                    1.3f)
                || !Mathf.Approximately(
                    bodyController.radius,
                    0.42f)
                || !Mathf.Approximately(
                    bodyController.slopeLimit,
                    45f)
                || !Mathf.Approximately(
                    bodyController.stepOffset,
                    0.25f))
            {
                // 낮고 넓은 몸체를 사용해 사람형 근거리 적과 돌진 실루엣을 구분한다.
                bodyController.center =
                    new Vector3(0f, 0.65f, 0f);
                bodyController.height = 1.3f;
                bodyController.radius = 0.42f;
                bodyController.slopeLimit = 45f;
                bodyController.stepOffset = 0.25f;
                EditorUtility.SetDirty(bodyController);
                changed = true;
            }

            Transform visualTransform =
                enemy.transform.Find("ChargeEnemy_Visual");
            if (visualTransform == null)
            {
                // 실제 모델 전에는 넓은 큐브로 빠른 지상형 적의 역할을 표현한다.
                GameObject visual =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cube);
                visual.name = "ChargeEnemy_Visual";
                visual.transform.SetParent(
                    enemy.transform,
                    false);
                Object.DestroyImmediate(
                    visual.GetComponent<BoxCollider>());
                visualTransform = visual.transform;
                changed = true;
            }

            Vector3 visualLocalPosition =
                new(0f, 0.65f, 0f);
            Vector3 visualLocalScale =
                new(1.05f, 1.15f, 0.85f);
            if ((visualTransform.localPosition
                    - visualLocalPosition).sqrMagnitude
                    > 0.000001f
                || (visualTransform.localScale
                    - visualLocalScale).sqrMagnitude
                    > 0.000001f)
            {
                // 메시 바닥을 발 기준 루트에 맞추고 진행 방향이 읽히는 가로 비율을 사용한다.
                visualTransform.localPosition =
                    visualLocalPosition;
                visualTransform.localRotation =
                    Quaternion.identity;
                visualTransform.localScale =
                    visualLocalScale;
                EditorUtility.SetDirty(visualTransform);
                changed = true;
            }

            MeshRenderer visualRenderer =
                visualTransform.GetComponent<MeshRenderer>();
            if (visualRenderer != null
                && visualRenderer.sharedMaterial != material)
            {
                // 공유 재질은 유지하고 런타임 상태 색만 MaterialPropertyBlock으로 덮어쓴다.
                visualRenderer.sharedMaterial = material;
                EditorUtility.SetDirty(visualRenderer);
                changed = true;
            }

            Transform indicatorTransform =
                enemy.transform.Find(
                    "ChargeEnemy_DirectionIndicator");
            if (indicatorTransform == null)
            {
                // 몸 앞의 얇은 막대를 선딜 방향과 돌진 활성 표시로 재사용한다.
                GameObject indicator =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cube);
                indicator.name =
                    "ChargeEnemy_DirectionIndicator";
                indicator.transform.SetParent(
                    enemy.transform,
                    false);
                Object.DestroyImmediate(
                    indicator.GetComponent<BoxCollider>());
                indicatorTransform = indicator.transform;
                changed = true;
            }

            Vector3 indicatorLocalPosition =
                new(-0.95f, 0.65f, 0f);
            Vector3 indicatorLocalScale =
                new(0.65f, 0.16f, 0.9f);
            if ((indicatorTransform.localPosition
                    - indicatorLocalPosition).sqrMagnitude
                    > 0.000001f
                || (indicatorTransform.localScale
                    - indicatorLocalScale).sqrMagnitude
                    > 0.000001f)
            {
                // 기본 왼쪽 표시 위치는 런타임에 잠긴 돌진 방향에 따라 좌우 반전된다.
                indicatorTransform.localPosition =
                    indicatorLocalPosition;
                indicatorTransform.localRotation =
                    Quaternion.identity;
                indicatorTransform.localScale =
                    indicatorLocalScale;
                EditorUtility.SetDirty(indicatorTransform);
                changed = true;
            }

            MeshRenderer indicatorRenderer =
                indicatorTransform.GetComponent<MeshRenderer>();
            if (indicatorRenderer != null)
            {
                if (indicatorRenderer.sharedMaterial != material)
                {
                    indicatorRenderer.sharedMaterial =
                        material;
                    changed = true;
                }

                // Play 전에는 숨기고 돌진 상태 머신이 예고와 실행 중에만 표시한다.
                if (indicatorRenderer.enabled)
                {
                    indicatorRenderer.enabled = false;
                    changed = true;
                }

                EditorUtility.SetDirty(indicatorRenderer);
            }

            Health health = enemy.GetComponent<Health>();
            if (health == null)
            {
                health = enemy.AddComponent<Health>();
                health.Configure(4);
                changed = true;
            }
            else if (health.MaxHealth != 4)
            {
                // 세 번째 적은 공격 중단을 여러 번 시도할 수 있도록 일반 적보다 체력을 한 칸 늘린다.
                health.Configure(4);
                EditorUtility.SetDirty(health);
                changed = true;
            }

            ChargeEnemyController controller =
                enemy.GetComponent<ChargeEnemyController>();
            if (controller == null)
            {
                controller =
                    enemy.AddComponent<ChargeEnemyController>();
                changed = true;
            }

            if (controller.Configure(
                player.transform,
                visualRenderer,
                indicatorRenderer))
            {
                // 직렬화 참조가 달라진 경우에만 멱등 마이그레이션 결과를 저장한다.
                EditorUtility.SetDirty(controller);
                changed = true;
            }

            return changed;
        }

        private static GameObject CreateCheckpoint(
            Transform parent,
            Material material)
        {
            // 하나의 큐브를 시각 기둥과 Trigger로 함께 사용해 체크포인트 흐름을 빠르게 시연한다.
            GameObject checkpoint =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            checkpoint.name = "Checkpoint_Start";
            checkpoint.transform.SetParent(parent);
            checkpoint.transform.position = new Vector3(1.25f, 1f, 0f);
            checkpoint.transform.localScale = new Vector3(0.4f, 2f, 0.4f);
            checkpoint.GetComponent<MeshRenderer>().sharedMaterial = material;

            BoxCollider trigger = checkpoint.GetComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(3.25f, 1f, 5f);

            Checkpoint checkpointComponent =
                checkpoint.AddComponent<Checkpoint>();
            checkpointComponent.Configure(
                "start_hall",
                new Vector3(0f, -0.95f, 0f),
                checkpoint.GetComponent<MeshRenderer>());
            return checkpoint;
        }

        private static GameObject CreateRespawnHazard(
            Transform parent,
            Material material)
        {
            // 체크포인트 뒤에 대시로 통과하거나 접촉해 재시작을 검증할 위험 지대를 만든다.
            GameObject hazard =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            hazard.name = "RespawnHazard";
            hazard.transform.SetParent(parent);
            hazard.transform.position = new Vector3(5.5f, 0.15f, 0f);
            hazard.transform.localScale = new Vector3(0.6f, 0.3f, 2.2f);
            hazard.GetComponent<MeshRenderer>().sharedMaterial = material;

            BoxCollider trigger = hazard.GetComponent<BoxCollider>();
            trigger.isTrigger = true;

            DamageVolume damageVolume = hazard.AddComponent<DamageVolume>();
            damageVolume.Configure(99);
            return hazard;
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

            // 같은 색을 다시 대입하면 URP의 호환 색 속성이 바뀌어 불필요한 에셋 diff가 생기므로 건너뛴다.
            if (!ColorsApproximately(
                material.color,
                color))
            {
                material.color = color;
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static bool ColorsApproximately(
            Color first,
            Color second)
        {
            // 색 공간 변환의 미세한 부동소수점 차이를 무시해 같은 URP 색상이 반복 저장되지 않게 한다.
            return Mathf.Approximately(first.r, second.r)
                && Mathf.Approximately(first.g, second.g)
                && Mathf.Approximately(first.b, second.b)
                && Mathf.Approximately(first.a, second.a);
        }

        private static void RemoveExistingPrototypeRoots()
        {
            Object.DestroyImmediate(GameObject.Find("Player"));
            Object.DestroyImmediate(GameObject.Find("Graybox"));
            Object.DestroyImmediate(GameObject.Find(GrayboxRootName));
        }
    }
}
