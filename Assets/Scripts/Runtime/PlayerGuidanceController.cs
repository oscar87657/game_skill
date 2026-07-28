// GOLDEN STANDARD
// 목적: 실제 조작 성공과 영구 진행 상태를 HUD 목표와 월드 비콘으로 번역한다.
// 책임: 이동·전투·능력·월드 이벤트 구독, 현재 단계 갱신과 단일 비콘 목적지 선택을 담당한다.
// 불변식: 안내 시스템은 능력·보상·보스 상태를 변경하지 않고 공개 상태를 읽기만 한다.
// 선택 이유: 이벤트 기반 Controller와 순수 진행 규칙을 분리하면 튜토리얼 문구와 월드 배치를 독립적으로 교체할 수 있다.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameSkill
{
    [Serializable]
    public sealed class GuidanceWaypoint
    {
        [SerializeField]
        private string markerId;
        [SerializeField]
        private Vector3 worldPosition;

        public GuidanceWaypoint(
            string id,
            Vector3 position)
        {
            // 빌더와 테스트가 같은 정규화 규칙으로 목적지 키와 좌표를 만들게 한다.
            markerId =
                id?.Trim()
                ?? string.Empty;
            worldPosition = position;
        }

        public string MarkerId =>
            markerId;
        public Vector3 WorldPosition =>
            worldPosition;

        public bool Matches(
            string requestedMarkerId)
        {
            // 빈 키는 어떤 목적지와도 일치시키지 않아 완료 단계에서 이전 비콘을 남기지 않는다.
            return !string.IsNullOrWhiteSpace(
                    requestedMarkerId)
                && string.Equals(
                    markerId,
                    requestedMarkerId.Trim(),
                    StringComparison.Ordinal);
        }

        public bool HasSameValue(
            GuidanceWaypoint other)
        {
            // 씬 마이그레이션은 객체 참조가 아니라 직렬화될 ID와 좌표 값이 같은지 비교한다.
            return other != null
                && string.Equals(
                    markerId,
                    other.markerId,
                    StringComparison.Ordinal)
                && (worldPosition
                    - other.worldPosition)
                    .sqrMagnitude
                    < 0.000001f;
        }
    }

    [DisallowMultipleComponent]
    public sealed class PlayerGuidanceController :
        MonoBehaviour
    {
        private const string RewardId =
            "reward_shaft_health_fragment";
        private const string ShortcutId =
            "shortcut_shaft_return";
        private const string BossId =
            "ability_warden";

        [SerializeField]
        private Transform player;
        [SerializeField]
        private SideScrollerMotor motor;
        [SerializeField]
        private PlayerCombat combat;
        [SerializeField]
        private PlayerAbilityState abilityState;
        [SerializeField]
        private PlayerWorldState worldState;
        [SerializeField]
        private AbilityDefinition doubleJumpAbility;
        [SerializeField]
        private AbilityDefinition airDashAbility;
        [SerializeField]
        private AbilityDefinition wallTraversalAbility;
        [SerializeField]
        private Text objectiveLabel;
        [SerializeField]
        private Text hintLabel;
        [SerializeField]
        private WorldGuidanceMarker worldMarker;
        [SerializeField]
        private List<GuidanceWaypoint> waypoints =
            new();

        private Vector3 sessionStartPosition;
        private bool moved;
        private bool jumped;
        private bool dashed;
        private bool attacked;
        private bool isSubscribed;

        public GuidanceStage CurrentStage
        {
            get;
            private set;
        }
        public string ObjectiveText =>
            objectiveLabel != null
                ? objectiveLabel.text
                : string.Empty;
        public string HintText =>
            hintLabel != null
                ? hintLabel.text
                : string.Empty;
        public int WaypointCount =>
            waypoints.Count;
        public bool IsConfigured =>
            player != null
            && motor != null
            && combat != null
            && abilityState != null
            && worldState != null
            && doubleJumpAbility != null
            && airDashAbility != null
            && wallTraversalAbility != null
            && objectiveLabel != null
            && hintLabel != null
            && worldMarker != null;

        private void Awake()
        {
            // 첫 이동 거리를 씬 원점이 아니라 실제 플레이 시작 위치를 기준으로 측정한다.
            CacheSessionStart();
        }

        private void OnEnable()
        {
            // 활성 HUD만 게임플레이 이벤트를 구독하고 현재 직렬화 상태를 즉시 화면에 반영한다.
            Subscribe();
            Refresh();
        }

        private void Start()
        {
            // 모든 상태 컴포넌트의 Awake가 끝난 뒤 저장된 초기 능력과 현재 구역을 한 번 더 읽는다.
            CacheSessionStart();
            Refresh();
        }

        private void Update()
        {
            // 이동 튜토리얼을 완료하기 전까지만 시작점과 수평 거리를 비교해 매 프레임 비용을 제한한다.
            if (moved
                || player == null)
            {
                return;
            }

            if (Mathf.Abs(
                    player.position.x
                    - sessionStartPosition.x)
                < 0.75f)
            {
                return;
            }

            moved = true;
            Refresh();
        }

        private void OnDisable()
        {
            // 비활성 HUD가 이전 플레이어 이벤트를 계속 받아 중복 안내를 만들지 않도록 해제한다.
            Unsubscribe();
            if (worldMarker != null)
            {
                worldMarker.SetVisible(false);
            }
        }

        public bool Configure(
            Transform playerTransform,
            SideScrollerMotor playerMotor,
            PlayerCombat playerCombat,
            PlayerAbilityState playerAbilityState,
            PlayerWorldState playerWorldState,
            AbilityDefinition doubleJump,
            AbilityDefinition airDash,
            AbilityDefinition wallTraversal,
            Text objectiveText,
            Text hintText,
            WorldGuidanceMarker marker,
            IEnumerable<GuidanceWaypoint> markerWaypoints)
        {
            // 호출자 컬렉션과 Inspector 목록을 분리하고 유효한 목적지만 직렬화 대상으로 복사한다.
            var requestedWaypoints =
                new List<GuidanceWaypoint>();
            if (markerWaypoints != null)
            {
                // 빈 키의 목적지는 어느 단계에서도 찾을 수 없으므로 구성 경계에서 제외한다.
                foreach (GuidanceWaypoint waypoint
                    in markerWaypoints)
                {
                    if (waypoint != null
                        && !string.IsNullOrWhiteSpace(
                            waypoint.MarkerId))
                    {
                        requestedWaypoints.Add(
                            waypoint);
                    }
                }
            }

            bool changed =
                player != playerTransform
                || motor != playerMotor
                || combat != playerCombat
                || abilityState != playerAbilityState
                || worldState != playerWorldState
                || doubleJumpAbility != doubleJump
                || airDashAbility != airDash
                || wallTraversalAbility
                    != wallTraversal
                || objectiveLabel != objectiveText
                || hintLabel != hintText
                || worldMarker != marker
                || !WaypointsMatch(
                    waypoints,
                    requestedWaypoints);

            Unsubscribe();
            player = playerTransform;
            motor = playerMotor;
            combat = playerCombat;
            abilityState = playerAbilityState;
            worldState = playerWorldState;
            doubleJumpAbility = doubleJump;
            airDashAbility = airDash;
            wallTraversalAbility =
                wallTraversal;
            objectiveLabel = objectiveText;
            hintLabel = hintText;
            worldMarker = marker;
            waypoints.Clear();
            waypoints.AddRange(
                requestedWaypoints);
            CacheSessionStart();
            Subscribe();
            Refresh();
            return changed;
        }

        public void Refresh()
        {
            // 현재 상태를 순수 진행 규칙에 전달하고 결과만 View에 적용한다.
            string currentZoneId =
                worldState != null
                && worldState.CurrentZone != null
                    ? worldState.CurrentZone.Id
                    : string.Empty;
            CurrentStage =
                GuidanceProgression.Resolve(
                    moved,
                    jumped,
                    dashed,
                    attacked,
                    HasAbility(
                        doubleJumpAbility),
                    HasAbility(
                        airDashAbility),
                    HasAbility(
                        wallTraversalAbility),
                    currentZoneId,
                    worldState != null
                        && worldState.IsRewardCollected(
                            RewardId),
                    worldState != null
                        && worldState.IsShortcutUnlocked(
                            ShortcutId),
                    worldState != null
                        && worldState.IsBossDefeated(
                            BossId));
            ApplyContent(
                GuidanceProgression.ContentFor(
                    CurrentStage));
        }

        private bool HasAbility(
            AbilityDefinition ability)
        {
            // 부분 구성된 EditMode와 정상 PlayMode 모두 같은 null 안전 조회를 사용한다.
            return abilityState != null
                && ability != null
                && abilityState.HasAbility(
                    ability);
        }

        private void ApplyContent(
            GuidanceContent content)
        {
            // HUD 두 줄을 같은 단계 데이터에서 갱신해 목표와 조작 힌트가 어긋나지 않게 한다.
            if (objectiveLabel != null)
            {
                objectiveLabel.text =
                    content.Objective;
            }

            if (hintLabel != null)
            {
                hintLabel.text =
                    content.Hint;
            }

            if (worldMarker == null)
            {
                return;
            }

            GuidanceWaypoint target =
                FindWaypoint(
                    content.MarkerId);
            if (!content.HasMarker
                || target == null)
            {
                worldMarker.SetVisible(false);
                return;
            }

            worldMarker.ShowAt(
                target.WorldPosition);
        }

        private GuidanceWaypoint FindWaypoint(
            string markerId)
        {
            // 소수의 설계 목적지를 선형 탐색해 Dictionary 직렬화용 보조 구조를 만들지 않는다.
            foreach (GuidanceWaypoint waypoint
                in waypoints)
            {
                if (waypoint != null
                    && waypoint.Matches(
                        markerId))
                {
                    return waypoint;
                }
            }

            return null;
        }

        private void CacheSessionStart()
        {
            // 재구성이나 Play 시작 시점의 실제 위치를 이동 학습 기준으로 저장한다.
            if (player != null)
            {
                sessionStartPosition =
                    player.position;
            }
        }

        private void Subscribe()
        {
            // Configure와 OnEnable이 이어져도 각 입력·진행 이벤트를 한 번만 받는다.
            if (isSubscribed)
            {
                return;
            }

            if (motor != null)
            {
                motor.Jumped +=
                    HandleJumped;
                motor.DashStarted +=
                    HandleDashStarted;
            }

            if (combat != null)
            {
                combat.AttackStarted +=
                    HandleAttackStarted;
            }

            if (abilityState != null)
            {
                abilityState.AbilityStateChanged +=
                    HandleProgressChanged;
            }

            if (worldState != null)
            {
                worldState.ZoneEntered +=
                    HandleZoneEntered;
                worldState.RewardCollected +=
                    HandleIdProgressChanged;
                worldState.ShortcutUnlocked +=
                    HandleIdProgressChanged;
                worldState.BossDefeated +=
                    HandleIdProgressChanged;
                worldState.WorldStateRestored +=
                    HandleProgressChanged;
            }

            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            // 부분 구성 상태에서도 실제 연결된 참조만 안전하게 해제한다.
            if (!isSubscribed)
            {
                return;
            }

            if (motor != null)
            {
                motor.Jumped -=
                    HandleJumped;
                motor.DashStarted -=
                    HandleDashStarted;
            }

            if (combat != null)
            {
                combat.AttackStarted -=
                    HandleAttackStarted;
            }

            if (abilityState != null)
            {
                abilityState.AbilityStateChanged -=
                    HandleProgressChanged;
            }

            if (worldState != null)
            {
                worldState.ZoneEntered -=
                    HandleZoneEntered;
                worldState.RewardCollected -=
                    HandleIdProgressChanged;
                worldState.ShortcutUnlocked -=
                    HandleIdProgressChanged;
                worldState.BossDefeated -=
                    HandleIdProgressChanged;
                worldState.WorldStateRestored -=
                    HandleProgressChanged;
            }

            isSubscribed = false;
        }

        private void HandleJumped()
        {
            // 점프 버튼이 아니라 Motor가 승인한 점프만 학습 완료로 기록한다.
            jumped = true;
            Refresh();
        }

        private void HandleDashStarted(
            float direction)
        {
            // 대시 방향과 무관하게 실제 시작 이벤트 한 번으로 조작 학습을 완료한다.
            dashed = true;
            Refresh();
        }

        private void HandleAttackStarted(
            int comboStep)
        {
            // 콤보 단계와 무관하게 승인된 첫 공격으로 기본 전투 학습을 완료한다.
            attacked = true;
            Refresh();
        }

        private void HandleProgressChanged()
        {
            // 능력 전체 복원과 월드 전체 복원은 같은 상태 재평가 경로를 사용한다.
            Refresh();
        }

        private void HandleZoneEntered(
            WorldZoneDefinition zone)
        {
            // 구역 진입으로 샤프트·보스 내부 목표가 달라질 수 있으므로 즉시 재평가한다.
            Refresh();
        }

        private void HandleIdProgressChanged(
            string progressId)
        {
            // 보상·지름길·보스 이벤트의 ID는 상태 원본에 이미 기록됐으므로 다시 조회해 다음 목표를 고른다.
            Refresh();
        }

        private static bool WaypointsMatch(
            IReadOnlyList<GuidanceWaypoint> first,
            IReadOnlyList<GuidanceWaypoint> second)
        {
            // 목적지 수가 다르면 이후 값 비교 없이 다른 구성으로 판단한다.
            if (first.Count != second.Count)
            {
                return false;
            }

            // ID와 좌표 값을 순서대로 비교해 빌더 재실행의 불필요한 씬 변경을 막는다.
            for (int index = 0;
                 index < first.Count;
                 index++)
            {
                if (first[index] == null
                    || !first[index].HasSameValue(
                        second[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
