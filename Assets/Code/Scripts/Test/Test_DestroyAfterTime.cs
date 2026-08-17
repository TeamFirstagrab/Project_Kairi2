using UnityEngine;

public class Test_DestroyAfterTime : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.25f; // 이펙트가 살아서 화면에 보일 시간 (초 단위)

    private void Start()
    {
        // 지정된 시간(lifetime) 후에 이 이펙트 오브젝트를 씬에서 완전히 삭제 파괴합니다!
        Destroy(gameObject, lifetime);
    }
}