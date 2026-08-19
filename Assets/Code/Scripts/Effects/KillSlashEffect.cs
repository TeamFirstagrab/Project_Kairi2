using UnityEngine;

public class KillSlashEffect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float duration = 0.2f; // 이펙트 유지 시간
    [SerializeField] private float rotationOffset = -90f; // 기본 스프라이트 방향 보정용 오프셋 각도 (세로형인 경우 -90)
    [SerializeField] private float moveSpeed = 15f; // 앞으로 날아가는 속도

    [Header("Afterimage Settings")]
    [SerializeField] private bool useAfterimage = true; // 잔상 효과 사용 여부
    [SerializeField] private float afterimageInterval = 0.02f; // 잔상 생성 주기
    [SerializeField] private float afterimageFadeDuration = 0.15f; // 잔상이 사라지는 시간
    [SerializeField] private float afterimageStartAlpha = 0.5f; // 잔상 시작 투명도
    
    private float timer = 0f;
    private float afterimageTimer = 0f;
    private Vector3 moveDirection;

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // 보정 전의 transform.right가 공격 방향(Enemy.cs가 설정해준 방향)입니다.
        moveDirection = transform.right;

        // 공격 방향 회전(Enemy.cs에서 적용한 회전)에 오프셋 각도 적용
        transform.rotation *= Quaternion.Euler(0f, 0f, rotationOffset);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // 공격 방향으로 날리기
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // 잔상 생성 처리
        if (useAfterimage)
        {
            afterimageTimer += Time.deltaTime;
            if (afterimageTimer >= afterimageInterval)
            {
                afterimageTimer = 0f;
                SpawnAfterimage();
            }
        }

        // 시간에 따라 서서히 사라지게 처리 (Fade Out)
        float progress = timer / duration;
        if (progress >= 1.0f)
        {
            Destroy(gameObject);
        }
        else
        {
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(1f, 0f, progress);
                spriteRenderer.color = color;
            }
        }
    }

    private void SpawnAfterimage()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;

        // 잔상용 게임오브젝트 생성
        GameObject ghost = new GameObject("SlashAfterimage");
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.localScale;

        // 스프라이트 렌더러 복사
        SpriteRenderer ghostSR = ghost.AddComponent<SpriteRenderer>();
        ghostSR.sprite = spriteRenderer.sprite;
        ghostSR.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, afterimageStartAlpha);
        ghostSR.sortingLayerID = spriteRenderer.sortingLayerID;
        ghostSR.sortingOrder = spriteRenderer.sortingOrder - 1; // 원본보다 살짝 아래 레이어에 배치

        // 페이드아웃 및 제거 루틴 시작
        StartCoroutine(FadeAndDestroyGhost(ghost, ghostSR));
    }

    private System.Collections.IEnumerator FadeAndDestroyGhost(GameObject ghostObj, SpriteRenderer sr)
    {
        float t = 0f;
        Color startColor = sr.color;

        while (t < afterimageFadeDuration)
        {
            t += Time.deltaTime;
            if (sr == null || ghostObj == null) yield break;

            float alpha = Mathf.Lerp(afterimageStartAlpha, 0f, t / afterimageFadeDuration);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        if (ghostObj != null)
        {
            Destroy(ghostObj);
        }
    }
}
