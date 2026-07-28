// GOLDEN STANDARD
// 목적: 현재 목표의 월드 위치를 하나의 재사용 가능한 비콘으로 표시한다.
// 책임: 목표 위치 이동, 렌더러 표시 전환과 읽기 쉬운 부유·맥동 애니메이션을 담당한다.
// 불변식: 비콘은 충돌과 진행 상태를 변경하지 않으며 동시에 한 위치만 가리킨다.
// 선택 이유: 단계별 표식을 여러 개 유지하는 대신 단일 View를 이동하면 씬 크기와 관리 비용이 작다.
using System.Collections.Generic;
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    public sealed class WorldGuidanceMarker : MonoBehaviour
    {
        [SerializeField]
        private Transform animatedVisual;
        [SerializeField]
        private List<Renderer> markerRenderers =
            new();

        private Vector3 baseLocalPosition;
        private Vector3 baseLocalScale =
            Vector3.one;

        public bool IsVisible { get; private set; }
        public int RendererCount =>
            markerRenderers.Count;

        private void Awake()
        {
            // 씬에 저장된 기준 위치와 크기를 캐시해 매 프레임 누적 오차 없이 애니메이션한다.
            CacheVisualTransform();
        }

        private void Update()
        {
            // 숨겨진 비콘은 Transform 갱신도 생략해 안내가 끝난 뒤 비용을 만들지 않는다.
            if (!IsVisible
                || animatedVisual == null)
            {
                return;
            }

            float wave =
                Mathf.Sin(Time.time * 3.6f);
            animatedVisual.localPosition =
                baseLocalPosition
                + Vector3.up
                    * (0.12f * wave);
            animatedVisual.localScale =
                baseLocalScale
                * (1f + 0.06f * wave);
        }

        public bool Configure(
            Transform visual,
            IEnumerable<Renderer> renderers)
        {
            // 호출자의 컬렉션과 Inspector 목록을 분리하고 null Renderer는 표시 계약에서 제외한다.
            var requestedRenderers =
                new List<Renderer>();
            if (renderers != null)
            {
                // 빌더가 전달한 시각 자식만 순서대로 복사해 중복 실행 결과를 비교할 수 있게 한다.
                foreach (Renderer renderer
                    in renderers)
                {
                    if (renderer != null)
                    {
                        requestedRenderers.Add(
                            renderer);
                    }
                }
            }

            bool changed =
                animatedVisual != visual
                || !RenderersMatch(
                    markerRenderers,
                    requestedRenderers);
            animatedVisual = visual;
            markerRenderers.Clear();
            markerRenderers.AddRange(
                requestedRenderers);
            CacheVisualTransform();
            SetVisible(false);
            return changed;
        }

        public void ShowAt(
            Vector3 worldPosition)
        {
            // 같은 비콘 루트를 다음 목표 좌표로 옮겨 단계별 View 중복 없이 길을 안내한다.
            transform.position =
                worldPosition;
            if (animatedVisual != null)
            {
                animatedVisual.localPosition =
                    baseLocalPosition;
                animatedVisual.localScale =
                    baseLocalScale;
            }

            SetVisible(true);
        }

        public void SetVisible(
            bool visible)
        {
            // 모든 시각 Renderer를 함께 전환해 빔과 다이아몬드가 서로 다른 상태로 남지 않게 한다.
            IsVisible = visible;
            foreach (Renderer markerRenderer
                in markerRenderers)
            {
                if (markerRenderer != null)
                {
                    markerRenderer.enabled =
                        visible;
                }
            }
        }

        private void CacheVisualTransform()
        {
            // 시각 자식이 없는 부분 구성도 안전하게 허용하고 기본값은 다음 Configure에서 갱신한다.
            if (animatedVisual == null)
            {
                return;
            }

            baseLocalPosition =
                animatedVisual.localPosition;
            baseLocalScale =
                animatedVisual.localScale;
        }

        private static bool RenderersMatch(
            IReadOnlyList<Renderer> first,
            IReadOnlyList<Renderer> second)
        {
            // 길이와 참조 순서를 비교해 같은 빌더 구성이 씬을 불필요하게 Dirty로 만들지 않게 한다.
            if (first.Count != second.Count)
            {
                return false;
            }

            // 각 Renderer 참조를 순서대로 확인해 일부 자식만 교체된 경우도 마이그레이션한다.
            for (int index = 0;
                 index < first.Count;
                 index++)
            {
                if (first[index] != second[index])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
