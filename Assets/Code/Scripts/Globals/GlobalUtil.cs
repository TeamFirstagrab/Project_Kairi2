using UnityEngine;

public class GlobalUtil : MonoBehaviour
{
	public static bool IsNullScript(object script)
	{
		if (script == null)
		{
			Debug.LogError($"스크립트 데이터 없음");
			return true;
		}

		return false;
	}
}
