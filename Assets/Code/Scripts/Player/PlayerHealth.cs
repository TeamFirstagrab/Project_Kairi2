using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IDamageable
{
	[Header("UI")]
	public GameObject glitchGlobalVolume;
	public GameObject tvGlobalVolume;
	public Image blackCanvas;

	[SerializeField] private Vector2 spawnPoint;

	private Rigidbody2D rigid;
	private PlayerDash dash;
	private PlayerSlowMode slowMode;

	private void Awake()
	{
		rigid = GetComponent<Rigidbody2D>();
		dash = GetComponent<PlayerDash>();
		slowMode = GetComponent<PlayerSlowMode>();
	}

	public void TakeDamage(int attack)
	{
		if (dash.isDashing) return;  // 대쉬 중 무적

		GameManager.Instance.playerStatsRuntime.currentHP -= attack;

		if (GameManager.Instance.playerStatsRuntime.currentHP <= 0)
		{
			slowMode.ExitSlow();
			StartCoroutine(PlayerDie());
		}
	}

	private IEnumerator PlayerDie()
	{
		if (glitchGlobalVolume && tvGlobalVolume)
		{
			glitchGlobalVolume.SetActive(true);
			tvGlobalVolume.SetActive(true);
			yield return new WaitForSeconds(0.5f);
			blackCanvas.gameObject.SetActive(true);
			GameManager.Instance.sceneReloader.SetAlpha(1f);
			yield return new WaitForSeconds(0.5f);
			Respawn();
		}

	}

	private void Respawn()
	{
		rigid.linearVelocity = Vector2.zero;
		transform.position = spawnPoint;
		GameManager.Instance.playerStatsRuntime.currentHP =
			GameManager.Instance.playerStats.maxHP;
		blackCanvas.gameObject.SetActive(false);
		glitchGlobalVolume.SetActive(false);
		tvGlobalVolume.SetActive(false);
	}
}