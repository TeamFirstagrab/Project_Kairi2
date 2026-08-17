using UnityEngine;
using EnumType;

public class PlayerThrow : MonoBehaviour
{
    [Header("투사체 설정")]
    [SerializeField] private float throwSpeed = 22f; // 날아가는 속도
    [SerializeField] private GameObject projectilePrefab; // ThrownProjectile 프리팹 연결

    private ThrowableType currentItem = ThrowableType.None;
    private ThrowablePickup nearbyPickup; // 감지된 바닥의 오브젝트

    // 외부(PlayerController)에서 상태 체크를 하기 위한 함수들
    public bool HasItem() => currentItem != ThrowableType.None;
    public bool HasNearbyPickup() => nearbyPickup != null;

    public void SetNearbyPickup(ThrowablePickup pickup)
    {
        nearbyPickup = pickup;
    }

    public void ClearNearbyPickup(ThrowablePickup pickup)
    {
        if (nearbyPickup == pickup)
        {
            nearbyPickup = null;
        }
    }

    // 우클릭 입력 시 실행할 액션
    public void ExecuteThrowAction()
    {
        if (currentItem == ThrowableType.None)
        {
            if (nearbyPickup != null)
            {
                PickupItem(nearbyPickup);
            }
        }
        else
        {
            ThrowItem();
        }
    }

    private void PickupItem(ThrowablePickup pickup)
    {
        currentItem = pickup.type;

        // 우상단 UI 업데이트
        if (PlayerThrowUI.Instance != null)
        {
            PlayerThrowUI.Instance.UpdateUI(pickup.uiSprite);
        }

        // 맵에 있는 아이템 파괴
        Destroy(pickup.gameObject);
        nearbyPickup = null;
    }

    private void ThrowItem()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[PlayerThrow] 투사체 프리팹이 연결되지 않았습니다.");
            return;
        }

        // 마우스 방향 계산
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector2 throwDirection = (mouseWorldPos - transform.position).normalized;

        // 투사체 생성 및 날려보내기
        GameObject projObj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        if (projObj.TryGetComponent<ThrownProjectile>(out var projectile))
        {
            projectile.Launch(throwDirection, throwSpeed);
        }

        // 인벤토리 비우기 및 UI 리셋
        currentItem = ThrowableType.None;
        if (PlayerThrowUI.Instance != null)
        {
            PlayerThrowUI.Instance.UpdateUI(null);
        }
    }
}