using UnityEngine;
using EnumType;

public class ThrowablePickup : MonoBehaviour
{
    public ThrowableType type; // 꽃병, 병, 돌 등의 종류
    public Sprite uiSprite;    // 주웠을 때 UI에 띄울 스프라이트 이미지

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 충돌한 대상이 플레이어인지 태그로 확인
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<PlayerThrow>(out var playerThrow))
            {
                playerThrow.SetNearbyPickup(this);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<PlayerThrow>(out var playerThrow))
            {
                playerThrow.ClearNearbyPickup(this);
            }
        }
    }
}