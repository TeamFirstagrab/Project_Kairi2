using UnityEngine;
using UnityEngine.UI;

public class PlayerSlowCooldownUI : MonoBehaviour
{
    [Header("슬로우 쿨타임 UI 요소")]
    [SerializeField] private Image cooldownImage; // 스프라이트가 교체될 UI Image
    [SerializeField] private Sprite[] cooldownSprites; // 잘라둔 8개의 스프라이트를 담을 배열

    private Canvas canvas;
    private Transform playerTransform;
    private Vector3 initialScale;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        playerTransform = transform.parent; // 플레이어 자식으로 들어갈 터이므로 부모 참조
        initialScale = transform.localScale;

        // 항상 보이도록 설정하고 기본 상태(index 0)로 이미지 세팅
        ShowCooldown(true);
        if (cooldownImage != null && cooldownSprites != null && cooldownSprites.Length > 0)
        {
            cooldownImage.sprite = cooldownSprites[0];
        }
    }

    private void LateUpdate()
    {
        // 플레이어 캐릭터가 뒤집힐 때 UI가 같이 뒤집히지 않도록 축 고정
        if (playerTransform != null)
        {
            Vector3 parentScale = playerTransform.localScale;
            transform.localScale = new Vector3(
                Mathf.Sign(parentScale.x) * initialScale.x,
                initialScale.y,
                initialScale.z
            );
        }
    }

    // UI 보이기 / 숨기기
    public void ShowCooldown(bool show)
    {
        if (canvas != null)
        {
            canvas.enabled = show;
        }
    }

    // 쿨타임 게이지 업데이트
    public void UpdateCooldown(float currentTimer, float maxCooldown)
    {
        if (cooldownImage != null && cooldownSprites != null && cooldownSprites.Length > 0 && maxCooldown > 0f)
        {
            float ratio = currentTimer / maxCooldown;
            int spriteIndex = Mathf.Clamp((int)(ratio * cooldownSprites.Length), 0, cooldownSprites.Length - 1);
            cooldownImage.sprite = cooldownSprites[spriteIndex];
        }
    }
}