using UnityEngine;
using EnumType;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// [Kim 에너미 본체 스크립트]
/// FSM 상태 머신을 구동하고, 체력 정보와 물리 컴포넌트를 지니며,
/// 스크립터블 오브젝트 에셋인 EnemyStats를 연동하여 기획 수치를 처리하는 본체 클래스입니다.
/// </summary>
public class KimEnemy : MonoBehaviour, IDamageable
{
    [Header("에너미 능력치 데이터 (ScriptableObject)")]
    [Tooltip("에너미의 기본 스펙 데이터가 담긴 스크립터블 오브젝트 에셋을 등록합니다.")]
    public KimEnemyStats enemyStats;

    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public Animator anim;

    // FSM 상태 보관 딕셔너리
    public Dictionary<KimEnemyState, IKimEnemyState> stateList;

    // 현재 적의 상태
    private KimEnemyState currentEnemyState;

    // 실시간으로 변동하는 현재 체력 수치입니다. 다른 클래스에서 접근할 수 있도록 public으로 두되,
    // 인스펙터 창이 지저분해지거나 에셋 밸런스 설정과 혼동되는 것을 방지하기 위해 [HideInInspector]로 숨겨둡니다.
    [HideInInspector] public int currentHP;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        // 스크립터블 오브젝트에 작성해 둔 능력치 데이터를 기반으로 런타임 수치를 초기화합니다.
        if (enemyStats != null)
        {
            currentHP = (int)enemyStats.HP;
        }
        else
        {
            // 백업용 초기값
            currentHP = 3;
            Debug.LogWarning($"{gameObject.name}에 EnemyStats 에셋이 등록되지 않아 기본 체력 3으로 세팅되었습니다.");
        }

        InitStateList();
    }

    private void Update()
    {
        // FSM 상태별 실시간 업데이트 실행
        if (stateList.ContainsKey(currentEnemyState))
        {
            stateList[currentEnemyState]?.UpdateState(this);
        }
    }

    /// <summary>
    /// [FSM 상태 리스트 초기화]
    /// </summary>
    private void InitStateList()
    {
        stateList = new Dictionary<KimEnemyState, IKimEnemyState>();

        stateList[KimEnemyState.IDLE] = new KimEnemyIdle();
        stateList[KimEnemyState.PATROL] = new KimEnemyPatrol();
        stateList[KimEnemyState.CHASE] = new KimEnemyChase();
        stateList[KimEnemyState.ATTACK] = new KimEnemyAttack();
        stateList[KimEnemyState.DEAD] = new KimEnemyDead();

        // 기본 대기 상태로 시작
        currentEnemyState = KimEnemyState.IDLE;
        ChangeState(currentEnemyState);
    }

    /// <summary>
    /// [FSM 상태 강제 전환 함수]
    /// </summary>
    public void ChangeState(KimEnemyState nextState)
    {
        if (stateList.ContainsKey(currentEnemyState))
        {
            stateList[currentEnemyState]?.ExitState(this);
        }

        currentEnemyState = nextState;

        if (stateList.ContainsKey(currentEnemyState))
        {
            stateList[currentEnemyState]?.EnterState(this);
        }
    }

    /// <summary>
    /// [데미지 피격 연산 (IDamageable 규격)]
    /// </summary>
    public void TakeDamage(int attackDamage)
    {
        // 1. [이미 사망한 상태에서의 중복 타격 방지 장치]
        // 이미 체력이 0 이하이거나 현재 상태가 DEAD 상태인 경우, 추가적인 대미지 연산 및 상태 전환을 원천 차단합니다.
        if (currentHP <= 0 || currentEnemyState == KimEnemyState.DEAD) return;

        currentHP -= attackDamage;

        if (currentHP <= 0)
        {
            currentHP = 0;
            ChangeState(KimEnemyState.DEAD);
        }
    }

    /// <summary>
    /// [에디터 디버깅용 사거리 시각화 기능]
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (enemyStats == null) return;

        // 1. [부채꼴 시야 탐지 범위 시각화 (SightRange & SightAngle 연동)]
#if UNITY_EDITOR
        // 반투명 노란색 부채꼴 면 그리기
        Handles.color = new Color(1f, 0.92f, 0.016f, 0.12f);
        Vector3 facingDir = transform.eulerAngles.y > 90f ? Vector3.left : Vector3.right;
        
        // 부채꼴의 시작 각도 벡터 구하기
        Vector3 startDir = Quaternion.Euler(0f, 0f, -enemyStats.SightAngle) * facingDir;
        
        // 부채꼴 솔리드 아크 그리기
        Handles.DrawSolidArc(transform.position, Vector3.forward, startDir, enemyStats.SightAngle * 2f, enemyStats.SightRange);
        
        // 부채꼴 아크 테두리 그리기
        Handles.color = Color.yellow;
        Handles.DrawWireArc(transform.position, Vector3.forward, startDir, enemyStats.SightAngle * 2f, enemyStats.SightRange);
        
        // 부채꼴의 좌우 양끝 가이드라인 그리기
        Vector3 endDir = Quaternion.Euler(0f, 0f, enemyStats.SightAngle) * facingDir;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + startDir * enemyStats.SightRange);
        Gizmos.DrawLine(transform.position, transform.position + endDir * enemyStats.SightRange);
#endif

        // 2. 공격 유효 범위를 연한 빨간색으로 시각화 (AttackRange 필드 기준)
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, enemyStats.AttackRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyStats.AttackRange);
    }
}