using UnityEngine;

public class BloodEffectTestScript : MonoBehaviour
{
	public bool isActiveEffect = false;

	private BloodEffect effect;
	private bool previousActive;

	private void Awake()
	{
		effect = GetComponent<BloodEffect>();
	}

	private void Update()
	{
		// false -> true로 바뀌었을 때만 실행
		if (isActiveEffect && !previousActive)
		{
			effect.ActiveBloodEffect(Random.insideUnitCircle.normalized);
		}
		// isActiveEffect를 껐을 때
		else if (!isActiveEffect && previousActive)
		{
		}

			previousActive = isActiveEffect;
	}
}