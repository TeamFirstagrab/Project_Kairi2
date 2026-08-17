using UnityEngine; // 유니티 엔진의 기본 기능(MonoBehaviour, GameObject, Transform 등)을 사용하기 위한 네임스페이스입니다.
using EnumType;    // 프로젝트 내 정의된 공용 열거형(예: KimEnemyState 등)을 참조하기 위한 네임스페이스입니다.
using System.Collections.Generic; // FSM 상태 관리를 위해 Dictionary, List 등의 컬렉션을 사용하기 위한 네임스페이스입니다.

#if UNITY_EDITOR
using UnityEditor; // 유니티 에디터 상에서 사거리 및 시야각을 시각적으로 그리기 위한 에디터 API입니다. 빌드 시 에러를 방지하기 위해 전처리기로 감쌉니다.
#endif

/// <summary>
/// [Kim 에너미 본체 스크립트]
/// 1. 유니티의 MonoBehaviour를 상속받아 게임 오브젝트에 컴포넌트로 부착되어 동작합니다.
/// 2. IDamageable 인터페이스를 구현하여, 외부(플레이어 공격 등)로부터 일관된 규격으로 데미지를 입을 수 있도록 설계되었습니다.
/// 3. 유한 상태 머신(FSM) 상태 패턴을 구동하고, 물리 컴포넌트(Rigidbody2D)와 애니메이터(Animator)를 캐싱하여 제어합니다.
/// 4. 기획 데이터를 하드코딩하지 않고 데이터 컨테이너인 ScriptableObject(KimEnemyStats)를 연결하여 독립적으로 관리합니다.
/// </summary>
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("에너미 능력치 데이터 (ScriptableObject)")]
    [Tooltip("에너미의 기본 스펙 데이터(체력, 이동속도, 감지 범위 등)가 담긴 스크립터블 오브젝트 에셋을 등록합니다.")]
    public EnemyStats enemyStats;

    // 매 프레임 GetComponent를 호출하는 물리 연산 비용을 줄이기 위해 Awake에서 한 번만 찾아 메모리에 캐싱해두는 물리 컴포넌트 변수입니다.
    [HideInInspector] public Rigidbody2D rb;
    // 적의 상태 변화에 따라 애니메이션 파라미터를 제어하기 위해 Awake에서 캐싱해두는 애니메이터 컴포넌트 변수입니다.
    [HideInInspector] public Animator anim;

    // FSM 상태들을 열거형 키값으로 저장하고 관리하는 딕셔너리입니다. (상태 클래스들을 재사용하여 가비지 컬렉터 부담을 낮춥니다.)
    public Dictionary<KimEnemyState, IEnemyState> stateList;

    // FSM에서 현재 적이 어떤 행동 상태(대기, 순찰, 추적 등)에 있는지를 저장하는 제어용 상태 변수입니다.
    private KimEnemyState currentEnemyState;

    // 실시간으로 변동하는 현재 체력 수치입니다. 다른 클래스(피격 판정부 등)에서 접근할 수 있도록 public으로 두되,
    // 인스펙터 창이 지저분해지거나 에셋 밸런스 설정과 혼동되는 것을 방지하기 위해 [HideInInspector]로 숨겨둡니다.
    [HideInInspector] public int currentHP;

    [Header("원거리 공격 설정")]
    [Tooltip("체크하면 원거리 공격 상태(KimEnemyRangedAttack)를 사용하고, 체크 해제하면 근접 공격 상태(KimEnemyAttack)를 사용합니다.")]
    public bool isRanged = false;
    
    [Tooltip("원거리 적일 경우 발사할 총알(Bullet)의 원본 프리팹을 유니티 에디터 인스펙터에서 드래그하여 등록합니다.")]
    public GameObject bulletPrefab;
    
    [Tooltip("총알이 생성되어 날아갈 기준점(발사구 위치) 오브젝트의 Transform 컴포넌트를 등록합니다.")]
    public Transform firePoint;

    /// <summary>
    /// Awake는 게임 오브젝트가 생성되고 스크립트가 로드될 때 '최초 1회' 가장 먼저 호출되는 생명주기 함수입니다.
    /// 주로 다른 컴포넌트 참조를 가져오거나 내부 변수를 안전하게 캐싱(초기화)하는 데 사용됩니다.
    /// </summary>
    private void Awake()
    {
        // 런타임 중에 매번 GetComponent를 실행하면 성능 저하(비용이 큼)가 발생하므로, 
        // 씬 시작 시점에 본체 오브젝트에 부착된 Rigidbody2D와 Animator 컴포넌트를 미리 변수에 할당(캐싱)해 둡니다.
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// Start는 Awake 이후, 그리고 첫 번째 Update 프레임이 실행되기 바로 직전에 '최초 1회' 호출되는 생명주기 함수입니다.
    /// 다른 오브젝트나 스크립터블 오브젝트(데이터)가 완전히 준비된 상태에서 값을 받아와 설정하는 초기화 작업에 적합합니다.
    /// </summary>
    private void Start()
    {
        // 스크립터블 오브젝트에 기획자가 세팅해 둔 체력 수치(HP)를 실시간 체력 변수(currentHP)에 대입하여 기본 스펙을 세팅합니다.
        if (enemyStats != null)
        {
            currentHP = (int)enemyStats.HP;
        }
        else
        {
            // 기획 에셋이 실수로 누락되었을 경우 게임이 뻗는 현상을 방지하기 위한 안전 예외처리(백업용 체력 세팅)입니다.
            currentHP = 1;
            Debug.LogWarning($"{gameObject.name}에 EnemyStats 에셋이 등록되지 않아 기본 체력 3으로 세팅되었습니다.");
        }

        // FSM 상태 리스트를 등록하고 최초 상태를 지정하는 초기화 메서드를 구동합니다.
        InitStateList();
    }

    /// <summary>
    /// Update는 유니티에서 매 프레임(기기의 성능에 따라 초당 수십~수백 번) 주기적으로 실행되는 생명주기 함수입니다.
    /// 실시간 입력 탐지, FSM의 상태 지속 로직 실행 등 실시간 제어에 활용됩니다.
    /// </summary>
    private void Update()
    {
        // 현재 FSM이 딕셔너리에 등록된 유효한 상태인지 확인하고, 해당 상태 클래스의 UpdateState 로직을 매 프레임 구동합니다.
        if (stateList.ContainsKey(currentEnemyState))
        {
            stateList[currentEnemyState]?.UpdateState(this);
        }
    }

    /// <summary>
    /// [FSM 상태 리스트 초기화]
    /// 행동 상태에 필요한 각각의 상태 객체(Class)들을 생성하여 딕셔너리에 매핑해둡니다.
    /// 원거리 여부(isRanged) 필드값에 따라 근접 공격 상태와 원거리 공격 상태를 동적으로 주입합니다.
    /// </summary>
    private void InitStateList()
    {
        stateList = new Dictionary<KimEnemyState, IEnemyState>();
        
        // 공통 기본 상태 객체들을 할당합니다.
        stateList[KimEnemyState.IDLE] = new EnemyIdle();
        stateList[KimEnemyState.PATROL] = new EnemyPatrol();
        stateList[KimEnemyState.CHASE] = new EnemyChase();
        stateList[KimEnemyState.DEAD] = new EnemyDead();
        
        
        // 기획/에디터 설정 상 원거리 적인지 근거리 적인지에 따라 공격 상태의 구체 클래스를 다르게 결정(의존성 분기 주입)합니다.
        if (isRanged)
        {
            stateList[KimEnemyState.ATTACK] = new EnemyRangedAttack();
        }
        else
        {
            stateList[KimEnemyState.ATTACK] = new EnemyAttack();
        }
        
        // 적이 스폰되었을 때 시작할 기본 상태를 IDLE(대기) 상태로 할당하고 EnterState를 트리거합니다.
        currentEnemyState = KimEnemyState.IDLE;
        ChangeState(currentEnemyState);
    }

    /// <summary>
    /// [FSM 상태 강제 전환 함수]
    /// 이전 상태의 퇴장(ExitState) 처리를 수행하고 새 상태로 교체한 뒤, 새 상태의 입장(EnterState)을 실행합니다.
    /// </summary>
    /// <param name="nextState">전환하고자 하는 다음 KimEnemyState 열거형 값</param>
    public void ChangeState(KimEnemyState nextState)
    {
        // 1. 현재 수행 중이던 이전 상태 클래스가 있다면, 퇴장 로직(애니메이션 파라미터 리셋, 속도 정지 등)을 안전하게 실행합니다.
        if (stateList.ContainsKey(currentEnemyState))
        {
            stateList[currentEnemyState]?.ExitState(this);
        }

        // 2. 현재 상태 변수를 새로운 타깃 상태로 변경합니다.
        currentEnemyState = nextState;

        // 3. 새로 진입한 상태 클래스가 딕셔너리에 정상적으로 들어있는지 확인한 후, 입장 로직(애니메이션 재생 시작, 타이머 리셋 등)을 구동합니다.
        if (stateList.ContainsKey(currentEnemyState))
        {
            stateList[currentEnemyState]?.EnterState(this);
        }
    }

    /// <summary>
    /// [데미지 피격 연산 (IDamageable 인터페이스 구현 규격)]
    /// 플레이어의 공격 판정 스크립트 등에서 호출되며, 적의 체력을 깎고 사망 상태로 전환합니다.
    /// </summary>
    /// <param name="attackDamage">피격 시 깎이게 될 데미지 양</param>
    [Header("Kill Slash Effect")]
    [SerializeField] private GameObject killSlashEffectPrefab;
    [SerializeField] private GameObject bloodEffectPrefab;
    [SerializeField] private float bloodEffectOffset = 0.5f;

    private void SpawnBloodEffect(Vector2 dir)
    {
        if (bloodEffectPrefab == null) return;
        Vector3 spawnPosition = transform.position + (Vector3)(dir * bloodEffectOffset);
        GameObject effect = Instantiate(bloodEffectPrefab, spawnPosition, Quaternion.identity);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        effect.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void SpawnKillSlash(Vector2 dir)
    {
        if (killSlashEffectPrefab == null) return;
        GameObject effect = Instantiate(killSlashEffectPrefab, transform.position, Quaternion.identity);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        effect.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void TakeDamage(int attackDamage, Vector2 attackDirection)
    {
        TakeDamage(attackDamage, attackDirection, true);
    }

    public void TakeDamage(int attackDamage, Vector2 attackDirection, bool isSlash)
    {
        // 1. [이미 사망한 상태에서의 중복 타격 방지 장치]
        // 적이 이미 죽어가거나 체력이 없는 상태에서 공격을 연속으로 받을 때 피격 사운드가 겹치거나 
        // 사망 애니메이션이 중복으로 재생되는 등 오작동을 차단하기 위한 예외 방어코드입니다.
        if (currentHP <= 0 || currentEnemyState == KimEnemyState.DEAD) return;

        // 2. 체력을 대미지 크기만큼 차감합니다.
        currentHP -= attackDamage;

        // 3. 체력이 0 이하가 되었다면, 음수 값으로 떨어지지 않게 0으로 보정하고 강제로 DEAD(사망) 상태로 FSM을 전환합니다.
        if (currentHP <= 0)
        {
            currentHP = 0;
            if (isSlash)
            {
                SpawnKillSlash(attackDirection);
            }
            SpawnBloodEffect(attackDirection); ChangeState(KimEnemyState.DEAD);
        }
    }

    /// <summary>
    /// [에디터 디버깅용 범위 시각화 기능]
    /// 이 객체가 선택(Select)되어 있을 때 씬 뷰(Scene View) 창에 시안성을 높여줄 디버그 가이드를 그려줍니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 기획 데이터 에셋이 연결되어 있지 않으면 사거리를 계산할 수 없으므로 바로 반환(종료)합니다.
        if (enemyStats == null) return;

        // 1. [부채꼴 시야 탐지 범위 시각화 (SightRange & SightAngle 연동)]
        // Handles API는 빌드된 게임(배포판) 환경에서는 존재하지 않는 Editor 전용 API이므로, 빌드 에러를 방지하기 위해 #if UNITY_EDITOR 전처리기로 보호해야 합니다.
#if UNITY_EDITOR
        // (1) 부채꼴을 채워줄 면의 색상을 반투명한 노란색으로 정의합니다.
        Handles.color = new Color(1f, 0.92f, 0.016f, 0.12f);
        
        // (2) 적의 Y축 회전값(transform.eulerAngles.y)에 따라 왼쪽 또는 오른쪽을 바라보고 있는지를 탐지합니다. (Unity 2D 좌우 반전 기준)
        Vector3 facingDir = transform.eulerAngles.y > 90f ? Vector3.left : Vector3.right;
        
        // (3) 시야 각도의 시작 지점 벡터를 구합니다. (바라보는 방향 기준 아래쪽 경계)
        Vector3 startDir = Quaternion.Euler(0f, 0f, -enemyStats.SightAngle) * facingDir;
        
        // (4) 부채꼴의 채워진 면(Solid Arc)을 그립니다. 적의 위치에서, 월드 앞쪽(Z축)을 법선벡터로 두고, 시작 각도에서부터 시야각의 2배 범위만큼 기획 사거리 크기(SightRange)로 칠합니다.
        Handles.DrawSolidArc(transform.position, Vector3.forward, startDir, enemyStats.SightAngle * 2f, enemyStats.SightRange);
        
        // (5) 시야 범위의 바깥 테두리 선(Wire Arc)을 진한 노란색으로 강조해 그립니다.
        Handles.color = Color.yellow;
        Handles.DrawWireArc(transform.position, Vector3.forward, startDir, enemyStats.SightAngle * 2f, enemyStats.SightRange);
        
        // (6) 부채꼴의 좌우 경계 끝에서 적으로 뻗어지는 직선(가이드라인)을 그려 시각적으로 깔끔한 부채꼴을 완성합니다.
        Vector3 endDir = Quaternion.Euler(0f, 0f, enemyStats.SightAngle) * facingDir;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + startDir * enemyStats.SightRange);
        Gizmos.DrawLine(transform.position, transform.position + endDir * enemyStats.SightRange);
#endif

        // 2. [공격 유효 범위 시각화 (AttackRange 연동)]
        // (1) 적 본체의 원거리 또는 근접 공격이 유효하게 도달하는 거리를 투명도가 들어간 빨간색 구형태의 면으로 그립니다.
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, enemyStats.AttackRange);
        
        // (2) 공격 범위의 확실한 확인을 위해 진한 빨간색 테두리 구 형태 선을 덧그립니다.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyStats.AttackRange);
    }
}