using UnityEngine;

/// <summary>
/// [플레이어의 멋진 팔 역할 - 360도 마우스 조준 베기 대장!]
/// 마우스 커서가 화면 어디에 있든 그 방향을 정확히 노려보고,
/// 360도 온 사방으로 검광 이펙트를 휘두르며 적을 타격해 물리치는 똑똑한 전투 대장입니다.
/// </summary>
public class Test_KatanaAttack : MonoBehaviour
{
    [Header("전투 설정")]
    [Tooltip("칼을 한 번 베고 나서 다음 공격을 할 수 있을 때까지 기다리는 쿨타임 대기시간 (초)")]
    [SerializeField] private float attackCooldown = 0.35f;

    [Header("검광 이펙트")]
    [Tooltip("칼을 벨 때 화면에 소환되어 나타날 멋진 검광 스프라이트 프리팹")]
    [SerializeField] private GameObject slashEffectPrefab;
    [Tooltip("캐릭터 중심에서 몇 미터 앞에 검광을 띄워서 소환할지 조절하는 거리 오프셋")]
    [SerializeField] private Vector3 effectOffset = new Vector3(1f, 0f, 0f);

    [Header("타격 판정 설정 (C# 실시간 레이저 캐스팅)")]
    [Tooltip("우리가 휘두른 칼에 맞아 대미지를 입힐 대상 필터링 레이어 (Enemy 레이어)")]
    [SerializeField] private LayerMask enemyLayer;
    [Tooltip("칼날이 쓸고 지나가는 타격 범위의 둥근 원(Radius) 반경 크기")]
    [SerializeField] private float attackRadius = 1.8f;
    [Tooltip("한 대 때릴 때마다 적의 피를 얼마나 깎을지 결정하는 공격 대미지")]
    [SerializeField] private int attackDamage = 1;

    private float lastAttackTime; // 마지막으로 공격한 시간 기록 장치 (연타 방지용)
    private Animator anim;        // 캐릭터 팔다리 모션을 바꿀 애니메이터 컴포넌트

    // 뇌 컨트롤러가 "너 지금 칼 휘두르는 중이니?" 하고 물어볼 때 대답해 줄 열쇠(Property)입니다.
    public bool IsAttacking { get; private set; }

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// [뇌 조종사의 명령 수신부 - 칼질을 시도해 봐요!]
    /// </summary>
    public void TryAttack()
    {
        // [연타 제동 장치] 지금 게임 시간(Time.time)이 마지막 공격 시간 + 쿨타임보다 적다면 리턴!
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time; // 공격 성공 시점에 현재 시간 기록!
        IsAttacking = true;         // 나 지금 베기 작동 중이야! 하고 스위치 ON

        if (anim != null)
        {
            // 애니메이터에 설정된 베기 모션("attack") 트리거를 탁 쳐서 재생시킵니다.
            anim.SetTrigger("attack");
        }

        // 칼을 벨 때 마우스 방향을 360도로 정밀 분석해 이펙트를 스폰합니다!
        SpawnSlashEffect();
        
        // 0.25초 뒤에 자동으로 베기 모션을 끄고 다시 움직일 수 있게 제어기를 환원합니다.
        Invoke(nameof(ResetAttackState), 0.25f);
    }

    /// <summary>
    /// [카타나 제로 전용 360도 마우스 조준 검광 소환기]
    /// </summary>
    private void SpawnSlashEffect()
    {
        if (slashEffectPrefab == null) return;

        // 1. [스크린좌표에서 월드좌표 변환]
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f; // 2D 평면이므로 Z축 깊이는 0으로 고정 격리

        // 2. [조준 방향 벡터 계산]
        Vector3 targetDirection = (mouseWorldPosition - transform.position).normalized;

        // 3. [삼각함수 Atan2 각도 변환]
        float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;

        // 4. [이펙트 스폰 지점 확정]
        float offsetDistance = effectOffset.x; 
        Vector3 spawnPosition = transform.position + targetDirection * offsetDistance;

        // 5. [각도 주입 Quaternion 획득]
        Quaternion spawnRotation = Quaternion.Euler(0f, 0f, angle);

        // 6. [조준점 방향으로 몸 반전(Flip) 시전]
        if (mouseWorldPosition.x > transform.position.x)
        {
            transform.eulerAngles = Vector3.zero; // 우측 조준 시 원래 회전값 그대로
        }
        else
        {
            transform.eulerAngles = new Vector3(0f, 180f, 0f); // 좌측 조준 시 Y축 180도 순간 회전
        }

        // 7. 최종 검광 이펙트를 마우스 방향 회전값에 장착하여 스폰!
        Instantiate(slashEffectPrefab, spawnPosition, spawnRotation);

        // 8. [C# 다형성 타격 집행]
        PerformAttackCollision(spawnPosition);
    }

    /// <summary>
    /// [C# 실시간 원형 범위 스캔 타격기]
    /// </summary>
    private void PerformAttackCollision(Vector3 spawnPosition)
    {
        // 1. [원형 범위 레이더 충돌체 스캔]
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(spawnPosition, attackRadius, enemyLayer);

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            // [벽/장애물 가로막힘 체크 - 정밀 타격 차단]
            // 플레이어 중심에서 적의 충돌체 중심 방향으로 레이저를 발사하여 중간에 벽이나 지면이 가로막고 있는지 체크합니다.
            Vector2 origin = transform.position;
            Vector2 target = enemyCollider.bounds.center;
            Vector2 direction = target - origin;
            float distance = direction.magnitude;

            // 지면(Ground)과 벽(Wall) 레이어를 장애물 필터로 지정합니다.
            LayerMask obstacleMask = LayerMask.GetMask(Globals.LayerName.ground, Globals.LayerName.wall);
            RaycastHit2D hit = Physics2D.Raycast(origin, direction.normalized, distance, obstacleMask);

            // 중간에 장애물(벽 또는 땅)이 먼저 닿았다면 벽 넘어에 있는 적이므로 타격 대상에서 즉시 제외합니다.
            if (hit.collider != null)
            {
                Debug.Log($"[물리 타격 실패] 대상 {enemyCollider.name}이(가) 벽 뒤에 숨어있어 타격이 가로막혔습니다.");
                continue;
            }

            // 2. [IDamageable 규격 통신 기법]
            IDamageable damageable = enemyCollider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage); // 적에게 대미지 전송!
                Debug.Log($"[물리 전투 타격] 적 스캔 완료! 대상: {enemyCollider.name}, 가한 대미지: {attackDamage}");
            }
        }
    }

    private void ResetAttackState()
    {
        IsAttacking = false; // 칼 다 휘둘렀으니 움직일 수 있도록 스위치 복원
    }

    private void OnDrawGizmosSelected()
    {
        // 유니티 에디터 화면에서 이 플레이어 오브젝트를 클릭하면,
        // 씬 뷰 화면 상에 하늘색 원형으로 실제 칼이 닿을 물리 타격 사정거리를 시각적으로 그려줍니다.
        Gizmos.color = Color.cyan;
        Vector3 drawPosition;

        // 게임 실행 중(Play Mode)일 때는 마우스 조준 방향을 실시간 추적하여 360도 궤도로 가이드 원을 그려줍니다.
        if (Application.isPlaying && Camera.main != null)
        {
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPosition.z = 0f;
            Vector3 targetDirection = (mouseWorldPosition - transform.position).normalized;
            float offsetDistance = effectOffset.x;
            drawPosition = transform.position + targetDirection * offsetDistance;
        }
        else
        {
            // 에디트 모드(비실행 상태)에서는 캐릭터가 바라보는 수평 정면 방향에 고정해서 가이드 원을 그려줍니다.
            Vector3 defaultOffset = transform.position + transform.rotation * effectOffset;
            drawPosition = defaultOffset;
        }

        Gizmos.DrawWireSphere(drawPosition, attackRadius);
    }
}