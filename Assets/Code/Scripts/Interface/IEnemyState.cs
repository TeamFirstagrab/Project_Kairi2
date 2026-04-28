public interface IEnemyState
{
	void EnterState(Enemy p_enemy);
	void UpdateState(Enemy p_enemy);
	void ExitState(Enemy p_enemy);
}
