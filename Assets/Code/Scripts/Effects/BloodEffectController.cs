using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BloodEffectController : MonoBehaviour
{
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();

        // 1부터 6까지 무작위 애니메이션을 선택해 재생합니다.
        int randomIndex = Random.Range(1, 7);
        string stateName = "BloodEffect" + randomIndex;
        
        animator.Play(stateName);

        // 피 이펙트가 다 재생된 후 씬에서 자동으로 소멸하게 처리합니다.
        // (피 튀는 애니메이션은 보통 0.5초 이내이므로 1.0초 뒤 넉넉하게 삭제합니다.)
        Destroy(gameObject, 1.0f);
    }
}
