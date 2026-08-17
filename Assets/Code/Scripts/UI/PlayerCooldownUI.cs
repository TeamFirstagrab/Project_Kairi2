using UnityEngine;
using UnityEngine.UI;

public class PlayerCooldownUI : MonoBehaviour
{
    [Header("쿨타임 UI 요소")]
    [SerializeField] private Image cooldownImage; // 스프라이트가 교체될 UI Image
    [SerializeField] private Sprite[] cooldownSprites; // 잘라둔 8개의 스프라이트를 담을 배열

    private Canvas canvas;
    private Transform playerTransform;
    private Vector3 initialScale;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        playerTransform = transform.parent; // 플레이어의 자식으로 들어갈 예정이므로 부모 참조
        initialScale = transform.localScale;

        // 시작 시 UI를 숨김
        ShowCooldown(false);
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
            // 남은 시간 비율 (0.0 ~ 1.0)
            float ratio = currentTimer / maxCooldown;

            // 비율에 맞는 스프라이트 인덱스 계산 (시간이 다 되어갈수록 인덱스가 낮아지거나 높아지게 설정)
            // 예: 8장의 이미지가 있다면 ratio에 따라 0 ~ 7번 인덱스 선택
            int spriteIndex = Mathf.Clamp((int)(ratio * cooldownSprites.Length), 0, cooldownSprites.Length - 1);

            cooldownImage.sprite = cooldownSprites[spriteIndex];
        }
    }
}