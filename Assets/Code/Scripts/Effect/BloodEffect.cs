using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BloodEffect : MonoBehaviour
{
	[Header("Blood")]
	[SerializeField] private List<Sprite> bloodSprites;

	[Range(0f, 30f)]
	[SerializeField] private float dist = 4f;

	[Range(0f, 1f)]
	[SerializeField] private float bloodDist = 0.5f;

	[SerializeField] private int bloodCount = 20;

	[Header("Spawn")]
	[SerializeField] private float activeTime = 0.5f;

	private bool isActive;

	public void ActiveBloodEffect(Vector2 dir)
	{
		if (isActive)
			return;

		StartCoroutine(SpawnBlood(dir.normalized));
	}

	private IEnumerator SpawnBlood(Vector2 dir)
	{
		isActive = true;

		// 최초 위치
		Vector2 currentPosition = transform.position;

		for (int i = 0; i < bloodCount; i++)
		{
			// 첫 번째 피는 원래 방향을 어느 정도 반영
			// 이후 피는 직전 위치를 기준으로 랜덤하게 이동
			if (i == 0)
			{
				currentPosition = GetNextPosition(
					currentPosition,
					dir
				);
			}
			else
			{
				currentPosition = GetNextPosition(
					currentPosition,
					Random.insideUnitCircle.normalized
				);
			}

			SpawnBloodSprite(currentPosition);

			yield return new WaitForSeconds(activeTime / bloodCount);
		}

		isActive = false;
	}

	private Vector2 GetNextPosition(Vector2 currentPosition, Vector2 direction)
	{
		Vector2 center = transform.position;

		// 최대 20번까지 새로운 방향을 시도
		for (int i = 0; i < 20; i++)
		{
			Vector2 randomDirection =
				(direction + Random.insideUnitCircle * 0.5f).normalized;

			Vector2 nextPosition =
				currentPosition + randomDirection * bloodDist;

			// 중심에서 dist 거리 안에 있는지 확인
			if (Vector2.Distance(center, nextPosition) <= dist)
			{
				return nextPosition;
			}
		}

		// 적당한 방향을 찾지 못했다면
		// 중심 방향으로 이동
		Vector2 toCenter = (center - currentPosition).normalized;

		return currentPosition + toCenter * bloodDist;
	}

	private void SpawnBloodSprite(Vector2 spawnPosition)
	{
		GameObject blood = new GameObject("Blood");

		blood.transform.position = spawnPosition;

		SpriteRenderer renderer = blood.AddComponent<SpriteRenderer>();

		int spriteIdx = Random.Range(0, bloodSprites.Count);
		renderer.sprite = bloodSprites[spriteIdx];

		float scale = Random.Range(0.8f, 1.2f);
		blood.transform.localScale = Vector3.one * scale;
	}
}