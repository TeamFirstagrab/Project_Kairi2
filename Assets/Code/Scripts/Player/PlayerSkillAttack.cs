using UnityEngine;

public class PlayerSkillAttack : MonoBehaviour
{
    [Header("스킬 시전 시 슬로우 비율")]
    [SerializeField] private float slowFactor = 0.2f;
    [Header("플레이어 스킬 시전 최대 반지름")]
    [SerializeField] private float skillMaxRadius = 50f;
    private PlayerSlowMode slowMode;

    private void Awake()
    {
        slowMode = GetComponent<PlayerSlowMode>();
    }

    public void EnterSkill()
    {
        //slowMode.EnterSlow(slowFactor);


    }

    public void ExitSkill()
    {
        //slowMode.ExitSlow();
    }
}
