using UnityEngine;

/// <summary>
/// ⚔️ [플레이어의 멋진 팔 역할 - 360도 마우스 조준 베기 대장!]
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

    // 💡 뇌 컨트롤러가 "너 지금 칼 휘두르는 중이니?" 하고 물어볼 때 대답해 줄 열쇠(Property)입니다.
    public bool IsAttacking { get; private set; }

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    /// <summary>
    /// ⚔️ [뇌 조종사의 명령 수신부 - 칼질을 시도해 봐요!]
    /// </summary>
    public void TryAttack()
    {
        // 💡 [연타 제동 장치] 지금 게임 시간(Time.time)이 마지막 공격 시간 + 쿨타임보다 적다면 ➡️ 씹고 리턴!
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time; // 공격 성공 시점에 현재 시간 기록!
        IsAttacking = true;         // 나 지금 베기 작동 중이야! 하고 스위치 ON

        if (anim != null)
        {
            // 애니메이터에 설정된 베기 모션("attack") 트리거를 탁 쳐서 재생시킵니다.
            anim.SetTrigger("attack");
        }

        // 💡 [정석 핵심] 칼을 벨 때 마우스 방향을 360도로 정밀 분석해 이펙트를 스폰합니다!
        SpawnSlashEffect();
        
        // 💡 0.25초 뒤에 자동으로 베기 모션을 끄고 다시 움직일 수 있게 제어기를 환원합니다.
        Invoke(nameof(ResetAttackState), 0.25f);
    }

    /// <summary>
    /// ✨ [카타나 제로 전용 360도 마우스 조준 검광 소환기]
    /// </summary>
    private void SpawnSlashEffect()
    {
        if (slashEffectPrefab == null) return;

        // 💡 1. [스크린좌표 ➡️ 월드좌표 변환]
        // 유저가 모니터 화면 상에서 움직이는 마우스 포인터의 2D 픽셀 위치(Screen)를
        // 카메라의 시야 비율을 역산하여 실제 게임 월드 속 진짜 2D 위치(World) 좌표로 번역합니다.
        
        // 🔍 [수학 및 그래픽스 상세 설명]
        //  - ScreenToWorldPoint 란?
        //    * 컴퓨터 모니터 화면은 좌측 하단이 (0,0)이고 우측 상단이 모니터 가로세로 픽셀 해상도 크기(예: 1920x1080)인 픽셀 좌표계(Screen Space)를 씁니다.
        //    * 하지만 우리가 만든 게임 속 캐릭터와 월드는 카메라가 비추고 있는 고유한 미터 단위의 물리적 좌표계(World Space)를 씁니다.
        //    * 이 함수는 화면 상에 유저가 조준한 마우스 커서 위치(픽셀)를 메인 카메라의 시야각과 깊이를 계산하여 "실제 2차원 게임 월드의 몇 미터(m) 위치"인지를 완벽히 환산해 줍니다!
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f; // 2D 평면이므로 Z축 깊이는 0으로 고정 격리

        // 💡 2. [조준 방향 벡터 계산]
        // 플레이어 몸통 중심점(transform.position)에서 마우스 월드 목표 좌표를 뺄셈하여 
        // 마우스를 향해 똑바로 뻗어 나가는 사선 방향선(targetDirection)을 획득합니다.
        
        // 🔍 [수학 공식 상세 설명]
        //  - (목표점 - 시작점) 공식
        //    * 수학에서 두 점 사이의 방향 벡터를 구할 때는 [도착하고 싶은 위치(마우스)]에서 [출발하는 기준 위치(플레이어)]를 빼줍니다.
        //    * 이렇게 뺄셈을 하면 플레이어 몸 한가운데서 마우스를 향해 화살을 쏘듯 일직선으로 곧게 뻗은 조준선 벡터가 탄생합니다.
        //  - .normalized 란?
        //    * 마우스의 거리가 얼마나 멀든 상관없이, 조준하고 있는 방향 비율만 100% 똑같이 남겨둔 채 대각선 총 길이를 딱 '1미터' 크기인 기본 벡터(Unit Vector, 단위 벡터)로 알맞게 축소시킵니다.
        Vector3 targetDirection = (mouseWorldPosition - transform.position).normalized;

        // 💡 3. [삼각함수 Atan2 각도 변환]
        // 삼각함수 아크탄젠트(Atan2) 공식을 사용해 방향 벡터의 Y축 높이와 X축 길이를 넣어 
        // 마우스 포인터가 360도 사방 중 몇 도(도 단위: Deg) 방향을 가리키고 있는지 완벽히 회전각으로 바꿉니다.
        
        // 🔍 [수학 공식 상세 설명]
        //  - Mathf.Atan2(y, x) * Mathf.Rad2Deg 란?
        //    * 가로 길이와 세로 높이를 넣으면 그 빗변 경사가 몇 도 꺾여 있는지 역삼각함수인 '아크탄젠트'로 풀어냅니다.
        //    * 여기에 3.141592...로 계산된 호도법(Radian) 결과를 우리가 각도기로 잴 때 쓰는 일반 도(Degree, 0~360도) 단위로 변환해 주는 비율값(Rad2Deg)을 곱해 회전 각도를 계산해 냅니다.
        float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;

        // 💡 4. [이펙트 스폰 지점 확정]
        // 캐릭터 몸 한가운데서 스폰하면 어색하므로, 마우스 방향을 향해 offsetDistance(1미터 등) 만큼 
        // 앞으로 툭 튀어나온 최적의 칼질 발동 포인트를 스폰 지점으로 정합니다.
        float offsetDistance = effectOffset.x; 
        Vector3 spawnPosition = transform.position + targetDirection * offsetDistance;

        // 💡 5. [각도 주입 Quaternion 획득]
        // 3차원 회전 데이터인 쿼터니언(Quaternion)을 활용하여 Z축 각도로 마우스를 똑바로 노려보는 회전값을 정렬합니다.
        
        // 🔍 [수학 공식 상세 설명]
        //  - Quaternion.Euler 란?
        //    * 3차원 공간에서 3개의 축(X, Y, Z)을 기준으로 물체를 돌리는 가상의 오일러 각도(Euler Angle) 회전 방식을 컴퓨터가 수학적 에러(짐벌락 현상) 없이 부드럽게 4차원 연산으로 계산해 주는 쿼터니언(Quaternion) 수치로 매끄럽게 변환해 줍니다.
        //    * 여기선 X, Y축은 회전하지 않도록 0으로 잠그고, 2D 지면 회전축인 Z축에 구한 경사각(`angle`)을 통째로 주입하여 마우스를 노려보는 회전 방향을 획득합니다.
        Quaternion spawnRotation = Quaternion.Euler(0f, 0f, angle);

        // 💡 6. [조준점 방향으로 몸 반전(Flip) 시전]
        // 공격하는 찰나만큼은 조준한 마우스 방향을 보도록 플레이어를 즉시 좌/우 Y축 180도 회전시켜 뒤집습니다.
        
        // 🔍 [수학 및 물리 상세 설명]
        //  - transform.eulerAngles 란?
        //    * 게임 오브젝트의 3차원 절대 회전각도를 오일러 값(도 단위)으로 직접 열고 닫는 속성입니다.
        //    * 마우스의 X축 월드 위치가 플레이어 중심 위치보다 오른쪽에 있다면 원래 정상 시선 방향(0도)으로 두고, 왼쪽에 있다면 Y축(수직 제자리 회전축)을 180도 홱 회전하여 플레이어 몸 전체 스프라이트를 좌우 대칭 반전시킵니다.
        if (mouseWorldPosition.x > transform.position.x)
        {
            transform.eulerAngles = Vector3.zero; // 우측 조준 시 원래 회전값 그대로
        }
        else
        {
            transform.eulerAngles = new Vector3(0f, 180f, 0f); // 좌측 조준 시 Y축 180도 순간 회전
        }

        // 💡 7. 최종 검광 이펙트를 마우스 방향 회전값에 장착하여 스폰!
        Instantiate(slashEffectPrefab, spawnPosition, spawnRotation);

        // 💡 8. [C# 다형성 타격 집행]
        // 멋진 비주얼 이펙트가 소환된 그 동그란 범위 자리를 실시간으로 휩쓸어 적에게 대미지를 가합니다.
        PerformAttackCollision(spawnPosition);
    }

    /// <summary>
    /// 💥 [C# 실시간 원형 범위 스캔 타격기]
    /// </summary>
    private void PerformAttackCollision(Vector3 spawnPosition)
    {
        // 💡 1. [원형 범위 레이더 충돌체 스캔]
        // 검광 이펙트 중심점(spawnPosition)을 기준으로 attackRadius(1.8m) 만큼 둥근 가상의 원을 그립니다.
        // 그리고 그 원의 범위 안에 닿아있는 모든 'Enemy' 레이어 대상들을 단 한 마리도 빠짐없이 싹 다 긁어모읍니다.
        
        // 🔍 [물리 연산 상세 설명]
        //  - Physics2D.OverlapCircleAll 란?
        //    * 유니티 물리 엔진에서 실시간으로 가장 널리 쓰이는 '둥근 레이더 충돌 스캐너'입니다.
        //    * 중심 좌표(`spawnPosition`)에서 일정한 반지름(`attackRadius`) 크기만큼 지면에 보이지 않는 거대한 가상의 원형 물리 그물을 실시간으로 딱 던집니다.
        //    * 그리고 그 원의 범위 영역에 닿아 있거나 겹쳐 있는 모든 적의 물리 충돌 상자(Collider2D)들을 단 한 마리도 빠짐없이 모조리 포획하여 배열(`Collider2D[]`) 주머니에 쏙 담아서 줍니다.
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(spawnPosition, attackRadius, enemyLayer);

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            // 💡 2. [IDamageable 규격 통신 기법 - 최고 중요!]
            // 닿은 상대방의 정체를 굳이 확인(GetComponent<Enemy> 등)하지 않고,
            // 오직 "너 대미지를 입을 수 있는 녀석(IDamageable)이니?" 하고 묻고 맞다면 TakeDamage 명령을 실행합니다!
            // 이 덕분에 나중에 상자나 파괴물이 생겨도 플레이어 공격 코드를 전혀 바꾸지 않고 때릴 수 있습니다!
            
            // 🔍 [C# 객체 지향 프로그래밍 상세 설명]
            //  - 인터페이스(Interface) 다형성 이란?
            //    * 적 캐릭터 종류(일반 몹, 보스 몹, 부서지는 상자, 터지는 폭탄 등)가 수십 가지여도 그 컴포넌트 이름을 일일이 물어볼 필요가 없도록 만든 가상의 약속(규격 규정)입니다.
            //    * "대미지를 받을 수 있는 모든 사물은 오직 IDamageable 규격(프로토콜)을 준수해서 태어나라!" 라고 정해둡니다.
            //    * 그 덕분에 플레이어는 상대가 누구이든 오직 이 통일된 규격(`IDamageable`) 장착 여부만 물어보고, 대미지를 가하는 명령(`TakeDamage()`)을 간결하게 일괄 실행할 수 있게 되어 매우 깔끔하고 확장성이 뛰어난 코드가 됩니다!
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
        // 💡 유니티 에디터 화면에서 이 플레이어 오브젝트를 클릭하면, 
        // 씬 뷰 화면 상에 하늘색 원형으로 실제 칼이 닿을 물리 타격 사정거리를 시각적으로 편하게 그려줍니다.
        Gizmos.color = Color.cyan;
        Vector3 defaultOffset = transform.position + transform.rotation * effectOffset;
        Gizmos.DrawWireSphere(defaultOffset, attackRadius);
    }
}