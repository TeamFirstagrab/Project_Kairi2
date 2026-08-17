using UnityEngine;

public class KillSlashEffect : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float duration = 0.2f; // 이펙트 유지 시간
    [SerializeField] private float length = 8f;     // 일섬 선의 길이
    [SerializeField] private float startWidth = 0.3f; // 시작 두께 (이펙트 굵기)
    private float timer = 0f;

    private void Start()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        // 3개의 점으로 일섬 선 정의 (시작, 중간, 끝)
        lineRenderer.positionCount = 3;
        lineRenderer.SetPosition(0, Vector3.left * (length / 2f));
        lineRenderer.SetPosition(1, Vector3.zero);
        lineRenderer.SetPosition(2, Vector3.right * (length / 2f));
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // 시간에 따라 얇아지거나 사라지게 처리 (Fade Out)
        float progress = timer / duration;
        if (progress >= 1.0f)
        {
            Destroy(gameObject);
        }
        else
        {
            // 선의 굵기를 서서히 줄여 예리하게 사라지는 연출
            lineRenderer.widthMultiplier = Mathf.Lerp(startWidth, 0f, progress);
        }
    }
}
