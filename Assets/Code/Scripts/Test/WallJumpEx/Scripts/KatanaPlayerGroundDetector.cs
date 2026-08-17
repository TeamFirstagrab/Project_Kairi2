using UnityEngine;

/// <summary>
/// 플레이어가 바닥(Ground)에 닿아 있는지 감지하는 컴포넌트입니다.
/// 캐릭터의 콜라이더 범위를 기준으로 아래 방향으로 BoxCast를 쏘아 감지합니다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class KatanaPlayerGroundDetector : MonoBehaviour
{
    [Header("바닥 감지 레이어")]
    [Tooltip("바닥으로 판정할 레이어(LayerMask)를 지정합니다.")]
    [SerializeField] private LayerMask groundLayer;
    
    [Header("감지 거리 오프셋")]
    [Tooltip("콜라이더 하단으로부터 얼마나 더 아래쪽까지 감지할지 결정하는 여유 거리입니다.")]
    [SerializeField] private float extraCastDistance = 0.05f;

    // 플레이어의 콜라이더 레퍼런스 (크기 및 범위 측정을 위해 사용)
    private Collider2D playerCollider;

    /// <summary>
    /// 플레이어가 바닥에 접지해 있으면 true, 공중에 떠 있으면 false를 반환하는 읽기 전용 프로퍼티입니다.
    /// </summary>
    public bool IsGrounded { get; private set; }

    private void Awake()
    {
        playerCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        // 매 프레임마다 바닥 상태를 물리 엔진으로 검사합니다.
        IsGrounded = CheckGrounded();
    }

    /// <summary>
    /// Physics2D.BoxCast를 이용해 실제 바닥 충돌 여부를 감증하는 핵심 메서드입니다.
    /// </summary>
    private bool CheckGrounded()
    {
        // 현재 캐릭터 콜라이더의 영역 경계(Bounds)를 가져옵니다.
        Bounds bounds = playerCollider.bounds;
        
        // 박스캐스트를 발사할 거리 계산 (콜라이더 절반 높이 + 여유 거리)
        float castDistance = (bounds.size.y / 2f) + extraCastDistance;

        // 콜라이더의 중심에서 아래 방향으로 가로폭의 90% 크기인 납작한 박스를 아래로 쏘아 충돌을 확인합니다.
        RaycastHit2D hit = Physics2D.BoxCast(
            bounds.center,                              // 발사 시작 위치 (콜라이더 중심)
            new Vector2(bounds.size.x * 0.9f, 0.1f),   // 캐스팅할 박스의 크기 (가로는 약간 좁게, 세로는 납작하게)
            0f,                                         // 회전 각도 (회전 없음)
            Vector2.down,                               // 발사 방향 (아래쪽)
            castDistance,                               // 검사 거리
            groundLayer                                 // 충돌을 감지할 레이어
        );

        // 충돌한 콜라이더가 존재하면 바닥에 닿은 것으로 간주합니다.
        return hit.collider != null;
    }

    /// <summary>
    /// 씬(Scene) 뷰에서 바닥 감지 영역을 직관적으로 시각화해주는 함수입니다.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider2D>();
            if (playerCollider == null) return;
        }

        Bounds bounds = playerCollider.bounds;
        float castDistance = (bounds.size.y / 2f) + extraCastDistance;
        Vector2 boxSize = new Vector2(bounds.size.x * 0.9f, 0.1f);

        // 접지 상태에 따라 초록색(접지됨) 또는 빨간색(공중)으로 기즈모 색상을 변경합니다.
        Gizmos.color = IsGrounded ? Color.green : Color.red;

        Vector3 startPosition = bounds.center;
        Vector3 endPosition = bounds.center + Vector3.down * castDistance;

        // 시작 지점 박스 그리기
        Gizmos.DrawWireCube(startPosition, new Vector3(boxSize.x, boxSize.y, 0));
        // 감지 끝 지점 박스 그리기 (여유 거리가 반영된 위치)
        Gizmos.DrawWireCube(endPosition, new Vector3(boxSize.x, boxSize.y, 0));
        // 두 박스의 중심을 잇는 선 그리기
        Gizmos.DrawLine(startPosition, endPosition);
    }
}

