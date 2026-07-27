// GOLDEN STANDARD
// 목적: 플레이어 접촉을 체크포인트 활성화 요청으로 변환하고 시각 피드백을 제공한다.
// 책임: 트리거를 감지하고 고유 ID·재시작 위치를 PlayerCheckpointState에 전달한다.
// 불변식: 체크포인트는 체력이나 사망 상태를 직접 소유하지 않으며 Collider는 항상 Trigger다.
// 선택 이유: 월드 상호작용과 플레이어 진행 상태를 분리해 같은 체크포인트를 다른 씬에서도 재사용한다.
using UnityEngine;

namespace GameSkill
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class Checkpoint : MonoBehaviour
    {
        private static readonly int BaseColorProperty =
            Shader.PropertyToID("_BaseColor");

        [SerializeField] private string checkpointId = "checkpoint_start";
        [SerializeField] private Vector3 respawnOffset = new(0f, 0.05f, 0f);
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private Color inactiveColor =
            new(0.18f, 0.45f, 0.55f, 1f);
        [SerializeField] private Color activeColor =
            new(0.2f, 1f, 0.85f, 1f);

        private MaterialPropertyBlock visualProperties;

        public string Id => checkpointId;
        public Vector3 RespawnPosition => transform.position + respawnOffset;
        public bool IsActivated { get; private set; }

        private void Awake()
        {
            // 물리 설정이 잘못 저장된 프리팹도 체크포인트가 벽이 되지 않도록 강제한다.
            GetComponent<Collider>().isTrigger = true;
            visualRenderer ??= GetComponentInChildren<Renderer>();
            SetVisual(false);
        }

        public void Configure(
            string id,
            Vector3 offset,
            Renderer renderer)
        {
            // 에디터 빌더와 테스트가 private 직렬화 필드를 우회하지 않고 같은 설정 경로를 사용한다.
            checkpointId = id;
            respawnOffset = offset;
            visualRenderer = renderer;

            Collider checkpointCollider = GetComponent<Collider>();
            if (checkpointCollider != null)
            {
                checkpointCollider.isTrigger = true;
            }
        }

        public bool Activate(PlayerCheckpointState playerState)
        {
            // 트리거와 테스트가 동일한 공개 활성화 경로를 거쳐 결과 차이가 생기지 않게 한다.
            if (playerState == null
                || !playerState.ActivateCheckpoint(
                    checkpointId,
                    RespawnPosition))
            {
                return false;
            }

            IsActivated = true;
            SetVisual(true);
            Debug.Log(
                $"체크포인트 활성화: {checkpointId} / 체력 완전 회복",
                this);
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 자식 콜라이더가 진입해도 부모의 플레이어 진행 상태를 찾아 한 번 활성화한다.
            PlayerCheckpointState playerState =
                other.GetComponentInParent<PlayerCheckpointState>();
            if (playerState != null)
            {
                Activate(playerState);
            }
        }

        private void SetVisual(bool activated)
        {
            // MaterialPropertyBlock은 공유 Material을 복제하지 않고 이 오브젝트의 색만 바꾼다.
            if (visualRenderer == null)
            {
                return;
            }

            visualProperties ??= new MaterialPropertyBlock();
            visualRenderer.GetPropertyBlock(visualProperties);
            visualProperties.SetColor(
                BaseColorProperty,
                activated ? activeColor : inactiveColor);
            visualRenderer.SetPropertyBlock(visualProperties);
        }

        private void OnDrawGizmosSelected()
        {
            // 다음 사망 단계에서 사용될 정확한 재시작 위치를 Scene 뷰에 표시한다.
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(RespawnPosition, 0.2f);
            Gizmos.DrawLine(
                RespawnPosition,
                RespawnPosition + Vector3.up * 1.8f);
        }
    }
}
