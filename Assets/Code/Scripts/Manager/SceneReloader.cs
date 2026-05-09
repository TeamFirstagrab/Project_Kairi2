using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneReloader : MonoBehaviour
{
	public Image blackPanel;   // Canvas 안 Image
	public float fadeDuration = 1.5f;

	private void Start()
	{
		SetAlpha(1f);       // 씬 시작 시 바로 검정으로 세팅
		blackPanel.gameObject.SetActive(true);
		FadeOut();          // 페이드 아웃 시작
	}
	public void Reload()
	{
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}

	public void SetAlpha(float alpha)   // 알파값 바로 세팅
	{
		if (blackPanel == null) return;
		Color color = blackPanel.color;
		color.a = alpha;
		blackPanel.color = color;
	}

	public void FadeIn()
	{
		StartCoroutine(Fade(0f, 1f)); // 투명 -> 검정
	}

	public void FadeOut()
	{
		StartCoroutine(FadeOutCoroutine());
	}

	private IEnumerator FadeOutCoroutine()
	{
		yield return StartCoroutine(Fade(1f, 0f)); // 검정 -> 투명
		blackPanel.gameObject.SetActive(false);   // 페이드 끝나면 비활성화
	}

	private IEnumerator Fade(float from, float to)
	{
		if (blackPanel == null) yield break;

		blackPanel.gameObject.SetActive(true); // 먼저 활성화
		float timer = 0f;
		Color color = blackPanel.color;
		while (timer < fadeDuration)
		{
			timer += Time.deltaTime;
			color.a = Mathf.Lerp(from, to, timer / fadeDuration);
			blackPanel.color = color;
			yield return null;
		}
		color.a = to;
		blackPanel.color = color;
	}
}
