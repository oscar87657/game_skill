// GOLDEN STANDARD
// 목적: 플레이어 월드 진행 상태를 항상 표시되는 네 구역 지도 HUD에 반영한다.
// 책임: 방문·진입·세이브 복원 이벤트를 구독하고 노드와 연결 View를 갱신한다.
// 불변식: Presenter는 진행 상태를 변경하지 않으며 현재 구역은 지도 노드 하나에만 표시한다.
// 선택 이유: 이벤트 기반 Presenter는 매 프레임 UI를 다시 그리지 않고 지도 아트와 도메인 상태를 분리한다.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    public sealed class WorldMapPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerWorldState playerWorldState;
        [SerializeField] private WorldZoneDefinition initialZone;
        [SerializeField]
        private List<WorldMapNodeView> nodeViews = new();
        [SerializeField]
        private List<WorldMapConnectionView> connectionViews = new();

        private bool isSubscribed;

        public int NodeCount => nodeViews.Count;
        public int ConnectionCount => connectionViews.Count;

        private void OnEnable()
        {
            // 활성 HUD만 월드 진행 이벤트를 받아 중복 UI 갱신과 참조 누수를 막는다.
            Subscribe();
        }

        private void Start()
        {
            // 첫 Trigger 이벤트 전에도 시작 구역을 현재 위치로 보여 주도록 초기 상태를 그린다.
            RefreshMap();
        }

        private void OnDisable()
        {
            // 비활성 HUD가 이후 방문 이벤트를 계속 받지 않도록 구독을 해제한다.
            Unsubscribe();
        }

        public bool Configure(
            PlayerWorldState worldState,
            WorldZoneDefinition firstZone,
            IEnumerable<WorldMapNodeView> nodes,
            IEnumerable<WorldMapConnectionView> connections)
        {
            var requestedNodes = new List<WorldMapNodeView>();
            var requestedConnections =
                new List<WorldMapConnectionView>();

            // 호출자 컬렉션과 직렬화 목록을 분리해 에디터 빌더의 임시 List 수명을 공유하지 않는다.
            if (nodes != null)
            {
                foreach (WorldMapNodeView node in nodes)
                {
                    if (node != null)
                    {
                        requestedNodes.Add(node);
                    }
                }
            }

            // 연결 View도 동일하게 복사해 Inspector 데이터의 소유권을 Presenter에 둔다.
            if (connections != null)
            {
                foreach (WorldMapConnectionView connection in connections)
                {
                    if (connection != null)
                    {
                        requestedConnections.Add(connection);
                    }
                }
            }

            bool changed = playerWorldState != worldState
                || initialZone != firstZone
                || !NodeViewsMatch(
                    nodeViews,
                    requestedNodes)
                || !ConnectionViewsMatch(
                    connectionViews,
                    requestedConnections);
            if (!changed)
            {
                // 같은 구성에서도 생명주기상 해제된 이벤트가 있으면 다시 연결하고 표현을 복구한다.
                Subscribe();
                RefreshMap();
                return false;
            }

            Unsubscribe();
            playerWorldState = worldState;
            initialZone = firstZone;
            nodeViews.Clear();
            nodeViews.AddRange(requestedNodes);
            connectionViews.Clear();
            connectionViews.AddRange(requestedConnections);
            Subscribe();
            RefreshMap();
            return true;
        }

        public void RefreshMap()
        {
            WorldZoneDefinition currentZone =
                playerWorldState?.CurrentZone ?? initialZone;

            // 각 노드는 현재·방문 여부만 받아 자신의 색상과 레이블 표현을 책임진다.
            foreach (WorldMapNodeView node in nodeViews)
            {
                if (node == null || !node.IsConfigured)
                {
                    continue;
                }

                bool isCurrent = currentZone != null
                    && string.Equals(
                        currentZone.Id,
                        node.Zone.Id,
                        StringComparison.Ordinal);
                bool isVisited = playerWorldState != null
                    && playerWorldState.HasVisited(node.Zone);
                node.Apply(isVisited, isCurrent);
            }

            // 연결선은 양 끝 구역의 방문 여부를 한 번씩 읽어 발견 상태를 갱신한다.
            foreach (WorldMapConnectionView connection
                in connectionViews)
            {
                connection?.Apply(playerWorldState);
            }
        }

        public WorldMapVisualState GetNodeState(
            string zoneId)
        {
            // 테스트와 다른 HUD가 구역 ID로 현재 표현 상태를 조회할 수 있게 한다.
            foreach (WorldMapNodeView node in nodeViews)
            {
                if (node != null && node.Matches(zoneId))
                {
                    return node.State;
                }
            }

            return WorldMapVisualState.Hidden;
        }

        private void HandleZoneChanged(
            WorldZoneDefinition changedZone)
        {
            // 최초 방문과 재진입 모두 같은 전체 갱신 경로를 사용해 이벤트 순서 차이를 흡수한다.
            RefreshMap();
        }

        private void HandleWorldStateRestored()
        {
            // 여러 ID가 한 번에 교체되는 세이브 복원은 전체 지도를 한 번만 다시 그린다.
            RefreshMap();
        }

        private void Subscribe()
        {
            // OnEnable과 Configure가 연속 호출돼도 두 이벤트는 각각 한 번만 구독한다.
            if (isSubscribed || playerWorldState == null)
            {
                return;
            }

            playerWorldState.ZoneVisited += HandleZoneChanged;
            playerWorldState.ZoneEntered += HandleZoneChanged;
            playerWorldState.WorldStateRestored +=
                HandleWorldStateRestored;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            // 상태가 없거나 이미 해제된 경우에도 생명주기 종료를 안전하게 처리한다.
            if (!isSubscribed || playerWorldState == null)
            {
                isSubscribed = false;
                return;
            }

            playerWorldState.ZoneVisited -= HandleZoneChanged;
            playerWorldState.ZoneEntered -= HandleZoneChanged;
            playerWorldState.WorldStateRestored -=
                HandleWorldStateRestored;
            isSubscribed = false;
        }

        private static bool NodeViewsMatch(
            IReadOnlyList<WorldMapNodeView> current,
            IReadOnlyList<WorldMapNodeView> requested)
        {
            // 노드 개수가 다르면 같은 지도 레이아웃 구성이 아니므로 즉시 실패한다.
            if (current.Count != requested.Count)
            {
                return false;
            }

            // 구역·문구·UI 참조를 순서대로 비교해 빌더 재실행 결과를 결정적으로 유지한다.
            for (int index = 0; index < current.Count; index++)
            {
                WorldMapNodeView currentNode =
                    current[index];
                WorldMapNodeView requestedNode =
                    requested[index];
                if (currentNode == null
                    || requestedNode == null
                    || currentNode.Zone != requestedNode.Zone
                    || currentNode.MapLabel
                        != requestedNode.MapLabel
                    || currentNode.Background
                        != requestedNode.Background
                    || currentNode.Label
                        != requestedNode.Label)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ConnectionViewsMatch(
            IReadOnlyList<WorldMapConnectionView> current,
            IReadOnlyList<WorldMapConnectionView> requested)
        {
            // 연결선 개수가 다르면 같은 지도 그래프 구성이 아니므로 즉시 실패한다.
            if (current.Count != requested.Count)
            {
                return false;
            }

            // 양 끝 구역과 선 Image 참조를 비교해 UI 계층이 바뀐 경우에만 다시 직렬화한다.
            for (int index = 0; index < current.Count; index++)
            {
                WorldMapConnectionView currentConnection =
                    current[index];
                WorldMapConnectionView requestedConnection =
                    requested[index];
                if (currentConnection == null
                    || requestedConnection == null
                    || currentConnection.FirstZone
                        != requestedConnection.FirstZone
                    || currentConnection.SecondZone
                        != requestedConnection.SecondZone
                    || currentConnection.Line
                        != requestedConnection.Line)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
