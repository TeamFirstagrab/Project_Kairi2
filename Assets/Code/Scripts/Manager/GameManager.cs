using UnityEngine;

public class GameManager : MonoSingleton<GameManager>
{
	private static GameManager mngInstance = null;

	[Header("Manager 스크립트")]
	public PoolManager poolManager;

	[Header("플레이어 정보")]
	public GameObject playerObj;
	[HideInInspector] public PlayerController playerController;		// 컨트롤러 스크립트

	[Header("스탯")]
	public PlayerStats playerStats;
	public EnemyStats enemyStats;

	[Header("게임 실행 중 플레이어 스탯값 수정")]
	public PlayerStatsRuntime playerStatsRuntime;

	protected new void Awake()
	{
		// 스크립트 불러오기
		playerController = GetComponent<PlayerController>();

		// 창 설정
		Screen.fullScreenMode = FullScreenMode.Windowed;
		Screen.SetResolution(1920, 1080, false);	// 원하는 창 크기

		// 매니저 설정 (싱글톤)
		if(mngInstance)		// 인스턴스가 있을 경우 오브젝트 파괴 후 리턴
		{
			DestroyImmediate(gameObject);
			return;
		}

		mngInstance = this;
		DontDestroyOnLoad(gameObject);
		QualitySettings.vSyncCount = 1;     // 모니터 주사율에 동기
		Application.targetFrameRate = -1;	// 제한 안 함

		if(Instance != null && Instance != this)    // 중복 GameManager 방지
		{
			Destroy(gameObject);
			return;
		}

		base.Awake();   // MonoSingleton의 Awake 호출

		// 플레이어 스탯
		if (playerStatsRuntime != null) // 플레이어 초기화
			playerStatsRuntime = new PlayerStatsRuntime(playerStats);	// 스탯 값 복제ㅋ
	}
}
