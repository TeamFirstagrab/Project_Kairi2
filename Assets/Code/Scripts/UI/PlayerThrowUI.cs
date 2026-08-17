using UnityEngine;
using UnityEngine.UI;

public class PlayerThrowUI : MonoBehaviour
{
    public static PlayerThrowUI Instance { get; private set; }

    [SerializeField] private Image itemIconImage; // 인스펙터에서 UI Image 등록

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // 초기화 시 아이콘 비활성화
        UpdateUI(null);
    }

    public void UpdateUI(Sprite itemSprite)
    {
        if (itemSprite == null)
        {
            itemIconImage.enabled = false;
            itemIconImage.sprite = null;
        }
        else
        {
            itemIconImage.sprite = itemSprite;
            itemIconImage.enabled = true;
        }
    }
}