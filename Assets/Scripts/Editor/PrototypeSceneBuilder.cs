// GOLDEN STANDARD
// 목적: 가장 작은 플레이 가능한 2.5D Showcase 씬을 생성하고 마이그레이션한다.
// 책임: 플레이어·그레이박스·카메라·전투 더미·능력 진행 루프와 에디터 메뉴를 생성한다.
// 불변식: 빌더를 다시 실행해도 자신이 만든 이름의 프로토타입 루트만 제거한다.
// 선택 이유: 씬 생성은 에디터 전용으로 두어 런타임 스크립트를 게임플레이에 집중시킨다.
using System.Collections.Generic;
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

            camera.orthographic = true;
            camera.orthographicSize = 5.2f;
            camera.transform.SetPositionAndRotation(
                new Vector3(1.35f, 2.4f, -9f),
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

            if (changed)
            {
                Scene scene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
            }

            return changed;
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
                    new Vector2(-10.75f, 6.2f)),
                // 시작 홀은 체크포인트와 양쪽 출구가 동시에 읽히는 제한된 수평 이동을 허용한다.
                new(
                    startHall,
                    new Vector2(-4f, 2.4f),
                    new Vector2(2f, 3.4f)),
                // 이동 실험실은 긴 계단 동선을 따라가되 구역 밖을 과도하게 보여 주지 않는다.
                new(
                    traversalLab,
                    new Vector2(10f, 3f),
                    new Vector2(20f, 5.2f))
            };

            sideScrollerCamera.Configure(player.transform);
            if (!sideScrollerCamera.ConfigureWorldBounds(
                worldState,
                startHall,
                bounds))
            {
                return false;
            }

            EditorUtility.SetDirty(sideScrollerCamera);
            return true;
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
                abilityMaterial))
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
            Material rewardMaterial)
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
            if (reward == null)
            {
                // 완주 지점을 물리적으로 밟을 수 있는 보상 블록으로 생성한다.
                reward = CreateBlock(
                    parent,
                    "Backtrack_Reward",
                    new Vector3(-11f, 6.5f, 0f),
                    Vector3.one,
                    rewardMaterial);
                changed = true;
            }
            else if (SetTransformIfDifferent(
                reward.transform,
                new Vector3(-11f, 6.5f, 0f),
                Vector3.one))
            {
                // 이전 위치의 보상 블록을 샤프트 정상으로 멱등적으로 이동한다.
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
            dummy.transform.SetParent(parent);
            dummy.transform.position = new Vector3(10f, 2f, 0f);
            dummy.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
            dummy.GetComponent<MeshRenderer>().sharedMaterial = material;

            Health health = dummy.AddComponent<Health>();
            health.Configure(3);
            dummy.AddComponent<TrainingDummy>();
            return dummy;
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
            if (material.color != color)
            {
                material.color = color;
                EditorUtility.SetDirty(material);
            }

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
